using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de inventario estilo cuaderno: objetos a la izquierda y notas/pistas a la derecha.
/// Se abre y cierra con la tecla I (acción Inventory). Clic en una nota la reabre.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform itemSlotsRoot;
    [SerializeField] private Transform clueSlotsRoot;
    [SerializeField] private Text emptyItemsText;
    [SerializeField] private Text emptyCluesText;
    [SerializeField] private Text hintText;
    [SerializeField] private int maxItemSlots = 12;
    [SerializeField] private int maxClueSlots = 10;
    [SerializeField] private Sprite defaultItemIcon;
    [SerializeField] private Sprite defaultClueIcon;
    [SerializeField] private Sprite keyIcon;
    [SerializeField] private Sprite medallionIcon;
    [SerializeField] private Sprite crowbarIcon;
    [SerializeField] private Sprite batteryIcon;

    private readonly List<SlotView> itemSlots = new List<SlotView>();
    private readonly List<SlotView> clueSlots = new List<SlotView>();
    private bool open;

    private sealed class SlotView
    {
        public GameObject root;
        public Image icon;
        public Text label;
        public Image frame;
        public Button button;
        public string boundId;
    }

    public bool IsOpen => open;

    private void Awake()
    {
        CacheSlots(itemSlotsRoot, itemSlots, maxItemSlots, false);
        CacheSlots(clueSlotsRoot, clueSlots, maxClueSlots, true);
        if (panel != null)
        {
            panel.SetActive(false);
            panel.transform.localScale = Vector3.one;
        }
        if (hintText != null) hintText.text = "I — Inventario";
    }

    private void Start()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnChanged += Refresh;
        if (ClueManager.Instance != null) ClueManager.Instance.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnChanged -= Refresh;
        if (ClueManager.Instance != null) ClueManager.Instance.OnChanged -= Refresh;
    }

    public void Toggle()
    {
        if (open) Close();
        else Open();
    }

    public void Open()
    {
        if (panel == null || open) return;
        Refresh();
        panel.SetActive(true);
        open = true;
        panel.transform.localScale = Vector3.one;
        AudioManager.Instance?.Play(AudioManager.Sfx.Click, 0.6f);
    }

    public void Close()
    {
        if (panel == null || !open) return;
        panel.SetActive(false);
        open = false;
        AudioManager.Instance?.Play(AudioManager.Sfx.Click, 0.35f);
    }

    public void Refresh()
    {
        var items = InventoryManager.Instance != null ? InventoryManager.Instance.Items : null;
        int itemCount = items != null ? items.Count : 0;
        for (int i = 0; i < itemSlots.Count; i++)
        {
            bool has = i < itemCount;
            itemSlots[i].root.SetActive(has);
            itemSlots[i].boundId = null;
            if (!has) continue;
            InventoryManager.Item item = items[i];
            itemSlots[i].label.text = item.displayName;
            itemSlots[i].label.alignment = TextAnchor.MiddleLeft;
            itemSlots[i].icon.sprite = IconFor(item.id) ?? defaultItemIcon;
            itemSlots[i].icon.enabled = itemSlots[i].icon.sprite != null;
            itemSlots[i].boundId = item.id;
        }
        if (emptyItemsText != null)
        {
            emptyItemsText.text = itemCount == 0 ? "Todavía no llevas objetos." : string.Empty;
            emptyItemsText.alignment = TextAnchor.MiddleCenter;
        }

        var notes = ClueManager.Instance != null ? ClueManager.Instance.Entries : null;
        int noteCount = notes != null ? notes.Count : 0;
        for (int i = 0; i < clueSlots.Count; i++)
        {
            bool has = i < noteCount;
            clueSlots[i].root.SetActive(has);
            clueSlots[i].boundId = null;
            if (!has) continue;
            ClueManager.Entry entry = notes[i];
            clueSlots[i].label.text = entry.Title;
            clueSlots[i].label.alignment = TextAnchor.MiddleLeft;
            clueSlots[i].icon.sprite = defaultClueIcon;
            clueSlots[i].icon.enabled = defaultClueIcon != null;
            clueSlots[i].boundId = entry.Id;
        }
        if (emptyCluesText != null)
        {
            emptyCluesText.text = noteCount == 0 ? "Aún no has leído notas ni cartas.\n(Clic para releerlas aquí.)" : string.Empty;
            emptyCluesText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void OnClueClicked(int index)
    {
        if (!open || ClueManager.Instance == null) return;
        if (index < 0 || index >= ClueManager.Instance.Entries.Count) return;
        ClueManager.Entry entry = ClueManager.Instance.Entries[index];
        if (string.IsNullOrEmpty(entry.Body))
        {
            UIManager.Instance?.ShowMessage(entry.Title);
            return;
        }
        UIManager.Instance?.ShowNote(entry.Title, entry.Body);
    }

    private void OnItemClicked(int index)
    {
        if (!open || InventoryManager.Instance == null) return;
        var items = InventoryManager.Instance.Items;
        if (index < 0 || index >= items.Count) return;
        UIManager.Instance?.ShowMessage(items[index].displayName, 2.5f);
    }

    private Sprite IconFor(string id)
    {
        if (string.IsNullOrEmpty(id)) return defaultItemIcon;
        if (id.Contains("llave")) return keyIcon != null ? keyIcon : defaultItemIcon;
        if (id.Contains("medallon") || id.Contains("pieza")) return medallionIcon != null ? medallionIcon : defaultItemIcon;
        if (id.Contains("barreta")) return crowbarIcon != null ? crowbarIcon : defaultItemIcon;
        if (id.Contains("bater")) return batteryIcon != null ? batteryIcon : defaultItemIcon;
        return defaultItemIcon;
    }

    private void CacheSlots(Transform root, List<SlotView> list, int max, bool clues)
    {
        list.Clear();
        if (root == null) return;
        for (int i = 0; i < root.childCount && list.Count < max; i++)
        {
            Transform child = root.GetChild(i);
            Image frame = child.GetComponent<Image>();
            if (frame != null) frame.raycastTarget = true;

            Button button = child.GetComponent<Button>();
            if (button == null) button = child.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.95f, 0.88f, 0.7f, 0.9f);
            colors.pressedColor = new Color(0.85f, 0.75f, 0.55f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.RemoveAllListeners();
            int captured = list.Count;
            if (clues) button.onClick.AddListener(() => OnClueClicked(captured));
            else button.onClick.AddListener(() => OnItemClicked(captured));

            var view = new SlotView
            {
                root = child.gameObject,
                icon = child.Find("Icono")?.GetComponent<Image>(),
                label = child.Find("Nombre")?.GetComponent<Text>(),
                frame = frame,
                button = button
            };
            list.Add(view);
            child.gameObject.SetActive(false);
        }
    }
}
