using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Recorre cada nivel en modo Play llamando directamente a las interacciones y comprueba
/// puertas, pistas, acertijos, enemigos, vidas, reintento, salida y transición. El resultado sale en la consola.
/// </summary>
[InitializeOnLoad]
public static class EcosSmokeTest
{
    private const string RunningKey = "EcosSmokeTestRunning";
    private const string LevelKey = "EcosSmokeTestLevel";
    private const string ChainKey = "EcosSmokeTestChain";
    private const string TotalKey = "EcosSmokeTestTotal";

    private static readonly string[] Scenes = { "", "Nivel1_Aljibe", "Nivel2_Cripta", "Nivel3_Establos" };

    private static readonly List<string> errors = new List<string>();
    private static readonly List<string> failures = new List<string>();
    private static readonly List<(float delay, string name, Action action)> steps = new List<(float, string, Action)>();
    private static int index;
    private static double nextTime;
    private static double deadline;
    private static double waitStart;
    private static float storedValue;

    private static int Level => SessionState.GetInt(LevelKey, 1);
    private static string SceneName => Scenes[Mathf.Clamp(Level, 1, 3)];

    static EcosSmokeTest()
    {
        if (SessionState.GetBool(RunningKey, false)) Setup();
    }

    [MenuItem("Ecos del Mictlán/Prueba automática del Nivel 1", false, 21)]
    public static void RunLevel1() => Start(1, false);

    [MenuItem("Ecos del Mictlán/Prueba automática del Nivel 2", false, 22)]
    public static void RunLevel2() => Start(2, false);

    [MenuItem("Ecos del Mictlán/Prueba automática del Nivel 3", false, 23)]
    public static void RunLevel3() => Start(3, false);

    [MenuItem("Ecos del Mictlán/Prueba automática de los tres niveles", false, 24)]
    public static void RunAll() => Start(1, true);

    /// <summary>Para modo batch: -executeMethod EcosSmokeTest.Run con -ecosLevel N (0 = todos).</summary>
    public static void Run()
    {
        int level = 0;
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-ecosLevel") int.TryParse(args[i + 1], out level);
        if (level <= 0) Start(1, true);
        else Start(level, false);
    }

    private static void Start(int level, bool chain)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetInt(LevelKey, level);
        SessionState.SetBool(ChainKey, chain);
        SessionState.SetInt(TotalKey, 0);
        SessionState.SetBool(RunningKey, true);
        ResetRun();
        EditorSceneManager.OpenScene($"Assets/Scenes/{SceneName}.unity");
        Setup();
    }

    private static void ResetRun()
    {
        errors.Clear();
        failures.Clear();
        index = 0;
        deadline = 0;
        waitStart = 0;
    }

    private static void Setup()
    {
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        steps.Clear();
        switch (Level)
        {
            case 1: BuildLevel1Steps(); break;
            case 2: BuildLevel2Steps(); break;
            default: BuildLevel3Steps(); break;
        }
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) errors.Add(condition + "\n" + stackTrace);
    }

    private static void WaitAndPlay()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            waitStart = 0;
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (waitStart == 0) waitStart = now;
        if (now - waitStart < 8) return;
        if (SceneManager.GetActiveScene().name != SceneName)
            EditorSceneManager.OpenScene($"Assets/Scenes/{SceneName}.unity");
        Debug.Log($"SMOKE: entrando en Play ({SceneName})");
        EditorApplication.EnterPlaymode();
    }

    // ---------- Utilidades ----------

    private static T Find<T>(string name) where T : Component
    {
        GameObject go = GameObject.Find(name);
        if (go == null) { failures.Add("No existe " + name); return null; }
        return go.GetComponent<T>();
    }

    private static void Check(bool ok, string what)
    {
        Debug.Log((ok ? "[OK] " : "[FALLO] ") + what);
        if (!ok) failures.Add(what);
    }

    private static void CheckWalls()
    {
        var walls = GameObject.Find("Muros")?.GetComponent<CompositeCollider2D>();
        Check(walls != null && walls.pathCount > 0, "Los muros tienen colisión (" + (walls != null ? walls.pathCount : 0) + " contornos)");
        if (walls == null) return;
        Vector2 center = (Vector2)walls.bounds.center;
        Check(!LineOfSight.Clear(center + Vector2.left * 100f, center + Vector2.right * 100f), "Los muros bloquean la línea de visión");
    }

    private static void CheckPatrolRoute(string enemyName)
    {
        GameObject enemy = GameObject.Find(enemyName);
        var patrol = enemy.GetComponent<EnemyPatrol>();
        NavGraph nav = new SerializedObject(enemy.GetComponent<EnemyBrain>()).FindProperty("navGraph").objectReferenceValue as NavGraph;
        var blocked = new List<string>();
        for (int i = 0; i + 1 < patrol.Count; i++)
        {
            Vector2 a = patrol.PointAt(i), b = patrol.PointAt(i + 1);
            bool ok = LineOfSight.ClearWide(a, b, 0.35f) || (nav != null && nav.TryGetNextPoint(a, b, out _));
            if (!ok)
            {
                blocked.Add($"{i + 1}->{i + 2}");
                LogBlockedSegments(a, b);
            }
        }
        Check(blocked.Count == 0, $"La patrulla de {enemyName} no atraviesa muros" + (blocked.Count > 0 ? " (bloqueados: " + string.Join(", ", blocked) + ")" : ""));
    }

    private static void LogBlockedSegments(params Vector2[] path)
    {
        var hits = new List<RaycastHit2D>();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        for (int i = 0; i + 1 < path.Length; i++)
        {
            Vector2 a = path[i], b = path[i + 1];
            hits.Clear();
            Physics2D.CircleCast(a, 0.3f, (b - a).normalized, filter, hits, (b - a).magnitude);
            foreach (RaycastHit2D h in hits)
            {
                Rigidbody2D body = h.collider.attachedRigidbody;
                if (body != null && body.bodyType == RigidbodyType2D.Dynamic) continue;
                Debug.Log($"SMOKE NAV: tramo {a} -> {b} bloqueado por {h.collider.name} (padre {h.collider.transform.parent?.name}) en {h.point}");
                break;
            }
        }
    }

    private static InteractionController Player => GameObject.FindWithTag("Player").GetComponent<InteractionController>();

    private static string Objective => GameObject.Find("Objetivo").GetComponent<UnityEngine.UI.Text>().text;

    private static void Close()
    {
        if (UIManager.Instance.IsReading) UIManager.Instance.CloseNote();
    }

    private static void Read(string noteName)
    {
        Find<NoteInteractable>(noteName).Interact(Player);
        Close();
    }

    private static void Press(params string[] plates)
    {
        foreach (string p in plates) Find<PuzzlePlate>(p).Interact(Player);
    }

    private static Vector2 Cell(float x, float row) => new Vector2(x + 0.5f, 31.5f - row);

    // ---------- Nivel 1 ----------

    private static void BuildLevel1Steps()
    {
        steps.Add((1.5f, "inicio", () =>
        {
            Check(LevelManager.Instance != null && UIManager.Instance != null && GameInput.Instance != null, "Sistemas presentes");
            CheckWalls();
            Check(Objective.Contains("oficina"), "Objetivo inicial");
            Check(GameProgress.SavedScene == "Nivel1_Aljibe", "El progreso guarda el nivel actual");
            Find<DoorController>("Porton").Interact(Player);
            Check(Find<DoorController>("Porton").State == DoorState.BloqueadaPorObjetivo, "Portón bloqueado al inicio");
            Find<DoorController>("PuertaOficina").Interact(Player);
        }));
        steps.Add((1f, "oficina", () =>
        {
            Check(LevelManager.Instance.HasFlag("puerta_oficina"), "Puerta de la oficina abierta");
            Find<DoorController>("RejaCampo").Interact(Player);
            Check(Find<DoorController>("RejaCampo").State == DoorState.BloqueadaPorLlave, "Reja rechaza sin llave");
            Find<NoteInteractable>("Carta").Interact(Player);
            Check(UIManager.Instance.IsReading, "Panel de lectura abierto");
        }));
        steps.Add((0.3f, "carta", () =>
        {
            Close();
            Check(LevelManager.Instance.HasFlag("carta_oficina") && ClueManager.Instance.Count == 1, "Carta registrada como pista 1");
            Read("Carta");
            Check(ClueManager.Instance.Count == 1, "Releer la carta no duplica la pista");
            Find<KeyItem>("Cajon").Interact(Player);
            Check(InventoryManager.Instance.Has("llave_oficina"), "Llave en el inventario");
            Find<DoorController>("RejaCampo").Interact(Player);
        }));
        steps.Add((1f, "campo", () =>
        {
            Check(LevelManager.Instance.HasFlag("puerta_campo"), "Reja del campo abierta con la llave");
            Read("NotaRota");
            Find<SearchZone>("ZonaBusqueda").Interact(Player);
            Check(ClueManager.Instance.Count == 2, "Buscar lejos del punto oculto no da la pista");
            Player.Movement.Teleport(GameObject.Find("PuntoOculto").transform.position);
        }));
        steps.Add((0.3f, "medallon", () =>
        {
            Find<SearchZone>("ZonaBusqueda").Interact(Player);
            Close();
            Check(ClueManager.Instance.Count == 3, "Tres pistas reunidas");
            Check(InventoryManager.Instance.Has("medallon_plata"), "El medallón queda en el inventario");
            Press("PlacaLuna", "PlacaGota", "PlacaFlor");
            Check(Find<PuzzlePlate>("PlacaLuna").State == PlateState.Error, "Orden incorrecto marca error");
        }));
        steps.Add((1.6f, "acertijo", () =>
        {
            Check(Find<PuzzlePlate>("PlacaLuna").State == PlateState.Inactiva, "Acertijo se reinicia tras error");
            Check(!LevelManager.Instance.HasFlag("aljibe_resuelto"), "Orden incorrecto no resuelve");
            Press("PlacaGota", "PlacaLuna", "PlacaFlor");
            Check(LevelManager.Instance.HasFlag("aljibe_resuelto"), "Orden correcto resuelve el acertijo");
            Check(Find<DoorController>("Porton").State == DoorState.Cerrada, "Portón desbloqueado");
            Player.Movement.Teleport(new Vector2(22f, 3f));
        }));
        steps.Add((9f, "llorona", () =>
        {
            EnemyBrain llorona = Find<EnemyBrain>("LaLlorona");
            Check(Find<DoorController>("PuertaCorredorNorte").State == DoorState.Abierta, "Corredor norte abierto");
            Check(llorona.State != EnemyState.Inactivo, "La Llorona patrulla (estado: " + llorona.State + ")");
            CheckPatrolRoute("LaLlorona");
            Player.Movement.Teleport(llorona.transform.position);
        }));
        steps.Add((0.6f, "dano", () =>
        {
            PlayerHealth h = Player.GetComponent<PlayerHealth>();
            Check(h.Lives == 2, "Contacto con La Llorona quita una vida (vidas: " + h.Lives + ")");
            Check(!h.TakeDamage(), "Invulnerabilidad temporal");
            Player.Movement.Teleport(new Vector2(22f, 3f));
        }));
        steps.Add((1.7f, "muerte1", () => Player.GetComponent<PlayerHealth>().TakeDamage()));
        steps.Add((1.7f, "muerte2", () =>
        {
            Player.GetComponent<PlayerHealth>().TakeDamage();
            Check(UIManager.Instance.IsGameOver && Time.timeScale == 0f, "Game Over con 0 vidas");
        }));
        steps.Add((0.5f, "reintento", () =>
        {
            LevelManager.Instance.RetryFromCheckpoint();
            PlayerHealth h = Player.GetComponent<PlayerHealth>();
            Check(h.Lives == 3 && !UIManager.Instance.IsGameOver && Time.timeScale == 1f, "Reintento desde punto de control");
            Check(InventoryManager.Instance.Has("llave_oficina") && ClueManager.Instance.Count == 3, "El reintento conserva el progreso");
            Find<DoorController>("Porton").Interact(Player);
        }));
        steps.Add((1f, "salida", () =>
        {
            Check(Find<DoorController>("Porton").State == DoorState.Abierta, "Portón abierto");
            Player.Movement.Teleport(new Vector2(22f, 0.5f));
        }));
        steps.Add((2.5f, "panel", () => Check(UIManager.Instance.IsLevelComplete, "Pantalla de nivel completado")));
        steps.Add((6f, "nivel2", () =>
        {
            Check(SceneManager.GetActiveScene().name == "Nivel2_Cripta", "Carga el Nivel 2");
            Check(GameProgress.SavedScene == "Nivel2_Cripta", "Progreso guardado en el Nivel 2");
            Check(InventoryManager.Instance != null && InventoryManager.Instance.Has("medallon_plata"), "El medallón pasa al Nivel 2");
        }));
    }

    // ---------- Nivel 2 ----------

    private static void BuildLevel2Steps()
    {
        steps.Add((1.5f, "inicio", () =>
        {
            Check(LevelManager.Instance != null && UIManager.Instance != null, "Sistemas presentes");
            CheckWalls();
            Check(Objective.Contains("capilla"), "Objetivo inicial");
            EnemyBrain momia = Find<EnemyBrain>("LaMomia");
            Check(momia.State == EnemyState.Reposo, "La momia empieza en reposo");
            NoiseSystem.Emit(GameObject.Find("PuntoInicio").transform.position, 1.5f, Player.gameObject);
            Check(momia.State == EnemyState.Reposo, "Caminar lejos no la despierta");
            Find<DoorController>("RejaCriptas").Interact(Player);
            Check(Find<DoorController>("RejaCriptas").State == DoorState.BloqueadaPorObjetivo, "La reja de las criptas está bloqueada");
            Find<DoorController>("PuertaCapilla").Interact(Player);
        }));
        steps.Add((1f, "capilla", () =>
        {
            Check(LevelManager.Instance.HasFlag("puerta_capilla"), "Puerta de la capilla abierta");
            Find<Lever>("MecanismoSacristia").Interact(Player);
            Check(!Find<Lever>("MecanismoSacristia").IsActivated, "El mecanismo exige leer las pistas");
            Find<DoorController>("PuertaSacristia").Interact(Player);
        }));
        steps.Add((1f, "sacristia", () =>
        {
            Check(LevelManager.Instance.HasFlag("puerta_sacristia"), "Puerta de la sacristía abierta");
            Press("PlacaSol");
            Check(Find<PuzzlePlate>("PlacaSol").State == PlateState.Inactiva, "Las placas exigen las pistas");
            Read("DiarioSacristan");
            Read("OracionCapellan");
            Check(ClueManager.Instance.Count == 2, "Dos pistas de la sacristía");
            Find<Lever>("MecanismoSacristia").Interact(Player);
            Check(Find<Lever>("MecanismoSacristia").IsActivated, "Mecanismo accionado");
        }));
        steps.Add((1.4f, "mecanismo", () =>
        {
            EnemyBrain momia = Find<EnemyBrain>("LaMomia");
            Check(LevelManager.Instance.HasFlag("mecanismo_sacristia"), "Bandera del mecanismo");
            Check(momia.State == EnemyState.Patrulla, "La momia despierta y patrulla (estado: " + momia.State + ")");
            Check(Find<DoorController>("RejaCriptas").State == DoorState.Abierta, "La reja de las criptas se abre");

            NavGraph nav = UnityEngine.Object.FindAnyObjectByType<NavGraph>();
            Vector2 from = GameObject.Find("PuntoInicio").transform.position;
            Vector2 to = Cell(6.5f, 4f);
            bool found = nav.TryGetNextPoint(from, to, out Vector2 next);
            Check(found && (next - to).sqrMagnitude > 0.01f, "La navegación encuentra una ruta rodeando muros");
            if (!found || (next - to).sqrMagnitude <= 0.01f) LogBlockedSegments(from, Cell(21.5f, 28f), Cell(21.5f, 25f), Cell(21.5f, 23.5f), Cell(13.5f, 23.5f),
                Cell(13.5f, 18.5f), Cell(11.5f, 18.5f), Cell(6.5f, 18.5f), Cell(6.5f, 12f), to);

            NoiseSystem.Emit((Vector2)momia.transform.position + Vector2.right, 2f, Player.gameObject);
            Check(momia.State == EnemyState.Investigar, "La momia investiga un ruido cercano");
        }));
        steps.Add((0.5f, "ruido", () =>
        {
            ChainTrap chains = Find<ChainTrap>("CadenasOeste");
            chains.Rattle(false);
            Check(chains.RattleCount == 1, "Las cadenas suenan");
            Press("PlacaLuna", "PlacaSol", "PlacaEspiral", "PlacaCalavera");
            Check(Find<PuzzlePlate>("PlacaLuna").State == PlateState.Error, "Orden incorrecto de símbolos marca error");
        }));
        steps.Add((1.6f, "acertijo", () =>
        {
            Check(Find<PuzzlePlate>("PlacaLuna").State == PlateState.Inactiva, "Los símbolos se reinician tras el error");
            Press("PlacaSol", "PlacaLuna", "PlacaEspiral", "PlacaCalavera");
            Check(LevelManager.Instance.HasFlag("cripta_resuelta"), "Orden sol, luna, espiral, calavera resuelve");
        }));
        steps.Add((2f, "puerta", () =>
        {
            Check(Find<DoorController>("PuertaPiedra").State == DoorState.Abierta, "La puerta de piedra se abre");
            EnemyBrain momia = Find<EnemyBrain>("LaMomia");
            Check(!momia.IsHarmless, "La momia está activa (estado: " + momia.State + ")");
            CheckPatrolRoute("LaMomia");
            Player.Movement.Teleport(momia.transform.position);
        }));
        steps.Add((0.5f, "dano", () =>
        {
            PlayerHealth h = Player.GetComponent<PlayerHealth>();
            EnemyBrain momia = Find<EnemyBrain>("LaMomia");
            Check(h.Lives == 2, "Contacto con la momia quita una vida (vidas: " + h.Lives + ")");
            Check(momia.State == EnemyState.Aturdido, "La momia queda aturdida tras golpear");
            Find<KeyItem>("MitadMedallon").Interact(Player);
            Check(InventoryManager.Instance.Has("pieza_medallon"), "Mitad del medallón en el inventario");
            Check(UIManager.Instance.IsReading, "La carta sellada se muestra al tomar el medallón");
            Check(ClueManager.Instance != null && ClueManager.Instance.Has("carta_sellada"), "La carta queda en el inventario");
            if (UIManager.Instance.IsReading) UIManager.Instance.CloseNote();
            Player.Movement.Teleport(GameObject.Find("Salida").transform.position);
        }));
        steps.Add((2.5f, "panel", () => Check(UIManager.Instance.IsLevelComplete, "Pantalla de nivel completado")));
        steps.Add((6f, "nivel3", () =>
        {
            Check(SceneManager.GetActiveScene().name == "Nivel3_Establos", "Carga el Nivel 3");
            Check(GameProgress.SavedScene == "Nivel3_Establos", "Progreso guardado en el Nivel 3");
            Check(InventoryManager.Instance != null && InventoryManager.Instance.Has("pieza_medallon"), "La mitad del medallón pasa al Nivel 3");
        }));
    }

    // ---------- Nivel 3 ----------

    private static void BuildLevel3Steps()
    {
        steps.Add((1.5f, "inicio", () =>
        {
            Check(LevelManager.Instance != null && UIManager.Instance != null, "Sistemas presentes");
            CheckWalls();
            FlashlightController f = Player.GetComponent<FlashlightController>();
            Check(f != null && !f.IsOn && Mathf.Approximately(f.Battery, 100f), "Linterna apagada con batería llena");
            Check(Find<EnemyBrain>("CharroNegro").State == EnemyState.Inactivo, "El Charro aún no aparece");
            Find<Lever>("PalancaPorton").Interact(Player);
            Check(!Find<Lever>("PalancaPorton").IsActivated, "La palanca exige documentos y herramienta");
            f.Toggle();
            Check(f.IsOn, "F enciende la linterna");
        }));
        steps.Add((2f, "bateria", () =>
        {
            FlashlightController f = Player.GetComponent<FlashlightController>();
            Check(f.Battery < 100f && f.Battery > 90f, "Encendida consume batería (" + f.Battery.ToString("0.0") + ")");
            f.Toggle();
            storedValue = f.Battery;
        }));
        steps.Add((1.5f, "apagada", () =>
        {
            FlashlightController f = Player.GetComponent<FlashlightController>();
            Check(Mathf.Approximately(f.Battery, storedValue), "Apagada no consume batería");
            Find<BatteryPickup>("Baterias_Patio").Interact(Player);
            Check(Mathf.Approximately(f.Battery, 100f), "Las baterías recargan la linterna");
            Find<DoorController>("PuertaRegistros").Interact(Player);
        }));
        steps.Add((1f, "registros", () =>
        {
            Read("RegistroHacienda");
            Check(LevelManager.Instance.HasFlag("registro_hacienda"), "Registro de la hacienda leído");
            Check(Find<EnemyBrain>("CharroNegro").State != EnemyState.Inactivo, "El Charro aparece tras leer el registro");
            Find<KeyItem>("Archivero").Interact(Player);
            Check(InventoryManager.Instance.Has("llave_herramientas"), "Llave del cuarto de herramientas");
            Find<DoorController>("PuertaHerramientas").Interact(Player);
        }));
        steps.Add((1f, "herramientas", () =>
        {
            Check(Find<DoorController>("PuertaHerramientas").State == DoorState.Abierta, "Cuarto de herramientas abierto con la llave");
            Find<KeyItem>("Barreta").Interact(Player);
            Check(InventoryManager.Instance.Has("barreta"), "Barreta en el inventario");
            Read("NotaPalanca");
            Find<Lever>("PalancaPorton").Interact(Player);
            Check(!Find<Lever>("PalancaPorton").IsActivated, "Sin los tres documentos la palanca no se mueve");

            LightRevealable reveal = Find<LightRevealable>("CartaPacto");
            Check(!reveal.IsRevealed, "La carta escondida no se ve sin luz");
            Vector2 letter = reveal.transform.position;
            Player.Movement.Teleport(letter + Vector2.down * 2.5f);
            FlashlightController f = Player.GetComponent<FlashlightController>();
            f.SetDirection(Vector2.up);
            f.SetOn(true);
        }));
        steps.Add((1.2f, "revelar", () =>
        {
            FlashlightController f = Player.GetComponent<FlashlightController>();
            Check(Find<LightRevealable>("CartaPacto").IsRevealed, "La linterna revela la carta escondida");
            f.SetOn(false);
            Read("CartaPacto");
            Check(ClueManager.Instance.Count == 3, "Tres documentos del pacto");

            EnemyBrain charro = Find<EnemyBrain>("CharroNegro");
            charro.Activate(Cell(15.5f, 10f));
            Player.Movement.Teleport(Cell(11f, 10f));
            f.SetDirection(Vector2.right);
            f.SetOn(true);
        }));
        steps.Add((0.4f, "luz", () =>
        {
            EnemyBrain charro = Find<EnemyBrain>("CharroNegro");
            Check(charro.State == EnemyState.Aturdido, "La luz aturde al Charro (estado: " + charro.State + ")");
            Check(charro.GetComponent<LightReaction>().ReactionCount == 1, "Una reacción a la luz");
        }));
        steps.Add((2f, "enfriamiento", () =>
        {
            EnemyBrain charro = Find<EnemyBrain>("CharroNegro");
            LightReaction reaction = charro.GetComponent<LightReaction>();
            Check(charro.State != EnemyState.Aturdido && reaction.ReactionCount == 1, "El efecto de la luz es breve y con enfriamiento");
            Player.GetComponent<FlashlightController>().SetOn(false);
            Player.Movement.Teleport(Cell(4.5f, 10.5f));
            Find<Lever>("PalancaPorton").Interact(Player);
        }));
        steps.Add((0.8f, "palanca", () =>
        {
            Check(LevelManager.Instance.HasFlag("palanca_activada"), "Palanca accionada");
            Check(Find<FinalChaseSequence>("PersecucionFinal").Started, "Empieza la persecución final");
        }));
        steps.Add((2.5f, "persecucion", () =>
        {
            EnemyBrain charro = Find<EnemyBrain>("CharroNegro");
            Check(charro.State == EnemyState.Persecucion && charro.IsHunting, "El Charro persigue al jugador (estado: " + charro.State + ")");
            CheckPatrolRoute("CharroNegro");
            Check(Find<DoorController>("PortonPrincipal").State == DoorState.Abierta, "El portón principal se abre");
            Player.Movement.Teleport(GameObject.Find("Salida").transform.position);
        }));
        steps.Add((2.5f, "panel", () => Check(UIManager.Instance.IsLevelComplete, "Pantalla final de nivel")));
        steps.Add((6f, "victoria", () =>
        {
            Check(SceneManager.GetActiveScene().name == "Victoria", "Carga la pantalla de Victoria");
            Check(!GameProgress.HasSave, "Al terminar el juego se borra la partida guardada");
        }));
    }

    // ---------- Ejecución ----------

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            deadline = 0;
            WaitAndPlay();
            return;
        }
        if (steps.Count == 0) return;
        double now = EditorApplication.timeSinceStartup;
        if (deadline == 0) { deadline = now + 90; nextTime = now + steps[0].delay; }

        if (now > deadline) { failures.Add("Tiempo agotado"); Finish(); return; }
        if (now < nextTime) return;

        try { steps[index].action(); }
        catch (Exception e) { failures.Add(steps[index].name + ": " + e); }

        index++;
        if (index >= steps.Count) { Finish(); return; }
        nextTime = now + steps[index].delay;
    }

    private static void Finish()
    {
        foreach (string e in errors) Debug.Log("[ERROR REGISTRADO] " + e);
        foreach (string f in failures) Debug.Log("[FALLO] " + f);
        int problems = failures.Count + errors.Count;
        Debug.Log($"SMOKE RESULT nivel {Level}: {failures.Count} fallos, {errors.Count} errores");
        int total = SessionState.GetInt(TotalKey, 0) + problems;
        SessionState.SetInt(TotalKey, total);

        if (SessionState.GetBool(ChainKey, false) && Level < 3)
        {
            SessionState.SetInt(LevelKey, Level + 1);
            ResetRun();
            steps.Clear();
            Setup();
            EditorApplication.ExitPlaymode();
            return;
        }

        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        SessionState.SetBool(RunningKey, false);
        Debug.Log($"SMOKE TOTAL: {total} problemas");

        if (Application.isBatchMode) EditorApplication.Exit(total == 0 ? 0 : 1);
        else EditorApplication.ExitPlaymode();
    }
}
