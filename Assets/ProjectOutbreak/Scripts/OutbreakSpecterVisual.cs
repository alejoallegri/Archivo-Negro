using UnityEngine;

namespace ProjectOutbreak
{
    /// <summary>Flotación suave y brillo pulsante sin afectar NavMesh ni hitboxes.</summary>
    public class OutbreakSpecterVisual : MonoBehaviour
    {
        public Transform animatedModel;
        public Renderer[] ghostRenderers;
        public Color spectralColor = new Color(.32f, .77f, 1f, .48f);
        public float floatAmplitude = .13f;
        public float floatFrequency = 1.3f;

        private Vector3 originalLocalPosition;
        private MaterialPropertyBlock properties;
        private OutbreakEnemy enemy;
        private float phase;

        private void Awake()
        {
            enemy = GetComponent<OutbreakEnemy>();
            if (animatedModel != null) originalLocalPosition = animatedModel.localPosition;
            properties = new MaterialPropertyBlock();
            phase = Random.value * Mathf.PI * 2f;
        }

        private void LateUpdate()
        {
            if (animatedModel == null) return;
            float pulse = Mathf.Sin(Time.time * floatFrequency * 2f + phase);
            float bob = Mathf.Sin(Time.time * floatFrequency + phase) * floatAmplitude;
            animatedModel.localPosition = originalLocalPosition + Vector3.up * bob;

            if (ghostRenderers == null) return;
            Color color = spectralColor;
            color.a = Mathf.Clamp01(spectralColor.a + pulse * .09f);
            if (enemy != null && enemy.IsDead) color.a *= .28f;
            foreach (var renderer in ghostRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * (1.8f + .6f * pulse));
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
