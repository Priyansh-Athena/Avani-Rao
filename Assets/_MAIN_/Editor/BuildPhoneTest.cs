using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildPhoneTest
{
    [MenuItem("Tools/VR Museum/Build Phone Test APK (Touch Triggers)")]
    private static void BuildTouchTest() => Build(true);

    [MenuItem("Tools/VR Museum/Build Controller APK (No Touch Triggers)")]
    private static void BuildController() => Build(false);

    private static void Build(bool touchTriggers)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUtility.DisplayDialog("Phone Test", "Switch the active build target to Android first.", "OK");
            return;
        }

        string defaultName = touchTriggers ? "VRMuseumTouchTest" : "VRMuseumController";
        string path = EditorUtility.SaveFilePanel("Build Android APK", "", defaultName, "apk");
        if (string.IsNullOrEmpty(path)) return;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && scene.path != "Assets/_MAIN_/Scenes/Controller Test.unity")
            .Select(scene => scene.path)
            .ToArray();

        bool previousBundleSetting = EditorUserBuildSettings.buildAppBundle;
        try
        {
            EditorUserBuildSettings.buildAppBundle = false;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = touchTriggers ? BuildOptions.Development : BuildOptions.None,
                extraScriptingDefines = touchTriggers ? new[] { "ENABLE_PHONE_TRIGGER_SIMULATOR" } : new string[0]
            });

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log((touchTriggers ? "Touch-test" : "Controller-only") + " APK built: " + path);
            else
                Debug.LogError("Phone test build result: " + report.summary.result);
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundleSetting;
        }
    }
}