using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectOutbreak
{
    /// <summary>Linterna sin modelo: solamente un Spot Light unido a la cámara.</summary>
    [RequireComponent(typeof(Light))]
    public class OutbreakFlashlight : MonoBehaviour
    {
        public Key toggleKey = Key.F;
        public bool initiallyOn = true;
        private Light spot;

        private void Awake()
        {
            spot = GetComponent<Light>();
            spot.enabled = initiallyOn;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current[toggleKey].wasPressedThisFrame)
                spot.enabled = !spot.enabled;
        }
    }
}
