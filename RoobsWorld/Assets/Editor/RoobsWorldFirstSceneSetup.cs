#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Adds Bootstrap to the first scene in Build Settings and assigns InputSystem_Actions.
    /// Run once after Setup Build Settings to make the first scene playable.
    /// </summary>
    public static class RoobsWorldFirstSceneSetup
    {
        private const string InputActionsAssetName = "InputSystem_Actions";

        /// <summary>
        /// Call from Unity menu or via -executeMethod RoobsWorld.Editor.RoobsWorldFirstSceneSetup.SetupFirstScene when Unity is not open elsewhere.
        /// </summary>
        [MenuItem("RoobsWorld/Setup First Scene (Add Bootstrap & Assign Input)")]
        public static void SetupFirstScene()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                Debug.LogWarning("[RoobsWorld] No scenes in Build Settings. Run RoobsWorld → Setup Build Settings first.");
                return;
            }

            string firstPath = scenes[0].path;
            var scene = EditorSceneManager.OpenScene(firstPath);
            if (!scene.IsValid())
            {
                Debug.LogError($"[RoobsWorld] Could not open scene: {firstPath}");
                return;
            }

            var bootstrap = Object.FindAnyObjectByType<RoobsWorld.Core.Bootstrap>();
            if (bootstrap == null)
            {
                var go = new GameObject("Bootstrap");
                bootstrap = go.AddComponent<RoobsWorld.Core.Bootstrap>();
                Debug.Log("[RoobsWorld] Added Bootstrap GameObject to first scene.");
            }
            else
            {
                Debug.Log("[RoobsWorld] Bootstrap already present in first scene.");
            }

            Object inputAsset = FindInputActionAsset();
            var so = new SerializedObject(bootstrap);

            // Make the first scene behave like a real entry point.
            var autoStartProp = so.FindProperty("_autoStartCell");
            if (autoStartProp != null) autoStartProp.boolValue = true;

            if (inputAsset != null)
            {
                var prop = so.FindProperty("_inputActions");
                if (prop != null)
                {
                    prop.objectReferenceValue = inputAsset;
                    Debug.Log($"[RoobsWorld] Assigned {InputActionsAssetName} to Bootstrap.");
                }
            }
            else
            {
                Debug.LogWarning($"[RoobsWorld] Could not find {InputActionsAssetName}. Place the asset in Assets/ or Resources/ and run again.");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[RoobsWorld] Configured Bootstrap to auto-start Cell Stage.");

            EnsureEventSystem(inputAsset);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
        }

        private static Object FindInputActionAsset()
        {
            var guids = AssetDatabase.FindAssets("t:InputActionAsset " + InputActionsAssetName);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null && asset.name == InputActionsAssetName)
                    return asset;
            }
            var fromAssets = AssetDatabase.LoadAssetAtPath<Object>($"Assets/{InputActionsAssetName}.inputactions");
            if (fromAssets != null) return fromAssets;
            return Resources.Load<Object>(InputActionsAssetName);
        }

        private static void EnsureEventSystem(Object inputAsset)
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                Debug.Log("[RoobsWorld] EventSystem already present in scene.");
                return;
            }

            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            var uiModule = esObj.AddComponent<InputSystemUIInputModule>();

            if (inputAsset is UnityEngine.InputSystem.InputActionAsset actionAsset)
                uiModule.actionsAsset = actionAsset;

            Debug.Log("[RoobsWorld] Added EventSystem with InputSystemUIInputModule to scene.");
        }

        /// <summary>Test hook for input-asset discovery logic. Do not use outside tests.</summary>
        public static Object FindInputActionAssetForTests()
        {
            return FindInputActionAsset();
        }
    }
}
#endif
