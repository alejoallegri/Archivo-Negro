using UnityEngine;

namespace ProjectOutbreak
{
    /// <summary>Zona corporal con multiplicador de daño de proyectil.</summary>
    public class OutbreakHitbox : MonoBehaviour
    {
        [Min(.1f)] public float damageMultiplier = 1f;
    }
}
