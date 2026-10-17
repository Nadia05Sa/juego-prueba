using UnityEngine;
using UnityEngine.SceneManagement;
using static EcosBuildUtils;
using static EcosLevelKit;

/// <summary>Construye el menú principal y la pantalla final de victoria.</summary>
public static class EcosMenuBuilder
{
    public static void BuildMenuScene()
    {
        Scene scene = NewScene();
        MakeCamera(new Vector3(0f, 0f, -10f), new Color(0.05f, 0.02f, 0.02f));
        MakeEventSystem();

        var music = new GameObject("Musica");
        MakeAudio(music, EcosAssetFactory.MenuPad, 0.28f, true, true);

        Canvas canvas = MakeCanvas("Canvas");
        Transform root = canvas.transform;
        MakeImage(Stretch(root, "Fondo"), new Color(0.04f, 0.02f, 0.03f), null, false);

        MakeText(MakeRect(root, "Titulo", Center, Center, Center, new Vector2(0f, 320f), new Vector2(1400f, 140f)),
            "Ecos del Mictlán", 110, new Color(0.95f, 0.55f, 0.2f), TextAnchor.MiddleCenter);
        MakeText(MakeRect(root, "Subtitulo", Center, Center, Center, new Vector2(0f, 220f), new Vector2(1400f, 60f)),
            "Una hacienda abandonada. Tres leyendas. Una sola salida.", 32, TextColor, TextAnchor.MiddleCenter);

        RectTransform controls = Stretch(root, "PanelControles");
        MakeImage(controls, new Color(0f, 0f, 0f, 0.92f));
        MakeText(MakeRect(controls, "TextoControles", Center, Center, Center, new Vector2(0f, 60f), new Vector2(1000f, 520f)),
            "<b>Controles</b>\n\n" +
            "WASD / Flechas — Moverse\n" +
            "Shift — Correr (hace más ruido)\n" +
            "E — Interactuar, leer, recoger\n" +
            "Q — Esconderse / salir del escondite\n" +
            "I — Abrir el inventario\n" +
            "F — Linterna (nivel 3)\n" +
            "Esc — Pausa / cerrar documento",
            34, TextColor, TextAnchor.MiddleCenter);

        MakeButton(root, "Nueva partida", new Vector2(0f, 80f), MenuAction.NuevaPartida);
        MakeButton(root, "Continuar", new Vector2(0f, -10f), MenuAction.ContinuarPartida);
        MakeButton(root, "Controles", new Vector2(0f, -100f), MenuAction.AlternarPanel, null, controls.gameObject);
        MakeButton(root, "Salir", new Vector2(0f, -190f), MenuAction.Salir);
        MakeButton(controls, "Cerrar", new Vector2(0f, -320f), MenuAction.AlternarPanel, null, controls.gameObject);
        controls.SetAsLastSibling();
        controls.gameObject.SetActive(false);

        MakeText(MakeRect(root, "Aviso", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1600f, 40f)),
            "Historia y personajes ficticios inspirados en el folclore mexicano.", 22, new Color(0.7f, 0.65f, 0.6f), TextAnchor.MiddleCenter);

        Save(scene, MenuScene);
    }

    public static void BuildVictoryScene()
    {
        Scene scene = NewScene();
        MakeCamera(new Vector3(0f, 0f, -10f), Color.black);
        MakeEventSystem();

        var music = new GameObject("Musica");
        MakeAudio(music, EcosAssetFactory.MenuPad, 0.22f, true, true);

        Canvas canvas = MakeCanvas("Canvas");
        Transform root = canvas.transform;
        MakeImage(Stretch(root, "Fondo"), new Color(0.03f, 0.04f, 0.06f), null, false);

        MakeText(MakeRect(root, "Titulo", Center, Center, Center, new Vector2(0f, 300f), new Vector2(1600f, 120f)),
            "Escapaste de la hacienda", 90, new Color(0.95f, 0.75f, 0.35f), TextAnchor.MiddleCenter);
        MakeText(MakeRect(root, "Texto", Center, Center, Center, new Vector2(0f, 140f), new Vector2(1400f, 200f)),
            "El lamento del aljibe, el silencio de la cripta y el pacto del Charro Negro quedan atrás.\n" +
            "Con el medallón unido, el acuerdo se rompió. Detrás del portón, la hacienda vuelve a quedar en silencio...\n" +
            "por ahora.",
            34, TextColor, TextAnchor.MiddleCenter);

        MakeButton(root, "Volver a jugar", new Vector2(0f, -40f), MenuAction.NuevaPartida);
        MakeButton(root, "Menú principal", new Vector2(0f, -130f), MenuAction.MenuPrincipal, MenuScene);
        MakeButton(root, "Salir", new Vector2(0f, -220f), MenuAction.Salir);

        MakeText(MakeRect(root, "Creditos", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1600f, 80f)),
            "Ecos del Mictlán\nHistoria y personajes ficticios inspirados en el folclore mexicano.",
            22, new Color(0.7f, 0.65f, 0.6f), TextAnchor.MiddleCenter);

        Save(scene, VictoryScene);
    }
}
