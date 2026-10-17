using UnityEngine;

/// <summary>
/// Configura un ParticleSystem 2D discreto (polvo, pétalos, niebla ligera).
/// Se puede crear vacío y llamar a Apply, o dejar los valores del Inspector.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AmbientParticles : MonoBehaviour
{
    [SerializeField] private Color tint = new Color(1f, 0.85f, 0.45f, 0.35f);
    [SerializeField] private Vector2 area = new Vector2(10f, 8f);
    [SerializeField] private float rate = 4f;
    [SerializeField] private float lifetime = 7f;
    [SerializeField] private float startSize = 0.07f;
    [SerializeField] private float speedMin = 0.05f;
    [SerializeField] private float speedMax = 0.22f;
    [SerializeField] private int maxParticles = 48;
    [SerializeField] private Sprite particleSprite;

    public void Apply(Color color, Vector2 boxArea, float emissionRate = 4f, Sprite sprite = null)
    {
        tint = color;
        area = boxArea;
        rate = emissionRate;
        if (sprite != null) particleSprite = sprite;
        Configure();
    }

    private void Start() => Configure();

    private void Configure()
    {
        var ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSize = startSize;
        main.startColor = tint;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(area.x, area.y, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // Las tres curvas deben usar el mismo modo (TwoConstants).
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(tint, 0f), new GradientColorKey(tint, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(tint.a, 0.2f),
                new GradientAlphaKey(tint.a * 0.6f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOver.color = grad;

        var sizeOver = ps.sizeOverLifetime;
        sizeOver.enabled = true;
        sizeOver.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 0.2f));

        var renderer = GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = -50;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            var mat = new Material(shader);
            if (particleSprite != null && particleSprite.texture != null)
                mat.mainTexture = particleSprite.texture;
            mat.color = Color.white;
            renderer.material = mat;
        }

        if (!ps.isPlaying) ps.Play();
    }
}
