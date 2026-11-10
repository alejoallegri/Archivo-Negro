#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectOutbreak.Editor
{
    public static class OutbreakCombatUpgrade
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string ScenePath = Root + "/Scenes/ProjectOutbreakCombate.unity";

        [MenuItem("Project Outbreak/3 - Mejorar arma y hitboxes")]
        public static void Upgrade()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/ProjectOutbreakAnimado.unity" && scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog("Abrí la escena animada",
                    "Abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakAnimado.unity antes de ejecutar la mejora.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("Actualizar combate",
                "Se guardará una escena nueva llamada ProjectOutbreakCombate; las anteriores permanecerán intactas. ¿Continuar?", "Actualizar", "Cancelar")) return;

            try
            {
                EditorUtility.DisplayProgressBar("Project Outbreak", "Creando materiales de pistola...", .1f);
                var metal = MakeGunMaterial("Metal_pavonado", new Color(.20f, .23f, .27f), 11);
                var grip = MakeGunMaterial("Empunadura_polimero", new Color(.11f, .12f, .13f), 37);
                var accent = MakeGunMaterial("Acero_gris", new Color(.48f, .51f, .54f), 71);
                var bullet = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Proyectil.prefab");
                if (bullet == null) throw new Exception("No encontré Prefabs/Proyectil.prefab.");

                var gun = UnityEngine.Object.FindFirstObjectByType<OutbreakGun>();
                if (gun == null) throw new Exception("No encontré el componente OutbreakGun en la escena.");
                var camera = gun.transform;
                EditorUtility.DisplayProgressBar("Project Outbreak", "Modelando la pistola...", .3f);

                var old = camera.Find("Arma simple");
                if (old != null) old.gameObject.SetActive(false); // Conserva el prototipo como referencia.
                var previous = camera.Find("Pistola detallada");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);

                var visual = new GameObject("Pistola detallada").transform;
                visual.SetParent(camera, false);
                visual.localPosition = new Vector3(.32f, -.28f, .58f);
                Block(visual, "Corredera superior", new Vector3(0, .08f, .04f), new Vector3(.19f, .12f, .46f), metal);
                Block(visual, "Armazon", new Vector3(0, -.015f, .08f), new Vector3(.17f, .10f, .35f), accent);
                Block(visual, "Empunadura", new Vector3(0, -.19f, -.065f), new Vector3(.15f, .32f, .12f), grip,
                    Quaternion.Euler(18, 0, 0));
                Block(visual, "Guardamonte", new Vector3(0, -.13f, .15f), new Vector3(.11f, .025f, .15f), metal);
                Block(visual, "Mira trasera", new Vector3(0, .159f, -.12f), new Vector3(.12f, .035f, .028f), accent);
                Block(visual, "Mira delantera", new Vector3(0, .155f, .25f), new Vector3(.06f, .033f, .026f), accent);
                for (int i = 0; i < 6; i++)
                    Block(visual, "Ranura corredera " + i, new Vector3(.099f, .08f, -.14f + i * .026f),
                        new Vector3(.007f, .073f, .009f), accent);
                for (int i = 0; i < 5; i++)
                    Block(visual, "Textura empunadura " + i, new Vector3(.078f, -.10f - i * .045f, -.065f),
                        new Vector3(.008f, .009f, .095f), accent);

                var flashPoint = new GameObject("Fogonazo").transform;
                flashPoint.SetParent(visual, false);
                flashPoint.localPosition = new Vector3(0, .08f, .315f);
                var flash = flashPoint.gameObject.AddComponent<Light>();
                flash.type = LightType.Point;
                flash.color = new Color(1f, .68f, .15f);
                flash.range = 4f;
                flash.intensity = 7f;
                flash.enabled = false;

                var fx = camera.GetComponent<OutbreakWeaponFX>();
                if (fx == null) fx = camera.gameObject.AddComponent<OutbreakWeaponFX>();
                fx.weaponVisual = visual;
                fx.muzzleFlash = flash;
                gun.weaponFX = fx;
                gun.muzzle.SetParent(flashPoint, false);
                gun.muzzle.localPosition = Vector3.zero;
                gun.muzzle.localRotation = Quaternion.identity;

                EditorUtility.DisplayProgressBar("Project Outbreak", "Trazando balas y creando hitboxes...", .65f);
                AddTrail(bullet);
                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(FindObjectsSortMode.None);
                foreach (var enemy in enemies) AddHitboxes(enemy);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Mejoras instaladas",
                    "Se creó ProjectOutbreakCombate.unity: pistola con materiales, retroceso, fogonazo, balas con estela y hitboxes de cabeza/torso/extremidades para " + enemies.Length + " enemigos. Presioná Play.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error de actualización", ex.Message + "\nMandame la Console si vuelve a ocurrir.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void AddTrail(GameObject original)
        {
            string path = AssetDatabase.GetAssetPath(original);
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var trail = prefabRoot.GetComponent<TrailRenderer>();
                if (trail == null) trail = prefabRoot.AddComponent<TrailRenderer>();
                trail.time = .23f;
                trail.minVertexDistance = .035f;
                trail.startWidth = .085f;
                trail.endWidth = .006f;
                trail.startColor = new Color(1f, .94f, .38f, 1f);
                trail.endColor = new Color(1f, .3f, 0, 0);
                var matPath = Root + "/Materials/Estela.mat";
                var trailMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (trailMat == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    trailMat = new Material(shader);
                    AssetDatabase.CreateAsset(trailMat, matPath);
                }
                trail.sharedMaterial = trailMat;
                trail.textureMode = LineTextureMode.Stretch;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        }

        private static void AddHitboxes(OutbreakEnemy enemy)
        {
            var animator = enemy.animator;
            if (animator == null || !animator.isHuman)
            {
                Debug.LogWarning("Sin avatar humanoide para hitboxes: " + enemy.name);
                return;
            }
            AddZone(animator, HumanBodyBones.Head, "Cabeza x3", .22f, 3f);
            AddZone(animator, HumanBodyBones.Chest, "Torso x1", .34f, 1f);
            AddZone(animator, HumanBodyBones.Hips, "Cadera x1", .25f, 1f);
            AddZone(animator, HumanBodyBones.LeftUpperArm, "Brazo izquierdo x0.6", .16f, .6f);
            AddZone(animator, HumanBodyBones.RightUpperArm, "Brazo derecho x0.6", .16f, .6f);
            AddZone(animator, HumanBodyBones.LeftLowerArm, "Antebrazo izquierdo x0.6", .13f, .6f);
            AddZone(animator, HumanBodyBones.RightLowerArm, "Antebrazo derecho x0.6", .13f, .6f);
            AddZone(animator, HumanBodyBones.LeftUpperLeg, "Pierna izquierda x0.6", .19f, .6f);
            AddZone(animator, HumanBodyBones.RightUpperLeg, "Pierna derecha x0.6", .19f, .6f);
            AddZone(animator, HumanBodyBones.LeftLowerLeg, "Pantorrilla izquierda x0.6", .15f, .6f);
            AddZone(animator, HumanBodyBones.RightLowerLeg, "Pantorrilla derecha x0.6", .15f, .6f);
            // La cápsula invisible anterior absorbería todos los impactos y anularía las zonas.
            var oldCollider = enemy.GetComponent<CapsuleCollider>();
            if (oldCollider != null) oldCollider.enabled = false;
            EditorUtility.SetDirty(enemy);
        }

        private static void AddZone(Animator animator, HumanBodyBones bone, string label, float radius, float multiplier)
        {
            Transform parent = animator.GetBoneTransform(bone);
            if (parent == null) return;
            Transform existing = parent.Find("Zona " + label);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var hitbox = new GameObject("Zona " + label);
            hitbox.transform.SetParent(parent, false);
            var collider = hitbox.AddComponent<SphereCollider>();
            collider.radius = radius;
            collider.isTrigger = false;
            var zone = hitbox.AddComponent<OutbreakHitbox>();
            zone.damageMultiplier = multiplier;
        }

        private static void Block(Transform parent, string name, Vector3 localPos, Vector3 size,
            Material mat, Quaternion? rotation = null)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localRotation = rotation ?? Quaternion.identity;
            cube.transform.localScale = size;
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        }

        private static Material MakeGunMaterial(string name, Color tint, int seed)
        {
            Directory.CreateDirectory(Root + "/Materials");
            string matPath = Root + "/Materials/Arma_" + name + ".mat";
            var found = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (found != null) return found;
            var texture = new Texture2D(128, 128, TextureFormat.RGB24, false);
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float grain = Mathf.PerlinNoise((x + seed) * .32f, (y + seed) * .32f) * .18f + .82f;
                texture.SetPixel(x, y, tint * grain);
            }
            texture.Apply();
            string texPath = Root + "/Materials/Arma_" + name + "_Base.png";
            File.WriteAllBytes(texPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", importedTexture);
            else material.mainTexture = importedTexture;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .45f);
            AssetDatabase.CreateAsset(material, matPath);
            return material;
        }
    }
}
#endif
