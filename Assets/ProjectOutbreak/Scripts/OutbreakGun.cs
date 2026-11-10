using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectOutbreak
{
    /// <summary>
    /// En Archivo Negro dispara un rayo instantáneo de contención.
    /// En las escenas originales conserva el sistema de proyectiles anterior.
    /// </summary>
    public class OutbreakGun : MonoBehaviour
    {
        // Campos de la versión anterior: conservados para no perder referencias serializadas.
        public Transform muzzle;
        public GameObject projectilePrefab;
        public float projectileSpeed = 32f;
        public float fireInterval = .28f;
        public OutbreakWeaponFX weaponFX;

        [Header("Archivo Negro - rayo de contención")]
        public Material spectralBeamMaterial;
        public int beamDamage = 35;
        public float beamRange = 160f;
        public float beamLifetime = .13f;
        public float beamWidth = .075f;

        private float nextShot;

        private void Update()
        {
            if (OutbreakGame.Instance == null || OutbreakGame.Instance.ended) return;
            if (Mouse.current == null || !Mouse.current.leftButton.isPressed || Time.time < nextShot) return;
            if (muzzle == null) return;
            nextShot = Time.time + fireInterval;

            if (OutbreakArchiveMission.Instance != null)
                FireContainmentRay();
            else
                FireOriginalProjectile();

            if (weaponFX != null) weaponFX.Shoot();
        }

        private void FireContainmentRay()
        {
            Vector3 origin = transform.position;
            Vector3 direction = transform.forward;
            Vector3 end = origin + direction * beamRange;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, beamRange,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var enemy = hit.collider.GetComponentInParent<OutbreakEnemy>();
                if (enemy != null && !enemy.IsDead)
                {
                    var hitbox = hit.collider.GetComponent<OutbreakHitbox>();
                    float multiplier = hitbox != null ? hitbox.damageMultiplier : 1f;
                    enemy.TakeDamage(Mathf.RoundToInt(beamDamage * multiplier));
                }
                StartCoroutine(ShowImpact(end));
            }

            // Haz principal. Se crea desde el cañón hacia el punto alcanzado por la mira.
            var beam = new GameObject("Rayo espectral - efimero");
            var line = beam.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, muzzle.position);
            line.SetPosition(1, end);
            line.startWidth = beamWidth;
            line.endWidth = beamWidth * .35f;
            line.numCapVertices = 5;
            line.numCornerVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            if (spectralBeamMaterial != null) line.sharedMaterial = spectralBeamMaterial;
            line.startColor = new Color(.65f, 1f, .99f, 1f);
            line.endColor = new Color(.17f, .82f, 1f, .9f);
            StartCoroutine(FadeBeam(line));
        }

        private IEnumerator FadeBeam(LineRenderer line)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(.03f, beamLifetime);
            Color start = new Color(.65f, 1f, .99f, 1f);
            Color end = new Color(.17f, .82f, 1f, .9f);
            while (line != null && elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                line.startWidth = beamWidth * (1f + fade * .3f);
                line.endWidth = beamWidth * .35f * fade;
                start.a = fade;
                end.a = fade * .9f;
                line.startColor = start;
                line.endColor = end;
                yield return null;
            }
            if (line != null) Destroy(line.gameObject);
        }

        private IEnumerator ShowImpact(Vector3 point)
        {
            var flash = new GameObject("Impacto de energía");
            flash.transform.position = point;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(.22f, .91f, 1f);
            light.range = 2.1f;
            light.intensity = 2.8f;
            yield return new WaitForSeconds(.09f);
            if (flash != null) Destroy(flash);
        }

        private void FireOriginalProjectile()
        {
            if (projectilePrefab == null) return;
            Vector3 direction = transform.forward;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit aim,
                150f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                direction = (aim.point - muzzle.position).normalized;

            var projectile = Instantiate(projectilePrefab, muzzle.position,
                Quaternion.LookRotation(direction));
            foreach (var bodyCollider in GetComponentsInParent<Collider>())
            {
                var projectileCollider = projectile.GetComponent<Collider>();
                if (projectileCollider != null)
                    Physics.IgnoreCollision(bodyCollider, projectileCollider);
            }
            var body = projectile.GetComponent<Rigidbody>();
            if (body != null) body.linearVelocity = direction * projectileSpeed;
            Destroy(projectile, 4f);
        }
    }
}
