using UnityEngine;

/// <summary>
/// Red de puntos de navegación para enemigos que caminan (no atraviesan paredes).
/// Dos nodos están conectados si un cuerpo del radio indicado cabe en línea recta entre ellos;
/// las conexiones se recalculan periódicamente para tener en cuenta puertas que se abren.
/// </summary>
public class NavGraph : MonoBehaviour
{
    [SerializeField] private Transform[] nodes;
    [SerializeField] private float agentRadius = 0.3f;
    [SerializeField] private float rebuildInterval = 1f;

    private bool[,] links;
    private float builtAt = float.NegativeInfinity;
    private float[] cost;
    private int[] previous;
    private bool[] done;
    private float[] goalCost;

    public int NodeCount => nodes != null ? nodes.Length : 0;

    public bool IsClear(Vector2 from, Vector2 to) => LineOfSight.ClearWide(from, to, agentRadius);

    /// <summary>
    /// Devuelve el siguiente punto hacia el destino: el propio destino si el camino es directo,
    /// o el primer nodo de la ruta más corta. False si no hay ruta conocida.
    /// </summary>
    public bool TryGetNextPoint(Vector2 from, Vector2 to, out Vector2 next)
    {
        next = to;
        if (IsClear(from, to)) return true;
        if (Vector2.Distance(from, to) < 1.5f && LineOfSight.Clear(from, to)) return true;

        int n = NodeCount;
        if (n == 0) return false;
        EnsureLinks();

        for (int i = 0; i < n; i++)
        {
            Vector2 p = nodes[i].position;
            cost[i] = IsClear(from, p) ? Vector2.Distance(from, p) : float.PositiveInfinity;
            goalCost[i] = LineOfSight.Clear(p, to) ? Vector2.Distance(p, to) : float.PositiveInfinity;
            previous[i] = -1;
            done[i] = false;
        }

        for (int step = 0; step < n; step++)
        {
            int u = -1;
            for (int i = 0; i < n; i++)
                if (!done[i] && !float.IsInfinity(cost[i]) && (u < 0 || cost[i] < cost[u])) u = i;
            if (u < 0) break;
            done[u] = true;

            for (int v = 0; v < n; v++)
            {
                if (!links[u, v] || done[v]) continue;
                float c = cost[u] + Vector2.Distance(nodes[u].position, nodes[v].position);
                if (c < cost[v])
                {
                    cost[v] = c;
                    previous[v] = u;
                }
            }
        }

        int end = -1;
        float best = float.PositiveInfinity;
        for (int i = 0; i < n; i++)
        {
            float total = cost[i] + goalCost[i];
            if (total < best)
            {
                best = total;
                end = i;
            }
        }
        if (end < 0) return false;

        int first = end;
        int second = -1;
        while (previous[first] >= 0)
        {
            second = first;
            first = previous[first];
        }

        Vector2 firstPoint = nodes[first].position;
        if (Vector2.Distance(from, firstPoint) > 0.35f) next = firstPoint;
        else next = second >= 0 ? (Vector2)nodes[second].position : to;
        return true;
    }

    private void EnsureLinks()
    {
        int n = NodeCount;
        if (links == null || links.GetLength(0) != n)
        {
            links = new bool[n, n];
            cost = new float[n];
            previous = new int[n];
            done = new bool[n];
            goalCost = new float[n];
            builtAt = float.NegativeInfinity;
        }
        if (Time.time - builtAt < rebuildInterval) return;
        builtAt = Time.time;

        for (int a = 0; a < n; a++)
        {
            links[a, a] = false;
            for (int b = a + 1; b < n; b++)
            {
                bool ok = IsClear(nodes[a].position, nodes[b].position);
                links[a, b] = ok;
                links[b, a] = ok;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (nodes == null) return;
        Gizmos.color = new Color(0.3f, 1f, 0.5f);
        foreach (Transform t in nodes)
            if (t != null) Gizmos.DrawWireSphere(t.position, 0.2f);
    }
}
