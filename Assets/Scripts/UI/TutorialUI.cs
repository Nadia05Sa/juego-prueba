using UnityEngine;

/// <summary>
/// Panel de controles al inicio del nivel. Bloquea el movimiento hasta pulsar E o Esc.
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private PlayerMovement playerMovement;

    private bool open;
    private int openedFrame = -1;

    public bool IsOpen => open && panel != null && panel.activeInHierarchy;

    private void Awake()
    {
        // Si el panel es este mismo GameObject, no apagarlo aquí:
        // Show() llama SetActive(true) → Awake → SetActive(false) dejaba el
        // movimiento bloqueado con el tutorial invisible e imposible de cerrar.
        if (panel != null && panel != gameObject)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (!IsOpen) return;
        if (Time.frameCount == openedFrame) return;
        GameInput input = GameInput.Instance;
        if (input == null) return;
        if (input.InteractPressed || input.PausePressed)
            Hide();
    }

    public void Show()
    {
        if (panel == null || open) return;
        open = true;
        openedFrame = Time.frameCount;
        playerMovement?.AddLock(this);
        panel.SetActive(true);

        // Seguridad: si algo reapagó el panel al activarlo, liberar el movimiento.
        if (!panel.activeInHierarchy)
        {
            open = false;
            playerMovement?.RemoveLock(this);
        }
    }

    public void Hide()
    {
        if (!open) return;
        open = false;
        playerMovement?.RemoveLock(this);
        if (panel != null && panel.activeSelf)
            panel.SetActive(false);
    }
}
