#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectOutbreak.Editor
{
    /// <summary>
    /// Instala el personaje y las animaciones de Mixamo en una copia de la escena funcional.
    /// Los FBX se copian por separado para evitar volver a distribuir 443 MB.
    /// </summary>
    public static class OutbreakAnimationSetup
    {
        private const string Base = "Assets/ProjectOutbreak";
        private const string Models = Base + "/Models";
        private const string ControllerPath = Base + "/Animations/Romero.controller";
        private const string AnimatedScenePath = Base + "/Scenes/ProjectOutbreakAnimado.unity";

        [MenuItem("Project Outbreak/2 - Instalar Romero y animaciones")]
        public static void Install()
        {
            if (!ConfirmScene()) return;
            string modelPath = Models + "/T-Pose.fbx";
            string walkPath = Models + "/Walking.fbx";
            string hitPath = Models + "/Zombie Reaction Hit.fbx";
            string deathPath = Models + "/Dying.fbx";
            foreach (var path in new[] { modelPath, walkPath, hitPath, deathPath })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) continue;
                EditorUtility.DisplayDialog("Falta un archivo", "No encontré: " + path + "\n\nCopiá los 4 FBX a Assets/ProjectOutbreak/Models sin cambiar sus nombres.", "Entendido");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("Preparando Romero", "Configurando el esqueleto humanoide...", .1f);
                ConfigureModel(modelPath, null, false);
                Avatar sourceAvatar = FindAvatar(modelPath);
                if (sourceAvatar == null || !sourceAvatar.isValid || !sourceAvatar.isHuman)
                    throw new Exception("No se pudo generar un Avatar Humanoid válido para T-Pose.fbx. Revisá la pestaña Rig de ese modelo.");

                EditorUtility.DisplayProgressBar("Preparando Romero", "Importando animaciones (puede tardar)...", .3f);
                ConfigureModel(walkPath, sourceAvatar, true);
                ConfigureModel(hitPath, sourceAvatar, false);
                ConfigureModel(deathPath, sourceAvatar, false);

                var walking = FindClip(walkPath);
                var hit = FindClip(hitPath);
                var dying = FindClip(deathPath);
                if (walking == null || hit == null || dying == null)
                    throw new Exception("No encontré clips de animación en alguno de los FBX.");

                Directory.CreateDirectory(Base + "/Animations");
                EditorUtility.DisplayProgressBar("Preparando Romero", "Creando Animator Controller...", .65f);
                var controller = CreateController(walking, hit, dying);
                var character = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (character == null) throw new Exception("No se pudo leer el prefab T-Pose.");

                var enemies = UnityEngine.Object.FindObjectsByType<OutbreakEnemy>(FindObjectsSortMode.None);
                if (enemies.Length == 0)
                    throw new Exception("No hay enemigos en la escena abierta. Abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreak.unity antes de usar esta opción.");

                foreach (var enemy in enemies)
                {
                    // La cápsula conserva su colisionador, IA y lógica de daño.
                    var capsuleRenderer = enemy.GetComponent<Renderer>();
                    if (capsuleRenderer != null) capsuleRenderer.enabled = false;

                    // Evita duplicados si el usuario ejecuta la herramienta dos veces.
                    Transform old = enemy.transform.Find("Modelo Romero");
                    if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(character);
                    instance.name = "Modelo Romero";
                    instance.transform.SetParent(enemy.transform, false);
                    instance.transform.localPosition = Vector3.down;
                    instance.transform.localRotation = Quaternion.identity;

                    // Dimensiona el personaje para que mida ~1,85 metros.
                    float height = MeasureHeight(instance);
                    if (height > .01f)
                        instance.transform.localScale *= 1.85f / height;

                    // Los colliders pertenecen a la cápsula padre, no al FBX.
                    foreach (var extraCollider in instance.GetComponentsInChildren<Collider>(true))
                        UnityEngine.Object.DestroyImmediate(extraCollider);

                    var animator = instance.GetComponent<Animator>();
                    if (animator == null) animator = instance.AddComponent<Animator>();
                    animator.avatar = sourceAvatar;
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false; // NavMeshAgent gobierna el movimiento.
                    enemy.animator = animator;
                    EditorUtility.SetDirty(enemy);
                }

                EditorUtility.DisplayProgressBar("Preparando Romero", "Guardando una escena nueva...", .9f);
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, AnimatedScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(AnimatedScenePath, true) };
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Romero instalado",
                    "Se reemplazó la apariencia de " + enemies.Length + " enemigos. La escena original sigue guardada por separado. Presioná Play para probar las animaciones.", "Listo");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("No se completó la instalación", exception.Message + "\n\nCopiá el error rojo de la Console para que podamos corregirlo.", "Entendido");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        private static bool ConfirmScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != Base + "/Scenes/ProjectOutbreak.unity" &&
                scene.path != AnimatedScenePath)
            {
                EditorUtility.DisplayDialog("Abrí la escena del juego",
                    "Primero abrí Assets/ProjectOutbreak/Scenes/ProjectOutbreak.unity haciendo doble clic.", "Entendido");
                return false;
            }
            return EditorUtility.DisplayDialog("Instalar animaciones",
                "Se creará/actualizará ProjectOutbreakAnimado.unity, conservando la escena base. ¿Continuar?", "Continuar", "Cancelar");
        }

        private static void ConfigureModel(string path, Avatar sharedAvatar, bool loop)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new Exception("El archivo no se reconoce como FBX: " + path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = sharedAvatar == null
                ? ModelImporterAvatarSetup.CreateFromThisModel
                : ModelImporterAvatarSetup.CopyFromOther;
            if (sharedAvatar != null) importer.sourceAvatar = sharedAvatar;
            importer.importAnimation = true;
            if (loop)
            {
                var clips = importer.defaultClipAnimations;
                for (int i = 0; i < clips.Length; i++)
                {
                    clips[i].loopTime = true;
                    clips[i].lockRootRotation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].lockRootPositionXZ = true;
                }
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
        }

        private static Avatar FindAvatar(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Avatar avatar) return avatar;
            return null;
        }

        private static AnimationClip FindClip(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__") && clip.length > .01f)
                    return clip;
            return null;
        }

        private static AnimatorController CreateController(AnimationClip walk, AnimationClip hit, AnimationClip dying)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var states = controller.layers[0].stateMachine;
            var walkState = states.AddState("Caminar");
            walkState.motion = walk;
            states.defaultState = walkState;
            var hitState = states.AddState("Recibir daño");
            hitState.motion = hit;
            var deathState = states.AddState("Morir");
            deathState.motion = dying;

            var hitTransition = states.AddAnyStateTransition(hitState);
            hitTransition.AddCondition(AnimatorConditionMode.If, 0, "Hit");
            hitTransition.hasExitTime = false;
            hitTransition.duration = .1f;
            hitTransition.canTransitionToSelf = false;

            var recover = hitState.AddTransition(walkState);
            recover.hasExitTime = true;
            recover.exitTime = .9f;
            recover.duration = .1f;

            var dieTransition = states.AddAnyStateTransition(deathState);
            dieTransition.AddCondition(AnimatorConditionMode.If, 0, "Die");
            dieTransition.hasExitTime = false;
            dieTransition.duration = .08f;
            dieTransition.canTransitionToSelf = false;
            return controller;
        }

        private static float MeasureHeight(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds.size.y;
        }
    }
}
#endif
