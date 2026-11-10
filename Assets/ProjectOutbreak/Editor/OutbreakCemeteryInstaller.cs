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
    /// <summary>Convierte el mapa industrial en un cementerio nocturno transitable.</summary>
    public static class OutbreakCemeteryInstaller
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string Output = Root + "/Scenes/ProjectOutbreakCementerio.unity";

        [MenuItem("Project Outbreak/7 - Cementerio nocturno y salto limitado")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/ProjectOutbreakOscuro.unity" && scene.path != Output)
            {
                EditorUtility.DisplayDialog("Abrí la escena oscura",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakOscuro.unity.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("Generar cementerio",
                "Convertir el nivel en un cementerio de noche, limitar el salto y preparar la vida sobre la cabeza al apuntar. Se guardará una escena nueva. ¿Continuar?", "Generar", "Cancelar")) return;

            try
            {
                var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                var game = UnityEngine.Object.FindFirstObjectByType<OutbreakGame>();
                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(FindObjectsSortMode.None);
                if (nav == null || game == null || enemies.Length == 0)
                    throw new Exception("No encontré el nivel, la partida o los infectados.");

                var level = nav.transform;
                var oldCemetery = level.Find("Cementerio nocturno");
                if (oldCemetery != null) UnityEngine.Object.DestroyImmediate(oldCemetery.gameObject);

                // Desactiva coberturas industriales; no elimina recursos ni escenas anteriores.
                foreach (Transform child in level)
                {
                    string n = child.name;
                    if (n == "Expansion - Sectores nuevos" || n.StartsWith("Contenedor") || n == "Cobertura")
                        child.gameObject.SetActive(false);
                }

                var turf = MaterialFor("Cesped_oscuro", new Color(.095f, .16f, .11f));
                var stone = MaterialFor("Lapida_piedra", new Color(.25f, .27f, .29f));
                var darkStone = MaterialFor("Mausoleo_piedra", new Color(.16f, .18f, .22f));
                var dirt = MaterialFor("Camino_tierra", new Color(.23f, .18f, .14f));
                var iron = MaterialFor("Rejas_negras", new Color(.08f, .10f, .12f));

                var floor = level.Find("Suelo");
                if (floor != null)
                {
                    floor.localScale = new Vector3(60, .6f, 60);
                    floor.GetComponent<Renderer>().sharedMaterial = turf;
                }
                ConfigureWall(level, "Pared norte", new Vector3(0, 2.2f, 30), new Vector3(60, 5, .8f), darkStone);
                ConfigureWall(level, "Pared sur", new Vector3(0, 2.2f, -30), new Vector3(60, 5, .8f), darkStone);
                ConfigureWall(level, "Pared este", new Vector3(30, 2.2f, 0), new Vector3(.8f, 5, 60), darkStone);
                ConfigureWall(level, "Pared oeste", new Vector3(-30, 2.2f, 0), new Vector3(.8f, 5, 60), darkStone);

                var cemetery = new GameObject("Cementerio nocturno").transform;
                cemetery.SetParent(level, false);
                EditorUtility.DisplayProgressBar("Project Outbreak", "Colocando lápidas y mausoleos...", .25f);

                // Caminos de tierra: decorativos, sin collider; no cortan el NavMesh.
                Decorative(cemetery, "Sendero principal", new Vector3(0, .018f, 0), new Vector3(4.8f, .04f, 56), dirt);
                Decorative(cemetery, "Sendero transversal", new Vector3(0, .02f, 6), new Vector3(50, .04f, 3.5f), dirt);

                var random = new System.Random(7213);
                int graveCount = 0;
                for (int row = -5; row <= 5; row++)
                for (int col = -5; col <= 5; col++)
                {
                    float x = col * 4.4f + (float)(random.NextDouble() - .5) * .6f;
                    float z = row * 4.5f + (float)(random.NextDouble() - .5) * .7f;
                    if (Mathf.Abs(x) < 4f || Mathf.Abs(z - 6) < 3.5f) continue;
                    if (x > 12 && z > 12) continue; // espacio para mausoleo
                    if (x < -16 && z < -14) continue;
                    var marker = new GameObject("Sepultura " + (++graveCount)).transform;
                    marker.SetParent(cemetery, false);
                    marker.position = new Vector3(x, 0, z);
                    float h = 1.05f + (float)random.NextDouble() * .6f;
                    if (graveCount % 5 == 0)
                    {
                        Block(marker, "Cruz vertical", new Vector3(0, h * .5f, 0), new Vector3(.20f, h, .28f), stone, false);
                        Block(marker, "Cruz horizontal", new Vector3(0, h * .72f, 0), new Vector3(.8f, .2f, .28f), stone, false);
                    }
                    else
                    {
                        Block(marker, "Lapida", new Vector3(0, h * .5f, 0), new Vector3(.95f, h, .25f), stone, false);
                        Block(marker, "Base", new Vector3(0, .15f, .33f), new Vector3(1.35f, .3f, 1.5f), darkStone, false);
                    }
                }

                // Mausoleos bajos pero no escalables con el salto reducido.
                BuildMausoleum(cemetery, "Mausoleo este", new Vector3(19, 0, 18), darkStone, stone);
                BuildMausoleum(cemetery, "Mausoleo oeste", new Vector3(-20, 0, 17), darkStone, stone);

                // Rejas con huecos visuales en el perímetro (las paredes invisibles/stone frenan la salida).
                for (int i = -6; i <= 6; i++)
                {
                    float x = i * 4.6f;
                    Block(cemetery, "Reja norte " + i, new Vector3(x, 1.6f, 28.9f), new Vector3(.14f, 3.2f, .14f), iron);
                    Block(cemetery, "Reja sur " + i, new Vector3(x, 1.6f, -28.9f), new Vector3(.14f, 3.2f, .14f), iron);
                }

                // Noche auténtica: skybox fuera, fondo azul negro y neblina.
                RenderSettings.skybox = null;
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.025f, .035f, .06f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(.025f, .035f, .065f);
                RenderSettings.fogDensity = .029f;
                foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(.012f, .018f, .038f);
                }
                foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (l.type == LightType.Directional)
                    {
                        l.intensity = .07f;
                        l.color = new Color(.35f, .45f, .75f);
                    }
                }
                // Faroles: poca luz, lejos del centro; la linterna sigue siendo útil.
                for (int i = 0; i < 4; i++)
                {
                    var lamp = new GameObject("Farol cementerio " + i);
                    lamp.transform.SetParent(cemetery, false);
                    lamp.transform.position = new Vector3(i % 2 == 0 ? -23 : 23, 3.2f, i < 2 ? -19 : 18);
                    var l = lamp.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 10f;
                    l.intensity = 2.4f;
                    l.color = new Color(.95f, .66f, .36f);
                }

                // Saltar poco y sin sprint-jumps que permitan escalar bloques.
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    var controller = player.GetComponent("FirstPersonController");
                    if (controller != null)
                    {
                        var serialized = new SerializedObject(controller);
                        var jump = serialized.FindProperty("JumpHeight");
                        if (jump != null)
                        {
                            jump.floatValue = .26f;
                            serialized.ApplyModifiedProperties();
                        }
                        else Debug.LogWarning("No encontré el campo JumpHeight. Podés poner Jump Height = 0.26 en el Inspector.");
                    }
                }

                EditorUtility.DisplayProgressBar("Project Outbreak", "Recalculando NavMesh y enemigos...", .8f);
                nav.BuildNavMesh();
                // La vida se dibuja encima del zombi desde OutbreakGame.cs actualizado.
                // Repartimos los infectados por el cementerio y dejamos despejado el inicio.
                Vector3[] spawnZones =
                {
                    new(-23,0,23), new(-15,0,23), new(-7,0,23), new(7,0,23), new(23,0,23),
                    new(-25,0,7), new(25,0,7), new(-25,0,-7), new(25,0,-7),
                    new(-22,0,-23), new(-14,0,-23), new(-7,0,-23), new(7,0,-23),
                    new(14,0,-23), new(23,0,-23), new(-11,0,11), new(12,0,-11)
                };
                for (int i = 0; i < enemies.Length; i++)
                {
                    var position = spawnZones[i % spawnZones.Length];
                    if (NavMesh.SamplePosition(position, out var hit, 6f, NavMesh.AllAreas))
                        enemies[i].transform.position = hit.position + Vector3.up;
                }
                game.totalEnemies = enemies.Length;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Output);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Output, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Cementerio generado",
                    "Se creó ProjectOutbreakCementerio.unity con " + graveCount + " sepulturas, 2 mausoleos y " + enemies.Length + " infectados. Salto reducido y barra al apuntar sobre la cabeza. Probá Play.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("No se completó", ex.Message + "\nMandame la Console si aparece el error.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void BuildMausoleum(Transform parent, string label, Vector3 position, Material wall, Material trim)
        {
            var root = new GameObject(label).transform;
            root.SetParent(parent, false);
            root.position = position;
            Block(root, "Estructura", new Vector3(0, 2, 0), new Vector3(7, 4, 6), wall, false);
            Block(root, "Cornisa", new Vector3(0, 4.2f, 0), new Vector3(8, .55f, 7), trim, false);
            Block(root, "Entrada tallada", new Vector3(0, 1.4f, -3.04f), new Vector3(2.1f, 2.8f, .12f), trim, false);
        }

        private static void ConfigureWall(Transform parent, string name, Vector3 position, Vector3 size, Material mat)
        {
            var t = parent.Find(name);
            if (t == null) return;
            t.position = position;
            t.localScale = size;
            t.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void Decorative(Transform parent, string name, Vector3 position, Vector3 size, Material mat)
        {
            var go = Block(parent, name, position, size, mat);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 size,
            Material mat, bool world = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            if (world) go.transform.position = position;
            else go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static Material MaterialFor(string name, Color color)
        {
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Textures");
            string path = Root + "/Materials/Cementerio_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            string texPath = Root + "/Textures/Cementerio_" + name + ".png";
            var pixels = new Texture2D(256, 256, TextureFormat.RGB24, true);
            bool grassy = name.Contains("Cesped");
            for (int y = 0; y < 256; y++)
            for (int x = 0; x < 256; x++)
            {
                float detail = Mathf.PerlinNoise(x * .11f + 14, y * .11f + 8);
                float grain = Mathf.PerlinNoise(x * .49f + 90, y * .49f + 3);
                float brightness = .57f + detail * .45f + grain * .18f;
                if (grassy && (x + y * 3) % 17 == 0) brightness *= 1.35f;
                pixels.SetPixel(x, y, color * brightness);
            }
            pixels.Apply();
            File.WriteAllBytes(texPath, pixels.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(texPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetTextureScale("_BaseMap", new Vector2(4, 4));
                mat.SetColor("_BaseColor", Color.white);
            }
            else mat.mainTexture = texture;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}
#endif
