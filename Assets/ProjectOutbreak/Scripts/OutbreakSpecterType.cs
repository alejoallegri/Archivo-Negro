using UnityEngine;
using UnityEngine.AI;

namespace ProjectOutbreak
{
    /// <summary>Tres tipos de espectro con velocidades y persecuciones distintas.</summary>
    public class OutbreakSpecterType : MonoBehaviour
    {
        public enum SpecterKind { Eco, Custodio, Susurrante }
        public SpecterKind kind;
        public float baseSpeed = 2.4f;

        private NavMeshAgent agent;
        private OutbreakEnemy enemy;
        private Transform player;
        private float phase;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            enemy = GetComponent<OutbreakEnemy>();
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
            phase = Random.value * 6f;
        }

        private void Update()
        {
            if (agent == null || !agent.enabled || enemy == null || enemy.IsDead ||
                OutbreakGame.Instance == null || OutbreakGame.Instance.ended) return;

            switch (kind)
            {
                case SpecterKind.Eco:
                    // Eco: intenta alcanzar al jugador rápidamente, pero cae con pocos impactos.
                    agent.speed = baseSpeed;
                    agent.acceleration = 14f;
                    break;
                case SpecterKind.Custodio:
                    // Custodio: pesado, lento, con más daño y vida.
                    agent.speed = baseSpeed;
                    agent.acceleration = 5f;
                    break;
                case SpecterKind.Susurrante:
                    // Susurrante: avanza a impulsos, como si se materializara de a ratos.
                    float distance = player == null ? 20f : Vector3.Distance(transform.position, player.position);
                    float rhythm = Mathf.Max(0f, Mathf.Sin(Time.time * 2.4f + phase));
                    agent.speed = baseSpeed * (distance < 13f ? .75f + rhythm * .85f : 1f);
                    agent.acceleration = 10f;
                    break;
            }
        }
    }
}
