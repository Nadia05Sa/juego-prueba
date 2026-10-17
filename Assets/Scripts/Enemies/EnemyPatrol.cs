using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    [Tooltip("Ida y vuelta (1-2-3-2-1). Si está desactivado, recorre en bucle (1-2-3-1).")]
    [SerializeField] private bool pingPong = true;
    [SerializeField] private float waitTime = 1.5f;

    private int index;
    private int direction = 1;

    public bool HasPoints => points != null && points.Length > 0;
    public float WaitTime => waitTime;
    public int Count => HasPoints ? points.Length : 0;
    public Vector2 CurrentTarget => HasPoints ? (Vector2)points[index].position : (Vector2)transform.position;

    public Vector2 PointAt(int i) => points[Mathf.Clamp(i, 0, points.Length - 1)].position;

    public void SetIndex(int i)
    {
        if (!HasPoints) return;
        index = Mathf.Clamp(i, 0, points.Length - 1);
    }

    public void Advance()
    {
        if (!HasPoints || points.Length == 1) return;

        if (pingPong)
        {
            if (index + direction >= points.Length || index + direction < 0) direction = -direction;
            index += direction;
        }
        else
        {
            index = (index + 1) % points.Length;
        }
    }

    public int NearestIndex(Vector2 from)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < Count; i++)
        {
            float d = (PointAt(i) - from).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    public int FarthestIndex(Vector2 from)
    {
        int best = 0;
        float bestDist = -1f;
        for (int i = 0; i < Count; i++)
        {
            float d = (PointAt(i) - from).sqrMagnitude;
            if (d > bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasPoints) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Gizmos.DrawWireSphere(points[i].position, 0.25f);
            if (i + 1 < points.Length && points[i + 1] != null) Gizmos.DrawLine(points[i].position, points[i + 1].position);
        }
    }
}
