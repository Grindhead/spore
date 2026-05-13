#if UNITY_EDITOR
using RoobsWorld.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RoobsWorld.Editor
{
    public sealed class PlatformReleaseBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & BuildOptions.Development) != 0)
                return;

#if STEAMWORKS_NET
            if (PlatformServicesConfig.UsesDevelopmentSteamAppId)
                throw new BuildFailedException("Release Steam builds cannot use development App ID 480.");
#endif

#if ROOBSWORLD_PLAYSTATION
            if (PlatformServicesConfig.UsesPlaceholderPlayStationServiceId)
                throw new BuildFailedException("Release PlayStation builds must set PlatformServicesConfig.PlayStationTitleServiceId.");
#endif

#if ROOBSWORLD_XBOX
            if (PlatformServicesConfig.UsesPlaceholderXboxTitleId)
                throw new BuildFailedException("Release Xbox builds must set PlatformServicesConfig.XboxTitleId.");
#endif
        }
    }
}
#endif
