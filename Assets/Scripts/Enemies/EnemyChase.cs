using UnityEngine;

/// <summary>
/// Parámetros de persecución y búsqueda. EnemyBrain los usa para decidir cuándo perseguir,
/// cuándo rendirse y dónde buscar.
/// </summary>
public class EnemyChase : MonoBehaviour
{
    [SerializeField] private float chaseSpeed = 3.6f;
    [Tooltip("A esta distancia empieza a perseguir sin esperar el tiempo de sospecha.")]
    [SerializeField] private float closeChaseDistance = 3f;
    [Tooltip("Segundos sin ver al jugador antes de ir a su última posición conocida.")]
    [SerializeField] private float loseSightTime = 0.8f;
    [SerializeField] private float searchDuration = 4f;
    [SerializeField] private float searchRadius = 2.5f;

    public float ChaseSpeed => chaseSpeed;
    public float CloseChaseDistance => closeChaseDistance;
    public float LoseSightTime => loseSightTime;
    public float SearchDuration => searchDuration;

    public Vector2 RandomSearchPoint(Vector2 center, EnemyDetection detection)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 candidate = center + Random.insideUnitCircle * searchRadius;
            if (detection == null || detection.HasLineOfSight(candidate)) return candidate;
        }
        return center;
    }
}
