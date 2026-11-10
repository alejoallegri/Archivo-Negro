#if UNITY_EDITOR
using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ProjectOutbreak.Editor
{
    public static class OutbreakBuilder
    {
        private const string Root = "Assets/ProjectOutbreak";
        private static readonly Color FloorColor = new Color(.18f, .23f, .26f);
        private static readonly Color WallColor = new Color(.28f, .34f, .39f);

        [MenuItem("Project Outbreak/1 - Generar prototipo")]
        public static void Build()
        {
            var playerPrefab = FindStarterPlayer();
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Falta Starter Assets",
                    "No encontré el prefab oficial PlayerCapsule con FirstPersonController. Revisá que hayas importado también los archivos de First Person de Starter Assets.", "Entendido");
                return;
            }

            if (!EditorUtility.DisplayDialog("Generar Project Outbreak",
                "Se creará una escena NUEVA dentro de Assets/ProjectOutbreak. No se modificará tu escena actual. ¿Continuar?", "Generar", "Cancelar")) return;

            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Materials");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var level = new GameObject("Instalacion - NavMesh");
            CreateBlock(level.transform, "Suelo", new Vector3(0, -.3f, 0), new Vector3(36, .6f, 36), FloorColor);
            CreateBlock(level.transform, "Pared norte", new Vector3(0, 2.2f, 18), new Vector3(36, 5, .7f), WallColor);
            CreateBlock(level.transform, "Pared sur", new Vector3(0, 2.2f, -18), new Vector3(36, 5, .7f), WallColor);
            CreateBlock(level.transform, "Pared este", new Vector3(18, 2.2f, 0), new Vector3(.7f, 5, 36), WallColor);
            CreateBlock(level.transform, "Pared oeste", new Vector3(-18, 2.2f, 0), new Vector3(.7f, 5, 36), WallColor);
            // Obstáculos para que la navegación tenga que rodearlos.
            CreateBlock(level.transform, "Contenedor A", new Vector3(-6, 1, -2), new Vector3(3, 2, 7), WallColor);
            CreateBlock(level.transform, "Contenedor B", new Vector3(6, 1, 4), new Vector3(4, 2, 6), WallColor);
            CreateBlock(level.transform, "Contenedor C", new Vector3(0, 1, 10), new Vector3(6, 2, 2), WallColor);
            CreateBlock(level.transform, "Cobertura", new Vector3(2, .9f, -8), new Vector3(5, 1.8f, 2), WallColor);

            var nav = level.AddComponent<NavMeshSurface>();
            nav.collectObjects = CollectObjects.Children;
            nav.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            nav.BuildNavMesh();

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Jugador - Starter Assets oficial";
            player.transform.position = new Vector3(0, 1.05f, -13);
            player.tag = "Player";
            // Starter Assets gira el objeto CinemachineCameraTarget; acoplamos una cámara real.
            Transform cameraTarget = FindChild(player.transform, "CinemachineCameraTarget");
            if (cameraTarget == null) cameraTarget = FindChild(player.transform, "CameraTarget");
            if (cameraTarget == null)
            {
                cameraTarget = new GameObject("CinemachineCameraTarget").transform;
                cameraTarget.SetParent(player.transform, false);
                cameraTarget.localPosition = new Vector3(0, 1.6f, 0);
            }
            var cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(cameraTarget, false);
            cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();

            var muzzle = new GameObject("Salida del arma").transform;
            muzzle.SetParent(cameraGo.transform, false);
            muzzle.localPosition = new Vector3(.28f, -.19f, .7f);
            CreateBlock(cameraGo.transform, "Arma simple", new Vector3(.28f, -.26f, .4f),
                new Vector3(.14f, .18f, .55f), new Color(.09f, .1f, .12f), false);

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Proyectil";
            projectile.transform.localScale = Vector3.one * .16f;
            projectile.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(1f, .78f, .2f));
            var body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectile.AddComponent<OutbreakProjectile>();
            var bulletPrefab = PrefabUtility.SaveAsPrefabAsset(projectile, Root + "/Prefabs/Proyectil.prefab");
            UnityEngine.Object.DestroyImmediate(projectile);

            var gun = cameraGo.AddComponent<OutbreakGun>();
            gun.muzzle = muzzle;
            gun.projectilePrefab = bulletPrefab;

            int count = 0;
            foreach (var p in new[] { new Vector3(-13, 0, 11), new Vector3(13, 0, 12),
                new Vector3(-12, 0, -4), new Vector3(12, 0, -9), new Vector3(0, 0, 15) })
            {
                if (!NavMesh.SamplePosition(p, out var hit, 3, NavMesh.AllAreas)) continue;
                var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = "Infectado " + (++count);
                enemy.transform.position = hit.position + Vector3.up;
                enemy.GetComponent<Renderer>().sharedMaterial = MaterialFor(new Color(.49f, .75f, .42f));
                var agent = enemy.AddComponent<NavMeshAgent>();
                agent.speed = 2.8f;
                agent.angularSpeed = 500;
                agent.acceleration = 10;
                agent.stoppingDistance = 1.5f;
                enemy.AddComponent<OutbreakEnemy>();
            }

            var game = new GameObject("Sistema de partida").AddComponent<OutbreakGame>();
            game.totalEnemies = count;
            var sun = new GameObject("Luz principal").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.5f;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            RenderSettings.ambientLight = new Color(.48f, .5f, .55f);

            var scenePath = Root + "/Scenes/ProjectOutbreak.unity";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Project Outbreak generado",
                "Escena creada con " + count + " enemigos. Presioná Play. Si el personaje no se mueve, verificaremos la configuración de Starter Assets.", "Listo");
        }

        private static GameObject FindStarterPlayer()
        {
            foreach (var id in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(id);
                if (!path.Contains("Starter Assets") && !path.Contains("StarterAssets")) continue;
                if (!path.Contains("FirstPerson") && !path.Contains("First Person") && !path.Contains("PlayerCapsule")) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.GetComponent("FirstPersonController") != null) return go;
            }
            return null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static GameObject CreateBlock(Transform parent, string label, Vector3 position,
            Vector3 size, Color color, bool worldPosition = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(parent, false);
            if (worldPosition) go.transform.position = position;
            else go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = MaterialFor(color);
            return go;
        }

        private static Material MaterialFor(Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            string path = Root + "/Materials/Color_" + hex + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
