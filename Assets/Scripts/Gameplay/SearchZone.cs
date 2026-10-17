using UnityEngine;

/// <summary>
/// Zona amplia donde hay algo escondido. El jugador debe pulsar E cerca del punto oculto.
/// </summary>
public class SearchZone : Interactable
{
    [SerializeField] private string enterMessage = "Parece que hay algo entre las flores.";
    [SerializeField] private string wrongSpotMessage = "Solo hay flores marchitas aquí. Busca en otro lugar.";
    [SerializeField] private string emptyMessage = "Aquí ya no hay nada más.";
    [SerializeField] private Transform hiddenSpot;
    [SerializeField] private float searchRadius = 1.3f;
    [SerializeField] private NoteData foundNote;
    [Tooltip("Brillo tenue que delata el punto oculto.")]
    [SerializeField] private SpriteRenderer glint;
    [Tooltip("Opcional: objeto que se añade al inventario al encontrarlo.")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;

    private bool found;
    private bool enterShown;

    private void Reset() => prompt = "Buscar entre las flores";

    private void Update()
    {
        if (glint == null || found) return;
        Color c = glint.color;
        c.a = 0.25f + Mathf.PingPong(Time.time * 0.6f, 0.45f);
        glint.color = c;
    }

    public override void Interact(InteractionController interactor)
    {
        if (found)
        {
            UIManager.Instance?.ShowMessage(emptyMessage);
            return;
        }

        float dist = Vector2.Distance(interactor.transform.position, hiddenSpot.position);
        if (dist > searchRadius)
        {
            UIManager.Instance?.ShowMessage(wrongSpotMessage);
            return;
        }

        found = true;
        if (glint != null) glint.gameObject.SetActive(false);
        if (!string.IsNullOrEmpty(itemId)) InventoryManager.Instance?.Add(itemId, itemName);
        if (foundNote == null) return;

        UIManager.Instance?.ShowNote(foundNote.Title, foundNote.Body);
        ClueManager.Instance?.Register(foundNote.Id, foundNote.Title, foundNote.Body, foundNote.IsClue);
        LevelManager.Instance?.SetFlag(foundNote.Id);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enterShown || found || !other.CompareTag("Player")) return;
        enterShown = true;
        UIManager.Instance?.ShowMessage(enterMessage);
    }
}
