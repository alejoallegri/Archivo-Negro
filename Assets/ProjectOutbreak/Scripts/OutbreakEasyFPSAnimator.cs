using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectOutbreak
{
    /// <summary>Conecta las animaciones visuales de Easy FPS con Starter Assets.</summary>
    public class OutbreakEasyFPSAnimator : MonoBehaviour
    {
        public Animator armsAnimator;
        public CharacterController playerController;
        public float reloadDuration = 1.8f;
        private float reloadingUntil;
        private static readonly int WalkSpeed = Animator.StringToHash("walkSpeed");
        private static readonly int MaxSpeed = Animator.StringToHash("maxSpeed");
        private static readonly int Reloading = Animator.StringToHash("reloading");

        private void Update()
        {
            if (armsAnimator == null || playerController == null) return;
            Vector3 motion = playerController.velocity;
            motion.y = 0;
            float speed = motion.magnitude;
            armsAnimator.SetFloat(WalkSpeed, speed);
            armsAnimator.SetInteger(MaxSpeed, speed > 5f ? 5 : 0);
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame &&
                OutbreakGame.Instance != null && !OutbreakGame.Instance.ended)
                reloadingUntil = Time.time + reloadDuration;
            armsAnimator.SetBool(Reloading, Time.time < reloadingUntil);
        }
    }
}
