#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectOutbreak.Editor
{
    /// <summary>
    /// Integra únicamente la representación visual de Easy FPS.
    /// Su controlador antiguo se desactiva: Starter Assets y OutbreakGun siguen mandando.
    /// </summary>
    public static class OutbreakEasyFPSInstaller
    {
        private const string Root = "Assets/ProjectOutbreak";
        private const string Easy = "Assets/Easy FPS";
        private const string Output = Root + "/Scenes/ProjectOutbreakConArma.unity";

        [MenuItem("Project Outbreak/4 - Instalar manos y arma Easy FPS")]
        public static void Install()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Easy + "/Resources/NewGun_semi.prefab");
            if (source == null)
            {
                EditorUtility.DisplayDialog("Falta Easy FPS",
                    "No encontré Assets/Easy FPS/Resources/NewGun_semi.prefab.", "Entendido");
                return;
            }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Root + "/Scenes/ProjectOutbreakCombate.unity" && scene.path != Output)
            {
                EditorUtility.DisplayDialog("Abrí la escena de combate",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreakCombate.unity.", "Entendido");
                return;
            }
            if (!EditorUtility.DisplayDialog("Instalar manos animadas",
                "Se añadirá el rig semiautomático de Easy FPS. Se guarda una escena nueva y el sistema de disparos anterior permanece activo. ¿Continuar?", "Instalar", "Cancelar")) return;

            try
            {
                var gun = UnityEngine.Object.FindFirstObjectByType<OutbreakGun>();
                if (gun == null) throw new Exception("No encontré OutbreakGun en la escena.");
                var camera = gun.transform;
                var old = camera.Find("Pistola detallada");
                if (old != null) old.gameObject.SetActive(false);
                var existing = camera.Find("Easy FPS - Manos y arma");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                instance.name = "Easy FPS - Manos y arma";
                instance.transform.SetParent(camera, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                // La versión original controla movimiento, inventario, sonidos y disparos
                // mediante scripts antiguos. No queremos ejecutar nada de eso.
                foreach (var behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                    behaviour.enabled = false;
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                foreach (var rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
                    UnityEngine.Object.DestroyImmediate(rigidbody);
                foreach (var audio in instance.GetComponentsInChildren<AudioSource>(true))
                    audio.enabled = false;

                var character = FindChild(instance.transform, "FPS_Character_prefab");
                if (character != null)
                    character.localPosition = new Vector3(0, -.18f, .48f);

                // Ajustamos materiales del asset antiguo a URP en el proyecto del usuario.
                ConfigureMaterials();

                var animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null) throw new Exception("No encontré Animator dentro de NewGun_semi.");
                animator.enabled = true;
                animator.applyRootMotion = false;
                var adapter = camera.GetComponent<OutbreakEasyFPSAnimator>();
                if (adapter == null) adapter = camera.gameObject.AddComponent<OutbreakEasyFPSAnimator>();
                adapter.armsAnimator = animator;
                adapter.playerController = gun.GetComponentInParent<CharacterController>();

                // El arma del paquete tiene su propio punto de salida.
                // Conectamos el origen de nuestras balas físicas a esa posición.
                var muzzle = FindChild(instance.transform, "muzzelSpawn");
                if (muzzle != null) gun.muzzle = muzzle;

                // Conservamos nuestro fogonazo y retroceso, ahora aplicados al rig importado.
                var fx = camera.GetComponent<OutbreakWeaponFX>();
                if (fx != null)
                {
                    fx.weaponVisual = instance.transform;
                    if (fx.muzzleFlash != null)
                    {
                        fx.muzzleFlash.transform.SetParent(muzzle != null ? muzzle : instance.transform, false);
                        fx.muzzleFlash.transform.localPosition = Vector3.zero;
                        fx.muzzleFlash.gameObject.SetActive(true);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Output);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Output, true) };
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Arma instalada",
                    "Se creó ProjectOutbreakConArma.unity. El rig tiene manos y su Animator original. Probá Play, WASD, clic izquierdo y R. Si la posición o el tamaño no se ven bien, mandame captura.", "Listo");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("No se completó", exception.Message + "\nEnviame el error de Console.", "Entendido");
            }
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static void ConfigureMaterials()
        {
            var baseDir = Easy + "/Model_Animations_Textures/main character/";
            ConvertMaterial(baseDir + "Materials/Hands_Material.mat", baseDir + "Textures/Hands_Albedo.png", baseDir + "Textures/Hands_Normal.png");
            ConvertMaterial(baseDir + "Materials/Gun_Material.mat", baseDir + "Textures/Weapon_Albedo.png", baseDir + "Textures/Weapon_Normal.png");
            ConvertMaterial(baseDir + "Materials/Sword_Material.mat", baseDir + "Textures/Sword_Albedo.tif", baseDir + "Textures/Sword_Normal.tif");
        }

        private static void ConvertMaterial(string matPath, string albedoPath, string normalPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return;
            material.shader = shader;
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            if (albedo != null) material.SetTexture("_BaseMap", albedo);
            material.SetColor("_BaseColor", Color.white);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
        }
    }
}
#endif
