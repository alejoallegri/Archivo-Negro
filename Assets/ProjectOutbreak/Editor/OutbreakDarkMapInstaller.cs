#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering;

namespace ProjectOutbreak.Editor
{
    /// <summary>
    /// Convierte la escena ampliada en una instalación nocturna, sin descargar assets.
    /// Genera texturas de metal, cemento y contenedores, e instala una linterna física.
    /// </summary>
    public static class OutbreakDarkMapInstaller
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string Output = Root + "/Scenes/ProjectOutbreakOscuro.unity";

        [MenuItem("Project Outbreak/6 - Texturas, oscuridad y linterna")]
        public static void Build()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/ProjectOutbreakMapa.unity" && scene.path != Output)
            {
                EditorUtility.DisplayDialog("Abrí el mapa nuevo",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakMapa.unity.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("Ambientación nocturna",
                "Se texturizará el mapa, se oscurecerá el entorno y se añadirá una linterna con F. Se guardará una escena nueva sin sobrescribir la anterior. ¿Continuar?", "Generar", "Cancelar")) return;

            try
            {
                EditorUtility.DisplayProgressBar("Project Outbreak", "Creando texturas de superficies...", .1f);
                Material floor = BuildMaterial("Suelo_hormigon", new Color(.25f, .27f, .29f), 0);
                Material wall = BuildMaterial("Pared_metal", new Color(.22f, .26f, .29f), 1);
                Material steel = BuildMaterial("Contenedor_azul", new Color(.16f, .30f, .42f), 2);
                Material concrete = BuildMaterial("Bloques_hormigon", new Color(.38f, .39f, .39f), 0);
                Material yellow = BuildMaterial("Cajas_industriales", new Color(.57f, .43f, .17f), 3);
                Material red = BuildMaterial("Equipos_rojos", new Color(.49f, .16f, .14f), 2);

                var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                if (nav == null) throw new Exception("No encontré el objeto de nivel con NavMeshSurface.");
                int textured = 0;
                foreach (var renderer in nav.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string n = renderer.gameObject.name.ToLowerInvariant();
                    if (n.Contains("suelo")) renderer.sharedMaterial = floor;
                    else if (n.Contains("caj") || n.Contains("municiones") || n.Contains("palet")) renderer.sharedMaterial = yellow;
                    else if (n.Contains("generador")) renderer.sharedMaterial = red;
                    else if (n.Contains("contenedor")) renderer.sharedMaterial = steel;
                    else if (n.Contains("bloque") || n.Contains("cobertura") || n.Contains("columna") || n.Contains("consola") || n.Contains("barricada")) renderer.sharedMaterial = concrete;
                    else renderer.sharedMaterial = wall;
                    textured++;
                }

                EditorUtility.DisplayProgressBar("Project Outbreak", "Configurando oscuridad e iluminación...", .65f);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.085f, .105f, .14f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = new Color(.075f, .095f, .12f);
                RenderSettings.fogDensity = .016f;
                foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if (light.type == LightType.Directional)
                    {
                        light.intensity = .08f;
                        light.color = new Color(.38f, .49f, .72f);
                    }
                    else if (light.gameObject.name.StartsWith("Luz zona"))
                    {
                        light.intensity = 1.6f;
                        light.range = 9f;
                    }
                }

                var camera = Camera.main;
                if (camera == null) throw new Exception("No se encontró MainCamera para colocar la linterna.");
                var old = camera.transform.Find("Linterna - luz sin modelo");
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var objectLight = new GameObject("Linterna - luz sin modelo");
                objectLight.transform.SetParent(camera.transform, false);
                objectLight.transform.localPosition = new Vector3(.12f, -.10f, .18f);
                objectLight.transform.localRotation = Quaternion.identity;
                var spotlight = objectLight.AddComponent<Light>();
                spotlight.type = LightType.Spot;
                spotlight.color = new Color(.92f, .95f, 1f);
                spotlight.intensity = 15f;
                spotlight.range = 24f;
                spotlight.spotAngle = 64f;
                spotlight.innerSpotAngle = 34f;
                spotlight.shadows = LightShadows.Soft;
                spotlight.shadowStrength = .8f;
                objectLight.AddComponent<OutbreakFlashlight>();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Output);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Output, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Ambientación instalada",
                    "Se creó ProjectOutbreakOscuro.unity con " + textured + " objetos texturizados, luz ambiental baja, niebla y linterna. Presioná Play y usá F para encender/apagar la luz.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("No se completó", ex.Message + "\nMandame la Console si necesitás ayuda.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static Material BuildMaterial(string name, Color baseColor, int kind)
        {
            Directory.CreateDirectory(Root + "/Textures");
            Directory.CreateDirectory(Root + "/Materials");
            string texturePath = Root + "/Textures/Mapa_" + name + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                var generated = new Texture2D(256, 256, TextureFormat.RGB24, true);
                for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                {
                    float grain = Mathf.PerlinNoise((x + 17f) * .09f, (y + 27f) * .09f);
                    float micro = Mathf.PerlinNoise((x + 311f) * .54f, (y + 121f) * .54f);
                    float shade = .68f + grain * .22f + micro * .12f;
                    if (kind == 1) // paneles metálicos remachados
                    {
                        if (x % 64 < 3 || y % 64 < 3) shade *= .47f;
                        if ((x % 64 == 9 || x % 64 == 55) && (y % 64 == 9 || y % 64 == 55)) shade *= .35f;
                    }
                    else if (kind == 2) // acero corrugado
                    {
                        if (y % 32 < 5) shade *= 1.25f;
                        else if (y % 32 > 27) shade *= .66f;
                    }
                    else if (kind == 3) // cajas industriales con bandas
                    {
                        if (x % 128 > 111 && x % 128 < 124) shade *= .42f;
                        if (y % 128 > 113 && y % 128 < 120) shade *= .60f;
                    }
                    generated.SetPixel(x, y, baseColor * shade);
                }
                generated.Apply();
                File.WriteAllBytes(texturePath, generated.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(generated);
                AssetDatabase.ImportAsset(texturePath);
                var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.wrapMode = TextureWrapMode.Repeat;
                    textureImporter.mipmapEnabled = true;
                    textureImporter.SaveAndReimport();
                }
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }

            string materialPath = Root + "/Materials/Mapa_Textura_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
                material.SetColor("_BaseColor", Color.white);
            }
            else material.mainTexture = texture;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", kind == 0 ? .18f : .48f);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
