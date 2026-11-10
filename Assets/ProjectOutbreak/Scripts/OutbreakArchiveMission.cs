using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ProjectOutbreak
{
    /// <summary>
    /// Objetivo del Archivo Negro: recuperar expedientes y activar contención.
    /// La actividad paranormal aumenta al descubrir evidencia, no por bajas.
    /// </summary>
    public class OutbreakArchiveMission : MonoBehaviour
    {
        public static OutbreakArchiveMission Instance { get; private set; }

        public OutbreakArchiveEvidence[] documents;
        public Transform containmentTerminal;
        public OutbreakEnemy[] reserveSpecters;
        public Transform[] spawnPoints;
        public float interactionDistance = 2.6f;
        public float safeSpawnDistance = 16f;

        public int CollectedDocuments { get; private set; }
        public int DocumentCount => documents == null ? 0 : documents.Length;
        public string CurrentPrompt { get; private set; }
        public string LastEvent { get; private set; }

        private Transform player;
        private int nextReserveIndex;
        private float eventUntil;
        private bool firstWave;
        private bool secondWave;

        private void Awake() { Instance = this; }

        private void Start()
        {
            GameObject avatar = GameObject.FindGameObjectWithTag("Player");
            if (avatar != null) player = avatar.transform;
            if (documents == null) documents = new OutbreakArchiveEvidence[0];
            Announce("Recuperá cuatro expedientes y regresá a la terminal de contención.");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (player == null || OutbreakGame.Instance == null || OutbreakGame.Instance.ended) return;
            CurrentPrompt = string.Empty;

            if (Time.time > eventUntil) LastEvent = string.Empty;

            OutbreakArchiveEvidence nearest = null;
            float minDistance = interactionDistance;
            foreach (var document in documents)
            {
                if (document == null || document.collected) continue;
                float distance = Vector3.Distance(player.position, document.transform.position);
                if (distance < minDistance)
                {
                    nearest = document;
                    minDistance = distance;
                }
            }

            bool e = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (nearest != null)
            {
                CurrentPrompt = "E - Recuperar " + nearest.documentTitle;
                if (e) Collect(nearest);
                return;
            }

            if (containmentTerminal != null &&
                Vector3.Distance(player.position, containmentTerminal.position) < interactionDistance + .3f)
            {
                if (CollectedDocuments == DocumentCount && DocumentCount > 0)
                {
                    CurrentPrompt = "E - Activar contención y cerrar el expediente";
                    if (e)
                    {
                        Announce("ARCHIVO CERRADO. Contención finalizada.");
                        OutbreakGame.Instance.CompleteArchive();
                    }
                }
                else CurrentPrompt = "Terminal bloqueada: recuperá todos los expedientes";
            }
        }

        private void Collect(OutbreakArchiveEvidence document)
        {
            document.collected = true;
            document.gameObject.SetActive(false);
            CollectedDocuments++;
            Announce("RECUPERADO: " + document.documentTitle + " (" + CollectedDocuments + "/" + DocumentCount + ")");

            if (!firstWave && CollectedDocuments >= 2)
            {
                firstWave = true;
                Deploy(Mathf.Min(4, reserveSpecters == null ? 0 : reserveSpecters.Length));
                Announce("ALERTA: Se detectó un aumento de actividad espectral.");
            }
            else if (!secondWave && CollectedDocuments >= DocumentCount && DocumentCount > 0)
            {
                secondWave = true;
                Deploy(reserveSpecters == null ? 0 : reserveSpecters.Length - nextReserveIndex);
                Announce("ALERTA CRÍTICA: Regresá a la terminal de contención.");
            }
        }

        private void Announce(string message)
        {
            LastEvent = message;
            eventUntil = Time.time + 5f;
            Debug.Log("ARCHIVO NEGRO: " + message);
        }

        private void Deploy(int count)
        {
            if (reserveSpecters == null) return;
            for (int i = 0; i < count && nextReserveIndex < reserveSpecters.Length; i++)
            {
                var specter = reserveSpecters[nextReserveIndex++];
                if (specter == null || specter.IsDead) continue;
                Vector3 position = ChooseSpawn(i);
                specter.transform.position = position;
                specter.gameObject.SetActive(true);
            }
        }

        private Vector3 ChooseSpawn(int offset)
        {
            Vector3 result = player.position + player.forward * -20f;
            float bestScore = -100f;
            if (spawnPoints == null) return result;
            foreach (var point in spawnPoints)
            {
                if (point == null) continue;
                if (!NavMesh.SamplePosition(point.position, out NavMeshHit hit, 5f, NavMesh.AllAreas)) continue;
                float distance = Vector3.Distance(player.position, hit.position);
                // Solo consideramos ubicaciones que no hagan daño instantáneo al aparecer.
                if (distance < safeSpawnDistance) continue;
                float score = distance + Mathf.Sin((offset + nextReserveIndex) * 1.41f) * 1.2f;
                if (score <= bestScore) continue;
                bestScore = score;
                result = hit.position + Vector3.up;
            }
            if (bestScore < 0)
                Debug.LogWarning("ARCHIVO NEGRO: los marcadores estaban cerca del jugador. Revisá los puntos de aparición.");
            return result;
        }
    }
}
