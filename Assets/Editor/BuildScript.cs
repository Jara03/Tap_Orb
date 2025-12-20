// Assets/Editor/BuildScript.cs

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildAndroid()
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray(),
            locationPathName = "Builds/Android/TapOrb.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
    }

    public static void BuildiOS()
    {
        string buildPath = "Build/iOS";

        Debug.Log("🚀 Starting iOS build");
        Debug.Log($"📁 Build path: {Path.GetFullPath(buildPath)}");

        // S'assurer que le dossier existe
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
            Debug.Log("📂 Created Build/iOS directory");
        }

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("❌ No scenes found in Build Settings");
            EditorApplication.Exit(1);
            return;
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.LogError("❌ iOS build failed");
            EditorApplication.Exit(1);
        }

        Debug.Log("✅ iOS build completed successfully");
        EditorApplication.Exit(0);
    }
}