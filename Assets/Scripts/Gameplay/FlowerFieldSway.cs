using UnityEngine;

/// <summary>
/// Oscilación suave de muchas flores en un solo Update (sin un script por flor).
/// Si el jugador pasa cerca, el balanceo aumenta un poco.
/// </summary>
public class FlowerFieldSway : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float angle = 4f;
    [SerializeField] private float speed = 1.35f;
    [SerializeField] private float reactRadius = 1.7f;
    [SerializeField] private float reactBoost = 2.2f;

    private Transform[] flowers;
    private float[] phases;
    private float[] speeds;
    private Quaternion[] baseRotations;

    public void Configure(Transform[] targets, Transform playerTransform)
    {
        player = playerTransform;
        if (targets == null || targets.Length == 0)
        {
            flowers = System.Array.Empty<Transform>();
            return;
        }

        flowers = targets;
        phases = new float[flowers.Length];
        speeds = new float[flowers.Length];
        baseRotations = new Quaternion[flowers.Length];
        for (int i = 0; i < flowers.Length; i++)
        {
            phases[i] = Random.Range(0f, Mathf.PI * 2f);
            speeds[i] = speed * Random.Range(0.75f, 1.35f);
            baseRotations[i] = flowers[i] != null ? flowers[i].localRotation : Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        if (flowers == null || flowers.Length == 0) return;

        Vector3 playerPos = player != null ? player.position : Vector3.positiveInfinity;
        float reactSq = reactRadius * reactRadius;

        for (int i = 0; i < flowers.Length; i++)
        {
            Transform flower = flowers[i];
            if (flower == null) continue;

            float amp = angle;
            if (player != null)
            {
                float d = (flower.position - playerPos).sqrMagnitude;
                if (d < reactSq)
                {
                    float t = 1f - Mathf.Sqrt(d) / reactRadius;
                    amp *= 1f + t * (reactBoost - 1f);
                }
            }

            float rot = Mathf.Sin(Time.time * speeds[i] + phases[i]) * amp;
            flower.localRotation = baseRotations[i] * Quaternion.Euler(0f, 0f, rot);
        }
    }
}
