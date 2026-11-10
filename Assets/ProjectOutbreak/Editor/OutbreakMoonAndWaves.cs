#if UNITY_EDITOR
using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace ProjectOutbreak.Editor
{
    public static class OutbreakMoonAndWaves
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string Output = Root + "/Scenes/ProjectOutbreakLuna.unity";

        [MenuItem("Project Outbreak/8 - Luna, refugios y oleadas")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/ProjectOutbreakCementerio.unity" && scene.path != Output)
            {
                EditorUtility.DisplayDialog("Abrí el cementerio",
                    "Abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakCementerio.unity antes de ejecutar esta mejora.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("Luna, refugios y oleadas",
                "Mejorar iluminación, crear refugios y activar enemigos en oleadas seguras tras varias bajas. Se guardará otra escena. ¿Continuar?", "Generar", "Cancelar")) return;

            try
            {
                var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                var game = UnityEngine.Object.FindFirstObjectByType<OutbreakGame>();
                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(FindObjectsSortMode.None);
                if (nav == null || game == null || enemies.Length < 9)
                    throw new Exception("No encontré el NavMesh, sistema de partida o suficientes infectados.");

                var level = nav.transform;
                Transform previous = level.Find("Refugios y luz lunar");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var root = new GameObject("Refugios y luz lunar").transform;
                root.SetParent(level, false);
                var masonry = MaterialFor("Piedra_refugio", new Color(.24f, .25f, .28f));
                var roof = MaterialFor("Techo_oscuro", new Color(.13f, .14f, .18f));

                EditorUtility.DisplayProgressBar("Cementerio", "Construyendo refugios...", .15f);
                BuildShelter(root, "Refugio noroeste", new Vector3(-13, 0, 1), masonry, roof);
                BuildShelter(root, "Refugio noreste", new Vector3(13, 0, -3), masonry, roof);
                BuildShelter(root, "Refugio suroeste", new Vector3(-14, 0, -18), masonry, roof);
                BuildShelter(root, "Refugio sureste", new Vector3(14, 0, -18), masonry, roof);

                // Retirar lápidas de las parcelas ocupadas por los refugios.
                var cemetery = level.Find("Cementerio nocturno");
                if (cemetery != null)
                {
                    Vector3[] shelterCenters =
                    {
                        new(-13,0,1), new(13,0,-3), new(-14,0,-18), new(14,0,-18)
                    };
                    foreach (Transform grave in cemetery)
                    {
                        if (!grave.name.StartsWith("Sepultura")) continue;
                        foreach (var center in shelterCenters)
                            if (Vector2.Distance(new Vector2(grave.position.x, grave.position.z),
                                new Vector2(center.x, center.z)) < 5.3f)
                                grave.gameObject.SetActive(false);
                    }
                }

                EditorUtility.DisplayProgressBar("Cementerio", "Ajustando luz de luna y niebla...", .45f);
                // La iluminación de la luna debe permitir reconocer siluetas a distancia.
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.10f, .14f, .22f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(.043f, .063f, .11f);
                RenderSettings.fogDensity = .014f;
                foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type != LightType.Directional) continue;
                    light.intensity = .52f;
                    light.color = new Color(.50f, .65f, 1f);
                    light.shadows = LightShadows.Soft;
                    light.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
                }

                var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                moon.name = "Luna visible";
                moon.transform.SetParent(root, false);
                moon.transform.position = new Vector3(-60f, 87f, 130f);
                moon.transform.localScale = Vector3.one * 11f;
                UnityEngine.Object.DestroyImmediate(moon.GetComponent<Collider>());
                var unlit = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlit == null) unlit = Shader.Find("Unlit/Color");
                string moonPath = Root + "/Materials/Luna_blanca.mat";
                var moonMat = AssetDatabase.LoadAssetAtPath<Material>(moonPath);
                if (moonMat == null)
                {
                    moonMat = new Material(unlit);
                    AssetDatabase.CreateAsset(moonMat, moonPath);
                }
                if (moonMat.HasProperty("_BaseColor")) moonMat.SetColor("_BaseColor", new Color(.79f, .86f, 1f));
                else moonMat.color = new Color(.79f, .86f, 1f);
                moon.GetComponent<Renderer>().sharedMaterial = moonMat;

                EditorUtility.DisplayProgressBar("Cementerio", "Horneando rutas y organizando oleadas...", .7f);
                nav.BuildNavMesh();

                // Solo ocho infectados activos al empezar. El resto espera fuera del mapa.
                // Las posiciones iniciales están a más de 20 m del comienzo del jugador.
                Vector3[] initialPositions =
                {
                    new(-22,0,-18), new(22,0,-18), new(-24,0,6), new(24,0,6),
                    new(-16,0,22), new(16,0,22), new(-6,0,25), new(6,0,25)
                };
                for (int i = 0; i < enemies.Length; i++)
                {
                    if (i < initialPositions.Length)
                    {
                        if (NavMesh.SamplePosition(initialPositions[i], out var hit, 5f, NavMesh.AllAreas))
                            enemies[i].transform.position = hit.position + Vector3.up;
                        enemies[i].gameObject.SetActive(true);
                    }
                    else enemies[i].gameObject.SetActive(false);
                }

                Transform spawns = root.Find("Puntos de aparicion seguros");
                if (spawns != null) UnityEngine.Object.DestroyImmediate(spawns.gameObject);
                spawns = new GameObject("Puntos de aparicion seguros").transform;
                spawns.SetParent(root, false);
                Vector3[] markers =
                {
                    new(-25,0,24), new(25,0,24), new(-25,0,-24),
                    new(25,0,-24), new(0,0,26), new(-25,0,0), new(25,0,0)
                };
                var points = new Transform[markers.Length];
                for (int i = 0; i < markers.Length; i++)
                {
                    var marker = new GameObject("Spawn lejano " + (i + 1)).transform;
                    marker.SetParent(spawns, false);
                    marker.position = markers[i];
                    points[i] = marker;
                }

                var oldSpawner = UnityEngine.Object.FindFirstObjectByType<OutbreakWaveSpawner>();
                if (oldSpawner != null) UnityEngine.Object.DestroyImmediate(oldSpawner.gameObject);
                var spawner = new GameObject("Sistema de oleadas").AddComponent<OutbreakWaveSpawner>();
                spawner.reserveEnemies = new OutbreakEnemy[enemies.Length - 8];
                Array.Copy(enemies, 8, spawner.reserveEnemies, 0, enemies.Length - 8);
                spawner.safeSpawnPoints = points;
                spawner.firstWaveKills = 3;
                spawner.secondWaveKills = 8;
                spawner.firstWaveCount = 5;
                game.totalEnemies = enemies.Length; // Incluye reservas: ganar exige eliminarlos a todos.

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Output);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Output, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Luna y oleadas listas",
                    "Se creó ProjectOutbreakLuna.unity. 8 infectados iniciales; 5 tras 3 bajas y el resto tras 8 bajas. " +
                    "Los puntos de spawn se eligen lejos de donde estés. Se añadieron cuatro refugios y luz de luna.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("No se completó", ex.Message + "\nEnviame una captura de Console.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void BuildShelter(Transform parent, string name, Vector3 position, Material wall, Material roof)
        {
            var s = new GameObject(name).transform;
            s.SetParent(parent, false);
            s.position = position;
            // Refugio abierto por el sur: el jugador puede entrar y rodearlo por fuera.
            Block(s, "Pared posterior", new Vector3(0, 1.4f, 3.25f), new Vector3(7, 2.8f, .55f), wall);
            Block(s, "Costado oeste", new Vector3(-3.2f, 1.4f, 0), new Vector3(.55f, 2.8f, 6.2f), wall);
            Block(s, "Costado este", new Vector3(3.2f, 1.4f, 0), new Vector3(.55f, 2.8f, 6.2f), wall);
            Block(s, "Techo", new Vector3(0, 3.1f, 0), new Vector3(7.6f, .42f, 7.1f), roof);
            Block(s, "Banco", new Vector3(-1.6f, .5f, 1.2f), new Vector3(2.0f, 1f, .75f), wall);
            var lamp = new GameObject("Luz refugio");
            lamp.transform.SetParent(s, false);
            lamp.transform.localPosition = new Vector3(0, 2.6f, -.9f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, .73f, .42f);
            light.range = 6f;
            light.intensity = 2.1f;
        }

        private static void Block(Transform parent, string name, Vector3 local, Vector3 size, Material mat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = local;
            g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static Material MaterialFor(string name, Color color)
        {
            Directory.CreateDirectory(Root + "/Materials");
            string path = Root + "/Materials/Refugio_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
#endif
