using UnityEngine;
using UnityEngine.AI;

namespace ProjectOutbreak
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class OutbreakEnemy : MonoBehaviour
    {
        public int maxHealth = 100;
        public float attackRange = 1.8f;
        public float damagePerSecond = 14f;
        public float deathDelay = 2.2f;
        public Animator animator;
        private NavMeshAgent agent;
        private Transform target;
        private int health;
        private bool dead;
        public int CurrentHealth => Mathf.Max(0, health);
        public bool IsDead => dead;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = maxHealth;
        }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (dead || target == null || OutbreakGame.Instance == null || OutbreakGame.Instance.ended) return;
            float distance = Vector3.Distance(transform.position, target.position);
            if (agent.isOnNavMesh)
            {
                if (distance > attackRange) agent.SetDestination(target.position);
                else agent.ResetPath();
            }
            if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);
            if (distance <= attackRange) OutbreakGame.Instance.Hurt(damagePerSecond * Time.deltaTime);
        }

        public void TakeDamage(int damage)
        {
            if (dead) return;
            health -= damage;
            if (health > 0)
            {
                if (animator != null) animator.SetTrigger("Hit");
                return;
            }
            dead = true;
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            if (animator != null) animator.SetTrigger("Die");
            OutbreakGame.Instance.EnemyKilled();
            Destroy(gameObject, deathDelay);
        }
    }
}
