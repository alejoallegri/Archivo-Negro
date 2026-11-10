using UnityEngine;
using UnityEngine.AI;

namespace ProjectOutbreak
{
    /// <summary>
    /// Reserva infectados existentes para dos oleadas, sin modificar el contador global.
    /// Las apariciones eligen puntos transitables lejos de la posición actual del jugador.
    /// </summary>
    public class OutbreakWaveSpawner : MonoBehaviour
    {
        public OutbreakEnemy[] reserveEnemies;
        public Transform[] safeSpawnPoints;
        public int firstWaveKills = 3;
        public int secondWaveKills = 8;
        public int firstWaveCount = 5;
        public float minimumPlayerDistance = 16f;

        private int nextReservedIndex;
        private bool sentFirst;
        private bool sentSecond;
        private Transform player;

        private void Start()
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        private void Update()
        {
            var game = OutbreakGame.Instance;
            if (game == null || game.ended) return;
            int killed = game.totalEnemies - game.remainingEnemies;
            if (!sentFirst && killed >= firstWaveKills)
            {
                sentFirst = true;
                Deploy(Mathf.Min(firstWaveCount, reserveEnemies.Length));
            }
            if (!sentSecond && killed >= secondWaveKills)
            {
                sentSecond = true;
                Deploy(reserveEnemies.Length - nextReservedIndex);
            }
        }

        private void Deploy(int quantity)
        {
            for (int i = 0; i < quantity && nextReservedIndex < reserveEnemies.Length; i++)
            {
                var enemy = reserveEnemies[nextReservedIndex++];
                if (enemy == null) continue;
                Vector3 position = ChooseSpawn(i);
                enemy.transform.position = position;
                enemy.gameObject.SetActive(true);
            }
        }

        private Vector3 ChooseSpawn(int offset)
        {
            Vector3 bestPoint = player != null ? player.position + player.forward * -20f : Vector3.zero;
            float bestDistance = -1f;
            if (safeSpawnPoints == null || safeSpawnPoints.Length == 0) return bestPoint;

            // Varia un poco las ubicaciones y busca el punto más lejano del jugador.
            for (int i = 0; i < safeSpawnPoints.Length; i++)
            {
                var point = safeSpawnPoints[(i + offset) % safeSpawnPoints.Length];
                if (point == null) continue;
                Vector3 candidate = point.position + new Vector3(
                    Mathf.Sin((nextReservedIndex + i) * 2.17f) * 1.6f,
                    0,
                    Mathf.Cos((nextReservedIndex + i) * 1.73f) * 1.6f);
                if (!NavMesh.SamplePosition(candidate, out var hit, 3f, NavMesh.AllAreas)) continue;
                float distance = player == null ? 999f : Vector3.Distance(player.position, hit.position);
                if (distance > bestDistance)
                {
                    bestPoint = hit.position + Vector3.up;
                    bestDistance = distance;
                }
            }
            if (bestDistance < minimumPlayerDistance)
                Debug.LogWarning("No hay un spawn lo bastante distante; usaré el más lejano disponible.");
            return bestPoint;
        }
    }
}
