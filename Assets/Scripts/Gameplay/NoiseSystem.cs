using System;
using UnityEngine;

public static class NoiseSystem
{
    /// <summary>Posición del ruido, radio en unidades y objeto que lo produjo.</summary>
    public static event Action<Vector2, float, GameObject> OnNoise;

    public static void Emit(Vector2 position, float radius, GameObject source = null)
    {
        if (radius <= 0f) return;
        OnNoise?.Invoke(position, radius, source);
    }
}
