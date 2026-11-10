using UnityEngine;

namespace ProjectOutbreak
{
    /// <summary>Documento recolectable por proximidad y tecla E.</summary>
    public class OutbreakArchiveEvidence : MonoBehaviour
    {
        public string documentTitle = "EXPEDIENTE DESCONOCIDO";
        [HideInInspector] public bool collected;
    }
}
