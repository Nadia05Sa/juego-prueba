using UnityEngine;

/// <summary>
/// Objeto recogible. Si es contenedor (cajón, armario) permanece en escena y queda vacío;
/// si no, desaparece al recogerlo.
/// </summary>
public class KeyItem : Interactable
{
    [SerializeField] private string itemId = "llave_oficina";
    [SerializeField] private string itemName = "Llave oxidada";
    [TextArea(2, 4)]
    [SerializeField] private string pickupMessage;
    [SerializeField] private bool isContainer;
    [SerializeField] private string emptyPrompt = "Examinar";
    [SerializeField] private string emptyMessage = "Aquí ya no hay nada más.";
    [Tooltip("Visual del objeto que se oculta al recogerlo (para contenedores).")]
    [SerializeField] private GameObject visualToHide;
    [Tooltip("Opcional: bandera que se activa al recogerlo.")]
    [SerializeField] private string flagOnPickup;
    [Tooltip("Opcional: nota/carta que se muestra al recoger (p. ej. carta bajo el medallón).")]
    [SerializeField] private NoteData noteOnPickup;
    [TextArea(1, 3)]
    [SerializeField] private string afterNoteMessage;

    private bool taken;

    public override string GetPrompt() => taken ? emptyPrompt : prompt;

    public override void Interact(InteractionController interactor)
    {
        if (taken)
        {
            UIManager.Instance?.ShowMessage(EcosRichText.Format(emptyMessage));
            return;
        }

        taken = true;
        InventoryManager.Instance?.Add(itemId, itemName);
        AudioManager.Instance?.Play(AudioManager.Sfx.Pickup);
        LevelManager.Instance?.SetFlag(flagOnPickup);

        if (visualToHide != null) visualToHide.SetActive(false);
        if (!isContainer) gameObject.SetActive(false);

        if (noteOnPickup != null)
        {
            ClueManager.Instance?.Register(noteOnPickup.Id, noteOnPickup.Title, noteOnPickup.Body, noteOnPickup.IsClue);
            LevelManager.Instance?.SetFlag(noteOnPickup.Id);
            string followUp = !string.IsNullOrEmpty(afterNoteMessage)
                ? afterNoteMessage
                : pickupMessage;
            UIManager.Instance?.ShowNote(noteOnPickup.Title, noteOnPickup.Body, EcosRichText.Format(followUp));
            return;
        }

        UIManager.Instance?.ShowMessage(
            string.IsNullOrEmpty(pickupMessage) ? "Has obtenido: " + itemName : EcosRichText.Format(pickupMessage));
    }
}
