using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mecanismo o palanca de un solo uso. Comprueba sus requisitos (pistas, banderas y objeto)
/// y, si falta algo, dice exactamente qué. Al activarse hace ruido y activa una bandera.
/// </summary>
public class Lever : Interactable
{
    [Header("Requisitos")]
    [SerializeField] private int requiredClues;
    [Tooltip("Cómo se nombran las pistas en el mensaje, por ejemplo \"los documentos del pacto\".")]
    [SerializeField] private string requiredCluesName = "más pistas";
    [SerializeField] private string[] requiredFlags;
    [Tooltip("Nombre de cada bandera en el mensaje (mismo orden que requiredFlags).")]
    [SerializeField] private string[] requiredFlagNames;
    [SerializeField] private string requiredItemId;
    [SerializeField] private string requiredItemName;
    [TextArea(1, 3)]
    [SerializeField] private string lockedPrefix = "El mecanismo no se mueve. Te falta: ";

    [Header("Resultado")]
    [SerializeField] private string flagOnActivate;
    [TextArea(2, 4)]
    [SerializeField] private string activatedMessage;
    [SerializeField] private float noiseRadius = 9f;
    [SerializeField] private string usedPrompt = "Examinar el mecanismo";
    [TextArea(1, 3)]
    [SerializeField] private string usedMessage = "El mecanismo ya está accionado.";

    [Header("Visual")]
    [SerializeField] private Transform handle;
    [SerializeField] private float handleAngle = -70f;
    [SerializeField] private float moveDuration = 0.6f;

    public bool IsActivated { get; private set; }

    public override string GetPrompt() => IsActivated ? usedPrompt : prompt;

    public override void Interact(InteractionController interactor)
    {
        if (IsActivated)
        {
            UIManager.Instance?.ShowMessage(usedMessage);
            return;
        }

        List<string> missing = MissingRequirements();
        if (missing.Count > 0)
        {
            AudioManager.Instance?.Play(AudioManager.Sfx.Locked);
            UIManager.Instance?.ShowMessage(lockedPrefix + string.Join(", ", missing) + ".", 4f);
            return;
        }

        StartCoroutine(Activate());
    }

    public List<string> MissingRequirements()
    {
        var missing = new List<string>();
        int clues = ClueManager.Instance != null ? ClueManager.Instance.Count : 0;
        if (requiredClues > 0 && clues < requiredClues)
            missing.Add($"{requiredCluesName} ({clues}/{requiredClues})");

        if (requiredFlags != null)
        {
            for (int i = 0; i < requiredFlags.Length; i++)
            {
                if (LevelManager.Instance != null && LevelManager.Instance.HasFlag(requiredFlags[i])) continue;
                missing.Add(requiredFlagNames != null && i < requiredFlagNames.Length ? requiredFlagNames[i] : requiredFlags[i]);
            }
        }

        if (!string.IsNullOrEmpty(requiredItemId) && (InventoryManager.Instance == null || !InventoryManager.Instance.Has(requiredItemId)))
            missing.Add(requiredItemName);
        return missing;
    }

    private IEnumerator Activate()
    {
        IsActivated = true;
        AudioManager.Instance?.Play(AudioManager.Sfx.Lever);
        NoiseSystem.Emit(transform.position, noiseRadius, gameObject);
        if (!string.IsNullOrEmpty(activatedMessage)) UIManager.Instance?.ShowMessage(activatedMessage, 5f);

        if (handle != null)
        {
            Quaternion from = handle.localRotation;
            Quaternion to = Quaternion.Euler(0f, 0f, handleAngle);
            for (float t = 0f; t < moveDuration; t += Time.deltaTime)
            {
                handle.localRotation = Quaternion.Slerp(from, to, t / moveDuration);
                yield return null;
            }
            handle.localRotation = to;
        }

        LevelManager.Instance?.SetFlag(flagOnActivate);
    }
}
