using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildGame
{
    [MenuItem("Breakout/Create Scene and Build Windows")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources");
        if (!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/GameMaterial.mat"))
        {
            var material = new Material(Shader.Find("Standard"));
            material.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(material, "Assets/Resources/GameMaterial.mat");
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Breakout Game", typeof(BreakoutGame));
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Breakout.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Breakout.unity", true) };
        PlayerSettings.productName = "NEON BREAK / 3D";
        PlayerSettings.companyName = "Independent";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, "Builds/Windows/NeonBreak.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed: " + result.summary.result);
    }
}
