using UnityEngine;

/// <summary>
/// Cadenas colgantes: suenan cuando el jugador pasa por debajo y emiten ruido
/// (más si corre). Sirven para atraer a la entidad o para que el jugador aprenda a rodearlas.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ChainTrap : MonoBehaviour
{
    [SerializeField] private float walkNoiseRadius = 5f;
    [SerializeField] private float runNoiseRadius = 8f;
    [SerializeField] private float cooldown = 2.5f;
    [SerializeField] private Transform[] chains;
    [SerializeField] private float swingAngle = 25f;
    [TextArea(1, 3)]
    [SerializeField] private string firstMessage = "Las cadenas chocan entre sí con un estruendo metálico...";

    private float cooldownTimer;
    private float swing;
    private bool messageShown;

    public int RattleCount { get; private set; }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (swing <= 0f || chains == null) return;

        swing = Mathf.Max(0f, swing - Time.deltaTime * 0.6f);
        for (int i = 0; i < chains.Length; i++)
        {
            if (chains[i] == null) continue;
            float angle = Mathf.Sin(Time.time * 11f + i) * swingAngle * swing;
            chains[i].localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (cooldownTimer > 0f || !other.CompareTag("Player")) return;
        if (!other.TryGetComponent(out PlayerMovement movement) || !movement.IsMoving) return;
        Rattle(movement.IsRunning);
    }

    public void Rattle(bool loud)
    {
        cooldownTimer = cooldown;
        swing = 1f;
        RattleCount++;
        AudioManager.Instance?.Play(AudioManager.Sfx.Chains, loud ? 1f : 0.7f);
        NoiseSystem.Emit(transform.position, loud ? runNoiseRadius : walkNoiseRadius, gameObject);

        if (messageShown || string.IsNullOrEmpty(firstMessage)) return;
        messageShown = true;
        UIManager.Instance?.ShowMessage(firstMessage);
    }
}
