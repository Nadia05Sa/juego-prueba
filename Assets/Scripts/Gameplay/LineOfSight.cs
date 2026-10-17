using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Comprobaciones de visibilidad contra obstáculos estáticos (muros, muebles, puertas cerradas).
/// Ignora triggers y cuerpos dinámicos (jugador y enemigos).
/// </summary>
public static class LineOfSight
{
    private static readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();

    private static ContactFilter2D Filter
    {
        get
        {
            ContactFilter2D f = ContactFilter2D.noFilter;
            f.useTriggers = false;
            return f;
        }
    }

    public static bool Clear(Vector2 from, Vector2 to, Transform ignoreRoot = null)
    {
        hits.Clear();
        Physics2D.Linecast(from, to, Filter, hits);
        return !BlockedBy(hits, ignoreRoot);
    }

    /// <summary>Como Clear, pero con el grosor de un cuerpo (radio) para saber si cabe por el camino.</summary>
    public static bool ClearWide(Vector2 from, Vector2 to, float radius)
    {
        Vector2 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.001f) return true;
        hits.Clear();
        Physics2D.CircleCast(from, radius, delta / dist, Filter, hits, dist);
        return !BlockedBy(hits, null);
    }

    private static bool BlockedBy(List<RaycastHit2D> results, Transform ignoreRoot)
    {
        foreach (RaycastHit2D h in results)
        {
            if (h.collider == null) continue;
            if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
            Rigidbody2D body = h.collider.attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic) continue;
            return true;
        }
        return false;
    }
}
