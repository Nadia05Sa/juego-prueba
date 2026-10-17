using UnityEngine;

/// <summary>
/// Objeto que se examina abriendo un panel con fotografía (frente y reverso).
/// También sirve para lápidas u otros objetos con imagen + inscripción.
/// </summary>
public class PhotoInspectable : Interactable
{
    [SerializeField] private Sprite frontSprite;
    [SerializeField] private Sprite backSprite;
    [TextArea(2, 5)]
    [SerializeField] private string frontCaption =
        "Una fotografía familiar: una mujer con rebozo junto al aljibe, abrazando a dos niños. Alguien rayó su rostro.";
    [TextArea(2, 4)]
    [SerializeField] private string backCaption = "Al reverso, con tinta desvanecida:\nCatalina y sus hijos, 1909.";
    [SerializeField] private string flagOnExamine = "foto_familia";
    [Tooltip("Si se asigna, la nota queda en el inventario para releerla.")]
    [SerializeField] private NoteData registerNote;
    [Tooltip("Texto de ayuda del panel. Vacío = voltear/guardar.")]
    [SerializeField] private string inspectHint;
    [SerializeField] private bool allowFlip = true;
    [Tooltip("Si es true, el texto se dibuja encima de la imagen (lápidas).")]
    [SerializeField] private bool inscriptionOnImage;

    private bool examined;

    private void Reset() => prompt = "Examinar fotografía";

    public override bool CanInteract => base.CanInteract && frontSprite != null;

    public override void Interact(InteractionController interactor)
    {
        string hint = inspectHint;
        if (string.IsNullOrEmpty(hint) && !allowFlip)
            hint = "Esc — Cerrar";

        Sprite back = allowFlip ? backSprite : frontSprite;
        string backText = allowFlip ? backCaption : frontCaption;
        UIManager.Instance?.ShowPhoto(frontSprite, back, frontCaption, backText, hint, allowFlip, inscriptionOnImage);

        if (examined) return;
        examined = true;

        if (registerNote != null)
        {
            ClueManager.Instance?.Register(registerNote.Id, registerNote.Title, registerNote.Body, registerNote.IsClue);
            LevelManager.Instance?.SetFlag(registerNote.Id);
        }

        if (!string.IsNullOrEmpty(flagOnExamine))
            LevelManager.Instance?.SetFlag(flagOnExamine);
    }
}
