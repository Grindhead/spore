#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Keeps the Unity Editor Play button pointed at the real game entry scene.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartupScene
    {
        private const string StartupScenePath = "Assets/Scenes/CellStage.unity";

        static PlayModeStartupScene()
        {
            EditorApplication.delayCall += EnsureStartupScene;
        }

        [MenuItem("RoobsWorld/Use Cell Stage As Play Mode Start Scene")]
        public static void EnsureStartupScene()
        {
            var startupScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartupScenePath);
            if (startupScene == null)
            {
                Debug.LogWarning($"[RoobsWorld] Play Mode startup scene missing: {StartupScenePath}");
                return;
            }

            if (EditorSceneManager.playModeStartScene == startupScene)
                return;

            EditorSceneManager.playModeStartScene = startupScene;
            Debug.Log($"[RoobsWorld] Play Mode start scene set to {StartupScenePath}.");
        }
    }
}
#endif
