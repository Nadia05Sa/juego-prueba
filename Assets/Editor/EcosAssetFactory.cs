using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using Random = System.Random;

/// <summary>
/// Genera los recursos provisionales del prototipo: sprites, tiles, sonidos y notas.
/// Todo se puede sustituir después por arte y audio definitivos.
/// </summary>
public static class EcosAssetFactory
{
    public const string TilesFolder = "Assets/Art/Tiles";
    public const string ObjectsFolder = "Assets/Art/Objects";
    public const string CharactersFolder = "Assets/Art/Characters";
    public const string UISpritesFolder = "Assets/UI/Sprites";
    public const string SfxFolder = "Assets/Audio/SFX";
    public const string AmbienceFolder = "Assets/Audio/Ambience";
    public const string MusicFolder = "Assets/Audio/Music";
    public const string NotesFolder = "Assets/Data/Notes";

    public static Sprite Square, Circle, Heart, Cone, Drop, Moon, Flower, Glow, Sun, Spiral, Skull, Ring;
    public static Sprite SoftShadow, StoneRing, CrateTop, DryPlant, WoodPlank, Mattress, Pillow, AgedPaper;
    public static TileBase WallTile, ColumnTile, TreeTile, StoneTile, WoodTile, FieldTile;
    public static TileBase CryptWallTile, CryptFloorTile, TombTile, BenchTile, AltarTile;
    public static TileBase StableWallTile, StallTile, HayTile, DirtTile, CrateTile, BarnFloorTile;
    public static AudioClip Pickup, Clue, Error, Solve, Hurt, Door, Locked, PlatePress, Lament, Wind, Tension, MenuPad;
    public static AudioClip Footstep;
    public static AudioClip Chains, Lever, Click, StoneDoor, Hooves, HoovesLoop, Groan, Drip, StableAmbience;
    public static Sprite KeyIcon, MedallionIcon, CrowbarIcon, BatteryIcon, NoteIcon, BookPage;
    public static Sprite BookClosed, NoteSheet, PhotoProp;
    public static Sprite PhotoCatalinaFront, PhotoCatalinaBack;
    public static NoteData CartaOficina, LibroCuentas, NotaRota, Medallon;
    public static Sprite TombstoneFront;
    public static Sprite HorseSkeletonSprite, SaddleSprite, StallPostSprite, StallBeamSprite;
    public static Sprite[] HayPileSprites;
    public static NoteData DiarioSacristan, OracionCapellan, LapidaFundador, LapidaSinNombre, CartaSellada;
    public static NoteData RegistroHacienda, CartaPacto, NotaPalanca, BitacoraCaballerango;

    public static void CreateAll()
    {
        CreateSprites();
        CreateTiles();
        CreateAudio();
        CreateNotes();
        EcosAnimationFactory.CreateAll();
        AssetDatabase.SaveAssets();
    }

    // ---------------- Sprites ----------------

    private static void CreateSprites()
    {
        Square = MakeSprite(ObjectsFolder, "Cuadrado", 32, 32, 32f, new Vector2(0.5f, 0.5f), (u, v) => 1f, FilterMode.Point);
        Circle = MakeSprite(ObjectsFolder, "Circulo", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) => Disc(u, v, 0f, 0f, 1f));
        Glow = MakeSprite(ObjectsFolder, "Brillo", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            return Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
        });
        Heart = MakeSprite(UISpritesFolder, "Corazon", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float x = u * 1.25f;
            float y = v * 1.25f + 0.25f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f ? 1f : 0f;
        });
        Cone = MakeSprite(CharactersFolder, "ConoVision", 128, 256, 128f, new Vector2(0f, 0.5f), (u, v) =>
        {
            float px = (u + 1f) * 0.5f;
            float py = v;
            float d = Mathf.Sqrt(px * px + py * py);
            float angle = Mathf.Abs(Mathf.Atan2(py, px) * Mathf.Rad2Deg);
            if (d > 1f || angle > 50f) return 0f;
            return Mathf.Lerp(1f, 0.15f, d);
        });
        Drop = MakeSprite(ObjectsFolder, "SimboloGota", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float c = Disc(u, v, 0f, -0.3f, 0.55f);
            float tri = v > -0.3f && v < 0.9f && Mathf.Abs(u) < 0.55f * (0.9f - v) / 1.2f ? 1f : 0f;
            return Mathf.Max(c, tri);
        });
        Moon = MakeSprite(ObjectsFolder, "SimboloLuna", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
            Mathf.Clamp01(Disc(u, v, 0f, 0f, 0.8f) - Disc(u, v, 0.38f, 0.2f, 0.68f)));
        Flower = MakeSprite(ObjectsFolder, "SimboloFlor", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float a = Disc(u, v, 0f, 0f, 0.3f);
            for (int i = 0; i < 5; i++)
            {
                float ang = i * Mathf.PI * 2f / 5f + Mathf.PI / 2f;
                a = Mathf.Max(a, Disc(u, v, Mathf.Cos(ang) * 0.5f, Mathf.Sin(ang) * 0.5f, 0.38f));
            }
            return a;
        });
        Sun = MakeSprite(ObjectsFolder, "SimboloSol", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            float ang = Mathf.Atan2(v, u);
            float rays = d > 0.5f && d < 0.95f && Mathf.Cos(ang * 8f) > 0.55f ? 1f : 0f;
            return Mathf.Max(Disc(u, v, 0f, 0f, 0.42f), rays);
        });
        Spiral = MakeSprite(ObjectsFolder, "SimboloEspiral", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            if (d > 0.92f) return 0f;
            float ang = Mathf.Atan2(v, u) + Mathf.PI;
            float turns = d * 3f - ang / (2f * Mathf.PI);
            float frac = turns - Mathf.Floor(turns);
            return frac < 0.42f ? 1f : 0f;
        });
        Skull = MakeSprite(ObjectsFolder, "SimboloCalavera", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float head = Mathf.Max(Disc(u, v, 0f, 0.15f, 0.7f), v < -0.25f && v > -0.85f && Mathf.Abs(u) < 0.4f ? 1f : 0f);
            float holes = Mathf.Max(Disc(u, v, -0.28f, 0.1f, 0.2f), Disc(u, v, 0.28f, 0.1f, 0.2f));
            float nose = v < -0.1f && v > -0.3f && Mathf.Abs(u) < 0.08f ? 1f : 0f;
            float teeth = v < -0.55f && v > -0.85f && Mathf.Repeat((u + 0.4f) * 5f, 1f) < 0.25f ? 1f : 0f;
            return Mathf.Clamp01(head - holes - nose - teeth);
        });
        Ring = MakeSprite(ObjectsFolder, "Eslabon", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
            Mathf.Clamp01(Disc(u * 1.6f, v, 0f, 0f, 0.9f) - Disc(u * 1.6f, v, 0f, 0f, 0.55f)));

        // Iconos del inventario (sustituibles por arte definitivo).
        KeyIcon = MakeSprite(UISpritesFolder, "IconoLlave", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float head = Disc(u, v, -0.35f, 0.15f, 0.38f);
            float shaft = Mathf.Abs(v - 0.05f) < 0.1f && u > -0.35f && u < 0.55f ? 1f : 0f;
            float teeth = (u > 0.2f && u < 0.55f && v < 0.05f && v > -0.45f && Mathf.Repeat((u - 0.2f) * 4f, 1f) < 0.45f) ? 1f : 0f;
            return Mathf.Max(head, shaft, teeth);
        });
        MedallionIcon = MakeSprite(UISpritesFolder, "IconoMedallon", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float ring = Mathf.Clamp01(Disc(u, v, 0f, 0f, 0.85f) - Disc(u, v, 0f, 0f, 0.55f));
            float half = u < 0f ? Disc(u, v, 0f, 0f, 0.45f) : 0f;
            return Mathf.Max(ring, half);
        });
        CrowbarIcon = MakeSprite(UISpritesFolder, "IconoBarreta", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float bar = Mathf.Abs(u * 0.4f + v * 0.9f) < 0.12f && Mathf.Abs(u) < 0.85f ? 1f : 0f;
            float hook = Disc(u, v, 0.55f, -0.45f, 0.28f) * (u > 0.35f ? 1f : 0f);
            return Mathf.Max(bar, hook);
        });
        BatteryIcon = MakeSprite(UISpritesFolder, "IconoBateria", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float body = Mathf.Abs(u) < 0.45f && Mathf.Abs(v) < 0.7f ? 1f : 0f;
            float tip = Mathf.Abs(u) < 0.22f && v > 0.55f && v < 0.9f ? 1f : 0f;
            return Mathf.Max(body, tip);
        });
        NoteIcon = MakeSprite(UISpritesFolder, "IconoNota", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float paper = Mathf.Abs(u) < 0.7f && Mathf.Abs(v) < 0.85f ? 1f : 0f;
            float fold = u + v > 1.1f ? 0f : 1f;
            float lines = Mathf.Abs(v + 0.2f) < 0.04f || Mathf.Abs(v) < 0.04f || Mathf.Abs(v - 0.2f) < 0.04f ? 0.35f : 1f;
            return paper * fold * lines;
        });
        BookPage = MakeSprite(UISpritesFolder, "PaginaLibro", 128, 128, 128f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float edge = Mathf.Abs(u) > 0.92f || Mathf.Abs(v) > 0.92f ? 0.35f : 1f;
            float stain = 1f - 0.12f * Mathf.PerlinNoise((u + 1f) * 3f, (v + 1f) * 3f);
            return edge * stain;
        }, FilterMode.Bilinear);

        SoftShadow = MakeSprite(ObjectsFolder, "SombraSuave", 64, 64, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u * 0.7f + v * v);
            return Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
        });
        // Papel viejo para paneles de notas/cartas (manchas + bordes irregulares).
        AgedPaper = MakeSprite(UISpritesFolder, "PapelViejo", 256, 256, 128f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
            float ragged = 0.04f * Mathf.PerlinNoise((u + 3f) * 12f, (v + 2f) * 12f);
            if (ax > 0.96f - ragged || ay > 0.96f - ragged) return 0f;
            float stain = Mathf.PerlinNoise((u + 1f) * 2.5f, (v + 1f) * 2.5f);
            float blot = Mathf.PerlinNoise((u + 4f) * 6f, (v + 5f) * 6f);
            float edgeDark = (ax > 0.85f || ay > 0.85f) ? 0.82f : 1f;
            float a = 0.88f + 0.12f * stain - (blot > 0.78f ? 0.22f : 0f);
            return Mathf.Clamp01(a * edgeDark);
        }, FilterMode.Bilinear);
        StoneRing = MakeSprite(ObjectsFolder, "AnilloPiedra", 96, 96, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u + v * v);
            if (d > 0.98f || d < 0.55f) return 0f;
            float grain = 0.85f + 0.15f * Mathf.PerlinNoise((u + 1f) * 4f, (v + 1f) * 4f);
            float edge = d > 0.9f || d < 0.62f ? 1f : 0.75f;
            return grain * edge;
        }, FilterMode.Bilinear);
        CrateTop = MakeSprite(ObjectsFolder, "CajaTapa", 48, 48, 48f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
            if (ax > 0.9f || ay > 0.9f) return 0f;
            if (ax > 0.78f || ay > 0.78f) return 1f;
            if (Mathf.Abs(u) < 0.06f || Mathf.Abs(v) < 0.06f) return 0.55f;
            return 0.8f;
        }, FilterMode.Point);
        DryPlant = MakeSprite(ObjectsFolder, "PlantaSeca", 48, 48, 48f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float pot = Mathf.Abs(u) < 0.35f && v < -0.15f && v > -0.85f ? 1f : 0f;
            float bush = 0f;
            for (int i = 0; i < 5; i++)
            {
                float ang = i * 1.1f;
                bush = Mathf.Max(bush, Disc(u, v, Mathf.Cos(ang) * 0.35f, 0.15f + Mathf.Sin(ang) * 0.2f, 0.32f));
            }
            return Mathf.Max(pot, bush * (v > -0.2f ? 1f : 0f));
        }, FilterMode.Point);
        WoodPlank = MakeSprite(ObjectsFolder, "TablaMadera", 64, 32, 32f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            if (Mathf.Abs(u) > 0.95f || Mathf.Abs(v) > 0.9f) return 0f;
            float grain = 0.75f + 0.25f * Mathf.PerlinNoise((u + 1f) * 6f, (v + 1f) * 2f);
            float edge = Mathf.Abs(u) > 0.85f || Mathf.Abs(v) > 0.75f ? 1f : grain;
            return edge;
        }, FilterMode.Point);
        Mattress = MakeSprite(ObjectsFolder, "Colchon", 64, 48, 32f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            if (Mathf.Abs(u) > 0.92f || Mathf.Abs(v) > 0.88f) return 0f;
            float stain = Mathf.PerlinNoise((u + 2f) * 3f, (v + 2f) * 3f);
            if (stain > 0.72f && u > 0.1f && v < 0f) return 0.45f;
            return 0.9f;
        }, FilterMode.Point);
        Pillow = MakeSprite(ObjectsFolder, "Almohada", 48, 28, 32f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float d = Mathf.Sqrt(u * u * 0.6f + v * v);
            return d < 0.9f ? 1f - d * 0.15f : 0f;
        }, FilterMode.Bilinear);

        // Prop pequeño para la mesa (la foto completa solo se ve al examinar).
        PhotoProp = MakeSprite(ObjectsFolder, "FotoProp", 32, 40, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
            if (ax > 0.92f || ay > 0.92f) return 0f;
            if (ax > 0.78f || ay > 0.82f) return 1f; // marco
            return 0.85f; // papel sepia (se tiñe en el renderer)
        }, FilterMode.Point);

        BookClosed = MakeSprite(ObjectsFolder, "LibroCerrado", 32, 40, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            // Libro visto desde arriba: rectángulo con lomo a la izquierda.
            if (Mathf.Abs(u) > 0.75f || Mathf.Abs(v) > 0.9f) return 0f;
            if (u < -0.55f) return 1f; // lomo
            if (Mathf.Abs(u) > 0.65f || Mathf.Abs(v) > 0.8f) return 0.95f; // borde
            if (Mathf.Abs(v) < 0.02f) return 0.55f; // hendidura
            return 0.8f;
        }, FilterMode.Point);

        NoteSheet = MakeSprite(ObjectsFolder, "HojaNota", 32, 32, 64f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            if (Mathf.Abs(u) > 0.85f || Mathf.Abs(v) > 0.7f) return 0f;
            if (u + v > 1.35f) return 0f; // esquina doblada
            if (Mathf.Abs(v + 0.15f) < 0.03f || Mathf.Abs(v - 0.1f) < 0.03f) return 0.45f;
            return 1f;
        }, FilterMode.Point);

        TombstoneFront = MakeColorSprite(ObjectsFolder, "LapidaPiedra", 96, 128, 48f, DrawTombstone);

        HorseSkeletonSprite = MakeColorSprite(ObjectsFolder, "CaballoEsqueleto", 96, 64, 48f, (u, v) => DrawHorseSkeleton(u, v));
        SaddleSprite = MakeColorSprite(ObjectsFolder, "SillaMontar", 64, 48, 48f, (u, v) => DrawSaddle(u, v));
        StallPostSprite = MakeColorSprite(ObjectsFolder, "PosteEstablo", 16, 64, 32f, (u, v) => DrawWoodPost(u, v));
        StallBeamSprite = MakeColorSprite(ObjectsFolder, "VigaEstablo", 64, 16, 32f, (u, v) => DrawWoodBeam(u, v));
        HayPileSprites = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            int seed = 11 + i * 17;
            HayPileSprites[i] = MakeColorSprite(ObjectsFolder, "PajaMonton" + (i + 1), 48, 32, 32f,
                (u, v) => DrawHayPile(u, v, seed));
        }

        PhotoCatalinaFront = ImportSprite($"{ObjectsFolder}/FotoCatalina_frente.jpg", 256f, FilterMode.Bilinear);
        PhotoCatalinaBack = ImportSprite($"{ObjectsFolder}/FotoCatalina_reverso.jpg", 256f, FilterMode.Bilinear);
    }

    /// <summary>
    /// Lápida pixel art: piedra envejecida, arco irregular, base ancha, cruz y placa raspada.
    /// El texto largo se lee en el panel de nota, no sobre este sprite.
    /// </summary>
    private static Color DrawTombstone(float u, float v)
    {
        // Contorno irregular (desgaste en bordes).
        float chip = Mathf.PerlinNoise(u * 14f + 3.2f, v * 11f + 1.7f) * 0.07f;
        float halfW = 0.58f + (v < -0.55f ? 0.12f : 0f) - chip; // base más ancha
        float archCy = 0.38f;
        float archR = 0.58f + chip * 0.5f;
        bool inArch = v > 0.28f && Disc(u, v, 0f, archCy, archR) > 0.45f;
        bool inBody = Mathf.Abs(u) < halfW && v > -0.92f && v <= 0.38f;
        if (!inArch && !inBody) return Color.clear;
        // Esquinas inferiores rotas.
        if (v < -0.78f && Mathf.Abs(u) > halfW - 0.12f && Disc(u, v, Mathf.Sign(u) * (halfW - 0.08f), -0.78f, 0.14f) < 0.3f)
            return Color.clear;

        float grain = Mathf.PerlinNoise(u * 9f + 0.4f, v * 12f + 2.1f);
        float shade = Mathf.PerlinNoise(u * 3.5f + 8f, v * 2.8f);
        Color dark = new Color(0.28f, 0.26f, 0.23f, 1f);
        Color mid = new Color(0.48f, 0.45f, 0.4f, 1f);
        Color lit = new Color(0.68f, 0.64f, 0.56f, 1f);
        Color stone = Color.Lerp(dark, mid, grain);
        stone = Color.Lerp(stone, lit, Mathf.Clamp01(0.55f - u * 0.25f + v * 0.2f) * 0.45f * shade);

        float edgeDist = inArch
            ? Mathf.Abs(Mathf.Sqrt(u * u + (v - archCy) * (v - archCy)) - archR)
            : Mathf.Min(Mathf.Abs(Mathf.Abs(u) - halfW), Mathf.Abs(v + 0.92f));
        if (edgeDist < 0.05f)
            stone = Color.Lerp(stone, dark, 0.65f);

        bool crossStem = Mathf.Abs(u) < 0.05f && v > 0.12f && v < 0.52f;
        bool crossBar = Mathf.Abs(v - 0.38f) < 0.05f && Mathf.Abs(u) < 0.2f;
        if (crossStem || crossBar)
        {
            bool highlight = (u < -0.01f) || (v > 0.4f && crossBar);
            stone = highlight
                ? new Color(0.55f, 0.5f, 0.42f, 1f)
                : new Color(0.18f, 0.15f, 0.12f, 1f);
        }

        float[] lines = { 0.08f, -0.02f, -0.12f, -0.22f, -0.32f };
        for (int i = 0; i < lines.Length; i++)
        {
            float ly = lines[i];
            float lw = i == 1 || i == 2 ? 0.38f : 0.32f;
            if (Mathf.Abs(v - ly) < 0.018f && Mathf.Abs(u) < lw)
                stone = Color.Lerp(stone, new Color(0.16f, 0.13f, 0.1f, 1f), 0.75f);
            else if (Mathf.Abs(v - (ly + 0.012f)) < 0.01f && Mathf.Abs(u) < lw)
                stone = Color.Lerp(stone, lit, 0.25f);
        }

        if (Mathf.Abs(v - 0.16f) < 0.012f && Mathf.Abs(u) < 0.35f)
            stone = Color.Lerp(stone, dark, 0.7f);
        if (Mathf.Abs(v + 0.42f) < 0.012f && Mathf.Abs(u) < 0.35f)
            stone = Color.Lerp(stone, dark, 0.7f);
        if (Disc(u, v, 0f, 0.16f, 0.035f) > 0.5f || Disc(u, v, 0f, -0.42f, 0.035f) > 0.5f)
            stone = dark;

        if (Mathf.Abs(u) < 0.36f && v > -0.72f && v < -0.48f)
        {
            stone = Color.Lerp(stone, new Color(0.38f, 0.36f, 0.32f, 1f), 0.55f);
            if (Disc(u, v, -0.3f, -0.5f, 0.03f) > 0.5f || Disc(u, v, 0.3f, -0.5f, 0.03f) > 0.5f ||
                Disc(u, v, -0.3f, -0.68f, 0.03f) > 0.5f || Disc(u, v, 0.3f, -0.68f, 0.03f) > 0.5f)
                stone = dark;
            for (int i = 0; i < 3; i++)
            {
                float ly = -0.54f - i * 0.06f;
                if (Mathf.Abs(v - ly) < 0.012f && Mathf.Abs(u) < 0.26f)
                    stone = new Color(0.14f, 0.12f, 0.1f, 1f);
            }
        }

        float c1 = Mathf.Abs((v - 0.2f) - u * 1.4f);
        float c2 = Mathf.Abs((v + 0.55f) + u * 0.9f);
        if ((c1 < 0.02f && u > 0.1f && v > -0.1f && v < 0.45f) ||
            (c2 < 0.018f && u < -0.05f && v > -0.75f && v < -0.2f))
            stone = Color.Lerp(stone, new Color(0.12f, 0.1f, 0.08f, 1f), 0.85f);

        if (v < -0.62f)
        {
            float moss = Mathf.PerlinNoise(u * 16f, v * 20f + 4f);
            if (moss > 0.42f)
                stone = Color.Lerp(stone, new Color(0.22f, 0.32f, 0.18f, 1f), 0.55f + moss * 0.2f);
        }

        return stone;
    }
    private static Color DrawHorseSkeleton(float u, float v)
    {
        Color clear = Color.clear;
        Color bone = new Color(0.82f, 0.77f, 0.64f, 1f);       // #D2C5A2
        Color boneDark = new Color(0.55f, 0.48f, 0.38f, 1f);
        Color boneLight = new Color(0.9f, 0.86f, 0.75f, 1f);
        // Sombra suelo
        if (Disc(u, v, 0.05f, -0.55f, 0.85f) > 0.5f && v < -0.35f)
            return new Color(0.05f, 0.04f, 0.03f, 0.35f);

        // Cráneo (hocico a la izquierda)
        if (Disc(u, v, -0.62f, 0.12f, 0.22f) > 0.5f) return boneLight;
        if (Disc(u, v, -0.78f, 0.08f, 0.14f) > 0.5f) return bone; // hocico
        if (Disc(u, v, -0.68f, 0.18f, 0.05f) > 0.5f) return new Color(0.08f, 0.06f, 0.05f, 1f); // ojo
        if (Mathf.Abs(v + 0.02f) < 0.04f && u > -0.85f && u < -0.55f) return boneDark; // mandíbula

        // Columna + costillas
        if (Mathf.Abs(v - 0.05f) < 0.08f && u > -0.45f && u < 0.55f) return boneDark;
        for (int i = 0; i < 6; i++)
        {
            float cx = -0.35f + i * 0.15f;
            float dy = Mathf.Abs(v - 0.05f);
            float dx = Mathf.Abs(u - cx);
            if (dx < 0.04f && dy < 0.32f) return i % 2 == 0 ? bone : boneDark;
        }

        // Pelvis
        if (Disc(u, v, 0.55f, 0.05f, 0.16f) > 0.5f) return bone;

        // Cuatro patas
        float[] legsX = { -0.25f, -0.05f, 0.25f, 0.45f };
        float[] legsAng = { 0.08f, -0.05f, 0.1f, -0.08f };
        for (int i = 0; i < 4; i++)
        {
            float lx = legsX[i] + (v + 0.1f) * legsAng[i];
            if (Mathf.Abs(u - lx) < 0.045f && v < 0.05f && v > -0.7f)
                return v < -0.45f ? boneDark : bone;
        }

        // Cola ósea
        if (u > 0.65f && u < 0.95f && Mathf.Abs(v - 0.12f - (u - 0.65f) * 0.3f) < 0.05f) return boneDark;
        return clear;
    }

    private static Color DrawSaddle(float u, float v)
    {
        Color leather = new Color(0.46f, 0.31f, 0.22f, 1f); // #765039
        Color leatherDark = new Color(0.28f, 0.18f, 0.12f, 1f);
        Color leatherLight = new Color(0.58f, 0.42f, 0.3f, 1f);
        Color blanket = new Color(0.35f, 0.12f, 0.1f, 1f);
        Color silver = new Color(0.75f, 0.75f, 0.8f, 1f);
        if (Disc(u, v, 0f, -0.15f, 0.95f) > 0.5f && v < -0.35f)
            return new Color(0.05f, 0.04f, 0.03f, 0.3f);
        // Manta
        if (Mathf.Abs(u) < 0.85f && v > -0.55f && v < 0.15f) return blanket;
        // Asiento curvado
        if (Disc(u, v, 0f, 0.05f, 0.55f) > 0.5f && v > -0.35f) return leather;
        // Borrén delantero / trasero
        if (Disc(u, v, 0f, 0.35f, 0.22f) > 0.5f) return leatherLight;
        if (Disc(u, v, 0f, -0.25f, 0.2f) > 0.5f) return leatherDark;
        // Estribos
        if (Mathf.Abs(Mathf.Abs(u) - 0.7f) < 0.08f && v > -0.75f && v < -0.15f) return silver;
        if (Mathf.Abs(u) > 0.55f && Mathf.Abs(u) < 0.72f && Mathf.Abs(v + 0.05f) < 0.06f) return leatherDark;
        // Costura / brillo
        if (Mathf.Abs(v - 0.05f) < 0.03f && Mathf.Abs(u) < 0.4f) return leatherLight;
        return Color.clear;
    }

    private static Color DrawWoodPost(float u, float v)
    {
        Color wood = new Color(0.29f, 0.2f, 0.14f, 1f); // #493324
        Color woodLight = new Color(0.4f, 0.28f, 0.18f, 1f);
        Color woodDark = new Color(0.18f, 0.12f, 0.08f, 1f);
        if (Mathf.Abs(u) > 0.7f) return Color.clear;
        float grain = Mathf.Sin(v * 18f + u * 4f) * 0.5f + 0.5f;
        if (Mathf.Abs(u) > 0.45f) return woodDark;
        return Color.Lerp(wood, woodLight, grain * 0.35f);
    }

    private static Color DrawWoodBeam(float u, float v)
    {
        Color wood = new Color(0.32f, 0.22f, 0.15f, 1f);
        Color woodDark = new Color(0.2f, 0.13f, 0.09f, 1f);
        if (Mathf.Abs(v) > 0.65f) return Color.clear;
        float plank = Mathf.Floor((u + 1f) * 4f);
        float edge = Mathf.Abs(((u + 1f) * 4f) - plank - 0.5f);
        if (edge > 0.42f) return woodDark;
        return Color.Lerp(wood, woodDark, Mathf.Abs(v) * 0.4f);
    }

    private static Color DrawHayPile(float u, float v, int seed)
    {
        var rng = new Random(seed + (int)((u + 2f) * 40) + (int)((v + 2f) * 70));
        Color straw = new Color(0.7f, 0.54f, 0.24f, 1f); // #B28A3E
        Color strawLight = new Color(0.82f, 0.68f, 0.35f, 1f);
        Color strawDark = new Color(0.45f, 0.32f, 0.12f, 1f);
        // Forma irregular de montón
        float mound = Disc(u, v, 0.05f, -0.15f, 0.9f) * (0.7f + 0.3f * Disc(u, v, -0.2f, 0.1f, 0.55f));
        if (mound < 0.35f) return Color.clear;
        // Fibras
        float fiber = Mathf.Abs(Mathf.Sin(u * 22f + v * 9f + seed) * Mathf.Cos(v * 18f - u * 6f));
        if (fiber > 0.72f && mound > 0.5f) return strawLight;
        if (v < -0.25f) return strawDark;
        return rng.NextDouble() < 0.12 ? strawDark : straw;
    }

    private static Sprite MakeColorSprite(string folder, string name, int width, int height, float ppu,
        Func<float, float, Color> paint)
    {
        string path = $"{folder}/{name}.png";
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float u = (x + 0.5f) / width * 2f - 1f;
            float v = (y + 0.5f) / height * 2f - 1f;
            pixels[y * width + x] = paint(u, v);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>Importa un PNG/JPG existente como Sprite sin regenerarlo.</summary>
    private static Sprite ImportSprite(string path, float ppu, FilterMode filter)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Ecos del Mictlán: no se encontró la imagen '{path}'.");
            return null;
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static float Disc(float u, float v, float cx, float cy, float r)
    {
        float d = Mathf.Sqrt((u - cx) * (u - cx) + (v - cy) * (v - cy));
        return Mathf.Clamp01((r - d) * 32f + 0.5f);
    }

    /// <summary>La función recibe u y v en el rango -1..1 y devuelve el alfa del píxel.</summary>
    private static Sprite MakeSprite(string folder, string name, int width, int height, float ppu, Vector2 pivot,
        Func<float, float, float> alpha, FilterMode filter = FilterMode.Bilinear)
    {
        string path = $"{folder}/{name}.png";
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width * 2f - 1f;
                float v = (y + 0.5f) / height * 2f - 1f;
                pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha(u, v)));
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = filter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------------- Tiles ----------------

    private static void CreateTiles()
    {
        WallTile = MakeTile("Muro_Adobe", new Color(0.5f, 0.36f, 0.28f), Tile.ColliderType.Grid, 1.1f);
        ColumnTile = MakeTile("Columna", new Color(0.62f, 0.57f, 0.5f), Tile.ColliderType.Grid, 0.6f);
        TreeTile = MakeTile("Arbol", new Color(0.1f, 0.17f, 0.09f), Tile.ColliderType.Grid, 0.8f);
        StoneTile = MakeTile("Piso_Piedra", new Color(0.34f, 0.32f, 0.3f), Tile.ColliderType.None, 1.4f);
        WoodTile = MakeTile("Piso_Madera", new Color(0.36f, 0.24f, 0.16f), Tile.ColliderType.None, 1.8f);
        FieldTile = MakeTile("Piso_Campo", new Color(0.22f, 0.26f, 0.14f), Tile.ColliderType.None, 1.6f);

        CryptWallTile = MakeTile("Muro_Cripta", new Color(0.3f, 0.3f, 0.34f), Tile.ColliderType.Grid);
        CryptFloorTile = MakeTile("Piso_Cripta", new Color(0.17f, 0.17f, 0.2f), Tile.ColliderType.None);
        TombTile = MakeTile("Tumba", new Color(0.48f, 0.47f, 0.45f), Tile.ColliderType.Grid);
        BenchTile = MakeTile("Banca", new Color(0.34f, 0.22f, 0.12f), Tile.ColliderType.Grid);
        AltarTile = MakeTile("Altar", new Color(0.62f, 0.58f, 0.5f), Tile.ColliderType.Grid);

        StableWallTile = MakeTile("Muro_Establo", new Color(0.36f, 0.25f, 0.16f), Tile.ColliderType.Grid);
        StallTile = MakeTile("Tabique_Caballeriza", new Color(0.28f, 0.19f, 0.11f), Tile.ColliderType.Grid);
        HayTile = MakeTile("Piso_Paja", new Color(0.33f, 0.28f, 0.14f), Tile.ColliderType.None);
        DirtTile = MakeTile("Piso_Tierra", new Color(0.24f, 0.2f, 0.15f), Tile.ColliderType.None);
        CrateTile = MakeTile("Cajas", new Color(0.42f, 0.3f, 0.17f), Tile.ColliderType.Grid);
        BarnFloorTile = MakeTile("Piso_Troje", new Color(0.26f, 0.2f, 0.14f), Tile.ColliderType.None);
    }

    private static Tile MakeTile(string name, Color color, Tile.ColliderType collider, float grain = 1f)
    {
        string path = $"{TilesFolder}/{name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        bool isNew = tile == null;
        if (isNew) tile = ScriptableObject.CreateInstance<Tile>();
        // Sin borde oscuro: evita la cuadrícula visible entre tiles.
        Sprite spr = MakeSprite(TilesFolder, name + "_tex", 32, 32, 32f, new Vector2(0.5f, 0.5f), (u, v) =>
        {
            float n = Mathf.PerlinNoise((u + 2.3f) * 3.2f * grain, (v + 1.7f) * 3.2f * grain);
            float n2 = Mathf.PerlinNoise((u + 5f) * 7f, (v + 3f) * 7f);
            return Mathf.Clamp01(0.82f + 0.18f * n - 0.06f * n2);
        }, FilterMode.Bilinear);
        tile.sprite = spr != null ? spr : Square;
        tile.color = color;
        tile.colliderType = collider;
        if (isNew) AssetDatabase.CreateAsset(tile, path);
        else EditorUtility.SetDirty(tile);
        return tile;
    }

    // ---------------- Audio ----------------

    private static void CreateAudio()
    {
        var rng = new Random(13);
        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
        float Sin(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);

        Pickup = MakeWav(SfxFolder, "sfx_recoger", 0.3f, t => (t < 0.1f ? Sin(880f, t) : Sin(1320f, t)) * Mathf.Exp(-t * 10f) * 0.5f);

        Clue = MakeWav(SfxFolder, "sfx_pista", 0.6f, t =>
        {
            float f = t < 0.15f ? 660f : t < 0.3f ? 880f : 1320f;
            return Sin(f, t) * Mathf.Exp(-(t % 0.15f) * 8f) * 0.35f * (1f - t / 0.6f);
        });

        PlatePress = MakeWav(SfxFolder, "sfx_placa", 0.25f, t => Sin(220f, t) * Mathf.Exp(-t * 18f) * 0.6f + Noise() * Mathf.Exp(-t * 40f) * 0.2f);

        Error = MakeWav(SfxFolder, "sfx_error", 0.7f, t => (Sin(70f, t) + 0.5f * Sin(105f, t)) * Mathf.Exp(-t * 5f) * 0.6f);

        Solve = MakeWav(SfxFolder, "sfx_mecanismo", 1.3f, t =>
        {
            float f = 300f + 380f * t;
            return (Sin(f, t) + 0.3f * Sin(f * 2.01f, t)) * (1f - t / 1.3f) * 0.4f + Noise() * Mathf.Exp(-t * 6f) * 0.08f;
        });

        Hurt = MakeWav(SfxFolder, "sfx_dano", 0.35f, t => Noise() * Mathf.Exp(-t * 12f) * 0.5f + Sin(180f, t) * Mathf.Exp(-t * 8f) * 0.4f);

        float doorPhase = 0f;
        Door = MakeWav(SfxFolder, "sfx_puerta", 0.8f, t =>
        {
            float f = 90f + 30f * Sin(3f, t);
            doorPhase += f / 22050f;
            float saw = 2f * (doorPhase % 1f) - 1f;
            return saw * Mathf.Sin(Mathf.PI * t / 0.8f) * (0.6f + 0.4f * Sin(11f, t)) * 0.22f;
        });

        Locked = MakeWav(SfxFolder, "sfx_cerrado", 0.25f, t =>
            Noise() * (Mathf.Exp(-t * 60f) + (t > 0.12f ? Mathf.Exp(-(t - 0.12f) * 60f) : 0f)) * 0.5f);

        Footstep = MakeWav(SfxFolder, "sfx_paso", 0.12f, t =>
        {
            float thump = Mathf.Exp(-t * 55f) * (0.35f + 0.2f * Noise());
            float grit = Noise() * Mathf.Exp(-t * 40f) * 0.25f;
            return thump + grit;
        });

        float lamentPhase = 0f;
        Lament = MakeWav(AmbienceFolder, "amb_lamento", 5f, t =>
        {
            if (t > 3.6f) return 0f;
            float f = 520f - 140f * (t / 3.6f) + 14f * Sin(5.5f, t);
            lamentPhase += f / 22050f;
            float p = lamentPhase * 2f * Mathf.PI;
            float voice = Mathf.Sin(p) + 0.35f * Mathf.Sin(2f * p) + 0.15f * Mathf.Sin(3f * p);
            float env = Mathf.Min(1f, t / 0.4f) * Mathf.Min(1f, (3.6f - t) / 1f);
            return (voice * 0.3f + Noise() * 0.03f) * env;
        });

        float windState = 0f;
        Wind = MakeWav(AmbienceFolder, "amb_viento", 8f, t =>
        {
            // Viento suave, sin el ruido áspero anterior.
            float k = 0.008f + 0.012f * (0.5f + 0.5f * Sin(0.12f, t));
            windState += k * (Noise() - windState);
            float swell = 0.55f + 0.45f * Sin(0.08f, t);
            return windState * swell * 1.15f;
        });

        // Drone bajo y lejano: solo sube de volumen al perseguir (sin batidos fuertes).
        Tension = MakeWav(MusicFolder, "mus_tension", 10f, t =>
        {
            float pad = Sin(46f, t) * 0.22f + Sin(69f, t) * 0.1f + Sin(92f, t) * 0.05f;
            float breath = 0.65f + 0.35f * Sin(0.07f, t);
            return pad * breath * 0.45f;
        });

        float menuNoise = 0f;
        MenuPad = MakeWav(MusicFolder, "mus_menu", 12f, t =>
        {
            menuNoise += 0.004f * (Noise() - menuNoise);
            float pad = Sin(110f, t) * 0.08f + Sin(165f, t) * 0.05f + Sin(220f, t) * 0.03f;
            float swell = 0.7f + 0.3f * Sin(0.05f, t);
            return (pad + menuNoise * 0.35f) * swell * 0.55f;
        });

        Chains = MakeWav(SfxFolder, "sfx_cadenas", 1.2f, t =>
        {
            float s = 0f;
            for (int k = 0; k < 6; k++)
            {
                float start = k * 0.17f + (k % 2) * 0.05f;
                if (t < start) continue;
                float local = t - start;
                s += (Sin(2400f + k * 310f, t) + 0.6f * Sin(3700f + k * 170f, t)) * Mathf.Exp(-local * 22f);
            }
            return s * 0.16f + Noise() * Mathf.Exp(-t * 4f) * 0.04f;
        });

        Lever = MakeWav(SfxFolder, "sfx_palanca", 0.9f, t =>
            Sin(60f, t) * Mathf.Exp(-t * 4f) * 0.6f + Noise() * (t < 0.5f ? 0.15f : Mathf.Exp(-(t - 0.5f) * 30f) * 0.6f));

        Click = MakeWav(SfxFolder, "sfx_clic", 0.08f, t => Noise() * Mathf.Exp(-t * 120f) * 0.6f + Sin(1800f, t) * Mathf.Exp(-t * 90f) * 0.3f);

        float stoneState = 0f;
        StoneDoor = MakeWav(SfxFolder, "sfx_puerta_piedra", 1.8f, t =>
        {
            stoneState += 0.08f * (Noise() - stoneState);
            float env = Mathf.Min(1f, t / 0.2f) * Mathf.Clamp01((1.8f - t) / 0.4f);
            return (stoneState * 3f + Sin(42f, t) * 0.5f) * env * 0.6f;
        });

        float Hoof(float local) => local < 0f ? 0f : (Sin(110f, local) * 0.8f + Noise() * 0.6f) * Mathf.Exp(-local * 35f);
        Hooves = MakeWav(SfxFolder, "sfx_cascos", 1f, t => (Hoof(t) + Hoof(t - 0.12f) + Hoof(t - 0.5f) + Hoof(t - 0.62f)) * 0.5f);
        HoovesLoop = MakeWav(AmbienceFolder, "amb_cascos", 1.6f, t =>
        {
            float local = t % 0.8f;
            return (Hoof(local) + Hoof(local - 0.13f) + Hoof(local - 0.31f) + Hoof(local - 0.42f)) * 0.45f;
        });

        float groanPhase = 0f;
        Groan = MakeWav(AmbienceFolder, "amb_gemido", 4f, t =>
        {
            if (t > 2.6f) return 0f;
            float f = 85f + 12f * Sin(1.3f, t) + 4f * Noise();
            groanPhase += f / 22050f;
            float p = groanPhase * 2f * Mathf.PI;
            float voice = Mathf.Sin(p) + 0.5f * Mathf.Sin(2f * p) + 0.3f * Mathf.Sin(3f * p);
            float env = Mathf.Min(1f, t / 0.5f) * Mathf.Clamp01((2.6f - t) / 0.8f);
            return (voice * 0.25f + Noise() * 0.06f) * env;
        });

        float dripWind = 0f;
        Drip = MakeWav(AmbienceFolder, "amb_goteo", 8f, t =>
        {
            dripWind += 0.006f * (Noise() - dripWind);
            float s = dripWind * 0.7f;
            foreach (float start in new[] { 0.8f, 2.4f, 4.1f, 6.2f })
            {
                float local = t - start;
                if (local < 0f || local > 0.3f) continue;
                s += Sin(900f - local * 1600f, t) * Mathf.Exp(-local * 22f) * 0.18f;
            }
            return s;
        });

        float barnWind = 0f;
        StableAmbience = MakeWav(AmbienceFolder, "amb_establo", 10f, t =>
        {
            barnWind += 0.007f * (Noise() - barnWind);
            float creak = t > 5.2f && t < 6.1f
                ? Sin(140f + 40f * Sin(2.5f, t), t) * Mathf.Sin(Mathf.PI * (t - 5.2f) / 0.9f) * 0.06f
                : 0f;
            return barnWind * 1.1f + creak;
        });
    }

    private static AudioClip MakeWav(string folder, string name, float seconds, Func<float, float> sample, int rate = 22050)
    {
        string path = $"{folder}/{name}.wav";
        int count = Mathf.CeilToInt(seconds * rate);

        using (var stream = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(stream))
        {
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + count * 2);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(count * 2);
            for (int i = 0; i < count; i++)
            {
                float s = Mathf.Clamp(sample(i / (float)rate), -1f, 1f);
                w.Write((short)(s * 32767f));
            }
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    // ---------------- Notas ----------------

    private static void CreateNotes()
    {
        CartaOficina = MakeNote("carta_oficina", "Carta del administrador", true,
            "Don Aurelio:\n\n" +
            "Los peones ya no quieren acercarse al aljibe. Dicen que de noche se oye a una mujer llorar junto al agua " +
            "y que la han visto cruzar el corredor del norte.\n\n" +
            "Guardé la llave de la reja del campo en el cajón de este escritorio. Busca en el escritorio.\n\n" +
            "La vieja Remedios dice cómo calmarla en el aljibe. Primer paso:\n" +
            "«PRIMERO, el agua que se la llevó».\n\n" +
            "El resto de los pasos está escrito en otras partes de la casa.\n\n" +
            "— El administrador, noviembre de 1911");

        LibroCuentas = MakeNote("libro_cuentas", "Libro de cuentas", false,
            "Cosecha de 1911. Jornales pagados: 34.\n\n" +
            "Al margen, con otra letra:\n" +
            "«Faltan tres peones desde el Día de Muertos. Sus familias dicen que los llamó una voz desde el aljibe. No volvieron.»");

        NotaRota = MakeNote("nota_rota", "Nota rota", true,
            "...no fue el agua lo que la mató, fue la pena.\n\n" +
            "Cada noche vuelve a buscar a sus hijos. Remedios dice el segundo paso del aljibe:\n" +
            "«LUEGO, la luna que la vio llorar»...\n\n" +
            "(El resto del papel está quemado. Busca la tercera pista cerca del aljibe.)");

        Medallon = MakeNote("medallon", "Medallón de plata", true,
            "Entre las flores encuentras un medallón ennegrecido. Dentro hay un retrato diminuto de dos niños " +
            "y una inscripción raspada:\n\n" +
            "«...y AL FINAL, la flor que guía a los muertos de regreso a casa. Así descansará.»\n\n" +
            "Orden del aljibe: agua, luego luna, luego flor.");

        // ---------- Nivel 2: la cripta ----------
        DiarioSacristan = MakeNote("diario_sacristan", "Diario del sacristán", true,
            "3 de noviembre de 1911.\n\n" +
            "Don Ignacio mandó sellar su cámara con cuatro símbolos: sol, luna, espiral y calavera.\n\n" +
            "Me hizo repetir:\n" +
            "«El SOL abre la cámara y la CALAVERA la cierra.»\n\n" +
            "Lo de en medio lo dejó escrito el capellán (mira la otra hoja de esta mesa).\n\n" +
            "En la cripta: camina, no corras. Las cadenas hacen ruido y algo oye todo.");

        OracionCapellan = MakeNote("oracion_capellan", "Oración del capellán", true,
            "(Hoja del capellán de la hacienda — texto ficticio.)\n\n" +
            "«...y la LUNA se levanta siempre ANTES que la ESPIRAL del tiempo, para que quien duerme no despierte.»\n\n" +
            "Orden completo de los símbolos:\n" +
            "1) SOL   2) LUNA   3) ESPIRAL   4) CALAVERA\n\n" +
            "Al margen: «Que nadie haga ruido en el pasillo de las criptas.»");

        LapidaFundador = MakeNote("lapida_fundador", "Lápida del fundador", false,
            "AQUÍ DESCANSA\n\n" +
            "<b>DON IGNACIO\nDE LA VEGA</b>\n" +
            "Fundador de esta hacienda\n" +
            "1841 – 1898\n\n" +
            "--------------------\n\n" +
            "Debajo, raspado:\n" +
            "<color=#3A2A1A>Hizo un trato que no fue suyo.\n" +
            "Que no despierte.</color>");

        LapidaSinNombre = MakeNote("lapida_sin_nombre", "Lápida sin nombre", false,
            "<b>AQUÍ TERMINA\nEL CAMINO</b>\n\n" +
            "--------------------\n\n" +
            "Solo una calavera grabada.\n" +
            "Nadie dejó nombre ni fecha.");

        CartaSellada = MakeNote("carta_sellada", "Carta sellada", false,
            "La carta tiene el sello de la hacienda roto.\n\n" +
            "«Si alguien lee esto, sepa que mi padre no levantó esta hacienda con trabajo. " +
            "Hizo un acuerdo con un jinete vestido de negro que llegó una noche por el camino de los establos. " +
            "Partió el medallón en dos: una mitad quedó con la familia y la otra aquí, con él.\n\n" +
            "El resto del acuerdo lo guardó en los establos. Si quiere salir de aquí, tendrá que cruzarlos.»\n\n" +
            "— Aurelio de la Vega\n\n" +
            "Siguiente: sube por la escalera al fondo de esta cámara.");

        // ---------- Nivel 3: los establos ----------
        RegistroHacienda = MakeNote("registro_hacienda", "Registro de la hacienda", true,
            "Registro de peones, 1898 – 1911.\n\n" +
            "Cada año falta alguien después del Día de Muertos: «Se fue por el camino de los establos».\n\n" +
            "Para romper el acuerdo ficticio del jinete necesitas tres documentos:\n\n" +
            "1) Este registro (ya lo tienes).\n" +
            "2) La carta del pacto — en la paja de una caballeriza al NORTE. Solo se ve con la linterna (F).\n" +
            "3) La nota del caballerango — en los TROJES (pasillo entre caballerizas y estos cuartos).\n\n" +
            "También aquí: abre el ARCHIVERO para la llave del cuarto de herramientas (puerta vecina, más al sur).\n" +
            "La PALANCA del portón está al fondo OESTE de las caballerizas; hace falta la barreta.");

        CartaPacto = MakeNote("carta_pacto", "Carta del acuerdo", true,
            "(Documento ficticio, tinta casi negra.)\n\n" +
            "«Yo, Ignacio de la Vega, acepto que el jinete cuide de mis tierras y mis cosechas. " +
            "A cambio, cada año uno de los míos tomará el camino de los establos. " +
            "Si el medallón vuelve a unirse, el acuerdo se rompe y el jinete vendrá a cobrar lo que falte.»\n\n" +
            "Las dos mitades del medallón encajan en tu mano.\n\n" +
            "Siguiente: busca la nota en los trojes y la barreta (archivero, luego cuarto de herramientas).");

        NotaPalanca = MakeNote("nota_palanca", "Nota del caballerango", true,
            "«El portón principal (al ESTE del patio) no tiene cerradura.\n" +
            "Lo abre la PALANCA al fondo OESTE de las caballerizas.\n\n" +
            "El mecanismo está oxidado: sin una BARRETA no se mueve.\n" +
            "La barreta está en el cuarto de herramientas (llave en el archivero del cuarto de registros).\n\n" +
            "Cuando la palanca suena, se oye en toda la hacienda. Él también la oye.\n" +
            "Usa F (linterna) si te persigue; Q para esconderte.»");

        BitacoraCaballerango = MakeNote("bitacora_caballerango", "Bitácora del caballerango", false,
            "«Los caballos no duermen. Por la noche se oyen cascos en el patio.\n\n" +
            "Anoche lo vi junto al bebedero: sombrero ancho, traje negro, botones de plata. " +
            "Le alumbré la cara con el farol y se quedó quieto un momento, como si la luz le pesara. " +
            "Pero volvió a caminar.\n\n" +
            "Consejo: la linterna (F) lo frena poco tiempo. No pelees: corre o escóndete (Q).»");
    }

    private static NoteData MakeNote(string id, string title, bool isClue, string body)
    {
        string path = $"{NotesFolder}/{id}.asset";
        var note = AssetDatabase.LoadAssetAtPath<NoteData>(path);
        if (note == null)
        {
            note = ScriptableObject.CreateInstance<NoteData>();
            AssetDatabase.CreateAsset(note, path);
        }
        EcosBuildUtils.Set(note, "id", id);
        EcosBuildUtils.Set(note, "title", title);
        EcosBuildUtils.Set(note, "body", EcosRichText.Format(body));
        EcosBuildUtils.Set(note, "isClue", isClue);
        EditorUtility.SetDirty(note);
        return note;
    }
}
