#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Convenience menu to run the common project setup steps in order:
    /// tags → build settings scenes → first scene bootstrap wiring.
    /// </summary>
    public static class RoobsWorldQuickSetup
    {
        [MenuItem("RoobsWorld/Quick Setup (Tags + Build Settings + First Scene)")]
        public static void QuickSetup()
        {
            Debug.Log("[RoobsWorld] Quick Setup starting...");

            RoobsWorldProjectSetup.EnsureRequiredTags();
            RoobsWorldBuildSettingsSetup.SetupBuildSettings();
            RoobsWorldFirstSceneSetup.SetupFirstScene();

            Debug.Log("[RoobsWorld] Quick Setup complete.");
        }
    }
}
#endif

