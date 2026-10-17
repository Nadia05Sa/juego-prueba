using UnityEngine;
using UnityEngine.Rendering.Universal;
using static EcosBuildUtils;
using static EcosLevelKit;
using Object = UnityEngine.Object;

/// <summary>
/// Muebles y props cenitales con sombra, capas y volumen (vista top-down).
/// Sustituye los bloques Solid planos del prototipo.
/// </summary>
public static class EcosFurniture
{
    private static readonly Color ShadowColor = new Color(0.02f, 0.02f, 0.04f, 0.4f);
    private static readonly Color WoodDark = new Color(0.28f, 0.16f, 0.08f);
    private static readonly Color WoodMid = new Color(0.4f, 0.24f, 0.12f);
    private static readonly Color WoodLight = new Color(0.52f, 0.34f, 0.18f);
    private static readonly Color Dust = new Color(0.55f, 0.5f, 0.4f, 0.35f);
    private static readonly Vector2 ShadowBias = new Vector2(0.12f, -0.14f);

    public static void AddShadow(Transform parent, Vector2 size, int order = -26)
    {
        Sprite sprite = EcosAssetFactory.SoftShadow != null ? EcosAssetFactory.SoftShadow : EcosAssetFactory.Glow;
        Visual(parent, sprite, ShadowColor, size + new Vector2(0.25f, 0.2f), order, "Sombra", ShadowBias);
    }

    // ---------- Interior ----------

    /// <summary>Mueble a partir de un sprite externo (p. ej. Abandoned House en Nivel 1).</summary>
    public static GameObject FromSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size,
        bool solid = true)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        if (sprite != null)
            Visual(go.transform, sprite, Color.white, size, -20, "Sprite");
        else
            Visual(go.transform, EcosAssetFactory.Square, WoodMid, size, -20, "Sprite");
        if (solid)
            Box(go, size, false);
        return go;
    }

    public static GameObject Bed(Transform parent, string name, Vector2 position, Vector2 size,
        bool examineUnder = false, string examineText = null, string flag = null)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);

        Sprite wood = EcosAssetFactory.WoodPlank != null ? EcosAssetFactory.WoodPlank : EcosAssetFactory.Square;
        Sprite mattress = EcosAssetFactory.Mattress != null ? EcosAssetFactory.Mattress : EcosAssetFactory.Square;
        Sprite pillow = EcosAssetFactory.Pillow != null ? EcosAssetFactory.Pillow : EcosAssetFactory.Circle;

        Visual(go.transform, wood, WoodDark, size, -20, "Marco");
        Visual(go.transform, mattress, new Color(0.42f, 0.14f, 0.14f), size * new Vector2(0.88f, 0.78f), -19, "Colchon", new Vector2(0f, 0.02f));
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.25f, 0.08f, 0.08f, 0.55f),
            size * new Vector2(0.28f, 0.18f), -18, "Mancha", new Vector2(size.x * 0.18f, -size.y * 0.12f));
        Visual(go.transform, pillow, new Color(0.78f, 0.72f, 0.62f),
            new Vector2(size.x * 0.55f, size.y * 0.2f), -17, "Almohada", new Vector2(0f, size.y * 0.28f));

        SpriteRenderer sheet = Visual(go.transform, EcosAssetFactory.Square, new Color(0.7f, 0.68f, 0.6f, 0.85f),
            new Vector2(size.x * 0.75f, size.y * 0.32f), -16, "Sabana", new Vector2(0f, -size.y * 0.08f));
        var floatAnim = go.AddComponent<SpriteFloat>();
        Set(floatAnim, "target", sheet.transform);
        Set(floatAnim, "height", 0.018f);
        Set(floatAnim, "speed", 1.1f);
        Set(floatAnim, "spinDegrees", 1.5f);

        // Pata dañada (detalle)
        Visual(go.transform, EcosAssetFactory.Square, WoodDark * 0.7f,
            new Vector2(0.18f, 0.18f), -15, "PataRota", new Vector2(-size.x * 0.42f, -size.y * 0.42f));

        Box(go, size, false);

        if (examineUnder)
        {
            Examinable(go,
                "Revisar debajo de la cama",
                examineText ?? "Debajo de la cama solo hay polvo, una media rota y el rastro de algo que se arrastró hace mucho.",
                size + new Vector2(0.8f, 0.8f),
                flag);
        }

        return go;
    }

    public static GameObject Desk(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        Sprite wood = EcosAssetFactory.WoodPlank != null ? EcosAssetFactory.WoodPlank : EcosAssetFactory.Square;
        Visual(go.transform, wood, WoodMid, size, -20, "Tablero");
        Visual(go.transform, EcosAssetFactory.Square, Dust, size * new Vector2(0.9f, 0.75f), -19, "Polvo");
        // Borde / grosor sugerido
        Visual(go.transform, EcosAssetFactory.Square, WoodDark,
            new Vector2(size.x * 0.98f, size.y * 0.12f), -18, "Canto", new Vector2(0f, -size.y * 0.42f));
        Box(go, size, false);
        return go;
    }

    public static GameObject Chair(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size * 1.1f);
        Visual(go.transform, EcosAssetFactory.Square, WoodDark, size, -20, "Asiento");
        Visual(go.transform, EcosAssetFactory.Square, WoodMid,
            new Vector2(size.x * 0.85f, size.y * 0.35f), -19, "Respaldo", new Vector2(0f, size.y * 0.35f));
        Box(go, size, false);
        return go;
    }

    public static GameObject Bookshelf(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        Visual(go.transform, EcosAssetFactory.Square, WoodDark, size, -20, "Cuerpo");
        // Estantes
        float[] rows = { 0.28f, 0f, -0.28f };
        foreach (float y in rows)
        {
            Visual(go.transform, EcosAssetFactory.Square, WoodLight,
                new Vector2(size.x * 0.85f, size.y * 0.06f), -19, "Estante", new Vector2(0f, size.y * y));
        }
        // Libros / huecos
        Visual(go.transform, EcosAssetFactory.BookClosed ?? EcosAssetFactory.Square, new Color(0.45f, 0.2f, 0.1f),
            new Vector2(size.x * 0.35f, size.y * 0.18f), -18, "Libros", new Vector2(-size.x * 0.12f, size.y * 0.28f), 8f);
        Visual(go.transform, EcosAssetFactory.BookClosed ?? EcosAssetFactory.Square, new Color(0.2f, 0.25f, 0.35f),
            new Vector2(size.x * 0.28f, size.y * 0.16f), -18, "Libros2", new Vector2(size.x * 0.15f, -size.y * 0.02f), -6f);
        // Hueco vacío
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.12f, 0.08f, 0.05f),
            new Vector2(size.x * 0.3f, size.y * 0.14f), -18, "Hueco", new Vector2(0f, -size.y * 0.28f));
        Box(go, size, false);
        return go;
    }

    public static GameObject FilingCabinet(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.28f, 0.28f, 0.3f), size, -20, "Cuerpo");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.2f, 0.2f, 0.22f),
            new Vector2(size.x * 0.85f, size.y * 0.08f), -19, "Cajon1", new Vector2(0f, size.y * 0.2f));
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.22f, 0.22f, 0.24f),
            new Vector2(size.x * 0.85f, size.y * 0.08f), -19, "Cajon2", new Vector2(0f, -size.y * 0.15f));
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.55f, 0.45f, 0.25f),
            new Vector2(0.12f, 0.12f), -18, "Tirador", new Vector2(size.x * 0.25f, size.y * 0.2f));
        Box(go, size, false);
        return go;
    }

    public static GameObject Crib(Transform parent, string name, Vector2 position, Vector2 size, string examineText)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        Visual(go.transform, EcosAssetFactory.WoodPlank ?? EcosAssetFactory.Square, WoodLight, size, -20, "Marco");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.55f, 0.5f, 0.4f),
            size * new Vector2(0.7f, 0.55f), -19, "Interior");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.65f, 0.62f, 0.55f, 0.7f),
            size * new Vector2(0.5f, 0.25f), -18, "Tela", new Vector2(0f, -size.y * 0.05f));
        Box(go, size, false);
        Examinable(go, "Examinar cuna", examineText, size + new Vector2(1f, 1f));
        return go;
    }

    public static GameObject Wardrobe(Transform parent, string name, string spotName, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        Visual(go.transform, EcosAssetFactory.Square, WoodDark, size, -20, "Cuerpo");
        // Puerta izquierda
        Visual(go.transform, EcosAssetFactory.Square, WoodMid,
            new Vector2(size.x * 0.42f, size.y * 0.85f), -19, "PuertaIzq", new Vector2(-size.x * 0.22f, 0f));
        // Puerta derecha entreabierta (rotado)
        SpriteRenderer door = Visual(go.transform, EcosAssetFactory.Square, WoodLight,
            new Vector2(size.x * 0.38f, size.y * 0.85f), -18, "PuertaDer", new Vector2(size.x * 0.18f, 0.05f), -18f);
        Visual(door.transform, EcosAssetFactory.Circle, new Color(0.45f, 0.35f, 0.15f),
            new Vector2(0.1f, 0.1f), -17, "Bisagra", new Vector2(-0.35f, 0.2f));
        // Interior oscuro visible
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.08f, 0.05f, 0.04f),
            new Vector2(size.x * 0.2f, size.y * 0.6f), -21, "Interior", new Vector2(size.x * 0.05f, 0f));

        Box(go, size + new Vector2(0.3f, 0.3f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    // ---------- Exterior / pasillos ----------

    public static GameObject Fountain(Transform parent, Vector2 position)
    {
        GameObject go = GO("Fuente", parent, position);
        AddShadow(go.transform, new Vector2(2.9f, 2.9f));
        Sprite ring = EcosAssetFactory.StoneRing != null ? EcosAssetFactory.StoneRing : EcosAssetFactory.Circle;
        // Base ancha + anillo + interior seco + detalles.
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.38f, 0.35f, 0.32f), new Vector2(2.85f, 2.85f), -21, "Base");
        Visual(go.transform, ring, new Color(0.62f, 0.58f, 0.52f), new Vector2(2.55f, 2.55f), -20, "Borde");
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.1f, 0.08f, 0.06f), new Vector2(1.65f, 1.65f), -19, "Interior");
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.22f, 0.18f, 0.12f, 0.55f),
            new Vector2(0.9f, 0.7f), -18, "Lodo", new Vector2(0.15f, -0.2f));
        Visual(go.transform, EcosAssetFactory.DryPlant ?? EcosAssetFactory.Circle, new Color(0.45f, 0.32f, 0.12f, 0.8f),
            new Vector2(0.55f, 0.45f), -17, "Hojas", new Vector2(0.3f, -0.1f), 25f);
        Visual(go.transform, EcosAssetFactory.NoteSheet ?? EcosAssetFactory.Square, new Color(0.4f, 0.32f, 0.15f, 0.65f),
            new Vector2(0.4f, 0.22f), -17, "Hojas2", new Vector2(-0.35f, 0.25f), -35f);
        // Inscripción sugerida en el borde.
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.15f, 0.12f, 0.1f, 0.7f),
            new Vector2(0.55f, 0.12f), -16, "Marca", new Vector2(0f, 0.95f));
        go.AddComponent<CircleCollider2D>().radius = 1.2f;
        Examinable(go, "Examinar la fuente",
            "Una fuente seca, llena de hojas muertas. En el borde alguien escribió con carbón: No te acerques al aljibe de noche.",
            new Vector2(3.6f, 3.6f));
        return go;
    }

    public class WellParts
    {
        public GameObject Root;
        public SpriteRenderer Water;
        public Light2D PuzzleLight;
    }

    public static WellParts Well(Transform parent, Vector2 position)
    {
        GameObject go = GO("Aljibe", parent, position);
        AddShadow(go.transform, new Vector2(3.6f, 3.6f));
        Sprite ring = EcosAssetFactory.StoneRing != null ? EcosAssetFactory.StoneRing : EcosAssetFactory.Circle;
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.35f, 0.35f, 0.36f), new Vector2(3.55f, 3.55f), -21, "Base");
        Visual(go.transform, ring, new Color(0.55f, 0.54f, 0.52f), new Vector2(3.35f, 3.35f), -20, "Brocal");
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.28f, 0.38f, 0.2f, 0.6f),
            new Vector2(0.85f, 0.55f), -19, "Musgo", new Vector2(-1f, 0.7f));
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.4f, 0.38f, 0.35f, 0.5f),
            new Vector2(0.4f, 0.25f), -19, "PiedraSuelta", new Vector2(1.1f, -0.5f), 20f);
        SpriteRenderer water = Visual(go.transform, EcosAssetFactory.Circle, new Color(0.03f, 0.05f, 0.07f),
            new Vector2(2.1f, 2.1f), -18, "Agua");
        Visual(go.transform, EcosAssetFactory.Glow, new Color(0.15f, 0.25f, 0.3f, 0.25f),
            new Vector2(1.6f, 1.6f), -17, "Reflejo");
        go.AddComponent<CircleCollider2D>().radius = 1.55f;

        Light2D puzzleLight = MakeLight(go.transform, "LuzAgua", Light2D.LightType.Point, new Color(0.4f, 0.8f, 1f), 1.2f, 4f);
        puzzleLight.enabled = false;

        Examinable(go, "Examinar el aljibe",
            "Un aljibe de piedra. En el brocal hay tres símbolos grabados: una gota, una luna y una flor. Del fondo sube un olor a agua estancada.",
            new Vector2(4.4f, 4.4f));

        return new WellParts { Root = go, Water = water, PuzzleLight = puzzleLight };
    }

    public static GameObject CrateStack(Transform parent, string name, string spotName, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size * 1.15f);
        Sprite crate = EcosAssetFactory.CrateTop != null ? EcosAssetFactory.CrateTop : EcosAssetFactory.Square;
        Visual(go.transform, crate, new Color(0.42f, 0.28f, 0.14f), size * 0.95f, -20, "CajaBaja", new Vector2(-0.08f, -0.08f));
        Visual(go.transform, crate, new Color(0.48f, 0.32f, 0.16f), size * 0.85f, -19, "CajaAlta", new Vector2(0.1f, 0.12f), 8f);
        Box(go, size + new Vector2(0.35f, 0.35f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    public static GameObject HayBale(Transform parent, string name, string spotName, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size * 1.25f);
        Sprite hay = PickHaySprite(name.GetHashCode());
        Visual(go.transform, hay, Color.white, size * 1.15f, -20, "Paca");
        Visual(go.transform, hay, new Color(0.9f, 0.85f, 0.7f, 0.9f), size * 0.75f, -19, "Paca2",
            new Vector2(0.12f, 0.08f), 12f);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.35f, 0.28f, 0.1f),
            new Vector2(size.x * 0.95f, size.y * 0.12f), -17, "Cuerda", new Vector2(0f, 0.05f));
        Box(go, size + new Vector2(0.35f, 0.35f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    public static GameObject HayPile(Transform parent, string name, Vector2 position, float scale = 1f, int variant = 0)
    {
        GameObject go = GO(name, parent, position);
        Sprite hay = PickHaySprite(variant);
        AddShadow(go.transform, new Vector2(1.4f, 0.9f) * scale);
        Visual(go.transform, hay, Color.white, new Vector2(1.35f, 0.9f) * scale, -50, "Monton");
        return go;
    }

    private static Sprite PickHaySprite(int seed)
    {
        var piles = EcosAssetFactory.HayPileSprites;
        if (piles == null || piles.Length == 0)
            return EcosAssetFactory.Circle;
        int i = Mathf.Abs(seed) % piles.Length;
        return piles[i] != null ? piles[i] : EcosAssetFactory.Circle;
    }

    public static GameObject StallBay(Transform parent, string name, Vector2 position, float width = 4.5f, float depth = 3.2f)
    {
        GameObject go = GO(name, parent, position);
        Sprite post = EcosAssetFactory.StallPostSprite != null ? EcosAssetFactory.StallPostSprite : EcosAssetFactory.Square;
        Sprite beam = EcosAssetFactory.StallBeamSprite != null ? EcosAssetFactory.StallBeamSprite : EcosAssetFactory.WoodPlank;
        Color wood = new Color(0.29f, 0.2f, 0.14f);
        // Postes
        Visual(go.transform, post, Color.white, new Vector2(0.35f, depth * 0.95f), -48, "PosteIzq", new Vector2(-width * 0.45f, 0f));
        Visual(go.transform, post, Color.white, new Vector2(0.35f, depth * 0.95f), -48, "PosteDer", new Vector2(width * 0.45f, 0f));
        // Vigas
        Visual(go.transform, beam, Color.white, new Vector2(width * 0.95f, 0.28f), -47, "VigaNorte", new Vector2(0f, depth * 0.4f));
        Visual(go.transform, beam, Color.white, new Vector2(width * 0.7f, 0.22f), -47, "VigaSur", new Vector2(0f, -depth * 0.35f));
        // Tablones laterales (sombra de compartimento)
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.12f, 0.08f, 0.05f, 0.45f),
            new Vector2(width * 0.85f, depth * 0.55f), -52, "SombraInterior", new Vector2(0f, 0.1f));
        Visual(go.transform, EcosAssetFactory.Square, wood,
            new Vector2(0.12f, depth * 0.5f), -46, "Tabla", new Vector2(-width * 0.42f, 0.15f));
        Visual(go.transform, EcosAssetFactory.Square, wood,
            new Vector2(0.12f, depth * 0.5f), -46, "Tabla2", new Vector2(width * 0.42f, 0.15f));
        return go;
    }

    public static GameObject Tombstone(Transform parent, string name, Vector2 position, string worldText, string fullText)
    {
        GameObject go = GO(name, parent, position);
        Vector2 size = new Vector2(1.05f, 1.35f);
        AddShadow(go.transform, new Vector2(size.x * 1.25f, size.y * 0.45f));
        Sprite stone = EcosAssetFactory.TombstoneFront != null ? EcosAssetFactory.TombstoneFront : EcosAssetFactory.Square;
        Visual(go.transform, stone, Color.white, size, -20, "Piedra");
        // Inscripción breve en el mundo (el texto completo va al panel de lectura).
        if (!string.IsNullOrEmpty(worldText))
        {
            var textGo = new GameObject("TextoMundo");
            textGo.transform.SetParent(go.transform, false);
            textGo.transform.localPosition = new Vector3(0f, -0.05f, 0f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = worldText;
            tm.font = UIFont;
            tm.fontSize = 32;
            tm.characterSize = 0.038f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.14f, 0.11f, 0.08f, 0.92f);
            tm.richText = false;
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = -15;
                if (UIFont != null && UIFont.material != null)
                    mr.sharedMaterial = UIFont.material;
            }
        }
        return go;
    }

    public static GameObject Pew(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size * 1.1f);
        Sprite wood = EcosAssetFactory.WoodPlank != null ? EcosAssetFactory.WoodPlank : EcosAssetFactory.Square;
        Visual(go.transform, wood, WoodDark, size, -22, "Banca");
        Visual(go.transform, wood, WoodMid, new Vector2(size.x * 0.96f, size.y * 0.35f), -21, "Asiento", new Vector2(0f, size.y * 0.05f));
        Visual(go.transform, EcosAssetFactory.Square, WoodLight,
            new Vector2(size.x * 0.98f, size.y * 0.18f), -20, "Respaldo", new Vector2(0f, size.y * 0.38f));
        // Reclinatorio
        Visual(go.transform, EcosAssetFactory.Square, WoodDark * 0.85f,
            new Vector2(size.x * 0.9f, size.y * 0.2f), -19, "Reclinatorio", new Vector2(0f, -size.y * 0.35f));
        // Remates laterales
        Visual(go.transform, EcosAssetFactory.Square, WoodMid,
            new Vector2(0.12f, size.y * 0.9f), -18, "BrazoIzq", new Vector2(-size.x * 0.48f, 0f));
        Visual(go.transform, EcosAssetFactory.Square, WoodMid,
            new Vector2(0.12f, size.y * 0.9f), -18, "BrazoDer", new Vector2(size.x * 0.48f, 0f));
        Box(go, size, false);
        return go;
    }

    public static void DecorateAltar(Transform parent, Vector2 center)
    {
        GameObject altar = GO("Altar", parent, center);
        AddShadow(altar.transform, new Vector2(5.2f, 1.8f));
        Visual(altar.transform, EcosAssetFactory.Square, new Color(0.55f, 0.52f, 0.45f), new Vector2(5f, 1.4f), -24, "Mesa");
        Visual(altar.transform, EcosAssetFactory.Square, new Color(0.42f, 0.4f, 0.36f), new Vector2(5.2f, 0.25f), -23, "Borde", new Vector2(0f, -0.55f));
        Visual(altar.transform, EcosAssetFactory.Square, new Color(0.7f, 0.68f, 0.6f, 0.5f), new Vector2(1.2f, 0.8f), -22, "Mantel", new Vector2(0f, 0.1f));
        // Cruz
        Visual(altar.transform, EcosAssetFactory.Square, new Color(0.75f, 0.7f, 0.45f), new Vector2(0.12f, 0.7f), -10, "CruzV", new Vector2(0f, 0.55f));
        Visual(altar.transform, EcosAssetFactory.Square, new Color(0.75f, 0.7f, 0.45f), new Vector2(0.45f, 0.12f), -10, "CruzH", new Vector2(0f, 0.7f));
        Examinable(altar, "Examinar el altar",
            "Un altar de cantera cubierto de polvo y cera derretida. Alguien dejó flores secas de cempasúchil y una veladora apagada.",
            new Vector2(6.5f, 2.6f));

        // Cempasúchil
        Color marigold = new Color(1f, 0.55f, 0.1f);
        Color marigoldDark = new Color(0.85f, 0.35f, 0.05f);
        Vector2[] flowerPos =
        {
            center + new Vector2(-1.8f, 0.35f), center + new Vector2(-1.2f, 0.55f),
            center + new Vector2(1.2f, 0.5f), center + new Vector2(1.9f, 0.3f),
            center + new Vector2(-0.6f, -0.7f), center + new Vector2(0.7f, -0.65f)
        };
        for (int i = 0; i < flowerPos.Length; i++)
        {
            Sprite flower = EcosAssetFactory.Flower != null ? EcosAssetFactory.Flower : EcosAssetFactory.Circle;
            Decor(parent, "Cempasuchil", flower, i % 2 == 0 ? marigold : marigoldDark, flowerPos[i],
                new Vector2(0.45f, 0.45f), -12, i * 25f);
        }

        Candle(parent, center + new Vector2(-2.2f, -0.2f));
        Candle(parent, center + new Vector2(2.2f, -0.2f));
        Candle(parent, center + new Vector2(-0.9f, 0.55f));
        Candle(parent, center + new Vector2(0.9f, 0.55f));
    }

    public static Lever Winch(Transform parent, string name, Vector2 position, string prompt)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, new Vector2(1.8f, 1.8f));
        // Pedestal
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.28f, 0.26f, 0.24f), new Vector2(1.4f, 0.7f), -15, "Base");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.4f, 0.38f, 0.35f), new Vector2(1.1f, 0.35f), -14, "Plataforma", new Vector2(0f, 0.25f));
        // Rueda grande
        GameObject wheel = GO("Rueda", go.transform);
        wheel.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        Visual(wheel.transform, EcosAssetFactory.Ring ?? EcosAssetFactory.Circle, new Color(0.55f, 0.45f, 0.28f), new Vector2(1.35f, 1.35f), -10, "Aro");
        Visual(wheel.transform, EcosAssetFactory.Circle, new Color(0.35f, 0.3f, 0.22f), new Vector2(0.35f, 0.35f), -9, "Cubo");
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 30f;
            Visual(wheel.transform, EcosAssetFactory.Square, new Color(0.5f, 0.4f, 0.25f),
                new Vector2(0.1f, 1.1f), -11, "Radio", Vector2.zero, ang);
        }
        // Manivela destacada
        Visual(wheel.transform, EcosAssetFactory.Square, new Color(0.75f, 0.55f, 0.2f),
            new Vector2(0.18f, 0.55f), -8, "Manivela", new Vector2(0.55f, 0.15f), -25f);
        Visual(wheel.transform, EcosAssetFactory.Circle, new Color(0.85f, 0.7f, 0.3f),
            new Vector2(0.22f, 0.22f), -7, "Empuñadura", new Vector2(0.7f, 0.35f));
        // Cadenas
        for (int k = 0; k < 3; k++)
            Visual(go.transform, EcosAssetFactory.Ring ?? EcosAssetFactory.Circle, new Color(0.5f, 0.5f, 0.55f),
                new Vector2(0.2f, 0.28f), -6, "Cadena", new Vector2(-0.55f + k * 0.12f, -0.45f - k * 0.15f));
        // Brillo para que se note
        Visual(go.transform, EcosAssetFactory.Glow, new Color(1f, 0.75f, 0.3f, 0.35f), new Vector2(2f, 2f), -16, "Resalte");

        TriggerChild(go.transform, new Vector2(2.4f, 2.4f));
        var lever = go.AddComponent<Lever>();
        Set(lever, "prompt", prompt);
        Set(lever, "handle", wheel.transform);
        Set(lever, "handleAngle", -120f);
        return lever;
    }

    public static GameObject Stairs(Transform parent, string name, Vector2 position)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, new Vector2(2.6f, 2.2f));
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            Color c = Color.Lerp(new Color(0.32f, 0.3f, 0.28f), new Color(0.5f, 0.48f, 0.42f), t);
            Visual(go.transform, EcosAssetFactory.Square, c,
                new Vector2(2.2f - i * 0.15f, 0.38f), -50 + i, "Escalon", new Vector2(0f, 0.85f - i * 0.38f));
        }
        // Barandales
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.4f, 0.28f, 0.14f),
            new Vector2(0.12f, 2f), -40, "BarandalIzq", new Vector2(-1.15f, 0.1f));
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.4f, 0.28f, 0.14f),
            new Vector2(0.12f, 2f), -40, "BarandalDer", new Vector2(1.15f, 0.1f));
        Visual(go.transform, EcosAssetFactory.Glow, new Color(1f, 0.8f, 0.4f, 0.25f),
            new Vector2(2.4f, 2.4f), -55, "LuzSalida");
        return go;
    }

    public static GameObject Saddle(Transform parent, string name, Vector2 position)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, new Vector2(1.8f, 1.3f));
        // Soporte / viga
        Visual(go.transform, EcosAssetFactory.StallBeamSprite != null ? EcosAssetFactory.StallBeamSprite : EcosAssetFactory.Square,
            Color.white, new Vector2(1.6f, 0.25f), -22, "Soporte", new Vector2(0f, -0.45f));
        Sprite saddle = EcosAssetFactory.SaddleSprite != null ? EcosAssetFactory.SaddleSprite : EcosAssetFactory.Square;
        Visual(go.transform, saddle, Color.white, new Vector2(1.55f, 1.15f), -18, "Silla");
        Visual(go.transform, EcosAssetFactory.Glow, new Color(1f, 0.85f, 0.55f, 0.28f),
            new Vector2(1.2f, 1f), -17, "Brillo");
        Box(go, new Vector2(1.5f, 1.1f), false);
        Examinable(go, "Examinar la silla de montar",
            "Una silla de montar de cuero oscuro con adornos de plata. Está tibia, como si alguien acabara de bajarse de ella.",
            new Vector2(2.4f, 2.4f));
        return go;
    }

    public static GameObject HorseSkeleton(Transform parent, string name, Vector2 position)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, new Vector2(2.8f, 1.6f));
        Sprite horse = EcosAssetFactory.HorseSkeletonSprite != null ? EcosAssetFactory.HorseSkeletonSprite : EcosAssetFactory.Square;
        Visual(go.transform, horse, Color.white, new Vector2(2.6f, 1.7f), -20, "Esqueleto");
        Examinable(go, "Examinar los restos",
            "El esqueleto de un caballo. Cráneo, costillas y patas aún encajan. El suelo alrededor está removido.",
            new Vector2(3.2f, 2.4f));
        return go;
    }

    public static GameObject Cart(Transform parent, string name, string spotName, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size * 1.15f);
        Visual(go.transform, EcosAssetFactory.WoodPlank ?? EcosAssetFactory.Square, WoodMid, size, -20, "Caja");
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.2f, 0.18f, 0.16f),
            new Vector2(size.y * 0.55f, size.y * 0.55f), -19, "RuedaIzq", new Vector2(-size.x * 0.35f, -size.y * 0.15f));
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.2f, 0.18f, 0.16f),
            new Vector2(size.y * 0.55f, size.y * 0.55f), -19, "RuedaDer", new Vector2(size.x * 0.35f, -size.y * 0.15f));
        Visual(go.transform, EcosAssetFactory.Square, WoodDark,
            new Vector2(size.x * 0.15f, size.y * 0.7f), -18, "Timon", new Vector2(-size.x * 0.55f, 0.1f));
        Box(go, size + new Vector2(0.4f, 0.4f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    public static GameObject DryWell(Transform parent, string name, Vector2 position, string examineText, float scale = 1f)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, new Vector2(2.6f, 2.6f) * scale);
        Sprite ring = EcosAssetFactory.StoneRing != null ? EcosAssetFactory.StoneRing : EcosAssetFactory.Circle;
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.4f, 0.38f, 0.35f), new Vector2(2.5f, 2.5f) * scale, -21, "Base");
        Visual(go.transform, ring, new Color(0.55f, 0.52f, 0.48f), new Vector2(2.3f, 2.3f) * scale, -20, "Brocal");
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.05f, 0.05f, 0.06f), new Vector2(1.4f, 1.4f) * scale, -19, "Fondo");
        Visual(go.transform, EcosAssetFactory.Glow, new Color(0.7f, 0.75f, 0.85f, 0.25f),
            new Vector2(0.35f, 0.35f) * scale, -18, "BrilloMetal", new Vector2(0.15f, -0.1f));
        go.AddComponent<CircleCollider2D>().radius = 1.15f * scale;
        Examinable(go, "Asomarse al pozo", examineText, new Vector2(3f, 3f) * scale);
        return go;
    }

    public static GameObject StoneMarker(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size * 1.1f);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.52f, 0.5f, 0.46f), size, -20, "Piedra");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.35f, 0.33f, 0.3f),
            size * new Vector2(0.7f, 0.15f), -19, "Inscripcion", new Vector2(0f, size.y * 0.1f));
        return go;
    }

    public static GameObject Workbench(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = Desk(parent, name, position, size);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.45f, 0.45f, 0.48f),
            new Vector2(0.5f, 0.12f), -12, "Herramienta", new Vector2(size.x * 0.25f, 0.1f), 25f);
        return go;
    }

    public static GameObject Niche(Transform parent, string name, string spotName, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        AddShadow(go.transform, size);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.22f, 0.2f, 0.18f), size * 1.15f, -41, "MarcoPiedra");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.06f, 0.05f, 0.05f, 0.95f), size, -40, "Hueco");
        Box(go, size + new Vector2(0.4f, 0.4f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    public static GameObject WallShadowHide(Transform parent, string name, string spotName, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        Visual(go.transform, EcosAssetFactory.SoftShadow ?? EcosAssetFactory.Glow,
            new Color(0.02f, 0.02f, 0.03f, 0.75f), size * 1.3f, -40, "SombraMuro");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.15f, 0.14f, 0.13f, 0.5f), size, -39, "Rincon");
        Box(go, size + new Vector2(0.4f, 0.4f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    public static void DryPlantAt(Transform parent, Vector2 position, float scale, float rotation)
    {
        Sprite plant = EcosAssetFactory.DryPlant != null ? EcosAssetFactory.DryPlant : EcosAssetFactory.Circle;
        Decor(parent, "PlantaSeca", plant, new Color(0.4f, 0.3f, 0.14f), position,
            new Vector2(scale, scale * 1.1f), -60, rotation);
    }

    public static void EnhancePlate(PuzzlePlate plate)
    {
        if (plate == null) return;
        Transform t = plate.transform;
        // Losa de piedra bajo el símbolo existente.
        Visual(t, EcosAssetFactory.Square, new Color(0.42f, 0.4f, 0.38f), new Vector2(1.05f, 1.05f), -31, "Losa");
        Visual(t, EcosAssetFactory.Square, new Color(0.3f, 0.28f, 0.26f), new Vector2(1.15f, 1.15f), -32, "BordeLosa");
        AddShadow(t, new Vector2(1.2f, 1.2f), -33);
    }
}
