using UnityEngine;

namespace ProjectOutbreak
{
    /// <summary>Marcador de misión visible sobre cada expediente no recuperado.</summary>
    public class OutbreakArchiveBeacon : MonoBehaviour
    {
        public int documentNumber = 1;
        public Transform rotatingHalo;
        public Light beaconLight;

        private GUIStyle markerStyle;
        private OutbreakArchiveEvidence evidence;

        private void Awake() { evidence = GetComponent<OutbreakArchiveEvidence>(); }

        private void Update()
        {
            if (rotatingHalo != null) rotatingHalo.Rotate(Vector3.up, 35f * Time.deltaTime, Space.Self);
            if (beaconLight != null)
                beaconLight.intensity = 1.75f + Mathf.Sin(Time.time * 3f + documentNumber) * .42f;
        }

        private void OnGUI()
        {
            if (evidence == null || evidence.collected || Camera.main == null) return;
            if (OutbreakGame.Instance != null && OutbreakGame.Instance.ended) return;
            var view = Camera.main;
            Vector3 screen = view.WorldToScreenPoint(transform.position + Vector3.up * 3.2f);
            if (screen.z <= 0f) return;
            float distance = Vector3.Distance(view.transform.position, transform.position);
            if (distance > 55f) return;

            if (markerStyle == null)
            {
                markerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                markerStyle.normal.textColor = new Color(.55f, 1f, .94f);
            }

            float x = Mathf.Clamp(screen.x - 88f, 5f, Screen.width - 181f);
            float y = Mathf.Clamp(Screen.height - screen.y - 22f, 75f, Screen.height - 55f);
            GUI.Box(new Rect(x, y, 176f, 38f), "");
            GUI.Label(new Rect(x + 4f, y + 5f, 168f, 27f),
                "ARCHIVO " + documentNumber + "  |  " + Mathf.CeilToInt(distance) + " m", markerStyle);
        }
    }
}
