#if UNITY_EDITOR
using System.IO;
using RoobsWorld.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RoobsWorld.Editor
{
    public sealed class SteamBuildPostprocessor : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows &&
                report.summary.platform != BuildTarget.StandaloneWindows64 &&
                report.summary.platform != BuildTarget.StandaloneLinux64 &&
                report.summary.platform != BuildTarget.StandaloneOSX)
            {
                return;
            }

            string outputPath = report.summary.outputPath;
            string directory = Directory.Exists(outputPath)
                ? outputPath
                : Path.GetDirectoryName(outputPath);

            if (string.IsNullOrEmpty(directory))
                return;

            File.WriteAllText(Path.Combine(directory, "steam_appid.txt"), SteamClientIntegration.AppId.ToString());
        }
    }
}
#endif
