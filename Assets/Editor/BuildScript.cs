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
        try
        {
            string buildPath = "Builds/iOS";

            Debug.Log("🚀 Starting iOS build (Build Profile)");
            Debug.Log($"📁 Build path: {Path.GetFullPath(buildPath)}");

            // ✅ IMPORTANT : clean l'export précédent sinon Unity peut croire qu'on veut "append"
            if (Directory.Exists(buildPath))
            {
                Debug.Log("🧹 Cleaning previous iOS build folder...");
                Directory.Delete(buildPath, true);
            }

#if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            var profile = UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile();

            if (profile == null)
            {
                Debug.LogError("❌ No active BuildProfile found (did you pass -activeBuildProfile ?)");
                EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerWithProfileOptions()  //BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = buildPath,

                // ✅ PAS d'append en CI → export Xcode fresh, stable
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError("❌ iOS build failed: " + report.summary.result);
                EditorApplication.Exit(1);
                return;
            }
#else
        Debug.LogError("❌ Build Profiles API not available in this Unity version.");
        EditorApplication.Exit(1);
        return;
#endif

            Debug.Log("✅ iOS build completed successfully");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("💥 EXCEPTION in BuildScript.BuildiOS()");
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

}
