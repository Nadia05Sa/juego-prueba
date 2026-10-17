using UnityEngine;

public class ExaminableObject : Interactable
{
    [TextArea(2, 6)]
    [SerializeField] private string description;
    [Tooltip("Opcional: texto en las siguientes veces que se examina.")]
    [TextArea(2, 6)]
    [SerializeField] private string repeatDescription;
    [Tooltip("Opcional: bandera que se activa al examinar por primera vez.")]
    [SerializeField] private string flagOnExamine;
    [SerializeField] private float messageDuration = 5f;

    private bool examined;

    public override void Interact(InteractionController interactor)
    {
        string text = examined && !string.IsNullOrEmpty(repeatDescription) ? repeatDescription : description;
        UIManager.Instance?.ShowMessage(EcosRichText.Format(text), messageDuration);

        if (examined) return;
        examined = true;
        LevelManager.Instance?.SetFlag(flagOnExamine);
    }
}
