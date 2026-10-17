using UnityEngine;

/// <summary>
/// Base de todo objeto con el que se interactúa con E. Necesita un Collider2D con Is Trigger
/// (en este objeto o en un hijo) para que el jugador lo detecte.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected string prompt = "Examinar";
    [Tooltip("Si hay varios objetos cerca, gana el de mayor prioridad y luego el más cercano.")]
    [SerializeField] private int priority;

    public int Priority => priority;
    public virtual bool CanInteract => isActiveAndEnabled;
    public virtual string GetPrompt() => prompt;

    public abstract void Interact(InteractionController interactor);
}
