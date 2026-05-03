#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Batch-mode build entry point. Invoke with:
    ///   -executeMethod RoobsWorld.Editor.BuildScript.BuildWindows64
    /// Output path defaults to ../../Build/RoobsWorld.exe or can be overridden
    /// via the -buildOutput command-line argument.
    /// </summary>
    public static class BuildScript
    {
        private const string DefaultOutput = "../../Build/RoobsWorld.exe";

        [MenuItem("RoobsWorld/Build Windows 64")]
        public static void BuildWindows64()
        {
            string output = GetArg("-buildOutput") ?? DefaultOutput;

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Debug.LogError("[Build] No enabled scenes in Build Settings.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            Debug.Log($"[Build] Building {scenes.Length} scenes → {output}");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] Success — {summary.totalSize / 1024 / 1024} MB in {summary.totalTime.TotalSeconds:F1}s");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[Build] Failed: {summary.totalErrors} error(s)");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
#endif
