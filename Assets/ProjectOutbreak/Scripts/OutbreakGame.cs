using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectOutbreak
{
    /// <summary>
    /// Compatible con las escenas anteriores. Si hay un OutbreakArchiveMission,
    /// reemplaza el objetivo de exterminio por la investigación y contención.
    /// </summary>
    public class OutbreakGame : MonoBehaviour
    {
        public static OutbreakGame Instance { get; private set; }
        public int totalEnemies = 5;
        public int remainingEnemies;
        public float health = 100f;
        public bool ended;
        public bool won;

        private GUIStyle large;
        private GUIStyle small;
        private GUIStyle targetStyle;
        private GUIStyle promptStyle;

        private void Awake()
        {
            Instance = this;
            remainingEnemies = totalEnemies;
        }

        public void Hurt(float damage)
        {
            if (ended) return;
            health = Mathf.Max(0f, health - damage);
            if (health <= 0) End(false);
        }

        public void EnemyKilled()
        {
            if (ended) return;
            remainingEnemies = Mathf.Max(0, remainingEnemies - 1);
            // En el Archivo Negro ganar NO depende de matar a todos los espectros.
            if (remainingEnemies == 0 && OutbreakArchiveMission.Instance == null) End(true);
        }

        public void CompleteArchive()
        {
            if (!ended && OutbreakArchiveMission.Instance != null) End(true);
        }

        private void End(bool victory)
        {
            ended = true;
            won = victory;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (ended && UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            if (large == null)
            {
                large = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
                };
                large.normal.textColor = Color.white;
                small = new GUIStyle(GUI.skin.label) { fontSize = 21, fontStyle = FontStyle.Bold };
                small.normal.textColor = Color.white;
                targetStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
                targetStyle.normal.textColor = Color.white;
                promptStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
                };
                promptStyle.normal.textColor = new Color(.66f, 1f, .96f);
            }

            var archive = OutbreakArchiveMission.Instance;
            if (archive == null)
            {
                GUI.Box(new Rect(12, 12, 270, 84), "");
                GUI.Label(new Rect(24, 20, 260, 30), "VIDA: " + Mathf.CeilToInt(health), small);
                GUI.Label(new Rect(24, 50, 260, 30), "INFECTADOS: " + remainingEnemies, small);
            }
            else
            {
                GUI.Box(new Rect(12, 12, 312, 120), "ARCHIVO NEGRO");
                GUI.Label(new Rect(24, 36, 290, 30), "INTEGRIDAD: " + Mathf.CeilToInt(health), small);
                GUI.Label(new Rect(24, 65, 290, 30), "ESPECTROS: " + remainingEnemies, small);
                GUI.Label(new Rect(24, 94, 290, 30), "EXPEDIENTES: " +
                    archive.CollectedDocuments + "/" + archive.DocumentCount, small);
            }

            if (!ended)
            {
                GUI.Label(new Rect(Screen.width / 2 - 8, Screen.height / 2 - 15, 35, 30), "+", small);
                if (archive == null)
                    GUI.Label(new Rect(Screen.width - 660, Screen.height - 38, 655, 30),
                        "WASD mover | Mouse apuntar | Click disparar | Espacio saltar", small);
                else
                {
                    string objective = archive.CollectedDocuments < archive.DocumentCount
                        ? "OBJETIVO: Recuperá los expedientes del archivo"
                        : "OBJETIVO: Regresá a la terminal central";
                    GUI.Box(new Rect(Screen.width - 520, 12, 505, 60), objective);

                    if (!string.IsNullOrEmpty(archive.LastEvent))
                        GUI.Label(new Rect(Screen.width / 2 - 450, 30, 900, 40), archive.LastEvent, promptStyle);

                    if (!string.IsNullOrEmpty(archive.CurrentPrompt))
                        GUI.Label(new Rect(Screen.width / 2 - 390, Screen.height / 2 + 72,
                            780, 35), archive.CurrentPrompt, promptStyle);

                    GUI.Label(new Rect(18, Screen.height - 39, 920, 30),
                        "WASD moverte  |  E investigar  |  Click contener  |  F linterna", small);
                }
                DrawTargetHealth();
            }
            else
            {
                GUI.Box(new Rect(Screen.width / 2 - 280, Screen.height / 2 - 95, 560, 190), "");
                GUI.Label(new Rect(Screen.width / 2 - 270, Screen.height / 2 - 65, 540, 65),
                    won ? (archive != null ? "¡ARCHIVO CONTENIDO!" : "¡MISIÓN COMPLETADA!")
                        : "FIN DE LA PARTIDA", large);
                GUI.Label(new Rect(Screen.width / 2 - 260, Screen.height / 2 + 15, 520, 45),
                    "Presioná R para reiniciar", large);
            }
        }

        private void DrawTargetHealth()
        {
            Camera view = Camera.main;
            if (view == null) return;
            if (!Physics.Raycast(view.transform.position, view.transform.forward,
                out RaycastHit hit, 120f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore)) return;

            var target = hit.collider.GetComponentInParent<OutbreakEnemy>();
            if (target == null || target.IsDead) return;

            Vector3 top = target.transform.position + Vector3.up * 1.3f;
            if (target.animator != null && target.animator.isHuman)
            {
                Transform head = target.animator.GetBoneTransform(HumanBodyBones.Head);
                if (head != null) top = head.position + Vector3.up * .48f;
            }
            Vector3 point = view.WorldToScreenPoint(top);
            if (point.z <= 0) return;

            float ratio = Mathf.Clamp01((float)target.CurrentHealth / Mathf.Max(1, target.maxHealth));
            float x = Mathf.Clamp(point.x - 92f, 6f, Screen.width - 190f);
            float y = Mathf.Clamp(Screen.height - point.y - 56f, 6f, Screen.height - 55f);
            GUI.Box(new Rect(x, y, 184, 52), "");
            GUI.Label(new Rect(x + 9, y + 2, 170, 24),
                target.name + "  " + target.CurrentHealth + "/" + target.maxHealth, targetStyle);

            Color previous = GUI.color;
            GUI.color = new Color(.18f, .21f, .24f, 1f);
            GUI.DrawTexture(new Rect(x + 10, y + 32, 164, 12), Texture2D.whiteTexture);
            GUI.color = OutbreakArchiveMission.Instance == null
                ? Color.Lerp(new Color(.85f, .15f, .13f), new Color(.22f, .8f, .35f), ratio)
                : Color.Lerp(new Color(.69f, .28f, .9f), new Color(.28f, .91f, 1f), ratio);
            GUI.DrawTexture(new Rect(x + 10, y + 32, 164 * ratio, 12), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
