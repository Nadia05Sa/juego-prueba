using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Progreso persistente entre niveles (PlayerPrefs): último nivel alcanzado y objetos
/// que pasan de un nivel al siguiente. No guarda estado estático en memoria.
/// </summary>
public static class GameProgress
{
    public static readonly string[] LevelScenes = { "Nivel1_Aljibe", "Nivel2_Cripta", "Nivel3_Establos" };

    private const string LevelKey = "ecos_nivel_actual";
    private const string ItemsKey = "ecos_objetos";
    private const char ItemSeparator = '|';
    private const char FieldSeparator = '=';

    public static string FirstLevel => LevelScenes[0];
    public static bool HasSave => PlayerPrefs.HasKey(LevelKey);
    public static string SavedScene => PlayerPrefs.GetString(LevelKey, FirstLevel);

    public static bool IsLevel(string scene) => System.Array.IndexOf(LevelScenes, scene) >= 0;

    public static void NewGame()
    {
        PlayerPrefs.DeleteKey(LevelKey);
        PlayerPrefs.DeleteKey(ItemsKey);
        PlayerPrefs.Save();
    }

    public static void SaveLevelReached(string scene)
    {
        if (!IsLevel(scene)) return;
        PlayerPrefs.SetString(LevelKey, scene);
        PlayerPrefs.Save();
    }

    public static void CarryItem(string id, string displayName)
    {
        if (string.IsNullOrEmpty(id)) return;
        List<(string id, string name)> items = CarriedItems();
        if (items.Exists(i => i.id == id)) return;
        items.Add((id, displayName));

        var parts = new List<string>();
        foreach ((string itemId, string name) in items) parts.Add(itemId + FieldSeparator + name);
        PlayerPrefs.SetString(ItemsKey, string.Join(ItemSeparator.ToString(), parts));
        PlayerPrefs.Save();
    }

    public static List<(string id, string name)> CarriedItems()
    {
        var result = new List<(string, string)>();
        string raw = PlayerPrefs.GetString(ItemsKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return result;

        foreach (string entry in raw.Split(ItemSeparator))
        {
            int split = entry.IndexOf(FieldSeparator);
            if (split <= 0) continue;
            result.Add((entry.Substring(0, split), entry.Substring(split + 1)));
        }
        return result;
    }
}
