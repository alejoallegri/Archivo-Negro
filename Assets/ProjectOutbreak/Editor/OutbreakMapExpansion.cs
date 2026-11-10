#if UNITY_EDITOR
using System;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace ProjectOutbreak.Editor
{
    /// <summary>
    /// Amplía la arena existente con sectores, coberturas, iluminación y más infectados.
    /// No borra ni modifica la escena original; guarda una variante.
    /// </summary>
    public static class OutbreakMapExpansion
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string Output = Root + "/Scenes/ProjectOutbreakMapa.unity";

        [MenuItem("Project Outbreak/5 - Ampliar mapa y enemigos")]
        public static void Build()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/ProjectOutbreakConArma.unity" && scene.path != Output)
            {
                EditorUtility.DisplayDialog("Abrí la escena correcta",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakConArma.unity.\nLa herramienta creará una copia nueva.", "Entendido");
                return;
            }

            if (!EditorUtility.DisplayDialog("Ampliar Project Outbreak",
                "Añadir sectores, coberturas, detalles, luces y 12 infectados adicionales. Se guardará una escena nueva, manteniendo la anterior. ¿Continuar?", "Generar", "Cancelar")) return;

            try
            {
                var surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                var game = UnityEngine.Object.FindFirstObjectByType<OutbreakGame>();
                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(FindObjectsSortMode.None);
                if (surface == null || game == null || enemies.Length == 0)
                    throw new Exception("No se encontró NavMeshSurface, OutbreakGame o los enemigos de la escena original.");

                var level = surface.transform;
                Transform previous = level.Find("Expansion - Sectores nuevos");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var map = new GameObject("Expansion - Sectores nuevos").transform;
                map.SetParent(level, false);

                Directory.CreateDirectory(Root + "/Materials");
                Material wall = MaterialFor("Pared_metal", new Color(.24f, .30f, .36f));
                Material container = MaterialFor("Contenedor_azul", new Color(.18f, .31f, .43f));
                Material warning = MaterialFor("Seguridad_amarillo", new Color(.76f, .56f, .19f));
                Material concrete = MaterialFor("Hormigon", new Color(.43f, .44f, .43f));
                Material red = MaterialFor("Emergencia_rojo", new Color(.58f, .2f, .17f));

                EditorUtility.DisplayProgressBar("Mapa Project Outbreak", "Construyendo sectores...", .2f);
                // SECTOR DE ALMACÉN: pasillos entre contenedores y cajas.
                Block(map, "Almacen - separador norte", new Vector3(-9, 1.5f, 5), new Vector3(11, 3, .5f), wall);
                Block(map, "Almacen - separador sur", new Vector3(-11, 1.5f, -4), new Vector3(.5f, 3, 9), wall);
                Block(map, "Almacen - contenedor 1", new Vector3(-13, 1.25f, 9), new Vector3(2.6f, 2.5f, 3.5f), container);
                Block(map, "Almacen - contenedor 2", new Vector3(-8, 1.25f, 11), new Vector3(3.5f, 2.5f, 2.3f), container);
                Block(map, "Almacen - cajon 1", new Vector3(-14, .65f, -10), new Vector3(1.5f, 1.3f, 1.5f), warning);
                Block(map, "Almacen - cajon 2", new Vector3(-12, .65f, -9), new Vector3(1.5f, 1.3f, 1.5f), warning);

                // ZONA TÉCNICA: paredes discontinuas para permitir el paso de NavMesh.
                Block(map, "Tecnica - pared 1", new Vector3(10, 1.6f, -1), new Vector3(.5f, 3.2f, 8), wall);
                Block(map, "Tecnica - pared 2", new Vector3(13, 1.6f, 5), new Vector3(8, 3.2f, .5f), wall);
                Block(map, "Tecnica - generador", new Vector3(13, .9f, -7), new Vector3(3.5f, 1.8f, 2.8f), red);
                Block(map, "Tecnica - consola", new Vector3(15, .65f, 0), new Vector3(2.6f, 1.3f, 1.2f), concrete);
                Block(map, "Tecnica - consola 2", new Vector3(14, .65f, 12), new Vector3(2.7f, 1.3f, 1.2f), concrete);

                // NÚCLEO CENTRAL: coberturas bajas y acceso desde varios lados.
                Block(map, "Centro - barricada oeste", new Vector3(-3, .65f, 3), new Vector3(1, 1.3f, 4), concrete);
                Block(map, "Centro - barricada este", new Vector3(4, .65f, -2), new Vector3(1, 1.3f, 4), concrete);
                Block(map, "Centro - palet", new Vector3(0, .45f, 7), new Vector3(3, .9f, 2), warning);
                Block(map, "Centro - contenedor", new Vector3(1, 1, 15), new Vector3(3, 2, 2), container);

                // ZONA SUR: columnas y cobertura de escape.
                for (int i = 0; i < 4; i++)
                    Block(map, "Sur - columna " + (i + 1), new Vector3(-13 + i * 7, 1.9f, -15),
                        new Vector3(.8f, 3.8f, .8f), concrete);
                Block(map, "Sur - cobertura", new Vector3(-6, .65f, -12), new Vector3(3, 1.3f, 1), container);
                Block(map, "Sur - municiones", new Vector3(11, .45f, -14), new Vector3(1.5f, .9f, 1.5f), warning);

                var lightRoot = new GameObject("Luces de emergencia").transform;
                lightRoot.SetParent(map, false);
                for (int i = 0; i < 6; i++)
                {
                    var lamp = new GameObject("Luz zona " + i);
                    lamp.transform.SetParent(lightRoot, false);
                    lamp.transform.position = new Vector3(-12 + (i % 3) * 12, 4.1f, -10 + (i / 3) * 20);
                    var l = lamp.AddComponent<Light>();
                    l.type = LightType.Point;
                    l.range = 11;
                    l.intensity = 6;
                    l.color = i % 2 == 0 ? new Color(1f, .67f, .46f) : new Color(.61f, .80f, 1f);
                }

                EditorUtility.DisplayProgressBar("Mapa Project Outbreak", "Calculando rutas NavMesh...", .55f);
                surface.BuildNavMesh();

                // Enemigos extra: reutilizan el rig animado y las hitboxes existentes.
                var original = enemies[0];
                var positions = new[]
                {
                    new Vector3(-15,0,13), new Vector3(-5,0,15), new Vector3(10,0,15),
                    new Vector3(15,0,9), new Vector3(15,0,-12), new Vector3(8,0,-15),
                    new Vector3(-7,0,-16), new Vector3(-15,0,-6), new Vector3(-14,0,2),
                    new Vector3(1,0,3), new Vector3(8,0,8), new Vector3(-3,0,-8)
                };
                int spawned = 0;
                foreach (Vector3 position in positions)
                {
                    if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                        continue;
                    var clone = UnityEngine.Object.Instantiate(original.gameObject);
                    clone.name = "Infectado extra " + (++spawned);
                    clone.transform.position = hit.position + Vector3.up;
                    clone.transform.rotation = Quaternion.identity;
                    var agent = clone.GetComponent<NavMeshAgent>();
                    if (agent != null) agent.speed = 2.5f + (spawned % 4) * .35f;
                    var enemy = clone.GetComponent<OutbreakEnemy>();
                    if (enemy != null) enemy.maxHealth = spawned % 4 == 0 ? 160 : 100;
                }
                if (spawned < 8)
                    Debug.LogWarning("Solo se ubicaron " + spawned + " nuevos enemigos. Revisar el NavMesh.");

                game.totalEnemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(FindObjectsSortMode.None).Length;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Output);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Output, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Mapa ampliado",
                    "Se creó ProjectOutbreakMapa.unity con " + spawned + " enemigos extra (" + game.totalEnemies + " en total), nuevas coberturas, sectores y luces. La barra de vida al apuntar está en el script OutbreakGame actualizado.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error de generación", ex.Message + "\nMandame la Console para corregirlo.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void Block(Transform parent, string name, Vector3 position, Vector3 scale, Material mat)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static Material MaterialFor(string name, Color color)
        {
            string path = Root + "/Materials/Mapa_" + name + ".mat";
            var found = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (found != null) return found;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
#endif
