#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Ensures required tags (e.g. Food for creature AI) exist. Run via menu or on load.
    /// </summary>
    public static class RoobsWorldProjectSetup
    {
        private const string FOOD_TAG = "Food";
        private const string AI_GENERATED_TAG = "AI_Generated";
        private const string AI_ENEMY_TAG = "AI_Enemy";
        private const string AI_LOOT_TAG = "AI_Loot";

        [MenuItem("RoobsWorld/Ensure Required Tags")]
        public static void EnsureRequiredTags()
        {
            AddTagIfMissing(FOOD_TAG);
            AddTagIfMissing(AI_GENERATED_TAG);
            AddTagIfMissing(AI_ENEMY_TAG);
            AddTagIfMissing(AI_LOOT_TAG);
        }

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (Application.isBatchMode)
                return;

            EditorApplication.delayCall += () =>
            {
                AddTagIfMissing(FOOD_TAG);
                AddTagIfMissing(AI_GENERATED_TAG);
                AddTagIfMissing(AI_ENEMY_TAG);
                AddTagIfMissing(AI_LOOT_TAG);
            };
        }

        private static void AddTagIfMissing(string tagName)
        {
            var tagManagerAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAssets == null || tagManagerAssets.Length == 0) return;

            var tagManager = new SerializedObject(tagManagerAssets[0]);
            tagManager.Update();
            var tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                    return;
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"[RoobsWorld] Tag '{tagName}' added to Project Settings.");
        }
    }
}
#endif
