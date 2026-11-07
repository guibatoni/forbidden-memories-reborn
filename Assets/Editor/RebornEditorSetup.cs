#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Reborn.Presentation;

public static class RebornEditorSetup
{
    private const string ScenePath = "Assets/Scenes/Prototype.unity";

    [MenuItem("Reborn/01 - Gerar cena do prototipo")]
    public static void CreateScene()
    {
        if (!EditorUtility.DisplayDialog("Forbidden Memories Reborn",
            "Criar a cena do prototipo? Salve quaisquer mudancas em outras cenas antes de continuar.",
            "Gerar cena", "Cancelar")) return;
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("REBORN_Prototype").AddComponent<RebornGame>();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes = new [] { new EditorBuildSettingsScene(ScenePath,true) };
        AssetDatabase.Refresh();
        Debug.Log("REBORN: Cena criada. Clique Play. Para build Windows use Reborn > 02.");
    }

    [MenuItem("Reborn/02 - Build Windows x64")]
    public static void BuildWindows()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("REBORN","Gere a cena primeiro em Reborn > 01.","Entendi");
            return;
        }
        Directory.CreateDirectory("Builds/Windows");
        var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/ForbiddenMemoriesReborn.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        Debug.Log("REBORN: Build concluido com resultado: " + report.summary.result);
    }
}
#endif
