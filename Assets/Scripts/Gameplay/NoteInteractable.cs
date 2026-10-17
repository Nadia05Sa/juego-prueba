using UnityEngine;

public class NoteInteractable : Interactable
{
    [SerializeField] private NoteData note;
    [Tooltip("Mensaje breve al cerrar la nota la primera vez (orientación).")]
    [SerializeField] private string afterCloseHint;

    private bool read;

    private void Reset() => prompt = "Leer";

    public override bool CanInteract => base.CanInteract && note != null;

    public override void Interact(InteractionController interactor)
    {
        string follow = !read && !string.IsNullOrEmpty(afterCloseHint) ? afterCloseHint : null;
        UIManager.Instance?.ShowNote(note.Title, note.Body, follow);
        if (read) return;

        read = true;
        ClueManager.Instance?.Register(note.Id, note.Title, note.Body, note.IsClue);
        LevelManager.Instance?.SetFlag(note.Id);
    }
}
