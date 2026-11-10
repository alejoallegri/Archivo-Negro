using UnityEngine;

namespace ProjectOutbreak
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class OutbreakProjectile : MonoBehaviour
    {
        public int damage = 40;
        private bool hit;

        private void OnCollisionEnter(Collision collision)
        {
            if (hit) return;
            hit = true;
            var enemy = collision.collider.GetComponentInParent<OutbreakEnemy>();
            if (enemy != null)
            {
                var zone = collision.collider.GetComponent<OutbreakHitbox>();
                float multiplier = zone != null ? zone.damageMultiplier : 1f;
                enemy.TakeDamage(Mathf.RoundToInt(damage * multiplier));
            }
            Destroy(gameObject);
        }
    }
}
