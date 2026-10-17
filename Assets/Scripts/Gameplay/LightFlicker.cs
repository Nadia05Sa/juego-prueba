using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Parpadeo suave de una Light2D (velas, faroles).</summary>
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private Light2D light2D;
    [SerializeField] private float amount = 0.12f;
    [SerializeField] private float speed = 5f;

    private float baseIntensity;
    private float phase;

    private void Awake()
    {
        if (light2D == null) light2D = GetComponent<Light2D>();
        if (light2D != null) baseIntensity = light2D.intensity;
        phase = Random.Range(0f, 20f);
    }

    private void Update()
    {
        if (light2D == null || !light2D.enabled) return;
        float n = Mathf.PerlinNoise(Time.time * speed, phase);
        light2D.intensity = baseIntensity * (1f + (n - 0.5f) * 2f * amount);
    }
}
