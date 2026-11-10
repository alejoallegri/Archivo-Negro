#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace ProjectOutbreak.Editor
{
    /// <summary>
    /// Transforma el cementerio funcional en un archivo paranormal.
    /// No modifica el prefab del jugador, sus manos, el arma ni los modelos FBX.
    /// </summary>
    public static class OutbreakBlackArchiveInstaller
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string Source = Root + "/Scenes/ProjectOutbreakLuna.unity";
        private const string SourceTextured = Root + "/Scenes/ProjectOutbreakTexturizado.unity";
        private const string Destination = Root + "/Scenes/ArchivoNegro.unity";
        private const string Materials = Root + "/Materials/ArchivoNegro";
        private const string Textures = Root + "/Textures/ArchivoNegro";
        private const string ArchiveRootName = "ARCHIVO NEGRO - Instalacion paranormal";

        [MenuItem("Project Outbreak/10 - Crear ARCHIVO NEGRO paranormal")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Source && scene.path != SourceTextured && scene.path != Destination)
            {
                EditorUtility.DisplayDialog("Escena incorrecta",
                    "Abrí primero ProjectOutbreakLuna.unity (o ProjectOutbreakTexturizado.unity). " +
                    "Después ejecutá Project Outbreak > 10 - Crear ARCHIVO NEGRO paranormal.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("ARCHIVO NEGRO",
                "Transformar la escena en un archivo subterráneo, reemplazar visualmente " +
                "los infectados por espectros y crear una misión de investigación. " +
                "El arma no se modifica. Se guardará otra escena. ¿Continuar?",
                "Crear Archivo Negro", "Cancelar")) return;

            try
            {
                var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                var game = UnityEngine.Object.FindFirstObjectByType<OutbreakGame>();
                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                var avatar = GameObject.FindGameObjectWithTag("Player");
                if (nav == null || game == null || avatar == null || enemies.Length < 8)
                    throw new Exception("No encontré el nivel, jugador, partida o los enemigos. " +
                        "La escena debe ser la del último cementerio funcional.");

                EnsureFolders();
                Transform level = nav.transform;
                foreach (Transform child in level)
                {
                    if (child.name == "Cementerio nocturno" || child.name == "Refugios y luz lunar" ||
                        child.name == "Expansion - Sectores nuevos" || child.name.StartsWith("Contenedor") ||
                        child.name == "Cobertura")
                        child.gameObject.SetActive(false);
                }

                var previous = level.Find(ArchiveRootName);
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var root = new GameObject(ArchiveRootName).transform;
                root.SetParent(level, false);

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Generando materiales y arquitectura...", .15f);
                var floor = MaterialFor("Suelo_baldosas", new Color(.20f, .27f, .30f), 0, new Vector2(27, 27));
                var wall = MaterialFor("Paredes_hormigon", new Color(.22f, .29f, .32f), 1, new Vector2(11, 2));
                var ceiling = MaterialFor("Techo", new Color(.12f, .17f, .20f), 1, new Vector2(18, 18));
                var steel = MaterialFor("Estantes_acero", new Color(.18f, .27f, .30f), 2, Vector2.one);
                var metal = MaterialFor("Paneles_metal", new Color(.20f, .26f, .31f), 2, Vector2.one);
                var rust = MaterialFor("Oxido", new Color(.31f, .20f, .17f), 3, Vector2.one);
                var screen = MaterialFor("Pantalla_cian", new Color(.16f, .89f, .93f), 4, Vector2.one, true);
                var alert = MaterialFor("Alarma_roja", new Color(.96f, .20f, .22f), 4, Vector2.one, true);
                var bulb = MaterialFor("Tubos_luz", new Color(.71f, .94f, 1f), 4, Vector2.one, true);

                Transform originalFloor = level.Find("Suelo");
                if (originalFloor != null) originalFloor.GetComponent<Renderer>().sharedMaterial = floor;
                foreach (Transform child in level)
                    if (child.name.StartsWith("Pared "))
                    {
                        var renderer = child.GetComponent<Renderer>();
                        if (renderer != null) renderer.sharedMaterial = wall;
                    }

                BuildArchitecture(root, wall, steel, metal, rust, ceiling, screen, alert);
                BuildEvidence(root, metal, screen);

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Iluminando los sectores...", .42f);
                BuildLighting(root, bulb, alert, screen);

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Preparando rutas de navegación...", .58f);
                nav.BuildNavMesh();
                avatar.transform.position = new Vector3(0, 1.05f, -25f);
                avatar.transform.rotation = Quaternion.identity;

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Convirtiendo enemigos en espectros...", .76f);
                BuildSpecters(enemies);

                // La antigua lógica de oleadas por asesinatos queda deshabilitada:
                // ahora aparecen espectros cuando recuperamos expedientes.
                foreach (var oldWave in UnityEngine.Object.FindObjectsByType<OutbreakWaveSpawner>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                    oldWave.enabled = false;

                BuildMission(root, enemies);
                game.totalEnemies = enemies.Length;
                game.ended = false;
                game.won = false;
                game.health = 100f;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Destination);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Destination, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("ARCHIVO NEGRO creado",
                    "Se creó Assets/ProjectOutbreak/Scenes/ArchivoNegro.unity. " +
                    "Recuperá cuatro expedientes con E, defendete de espectros y volvé a la terminal. " +
                    "La pistola mantiene el mismo modelo y funciona como arma de contención.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("No se pudo crear ARCHIVO NEGRO",
                    ex.Message + "\n\nEnviame una captura de Console si aparece un error.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Materials"))
                AssetDatabase.CreateFolder(Root, "Materials");
            if (!AssetDatabase.IsValidFolder(Root + "/Textures"))
                AssetDatabase.CreateFolder(Root, "Textures");
            if (!AssetDatabase.IsValidFolder(Materials))
                AssetDatabase.CreateFolder(Root + "/Materials", "ArchivoNegro");
            if (!AssetDatabase.IsValidFolder(Textures))
                AssetDatabase.CreateFolder(Root + "/Textures", "ArchivoNegro");
        }

        private static Material MaterialFor(string label, Color baseColor, int kind,
            Vector2 tiling, bool emissive = false)
        {
            string texturePath = Textures + "/AN_" + label + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                const int res = 256;
                var source = new Texture2D(res, res, TextureFormat.RGB24, true);
                for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    float large = Mathf.PerlinNoise((x + 23f) * .055f, (y + 59f) * .055f);
                    float detail = Mathf.PerlinNoise((x + 128f) * .41f, (y + 87f) * .41f);
                    float amount = .73f + large * .30f + detail * .13f;
                    if (kind == 0 && (x % 64 < 3 || y % 64 < 3)) amount *= .37f; // juntas de baldosa
                    if (kind == 1 && (y % 100 < 3 || x % 128 < 2)) amount *= .64f; // grietas
                    if (kind == 2 && y % 32 < 4) amount *= .58f; // metal industrial
                    if (kind == 3 && large > .55f) amount *= .72f; // óxido
                    if (kind == 4) amount = 1f; // luminoso
                    source.SetPixel(x, y, baseColor * amount);
                }
                source.Apply();
                File.WriteAllBytes(texturePath, source.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(source);
                AssetDatabase.ImportAsset(texturePath);
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            }

            string matPath = Materials + "/AN_" + label + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, matPath);
            }
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
                material.SetColor("_BaseColor", Color.white);
            }
            else
            {
                material.mainTexture = texture;
                material.mainTextureScale = tiling;
                material.color = Color.white;
            }
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", kind == 2 ? .55f : 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", kind == 4 ? .65f : .18f);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", baseColor * 2.1f);
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject Block(Transform parent, string name, Vector3 position,
            Vector3 scale, Material material, bool physical = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!physical) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void BuildArchitecture(Transform root, Material wall, Material shelves,
            Material metal, Material rust, Material ceiling, Material screen, Material alarm)
        {
            // Techo visual sin collider: no crea islas de NavMesh encima.
            Block(root, "Techo completo", new Vector3(0, 5.55f, 0),
                new Vector3(59.5f, .38f, 59.5f), ceiling, false);

            // Pasillo central con interrupciones para que todos los sectores se conecten.
            foreach (float x in new[] { -8f, 8f })
            {
                Block(root, "Pared sector - sur", new Vector3(x, 2.4f, -18.5f),
                    new Vector3(.45f, 4.8f, 9f), wall);
                Block(root, "Pared sector - centro", new Vector3(x, 2.4f, -3f),
                    new Vector3(.45f, 4.8f, 7f), wall);
                Block(root, "Pared sector - norte", new Vector3(x, 2.4f, 13.5f),
                    new Vector3(.45f, 4.8f, 8f), wall);
            }

            // Estanterías de archivo. Pasillos anchos para caminar y esquivar espectros.
            foreach (float x in new[] { -23.5f, -15.5f, 15.5f, 23.5f })
            foreach (float z in new[] { -15f, 0f, 15f })
            {
                var rack = new GameObject("Estanteria archivos").transform;
                rack.SetParent(root, false);
                rack.localPosition = new Vector3(x, 0, z);
                Block(rack, "Estante cuerpo", new Vector3(0, 1.55f, 0),
                    new Vector3(1.25f, 3.1f, 5.6f), shelves);
                for (int i = 0; i < 4; i++)
                    Block(rack, "Fila carpetas " + i, new Vector3(.66f, .65f + i * .70f, 0),
                        new Vector3(.06f, .19f, 5.3f), i % 2 == 0 ? rust : metal, false);
            }

            // Recepción y puestos de control (con separaciones transitables).
            Block(root, "Recepcion - mostrador", new Vector3(-3.6f, .67f, -23.8f),
                new Vector3(4.1f, 1.35f, 1.3f), metal);
            Block(root, "Recepcion - monitor", new Vector3(-3.6f, 1.75f, -23.7f),
                new Vector3(1.6f, 1.05f, .2f), screen, false);
            Block(root, "Control acceso - sur", new Vector3(3.6f, 1.15f, -21f),
                new Vector3(1.7f, 2.3f, 2.2f), metal);

            // Marcas de alarma y ventanas selladas, solo decorativas.
            foreach (float z in new[] { -20f, 1f, 22f })
            {
                Block(root, "Señal emergencia oeste", new Vector3(-29.5f, 3.1f, z),
                    new Vector3(.11f, .33f, 1.5f), alarm, false);
                Block(root, "Señal emergencia este", new Vector3(29.5f, 3.1f, z),
                    new Vector3(.11f, .33f, 1.5f), alarm, false);
            }

            // Terminal de contención frente al muro norte.
            var terminal = new GameObject("Terminal principal - contencion").transform;
            terminal.SetParent(root, false);
            terminal.localPosition = new Vector3(0, 0, 24.7f);
            Block(terminal, "Base terminal", new Vector3(0, 1.2f, 0),
                new Vector3(3.4f, 2.4f, 1.5f), metal);
            Block(terminal, "Pantalla terminal", new Vector3(0, 2.0f, -.79f),
                new Vector3(2.65f, 1.25f, .11f), screen, false);
            Block(terminal, "Luz de alerta", new Vector3(0, 2.8f, -.8f),
                new Vector3(2.8f, .13f, .13f), alarm, false);

            // Las paredes exteriores existentes siguen bloqueando el límite del mapa.
        }

        private static void BuildEvidence(Transform root, Material stand, Material highlight)
        {
            Vector3[] points =
            {
                new(-24.6f, 0, -22.3f), new(22f, 0, -5f),
                new(-22f, 0, 21f), new(22f, 0, 22f)
            };
            string[] names =
            {
                "Registro de ingreso 01", "Grabación del incidente 02",
                "Informe de laboratorio 03", "Orden de contención 04"
            };
            for (int i = 0; i < points.Length; i++)
            {
                var evidence = new GameObject("Expediente " + (i + 1)).transform;
                evidence.SetParent(root, false);
                evidence.localPosition = points[i];
                Block(evidence, "Mesa de archivo", new Vector3(0, .42f, 0),
                    new Vector3(1.45f, .84f, 1.1f), stand);
                Block(evidence, "Expediente brillante", new Vector3(0, .96f, 0),
                    new Vector3(.86f, .12f, .59f), highlight, false);
                var marker = evidence.gameObject.AddComponent<OutbreakArchiveEvidence>();
                marker.documentTitle = names[i];
            }
        }

        private static void BuildLighting(Transform root, Material whiteLamp,
            Material warningLamp, Material terminalLamp)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.095f, .14f, .18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(.018f, .041f, .051f);
            RenderSettings.fogDensity = .011f;

            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional) light.intensity = .025f;
                if (light.type == LightType.Spot && light.name == "Linterna - luz sin modelo")
                {
                    light.intensity = 16f;
                    light.range = 27f;
                }
            }
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.007f, .015f, .023f);
                camera.allowHDR = true;
            }

            var lighting = new GameObject("Iluminacion fluorescente y alarmas").transform;
            lighting.SetParent(root, false);
            foreach (float x in new[] { -21f, -4f, 4f, 21f })
            foreach (float z in new[] { -16f, 0f, 17f })
            {
                var l = new GameObject("Tubo fluorescente").transform;
                l.SetParent(lighting, false);
                l.localPosition = new Vector3(x, 5.05f, z);
                Block(l, "Difusor de luz", Vector3.zero,
                    new Vector3(1.6f, .10f, .26f), whiteLamp, false);
                var point = l.gameObject.AddComponent<Light>();
                point.type = LightType.Point;
                point.range = 11.5f;
                point.intensity = 3.2f;
                point.color = new Color(.60f, .83f, 1f);
                point.shadows = LightShadows.None;
            }
            foreach (float x in new[] { -6f, 6f })
            foreach (float z in new[] { -10f, 9f })
            {
                var l = new GameObject("Baliza de emergencia").transform;
                l.SetParent(lighting, false);
                l.localPosition = new Vector3(x, 4.5f, z);
                Block(l, "Panel alarma", Vector3.zero,
                    new Vector3(.65f, .13f, .65f), warningLamp, false);
                var light = l.gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, .14f, .19f);
                light.range = 7f;
                light.intensity = 2.4f;
                light.shadows = LightShadows.None;
            }
            var terminal = new GameObject("Luz terminal central").transform;
            terminal.SetParent(lighting, false);
            terminal.localPosition = new Vector3(0, 2.6f, 23.8f);
            var terminalLight = terminal.gameObject.AddComponent<Light>();
            terminalLight.type = LightType.Point;
            terminalLight.color = new Color(.24f, 1f, .90f);
            terminalLight.intensity = 3.2f;
            terminalLight.range = 7f;
        }

        private static Material SpecterMaterial(string name, Color color)
        {
            string path = Materials + "/AN_Espectro_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            // Blend alfa de URP: cuerpo transparente y emisión para distinguir siluetas.
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 2f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildSpecters(OutbreakEnemy[] enemies)
        {
            var blue = new Color(.28f, .79f, 1f, .47f);
            var violet = new Color(.62f, .32f, 1f, .43f);
            Material blueMaterial = SpecterMaterial("Azul", blue);
            Material purpleMaterial = SpecterMaterial("Violeta", violet);

            Vector3[] initial =
            {
                new(-25,0,12), new(25,0,13), new(-18,0,21), new(18,0,19),
                new(-24,0,-3), new(24,0,-4)
            };
            for (int i = 0; i < enemies.Length; i++)
            {
                var ghost = enemies[i];
                ghost.gameObject.SetActive(i < initial.Length);
                ghost.name = "Espectro " + (i + 1).ToString("00");
                ghost.maxHealth = i % 4 == 0 ? 130 : 85;
                ghost.damagePerSecond = i % 4 == 0 ? 11f : 8f;
                ghost.attackRange = 1.7f;

                var agent = ghost.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.speed = 2.0f + (i % 4) * .17f;
                    agent.stoppingDistance = 1.6f;
                }

                if (i < initial.Length && NavMesh.SamplePosition(initial[i],
                    out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    Vector3 pos = hit.position + Vector3.up;
                    if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(pos);
                    else ghost.transform.position = pos;
                }

                Animator modelAnimator = ghost.animator;
                if (modelAnimator == null) modelAnimator = ghost.GetComponentInChildren<Animator>(true);
                if (modelAnimator == null) continue;
                var meshes = modelAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var all = new List<Renderer>();
                Material ghostMat = i % 3 == 0 ? purpleMaterial : blueMaterial;
                foreach (var mesh in meshes)
                {
                    int num = Mathf.Max(1, mesh.sharedMaterials.Length);
                    var shared = new Material[num];
                    for (int k = 0; k < num; k++) shared[k] = ghostMat;
                    mesh.sharedMaterials = shared;
                    all.Add(mesh);
                }

                var visual = ghost.GetComponent<OutbreakSpecterVisual>();
                if (visual == null) visual = ghost.gameObject.AddComponent<OutbreakSpecterVisual>();
                visual.animatedModel = modelAnimator.transform;
                visual.ghostRenderers = all.ToArray();
                visual.spectralColor = i % 3 == 0 ? violet : blue;
            }
        }

        private static void BuildMission(Transform root, OutbreakEnemy[] enemies)
        {
            var missionObject = new GameObject("Sistema de expedientes y contencion");
            missionObject.transform.SetParent(root, false);
            var mission = missionObject.AddComponent<OutbreakArchiveMission>();
            mission.documents = root.GetComponentsInChildren<OutbreakArchiveEvidence>(true);
            var terminal = root.Find("Terminal principal - contencion");
            mission.containmentTerminal = terminal;

            int initialCount = Mathf.Min(6, enemies.Length);
            mission.reserveSpecters = new OutbreakEnemy[enemies.Length - initialCount];
            Array.Copy(enemies, initialCount, mission.reserveSpecters, 0, mission.reserveSpecters.Length);

            var spawnRoot = new GameObject("Spawns paranormales - seguros").transform;
            spawnRoot.SetParent(root, false);
            Vector3[] locations =
            {
                new(-25,0,25), new(25,0,25), new(-25,0,-7),
                new(25,0,-7), new(-18,0,14), new(18,0,14)
            };
            mission.spawnPoints = new Transform[locations.Length];
            for (int i = 0; i < locations.Length; i++)
            {
                var marker = new GameObject("Aparicion espectral " + (i + 1)).transform;
                marker.SetParent(spawnRoot, false);
                marker.localPosition = locations[i];
                mission.spawnPoints[i] = marker;
            }
        }
    }
}
#endif
