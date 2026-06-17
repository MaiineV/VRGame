#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>
    /// One-click Android (Quest) APK build using the scenes enabled in Build Settings.
    /// Outputs to Builds/PourDecisions.apk. Requires the active build target to already be
    /// Android (switching platform is a heavy reimport — do it manually first if needed).
    /// </summary>
    public static class BuildAPK
    {
        [MenuItem("Pour Decisions/Build/Android APK")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.LogError($"[BuildAPK] Target activo = {EditorUserBuildSettings.activeBuildTarget}, no Android. " +
                               "Cambiá a Android en File > Build Settings > Switch Platform y reintentá.");
                return;
            }

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Debug.LogError("[BuildAPK] No hay escenas habilitadas en Build Settings."); return; }

            if (!Directory.Exists("Builds")) Directory.CreateDirectory("Builds");
            var outPath = "Builds/PourDecisions.apk";

            EditorUserBuildSettings.buildAppBundle = false; // .apk, no .aab

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildAPK] Build de {scenes.Length} escenas -> {outPath} ...");
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            if (s.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[BuildAPK] OK -> {outPath} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime}).");
            else
                Debug.LogError($"[BuildAPK] FALLÓ: {s.result} (errores: {s.totalErrors}). Mirá la consola para el detalle.");
        }
    }
}
#endif
