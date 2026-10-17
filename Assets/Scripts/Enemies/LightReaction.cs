using UnityEngine;

/// <summary>
/// Reacción de una entidad a la linterna: se detiene unos instantes al ser iluminada de cerca.
/// Tiene enfriamiento, así que la luz nunca la detiene de forma indefinida.
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public class LightReaction : MonoBehaviour
{
    [SerializeField] private float stunDuration = 1.5f;
    [SerializeField] private float cooldown = 6f;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Color stunnedTint = new Color(0.6f, 0.6f, 0.75f);
    [TextArea(1, 3)]
    [SerializeField] private string firstMessage = "La luz lo detiene un instante... pero no por mucho tiempo.";

    private EnemyBrain brain;
    private FlashlightController flashlight;
    private float cooldownTimer;
    private bool messageShown;
    private Color bodyColor;

    public int ReactionCount { get; private set; }
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    private void Awake()
    {
        brain = GetComponent<EnemyBrain>();
        if (body != null) bodyColor = body.color;
    }

    private void Start() => flashlight = FindAnyObjectByType<FlashlightController>();

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        UpdateTint();

        if (flashlight == null || cooldownTimer > 0f || brain.IsHarmless) return;
        if (Vector2.Distance(flashlight.transform.position, transform.position) > maxDistance) return;
        if (!flashlight.IsIlluminating(transform.position)) return;

        brain.Stun(stunDuration);
        cooldownTimer = cooldown;
        ReactionCount++;
        AudioManager.Instance?.Play(AudioManager.Sfx.Hooves, 0.8f);

        if (messageShown) return;
        messageShown = true;
        UIManager.Instance?.ShowMessage(firstMessage);
    }

    private void UpdateTint()
    {
        if (body == null) return;
        Color target = brain.State == EnemyState.Aturdido ? stunnedTint : bodyColor;
        target.a = body.color.a;
        body.color = target;
    }
}
