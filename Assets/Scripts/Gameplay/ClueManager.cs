using System;
using System.Collections.Generic;
using UnityEngine;

public class ClueManager : MonoBehaviour
{
    public static ClueManager Instance { get; private set; }

    public struct Entry
    {
        public string Id;
        public string Title;
        public string Body;
        public bool IsClue;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private int clueCount;

    /// <summary>Solo pistas (isClue). Usado por acertijos y requisitos de mecanismos.</summary>
    public int Count => clueCount;
    public IReadOnlyList<Entry> Entries => entries;
    public event Action OnChanged;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Has(string id)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Id == id) return true;
        return false;
    }

    /// <summary>Registra una nota/carta leída. Las pistas (isClue) cuentan para acertijos.</summary>
    public bool Register(string id, string title, string body, bool isClue)
    {
        if (string.IsNullOrEmpty(id) || Has(id)) return false;

        entries.Add(new Entry
        {
            Id = id,
            Title = title ?? string.Empty,
            Body = EcosRichText.Format(body ?? string.Empty),
            IsClue = isClue
        });

        if (isClue)
        {
            clueCount++;
            AudioManager.Instance?.Play(AudioManager.Sfx.Clue);
            UIManager.Instance?.ShowMessage("Pista encontrada: " + title);
        }

        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Compatibilidad: registra una pista sin cuerpo.</summary>
    public bool AddClue(string id, string title) => Register(id, title, string.Empty, true);

    public bool TryGet(string id, out Entry entry)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Id != id) continue;
            entry = entries[i];
            return true;
        }
        entry = default;
        return false;
    }
}
