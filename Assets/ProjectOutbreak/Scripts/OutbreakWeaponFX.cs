using UnityEngine;

namespace ProjectOutbreak
{
    public class OutbreakWeaponFX : MonoBehaviour
    {
        public Transform weaponVisual;
        public Light muzzleFlash;
        public float recoilDistance = .12f;
        public float recoilRecovery = 12f;
        public float swayAmount = .012f;
        private Vector3 basePosition;
        private Quaternion baseRotation;
        private float recoil;
        private float flashUntil;

        private void Start()
        {
            if (weaponVisual != null)
            {
                basePosition = weaponVisual.localPosition;
                baseRotation = weaponVisual.localRotation;
            }
            if (muzzleFlash != null) muzzleFlash.enabled = false;
        }

        public void Shoot()
        {
            recoil = recoilDistance;
            flashUntil = Time.time + .06f;
        }

        private void LateUpdate()
        {
            recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * recoilRecovery * recoilDistance);
            if (weaponVisual != null)
            {
                float walking = Mathf.Sin(Time.time * 10f) * swayAmount;
                weaponVisual.localPosition = basePosition + new Vector3(0, walking, -recoil);
                // Mantener el ángulo elegido en el Inspector y sumar solo el retroceso.
                weaponVisual.localRotation = baseRotation * Quaternion.Euler(recoil * 40f, 0, 0);
            }
            if (muzzleFlash != null) muzzleFlash.enabled = Time.time < flashUntil;
        }
    }
}
