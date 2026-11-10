#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectOutbreak.Editor
{
    /// <summary>
    /// Aplica los seis materiales CC0 elegidos para el cementerio existente.
    /// No modifica scripts de juego, enemigos, modelos ni prefabs externos.
    /// </summary>
    public static class OutbreakRealismInstaller
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string InputScene = Root + "/Scenes/ProjectOutbreakLuna.unity";
        private const string OutputScene = Root + "/Scenes/ProjectOutbreakTexturizado.unity";
        private const string SourceFolder = Root + "/RealTextures";
        private const string MaterialFolder = Root + "/Materials/Realismo";

        private static readonly string[] AssetIds =
        {
            "sparse_grass", "grass_path_2", "mossy_sandstone",
            "mossy_stone_wall", "rusty_metal_02", "weathered_planks"
        };

        private static readonly Dictionary<string, TextureSet> Textures = new Dictionary<string, TextureSet>();
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        private sealed class TextureSet
        {
            public Texture2D diffuse;
            public Texture2D normal;
            public Texture2D roughness;
            public Texture2D packedSmoothness;
        }

        [MenuItem("Project Outbreak/9 - Texturas PBR e iluminacion")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != InputScene && scene.path != OutputScene)
            {
                EditorUtility.DisplayDialog("Abrí el cementerio actual",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakLuna.unity. " +
                    "Si ya habías aplicado las texturas, abrí ProjectOutbreakTexturizado.unity.", "Entendido");
                return;
            }

            if (!AssetDatabase.IsValidFolder(SourceFolder))
            {
                EditorUtility.DisplayDialog("Faltan las texturas",
                    "Creá la carpeta Assets/ProjectOutbreak/RealTextures y descomprimí allí " +
                    "las seis texturas descargadas. Luego volvé a ejecutar el paso 9.", "Entendido");
                return;
            }

            try
            {
                Textures.Clear();
                Materials.Clear();
                LoadTextures();

                var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                if (nav == null || nav.transform.Find("Cementerio nocturno") == null)
                    throw new Exception("No encontré el cementerio de la escena. Abrí ProjectOutbreakLuna.unity.");

                if (!EditorUtility.DisplayDialog("Texturizar el cementerio",
                    "Se aplicarán las seis texturas PBR, normales y rugosidad y se ajustarán las luces. " +
                    "Se guardará una escena nueva: ProjectOutbreakTexturizado.unity. ¿Continuar?",
                    "Instalar", "Cancelar")) return;

                EditorUtility.DisplayProgressBar("Project Outbreak", "Preparando materiales PBR...", .20f);
                int count = ApplyToLevel(nav.transform);

                EditorUtility.DisplayProgressBar("Project Outbreak", "Ajustando luz lunar y faroles...", .65f);
                ConfigureLighting();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, OutputScene);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(OutputScene, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Cementerio texturizado",
                    "¡Listo! Se texturizaron " + count + " superficies del escenario y se mejoraron " +
                    "luna, niebla, faroles y refugios. Se creó ProjectOutbreakTexturizado.unity. " +
                    "Presioná Play y probá la linterna con F.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("No se pudo instalar", ex.Message +
                    "\n\nSi sigue fallando, enviame una captura de Console y de RealTextures.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void LoadTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceFolder });
            var imagePaths = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga")
                    imagePaths.Add(path);
            }

            var missing = new List<string>();
            foreach (string id in AssetIds)
            {
                string diffuse = FindMap(imagePaths, id, "diff");
                if (diffuse == null)
                {
                    missing.Add(id + " (Diffuse)");
                    continue;
                }

                string normal = FindMap(imagePaths, id, "nor_gl");
                string rough = FindMap(imagePaths, id, "rough");

                if (normal != null) ConfigureTextureImport(normal, true, false);
                if (rough != null) ConfigureTextureImport(rough, false, true);

                var set = new TextureSet
                {
                    diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse),
                    normal = normal == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(normal),
                    roughness = rough == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(rough)
                };
                Textures[id] = set;

                if (normal == null) Debug.LogWarning("Project Outbreak: faltó Normal (GL) de " + id + ". Se aplica solo Diffuse.");
                if (rough == null) Debug.LogWarning("Project Outbreak: faltó Rough de " + id + ". Se usa rugosidad fija.");
            }

            if (missing.Count > 0)
                throw new Exception("No encontré estas texturas en " + SourceFolder + ":\n- " +
                    string.Join("\n- ", missing.ToArray()) +
                    "\n\nDescomprimí sus ZIP y asegurate de incluir los JPG/PNG Diffuse.");

            EnsureFolder(Root + "/Materials", "Realismo");
        }

        private static string FindMap(List<string> paths, string id, string map)
        {
            foreach (string path in paths)
            {
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (file.StartsWith(id + "_", StringComparison.Ordinal) &&
                    (file.Contains("_" + map + "_") || file.EndsWith("_" + map, StringComparison.Ordinal)))
                    return path;
            }
            return null;
        }

        private static void ConfigureTextureImport(string path, bool normal, bool linearReadable)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool dirty = false;
            if (normal)
            {
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    dirty = true;
                }
            }
            else if (linearReadable)
            {
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    dirty = true;
                }
                if (importer.sRGBTexture) { importer.sRGBTexture = false; dirty = true; }
                if (!importer.isReadable) { importer.isReadable = true; dirty = true; }
            }
            if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; dirty = true; }
            if (dirty) importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material MaterialFor(string id, string variant, Vector2 tiling, float metallic)
        {
            string key = id + "_" + variant;
            if (Materials.TryGetValue(key, out var cached)) return cached;

            string path = MaterialFolder + "/PO_" + key + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader) mat.shader = shader;

            TextureSet maps = Textures[id];
            string albedo = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            mat.SetTexture(albedo, maps.diffuse);
            mat.SetTextureScale(albedo, tiling);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            else mat.color = Color.white;

            if (maps.normal != null)
            {
                mat.SetTexture("_BumpMap", maps.normal);
                mat.SetFloat("_BumpScale", .90f);
                mat.EnableKeyword("_NORMALMAP");
            }
            else mat.DisableKeyword("_NORMALMAP");

            if (maps.roughness != null && mat.HasProperty("_MetallicGlossMap"))
            {
                if (maps.packedSmoothness == null)
                    maps.packedSmoothness = MakePackedSmoothness(id, maps.roughness, metallic);

                mat.SetTexture("_MetallicGlossMap", maps.packedSmoothness);
                mat.SetFloat("_Metallic", 1f); // El canal R del mapa indica cuánto metal hay.
                mat.SetFloat("_Smoothness", 1f); // El canal A contiene 1 - roughness.
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                if (mat.HasProperty("_SmoothnessTextureChannel"))
                    mat.SetFloat("_SmoothnessTextureChannel", 0f);
            }
            else
            {
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", metallic > .25f ? .26f : .16f);
                mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            Materials[key] = mat;
            return mat;
        }

        private static Texture2D MakePackedSmoothness(string id, Texture2D rough, float metal)
        {
            string path = MaterialFolder + "/PO_" + id + "_MetallicSmoothness.png";
            const int resolution = 512;
            var pixels = new Color32[resolution * resolution];
            byte metallic = (byte)Mathf.RoundToInt(Mathf.Clamp01(metal) * 255f);

            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float u = (x + .5f) / resolution;
                float v = (y + .5f) / resolution;
                float smooth = 1f - rough.GetPixelBilinear(u, v).grayscale;
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(smooth) * 255f);
                pixels[y * resolution + x] = new Color32(metallic, 0, 0, alpha);
            }

            var image = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
            image.SetPixels32(pixels);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static int ApplyToLevel(Transform level)
        {
            int count = 0;
            foreach (var renderer in level.GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = renderer.gameObject.name;
                string materialId = null;
                string variant = "normal";
                Vector2 tile = Vector2.one;
                float metallic = 0;

                if (n == "Suelo")
                {
                    materialId = "sparse_grass";
                    variant = "suelo"; tile = new Vector2(26, 26);
                }
                else if (n == "Sendero principal")
                {
                    materialId = "grass_path_2";
                    variant = "sendero_principal"; tile = new Vector2(2, 17);
                }
                else if (n == "Sendero transversal")
                {
                    materialId = "grass_path_2";
                    variant = "sendero_transversal"; tile = new Vector2(16, 2);
                }
                else if (n.StartsWith("Pared ", StringComparison.Ordinal))
                {
                    materialId = "mossy_stone_wall";
                    variant = "pared_perimetro"; tile = new Vector2(19, 3);
                }
                else if (n == "Lapida" || n.StartsWith("Cruz ", StringComparison.Ordinal))
                {
                    materialId = "mossy_sandstone";
                    variant = "lapida";
                }
                else if (n == "Base" && HasAncestor(renderer.transform, "Sepultura"))
                {
                    materialId = "mossy_sandstone";
                    variant = "base_tumba";
                }
                else if (n == "Estructura" && HasAncestor(renderer.transform, "Mausoleo"))
                {
                    materialId = "mossy_stone_wall";
                    variant = "mausoleo"; tile = new Vector2(3, 2);
                }
                else if ((n == "Cornisa" || n == "Entrada tallada") && HasAncestor(renderer.transform, "Mausoleo"))
                {
                    materialId = "mossy_sandstone";
                    variant = "adorno_mausoleo"; tile = new Vector2(2, 1);
                }
                else if (n.StartsWith("Reja ", StringComparison.Ordinal))
                {
                    materialId = "rusty_metal_02";
                    variant = "reja"; tile = new Vector2(1, 2); metallic = .48f;
                }
                else if (HasAncestor(renderer.transform, "Refugio "))
                {
                    if (n == "Techo")
                    {
                        materialId = "weathered_planks";
                        variant = "techo"; tile = new Vector2(3, 3);
                    }
                    else if (n == "Banco")
                    {
                        materialId = "weathered_planks";
                        variant = "banco"; tile = Vector2.one;
                    }
                    else if (n.StartsWith("Pared", StringComparison.Ordinal) || n.StartsWith("Costado", StringComparison.Ordinal))
                    {
                        materialId = "mossy_stone_wall";
                        variant = "pared_refugio"; tile = new Vector2(3, 2);
                    }
                }

                if (materialId == null) continue;
                renderer.sharedMaterial = MaterialFor(materialId, variant, tile, metallic);
                EditorUtility.SetDirty(renderer);
                count++;
            }
            if (count < 50) throw new Exception("Solo se encontraron " + count +
                " objetos del cementerio. Comprobá que abriste la última escena ProjectOutbreakLuna.unity.");
            return count;
        }

        private static bool HasAncestor(Transform child, string prefix)
        {
            for (Transform t = child.parent; t != null; t = t.parent)
                if (t.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void ConfigureLighting()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.125f, .155f, .205f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(.050f, .067f, .098f);
            RenderSettings.fogDensity = .010f;
            RenderSettings.reflectionIntensity = .65f;

            int directionalCount = 0;
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalCount++;
                    light.color = new Color(.64f, .76f, 1f);
                    light.intensity = .85f;
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = .66f;
                    light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
                }
                else if (light.gameObject.name.StartsWith("Farol cementerio", StringComparison.Ordinal))
                {
                    light.color = new Color(1f, .67f, .37f);
                    light.intensity = 5.0f;
                    light.range = 13f;
                    light.shadows = LightShadows.None;
                    AddBulb(light.transform, true);
                }
                else if (light.gameObject.name == "Luz refugio")
                {
                    light.color = new Color(1f, .72f, .45f);
                    light.intensity = 4.5f;
                    light.range = 10f;
                    light.shadows = LightShadows.None;
                    AddBulb(light.transform, false);
                }
                else if (light.type == LightType.Spot && light.gameObject.name == "Linterna - luz sin modelo")
                {
                    light.range = 29f;
                    light.spotAngle = 63f;
                    light.innerSpotAngle = 35f;
                    light.intensity = 15f;
                }
            }

            if (directionalCount == 0)
            {
                var source = new GameObject("Luz de luna - direccional");
                var moonlight = source.AddComponent<Light>();
                moonlight.type = LightType.Directional;
                moonlight.color = new Color(.64f, .76f, 1f);
                moonlight.intensity = .85f;
                moonlight.shadows = LightShadows.Soft;
                moonlight.shadowStrength = .66f;
                moonlight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            }

            foreach (var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(.014f, .023f, .048f);
                cam.allowHDR = true;
            }
        }

        private static void AddBulb(Transform lightSource, bool withPost)
        {
            string bulbName = "Bombilla luminosa decorativa";
            if (lightSource.Find(bulbName) == null)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = bulbName;
                sphere.transform.SetParent(lightSource, false);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = Vector3.one * .19f;
                UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
                sphere.GetComponent<Renderer>().sharedMaterial = BulbMaterial();
            }
            if (withPost && lightSource.Find("Poste hierro") == null)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = "Poste hierro";
                pole.transform.SetParent(lightSource, false);
                pole.transform.localPosition = new Vector3(0, -1.5f, 0);
                pole.transform.localScale = new Vector3(.075f, 1.5f, .075f);
                UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());
                pole.GetComponent<Renderer>().sharedMaterial = MaterialFor("rusty_metal_02", "poste_luz", Vector2.one, .48f);
            }
        }

        private static Material BulbMaterial()
        {
            string path = MaterialFolder + "/PO_Bombilla_calida.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            Color warm = new Color(1f, .80f, .45f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", warm);
            else mat.color = warm;
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", warm * 3.5f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
