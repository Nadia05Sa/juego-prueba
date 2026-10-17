using System.Collections;
using UnityEngine;

public enum DoorState
{
    Cerrada,
    Abierta,
    BloqueadaPorLlave,
    BloqueadaPorObjetivo,
    Abriendo
}

public class DoorController : Interactable
{
    [SerializeField] private DoorState initialState = DoorState.Cerrada;

    [Header("Requisitos")]
    [SerializeField] private string requiredItemId;
    [SerializeField] private string requiredItemName;
    [SerializeField] private bool consumeItem;
    [SerializeField] private string requiredFlag;
    [TextArea(2, 4)]
    [SerializeField] private string lockedMessage = "Está cerrada.";

    [Header("Al desbloquear por objetivo")]
    [SerializeField] private bool autoOpenOnFlag;
    [TextArea(1, 3)]
    [SerializeField] private string unlockMessage;

    [Header("Al abrir")]
    [SerializeField] private string flagOnOpen;
    [SerializeField] private float noiseRadiusOnOpen = 3f;
    [SerializeField] private AudioManager.Sfx openSound = AudioManager.Sfx.Door;

    [Header("Referencias")]
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private float openDuration = 0.6f;
    [SerializeField] private float openAlpha = 0.18f;
    [Tooltip("Opcional: Animator con un trigger \"Open\".")]
    [SerializeField] private Animator animator;

    public DoorState State { get; private set; }

    private void Reset() => prompt = "Abrir";

    private void Awake()
    {
        State = initialState;
        if (State == DoorState.Abierta)
        {
            ApplyOpenVisual(1f);
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.Play("Open", 0, 1f);
        }
    }

    private void Start()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.OnFlagSet += HandleFlag;
    }

    private void OnDestroy()
    {
        if (LevelManager.Instance != null) LevelManager.Instance.OnFlagSet -= HandleFlag;
    }

    public override bool CanInteract => base.CanInteract && State != DoorState.Abierta && State != DoorState.Abriendo;

    public override void Interact(InteractionController interactor)
    {
        switch (State)
        {
            case DoorState.Cerrada:
                Open();
                break;

            case DoorState.BloqueadaPorLlave:
                if (InventoryManager.Instance != null && InventoryManager.Instance.Has(requiredItemId))
                {
                    if (consumeItem) InventoryManager.Instance.Remove(requiredItemId);
                    UIManager.Instance?.ShowMessage("Usas: " + requiredItemName);
                    Open();
                }
                else
                {
                    Deny();
                }
                break;

            case DoorState.BloqueadaPorObjetivo:
                if (LevelManager.Instance != null && LevelManager.Instance.HasFlag(requiredFlag)) Open();
                else Deny();
                break;
        }
    }

    private void HandleFlag(string flag)
    {
        if (State != DoorState.BloqueadaPorObjetivo || flag != requiredFlag) return;

        if (!string.IsNullOrEmpty(unlockMessage)) UIManager.Instance?.ShowMessage(unlockMessage, 4f);
        if (autoOpenOnFlag) Open();
        else State = DoorState.Cerrada;
    }

    private void Deny()
    {
        AudioManager.Instance?.Play(AudioManager.Sfx.Locked);
        UIManager.Instance?.ShowMessage(lockedMessage);
    }

    public void Open()
    {
        if (State == DoorState.Abierta || State == DoorState.Abriendo) return;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        State = DoorState.Abriendo;
        AudioManager.Instance?.Play(openSound);
        NoiseSystem.Emit(transform.position, noiseRadiusOnOpen, gameObject);
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Open");

        for (float t = 0f; t < openDuration; t += Time.deltaTime)
        {
            ApplyOpenVisual(t / openDuration);
            yield return null;
        }

        ApplyOpenVisual(1f);
        State = DoorState.Abierta;
        LevelManager.Instance?.SetFlag(flagOnOpen);
    }

    private void ApplyOpenVisual(float progress)
    {
        // Si hay Animator de puerta, él mueve el visual; solo desactivamos colisión.
        bool animated = animator != null && animator.runtimeAnimatorController != null;
        if (!animated && doorRenderer != null)
        {
            Color c = doorRenderer.color;
            c.a = Mathf.Lerp(1f, openAlpha, progress);
            doorRenderer.color = c;
        }
        if (progress >= 1f && blockingCollider != null) blockingCollider.enabled = false;
    }
}
