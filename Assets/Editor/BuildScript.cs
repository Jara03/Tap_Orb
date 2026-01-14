// Assets/Editor/BuildScript.cs

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build Profiles (Unity 6 / 2023+)
#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
using UnityEditor.Build.Profile;
#endif

public static class BuildScript
{
    // ✅ Ton Build Profile iOS
    private const string IOS_PROFILE_PATH = "Assets/Settings/BuildProfiles/iOS_Profile.asset";

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

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("❌ Android build failed");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("✅ Android build completed successfully");
        EditorApplication.Exit(0);
    }

    public static void BuildiOS()
    {
        string buildPath = "Builds/iOS";

        Debug.Log("🚀 Starting iOS build (Build Profile)");
        Debug.Log($"📁 Build path: {Path.GetFullPath(buildPath)}");

        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
            Debug.Log("📂 Created Builds/iOS directory");
        }

#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
        // ✅ 1) On essaye d'utiliser le Build Profile ACTIF
        // (idéal si tu passes -activeBuildProfile en CI)
        BuildProfile profile = BuildProfile.GetActiveBuildProfile();

        // ✅ 2) Sinon on charge ton asset explicitement
        if (profile == null)
        {
            Debug.LogWarning("⚠️ No active BuildProfile found. Loading iOS profile from asset path...");
            profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(IOS_PROFILE_PATH);
        }

        if (profile == null)
        {
            Debug.LogError($"❌ BuildProfile not found at path: {IOS_PROFILE_PATH}");
            EditorApplication.Exit(1);
            return;
        }

        // ✅ Build via Build Profile (reproduit la config editor)
        var options = new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,
            locationPathName = buildPath,
            options = BuildOptions.AcceptExternalModificationsToPlayer // iOS -> export Xcode
        };

        var report = BuildPipeline.BuildPlayer(options);
#else
        Debug.LogError("❌ Build Profiles API not available in this Unity version.");
        EditorApplication.Exit(1);
        return;
#endif

        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("❌ iOS build failed");
            Debug.LogError($"Result: {report.summary.result}");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("✅ iOS build completed successfully");
        EditorApplication.Exit(0);
    }
}
