using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class InteractionController : MonoBehaviour
{
    private readonly List<Interactable> nearby = new List<Interactable>();
    private PlayerMovement movement;
    private PlayerHiding hiding;

    public PlayerMovement Movement => movement;
    public Interactable Current { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        hiding = GetComponent<PlayerHiding>();
    }

    private void Update()
    {
        nearby.RemoveAll(i => i == null || !i.isActiveAndEnabled);

        UIManager ui = UIManager.Instance;
        bool uiBlocking = ui != null && ui.IsBlockingInput;
        bool hidden = hiding != null && hiding.IsHidden;

        Current = uiBlocking || hidden || movement.IsLocked ? null : FindBest();

        if (ui != null) ui.SetPrompt(uiBlocking ? string.Empty : BuildPrompt(hidden));

        if (Current != null && GameInput.Instance != null && GameInput.Instance.InteractPressed)
            Current.Interact(this);
    }

    private string BuildPrompt(bool hidden)
    {
        if (hidden) return "Q — Salir del escondite";

        string text = Current != null ? "E — " + Current.GetPrompt() : string.Empty;
        if (hiding != null && hiding.CanHide)
            text = string.IsNullOrEmpty(text) ? "Q — Esconderse" : text + "     Q — Esconderse";
        return text;
    }

    private Interactable FindBest()
    {
        Interactable best = null;
        float bestDist = float.MaxValue;
        Vector2 pos = transform.position;

        foreach (Interactable i in nearby)
        {
            if (!i.CanInteract) continue;
            float d = ((Vector2)i.transform.position - pos).sqrMagnitude;
            if (best == null || i.Priority > best.Priority || (i.Priority == best.Priority && d < bestDist))
            {
                best = i;
                bestDist = d;
            }
        }
        return best;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Interactable i = other.GetComponentInParent<Interactable>();
        if (i != null && !nearby.Contains(i)) nearby.Add(i);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Interactable i = other.GetComponentInParent<Interactable>();
        if (i != null) nearby.Remove(i);
    }
}
