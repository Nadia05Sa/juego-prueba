using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using static EcosLevelKit;

/// <summary>
/// Menú Ecos del Mictlán: construye las escenas generadas por código y compila el juego.
/// Volver a construir una escena la sobrescribe por completo.
/// </summary>
public static class EcosLevelBuilder
{
    private static readonly string[] SceneOrder = { MenuScene, Level1Scene, Level2Scene, Level3Scene, VictoryScene };

    [MenuItem("Ecos del Mictlán/Construir todo el juego", false, 0)]
    public static void BuildAll()
    {
        if (!Prepare()) return;
        EcosMenuBuilder.BuildMenuScene();
        EcosMenuBuilder.BuildVictoryScene();
        EcosLevel1Builder.Build();
        EcosLevel2Builder.Build();
        EcosLevel3Builder.Build();
        Finish(Level1Scene, "todo el juego");
    }

    [MenuItem("Ecos del Mictlán/Construir solo menú y victoria", false, 11)]
    public static void BuildMenus()
    {
        if (!Prepare()) return;
        EcosMenuBuilder.BuildMenuScene();
        EcosMenuBuilder.BuildVictoryScene();
        Finish(MenuScene, "menú y victoria");
    }

    [MenuItem("Ecos del Mictlán/Construir solo el Nivel 1", false, 12)]
    public static void BuildLevel1()
    {
        if (!Prepare()) return;
        EcosLevel1Builder.Build();
        Finish(Level1Scene, "Nivel 1");
    }

    [MenuItem("Ecos del Mictlán/Construir solo el Nivel 2", false, 13)]
    public static void BuildLevel2()
    {
        if (!Prepare()) return;
        EcosLevel2Builder.Build();
        Finish(Level2Scene, "Nivel 2");
    }

    [MenuItem("Ecos del Mictlán/Construir solo el Nivel 3", false, 14)]
    public static void BuildLevel3()
    {
        if (!Prepare()) return;
        EcosLevel3Builder.Build();
        Finish(Level3Scene, "Nivel 3");
    }

    [MenuItem("Ecos del Mictlán/Compilar para Windows", false, 40)]
    public static void BuildWindows()
    {
        ApplyPlayerSettings();
        UpdateBuildSettings();

        string path = Path.Combine("Builds", "Windows", "EcosDelMictlan.exe");
        var options = new BuildPlayerOptions
        {
            scenes = System.Array.ConvertAll(SceneOrder, s => $"{ScenesFolder}/{s}.unity"),
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"Ecos del Mictlán: compilación correcta en {Path.GetFullPath(path)} ({summary.totalSize / (1024 * 1024)} MB).");
        else
            Debug.LogError($"Ecos del Mictlán: la compilación terminó con resultado {summary.result} ({summary.totalErrors} errores).");

        if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static bool Prepare()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EcosAssetFactory.CreateAll();
        EcosAbandonedHouseArt.Import();
        return true;
    }

    private static void Finish(string sceneToOpen, string what)
    {
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene($"{ScenesFolder}/{sceneToOpen}.unity");
        Debug.Log($"Ecos del Mictlán: construido {what}. Pulsa Play para probarlo.");
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (string s in SceneOrder)
        {
            string path = $"{ScenesFolder}/{s}.unity";
            if (File.Exists(path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ApplyPlayerSettings()
    {
        PlayerSettings.productName = "Ecos del Mictlán";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
    }
}
