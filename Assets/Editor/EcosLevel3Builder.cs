using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static EcosBuildUtils;
using static EcosLevelKit;

/// <summary>
/// Nivel 3 — El pacto del Charro Negro. Patio trasero → caballerizas, trojes, cuarto de registros
/// y cuarto de herramientas (tres documentos y una barreta) → palanca → persecución → portón principal.
/// </summary>
public static class EcosLevel3Builder
{
    private static LevelMap map;

    private static Vector2 Cell(float x, float row) => map.Cell(x, row);

    private static LevelMap BuildLayout()
    {
        var m = new LevelMap(48, 32);

        m.Fill(2, 2, 22, 18, 'h');            // Caballerizas
        foreach (int px in new[] { 7, 13, 18 })
        {
            m.Fill(px, 2, px, 7, 'W');        // Tabiques norte
            m.Fill(px, 13, px, 18, 'W');      // Tabiques sur
        }

        m.Fill(24, 2, 34, 18, ':');           // Trojes
        m.Fill(23, 9, 23, 11, ':');           // Paso caballerizas → trojes
        m.Fill(25, 5, 31, 5, 'X');            // Estantes y cajas
        m.Fill(26, 9, 32, 9, 'X');
        m.Fill(25, 13, 31, 13, 'X');
        m.Fill(26, 16, 33, 16, 'X');

        m.Fill(36, 2, 45, 9, ':');            // Cuarto de herramientas
        m.Fill(35, 5, 35, 6, ':');
        m.Fill(36, 11, 45, 18, ':');          // Cuarto de registros
        m.Fill(35, 14, 35, 15, ':');

        m.Fill(2, 20, 30, 30, 'd');           // Patio trasero
        m.Fill(9, 19, 10, 19, 'h');           // Puerta caballerizas → patio
        m.Fill(28, 19, 29, 19, ':');          // Puerta trojes → patio
        m.Fill(31, 25, 43, 28, 'd');          // Camino al portón
        m.Fill(44, 26, 47, 27, 'd');          // Portón principal

        m.Fill(14, 23, 17, 23, 'x');          // Bebedero
        m.Fill(6, 27, 7, 28, 'x');            // Pozo
        return m;
    }

    private static Dictionary<char, (TileBase, TileBase)> Palette() => new Dictionary<char, (TileBase, TileBase)>
    {
        { '#', (EcosAssetFactory.StableWallTile, null) },
        { 'W', (EcosAssetFactory.StallTile, EcosAssetFactory.HayTile) },
        { 'h', (null, EcosAssetFactory.HayTile) },
        { ':', (null, EcosAssetFactory.BarnFloorTile) },
        { 'd', (null, EcosAssetFactory.DirtTile) },
        { 'X', (EcosAssetFactory.CrateTile, EcosAssetFactory.BarnFloorTile) },
        { 'x', (EcosAssetFactory.CrateTile, EcosAssetFactory.DirtTile) }
    };

    public static void Build()
    {
        map = BuildLayout();
        var setup = new LevelSetup
        {
            Title = "Nivel 3 — El pacto del Charro Negro",
            StartHint = "F — Linterna. Empieza en el cuarto de registros (puerta al ESTE del patio). Lee el libro del escritorio.",
            TutorialExtra =
                "Mapa del patio (da igual por dónde entres):\n" +
                "• ESTE — cuartos de registros y herramientas · portón de salida\n" +
                "• OESTE / NORTE — caballerizas: carta en la paja (F) · palanca del portón\n" +
                "• TROJES — pasillo entre caballerizas y cuartos: nota del caballerango\n\n" +
                "Flujo: registro → 3 documentos → archivero (llave) → barreta → palanca oeste → portón este.\n" +
                "Si te persigue el Charro: F lo frena un momento; Q para esconderte.",
            NextScene = VictoryScene,
            ExitMessage = "Cruzas el portón principal. Detrás de ti, los cascos se detienen de golpe.",
            CompleteTitle = "Escapaste",
            CompleteText = "El portón se cierra a tus espaldas.\nCon el medallón unido, el acuerdo queda roto.",
            GameOverText = "Los cascos se detienen justo detrás de ti.",
            GlobalLightColor = new Color(0.45f, 0.5f, 0.75f),
            GlobalLightIntensity = 0.2f,
            Ambience = EcosAssetFactory.StableAmbience,
            PlayerLightRadius = 3f,
            PlayerLightIntensity = 0.7f,
            Flashlight = true
        };

        LevelContext ctx = BeginLevel(map, Palette(), Cell(4.5f, 24f), setup);

        GameObject outside = GO("LimiteExterior", null, new Vector2(48.5f, Cell(0f, 26.5f).y));
        Box(outside, new Vector2(1f, 4f), false);

        BuildYard(ctx);
        BuildStables(ctx);
        BuildGranary(ctx);
        BuildRecordsAndTools(ctx);
        BuildCharro(ctx);

        SetObjectives(ctx.LevelManager, new[]
        {
            ("Lee el registro (cuarto al este del patio)", ObjectiveCondition.Flag, "registro_hacienda", 0),
            ("Documentos del pacto ({0}/3): registro · paja con F · nota en trojes", ObjectiveCondition.ClueCount, "", 3),
            ("Archivero → llave → barreta (cuarto de herramientas)", ObjectiveCondition.HasItem, "barreta", 0),
            ("Fuerza la palanca (fondo oeste de las caballerizas)", ObjectiveCondition.Flag, "palanca_activada", 0),
            ("¡Corre al portón principal (este del patio)!", ObjectiveCondition.Flag, "nivel_completado", 0)
        });

        Save(ctx.Scene, Level3Scene);
    }

    private static void BuildYard(LevelContext ctx)
    {
        Transform yard = Area(ctx, "PatioTrasero");

        CheckpointAt(yard, Cell(4.5f, 24f));
        Lamp(yard, Cell(10f, 21f), 4f, 0.9f);
        Lamp(yard, Cell(25f, 24f), 4f, 0.9f);
        Lamp(yard, Cell(38f, 25.4f), 3.5f, 0.8f);

        EcosFurniture.DryWell(yard, "PozoExaminable", Cell(6.5f, 27.5f),
            "Un pozo seco. En el fondo brilla algo metálico: botones de plata, como los de un traje de charro.");

        GameObject trough = EcosFurniture.Desk(yard, "BebederoExaminable", Cell(15.5f, 23f), new Vector2(3.2f, 1.1f));
        // Retinte visual hacia metal/agua del bebedero.
        foreach (SpriteRenderer r in trough.GetComponentsInChildren<SpriteRenderer>())
        {
            if (r.name == "Sombra") continue;
            r.color = new Color(0.35f, 0.38f, 0.4f, r.color.a);
        }
        Examinable(trough, "Examinar el bebedero",
            "El agua del bebedero está quieta, pero en el lodo hay huellas de herraduras recientes.",
            new Vector2(5f, 2.6f));

        EcosFurniture.Cart(yard, "Escondite_Carreta", "debajo de una carreta", Cell(22f, 27.5f), new Vector2(2.4f, 1.4f));
        EcosFurniture.HayBale(yard, "Escondite_Pacas", "detrás de unas pacas de heno", Cell(29.5f, 29.5f), new Vector2(1.4f, 1.2f));
        Battery(yard, "Baterias_Patio", Cell(26f, 29.5f));

        // Señales de orientación: el jugador puede empezar en cualquier zona.
        PlaceYardSign(yard, "SenalRegistros", Cell(32f, 22.5f),
            "Leer el cartel",
            "Cartel de madera:\n→ ESTE: Cuarto de registros y herramientas\n→ ESTE (más lejos): Portón principal\n← OESTE: Caballerizas (carta en la paja, palanca)\n↑ TROJES: Nota del caballerango");
        PlaceYardSign(yard, "SenalCaballerizas", Cell(11f, 20.5f),
            "Leer el letrero",
            "Letrero del establo:\nCABALLERIZAS\n• Carta del pacto: caballeriza norte, entre la paja (alumbra con F)\n• Palanca del portón: fondo oeste\n• Salida al patio: sur");
        PlaceYardSign(yard, "SenalPorton", Cell(38f, 26.5f),
            "Leer el aviso del portón",
            "Aviso junto al portón:\n«Sin cerradura. Se abre desde la palanca de las caballerizas (oeste).\nNecesitas los documentos del pacto y una barreta.»");

        DoorController gate = Door(yard, "PortonPrincipal", Cell(45f, 26.5f), true, new Color(0.18f, 0.16f, 0.15f),
            DoorState.BloqueadaPorObjetivo, "Abrir el portón",
            "El portón no tiene cerradura. Lo mueve la palanca al oeste de las caballerizas (hace falta barreta y los 3 documentos).");
        Set(gate, "requiredFlag", "palanca_activada");
        Set(gate, "autoOpenOnFlag", true);
        Set(gate, "noiseRadiusOnOpen", 6f);
        Exit(yard, Cell(47f, 26.5f), new Vector2(1f, 2f), "palanca_activada", "El portón sigue cerrado.");

        GameObject glimpse = Silhouette(yard, "SiluetaPatio", Cell(27f, 21f));
        var g = glimpse.AddComponent<ApparitionGlimpse>();
        Set(g, "renderers", glimpse.GetComponentsInChildren<SpriteRenderer>());
        Set(g, "vanishDistance", 6f);
        Set(g, "message", "Una silueta con sombrero ancho te observa desde la oscuridad... y se desvanece.");
        Set(g, "hideOnFlag", "registro_hacienda");
        Set(g, "sound", MakeAudio(glimpse, EcosAssetFactory.Hooves, 0.7f, false, false, true));
    }

    private static void BuildStables(LevelContext ctx)
    {
        Transform stables = Area(ctx, "Caballerizas");

        Lamp(stables, Cell(12f, 10f), 3.5f, 0.8f);
        CheckpointAt(stables, Cell(4.5f, 10.5f));

        Lever lever = MakeLever(stables, "PalancaPorton", Cell(2.6f, 10.5f), "Forzar la palanca del portón");
        Set(lever, "requiredClues", 3);
        Set(lever, "requiredCluesName", "los documentos del pacto");
        Set(lever, "requiredItemId", "barreta");
        Set(lever, "requiredItemName", "una barreta para forzarla");
        Set(lever, "lockedPrefix", "La palanca está oxidada y trabada. Te falta: ");
        Set(lever, "flagOnActivate", "palanca_activada");
        Set(lever, "noiseRadius", 14f);
        Set(lever, "activatedMessage", "Fuerzas la palanca con la barreta. Un estruendo de cadenas recorre la hacienda: el portón principal se abre.");
        Set(lever, "usedPrompt", "Examinar la palanca");
        Set(lever, "usedMessage", "La palanca ya está abajo. ¡Corre al portón!");

        EcosFurniture.Saddle(stables, "SillaMontar", Cell(4f, 3f));
        EcosFurniture.HorseSkeleton(stables, "EsqueletoCaballo", Cell(10.5f, 3.2f));

        // Carta del acuerdo: solo se ve con la linterna (junto a pacas, más visible al alumbrar).
        GameObject letter = GO("CartaPacto", stables, Cell(16.5f, 4.2f));
        SpriteRenderer paper = Visual(letter.transform, EcosAssetFactory.NoteSheet != null ? EcosAssetFactory.NoteSheet : EcosAssetFactory.Square,
            new Color(0.92f, 0.86f, 0.7f), new Vector2(0.55f, 0.4f), -5, "Papel", default, -12f);
        Visual(letter.transform, EcosAssetFactory.Glow, new Color(1f, 0.9f, 0.5f, 0.4f), new Vector2(1.1f, 1.1f), -6, "BrilloNota");
        GameObject trigger = TriggerChild(letter.transform, new Vector2(2.2f, 2.2f));
        var note = letter.AddComponent<NoteInteractable>();
        Set(note, "note", EcosAssetFactory.CartaPacto);
        Set(note, "prompt", "Leer la carta escondida en la paja");
        Set(note, "afterCloseHint", "Te falta la nota de los trojes y la barreta (archivero → herramientas). Palanca al oeste.");
        var reveal = letter.AddComponent<LightRevealable>();
        Set(reveal, "renderers", new Object[] { paper });
        Set(reveal, "enableWhenRevealed", new Object[] { trigger });
        Set(reveal, "revealMessage", "La luz de la linterna revela un papel escondido entre la paja.");

        EcosFurniture.HayBale(stables, "Escondite_Heno", "una paca de heno", Cell(15.5f, 17.5f), new Vector2(1.5f, 1.1f));
        EcosFurniture.HayBale(stables, "PacaCarta", "detrás de unas pacas", Cell(17.2f, 3.8f), new Vector2(1.2f, 0.9f));
        EcosFurniture.Niche(stables, "Escondite_Caballeriza", "el fondo oscuro de una caballeriza", Cell(20.5f, 2.6f), new Vector2(1.4f, 1f));
        Battery(stables, "Baterias_Caballeriza", Cell(3f, 17.5f));

        for (int i = 0; i < 4; i++)
        {
            float x = 4.5f + i * 5.5f;
            EcosFurniture.StallBay(stables, "Caballeriza_" + i, Cell(x, 10f), 4.8f, 3.4f);
            EcosFurniture.HayPile(stables, "PajaBaja_" + i, Cell(x, 5f + (i % 2) * 0.25f), 0.95f + (i % 3) * 0.08f, i);
            EcosFurniture.HayPile(stables, "PajaAlta_" + i, Cell(x + 0.9f, 15.4f), 0.85f, i + 2);
        }

        GameObject glimpse = Silhouette(stables, "SiluetaCaballerizas", Cell(20f, 10f));
        var g = glimpse.AddComponent<ApparitionGlimpse>();
        Set(g, "renderers", glimpse.GetComponentsInChildren<SpriteRenderer>());
        Set(g, "vanishDistance", 5f);
        Set(g, "message", "Al fondo del pasillo, una figura de negro se esfuma entre los caballos.");
        Set(g, "hideOnFlag", "registro_hacienda");
        Set(g, "sound", MakeAudio(glimpse, EcosAssetFactory.Hooves, 0.7f, false, false, true));
    }

    private static void BuildGranary(LevelContext ctx)
    {
        Transform granary = Area(ctx, "Trojes");

        CheckpointAt(granary, Cell(24.5f, 10.5f));
        Lamp(granary, Cell(29f, 11f), 3f, 0.7f);
        // Nota bien visible junto a la lámpara del pasillo de trojes.
        NoteInteractable notaPalanca = Note(granary, "NotaPalanca", Cell(28.5f, 12.5f), EcosAssetFactory.NotaPalanca,
            "Leer la nota del caballerango", new Vector2(2f, 2f));
        Set(notaPalanca, "afterCloseHint",
            "Palanca al oeste de caballerizas. Barreta: archivero del cuarto de registros → cuarto de herramientas.");
        Lamp(granary, Cell(28.5f, 13.2f), 2.5f, 0.9f);
        Battery(granary, "Baterias_Trojes", Cell(33.5f, 11f));
        EcosFurniture.CrateStack(granary, "Escondite_Costales", "detrás de unos costales", Cell(33.5f, 17.5f), new Vector2(1.2f, 1.2f));
        EcosFurniture.HayBale(granary, "PacaTroje", "detrás de unas pacas", Cell(30f, 4f), new Vector2(1.3f, 1f));

        var rng = new System.Random(11);
        for (int i = 0; i < 14; i++)
        {
            float x = 24.3f + (float)rng.NextDouble() * 10f;
            float row = 2.3f + (float)rng.NextDouble() * 16f;
            EcosFurniture.HayPile(granary, "PajaTroje_" + i, Cell(x, row), 0.55f + (float)rng.NextDouble() * 0.35f, i);
        }
    }

    private static void BuildRecordsAndTools(LevelContext ctx)
    {
        Transform rooms = Area(ctx, "Cuartos");

        DoorController records = Door(rooms, "PuertaRegistros", Cell(35f, 14.5f), true, WoodColor,
            DoorState.Cerrada, "Abrir el cuarto de registros", "Está cerrada.");
        Set(records, "flagOnOpen", "puerta_registros");

        EcosFurniture.Desk(rooms, "Escritorio", Cell(40f, 12.5f), new Vector2(3f, 1.2f));
        NoteInteractable registro = Note(rooms, "RegistroHacienda", Cell(40f, 12.6f), EcosAssetFactory.RegistroHacienda,
            "Leer el registro de la hacienda", new Vector2(2f, 2.8f));
        Set(registro, "afterCloseHint",
            "Siguiente: archivero (llave) · carta en la paja de caballerizas (F) · nota en los trojes. Si te persiguen: F o Q.");
        // Resalte sobre el registro para que se vea de lejos.
        Decor(rooms, "BrilloRegistro", EcosAssetFactory.Glow, new Color(1f, 0.85f, 0.4f, 0.45f), Cell(40f, 12.6f), new Vector2(1.4f, 1.4f), -4);
        Lamp(rooms, Cell(38f, 17f), 3f, 0.8f);
        Lamp(rooms, Cell(40f, 13.5f), 2.2f, 0.75f);
        CheckpointAt(rooms, Cell(37.5f, 15.5f));

        GameObject cabinet = EcosFurniture.FilingCabinet(rooms, "Archivero", Cell(44.5f, 16f), new Vector2(1f, 1.4f));
        TriggerChild(cabinet.transform, new Vector2(2.4f, 2.4f));
        var key = cabinet.AddComponent<KeyItem>();
        Set(key, "prompt", "Abrir el archivero");
        Set(key, "itemId", "llave_herramientas");
        Set(key, "itemName", "Llave del cuarto de herramientas");
        Set(key, "pickupMessage", "Entre carpetas húmedas encuentras una llave con una etiqueta: Herramientas.");
        Set(key, "isContainer", true);
        Set(key, "emptyMessage", "Solo quedan papeles mohosos.");

        DoorController tools = Door(rooms, "PuertaHerramientas", Cell(35f, 5.5f), true, WoodColor,
            DoorState.BloqueadaPorLlave, "Abrir el cuarto de herramientas", "La puerta del cuarto de herramientas tiene un candado.");
        Set(tools, "requiredItemId", "llave_herramientas");
        Set(tools, "requiredItemName", "Llave del cuarto de herramientas");

        EcosFurniture.Workbench(rooms, "BancoTrabajo", Cell(40f, 2.5f), new Vector2(3.4f, 1f));
        Pickup(rooms, "Barreta", Cell(43.5f, 3f), new Color(0.45f, 0.45f, 0.5f), new Vector2(0.9f, 0.15f),
            "Tomar la barreta", "barreta", "Barreta de hierro",
            "Tomas una barreta de hierro. Con esto se podría forzar un mecanismo oxidado.");
        Note(rooms, "Bitacora", Cell(42f, 6.5f), EcosAssetFactory.BitacoraCaballerango, "Leer la bitácora", new Vector2(2f, 2f));
        Decor(rooms, "BrilloBitacora", EcosAssetFactory.Glow, new Color(1f, 0.85f, 0.4f, 0.4f), Cell(42f, 6.5f), new Vector2(1.2f, 1.2f), -4);
        Candle(rooms, Cell(41.5f, 2.6f));
    }

    private static void BuildCharro(LevelContext ctx)
    {
        // El área no debe llamarse igual que el enemigo: GameObject.Find devolvería el padre sin EnemyBrain.
        Transform root = Area(ctx, "ZonaCharro");

        NavGraph nav = BuildNavGraph(root, new[]
        {
            // Caballerizas
            Cell(3.5f, 10f), Cell(9.5f, 10f), Cell(15.5f, 10f), Cell(21f, 10f),
            Cell(4.5f, 5f), Cell(10f, 5f), Cell(15.5f, 5f), Cell(20.5f, 5f),
            Cell(4.5f, 15.5f), Cell(10f, 15.5f), Cell(15.5f, 15.5f), Cell(20.5f, 15.5f),
            Cell(9.5f, 19f),
            // Patio y camino al portón
            Cell(9.5f, 21.5f), Cell(4.5f, 24f), Cell(12f, 26.5f), Cell(20f, 21.5f), Cell(18f, 25.5f),
            Cell(28.5f, 21.5f), Cell(28.5f, 26.5f), Cell(31.5f, 26.5f), Cell(38f, 26.5f), Cell(43f, 26.5f),
            // Trojes
            Cell(23f, 10f), Cell(24.5f, 10f), Cell(24.5f, 3f), Cell(24.5f, 7f), Cell(24.5f, 14.5f), Cell(24.5f, 17.5f),
            Cell(33f, 3f), Cell(33.5f, 7f), Cell(33f, 11f), Cell(33f, 14.5f), Cell(28.5f, 17.5f), Cell(28.5f, 19f),
            // Cuartos
            Cell(35f, 5.5f), Cell(37.5f, 5.5f), Cell(41f, 6.5f), Cell(35f, 14.5f), Cell(37.5f, 14.5f), Cell(41f, 16f)
        });

        Vector2[] route = { Cell(20f, 10f), Cell(9.5f, 10.5f), Cell(9.5f, 16f), Cell(9.5f, 22f), Cell(18f, 25.5f), Cell(28f, 22.5f) };

        EnemyBrain brain = BuildEnemy(root, new EnemySpec
        {
            Name = "CharroNegro",
            BodyColor = Color.white,
            DetailColor = new Color(0.02f, 0.02f, 0.02f),
            BodySize = Vector2.one * 1.2f,
            ConeColor = new Color(1f, 0.35f, 0.25f, 0.12f),
            Voice = EcosAssetFactory.HoovesLoop,
            IdleVoiceVolume = 0.35f,
            ViewDistance = 7f,
            ViewAngle = 90f,
            Hearing = 1f,
            PatrolSpeed = 1.8f,
            InvestigateSpeed = 2.6f,
            ChaseSpeed = 4.3f,
            CloseChaseDistance = 3f,
            LoseSightTime = 1.2f,
            SearchDuration = 5f,
            SearchRadius = 3f,
            StartActive = false,
            RetreatAfterHit = false,
            StunAfterHit = 1.5f,
            WakeOnFlag = "registro_hacienda",
            Nav = nav,
            Animator = EcosAnimationFactory.CharroController
        }, route, route[0]);

        SpriteRenderer body = brain.transform.Find("Cuerpo").GetComponent<SpriteRenderer>();

        var reaction = brain.gameObject.AddComponent<LightReaction>();
        Set(reaction, "body", body);

        SavePrefabCopy(brain.gameObject, "Assets/Prefabs/Enemies/CharroNegro.prefab");

        GameObject chaseGo = GO("PersecucionFinal", root);
        var chase = chaseGo.AddComponent<FinalChaseSequence>();
        Set(chase, "hunter", brain);
        Set(chase, "spawnPoint", GO("PuntoAparicionCharro", root, Cell(21f, 10f)).transform);
        Set(chase, "warningSound", MakeAudio(chaseGo, EcosAssetFactory.Hooves, 1f, false, false));
    }

    private static void PlaceYardSign(Transform parent, string name, Vector2 position, string prompt, string text)
    {
        GameObject sign = EcosFurniture.StoneMarker(parent, name, position, new Vector2(1.4f, 0.55f));
        Examinable(sign, prompt, text, new Vector2(2.4f, 1.8f));
    }

    private static GameObject Silhouette(Transform parent, string name, Vector2 position)
    {
        GameObject go = GO(name, parent, position);
        SpriteRenderer body = Visual(go.transform, EcosAssetFactory.Circle, new Color(0.05f, 0.05f, 0.06f, 0.85f), new Vector2(0.85f, 1.4f), 12, "Silueta");
        Visual(body.transform, EcosAssetFactory.Circle, new Color(0.03f, 0.03f, 0.03f, 0.85f), new Vector2(1.6f, 0.35f), 13, "Sombrero", new Vector2(0f, 0.45f));
        Visual(body.transform, EcosAssetFactory.Circle, new Color(1f, 0.25f, 0.15f, 0.9f), new Vector2(0.12f, 0.08f), 14, "Ojos", new Vector2(0f, 0.18f));
        return go;
    }

    private static void Battery(Transform parent, string name, Vector2 position)
    {
        GameObject go = GO(name, parent, position);
        SpriteRenderer pila = Visual(go.transform, EcosAssetFactory.Square, new Color(0.3f, 0.6f, 0.35f), new Vector2(0.25f, 0.4f), -6, "Pila");
        Visual(go.transform, EcosAssetFactory.Glow, new Color(0.6f, 1f, 0.6f, 0.35f), new Vector2(0.9f, 0.9f), -7, "Brillo");
        TriggerChild(go.transform, new Vector2(1.4f, 1.4f));
        Set(go.AddComponent<BatteryPickup>(), "prompt", "Recoger baterías");
        var floatAnim = go.AddComponent<SpriteFloat>();
        Set(floatAnim, "target", pila.transform);
        Set(floatAnim, "height", 0.05f);
        Set(floatAnim, "speed", 2.6f);
    }
}
