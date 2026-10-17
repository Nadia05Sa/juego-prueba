using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Genera spritesheets, clips y Animator Controllers
/// (jugador, La Llorona, momia, Charro Negro).
/// </summary>
public static class EcosAnimationFactory
{
    public const string CharactersFolder = "Assets/Art/Characters";
    public const string AnimFolder = "Assets/Art/Animations";

    public static RuntimeAnimatorController PlayerController { get; private set; }
    public static RuntimeAnimatorController LloronaController { get; private set; }
    public static RuntimeAnimatorController MummyController { get; private set; }
    public static RuntimeAnimatorController CharroController { get; private set; }
    public static RuntimeAnimatorController DoorController { get; private set; }

    public static void CreateAll()
    {
        EnsureFolder(CharactersFolder);
        EnsureFolder(AnimFolder);

        PlayerController = BuildDirectionalController(
            "Player",
            DrawPlayer,
            new Color(0.82f, 0.68f, 0.48f),
            new Color(0.16f, 0.1f, 0.07f),
            new Color(0.35f, 0.22f, 0.14f));

        LloronaController = BuildDirectionalController(
            "Llorona",
            DrawLlorona,
            new Color(0.92f, 0.95f, 1f),
            new Color(0.04f, 0.04f, 0.07f),
            new Color(0.65f, 0.8f, 1f),
            cellSize: 48);

        // wrap=crema, rag=marrón, accent=ojos (discretos)
        MummyController = BuildDirectionalController(
            "Momia",
            DrawMummy,
            new Color(0.86f, 0.78f, 0.58f),
            new Color(0.42f, 0.32f, 0.2f),
            new Color(0.75f, 0.18f, 0.12f),
            cellSize: 48);

        // suit=#171513, hat=#38291F, silver/gold=#A77A38
        CharroController = BuildDirectionalController(
            "Charro",
            DrawCharro,
            new Color(0.09f, 0.08f, 0.07f),
            new Color(0.22f, 0.16f, 0.12f),
            new Color(0.65f, 0.48f, 0.22f),
            cellSize: 48);

        // Puertas: ya no se genera Animator (vuelven al fade simple).
        DoorController = null;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ---------- Controllers direccionales ----------

    private delegate void FramePainter(Color[] pixels, int cell, int ox, int oy, int dir, int frame, Color body, Color hair, Color accent);

    private static RuntimeAnimatorController BuildDirectionalController(
        string name, FramePainter paint, Color body, Color hair, Color accent, int cellSize = 32)
    {
        int cell = cellSize;
        string[] dirNames = { "Down", "Up", "Left", "Right" };
        var sprites = new Sprite[16];
        for (int d = 0; d < 4; d++)
        for (int f = 0; f < 4; f++)
            sprites[d * 4 + f] = WriteFrame($"{CharactersFolder}/{name}_{dirNames[d]}_{f}.png", cell,
                (px, c) => paint(px, c, 0, 0, d, f, body, hair, accent));

        var idleClips = new AnimationClip[4];
        var walkClips = new AnimationClip[4];
        for (int d = 0; d < 4; d++)
        {
            idleClips[d] = MakeSpriteClip($"{AnimFolder}/{name}_Idle_{dirNames[d]}.anim", "Cuerpo",
                new[] { sprites[d * 4] }, 4f, true);
            walkClips[d] = MakeSpriteClip($"{AnimFolder}/{name}_Walk_{dirNames[d]}.anim", "Cuerpo",
                new[] { sprites[d * 4 + 1], sprites[d * 4 + 2], sprites[d * 4 + 3], sprites[d * 4 + 2] }, 8f, true);
        }

        string controllerPath = $"{AnimFolder}/{name}.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null) AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("State", AnimatorControllerParameterType.Int);
        controller.AddParameter("Chase", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        BlendTree idleTree = CreateDirectionalTree(controller, "IdleTree", idleClips);
        BlendTree walkTree = CreateDirectionalTree(controller, "WalkTree", walkClips);

        AnimatorState idle = sm.AddState("Idle");
        idle.motion = idleTree;
        AnimatorState walk = sm.AddState("Walk");
        walk.motion = walkTree;
        sm.defaultState = idle;

        AnimatorStateTransition toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.08f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");

        AnimatorStateTransition toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.08f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static BlendTree CreateDirectionalTree(AnimatorController controller, string name, AnimationClip[] clips)
    {
        var tree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveY",
            hideFlags = HideFlags.HideInHierarchy
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        // Down, Up, Left, Right
        tree.AddChild(clips[0], new Vector2(0f, -1f));
        tree.AddChild(clips[1], new Vector2(0f, 1f));
        tree.AddChild(clips[2], new Vector2(-1f, 0f));
        tree.AddChild(clips[3], new Vector2(1f, 0f));
        return tree;
    }

    // ---------- Puerta ----------

    private static RuntimeAnimatorController BuildDoorController()
    {
        string closedPath = $"{AnimFolder}/Door_Closed.anim";
        string openingPath = $"{AnimFolder}/Door_Opening.anim";
        string openPath = $"{AnimFolder}/Door_Open.anim";

        AnimationClip closed = MakeTransformClip(closedPath, "Visual", 0f, 1f, 1f);
        AnimationClip opening = MakeDoorSwingClip(openingPath);
        AnimationClip open = MakeTransformClip(openPath, "Visual", -75f, 0.15f, 0.85f);

        string controllerPath = $"{AnimFolder}/Door.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            AssetDatabase.DeleteAsset(controllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState closedState = sm.AddState("Closed");
        closedState.motion = closed;
        AnimatorState openingState = sm.AddState("Opening");
        openingState.motion = opening;
        AnimatorState openState = sm.AddState("Open");
        openState.motion = open;
        sm.defaultState = closedState;

        AnimatorStateTransition startOpen = closedState.AddTransition(openingState);
        startOpen.hasExitTime = false;
        startOpen.duration = 0f;
        startOpen.AddCondition(AnimatorConditionMode.If, 0f, "Open");

        AnimatorStateTransition finishOpen = openingState.AddTransition(openState);
        finishOpen.hasExitTime = true;
        finishOpen.exitTime = 0.95f;
        finishOpen.duration = 0.05f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip MakeDoorSwingClip(string path)
    {
        var clip = new AnimationClip { frameRate = 30f, name = Path.GetFileNameWithoutExtension(path) };
        // Rotación Z 0 -> -75
        var rotCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.55f, -75f));
        clip.SetCurve("Visual", typeof(Transform), "localEulerAngles.z", rotCurve);

        var scaleX = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.55f, 0.2f));
        var scaleY = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.55f, 0.9f));
        clip.SetCurve("Visual", typeof(Transform), "localScale.x", scaleX);
        clip.SetCurve("Visual", typeof(Transform), "localScale.y", scaleY);

        SaveClip(clip, path);
        return clip;
    }

    private static AnimationClip MakeTransformClip(string path, string child, float angleZ, float scaleX, float scaleY)
    {
        var clip = new AnimationClip { frameRate = 12f, name = Path.GetFileNameWithoutExtension(path) };
        clip.SetCurve(child, typeof(Transform), "localEulerAngles.z", AnimationCurve.Constant(0f, 0.1f, angleZ));
        clip.SetCurve(child, typeof(Transform), "localScale.x", AnimationCurve.Constant(0f, 0.1f, scaleX));
        clip.SetCurve(child, typeof(Transform), "localScale.y", AnimationCurve.Constant(0f, 0.1f, scaleY));
        SaveClip(clip, path);
        return clip;
    }

    // ---------- Clips de sprite ----------

    private static AnimationClip MakeSpriteClip(string path, string childPath, Sprite[] frames, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps, name = Path.GetFileNameWithoutExtension(path) };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = childPath,
            propertyName = "m_Sprite"
        };
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        SaveClip(clip, path);
        return clip;
    }

    private static void SaveClip(AnimationClip clip, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
    }

    // ---------- Frames PNG ----------

    private static Sprite WriteFrame(string path, int cell, System.Action<Color[], int> paint)
    {
        var pixels = new Color[cell * cell];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        paint(pixels, cell);

        var tex = new Texture2D(cell, cell, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = cell;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- Pintores ----------

    private static void DrawPlayer(Color[] px, int cell, int ox, int oy, int dir, int frame, Color skin, Color hair, Color cloth)
    {
        // Investigadora top-down: torso, cabeza, piernas y lámpara (reposo = frame 0).
        int walk = frame == 0 ? 0 : (frame == 2 ? 0 : (frame % 2 == 1 ? -1 : 1));
        int lampBob = frame == 0 ? 0 : (frame % 2 == 0 ? 1 : -1);

        FillDisc(px, cell, 16, 23 + walk, 8, new Color(0f, 0f, 0f, 0.28f)); // sombra
        // Piernas
        FillRect(px, cell, 11, 19 + walk, 4, 7, cloth * 0.65f);
        FillRect(px, cell, 17, 19 - walk, 4, 7, cloth * 0.65f);
        // Torso / abrigo
        FillRect(px, cell, 10, 11, 12, 11, cloth);
        FillRect(px, cell, 12, 12, 8, 3, cloth * 1.15f);
        // Cabeza + cabello
        FillDisc(px, cell, 16, 8, 5, skin);
        FillDisc(px, cell, 16, 6, 5, hair);
        FillRect(px, cell, 12, 5, 3, 6, hair);
        FillRect(px, cell, 17, 5, 3, 6, hair);
        // Brazo + lámpara según dirección
        Vector2Int lamp = dir switch
        {
            1 => new Vector2Int(16, 3 + lampBob),      // arriba
            2 => new Vector2Int(7, 14 + lampBob),      // izquierda
            3 => new Vector2Int(25, 14 + lampBob),     // derecha
            _ => new Vector2Int(16, 26 + lampBob)      // abajo
        };
        FillRect(px, cell, lamp.x - 1, Mathf.Clamp(lamp.y - 3, 0, cell - 1), 2, 4, skin * 0.9f);
        FillDisc(px, cell, lamp.x, lamp.y, 3, new Color(1f, 0.9f, 0.45f, 0.9f));
        FillDisc(px, cell, lamp.x, lamp.y, 1, new Color(1f, 0.98f, 0.75f, 1f));
    }

    private static void DrawLlorona(Color[] px, int cell, int ox, int oy, int dir, int frame, Color dress, Color hair, Color accent)
    {
        // Silueta fantasma más alta y legible (vista top-down).
        float t = cell / 48f;
        int c = Mathf.RoundToInt(24 * t);
        int sway = frame == 0 ? 0 : (frame % 2 == 0 ? 2 : -2);
        int walk = frame == 0 ? 0 : (frame == 2 ? 0 : sway);

        // Aura
        FillDisc(px, cell, c, c + 4, Mathf.RoundToInt(16 * t), new Color(0.45f, 0.65f, 1f, 0.18f));
        FillDisc(px, cell, c, c + 2, Mathf.RoundToInt(12 * t), new Color(0.7f, 0.85f, 1f, 0.22f));

        // Vestido amplio (cola)
        FillDisc(px, cell, c + walk, c + Mathf.RoundToInt(10 * t), Mathf.RoundToInt(11 * t), new Color(dress.r, dress.g, dress.b, 0.95f));
        FillRect(px, cell, c - Mathf.RoundToInt(9 * t) + sway, c + Mathf.RoundToInt(2 * t),
            Mathf.RoundToInt(18 * t), Mathf.RoundToInt(16 * t), new Color(dress.r, dress.g, dress.b, 0.9f));
        // Pliegues
        FillRect(px, cell, c - 2 + sway, c + Mathf.RoundToInt(6 * t), 2, Mathf.RoundToInt(10 * t), new Color(0.75f, 0.8f, 0.95f, 0.5f));

        // Torso / cabeza
        FillDisc(px, cell, c, c - Mathf.RoundToInt(2 * t), Mathf.RoundToInt(6 * t), new Color(0.96f, 0.97f, 1f));
        // Cabello largo negro
        FillDisc(px, cell, c, c - Mathf.RoundToInt(4 * t), Mathf.RoundToInt(7 * t), hair);
        FillRect(px, cell, c - Mathf.RoundToInt(8 * t), c - Mathf.RoundToInt(2 * t), Mathf.RoundToInt(5 * t), Mathf.RoundToInt(14 * t), hair);
        FillRect(px, cell, c + Mathf.RoundToInt(3 * t), c - Mathf.RoundToInt(2 * t), Mathf.RoundToInt(5 * t), Mathf.RoundToInt(14 * t), hair);
        // Mechones delanteros
        FillRect(px, cell, c - Mathf.RoundToInt(3 * t), c - Mathf.RoundToInt(1 * t), 2, Mathf.RoundToInt(8 * t), hair);
        FillRect(px, cell, c + Mathf.RoundToInt(1 * t), c - Mathf.RoundToInt(1 * t), 2, Mathf.RoundToInt(8 * t), hair);

        // Ojos brillantes
        FillRect(px, cell, c - 3, c - Mathf.RoundToInt(2 * t), 2, 2, new Color(0.7f, 0.9f, 1f, 1f));
        FillRect(px, cell, c + 2, c - Mathf.RoundToInt(2 * t), 2, 2, new Color(0.7f, 0.9f, 1f, 1f));

        // Brillo del vestido
        FillDisc(px, cell, c + sway, c + Mathf.RoundToInt(8 * t), Mathf.RoundToInt(4 * t), new Color(accent.r, accent.g, accent.b, 0.4f));
    }

    private static void DrawMummy(Color[] px, int cell, int ox, int oy, int dir, int frame, Color wrap, Color rag, Color accent)
    {
        // Top-down con silueta humana: cabeza grande inclinada, pecho/cintura, brazo irregular.
        float s = cell / 48f;
        int c = Mathf.RoundToInt(24 * s);
        int bob = frame == 0 ? 0 : (frame % 2 == 0 ? 1 : -1);
        int drag = frame == 0 ? 0 : (frame == 1 ? 2 : (frame == 3 ? -2 : 0)); // arrastre de pierna
        int armDrop = frame == 0 ? 1 : (frame % 2 == 0 ? 3 : 0); // un brazo más bajo

        Color cream = new Color(0.9f, 0.84f, 0.68f, 1f);
        Color sand = wrap;
        Color brown = rag;
        Color shade = new Color(0.32f, 0.24f, 0.16f, 0.85f);

        // Sombra en suelo
        FillDisc(px, cell, c + bob, c + Mathf.RoundToInt(14 * s), Mathf.RoundToInt(11 * s), new Color(0.08f, 0.06f, 0.04f, 0.28f));

        // Piernas (postura irregular)
        FillRect(px, cell, c - Mathf.RoundToInt(5 * s) + drag, c + Mathf.RoundToInt(10 * s),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(10 * s), sand);
        FillRect(px, cell, c + Mathf.RoundToInt(2 * s) - drag, c + Mathf.RoundToInt(11 * s),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(9 * s), brown);

        // Cadera / cintura (más estrecha)
        FillDisc(px, cell, c + bob, c + Mathf.RoundToInt(9 * s), Mathf.RoundToInt(6 * s), sand);
        FillRect(px, cell, c - Mathf.RoundToInt(5 * s), c + Mathf.RoundToInt(7 * s),
            Mathf.RoundToInt(10 * s), Mathf.RoundToInt(4 * s), shade);

        // Pecho / hombros (más ancho)
        FillDisc(px, cell, c, c + Mathf.RoundToInt(3 * s), Mathf.RoundToInt(8 * s), cream);
        FillRect(px, cell, c - Mathf.RoundToInt(8 * s), c + Mathf.RoundToInt(1 * s),
            Mathf.RoundToInt(16 * s), Mathf.RoundToInt(7 * s), sand);
        // Sombra de pecho
        FillRect(px, cell, c - Mathf.RoundToInt(3 * s), c + Mathf.RoundToInt(2 * s),
            Mathf.RoundToInt(6 * s), Mathf.RoundToInt(4 * s), new Color(0.55f, 0.45f, 0.3f, 0.55f));

        // Brazo izquierdo más bajo (arrastre)
        FillRect(px, cell, c - Mathf.RoundToInt(11 * s), c + Mathf.RoundToInt(2 * s) + armDrop,
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(9 * s), cream);
        FillRect(px, cell, c + Mathf.RoundToInt(8 * s), c + Mathf.RoundToInt(1 * s),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(7 * s), sand);

        // Vendas cruzadas (crema / arena / marrón)
        for (int i = 0; i < 5; i++)
        {
            Color band = i % 3 == 0 ? cream : (i % 3 == 1 ? sand : brown);
            int y = c + Mathf.RoundToInt((i * 2.4f) * s);
            FillRect(px, cell, c - Mathf.RoundToInt(7 * s) + (i % 2), y, Mathf.RoundToInt(14 * s), 2, band);
        }
        // Venda diagonal
        for (int i = 0; i < 6; i++)
            FillRect(px, cell, c - Mathf.RoundToInt(6 * s) + i, c + Mathf.RoundToInt((-1 + i) * s), 2, 2, cream);

        // Tiras deshilachadas (hombro, cintura, brazo)
        FillRect(px, cell, c - Mathf.RoundToInt(10 * s), c + Mathf.RoundToInt(4 * s), 2, Mathf.RoundToInt(8 * s), cream);
        FillRect(px, cell, c + Mathf.RoundToInt(9 * s), c + Mathf.RoundToInt(6 * s), 2, Mathf.RoundToInt(7 * s), brown);
        FillRect(px, cell, c - Mathf.RoundToInt(2 * s), c + Mathf.RoundToInt(12 * s), 2, Mathf.RoundToInt(6 * s), sand);

        // Cabeza más grande, ligeramente inclinada
        int hx = c + 1;
        int hy = c - Mathf.RoundToInt(4 * s);
        FillDisc(px, cell, hx, hy, Mathf.RoundToInt(7 * s), sand);
        FillDisc(px, cell, hx - 1, hy - 1, Mathf.RoundToInt(6 * s), cream);
        // Cueva oscura de ojos
        FillDisc(px, cell, hx, hy + 1, Mathf.RoundToInt(3 * s), new Color(0.08f, 0.05f, 0.04f, 1f));
        // Ojos rojos discretos (1 px)
        FillRect(px, cell, hx - 2, hy + 1, 1, 1, accent);
        FillRect(px, cell, hx + 1, hy + 1, 1, 1, accent);
        // Venda colgante de la cabeza
        FillRect(px, cell, hx - Mathf.RoundToInt(6 * s), hy + 2, 2, Mathf.RoundToInt(7 * s), cream);
    }

    private static void DrawCharro(Color[] px, int cell, int ox, int oy, int dir, int frame, Color suit, Color hat, Color gold)
    {
        // Misma proporción que la momia; identidad = sombrero, traje y cara oculta.
        float s = cell / 48f;
        int c = Mathf.RoundToInt(24 * s);
        int bob = frame == 0 ? 0 : (frame % 2 == 0 ? 1 : -1);
        int step = frame == 0 ? 0 : (frame == 1 ? 2 : (frame == 3 ? -2 : 0));
        Color charcoal = suit;                                    // #171513
        Color brown = hat;                                        // #38291F
        Color agedGold = gold;                                    // #A77A38
        Color deepRed = new Color(0.39f, 0.12f, 0.14f, 1f);      // #641F24
        Color boot = new Color(0.12f, 0.1f, 0.09f, 1f);

        // Aura / sombra
        FillDisc(px, cell, c + bob, c + Mathf.RoundToInt(13 * s), Mathf.RoundToInt(12 * s), new Color(0.05f, 0.02f, 0.02f, 0.32f));

        // Piernas + botonadura dorada en el costado
        FillRect(px, cell, c - Mathf.RoundToInt(4 * s) + step, c + Mathf.RoundToInt(9 * s),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(11 * s), charcoal);
        FillRect(px, cell, c + Mathf.RoundToInt(1 * s) - step, c + Mathf.RoundToInt(10 * s),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(10 * s), charcoal);
        for (int i = 0; i < 5; i++)
            FillRect(px, cell, c + Mathf.RoundToInt(4 * s), c + Mathf.RoundToInt((10 + i * 2) * s), 1, 1, agedGold);
        // Botas
        FillRect(px, cell, c - Mathf.RoundToInt(4 * s) + step, c + Mathf.RoundToInt(18 * s), Mathf.RoundToInt(4 * s), 3, boot);
        FillRect(px, cell, c + Mathf.RoundToInt(1 * s) - step, c + Mathf.RoundToInt(18 * s), Mathf.RoundToInt(4 * s), 3, boot);

        // Cinturón + hebilla
        FillRect(px, cell, c - Mathf.RoundToInt(6 * s), c + Mathf.RoundToInt(8 * s), Mathf.RoundToInt(12 * s), 3, brown);
        FillRect(px, cell, c - 1, c + Mathf.RoundToInt(8 * s), 3, 3, agedGold);

        // Torso / chaqueta (casi negro; borde iluminado)
        FillDisc(px, cell, c, c + Mathf.RoundToInt(3 * s), Mathf.RoundToInt(7 * s), charcoal);
        FillRect(px, cell, c - Mathf.RoundToInt(7 * s) + bob, c, Mathf.RoundToInt(14 * s), Mathf.RoundToInt(8 * s), charcoal);
        FillRect(px, cell, c - Mathf.RoundToInt(7 * s), c + 1, 2, Mathf.RoundToInt(7 * s), brown); // borde linterna
        // Bordado hombro
        FillRect(px, cell, c - Mathf.RoundToInt(6 * s), c + Mathf.RoundToInt(1 * s), Mathf.RoundToInt(5 * s), 1, agedGold);

        // Moño rojo
        FillRect(px, cell, c - 2, c + Mathf.RoundToInt(1 * s), 5, 3, deepRed);

        // Brazos largos y bajos
        FillRect(px, cell, c - Mathf.RoundToInt(11 * s), c + Mathf.RoundToInt(2 * s) + (frame % 2),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(10 * s), charcoal);
        FillRect(px, cell, c + Mathf.RoundToInt(8 * s), c + Mathf.RoundToInt(3 * s),
            Mathf.RoundToInt(4 * s), Mathf.RoundToInt(9 * s), charcoal);
        // Manos / garras
        FillRect(px, cell, c - Mathf.RoundToInt(11 * s), c + Mathf.RoundToInt(11 * s), 3, 2, brown);
        FillRect(px, cell, c + Mathf.RoundToInt(9 * s), c + Mathf.RoundToInt(11 * s), 3, 2, brown);
        // Bordado manga
        FillRect(px, cell, c - Mathf.RoundToInt(10 * s), c + Mathf.RoundToInt(4 * s), 1, Mathf.RoundToInt(5 * s), agedGold);

        // Cabeza bajo sombra (casi negra)
        FillDisc(px, cell, c, c - Mathf.RoundToInt(2 * s), Mathf.RoundToInt(5 * s), new Color(0.06f, 0.05f, 0.05f));

        // Sombrero ancho (rasgo principal) — sobresale a los lados
        FillDisc(px, cell, c + bob, c - Mathf.RoundToInt(4 * s), Mathf.RoundToInt(11 * s), charcoal);
        FillRect(px, cell, c - Mathf.RoundToInt(12 * s), c - Mathf.RoundToInt(5 * s),
            Mathf.RoundToInt(24 * s), Mathf.RoundToInt(3 * s), charcoal);
        // Ala con borde dorado discreto
        FillRect(px, cell, c - Mathf.RoundToInt(11 * s), c - Mathf.RoundToInt(5 * s), Mathf.RoundToInt(22 * s), 1, agedGold);
        // Copa
        FillDisc(px, cell, c, c - Mathf.RoundToInt(7 * s), Mathf.RoundToInt(5 * s), brown);
        FillRect(px, cell, c - Mathf.RoundToInt(4 * s), c - Mathf.RoundToInt(5 * s), Mathf.RoundToInt(8 * s), 2, agedGold);

        // Ojos ámbar/rojos muy pequeños bajo el ala
        FillRect(px, cell, c - 2, c - Mathf.RoundToInt(1 * s), 1, 1, new Color(0.95f, 0.45f, 0.15f));
        FillRect(px, cell, c + 1, c - Mathf.RoundToInt(1 * s), 1, 1, new Color(0.95f, 0.45f, 0.15f));
    }

    private static void FillRect(Color[] px, int cell, int x, int y, int w, int h, Color c)
    {
        for (int j = 0; j < h; j++)
        for (int i = 0; i < w; i++)
        {
            int sx = x + i;
            int sy = y + j;
            if (sx < 0 || sy < 0 || sx >= cell || sy >= cell) continue;
            int idx = sy * cell + sx;
            px[idx] = AlphaOver(px[idx], c);
        }
    }

    private static void FillDisc(Color[] px, int cell, int cx, int cy, int r, Color c)
    {
        for (int j = -r; j <= r; j++)
        for (int i = -r; i <= r; i++)
        {
            if (i * i + j * j > r * r) continue;
            int sx = cx + i;
            int sy = cy + j;
            if (sx < 0 || sy < 0 || sx >= cell || sy >= cell) continue;
            int idx = sy * cell + sx;
            px[idx] = AlphaOver(px[idx], c);
        }
    }

    private static Color AlphaOver(Color under, Color over)
    {
        float a = over.a + under.a * (1f - over.a);
        if (a <= 0.001f) return Color.clear;
        return new Color(
            (over.r * over.a + under.r * under.a * (1f - over.a)) / a,
            (over.g * over.a + under.g * under.a * (1f - over.a)) / a,
            (over.b * over.a + under.b * under.a * (1f - over.a)) / a,
            a);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
