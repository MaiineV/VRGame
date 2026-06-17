using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VRGame.EditorTools
{
    /// <summary>
    /// One-click / CLI build for the Meta Quest (Android, Vulkan, ARM64, IL2CPP) target.
    /// Menu:  Build > Build Quest APK
    /// CLI :  -executeMethod VRGame.EditorTools.QuestBuilder.BuildFromCommandLine
    /// </summary>
    public static class QuestBuilder
    {
        private const string OutputDir = "Builds";
        private const string ApkName = "Pour Decisions.apk";

        [MenuItem("Build/Build Quest APK %#b")]
        public static void BuildQuestAPK()
        {
            string path = BuildInternal(developmentBuild: false);
            if (!string.IsNullOrEmpty(path))
                EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Build/Build Quest APK (Development)")]
        public static void BuildQuestAPKDev()
        {
            string path = BuildInternal(developmentBuild: true);
            if (!string.IsNullOrEmpty(path))
                EditorUtility.RevealInFinder(path);
        }

        // Entry point for: Unity -batchmode -quit -executeMethod VRGame.EditorTools.QuestBuilder.BuildFromCommandLine
        public static void BuildFromCommandLine()
        {
            bool dev = Environment.GetCommandLineArgs().Contains("-development");
            string path = BuildInternal(developmentBuild: dev);
            if (string.IsNullOrEmpty(path))
                EditorApplication.Exit(1);
        }

        private static string BuildInternal(bool developmentBuild)
        {
            // Make sure we are on the Android target.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    Debug.LogError("[QuestBuilder] Could not switch active build target to Android.");
                    return null;
                }
            }

            // Quest baseline: Vulkan, ARM64, IL2CPP.
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false; // APK, not AAB.

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[QuestBuilder] No enabled scenes in Build Settings.");
                return null;
            }

            Directory.CreateDirectory(OutputDir);
            string outputPath = Path.Combine(OutputDir, ApkName);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = developmentBuild
                    ? (BuildOptions.Development | BuildOptions.AllowDebugging)
                    : BuildOptions.None
            };

            Debug.Log($"[QuestBuilder] Building {(developmentBuild ? "DEV " : "")}APK -> {outputPath}\nScenes: {string.Join(", ", scenes)}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[QuestBuilder] BUILD SUCCEEDED: {outputPath} ({summary.totalSize / (1024 * 1024)} MB) in {summary.totalTime}.");
                return outputPath;
            }

            Debug.LogError($"[QuestBuilder] BUILD FAILED: {summary.result}, {summary.totalErrors} error(s).");
            return null;
        }
    }
}
