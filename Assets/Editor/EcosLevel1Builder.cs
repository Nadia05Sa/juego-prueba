using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using static EcosBuildUtils;
using static EcosLevelKit;

/// <summary>Nivel 1 — El lamento del aljibe (La Llorona).</summary>
public static class EcosLevel1Builder
{
    private static LevelMap map;

    private static Vector2 Cell(float x, float row) => map.Cell(x, row);

    private static LevelMap BuildLayout()
    {
        var m = new LevelMap(48, 32);
        m.Fill(1, 1, 11, 11, ':');    // Oficina
        m.Fill(13, 1, 46, 3, '.');    // Corredor norte (une con el patio del aljibe)
        m.Fill(31, 4, 46, 12, '.');   // Patio del aljibe
        m.Fill(13, 5, 29, 30, '.');   // Patio central
        m.Fill(1, 13, 11, 20, ':');   // Habitación A
        m.Fill(1, 22, 11, 30, ':');   // Habitación B
        m.Fill(31, 14, 46, 30, ',');  // Campo de cempasúchil

        m.Fill(21, 4, 22, 4, '.');    // Puerta corredor norte
        m.Fill(12, 7, 12, 8, ':');    // Puerta oficina
        m.Fill(12, 16, 12, 17, ':');  // Puerta habitación A
        m.Fill(5, 21, 6, 21, ':');    // Puerta entre habitaciones
        m.Fill(30, 18, 30, 19, '.');  // Reja del campo
        m.Fill(38, 13, 39, 13, '.');  // Paso del campo al aljibe
        m.Fill(21, 31, 22, 31, '.');  // Portón principal

        foreach (Vector2Int c in new[] { new Vector2Int(15, 7), new Vector2Int(27, 7), new Vector2Int(15, 27), new Vector2Int(27, 27) })
            m.Grid[c.x, c.y] = 'C';

        foreach (Vector2Int t in new[]
                 {
                     new Vector2Int(34, 16), new Vector2Int(36, 22), new Vector2Int(41, 17), new Vector2Int(44, 21),
                     new Vector2Int(33, 27), new Vector2Int(39, 29), new Vector2Int(45, 15), new Vector2Int(42, 24),
                     new Vector2Int(37, 26)
                 })
            m.Grid[t.x, t.y] = 'T';

        return m;
    }

    private static Dictionary<char, (TileBase, TileBase)> Palette() => new Dictionary<char, (TileBase, TileBase)>
    {
        { '#', (EcosAssetFactory.WallTile, null) },
        { 'C', (EcosAssetFactory.ColumnTile, EcosAssetFactory.StoneTile) },
        { 'T', (EcosAssetFactory.TreeTile, EcosAssetFactory.FieldTile) },
        { '.', (null, EcosAssetFactory.StoneTile) },
        { ':', (null, EcosAssetFactory.WoodTile) },
        { ',', (null, EcosAssetFactory.FieldTile) }
    };

    public static void Build()
    {
        map = BuildLayout();
        var setup = new LevelSetup
        {
            Title = "Nivel 1 — El lamento del aljibe",
            StartHint = "Primer paso: entra a la oficina (puerta lateral del patio) y pulsa E. Lee la carta del escritorio.",
            TutorialExtra =
                "Orientación del patio:\n" +
                "• Oficina — puerta lateral (empieza ahí).\n" +
                "• Campo / aljibe — más adelante, tras abrir con la llave.\n" +
                "• Portón principal — solo se abre al resolver el aljibe.\n\n" +
                "Reúne 3 pistas (agua, luna, flor) y actívalas en ese orden en el aljibe.",
            NextScene = Level2Scene,
            ExitMessage = "Cruzas el portón. El llanto se apaga a tus espaldas...",
            CompleteTitle = "Nivel 1 completado",
            CompleteText = "El lamento del aljibe se apaga.\nPero el camino fuera de la hacienda pasa por la vieja capilla,\ny allá abajo, en la cripta, algo sigue despierto.",
            CarryOverItems = new[] { "medallon_plata" },
            GameOverText = "El llanto se escucha cada vez más cerca.",
            GlobalLightColor = new Color(0.48f, 0.52f, 0.75f),
            GlobalLightIntensity = 0.4f,
            PlayerLightRadius = 5.6f,
            PlayerLightIntensity = 1.2f,
            Ambience = EcosAssetFactory.Wind
        };

        LevelContext ctx = BeginLevel(map, Palette(), Cell(21.5f, 29f), setup);

        GameObject outside = GO("LimiteExterior", null, new Vector2(22f, -0.5f));
        Box(outside, new Vector2(4f, 1f), false);

        BuildEntranceAndPatio(ctx);
        BuildOffice(ctx);
        BuildRooms(ctx);
        BuildField(ctx);
        AljibePuzzle puzzle = BuildAljibe(ctx);
        BuildLlorona(ctx, puzzle);

        SetObjectives(ctx.LevelManager, new[]
        {
            ("Entra a la oficina (puerta lateral del patio, E)", ObjectiveCondition.Flag, "puerta_oficina", 0),
            ("Lee la carta del escritorio en la oficina", ObjectiveCondition.Flag, "carta_oficina", 0),
            ("Saca la llave del cajón del escritorio", ObjectiveCondition.HasItem, "llave_oficina", 0),
            ("Abre la reja del campo con la llave", ObjectiveCondition.Flag, "puerta_campo", 0),
            ("Reúne las 3 pistas del aljibe ({0}/3)", ObjectiveCondition.ClueCount, "", 3),
            ("En el aljibe: activa agua, luna y flor (en ese orden)", ObjectiveCondition.Flag, "aljibe_resuelto", 0),
            ("Escapa por el portón principal", ObjectiveCondition.Flag, "nivel_completado", 0)
        });

        PrefabUtility.SaveAsPrefabAssetAndConnect(ctx.Player, "Assets/Prefabs/Player/Player.prefab", InteractionMode.AutomatedAction);
        Save(ctx.Scene, Level1Scene);
    }

    private static void BuildEntranceAndPatio(LevelContext ctx)
    {
        Transform patio = Area(ctx, "Patio");

        DoorController gate = Door(patio, "Porton", Cell(21.5f, 31f), false, new Color(0.22f, 0.22f, 0.25f),
            DoorState.BloqueadaPorObjetivo, "Abrir el portón", "El portón no cede. Algo en la hacienda no te deja ir todavía.");
        Set(gate, "requiredFlag", "aljibe_resuelto");
        Set(gate, "unlockMessage", "A lo lejos, el portón principal cruje y se destraba.");
        Set(gate, "noiseRadiusOnOpen", 6f);
        SavePrefabCopy(gate.gameObject, "Assets/Prefabs/Doors/Puerta.prefab");

        Exit(patio, new Vector2(22f, 0.35f), new Vector2(2f, 0.7f), "aljibe_resuelto", "Todavía no puedes irte.");

        DoorController north = Door(patio, "PuertaCorredorNorte", Cell(21.5f, 4f), false, WoodColor,
            DoorState.BloqueadaPorObjetivo, "Abrir", "La puerta del corredor está atrancada desde el otro lado.");
        Set(north, "requiredFlag", "aljibe_resuelto");
        Set(north, "autoOpenOnFlag", true);

        EcosFurniture.Fountain(patio, Cell(21.5f, 17f));

        Candle(patio, Cell(14f, 5.5f));
        SavePrefabCopy(Candle(patio, Cell(28f, 5.5f)), "Assets/Prefabs/Candles/Vela.prefab");

        GameObject nicho = EcosFurniture.Niche(patio, "Escondite_Nicho", "un nicho oscuro", Cell(13.1f, 12f), new Vector2(0.9f, 1.4f));
        SavePrefabCopy(nicho, "Assets/Prefabs/Escondite.prefab");
        EcosFurniture.CrateStack(patio, "Escondite_Cajas", "detrás de unas cajas", Cell(16f, 2f), new Vector2(1.2f, 1.2f));
        EcosFurniture.WallShadowHide(patio, "Escondite_Muro", "la sombra del muro", Cell(45.5f, 1.5f), new Vector2(1.2f, 1.2f));

        SavePrefabCopy(CheckpointAt(patio, Cell(21.5f, 27.5f)), "Assets/Prefabs/Checkpoints/PuntoControl.prefab");
    }

    private static void BuildOffice(LevelContext ctx)
    {
        Transform office = Area(ctx, "Oficina");

        DoorController door = Door(office, "PuertaOficina", Cell(12f, 7.5f), true, WoodColor,
            DoorState.Cerrada, "Abrir la oficina", "Está cerrada.");
        Set(door, "flagOnOpen", "puerta_oficina");

        GameObject desk = PlaceOfficeDesk(office, Cell(5f, 3f));
        PlaceOfficeChair(office, Cell(5f, 1.5f));
        PlaceOfficeBookshelf(office, Cell(1.1f, 6.5f));
        PlaceOfficeDresser(office, Cell(10.4f, 2f));

        // Props mínimos sobre el escritorio: tintero y pluma.
        Visual(desk.transform, EcosAssetFactory.Circle, new Color(0.12f, 0.1f, 0.1f), new Vector2(0.22f, 0.22f), -12, "Tintero", new Vector2(-0.9f, 0.1f));
        Visual(desk.transform, EcosAssetFactory.Square, new Color(0.15f, 0.12f, 0.1f), new Vector2(0.06f, 0.2f), -11, "Pluma", new Vector2(-0.75f, 0.15f), 35f);

        NoteInteractable carta = Note(office, "Carta", Cell(4f, 2.9f), EcosAssetFactory.CartaOficina, "Leer carta", new Vector2(1.4f, 2.6f));
        SavePrefabCopy(carta.gameObject, "Assets/Prefabs/Notes/Nota.prefab");
        Note(office, "LibroCuentas", Cell(1.9f, 6.5f), EcosAssetFactory.LibroCuentas, "Leer libro de cuentas", new Vector2(1.6f, 3f));

        GameObject drawer = GO("Cajon", office, Cell(6.3f, 3.3f));
        Visual(drawer.transform, EcosAssetFactory.WoodPlank ?? EcosAssetFactory.Square, new Color(0.22f, 0.13f, 0.07f), new Vector2(0.7f, 0.28f), -10);
        TriggerChild(drawer.transform, new Vector2(1.4f, 2.6f));
        var key = drawer.AddComponent<KeyItem>();
        Set(key, "prompt", "Abrir cajón");
        Set(key, "itemId", "llave_oficina");
        Set(key, "itemName", "Llave oxidada");
        Set(key, "pickupMessage", "Dentro del cajón hay una llave oxidada con una etiqueta: «Reja del campo».");
        Set(key, "isContainer", true);
        Set(key, "emptyMessage", "El cajón está vacío.");
        SavePrefabCopy(drawer, "Assets/Prefabs/Keys/Cajon_Llave.prefab");

        Candle(office, Cell(3f, 2.6f));
    }

    private static void BuildRooms(LevelContext ctx)
    {
        Transform rooms = Area(ctx, "Habitaciones");

        Door(rooms, "PuertaHabitacion", Cell(12f, 16.5f), true, WoodColor, DoorState.Cerrada, "Abrir", "Está cerrada.");
        Door(rooms, "PuertaInterior", Cell(5.5f, 21f), false, WoodColor, DoorState.Cerrada, "Abrir", "Está cerrada.");

        EcosFurniture.Bed(rooms, "Cama", Cell(2.5f, 14.8f), new Vector2(2f, 3f), true,
            "Debajo de la cama hay polvo grueso y una media de niño. Nada más… salvo un olor a agua estancada.",
            "bajo_cama");

        GameObject table = PlaceRoomTable(rooms, Cell(9f, 14f));
        Sprite prop = EcosAssetFactory.PhotoProp != null ? EcosAssetFactory.PhotoProp : EcosAssetFactory.Square;
        Visual(table.transform, prop, new Color(0.72f, 0.62f, 0.42f), new Vector2(0.42f, 0.52f), -12, "Fotografia", new Vector2(0f, 0.05f), -8f);
        Visual(table.transform, EcosAssetFactory.Glow, new Color(1f, 0.9f, 0.55f, 0.25f), new Vector2(0.55f, 0.65f), -13, "BrilloFoto", new Vector2(0f, 0.05f));
        TriggerChild(table.transform, new Vector2(2.4f, 2.6f));
        var photo = table.AddComponent<PhotoInspectable>();
        Set(photo, "prompt", "Examinar fotografía");
        Set(photo, "frontSprite", EcosAssetFactory.PhotoCatalinaFront);
        Set(photo, "backSprite", EcosAssetFactory.PhotoCatalinaBack);
        Set(photo, "frontCaption",
            "Una fotografía familiar: una mujer con rebozo junto al aljibe, abrazando a dos niños. Alguien rayó su rostro.");
        Set(photo, "backCaption", "Al reverso, con tinta desvanecida:\n«Catalina y sus hijos, 1909».");
        Set(photo, "flagOnExamine", "foto_familia");
        Candle(rooms, Cell(10.5f, 19.5f));

        EcosFurniture.Bed(rooms, "Cama2", Cell(2.5f, 24f), new Vector2(2f, 2.6f));
        EcosFurniture.Crib(rooms, "Cuna", Cell(8.5f, 24f), new Vector2(1.2f, 1.6f),
            "Una cuna vacía. La madera está húmeda, como si alguien la hubiera tocado con las manos mojadas.");
        Note(rooms, "NotaRota", Cell(4f, 27.5f), EcosAssetFactory.NotaRota, "Recoger nota rota", new Vector2(1.6f, 1.6f));
        PlaceRoomWardrobe(rooms, Cell(10.4f, 28.5f));
        if (EcosAbandonedHouseArt.Nightstand != null)
            EcosFurniture.FromSprite(rooms, "Mesita", EcosAbandonedHouseArt.Nightstand, Cell(5.5f, 14.2f), new Vector2(1.1f, 1.3f));
    }

    private static GameObject PlaceOfficeDesk(Transform office, Vector2 pos)
    {
        if (EcosAbandonedHouseArt.Desk != null)
            return EcosFurniture.FromSprite(office, "Escritorio", EcosAbandonedHouseArt.Desk, pos, new Vector2(2.6f, 1.7f));
        return EcosFurniture.Desk(office, "Escritorio", pos, new Vector2(3.4f, 1.2f));
    }

    private static void PlaceOfficeChair(Transform office, Vector2 pos)
    {
        if (EcosAbandonedHouseArt.Chair != null)
            EcosFurniture.FromSprite(office, "Silla", EcosAbandonedHouseArt.Chair, pos, new Vector2(1.1f, 1.5f));
        else
            EcosFurniture.Chair(office, "Silla", pos, new Vector2(0.8f, 0.7f));
    }

    private static void PlaceOfficeBookshelf(Transform office, Vector2 pos)
    {
        if (EcosAbandonedHouseArt.Bookshelf != null)
            EcosFurniture.FromSprite(office, "Librero", EcosAbandonedHouseArt.Bookshelf, pos, new Vector2(1.1f, 2.6f));
        else
            EcosFurniture.Bookshelf(office, "Librero", pos, new Vector2(1f, 3.2f));
    }

    private static void PlaceOfficeDresser(Transform office, Vector2 pos)
    {
        if (EcosAbandonedHouseArt.Dresser != null)
            EcosFurniture.FromSprite(office, "Archivero", EcosAbandonedHouseArt.Dresser, pos, new Vector2(1.8f, 1.3f));
        else
            EcosFurniture.FilingCabinet(office, "Archivero", pos, new Vector2(1f, 1.4f));
    }

    private static GameObject PlaceRoomTable(Transform rooms, Vector2 pos)
    {
        if (EcosAbandonedHouseArt.Dresser != null)
            return EcosFurniture.FromSprite(rooms, "Mesa", EcosAbandonedHouseArt.Desk != null ? EcosAbandonedHouseArt.Desk : EcosAbandonedHouseArt.Dresser,
                pos, new Vector2(1.6f, 1.2f));
        return EcosFurniture.Desk(rooms, "Mesa", pos, new Vector2(1.4f, 0.9f));
    }

    private static void PlaceRoomWardrobe(Transform rooms, Vector2 pos)
    {
        if (EcosAbandonedHouseArt.Wardrobe == null)
        {
            EcosFurniture.Wardrobe(rooms, "Escondite_Armario", "el armario", pos, new Vector2(1.1f, 2f));
            return;
        }

        GameObject go = EcosFurniture.FromSprite(rooms, "Escondite_Armario", EcosAbandonedHouseArt.Wardrobe, pos, new Vector2(1.2f, 2.4f), false);
        Box(go, new Vector2(1.5f, 2.7f), true);
        var spot = go.AddComponent<HidingSpot>();
        Set(spot, "spotName", "el armario");
    }

    private static void BuildField(LevelContext ctx)
    {
        Transform field = Area(ctx, "Campo");

        DoorController gate = Door(field, "RejaCampo", Cell(30f, 18.5f), true, new Color(0.3f, 0.3f, 0.33f),
            DoorState.BloqueadaPorLlave, "Abrir la reja", "Una reja con un candado oxidado. Necesitas una llave.");
        Set(gate, "requiredItemId", "llave_oficina");
        Set(gate, "requiredItemName", "Llave oxidada");
        Set(gate, "flagOnOpen", "puerta_campo");

        var rng = new System.Random(7);
        var flowerList = new List<Transform>(150);
        for (int i = 0; i < 150; i++)
        {
            float x = 31.2f + (float)rng.NextDouble() * 15.3f;
            float row = 14.2f + (float)rng.NextDouble() * 16.4f;
            float s = 0.28f + (float)rng.NextDouble() * 0.28f;
            Color c = rng.Next(3) == 0 ? new Color(0.95f, 0.75f, 0.15f) : new Color(0.95f, 0.45f, 0.08f);
            SpriteRenderer flower = Decor(field, "Cempasuchil", EcosAssetFactory.Flower, c, Cell(x, row), new Vector2(s, s), -60, (float)rng.NextDouble() * 360f);
            flowerList.Add(flower.transform);
        }

        var sway = field.gameObject.AddComponent<FlowerFieldSway>();
        sway.Configure(flowerList.ToArray(), ctx.Player.transform);

        MakeAmbientParticles(field, "PetalosCampo", Cell(38.5f, 22f), new Vector2(14f, 12f),
            new Color(1f, 0.7f, 0.25f, 0.4f), 5f, EcosAssetFactory.Flower);
        MakeAmbientParticles(field, "PolvoCampo", Cell(38.5f, 18f), new Vector2(16f, 14f),
            new Color(0.75f, 0.78f, 0.85f, 0.22f), 3f, EcosAssetFactory.Circle);

        GameObject search = GO("ZonaBusqueda", field, Cell(38.5f, 26f));
        Box(search, new Vector2(16f, 9f), true);
        GameObject hidden = GO("PuntoOculto", search.transform, Cell(43f, 27f));
        SpriteRenderer glint = Decor(hidden.transform, "Brillo", EcosAssetFactory.Glow, new Color(1f, 0.95f, 0.7f, 0.4f), Cell(43f, 27f), new Vector2(0.7f, 0.7f), -55);
        var zone = search.AddComponent<SearchZone>();
        Set(zone, "prompt", "Buscar entre las flores");
        Set(zone, "hiddenSpot", hidden.transform);
        Set(zone, "foundNote", EcosAssetFactory.Medallon);
        Set(zone, "glint", glint);
        Set(zone, "itemId", "medallon_plata");
        Set(zone, "itemName", "Mitad de un medallón de plata");

        GameObject glimpse = GO("SiluetaLejana", field, Cell(38.5f, 12.6f));
        Visual(glimpse.transform, EcosAssetFactory.Circle, new Color(0.85f, 0.9f, 1f, 0.55f), new Vector2(0.8f, 1.4f), 12, "Silueta");

        GameObject ambient = GO("EventoLlantoLejano", field, Cell(32.5f, 18.5f));
        Box(ambient, new Vector2(2f, 3f), true);
        AudioSource distant = MakeAudio(ambient, EcosAssetFactory.Lament, 0.3f, false, false);
        var evt = ambient.AddComponent<AmbientEventTrigger>();
        Set(evt, "message", "Entre las flores se escucha un llanto lejano...");
        Set(evt, "audioSource", distant);
        Set(evt, "glimpse", glimpse);

        CheckpointAt(field, Cell(33.5f, 20.5f));
    }

    private static AljibePuzzle BuildAljibe(LevelContext ctx)
    {
        Transform yard = Area(ctx, "PatioAljibe");

        EcosFurniture.WellParts well = EcosFurniture.Well(yard, Cell(38.5f, 6f));
        // Luz de luna fría permanente: contraste con la linterna cálida del jugador.
        MakeLight(yard, "LuzLuna", Light2D.LightType.Point, new Color(0.55f, 0.7f, 1f), 0.55f, 7f, Cell(38.5f, 6f));
        MakeAmbientParticles(yard, "NieblaAljibe", Cell(38.5f, 7f), new Vector2(8f, 6f),
            new Color(0.7f, 0.85f, 1f, 0.18f), 2.5f, EcosAssetFactory.Glow);

        GameObject puzzleGo = GO("AcertijoAljibe", yard, Cell(38.5f, 9.5f));
        var puzzle = puzzleGo.AddComponent<AljibePuzzle>();

        PuzzlePlate luna = Plate(yard, puzzle, "PlacaLuna", "luna", "la luna", EcosAssetFactory.Moon, new Color(0.85f, 0.88f, 1f), Cell(35f, 9.5f));
        PuzzlePlate flor = Plate(yard, puzzle, "PlacaFlor", "flor", "la flor", EcosAssetFactory.Flower, new Color(1f, 0.55f, 0.1f), Cell(38.5f, 10f));
        PuzzlePlate gota = Plate(yard, puzzle, "PlacaGota", "gota", "la gota", EcosAssetFactory.Drop, new Color(0.4f, 0.7f, 1f), Cell(42f, 9.5f));

        Set(puzzle, "correctOrder", new[] { "gota", "luna", "flor" });
        Set(puzzle, "plates", new Object[] { luna, flor, gota });
        Set(puzzle, "requiredClues", 3);
        Set(puzzle, "notReadyMessage", "Tres placas: una gota, una luna y una flor. Sin más pistas no sabes en qué orden presionarlas.");
        Set(puzzle, "flagOnSolved", "aljibe_resuelto");
        Set(puzzle, "solvedMessage", "Las placas se hunden. El agua del aljibe empieza a subir...");
        Set(puzzle, "aljibeRenderer", well.Water);
        Set(puzzle, "aljibeLight", well.PuzzleLight);

        GameObject cold = GO("EventoAireFrio", yard, Cell(38.5f, 12.5f));
        Box(cold, new Vector2(2f, 1f), true);
        Set(cold.AddComponent<AmbientEventTrigger>(), "message", "El aire se vuelve helado junto al aljibe.");

        CheckpointAt(yard, Cell(38.5f, 11.8f));
        return puzzle;
    }

    private static void BuildLlorona(LevelContext ctx, SequencePuzzle puzzle)
    {
        Transform root = Area(ctx, "Llorona");

        Vector2[] route =
        {
            Cell(35f, 11f), Cell(33f, 2f), Cell(21.5f, 2f), Cell(21.5f, 7f), Cell(15.5f, 15f), Cell(26f, 24f)
        };

        EnemyBrain brain = BuildEnemy(root, new EnemySpec
        {
            Name = "LaLlorona",
            BodyColor = new Color(0.9f, 0.93f, 1f),
            DetailColor = new Color(0.05f, 0.05f, 0.08f),
            BodySize = new Vector2(1.15f, 1.35f),
            ConeColor = new Color(0.55f, 0.75f, 1f, 0.12f),
            Voice = EcosAssetFactory.Lament,
            ViewDistance = 5.5f,
            ViewAngle = 95f,
            Hearing = 0.55f,
            PatrolSpeed = 1.45f,
            ChaseSpeed = 3.35f,
            CloseChaseDistance = 2.3f,
            StartActive = false,
            VanishOnReturn = true,
            Animator = EcosAnimationFactory.LloronaController
        }, route, route[1]);

        // Halo suave bajo la Llorona.
        if (brain.transform.Find("Cuerpo") != null)
            Visual(brain.transform, EcosAssetFactory.Glow, new Color(0.55f, 0.75f, 1f, 0.28f), new Vector2(1.8f, 1.8f), 4, "Aura");

        SavePrefabCopy(brain.gameObject, "Assets/Prefabs/Enemies/LaLlorona.prefab");

        GameObject encounterGo = GO("EncuentroLlorona", root);
        AudioSource lament = MakeAudio(encounterGo, EcosAssetFactory.Lament, 0.8f, false, false);
        var encounter = encounterGo.AddComponent<LloronaEncounter>();
        Set(encounter, "triggerPuzzle", puzzle);
        Set(encounter, "llorona", brain);
        Set(encounter, "apparitionPoint", GO("PuntoAparicion", root, Cell(38.5f, 3.2f)).transform);
        Set(encounter, "huntStartPoint", root.Find("PuntosPatrulla/PuntoPatrulla2"));
        Set(encounter, "lamentSource", lament);
        Set(encounter, "globalLight", ctx.GlobalLight);
        Set(encounter, "dimmedIntensity", 0.16f);
        Set(encounter, "apparitionTime", 3.2f);
        Set(encounter, "delayBeforeHunt", 1.6f);
        MakeAmbientParticles(root, "NieblaAparicion", Cell(38.5f, 4f), new Vector2(6f, 4f),
            new Color(0.65f, 0.8f, 1f, 0.2f), 3f, EcosAssetFactory.Glow);
    }
}
