using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Serializable]
    public struct Item
    {
        public string id;
        public string displayName;
    }

    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<Item> items = new List<Item>();

    public IReadOnlyList<Item> Items => items;
    public event Action OnChanged;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool Has(string id) => !string.IsNullOrEmpty(id) && items.Exists(i => i.id == id);

    public bool Add(string id, string displayName)
    {
        if (string.IsNullOrEmpty(id) || Has(id)) return false;
        items.Add(new Item { id = id, displayName = displayName });
        OnChanged?.Invoke();
        return true;
    }

    public bool Remove(string id)
    {
        int removed = items.RemoveAll(i => i.id == id);
        if (removed > 0) OnChanged?.Invoke();
        return removed > 0;
    }
}
