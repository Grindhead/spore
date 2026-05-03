#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoobsWorld.Stages.Creature;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Creates required scenes if missing and adds them to Build Settings in the correct order.
    /// </summary>
    public static class RoobsWorldBuildSettingsSetup
    {
        private static readonly string ScenesFolder = "Assets/Scenes";
        private static readonly string[] RequiredScenes = { "CellStage", "CreatureStage" };

        [MenuItem("RoobsWorld/Setup Build Settings (Create Scenes & Add to Build)")]
        public static void SetupBuildSettings()
        {
            if (!Directory.Exists(ScenesFolder))
            {
                Directory.CreateDirectory(ScenesFolder);
                AssetDatabase.Refresh();
            }

            var scenePaths = new List<string>();

            foreach (string sceneName in RequiredScenes)
            {
                string path = $"{ScenesFolder}/{sceneName}.unity";
                if (!File.Exists(path))
                {
                    var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    InitializeSceneDefaults(sceneName);
                    if (!EditorSceneManager.SaveScene(scene, path))
                    {
                        Debug.LogWarning($"[RoobsWorld] Failed to save scene to {path}");
                        continue;
                    }
                    Debug.Log($"[RoobsWorld] Created scene {path}");
                }
                scenePaths.Add(path);
            }

            var existing = EditorBuildSettings.scenes ?? new EditorBuildSettingsScene[0];
            EditorBuildSettings.scenes = BuildOrderedScenes(existing, scenePaths);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RoobsWorld] Build Settings updated. Scenes in build: {EditorBuildSettings.scenes.Length}");
        }

        /// <summary>Test hook for BuildOrderedScenes logic. Do not use outside tests.</summary>
        public static EditorBuildSettingsScene[] BuildOrderedScenesForTests(EditorBuildSettingsScene[] current, string[] requiredScenePaths)
        {
            return BuildOrderedScenes(current ?? new EditorBuildSettingsScene[0], requiredScenePaths?.ToList() ?? new List<string>());
        }

        private static EditorBuildSettingsScene[] BuildOrderedScenes(EditorBuildSettingsScene[] current, List<string> requiredScenePaths)
        {
            // Required scenes: enforce canonical order and ensure they're enabled.
            var requiredSet = new HashSet<string>(requiredScenePaths);
            var ordered = new List<EditorBuildSettingsScene>(requiredScenePaths.Count + current.Length);

            foreach (string requiredPath in requiredScenePaths)
            {
                ordered.Add(new EditorBuildSettingsScene(requiredPath, true));
            }

            // Append non-required scenes, preserving their current order and enabled state.
            foreach (var scene in current)
            {
                if (scene == null) continue;
                if (string.IsNullOrWhiteSpace(scene.path)) continue;
                if (requiredSet.Contains(scene.path)) continue;
                if (ordered.Any(s => s.path == scene.path)) continue;

                ordered.Add(new EditorBuildSettingsScene(scene.path, scene.enabled));
            }

            return ordered.ToArray();
        }

        private static void InitializeSceneDefaults(string sceneName)
        {
            if (sceneName != "CreatureStage")
                return;

            var stageObject = Object.FindAnyObjectByType<CreatureStage>();
            if (stageObject != null)
                return;

            var stageGo = new GameObject("CreatureStage");
            stageGo.AddComponent<CreatureStage>();
            stageGo.AddComponent<CreatureStageArena>();

            Debug.Log("[RoobsWorld] Added CreatureStage root object with runtime arena bootstrap.");
        }
    }
}
#endif
