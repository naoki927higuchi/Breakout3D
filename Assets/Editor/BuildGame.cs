using UnityEditor;
using UnityEngine;

public static class BuildGame
{
    [MenuItem("Breakout/Build Windows Player")]
    public static void Build()
    {
        const string scene = "Assets/Scenes/Breakout.unity";
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) ||
            !AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/GameMaterial.mat"))
            throw new System.Exception("Required scene or material is missing. Restore the project assets before building.");
        string output = Argument("-breakoutOutput", "Builds/Windows/NeonBreak.exe");
        string previousVersion = PlayerSettings.bundleVersion;
        try
        {
            PlayerSettings.bundleVersion = Argument("-breakoutVersion", previousVersion);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output));
            var result = BuildPipeline.BuildPlayer(new[] { scene }, output,
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception("Build failed: " + result.summary.result);
        }
        finally { PlayerSettings.bundleVersion = previousVersion; }
    }

    static string Argument(string name, string fallback)
    {
        var args = System.Environment.GetCommandLineArgs();
        int index = System.Array.IndexOf(args, name);
        if (index < 0) return fallback;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("-"))
            throw new System.ArgumentException("Missing value for " + name);
        return args[index + 1];
    }
}
