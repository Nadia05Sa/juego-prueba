using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using static EcosBuildUtils;
using static EcosLevelKit;

/// <summary>
/// Nivel 2 — El silencio de la cripta (momia ficticia). Atrio → nave → sacristía (pistas y mecanismo)
/// → pasillo de criptas (momia, cadenas, símbolos) → cámara final (mitad del medallón) → escalera.
/// </summary>
public static class EcosLevel2Builder
{
    private static LevelMap map;

    private static Vector2 Cell(float x, float row) => map.Cell(x, row);

    private static LevelMap BuildLayout()
    {
        var m = new LevelMap(44, 32);
        m.Fill(16, 26, 27, 30, '.');   // Atrio
        m.Fill(21, 25, 22, 25, '.');   // Puerta de la capilla
        m.Fill(13, 8, 30, 24, '.');    // Nave
        for (int r = 12; r <= 22; r += 2)
        {
            m.Fill(15, r, 19, r, 'B'); // Bancas
            m.Fill(24, r, 28, r, 'B');
        }
        m.Fill(19, 9, 24, 9, 'A');     // Altar
        m.Fill(32, 8, 41, 14, ',');    // Sacristía
        m.Fill(31, 10, 31, 11, ',');   // Puerta de la sacristía
        m.Fill(11, 18, 12, 19, ':');   // Reja de las criptas
        m.Fill(3, 2, 10, 29, ':');     // Pasillo oeste de criptas
        m.Fill(11, 2, 27, 6, ':');     // Pasillo norte de criptas
        m.Fill(28, 3, 28, 4, ':');     // Puerta de piedra
        m.Fill(29, 1, 41, 6, ':');     // Cámara final

        // Tumbas del pasillo oeste (dejan libre el centro, x 5–8)
        m.Fill(3, 9, 4, 10, 'T');
        m.Fill(3, 13, 4, 14, 'T');
        m.Fill(3, 24, 4, 25, 'T');
        m.Fill(9, 11, 10, 12, 'T');
        m.Fill(9, 15, 10, 16, 'T');
        m.Fill(9, 22, 10, 23, 'T');
        m.Fill(9, 26, 10, 27, 'T');
        // Tumbas del pasillo norte
        m.Fill(14, 2, 15, 2, 'T');
        m.Fill(21, 2, 22, 2, 'T');
        m.Fill(17, 6, 18, 6, 'T');
        m.Fill(24, 6, 25, 6, 'T');
        // Sarcófago de la cámara
        m.Fill(34, 3, 36, 4, 'T');
        return m;
    }

    private static Dictionary<char, (TileBase, TileBase)> Palette() => new Dictionary<char, (TileBase, TileBase)>
    {
        { '#', (EcosAssetFactory.CryptWallTile, null) },
        { '.', (null, EcosAssetFactory.StoneTile) },
        { ':', (null, EcosAssetFactory.CryptFloorTile) },
        { ',', (null, EcosAssetFactory.WoodTile) },
        { 'B', (EcosAssetFactory.BenchTile, EcosAssetFactory.StoneTile) },
        { 'A', (EcosAssetFactory.AltarTile, EcosAssetFactory.StoneTile) },
        { 'T', (EcosAssetFactory.TombTile, EcosAssetFactory.CryptFloorTile) }
    };

    public static void Build()
    {
        map = BuildLayout();
        var setup = new LevelSetup
        {
            Title = "Nivel 2 — El silencio de la cripta",
            StartHint = "Abre la capilla (E). Camina sin correr: el ruido alerta a lo que duerme abajo.",
            TutorialExtra =
                "Orden de exploración:\n" +
                "1) Capilla → sacristía (puerta a la derecha del altar).\n" +
                "2) Lee las 2 hojas y gira el torno (abre la reja).\n" +
                "3) Baja a las criptas: símbolos en orden Sol → Luna → Espiral → Calavera.\n" +
                "4) Cámara: toma la mitad del medallón y sube la escalera.\n\n" +
                "Evita las cadenas del techo. Q para esconderte si te persiguen.",
            NextScene = Level3Scene,
            ExitMessage = "Subes por la escalera de la cámara. Abajo, algo arrastra los pies en la oscuridad...",
            CompleteTitle = "Nivel 2 completado",
            CompleteText = "Llevas contigo las dos mitades del medallón.\nLa carta sellada habla de un jinete vestido de negro\ny de un acuerdo guardado en los establos.",
            CarryOverItems = new[] { "medallon_plata", "pieza_medallon" },
            GameOverText = "El silencio de la cripta se cierra sobre ti.",
            GlobalLightColor = new Color(0.55f, 0.55f, 0.75f),
            GlobalLightIntensity = 0.38f,
            Ambience = EcosAssetFactory.Drip,
            PlayerLightRadius = 5f,
            PlayerLightIntensity = 1f
        };

        LevelContext ctx = BeginLevel(map, Palette(), Cell(21.5f, 29f), setup);

        BuildAtriumAndNave(ctx);
        BuildSacristy(ctx);
        BuildCrypts(ctx);
        BuildChamber(ctx);
        BuildMummy(ctx);

        SetObjectives(ctx.LevelManager, new[]
        {
            ("Entra a la capilla (puerta del atrio, E)", ObjectiveCondition.Flag, "puerta_capilla", 0),
            ("Entra a la sacristía (puerta a la derecha del altar)", ObjectiveCondition.Flag, "puerta_sacristia", 0),
            ("Lee las 2 hojas de la mesa de la sacristía ({0}/2)", ObjectiveCondition.ClueCount, "", 2),
            ("Gira el torno de la sacristía (abre la reja de las criptas)", ObjectiveCondition.Flag, "mecanismo_sacristia", 0),
            ("Cripta: Sol → Luna → Espiral → Calavera (orden de las pistas)", ObjectiveCondition.Flag, "cripta_resuelta", 0),
            ("Toma la mitad del medallón en la cámara sellada", ObjectiveCondition.HasItem, "pieza_medallon", 0),
            ("Sube la escalera al fondo de la cámara", ObjectiveCondition.Flag, "nivel_completado", 0)
        });

        Save(ctx.Scene, Level2Scene);
    }

    private static void BuildAtriumAndNave(LevelContext ctx)
    {
        Transform nave = Area(ctx, "Capilla");

        DoorController door = Door(nave, "PuertaCapilla", Cell(21.5f, 25f), false, WoodColor,
            DoorState.Cerrada, "Abrir la puerta de la capilla", "Está cerrada.");
        Set(door, "flagOnOpen", "puerta_capilla");

        CheckpointAt(nave, Cell(21.5f, 27.5f));
        Candle(nave, Cell(17f, 26.5f));
        Candle(nave, Cell(26f, 26.5f));

        GameObject sign = EcosFurniture.StoneMarker(nave, "Inscripcion", Cell(18.5f, 29.5f), new Vector2(1.3f, 0.55f));
        Examinable(sign, "Leer la inscripción",
            "Capilla de la Hacienda de la Vega.\nGuarden silencio.\n\n" +
            "Dentro: avanza al altar. A la derecha está la sacristía (escritos y torno).\n" +
            "La reja de las criptas se abre desde ahí.",
            new Vector2(2f, 1.6f));

        EcosFurniture.DecorateAltar(nave, Cell(21.5f, 9f));
        Candle(nave, Cell(13.5f, 11f));
        Candle(nave, Cell(29.5f, 11f));
        Candle(nave, Cell(13.5f, 23.5f));
        Candle(nave, Cell(29.5f, 23.5f));

        // Bancas de la capilla (sobre los tiles B).
        for (int r = 12; r <= 22; r += 2)
        {
            EcosFurniture.Pew(nave, "BancaIzq_" + r, Cell(17f, r + 0.15f), new Vector2(3.6f, 0.85f));
            EcosFurniture.Pew(nave, "BancaDer_" + r, Cell(26f, r + 0.15f), new Vector2(3.6f, 0.85f));
        }

        EcosFurniture.Wardrobe(nave, "Escondite_Confesionario", "el confesionario", Cell(29.6f, 17f), new Vector2(1f, 1.6f));
        CheckpointAt(nave, Cell(13.5f, 20.5f));

        DoorController reja = Door(nave, "RejaCriptas", Cell(12f, 18.5f), true, new Color(0.3f, 0.3f, 0.33f),
            DoorState.BloqueadaPorObjetivo, "Abrir la reja", "Una reja de hierro cierra el paso a las criptas. Parece unida a algún mecanismo.");
        Set(reja, "requiredFlag", "mecanismo_sacristia");
        Set(reja, "autoOpenOnFlag", true);
        Set(reja, "unlockMessage", "A lo lejos, la reja de las criptas se levanta con un chirrido.");
        Set(reja, "noiseRadiusOnOpen", 4f);
    }

    private static void BuildSacristy(LevelContext ctx)
    {
        Transform sacristy = Area(ctx, "Sacristia");

        DoorController door = Door(sacristy, "PuertaSacristia", Cell(31f, 10.5f), true, WoodColor,
            DoorState.Cerrada, "Abrir la sacristía", "Está cerrada.");
        Set(door, "flagOnOpen", "puerta_sacristia");

        EcosFurniture.Desk(sacristy, "Mesa", Cell(36.5f, 10f), new Vector2(3f, 1.2f));
        Note(sacristy, "DiarioSacristan", Cell(35.7f, 9.9f), EcosAssetFactory.DiarioSacristan, "Leer el diario del sacristán", new Vector2(1.4f, 2.8f));
        Note(sacristy, "OracionCapellan", Cell(37.4f, 10.1f), EcosAssetFactory.OracionCapellan, "Leer la oración", new Vector2(1.4f, 2.8f));
        Candle(sacristy, Cell(36.5f, 9.6f));
        EcosFurniture.Wardrobe(sacristy, "Ropero", "el ropero", Cell(40.8f, 8.5f), new Vector2(1f, 1.6f));

        Lever mechanism = EcosFurniture.Winch(sacristy, "MecanismoSacristia", Cell(40.6f, 12.5f), "Girar el torno del mecanismo");
        Set(mechanism, "requiredClues", 2);
        Set(mechanism, "requiredCluesName", "entender las indicaciones de la sacristía");
        Set(mechanism, "lockedPrefix", "Un torno de hierro con manivela dorada, unido a cadenas bajo el suelo. Antes de moverlo deberías ");
        Set(mechanism, "flagOnActivate", "mecanismo_sacristia");
        Set(mechanism, "noiseRadius", 9f);
        Set(mechanism, "activatedMessage", "Giras el torno. Bajo el suelo, cadenas y engranes se mueven con estruendo... algo despertó en las criptas.");
        Set(mechanism, "usedPrompt", "Examinar el torno");
        Set(mechanism, "usedMessage", "El torno ya no gira más.");
        Lamp(sacristy, Cell(40.2f, 13.2f), 2.2f, 0.85f);

        EcosFurniture.Wardrobe(sacristy, "Escondite_Armario", "el armario de los ornamentos", Cell(33f, 13.5f), new Vector2(1.2f, 1.4f));
    }

    private static void BuildCrypts(LevelContext ctx)
    {
        Transform crypts = Area(ctx, "Criptas");

        GameObject smell = GO("EventoCriptas", crypts, Cell(9.5f, 18.5f));
        Box(smell, new Vector2(1f, 2f), true);
        AudioSource groan = MakeAudio(smell, EcosAssetFactory.Groan, 0.4f, false, false);
        var evt = smell.AddComponent<AmbientEventTrigger>();
        Set(evt, "message", "Huele a tierra seca y a tela vieja. Desde el fondo del pasillo llega un gemido ahogado.");
        Set(evt, "audioSource", groan);

        PlaceTombstone(crypts, "LapidaFundador", Cell(5f, 19f), EcosAssetFactory.LapidaFundador,
            "Leer la lápida del fundador");
        PlaceTombstone(crypts, "LapidaSinNombre", Cell(8.5f, 8f), EcosAssetFactory.LapidaSinNombre,
            "Leer la lápida");

        Chains(crypts, "CadenasOeste", Cell(4.5f, 20.5f), new Vector2(3f, 2f));
        Chains(crypts, "CadenasSur", Cell(9.2f, 24.5f), new Vector2(2.6f, 2f));
        Chains(crypts, "CadenasNorte", Cell(12.5f, 2.6f), new Vector2(2f, 1.8f));

        Candle(crypts, Cell(7f, 29f));
        Candle(crypts, Cell(10.5f, 4f));

        EcosFurniture.Niche(crypts, "Escondite_Nicho1", "un nicho funerario vacío", Cell(3.2f, 17.5f), new Vector2(0.9f, 1.3f));
        EcosFurniture.Niche(crypts, "Escondite_Nicho2", "un nicho funerario vacío", Cell(10.8f, 7f), new Vector2(0.9f, 1.3f));
        EcosFurniture.Niche(crypts, "Escondite_Nicho3", "detrás de un sarcófago", Cell(19.5f, 6.3f), new Vector2(1.2f, 0.9f));
        CheckpointAt(crypts, Cell(12f, 4f));

        GameObject puzzleGo = GO("AcertijoCripta", crypts, Cell(20f, 3f));
        var puzzle = puzzleGo.AddComponent<CryptPuzzle>();

        PuzzlePlate luna = Plate(crypts, puzzle, "PlacaLuna", "luna", "la luna", EcosAssetFactory.Moon, new Color(0.85f, 0.88f, 1f), Cell(15.5f, 4f));
        PuzzlePlate calavera = Plate(crypts, puzzle, "PlacaCalavera", "calavera", "la calavera", EcosAssetFactory.Skull, new Color(0.95f, 0.93f, 0.85f), Cell(18.5f, 4f));
        PuzzlePlate sol = Plate(crypts, puzzle, "PlacaSol", "sol", "el sol", EcosAssetFactory.Sun, new Color(1f, 0.8f, 0.25f), Cell(21.5f, 4f));
        PuzzlePlate espiral = Plate(crypts, puzzle, "PlacaEspiral", "espiral", "la espiral", EcosAssetFactory.Spiral, new Color(0.6f, 0.85f, 0.7f), Cell(24.5f, 4f));

        Light2D glow = MakeLight(crypts, "ResplandorPuerta", Light2D.LightType.Point, new Color(1f, 0.75f, 0.35f), 1.2f, 3f, Cell(27f, 3.5f));
        glow.enabled = false;

        Set(puzzle, "correctOrder", new[] { "sol", "luna", "espiral", "calavera" });
        Set(puzzle, "plates", new Object[] { luna, calavera, sol, espiral });
        Set(puzzle, "requiredClues", 2);
        Set(puzzle, "notReadyMessage", "Cuatro placas: sol, luna, espiral y calavera. No sabes en qué orden presionarlas; quizá en la sacristía haya algo escrito.");
        Set(puzzle, "flagOnSolved", "cripta_resuelta");
        Set(puzzle, "solvedMessage", "Los cuatro símbolos se iluminan. La losa de piedra empieza a moverse con un rugido sordo...");
        Set(puzzle, "errorMessage", "Un golpe seco retumba en el pasillo y las placas vuelven a su lugar.");
        Set(puzzle, "noiseRadiusOnPress", 2.5f);
        Set(puzzle, "noiseRadiusOnError", 6f);
        Set(puzzle, "noiseRadiusOnSolve", 8f);
        Set(puzzle, "glowOnSolved", new Object[] { glow });

        DoorController stone = Door(crypts, "PuertaPiedra", Cell(28f, 3.5f), true, new Color(0.5f, 0.48f, 0.44f),
            DoorState.BloqueadaPorObjetivo, "Empujar la losa", "Una losa de piedra con cuatro huecos grabados: sol, luna, espiral y calavera.");
        Set(stone, "requiredFlag", "cripta_resuelta");
        Set(stone, "autoOpenOnFlag", true);
        Set(stone, "noiseRadiusOnOpen", 6f);
        Set(stone, "openSound", AudioManager.Sfx.StoneDoor);
        Set(stone, "openDuration", 1.5f);
    }

    private static void Chains(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject go = GO(name, parent, position);
        Box(go, size, true);
        var links = new List<Object>();
        int count = Mathf.Max(2, Mathf.RoundToInt(size.x * 1.5f));
        for (int i = 0; i < count; i++)
        {
            float x = -size.x * 0.5f + (i + 0.5f) * size.x / count;
            GameObject chain = GO("Cadena" + (i + 1), go.transform);
            chain.transform.localPosition = new Vector3(x, size.y * 0.4f, 0f);
            for (int k = 0; k < 4; k++)
                Visual(chain.transform, EcosAssetFactory.Ring, new Color(0.55f, 0.55f, 0.6f), new Vector2(0.18f, 0.28f), 14, "Eslabon", new Vector2(0f, -k * 0.22f));
            links.Add(chain.transform);
        }
        var trap = go.AddComponent<ChainTrap>();
        Set(trap, "chains", links.ToArray());
    }

    private static void BuildChamber(LevelContext ctx)
    {
        Transform chamber = Area(ctx, "CamaraFinal");

        KeyItem medallion = Pickup(chamber, "MitadMedallon", Cell(35f, 3.5f), new Color(0.8f, 0.82f, 0.85f), new Vector2(0.35f, 0.35f),
            "Tomar la mitad del medallón", "pieza_medallon", "Otra mitad del medallón",
            "Sobre el sarcófago está la otra mitad de un medallón de plata.",
            "pieza_medallon", 3.4f);
        Set(medallion, "noteOnPickup", EcosAssetFactory.CartaSellada);
        Set(medallion, "afterNoteMessage", "Con las dos mitades del medallón, sal por la escalera al fondo de la cámara.");
        Candle(chamber, Cell(32f, 1.5f));
        Candle(chamber, Cell(38f, 1.5f));
        Candle(chamber, Cell(40f, 3f));

        EcosFurniture.Stairs(chamber, "Escalera", Cell(40.5f, 1.5f));
        Exit(chamber, Cell(40.5f, 1.5f), new Vector2(2.2f, 2.2f), "pieza_medallon", "Todavía no has encontrado lo que guarda la cámara.");
    }

    private static void PlaceTombstone(Transform parent, string name, Vector2 position, NoteData note, string prompt)
    {
        // Mundo: sprite de piedra + inscripción breve. Lectura: panel de pergamino (ShowNote).
        GameObject lapida = EcosFurniture.Tombstone(parent, name, position,
            ShortInscription(note), note != null ? note.Body : string.Empty);
        TriggerChild(lapida.transform, new Vector2(1.8f, 2f));
        var reader = lapida.AddComponent<NoteInteractable>();
        Set(reader, "note", note);
        Set(reader, "prompt", prompt);
        if (note != null && note.Id == "lapida_fundador")
            Set(reader, "afterCloseHint", "El raspado habla de un trato. Sigue explorando la cripta.");
    }

    private static string ShortInscription(NoteData note)
    {
        if (note == null) return string.Empty;
        if (note.Id == "lapida_fundador") return "IGNACIO\nDE LA VEGA";
        if (note.Id == "lapida_sin_nombre") return "AQUÍ\nTERMINA";
        return note.Title;
    }

    private static void BuildMummy(LevelContext ctx)
    {
        Transform root = Area(ctx, "Momia");

        NavGraph nav = BuildNavGraph(root, new[]
        {
            // Pasillo oeste
            Cell(6.5f, 28f), Cell(6.5f, 21f), Cell(6.5f, 18.5f), Cell(6.5f, 12f), Cell(6.5f, 4f),
            // Pasillo norte y cámara
            Cell(12f, 4f), Cell(19f, 4f), Cell(26f, 4f), Cell(27f, 3.5f), Cell(30f, 3.5f), Cell(33f, 5.5f), Cell(38.5f, 3f),
            // Reja y nave
            Cell(11.5f, 18.5f), Cell(13.5f, 18.5f), Cell(13.5f, 10f), Cell(13.5f, 23.5f), Cell(21.5f, 23.5f), Cell(21.5f, 11f),
            Cell(29.5f, 10.5f), Cell(29.5f, 23.5f),
            // Sacristía y atrio
            Cell(32.5f, 10.5f), Cell(36.5f, 12.5f), Cell(21.5f, 25f), Cell(21.5f, 28f)
        });

        Vector2[] route = { Cell(6.5f, 27.5f), Cell(6.5f, 19f), Cell(6.5f, 8f), Cell(6.5f, 4f), Cell(13f, 4f), Cell(20f, 5f), Cell(26f, 4f) };

        EnemyBrain brain = BuildEnemy(root, new EnemySpec
        {
            Name = "LaMomia",
            BodyColor = Color.white,
            DetailColor = new Color(0.4f, 0.32f, 0.2f),
            BodySize = Vector2.one * 1.15f,
            ConeColor = new Color(0.9f, 0.8f, 0.5f, 0.12f),
            Voice = EcosAssetFactory.Groan,
            IdleVoiceVolume = 0.45f,
            ViewDistance = 4.5f,
            ViewAngle = 80f,
            Hearing = 1.6f,
            PatrolSpeed = 1f,
            InvestigateSpeed = 1.8f,
            ChaseSpeed = 2.8f,
            CloseChaseDistance = 2f,
            LoseSightTime = 1.5f,
            SearchDuration = 6f,
            SearchRadius = 3f,
            StartActive = true,
            StartPatrolling = false,
            RetreatAfterHit = false,
            StunAfterHit = 2f,
            WakeOnFlag = "mecanismo_sacristia",
            Nav = nav,
            Animator = EcosAnimationFactory.MummyController
        }, route, route[0]);

        SavePrefabCopy(brain.gameObject, "Assets/Prefabs/Enemies/LaMomia.prefab");
    }
}
