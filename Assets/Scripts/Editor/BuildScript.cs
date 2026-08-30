using UnityEditor;
using System.IO;
using UnityEngine;

public static class BuildScript
{
    public static void BuildAndroid()
    {
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        string buildPath = "build/hect-run-game.apk";
        
        string dir = Path.GetDirectoryName(buildPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log("Starting Android build to: " + buildPath);
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Build failed with " + summary.totalErrors + " errors.");
            EditorApplication.Exit(1);
        }
    }
}
