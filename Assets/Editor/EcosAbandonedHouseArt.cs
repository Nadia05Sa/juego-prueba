using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Arte de Abandoned House Builder (YvLong) usado solo en el Nivel 1 (oficina / habitaciones).
/// Recorta muebles del sheet a PNGs sueltos; no se aplica en patio, campo ni otros niveles.
/// </summary>
public static class EcosAbandonedHouseArt
{
    public const string Folder = "Assets/Art/Imported/AbandonedHouse";
    public const string SheetPath = Folder + "/RundownHouse.png";

    public static Sprite Desk, Chair, Dresser, Bookshelf, Wardrobe, Nightstand;
    public static bool Ready;

    // Rects en espacio de textura Unity (origen abajo-izquierda). Sheet 550×500.
    private static readonly (string file, int x, int y, int w, int h)[] Slices =
    {
        ("AH_Desk", 64, 426, 31, 26),
        ("AH_Chair", 144, 389, 22, 31),
        ("AH_Dresser", 5, 427, 41, 26),
        ("AH_Bookshelf", 176, 443, 18, 41),
        ("AH_Wardrobe", 80, 165, 19, 47),
        ("AH_Nightstand", 16, 70, 25, 30)
    };

    public static void Import()
    {
        Ready = false;
        Desk = Chair = Dresser = Bookshelf = Wardrobe = Nightstand = null;

        if (!File.Exists(SheetPath))
        {
            Debug.LogWarning("Ecos del Mictlán: falta Abandoned House en " + SheetPath);
            return;
        }

        EnsureReadableSheet();
        var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
        if (sheet == null)
        {
            Debug.LogWarning("Ecos del Mictlán: no se pudo cargar " + SheetPath);
            return;
        }

        foreach (var s in Slices)
        {
            string path = $"{Folder}/{s.file}.png";
            CropAndSave(sheet, s.x, s.y, s.w, s.h, path);
            Sprite spr = ImportSingle(path);
            switch (s.file)
            {
                case "AH_Desk": Desk = spr; break;
                case "AH_Chair": Chair = spr; break;
                case "AH_Dresser": Dresser = spr; break;
                case "AH_Bookshelf": Bookshelf = spr; break;
                case "AH_Wardrobe": Wardrobe = spr; break;
                case "AH_Nightstand": Nightstand = spr; break;
            }
        }

        Ready = Desk != null && Chair != null && Dresser != null && Bookshelf != null && Wardrobe != null;
        if (!Ready)
            Debug.LogWarning("Ecos del Mictlán: no se pudieron recortar todos los sprites de Abandoned House.");
    }

    private static void EnsureReadableSheet()
    {
        AssetDatabase.ImportAsset(SheetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(SheetPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static void CropAndSave(Texture2D sheet, int x, int y, int w, int h, string path)
    {
        var pixels = sheet.GetPixels(x, y, w, h);
        var crop = new Texture2D(w, h, TextureFormat.RGBA32, false);
        crop.filterMode = FilterMode.Point;
        crop.SetPixels(pixels);
        crop.Apply();
        File.WriteAllBytes(path, crop.EncodeToPNG());
        Object.DestroyImmediate(crop);
    }

    private static Sprite ImportSingle(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
