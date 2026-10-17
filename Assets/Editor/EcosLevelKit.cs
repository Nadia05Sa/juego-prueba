using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static EcosBuildUtils;

/// <summary>Cuadrícula de caracteres de un nivel. La fila 0 es la parte superior del mapa.</summary>
public class LevelMap
{
    public readonly int Width;
    public readonly int Height;
    public readonly char[,] Grid;

    public LevelMap(int width, int height, char fill = '#')
    {
        Width = width;
        Height = height;
        Grid = new char[width, height];
        Fill(0, 0, width - 1, height - 1, fill);
    }

    public void Fill(int x0, int row0, int x1, int row1, char c)
    {
        for (int x = x0; x <= x1; x++)
            for (int r = row0; r <= row1; r++)
                Grid[x, r] = c;
    }

    /// <summary>Centro de la celda (x, fila) en coordenadas del mundo.</summary>
    public Vector2 Cell(float x, float row) => new Vector2(x + 0.5f, Height - 0.5f - row);
}

/// <summary>Opciones del jugador y del HUD que cambian entre niveles.</summary>
public class LevelSetup
{
    public string Title;
    public string StartHint;
    /// <summary>Texto extra del tutorial (orientación del nivel). Se muestra bajo los controles.</summary>
    public string TutorialExtra;
    public string NextScene;
    public string ExitMessage;
    public string CompleteTitle = "Nivel completado";
    public string CompleteText;
    public string[] CarryOverItems = new string[0];
    public string GameOverText = "Te alcanzaron.";
    public Color GlobalLightColor = new Color(0.6f, 0.65f, 0.9f);
    public float GlobalLightIntensity = 0.5f;
    public AudioClip Ambience;
    public float PlayerLightRadius = 5.5f;
    public float PlayerLightIntensity = 1f;
    public bool Flashlight;
}

/// <summary>Referencias principales de un nivel en construcción.</summary>
public class LevelContext
{
    public LevelMap Map;
    public Scene Scene;
    public GameObject Player;
    public LevelManager LevelManager;
    public Light2D GlobalLight;
    public Transform Root;
    public FlashlightController Flashlight;
}

/// <summary>Piezas compartidas por los constructores de niveles.</summary>
public static class EcosLevelKit
{
    public const string ScenesFolder = "Assets/Scenes";
    public const string MenuScene = "Menu";
    public const string Level1Scene = "Nivel1_Aljibe";
    public const string Level2Scene = "Nivel2_Cripta";
    public const string Level3Scene = "Nivel3_Establos";
    public const string VictoryScene = "Victoria";
    private const string InputAssetPath = "Assets/Settings/InputSystem_Actions.inputactions";

    public static readonly Color TextColor = new Color(0.96f, 0.9f, 0.8f);
    public static readonly Color WoodColor = new Color(0.4f, 0.26f, 0.14f);
    public static readonly Color DarkSpot = new Color(0.04f, 0.04f, 0.06f, 0.9f);

    public static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    public static void Save(Scene scene, string name) => EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{name}.unity");

    // =====================================================================
    // Base de un nivel
    // =====================================================================

    /// <summary>Crea mapa, luz, sistemas, jugador, cámara, HUD y LevelManager.</summary>
    public static LevelContext BeginLevel(LevelMap map, Dictionary<char, (TileBase wall, TileBase floor)> palette,
        Vector2 spawn, LevelSetup setup)
    {
        var ctx = new LevelContext { Map = map, Scene = NewScene() };

        BuildTilemaps(map, palette);
        ctx.GlobalLight = MakeLight(null, "LuzGlobal", Light2D.LightType.Global, setup.GlobalLightColor, setup.GlobalLightIntensity);

        GameObject systems = GO("Sistemas");
        var gameInput = systems.AddComponent<GameInput>();
        Set(gameInput, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath));
        systems.AddComponent<InventoryManager>();
        systems.AddComponent<ClueManager>();
        ctx.LevelManager = systems.AddComponent<LevelManager>();
        var checkpoints = systems.AddComponent<CheckpointManager>();
        BuildAudioManager(systems.transform, setup.Ambience);

        GameObject spawnGo = GO("PuntoInicio", systems.transform, spawn);
        Set(checkpoints, "initialSpawn", spawnGo.transform);

        ctx.Player = BuildPlayer(spawn, setup);
        ctx.Flashlight = ctx.Player.GetComponent<FlashlightController>();

        Camera cam = MakeCamera(new Vector3(spawn.x, spawn.y, -10f), Color.black);
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        Set(follow, "target", ctx.Player.transform);
        Set(follow, "minBounds", Vector2.zero);
        Set(follow, "maxBounds", new Vector2(map.Width, map.Height));

        BuildHud(ctx.Player, setup);
        MakeEventSystem();

        LevelManager lm = ctx.LevelManager;
        Set(lm, "levelTitle", setup.Title);
        Set(lm, "startHint", setup.StartHint ?? string.Empty);
        Set(lm, "nextScene", setup.NextScene);
        Set(lm, "menuScene", MenuScene);
        Set(lm, "playerHealth", ctx.Player.GetComponent<PlayerHealth>());
        Set(lm, "playerMovement", ctx.Player.GetComponent<PlayerMovement>());
        Set(lm, "playerHiding", ctx.Player.GetComponent<PlayerHiding>());
        Set(lm, "exitMessage", setup.ExitMessage);
        Set(lm, "completeTitle", setup.CompleteTitle);
        Set(lm, "completeText", setup.CompleteText);
        Set(lm, "carryOverItems", setup.CarryOverItems);

        ctx.Root = GO("Nivel").transform;
        return ctx;
    }

    public static Transform Area(LevelContext ctx, string name)
    {
        Transform t = GO(name).transform;
        t.SetParent(ctx.Root, false);
        return t;
    }

    public static void SetObjectives(LevelManager lm, (string text, ObjectiveCondition cond, string id, int count)[] steps)
    {
        var so = new SerializedObject(lm);
        SerializedProperty list = so.FindProperty("objectives");
        list.arraySize = steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            SerializedProperty e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("text").stringValue = steps[i].text;
            e.FindPropertyRelative("condition").intValue = (int)steps[i].cond;
            e.FindPropertyRelative("id").stringValue = steps[i].id;
            e.FindPropertyRelative("count").intValue = steps[i].count;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Tilemaps ----------

    public static void BuildTilemaps(LevelMap map, Dictionary<char, (TileBase wall, TileBase floor)> palette)
    {
        GameObject grid = GO("Grid");
        grid.AddComponent<Grid>();

        GameObject floorGo = GO("Suelo", grid.transform);
        var floor = floorGo.AddComponent<Tilemap>();
        floorGo.AddComponent<TilemapRenderer>().sortingOrder = -100;

        GameObject wallGo = GO("Muros", grid.transform);
        var walls = wallGo.AddComponent<Tilemap>();
        wallGo.AddComponent<TilemapRenderer>().sortingOrder = -80;

        for (int x = 0; x < map.Width; x++)
        {
            for (int row = 0; row < map.Height; row++)
            {
                if (!palette.TryGetValue(map.Grid[x, row], out (TileBase wall, TileBase floor) tiles)) continue;
                var pos = new Vector3Int(x, map.Height - 1 - row, 0);
                if (tiles.wall != null) walls.SetTile(pos, tiles.wall);
                if (tiles.floor != null) floor.SetTile(pos, tiles.floor);
            }
        }

        // El compuesto solo genera geometría si la combinación se activa con los tiles ya colocados.
        var body = wallGo.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        var tilemapCollider = wallGo.AddComponent<TilemapCollider2D>();
        var composite = wallGo.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
        tilemapCollider.ProcessTilemapChanges();
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        tilemapCollider.ProcessTilemapChanges();
        composite.GenerateGeometry();
        if (composite.pathCount == 0)
            Debug.LogError("Ecos del Mictlán: los muros no generaron colisiones.");
    }

    // ---------- Sistemas ----------

    private static void BuildAudioManager(Transform parent, AudioClip ambienceClip)
    {
        GameObject go = GO("AudioManager", parent);
        var manager = go.AddComponent<AudioManager>();
        AudioSource sfx = MakeAudio(GO("SFX", go.transform), null, 1f, false, false);
        AudioSource ambience = MakeAudio(GO("Ambiente", go.transform), ambienceClip != null ? ambienceClip : EcosAssetFactory.Wind, 0.22f, true, true);
        AudioSource tension = MakeAudio(GO("Tension", go.transform), EcosAssetFactory.Tension, 0f, true, true);
        Set(manager, "ambienceVolume", 0.16f);
        Set(manager, "tensionVolume", 0.36f);
        Set(manager, "fadeSpeed", 0.45f);

        Set(manager, "sfxSource", sfx);
        Set(manager, "ambienceSource", ambience);
        Set(manager, "tensionSource", tension);
        Set(manager, "pickup", EcosAssetFactory.Pickup);
        Set(manager, "clue", EcosAssetFactory.Clue);
        Set(manager, "error", EcosAssetFactory.Error);
        Set(manager, "solve", EcosAssetFactory.Solve);
        Set(manager, "hurt", EcosAssetFactory.Hurt);
        Set(manager, "door", EcosAssetFactory.Door);
        Set(manager, "locked", EcosAssetFactory.Locked);
        Set(manager, "platePress", EcosAssetFactory.PlatePress);
        Set(manager, "chains", EcosAssetFactory.Chains);
        Set(manager, "lever", EcosAssetFactory.Lever);
        Set(manager, "click", EcosAssetFactory.Click);
        Set(manager, "stoneDoor", EcosAssetFactory.StoneDoor);
        Set(manager, "hooves", EcosAssetFactory.Hooves);
        Set(manager, "footstep", EcosAssetFactory.Footstep);
    }

    private static GameObject BuildPlayer(Vector2 position, LevelSetup setup)
    {
        GameObject player = GO("Player", null, position);
        player.tag = "Player";

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

        var capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = new Vector2(0.6f, 0.8f);

        bool animated = EcosAnimationFactory.PlayerController != null;
        SpriteRenderer body = Visual(player.transform, EcosAssetFactory.Circle,
            animated ? Color.white : new Color(0.85f, 0.72f, 0.5f),
            animated ? new Vector2(0.95f, 0.95f) : new Vector2(0.7f, 0.9f), 10, "Cuerpo");
        SpriteRenderer hair = null;
        SpriteRenderer indicator = null;
        if (!animated)
        {
            hair = Visual(body.transform, EcosAssetFactory.Circle, new Color(0.18f, 0.12f, 0.08f), new Vector2(0.8f, 0.45f), 11, "Cabello", new Vector2(0f, 0.3f));
            indicator = Visual(player.transform, EcosAssetFactory.Circle, new Color(1f, 0.95f, 0.8f), new Vector2(0.18f, 0.18f), 12, "Orientacion");
        }

        Light2D playerLight = MakeLight(player.transform, "LuzJugador", Light2D.LightType.Point,
            new Color(1f, 0.82f, 0.55f), setup.PlayerLightIntensity, setup.PlayerLightRadius);
        playerLight.pointLightInnerRadius = Mathf.Max(0.35f, setup.PlayerLightRadius * 0.18f);
        var lightFlicker = playerLight.gameObject.AddComponent<LightFlicker>();
        Set(lightFlicker, "light2D", playerLight);
        Set(lightFlicker, "amount", 0.07f);
        Set(lightFlicker, "speed", 2.2f);

        player.AddComponent<PlayerMovement>();
        var anim = player.AddComponent<PlayerAnimation>();
        Set(anim, "body", body);
        if (indicator != null) Set(anim, "facingIndicator", indicator.transform);
        Set(anim, "bobAmount", animated ? 0f : 0.06f);

        if (animated)
        {
            var animator = player.AddComponent<Animator>();
            animator.runtimeAnimatorController = EcosAnimationFactory.PlayerController;
            animator.applyRootMotion = false;
            Set(anim, "animator", animator);
        }

        player.AddComponent<InteractionController>();

        var flashList = animated
            ? new Object[] { body }
            : new Object[] { body, hair, indicator };
        var hiding = player.AddComponent<PlayerHiding>();
        Set(hiding, "renderers", flashList);

        var health = player.AddComponent<PlayerHealth>();
        Set(health, "flashRenderers", flashList);

        if (setup.Flashlight)
        {
            Light2D beam = MakeLight(player.transform, "Linterna", Light2D.LightType.Point, new Color(1f, 0.95f, 0.8f), 1.3f, 7.5f);
            beam.pointLightInnerRadius = 0.5f;
            beam.pointLightOuterAngle = 60f;
            beam.pointLightInnerAngle = 36f;
            beam.enabled = false;
            var flashlight = player.AddComponent<FlashlightController>();
            Set(flashlight, "beam", beam);
            Set(flashlight, "range", 7.5f);
            Set(flashlight, "drainPerSecond", 2f);
        }

        player.AddComponent<AudioListener>();
        return player;
    }

    // ---------- HUD ----------

    private static void BuildHud(GameObject player, LevelSetup setup)
    {
        Canvas canvas = MakeCanvas("HUD");
        Transform root = canvas.transform;
        var ui = canvas.gameObject.AddComponent<UIManager>();

        RectTransform heartsRt = MakeRect(root, "Vidas", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(240f, 64f));
        var hearts = new Object[3];
        for (int i = 0; i < 3; i++)
        {
            RectTransform h = MakeRect(heartsRt, "Corazon" + (i + 1), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 70f, 0f), new Vector2(58f, 58f));
            hearts[i] = MakeImage(h, Color.red, EcosAssetFactory.Heart, false);
        }

        Text objective = MakeText(MakeRect(root, "Objetivo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(1400f, 78f)),
            "", 28, TextColor, TextAnchor.MiddleCenter);
        Text message = MakeText(MakeRect(root, "Mensaje", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1400f, 120f)),
            "", 28, Color.white, TextAnchor.MiddleCenter);
        Text prompt = MakeText(MakeRect(root, "Interaccion", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(1200f, 60f)),
            "", 32, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleCenter);
        float hintBottom = setup.Flashlight ? 110f : 28f;
        Text inventoryHint = MakeText(MakeRect(root, "AvisoInventario", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, hintBottom), new Vector2(360f, 40f)),
            "I — Inventario", 24, new Color(0.85f, 0.78f, 0.65f), TextAnchor.LowerRight);
        InventoryUI inventoryUi = BuildInventoryPanel(root, inventoryHint);

        if (setup.Flashlight) BuildBatteryBar(root, player.GetComponent<FlashlightController>());

        Image damage = MakeImage(Stretch(root, "DestelloDano"), new Color(0.8f, 0f, 0f, 0f), null, false);

        PhotoInspectUI photoUi = BuildPhotoInspectPanel(root);

        // Panel de lectura (carta / nota con look de papel viejo).
        RectTransform notePanel = Stretch(root, "PanelLectura");
        MakeImage(notePanel, new Color(0.02f, 0.01f, 0.01f, 0.82f));
        RectTransform paperShadow = MakeRect(notePanel, "SombraPapel", Center, Center, Center, new Vector2(10f, -12f), new Vector2(980f, 720f));
        MakeImage(paperShadow, new Color(0f, 0f, 0f, 0.55f), EcosAssetFactory.AgedPaper ?? EcosAssetFactory.BookPage, false);
        RectTransform paper = MakeRect(notePanel, "Papel", Center, Center, Center, Vector2.zero, new Vector2(960f, 700f));
        Sprite paperSprite = EcosAssetFactory.AgedPaper != null ? EcosAssetFactory.AgedPaper : EcosAssetFactory.BookPage;
        MakeImage(paper, new Color(0.78f, 0.68f, 0.48f), paperSprite, false);
        // Manchas decorativas
        MakeImage(MakeRect(paper, "Mancha1", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -30f), new Vector2(120f, 80f)),
            new Color(0.35f, 0.12f, 0.08f, 0.35f), EcosAssetFactory.Glow, false);
        MakeImage(MakeRect(paper, "Mancha2", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 40f), new Vector2(140f, 90f)),
            new Color(0.2f, 0.15f, 0.1f, 0.4f), EcosAssetFactory.Glow, false);
        Color ink = new Color(0.22f, 0.1f, 0.06f);
        Text noteTitle = MakeText(MakeRect(paper, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(820f, 70f)),
            "", 40, new Color(0.28f, 0.08f, 0.05f), TextAnchor.MiddleCenter, false);
        Text noteBody = MakeText(MakeRect(paper, "Cuerpo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(840f, 500f)),
            "", 26, ink, TextAnchor.UpperCenter, false);
        MakeText(MakeRect(paper, "Cerrar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(820f, 40f)),
            "E / Esc — Cerrar", 22, new Color(0.4f, 0.22f, 0.12f), TextAnchor.MiddleCenter, false);

        TutorialUI tutorialUi = BuildTutorialPanel(root, player, setup);

        // Pausa
        RectTransform pause = Stretch(root, "PanelPausa");
        MakeImage(pause, new Color(0f, 0f, 0f, 0.8f));
        MakeText(MakeRect(pause, "Titulo", Center, Center, Center, new Vector2(0f, 300f), new Vector2(800f, 100f)), "Pausa", 72, TextColor, TextAnchor.MiddleCenter);
        MakeButton(pause, "Continuar", new Vector2(0f, 150f), MenuAction.Continuar);
        MakeButton(pause, "Reiniciar nivel", new Vector2(0f, 60f), MenuAction.ReiniciarNivel);
        MakeButton(pause, "Menú principal", new Vector2(0f, -30f), MenuAction.MenuPrincipal, MenuScene);
        MakeButton(pause, "Salir del juego", new Vector2(0f, -120f), MenuAction.Salir);
        string controls = "WASD / Flechas: moverse   ·   Shift: correr   ·   E: interactuar   ·   Q: esconderse   ·   I: inventario";
        if (setup.Flashlight) controls += "   ·   F: linterna";
        controls += "   ·   Esc: pausa";
        MakeText(MakeRect(pause, "Controles", Center, Center, Center, new Vector2(0f, -300f), new Vector2(1600f, 100f)),
            controls, 26, new Color(0.8f, 0.75f, 0.7f), TextAnchor.MiddleCenter);

        // Game Over
        RectTransform gameOver = Stretch(root, "PanelGameOver");
        MakeImage(gameOver, new Color(0.08f, 0f, 0f, 0.88f));
        MakeText(MakeRect(gameOver, "Titulo", Center, Center, Center, new Vector2(0f, 260f), new Vector2(1200f, 110f)),
            "Te alcanzaron...", 80, new Color(0.85f, 0.15f, 0.15f), TextAnchor.MiddleCenter);
        MakeText(MakeRect(gameOver, "Texto", Center, Center, Center, new Vector2(0f, 170f), new Vector2(1200f, 60f)),
            setup.GameOverText, 30, TextColor, TextAnchor.MiddleCenter);
        MakeButton(gameOver, "Reintentar desde el punto de control", new Vector2(0f, 40f), MenuAction.ReintentarCheckpoint, null, null, 700f);
        MakeButton(gameOver, "Reiniciar nivel", new Vector2(0f, -50f), MenuAction.ReiniciarNivel, null, null, 700f);
        MakeButton(gameOver, "Menú principal", new Vector2(0f, -140f), MenuAction.MenuPrincipal, MenuScene, null, 700f);

        // Nivel completado
        RectTransform complete = Stretch(root, "PanelNivelCompletado");
        MakeImage(complete, new Color(0.02f, 0.02f, 0.03f, 0.92f));
        Text completeTitle = MakeText(MakeRect(complete, "Titulo", Center, Center, Center, new Vector2(0f, 140f), new Vector2(1400f, 120f)),
            "", 84, new Color(0.95f, 0.75f, 0.35f), TextAnchor.MiddleCenter);
        Text completeText = MakeText(MakeRect(complete, "Texto", Center, Center, Center, new Vector2(0f, -40f), new Vector2(1300f, 220f)),
            "", 34, TextColor, TextAnchor.UpperCenter);

        Image fade = MakeImage(Stretch(root, "Fundido"), Color.black, null, false);

        var heartsUi = heartsRt.gameObject.AddComponent<HeartsUI>();
        Set(heartsUi, "health", player.GetComponent<PlayerHealth>());
        Set(heartsUi, "hearts", hearts);
        Set(heartsUi, "damageOverlay", damage);

        Set(ui, "promptText", prompt);
        Set(ui, "objectiveText", objective);
        Set(ui, "messageText", message);
        Set(ui, "inventoryUI", inventoryUi);
        Set(ui, "tutorialUI", tutorialUi);
        Set(ui, "photoInspectUI", photoUi);
        Set(ui, "notePanel", notePanel.gameObject);
        Set(ui, "noteTitle", noteTitle);
        Set(ui, "noteBody", noteBody);
        Set(ui, "pausePanel", pause.gameObject);
        Set(ui, "gameOverPanel", gameOver.gameObject);
        Set(ui, "levelCompletePanel", complete.gameObject);
        Set(ui, "levelCompleteTitle", completeTitle);
        Set(ui, "levelCompleteText", completeText);
        Set(ui, "fadeImage", fade);
        Set(ui, "playerMovement", player.GetComponent<PlayerMovement>());
    }

    private static TutorialUI BuildTutorialPanel(Transform root, GameObject player, LevelSetup setup)
    {
        RectTransform overlay = Stretch(root, "PanelTutorial");
        MakeImage(overlay, new Color(0.02f, 0.01f, 0.02f, 0.88f));

        RectTransform card = MakeRect(overlay, "Tarjeta", Center, Center, Center, Vector2.zero, new Vector2(920f, 620f));
        MakeImage(card, new Color(0.12f, 0.06f, 0.05f, 0.96f));
        MakeImage(MakeRect(card, "Marco", Center, Center, Center, Vector2.zero, new Vector2(900f, 600f)),
            new Color(0.35f, 0.18f, 0.1f, 0.5f), EcosAssetFactory.AgedPaper, false);

        MakeText(MakeRect(card, "Titulo", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(840f, 50f)),
            "Cómo explorar la hacienda", 36, new Color(0.95f, 0.85f, 0.7f), TextAnchor.MiddleCenter);

        string body =
            "WASD / Flechas — Moverse\n" +
            "Shift — Correr (hace más ruido)\n" +
            "E — Interactuar (puertas, notas, objetos)\n" +
            "Q — Esconderse (cerca de un escondite)\n" +
            "I / Tab — Abrir el inventario\n" +
            "Esc — Pausar\n\n" +
            "Lee las cartas y busca pistas.\n" +
            "Si algo te persigue: corre o escóndete.\n" +
            "No intentes pelear: solo puedes escapar.";
        if (setup.Flashlight)
            body += "\nF — Encender / apagar la linterna (gasta batería).";
        if (!string.IsNullOrEmpty(setup.TutorialExtra))
            body += "\n\n" + setup.TutorialExtra;

        float cardH = string.IsNullOrEmpty(setup.TutorialExtra) ? 620f : 700f;
        card.sizeDelta = new Vector2(920f, cardH);

        MakeText(MakeRect(card, "Cuerpo", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(800f, cardH - 200f)),
            body, 24, new Color(0.9f, 0.82f, 0.72f), TextAnchor.MiddleLeft, false);

        MakeText(MakeRect(card, "Continuar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(800f, 40f)),
            "E — Empezar a explorar", 28, new Color(1f, 0.75f, 0.4f), TextAnchor.MiddleCenter);

        // El controlador vive en un hijo activo para que Update siga corriendo
        // aunque el overlay se apague al cerrar el tutorial.
        GameObject host = new GameObject("TutorialUI");
        host.transform.SetParent(root, false);
        var tutorial = host.AddComponent<TutorialUI>();
        Set(tutorial, "panel", overlay.gameObject);
        Set(tutorial, "playerMovement", player.GetComponent<PlayerMovement>());
        overlay.gameObject.SetActive(false);
        return tutorial;
    }

    private static PhotoInspectUI BuildPhotoInspectPanel(Transform root)
    {
        RectTransform overlay = Stretch(root, "PanelFotografia");
        MakeImage(overlay, new Color(0f, 0f, 0f, 0.82f));

        RectTransform card = MakeRect(overlay, "Marco", Center, Center, Center, new Vector2(0f, 40f), new Vector2(540f, 700f));
        MakeImage(card, new Color(0.1f, 0.07f, 0.05f));

        RectTransform photoRoot = MakeRect(card, "Foto", Center, Center, Center, new Vector2(0f, 20f), new Vector2(480f, 600f));
        Image photo = MakeImage(photoRoot, Color.white, EcosAssetFactory.PhotoCatalinaFront, false);
        photo.preserveAspect = true;

        // Inscripción encima de la imagen (lápidas): tinta oscura, área amplia.
        Text inscription = MakeText(MakeRect(photoRoot, "Inscripcion", Center, Center, Center, new Vector2(0f, -20f), new Vector2(380f, 420f)),
            "", 26, new Color(0.1f, 0.07f, 0.04f), TextAnchor.MiddleCenter, false);
        inscription.gameObject.SetActive(false);

        Text caption = MakeText(MakeRect(overlay, "Pie", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1100f, 110f)),
            "", 28, new Color(0.92f, 0.88f, 0.78f), TextAnchor.MiddleCenter);
        Text hint = MakeText(MakeRect(overlay, "Ayuda", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 40f)),
            "E — Voltear    ·    Esc — Guardar", 26, new Color(0.8f, 0.75f, 0.65f), TextAnchor.MiddleCenter);

        var inspect = overlay.gameObject.AddComponent<PhotoInspectUI>();
        Set(inspect, "panel", overlay.gameObject);
        Set(inspect, "photoRoot", photoRoot);
        Set(inspect, "photoImage", photo);
        Set(inspect, "captionText", caption);
        Set(inspect, "hintText", hint);
        Set(inspect, "inscriptionOverlay", inscription);
        overlay.gameObject.SetActive(false);
        return inspect;
    }

    private static InventoryUI BuildInventoryPanel(Transform root, Text hint)
    {
        RectTransform overlay = Stretch(root, "PanelInventario");
        MakeImage(overlay, new Color(0f, 0f, 0f, 0.72f));

        // Cuaderno abierto: dos páginas de pergamino.
        RectTransform book = MakeRect(overlay, "Cuaderno", Center, Center, Center, Vector2.zero, new Vector2(1180f, 720f));
        MakeImage(book, new Color(0.22f, 0.12f, 0.08f));

        Color page = new Color(0.86f, 0.8f, 0.66f);
        Color ink = new Color(0.22f, 0.14f, 0.08f);
        Color accent = new Color(0.45f, 0.55f, 0.28f);

        RectTransform left = MakeRect(book, "PaginaIzquierda", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(540f, 660f));
        MakeImage(left, page, EcosAssetFactory.BookPage);
        MakeText(MakeRect(left, "TituloObjetos", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(480f, 48f)),
            "Objetos", 36, ink, TextAnchor.MiddleCenter, false);
        MakeImage(MakeRect(left, "Linea", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(420f, 3f)), accent, null, false);

        RectTransform itemsRoot = MakeRect(left, "ListaObjetos", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(480f, 500f));
        Text emptyItems = MakeText(MakeRect(left, "VacioObjetos", Center, Center, Center, new Vector2(0f, -20f), new Vector2(420f, 80f)),
            "Todavía no llevas objetos.", 26, new Color(0.4f, 0.32f, 0.24f), TextAnchor.MiddleCenter, false);
        for (int i = 0; i < 12; i++)
            MakeInventorySlot(itemsRoot, "Objeto" + (i + 1), i, ink);

        RectTransform right = MakeRect(book, "PaginaDerecha", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(540f, 660f));
        MakeImage(right, new Color(0.84f, 0.78f, 0.64f), EcosAssetFactory.BookPage);
        MakeText(MakeRect(right, "TituloPistas", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(480f, 48f)),
            "Notas y cartas", 32, ink, TextAnchor.MiddleCenter, false);
        MakeImage(MakeRect(right, "Linea", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(420f, 3f)), accent, null, false);

        RectTransform cluesRoot = MakeRect(right, "ListaPistas", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(480f, 500f));
        Text emptyClues = MakeText(MakeRect(right, "VacioPistas", Center, Center, Center, new Vector2(0f, -20f), new Vector2(420f, 80f)),
            "Aún no has leído notas ni cartas.\n(Clic para abrirlas.)", 24, new Color(0.4f, 0.32f, 0.24f), TextAnchor.MiddleCenter, false);
        for (int i = 0; i < 10; i++)
            MakeInventorySlot(cluesRoot, "Pista" + (i + 1), i, ink);

        MakeText(MakeRect(book, "TituloLibro", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 36f), new Vector2(600f, 48f)),
            "Inventario", 42, new Color(0.95f, 0.85f, 0.55f), TextAnchor.MiddleCenter);
        MakeText(MakeRect(overlay, "Cerrar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(800f, 40f)),
            "I / Esc — Cerrar    ·    Clic en una nota para leerla", 24, new Color(0.85f, 0.8f, 0.7f), TextAnchor.MiddleCenter);

        // Cinta del centro (detalle visual).
        MakeImage(MakeRect(book, "Cinta", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 8f), new Vector2(28f, 90f)),
            new Color(0.35f, 0.45f, 0.2f), null, false);

        var inventory = overlay.gameObject.AddComponent<InventoryUI>();
        Set(inventory, "panel", overlay.gameObject);
        Set(inventory, "itemSlotsRoot", itemsRoot);
        Set(inventory, "clueSlotsRoot", cluesRoot);
        Set(inventory, "emptyItemsText", emptyItems);
        Set(inventory, "emptyCluesText", emptyClues);
        Set(inventory, "hintText", hint);
        Set(inventory, "defaultItemIcon", EcosAssetFactory.KeyIcon);
        Set(inventory, "defaultClueIcon", EcosAssetFactory.NoteIcon);
        Set(inventory, "keyIcon", EcosAssetFactory.KeyIcon);
        Set(inventory, "medallionIcon", EcosAssetFactory.MedallionIcon);
        Set(inventory, "crowbarIcon", EcosAssetFactory.CrowbarIcon);
        Set(inventory, "batteryIcon", EcosAssetFactory.BatteryIcon);
        overlay.gameObject.SetActive(false);
        return inventory;
    }

    private static void MakeInventorySlot(Transform parent, string name, int index, Color ink)
    {
        float y = 220f - index * 48f;
        RectTransform slot = MakeRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(460f, 44f));
        MakeImage(slot, new Color(0.78f, 0.7f, 0.55f, 0.55f), null, true);
        RectTransform iconRt = MakeRect(slot, "Icono", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(36f, 36f));
        Image icon = MakeImage(iconRt, Color.white, EcosAssetFactory.KeyIcon, false);
        icon.preserveAspect = true;
        MakeText(MakeRect(slot, "Nombre", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(380f, 40f)),
            "", 24, ink, TextAnchor.MiddleLeft, false);
        slot.gameObject.SetActive(false);
    }

    private static void BuildBatteryBar(Transform root, FlashlightController flashlight)
    {
        RectTransform panel = MakeRect(root, "Bateria", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(360f, 70f));
        Text label = MakeText(MakeRect(panel, "Texto", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 34f)),
            "", 24, TextColor, TextAnchor.MiddleRight);
        RectTransform back = MakeRect(panel, "Fondo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(0f, 24f));
        MakeImage(back, new Color(0f, 0f, 0f, 0.7f), null, false);
        RectTransform fill = MakeRect(back, "Relleno", Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
        Image fillImage = MakeImage(fill, new Color(1f, 0.85f, 0.4f), null, false);

        var ui = panel.gameObject.AddComponent<BatteryUI>();
        Set(ui, "flashlight", flashlight);
        Set(ui, "fill", fill);
        Set(ui, "fillImage", fillImage);
        Set(ui, "label", label);
    }

    // =====================================================================
    // Objetos reutilizables
    // =====================================================================

    public static DoorController Door(Transform parent, string name, Vector2 position, bool vertical, Color color,
        DoorState state, string prompt, string lockedMessage)
    {
        Vector2 size = vertical ? new Vector2(1f, 2f) : new Vector2(2f, 1f);
        GameObject go = GO(name, parent, position);
        SpriteRenderer visual = Visual(go.transform, EcosAssetFactory.Square, color, size, -15);
        BoxCollider2D solid = Box(go, size, false);
        TriggerChild(go.transform, vertical ? new Vector2(2.6f, 2f) : new Vector2(2f, 2.6f));

        var door = go.AddComponent<DoorController>();
        Set(door, "initialState", state);
        Set(door, "prompt", prompt);
        Set(door, "lockedMessage", lockedMessage);
        Set(door, "blockingCollider", solid);
        Set(door, "doorRenderer", visual);
        // Las puertas vuelven al fade de siempre (sin Animator de giro).
        return door;
    }

    public static GameObject Candle(Transform parent, Vector2 position)
    {
        GameObject go = GO("Vela", parent, position);
        Visual(go.transform, EcosAssetFactory.SoftShadow ?? EcosAssetFactory.Glow,
            new Color(0f, 0f, 0f, 0.3f), new Vector2(0.45f, 0.35f), -8, "Sombra", new Vector2(0.05f, -0.08f));
        Visual(go.transform, EcosAssetFactory.Circle, new Color(0.35f, 0.32f, 0.28f), new Vector2(0.32f, 0.28f), -7, "Plato");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.55f, 0.5f, 0.35f, 0.55f),
            new Vector2(0.18f, 0.1f), -6, "CeraDerramada", new Vector2(0.08f, -0.06f), 20f);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.92f, 0.88f, 0.75f), new Vector2(0.14f, 0.28f), -5, "Cera");
        SpriteRenderer flame = Visual(go.transform, EcosAssetFactory.Circle, new Color(1f, 0.75f, 0.25f), new Vector2(0.14f, 0.2f), -4, "Llama", new Vector2(0f, 0.22f));
        Light2D light = MakeLight(go.transform, "Luz", Light2D.LightType.Point, new Color(1f, 0.6f, 0.25f), 0.9f, 2.8f, new Vector2(0f, 0.22f));
        TriggerChild(go.transform, new Vector2(1.2f, 1.2f));

        var candle = go.AddComponent<CandleInteractable>();
        Set(candle, "flame", flame);
        Set(candle, "flameLight", light);
        var flicker = go.AddComponent<LightFlicker>();
        Set(flicker, "light2D", light);
        Set(flicker, "amount", 0.18f);
        Set(flicker, "speed", 4.5f);
        return go;
    }

    /// <summary>Lámpara fija: luz decorativa sin interacción (rutas iluminadas).</summary>
    public static void Lamp(Transform parent, Vector2 position, float radius = 4f, float intensity = 1f)
    {
        GameObject go = GO("Lampara", parent, position);
        Visual(go.transform, EcosAssetFactory.Circle, new Color(1f, 0.8f, 0.45f), new Vector2(0.3f, 0.3f), -4, "Farol");
        Light2D light = MakeLight(go.transform, "Luz", Light2D.LightType.Point, new Color(1f, 0.7f, 0.35f), intensity, radius);
        var flicker = go.AddComponent<LightFlicker>();
        Set(flicker, "light2D", light);
        Set(flicker, "amount", 0.1f);
        Set(flicker, "speed", 3f);
    }

    public static GameObject HidingSpotAt(Transform parent, string name, string spotName, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = GO(name, parent, position);
        Visual(go.transform, EcosAssetFactory.Square, color, size, -40);
        Box(go, size + new Vector2(0.4f, 0.4f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", spotName);
        return go;
    }

    public static GameObject CheckpointAt(Transform parent, Vector2 position)
    {
        GameObject go = GO("PuntoControl", parent, position);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.35f, 0.32f, 0.28f, 0.55f), new Vector2(1.1f, 1.1f), -72, "Baldosa");
        SpriteRenderer marker = Visual(go.transform, EcosAssetFactory.Circle, new Color(0.55f, 0.5f, 0.35f, 0.4f), new Vector2(0.55f, 0.55f), -70, "Marca");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.45f, 0.4f, 0.28f, 0.5f), new Vector2(0.5f, 0.08f), -69, "CruzH");
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.45f, 0.4f, 0.28f, 0.5f), new Vector2(0.08f, 0.5f), -69, "CruzV");
        Box(go, new Vector2(2f, 2f), true);
        var cp = go.AddComponent<Checkpoint>();
        Set(cp, "marker", marker);
        return go;
    }

    public static NoteInteractable Note(Transform parent, string name, Vector2 position, NoteData data, string prompt, Vector2 triggerSize)
    {
        GameObject go = GO(name, parent, position);
        string id = data != null ? data.Id ?? string.Empty : string.Empty;
        string title = data != null ? data.Title ?? string.Empty : string.Empty;
        string promptLower = prompt ?? string.Empty;
        bool isBook = id.IndexOf("libro", System.StringComparison.OrdinalIgnoreCase) >= 0
            || id.IndexOf("diario", System.StringComparison.OrdinalIgnoreCase) >= 0
            || id.IndexOf("bitacora", System.StringComparison.OrdinalIgnoreCase) >= 0
            || id.IndexOf("registro", System.StringComparison.OrdinalIgnoreCase) >= 0
            || title.IndexOf("libro", System.StringComparison.OrdinalIgnoreCase) >= 0
            || promptLower.IndexOf("libro", System.StringComparison.OrdinalIgnoreCase) >= 0;
        Sprite sprite = isBook && EcosAssetFactory.BookClosed != null
            ? EcosAssetFactory.BookClosed
            : (EcosAssetFactory.NoteSheet != null ? EcosAssetFactory.NoteSheet : EcosAssetFactory.Square);
        Color tint = isBook ? new Color(0.45f, 0.22f, 0.12f) : new Color(0.92f, 0.88f, 0.75f);
        Vector2 size = isBook ? new Vector2(0.38f, 0.48f) : new Vector2(0.32f, 0.24f);
        SpriteRenderer paper = Visual(go.transform, sprite, tint, size, -5, isBook ? "Libro" : "Papel", default, isBook ? -12f : 8f);
        TriggerChild(go.transform, triggerSize);
        var note = go.AddComponent<NoteInteractable>();
        Set(note, "note", data);
        Set(note, "prompt", prompt);
        var floatAnim = go.AddComponent<SpriteFloat>();
        Set(floatAnim, "target", paper.transform);
        Set(floatAnim, "height", 0.04f);
        Set(floatAnim, "speed", 1.6f);
        Set(floatAnim, "spinDegrees", isBook ? 0f : 3f);
        return note;
    }

    public static ExaminableObject Examinable(GameObject go, string prompt, string description, Vector2 triggerSize, string flag = null)
    {
        TriggerChild(go.transform, triggerSize);
        var ex = go.AddComponent<ExaminableObject>();
        Set(ex, "prompt", prompt);
        Set(ex, "description", description);
        if (flag != null) Set(ex, "flagOnExamine", flag);
        return ex;
    }

    public static KeyItem Pickup(Transform parent, string name, Vector2 position, Color color, Vector2 size, string prompt,
        string itemId, string itemName, string message, string flag = null, float triggerSize = 1.6f)
    {
        GameObject go = GO(name, parent, position);
        SpriteRenderer visual = Visual(go.transform, EcosAssetFactory.Square, color, size, -6);
        TriggerChild(go.transform, new Vector2(triggerSize, triggerSize));
        var key = go.AddComponent<KeyItem>();
        Set(key, "prompt", prompt);
        Set(key, "itemId", itemId);
        Set(key, "itemName", itemName);
        Set(key, "pickupMessage", message);
        if (flag != null) Set(key, "flagOnPickup", flag);
        var floatAnim = go.AddComponent<SpriteFloat>();
        Set(floatAnim, "target", visual.transform);
        Set(floatAnim, "height", 0.06f);
        Set(floatAnim, "speed", 2.4f);
        Set(floatAnim, "spinDegrees", 6f);
        return key;
    }

    public static PuzzlePlate Plate(Transform parent, SequencePuzzle puzzle, string name, string id, string displayName,
        Sprite symbol, Color symbolColor, Vector2 position)
    {
        GameObject go = GO(name, parent, position);
        Visual(go.transform, EcosAssetFactory.SoftShadow ?? EcosAssetFactory.Glow,
            new Color(0.02f, 0.02f, 0.04f, 0.35f), new Vector2(1.3f, 1.2f), -33, "Sombra", new Vector2(0.1f, -0.1f));
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.32f, 0.3f, 0.28f), new Vector2(1.12f, 1.12f), -32, "BordeLosa");
        SpriteRenderer plateRenderer = Visual(go.transform, EcosAssetFactory.Square, new Color(0.48f, 0.46f, 0.42f), new Vector2(0.95f, 0.95f), -30, "Placa");
        Visual(go.transform, symbol, symbolColor, new Vector2(0.58f, 0.58f), -29, "Simbolo");
        TriggerChild(go.transform, new Vector2(1.5f, 1.5f));

        var plate = go.AddComponent<PuzzlePlate>();
        Set(plate, "symbolId", id);
        Set(plate, "symbolName", displayName);
        Set(plate, "puzzle", puzzle);
        Set(plate, "plateRenderer", plateRenderer);
        return plate;
    }

    public static Lever MakeLever(Transform parent, string name, Vector2 position, string prompt)
    {
        GameObject go = GO(name, parent, position);
        Visual(go.transform, EcosAssetFactory.Square, new Color(0.3f, 0.3f, 0.32f), new Vector2(0.8f, 0.5f), -10, "Base");
        GameObject handle = GO("Palanca", go.transform);
        handle.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        Visual(handle.transform, EcosAssetFactory.Square, new Color(0.55f, 0.4f, 0.2f), new Vector2(0.14f, 0.8f), -9, "Mango", new Vector2(0f, 0.4f));
        TriggerChild(go.transform, new Vector2(2f, 2f));
        var lever = go.AddComponent<Lever>();
        Set(lever, "prompt", prompt);
        Set(lever, "handle", handle.transform);
        return lever;
    }

    public static LevelExit Exit(Transform parent, Vector2 position, Vector2 size, string requiredFlag, string notReadyMessage)
    {
        GameObject go = GO("Salida", parent, position);
        Box(go, size, true);
        var exit = go.AddComponent<LevelExit>();
        Set(exit, "requiredFlag", requiredFlag);
        Set(exit, "notReadyMessage", notReadyMessage);
        return exit;
    }

    public static void SavePrefabCopy(GameObject go, string path) => PrefabUtility.SaveAsPrefabAsset(go, path);

    // =====================================================================
    // Enemigos y navegación
    // =====================================================================

    public class EnemySpec
    {
        public string Name;
        public Color BodyColor;
        public Color DetailColor;
        public Vector2 BodySize = new Vector2(0.8f, 1.35f);
        public Color ConeColor = new Color(0.7f, 0.85f, 1f, 0.15f);
        public AudioClip Voice;
        public float IdleVoiceVolume = 0.5f;
        public float ViewDistance = 6f;
        public float ViewAngle = 100f;
        public float Hearing = 1f;
        public float PatrolSpeed = 1.5f;
        public float InvestigateSpeed = 2.2f;
        public float ChaseSpeed = 3.6f;
        public float CloseChaseDistance = 3f;
        public float LoseSightTime = 0.8f;
        public float SearchDuration = 4f;
        public float SearchRadius = 2.5f;
        public bool StartActive = true;
        public bool StartPatrolling = true;
        public bool VanishOnReturn;
        public bool RetreatAfterHit = true;
        public float StunAfterHit;
        public string WakeOnFlag;
        public NavGraph Nav;
        public RuntimeAnimatorController Animator;
    }

    public static EnemyBrain BuildEnemy(Transform parent, EnemySpec spec, Vector2[] route, Vector2 start)
    {
        var points = new List<Object>();
        Transform pointsRoot = GO("PuntosPatrulla", parent).transform;
        for (int i = 0; i < route.Length; i++)
            points.Add(GO("PuntoPatrulla" + (i + 1), pointsRoot, route[i]).transform);

        GameObject enemy = GO(spec.Name, parent, start);
        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        enemy.AddComponent<CircleCollider2D>().radius = 0.35f;

        bool animated = spec.Animator != null;
        Vector2 bodySize = animated ? (spec.BodySize.sqrMagnitude > 0.01f ? spec.BodySize : Vector2.one) : spec.BodySize;
        SpriteRenderer body = Visual(enemy.transform, EcosAssetFactory.Circle,
            animated ? Color.white : spec.BodyColor,
            bodySize, 12, "Cuerpo");
        if (!animated)
            Visual(body.transform, EcosAssetFactory.Circle, spec.DetailColor, new Vector2(0.9f, 0.4f), 13, "Detalle", new Vector2(0f, 0.32f));
        SpriteRenderer cone = Visual(enemy.transform, EcosAssetFactory.Cone, spec.ConeColor, new Vector2(spec.ViewDistance, spec.ViewDistance), 5, "ConoVision");

        GameObject hurtbox = GO("ZonaDano", enemy.transform);
        var hurtCollider = hurtbox.AddComponent<CircleCollider2D>();
        hurtCollider.isTrigger = true;
        hurtCollider.radius = 0.55f;
        var contact = hurtbox.AddComponent<EnemyContactDamage>();

        AudioSource voice = spec.Voice != null ? MakeAudio(enemy, spec.Voice, spec.IdleVoiceVolume, true, false, true) : null;

        var detection = enemy.AddComponent<EnemyDetection>();
        Set(detection, "viewDistance", spec.ViewDistance);
        Set(detection, "viewAngle", spec.ViewAngle);
        Set(detection, "hearingMultiplier", spec.Hearing);

        var patrol = enemy.AddComponent<EnemyPatrol>();
        Set(patrol, "points", points.ToArray());

        var chase = enemy.AddComponent<EnemyChase>();
        Set(chase, "chaseSpeed", spec.ChaseSpeed);
        Set(chase, "closeChaseDistance", spec.CloseChaseDistance);
        Set(chase, "loseSightTime", spec.LoseSightTime);
        Set(chase, "searchDuration", spec.SearchDuration);
        Set(chase, "searchRadius", spec.SearchRadius);

        var brain = enemy.AddComponent<EnemyBrain>();
        Set(brain, "patrolSpeed", spec.PatrolSpeed);
        Set(brain, "investigateSpeed", spec.InvestigateSpeed);
        Set(brain, "startActive", spec.StartActive);
        Set(brain, "startPatrolling", spec.StartPatrolling);
        Set(brain, "vanishOnReturn", spec.VanishOnReturn);
        Set(brain, "retreatAfterHit", spec.RetreatAfterHit);
        Set(brain, "stunAfterHit", spec.StunAfterHit);
        Set(brain, "wakeOnFlag", spec.WakeOnFlag ?? string.Empty);
        Set(brain, "navGraph", spec.Nav);
        Set(brain, "body", body);
        Set(brain, "visionCone", cone.transform);
        Set(brain, "voice", voice);
        Set(brain, "idleVoiceVolume", spec.IdleVoiceVolume);
        Set(contact, "brain", brain);

        if (animated)
        {
            var animator = enemy.AddComponent<Animator>();
            animator.runtimeAnimatorController = spec.Animator;
            animator.applyRootMotion = false;
            var enemyAnim = enemy.AddComponent<EnemyAnimation>();
            Set(enemyAnim, "animator", animator);
            Set(enemyAnim, "brain", brain);
            Set(enemyAnim, "body", rb);
        }

        return brain;
    }

    public static NavGraph BuildNavGraph(Transform parent, Vector2[] nodes)
    {
        GameObject go = GO("RedNavegacion", parent);
        var transforms = new Object[nodes.Length];
        for (int i = 0; i < nodes.Length; i++)
            transforms[i] = GO("Nodo" + (i + 1), go.transform, nodes[i]).transform;
        var graph = go.AddComponent<NavGraph>();
        Set(graph, "nodes", transforms);
        return graph;
    }
}
