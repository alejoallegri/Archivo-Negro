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
    /// Segunda fase de Archivo Negro: rayo, biblioteca antigua, skins espectrales,
    /// tres clases de enemigos y cuatro marcadores de expedientes.
    /// </summary>
    public static class OutbreakArchiveLibraryInstaller
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string SourceScene = Root + "/Scenes/ArchivoNegro.unity";
        private const string OutputScene = Root + "/Scenes/ArchivoNegroBiblioteca.unity";
        private const string MaterialFolder = Root + "/Materials/ArchivoNegro";
        private const string TextureFolder = Root + "/Textures/ArchivoNegro";
        private const string ImportFolder = Root + "/RealTextures";
        private const string DecorName = "Biblioteca antigua - muebles y archivos";

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> Images = new Dictionary<string, Texture2D>();
        private static string[] imagePaths;

        [MenuItem("Project Outbreak/11 - Biblioteca, espectros y rayos")]
        public static void Install()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != SourceScene && scene.path != OutputScene)
            {
                EditorUtility.DisplayDialog("Abrí Archivo Negro",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ArchivoNegro.unity. " +
                    "Si ya instalaste este paso, abrí ArchivoNegroBiblioteca.unity.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("Mejorar ARCHIVO NEGRO",
                "Añadir biblioteca con muebles detallados, tres tipos de espectros, " +
                "marcadores visibles y rayo de contención. Se guardará una escena nueva. ¿Continuar?",
                "Instalar", "Cancelar")) return;

            try
            {
                var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
                var weapon = UnityEngine.Object.FindFirstObjectByType<OutbreakGun>();
                var mission = UnityEngine.Object.FindFirstObjectByType<OutbreakArchiveMission>();
                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (nav == null || weapon == null || mission == null || enemies.Length < 6)
                    throw new Exception("No encontré la escena Archivo Negro completa. " +
                        "Revisá que el paso 10 funcione antes de instalar el 11.");

                EnsureFolders();
                LoadOptionalTexturePaths();
                Materials.Clear();
                Images.Clear();

                var archive = nav.transform.Find("ARCHIVO NEGRO - Instalacion paranormal");
                if (archive == null) throw new Exception("No encontré el escenario ARCHIVO NEGRO.");
                foreach (Transform t in archive)
                    if (t.name == "Estanteria archivos") t.gameObject.SetActive(false);

                var previous = archive.Find(DecorName);
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var decor = new GameObject(DecorName).transform;
                decor.SetParent(archive, false);

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Preparando materiales reales CC0...", .12f);
                Material wood = MakeMaterial("Madera_estantes", new Color(.65f, .57f, .44f), "fine_grained_wood", new Vector2(2, 1), .16f);
                Material oldPlanks = MakeMaterial("Mesas_viejas", new Color(.76f, .68f, .55f), "weathered_planks", new Vector2(2, 2), .12f);
                Material paintedMetal = MakeMaterial("Archiveros_oxidados", new Color(.72f, .69f, .61f), "rusty_metal_02", Vector2.one, .35f);
                Material wall = MakeMaterial("Pared_piedra", new Color(.66f, .68f, .66f), "cracked_concrete_wall", new Vector2(12, 3), .08f);
                Material tile = MakeMaterial("Baldosas_archivo", new Color(.62f, .65f, .64f), "large_grey_tiles", new Vector2(22, 22), .16f);
                Material bookcloth = MakeMaterial("Tela_tomos", new Color(.72f, .67f, .56f), "book_pattern", Vector2.one, .1f);
                Material paper = MakeMaterial("Papel_viejo", new Color(.72f, .63f, .43f), null, Vector2.one, .08f);
                Material darkTrim = MakeMaterial("Moldura_madera", new Color(.35f, .25f, .20f), "fine_grained_wood", Vector2.one, .17f);
                Material teal = Luminous("Marcador_cian", new Color(.18f, 1f, .82f));

                var floor = nav.transform.Find("Suelo");
                if (floor != null)
                {
                    var renderer = floor.GetComponent<Renderer>();
                    if (renderer != null) renderer.sharedMaterial = tile;
                }
                foreach (var mesh in archive.GetComponentsInChildren<MeshRenderer>(true))
                    if (mesh.name.StartsWith("Pared sector", StringComparison.Ordinal))
                        mesh.sharedMaterial = wall;
                foreach (Transform outer in nav.transform)
                    if (outer.name.StartsWith("Pared ", StringComparison.Ordinal))
                    {
                        var renderer = outer.GetComponent<Renderer>();
                        if (renderer != null) renderer.sharedMaterial = wall;
                    }

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Construyendo biblioteca y archiveros...", .31f);
                BuildLibrary(decor, wood, oldPlanks, paintedMetal, bookcloth, paper, darkTrim);

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Transformando tres tipos de espectros...", .61f);
                BuildEnemyVariants(enemies);
                BuildBeacons(mission, teal);

                EditorUtility.DisplayProgressBar("ARCHIVO NEGRO", "Activando rayo de contención y luces...", .78f);
                weapon.spectralBeamMaterial = Luminous("Rayo_material", new Color(.46f, 1f, 1f));
                weapon.beamDamage = 36;
                weapon.beamWidth = .067f;
                weapon.beamLifetime = .14f;
                if (weapon.weaponFX != null && weapon.weaponFX.muzzleFlash != null)
                    weapon.weaponFX.muzzleFlash.color = new Color(.36f, .95f, 1f);
                AdjustLighting();

                nav.BuildNavMesh();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, OutputScene);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(OutputScene, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Biblioteca y espectros listos",
                    "Se creó ArchivoNegroBiblioteca.unity. El arma dispara un rayo en esta escena, " +
                    "los espectros tienen tres variantes y los documentos están marcados. " +
                    "Las otras escenas mantienen las balas originales.", "Listo");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("No se completó el paso 11",
                    ex.Message + "\nEnviame una captura de Console para revisarlo.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Textures")) AssetDatabase.CreateFolder(Root, "Textures");
            if (!AssetDatabase.IsValidFolder(TextureFolder)) AssetDatabase.CreateFolder(Root + "/Textures", "ArchivoNegro");
            if (!AssetDatabase.IsValidFolder(Root + "/Materials")) AssetDatabase.CreateFolder(Root, "Materials");
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder(Root + "/Materials", "ArchivoNegro");
        }

        private static void LoadOptionalTexturePaths()
        {
            if (!AssetDatabase.IsValidFolder(ImportFolder)) { imagePaths = new string[0]; return; }
            var result = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ImportFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tga") result.Add(path);
            }
            imagePaths = result.ToArray();
        }

        private static Texture2D FindMap(string assetId, string map)
        {
            if (string.IsNullOrEmpty(assetId)) return null;
            string key = assetId + "_" + map;
            if (Images.TryGetValue(key, out var cached)) return cached;
            foreach (string path in imagePaths)
            {
                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool matchesId = name.StartsWith(assetId + "_", StringComparison.Ordinal);
                bool matchesMap = name.Contains("_" + map + "_") || name.EndsWith("_" + map, StringComparison.Ordinal);
                // Book Pattern usa 'col1' en lugar de 'diff'.
                if (assetId == "book_pattern" && map == "diff")
                    matchesMap = name.Contains("_col1_") || name.EndsWith("_col1", StringComparison.Ordinal);
                if (!matchesId || !matchesMap) continue;

                if (map == "nor_gl")
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.SaveAndReimport();
                    }
                }
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Images[key] = texture;
                return texture;
            }
            return null;
        }

        private static Material MakeMaterial(string name, Color tint, string assetId, Vector2 tiling,
            float smoothness)
        {
            if (Materials.TryGetValue(name, out var cached)) return cached;
            string path = MaterialFolder + "/BIB_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            Texture2D baseMap = FindMap(assetId, "diff");
            if (baseMap == null) baseMap = GeneratedTexture(name, tint);
            bool originalTexture = assetId != null && FindMap(assetId, "diff") != null;
            string slot = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            mat.SetTexture(slot, baseMap);
            mat.SetTextureScale(slot, tiling);
            Color color = originalTexture ? tint : Color.white;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;

            Texture2D normal = FindMap(assetId, "nor_gl");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            else mat.DisableKeyword("_NORMALMAP");

            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", name.Contains("Archiveros") ? .38f : 0f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            Materials[name] = mat;
            return mat;
        }

        private static Texture2D GeneratedTexture(string name, Color baseColor)
        {
            string path = TextureFolder + "/BIB_" + name + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int res = 256;
            var img = new Texture2D(res, res, TextureFormat.RGB24, true);
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float grain = Mathf.PerlinNoise((x + 37f) * .057f, (y + 43f) * .057f);
                float micro = Mathf.PerlinNoise((x + 137f) * .39f, (y + 7f) * .39f);
                float light = .58f + grain * .37f + micro * .19f;
                if (name.Contains("Papel") && (x + y * 3) % 97 < 2) light *= .72f;
                if (name.Contains("Estantes") && x % 31 < 2) light *= .8f;
                img.SetPixel(x, y, baseColor * light);
            }
            img.Apply();
            File.WriteAllBytes(path, img.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(img);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Luminous(string name, Color color)
        {
            string path = MaterialFolder + "/BIB_" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 local,
            Vector3 size, Material material, bool collision = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void BuildLibrary(Transform parent, Material wood, Material oldWood,
            Material metal, Material book, Material paper, Material trim)
        {
            var books = new GameObject("Ala biblioteca - colecciones antiguas").transform;
            books.SetParent(parent, false);
            var files = new GameObject("Ala archivo - gabinetes y cajones").transform;
            files.SetParent(parent, false);
            var reading = new GameObject("Sala central - mesas de lectura").transform;
            reading.SetParent(parent, false);

            int index = 0;
            foreach (float x in new[] { -23.5f, 23.5f })
            foreach (float z in new[] { -16f, 0f, 16f })
            {
                var obj = new GameObject("Biblioteca de madera " + ++index).transform;
                obj.SetParent(books, false);
                obj.localPosition = new Vector3(x, 0, z);
                obj.localRotation = Quaternion.Euler(0, x < 0 ? 90 : -90, 0);
                Bookcase(obj, wood, book, paper, trim, index);
            }

            index = 0;
            foreach (float x in new[] { -15.5f, 15.5f })
            foreach (float z in new[] { -16f, 0f, 16f })
            {
                var obj = new GameObject("Archivero metalico " + ++index).transform;
                obj.SetParent(files, false);
                obj.localPosition = new Vector3(x, 0, z);
                obj.localRotation = Quaternion.Euler(0, x < 0 ? 90 : -90, 0);
                DrawerCabinet(obj, metal, paper, trim);
            }

            // Mesas y sillas de lectura. El eje central queda libre para caminar.
            foreach (float x in new[] { -4f, 4f })
            foreach (float z in new[] { -10f, 5f, 18f })
            {
                var desk = new GameObject("Mesa de investigacion").transform;
                desk.SetParent(reading, false);
                desk.localPosition = new Vector3(x, 0, z);
                ReadingDesk(desk, oldWood, trim, paper, book);
            }

            // Cajas y expedientes sueltos, dejando pasillos entre las alas.
            int pile = 0;
            foreach (float x in new[] { -27.0f, 27.0f })
            foreach (float z in new[] { -22f, -7f, 9f, 24f })
            {
                var stack = new GameObject("Pila de cajas " + ++pile).transform;
                stack.SetParent(parent, false);
                stack.localPosition = new Vector3(x, 0, z);
                int layers = 2 + pile % 3;
                for (int i = 0; i < layers; i++)
                {
                    float shift = i % 2 == 0 ? -.12f : .15f;
                    Cube(stack, "Caja de expediente", new Vector3(shift, .23f + i * .46f, 0),
                        new Vector3(.78f, .44f, .86f), oldWood, true);
                    Cube(stack, "Etiqueta de papel", new Vector3(shift, .23f + i * .46f, -.44f),
                        new Vector3(.42f, .18f, .015f), paper);
                }
            }

            // Molduras y arcos dividen visualmente los sectores de lectura.
            foreach (float z in new[] { -18f, 0f, 17f })
            {
                var arch = new GameObject("Arco antiguo del pasillo").transform;
                arch.SetParent(parent, false);
                arch.localPosition = new Vector3(0, 0, z);
                Cube(arch, "Pilar izquierdo", new Vector3(-6.4f, 2.2f, 0),
                    new Vector3(.58f, 4.4f, .75f), trim, true);
                Cube(arch, "Pilar derecho", new Vector3(6.4f, 2.2f, 0),
                    new Vector3(.58f, 4.4f, .75f), trim, true);
                Cube(arch, "Dintel de madera", new Vector3(0, 4.5f, 0),
                    new Vector3(13.4f, .45f, .85f), trim);
            }
        }

        private static void Bookcase(Transform parent, Material wood, Material books,
            Material paper, Material trim, int seed)
        {
            Cube(parent, "Costado izquierdo tallado", new Vector3(-1.82f, 1.85f, 0),
                new Vector3(.22f, 3.7f, .80f), wood, true);
            Cube(parent, "Costado derecho tallado", new Vector3(1.82f, 1.85f, 0),
                new Vector3(.22f, 3.7f, .80f), wood, true);
            Cube(parent, "Fondo del estante", new Vector3(0, 1.86f, .35f),
                new Vector3(3.46f, 3.65f, .10f), wood, true);
            Cube(parent, "Cornisa superior", new Vector3(0, 3.8f, 0),
                new Vector3(4.0f, .25f, 1.03f), trim);
            Cube(parent, "Zocalo inferior", new Vector3(0, .16f, -.02f),
                new Vector3(4f, .28f, .91f), trim);

            var random = new System.Random(1000 + seed * 47);
            for (int shelf = 0; shelf < 5; shelf++)
            {
                float baseY = .48f + shelf * .68f;
                Cube(parent, "Baldas de madera " + shelf, new Vector3(0, baseY, 0),
                    new Vector3(3.48f, .11f, .82f), wood);

                float xpos = -1.58f;
                int count = 0;
                while (xpos < 1.48f && count++ < 28)
                {
                    float width = .14f + (float)random.NextDouble() * .11f;
                    float height = .40f + (float)random.NextDouble() * .13f;
                    float offset = (float)(random.NextDouble() - .5) * .10f;
                    var colorMaterial = count % 6 == 0 ? paper : count % 4 == 0 ? trim : books;
                    var volume = Cube(parent, "Libro o expediente " + count,
                        new Vector3(xpos + width * .5f, baseY + height * .5f + .055f, -.13f + offset),
                        new Vector3(width - .008f, height, .47f), colorMaterial);
                    volume.transform.localRotation = Quaternion.Euler(0, 0,
                        (float)(random.NextDouble() - .5) * 4f);
                    if (count % 3 == 0)
                        Cube(volume.transform, "Lomo con etiqueta",
                            new Vector3(0, -.12f, -.51f), new Vector3(.68f, .19f, .035f), paper);
                    xpos += width + .02f;
                }
            }
        }

        private static void DrawerCabinet(Transform parent, Material metal,
            Material paper, Material trim)
        {
            Cube(parent, "Gabinete de doce cajones", new Vector3(0, 1.52f, 0),
                new Vector3(3.35f, 3.04f, .94f), metal, true);
            Cube(parent, "Marco exterior", new Vector3(0, 3.12f, 0),
                new Vector3(3.55f, .14f, 1.08f), trim);
            for (int col = 0; col < 3; col++)
            for (int row = 0; row < 4; row++)
            {
                float x = -1.05f + col * 1.05f;
                float y = .42f + row * .69f;
                Cube(parent, "Cajon metalico", new Vector3(x, y, -.49f),
                    new Vector3(.94f, .55f, .08f), metal);
                Cube(parent, "Tirador de acero", new Vector3(x, y - .11f, -.56f),
                    new Vector3(.38f, .08f, .08f), trim);
                Cube(parent, "Etiqueta de inventario", new Vector3(x, y + .105f, -.542f),
                    new Vector3(.46f, .15f, .013f), paper);
            }
        }

        private static void ReadingDesk(Transform parent, Material wood, Material trim,
            Material paper, Material book)
        {
            Cube(parent, "Tablero desgastado", new Vector3(0, .86f, 0),
                new Vector3(2.15f, .16f, 1.18f), wood, true);
            foreach (float x in new[] { -.88f, .88f })
            foreach (float z in new[] { -.43f, .43f })
                Cube(parent, "Pata torneada", new Vector3(x, .42f, z),
                    new Vector3(.15f, .83f, .15f), trim);
            for (int i = 0; i < 4; i++)
                Cube(parent, "Folios apilados", new Vector3(-.47f, 1.0f + i * .024f, -.18f),
                    new Vector3(.65f, .02f, .43f), paper);
            Cube(parent, "Libro abierto", new Vector3(.51f, 1.0f, .10f),
                new Vector3(.52f, .06f, .48f), book);

            var chair = new GameObject("Silla de madera").transform;
            chair.SetParent(parent, false);
            chair.localPosition = new Vector3(0, 0, 1.36f);
            Cube(chair, "Asiento", new Vector3(0, .45f, 0),
                new Vector3(.67f, .12f, .65f), wood, true);
            Cube(chair, "Respaldo", new Vector3(0, .94f, .30f),
                new Vector3(.66f, 1.0f, .10f), trim);
            foreach (float x in new[] { -.25f, .25f })
            foreach (float z in new[] { -.25f, .25f })
                Cube(chair, "Pata silla", new Vector3(x, .23f, z),
                    new Vector3(.075f, .46f, .075f), trim);
        }

        private static Material SpecterSkin(string kind, Color color, string assetId)
        {
            string path = MaterialFolder + "/BIB_Espectro_" + kind + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            Texture2D pattern = FindMap(assetId, "diff");
            if (pattern == null) pattern = GenerateSpectralPattern(kind, color);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", pattern);
            else mat.mainTexture = pattern;
            var normal = FindMap(assetId, "nor_gl");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
            else mat.DisableKeyword("_NORMALMAP");

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
                mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 2.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D GenerateSpectralPattern(string kind, Color color)
        {
            string path = TextureFolder + "/BIB_Espectro_" + kind + ".png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int res = 512;
            var img = new Texture2D(res, res, TextureFormat.RGB24, true);
            float seed = kind == "Eco" ? 12f : kind == "Custodio" ? 42f : 78f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float noise = Mathf.PerlinNoise(x * .024f + seed, y * .024f + seed);
                float lines = Mathf.PerlinNoise(x * .095f + seed * 2, y * .095f + 13f);
                float streak = Mathf.Abs(Mathf.Sin(x * .06f + noise * 10f));
                float level = kind == "Eco" ? noise * .6f + streak * .4f
                    : kind == "Custodio" ? lines * .7f + noise * .3f
                    : noise * .38f + (y % 24 < 4 ? .2f : .45f);
                Color pixels = Color.Lerp(new Color(.12f, .12f, .19f), Color.white,
                    Mathf.Clamp01(level * 1.3f));
                img.SetPixel(x, y, pixels);
            }
            img.Apply();
            File.WriteAllBytes(path, img.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(img);
            AssetDatabase.ImportAsset(path);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void BuildEnemyVariants(OutbreakEnemy[] enemies)
        {
            Color cyan = new Color(.43f, .91f, 1f, .47f);
            Color violet = new Color(.67f, .39f, 1f, .63f);
            Color white = new Color(.78f, 1f, .83f, .38f);
            Material ecoMat = SpecterSkin("Eco", cyan, "cracked_concrete");
            Material custMat = SpecterSkin("Custodio", violet, "marble_rock_01");
            Material whisperMat = SpecterSkin("Susurrante", white, "book_pattern");

            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                OutbreakSpecterType.SpecterKind kind = (OutbreakSpecterType.SpecterKind)(i % 3);
                var config = enemy.GetComponent<OutbreakSpecterType>();
                if (config == null) config = enemy.gameObject.AddComponent<OutbreakSpecterType>();
                config.kind = kind;
                var agent = enemy.GetComponent<NavMeshAgent>();
                Material skin;
                Color tint;
                switch (kind)
                {
                    case OutbreakSpecterType.SpecterKind.Eco:
                        enemy.name = "Eco espectral " + (i + 1).ToString("00");
                        enemy.maxHealth = 60;
                        enemy.damagePerSecond = 7f;
                        config.baseSpeed = 3.2f;
                        skin = ecoMat; tint = cyan;
                        break;
                    case OutbreakSpecterType.SpecterKind.Custodio:
                        enemy.name = "Custodio espectral " + (i + 1).ToString("00");
                        enemy.maxHealth = 180;
                        enemy.damagePerSecond = 12f;
                        config.baseSpeed = 1.7f;
                        skin = custMat; tint = violet;
                        break;
                    default:
                        enemy.name = "Susurrante " + (i + 1).ToString("00");
                        enemy.maxHealth = 90;
                        enemy.damagePerSecond = 9f;
                        config.baseSpeed = 2.45f;
                        skin = whisperMat; tint = white;
                        break;
                }
                if (agent != null) agent.speed = config.baseSpeed;
                var visual = enemy.GetComponent<OutbreakSpecterVisual>();
                if (visual != null)
                {
                    visual.spectralColor = tint;
                    if (visual.ghostRenderers != null)
                        foreach (Renderer mesh in visual.ghostRenderers)
                        {
                            if (mesh == null) continue;
                            var current = mesh.sharedMaterials;
                            for (int j = 0; j < current.Length; j++) current[j] = skin;
                            mesh.sharedMaterials = current;
                        }
                }
            }
        }

        private static void BuildBeacons(OutbreakArchiveMission mission, Material neon)
        {
            if (mission.documents == null) return;
            for (int i = 0; i < mission.documents.Length; i++)
            {
                var doc = mission.documents[i];
                if (doc == null) continue;
                Transform previous = doc.transform.Find("Marcador objetivo brillante");
                if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                var marker = new GameObject("Marcador objetivo brillante").transform;
                marker.SetParent(doc.transform, false);
                var ring = new GameObject("Halo giratorio").transform;
                ring.SetParent(marker, false);
                ring.localPosition = new Vector3(0, 1.95f, 0);

                for (int part = 0; part < 16; part++)
                {
                    float a = part * Mathf.PI * 2f / 16f;
                    var segment = Cube(ring, "Seccion halo", new Vector3(Mathf.Cos(a) * .8f, 0,
                        Mathf.Sin(a) * .8f), new Vector3(.35f, .065f, .08f), neon);
                    segment.transform.localRotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                }
                Cube(marker, "Puntero vertical", new Vector3(0, 2.65f, 0),
                    new Vector3(.12f, 1.25f, .12f), neon);
                var lamp = new GameObject("Brillo del objetivo");
                lamp.transform.SetParent(marker, false);
                lamp.transform.localPosition = new Vector3(0, 1.8f, 0);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(.32f, 1f, .77f);
                light.intensity = 1.7f;
                light.range = 5f;
                var tag = doc.GetComponent<OutbreakArchiveBeacon>();
                if (tag == null) tag = doc.gameObject.AddComponent<OutbreakArchiveBeacon>();
                tag.documentNumber = i + 1;
                tag.rotatingHalo = ring;
                tag.beaconLight = light;
            }
        }

        private static void AdjustLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.115f, .116f, .112f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.054f, .05f, .048f);
            RenderSettings.fogDensity = .009f;
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.gameObject.name == "Tubo fluorescente")
                {
                    light.intensity = 1.7f;
                    light.color = new Color(.85f, .79f, .62f);
                    light.range = 12f;
                }
                else if (light.gameObject.name == "Baliza de emergencia") light.intensity = 1.6f;
            }
        }
    }
}
#endif
