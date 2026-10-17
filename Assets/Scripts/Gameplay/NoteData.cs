using UnityEngine;

[CreateAssetMenu(menuName = "Ecos del Mictlán/Nota", fileName = "Nota")]
public class NoteData : ScriptableObject
{
    [Tooltip("Identificador único. Al leer la nota se activa una bandera con este id.")]
    [SerializeField] private string id;
    [SerializeField] private string title;
    [TextArea(6, 20)]
    [SerializeField] private string body;
    [Tooltip("Si es pista, se registra en el ClueManager al leerla por primera vez.")]
    [SerializeField] private bool isClue;

    public string Id => id;
    public string Title => title;
    public string Body => body;
    public bool IsClue => isClue;
}
