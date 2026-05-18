#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Syncs Blender-generated model assets into runtime-loadable Resources prefabs and validates the asset wiring.
    /// Menu: RoobsWorld > Tools > Blender Assets > ...
    /// </summary>
    public static class BlenderAssetSyncAndValidation
    {
        private const string CellModelDir = "Assets/Art/Models/CellParts";
        private const string CreatureModelDir = "Assets/Art/Models/CreatureParts";
        private const string CellTextureDir = "Assets/Art/Textures/CellParts";
        private const string CreatureTextureDir = "Assets/Art/Textures/CreatureParts";

        private const string ResourceModelRoot = "Assets/Resources/Models";
        private const string ResourceCellDir = "Assets/Resources/Models/CellParts";
        private const string ResourceCreatureDir = "Assets/Resources/Models/CreatureParts";

        private static readonly string[] CellModelNames =
        {
            "cell_body",
            "cell_flagella",
            "cell_cilia",
            "cell_spike",
            "cell_jet",
            "cell_pickup",
            "cell_food_algae",
            "cell_food_bacteria",
            "cell_food_meat",
            "cell_toxin_cloud",
            "cell_current_glyph"
        };

        private static readonly string[] CreatureModelNames =
        {
            "mouth_basic",
            "eye_basic",
            "leg_basic",
            "arm_basic",
            "wing_basic",
            "tail_basic",
            "horn_basic",
            "fin_basic",
            "shell_basic",
            "spike_basic"
        };

        private static readonly string[] CellTextureNames =
        {
            "cell_membrane",
            "cell_appendage",
            "cell_jet",
            "cell_food_algae",
            "cell_food_bacteria",
            "cell_food_meat",
            "cell_toxin_cloud",
            "cell_current_glyph"
        };

        private static readonly string[] CreatureTextureNames =
        {
            "creature_skin",
            "creature_shell",
            "creature_bone",
            "creature_eye",
            "creature_fin"
        };

        [MenuItem("RoobsWorld/Tools/Blender Assets/Sync Models To Resources")]
        public static void SyncModelsToResources()
        {
            EnsureFolder(ResourceModelRoot);
            EnsureFolder(ResourceCellDir);
            EnsureFolder(ResourceCreatureDir);

            int synced = 0;
            synced += SyncCategory(CellModelDir, ResourceCellDir, CellModelNames);
            synced += SyncGeneratedCreatureParts(CreatureModelDir, ResourceCreatureDir);
            RemoveLegacyCreaturePrefabs(ResourceCreatureDir);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RoobsWorld] Blender asset sync complete. Synced {synced} resource prefabs.");
        }

        [MenuItem("RoobsWorld/Tools/Blender Assets/Validate Wiring")]
        public static void ValidateWiring()
        {
            int errors = 0;
            errors += ValidateModelCategory(CellModelDir, ResourceCellDir, CellModelNames);
            errors += ValidateModelCategory(CreatureModelDir, ResourceCreatureDir, CreatureModelNames);
            errors += ValidateTextureCategory(CellTextureDir, CellTextureNames);
            errors += ValidateTextureCategory(CreatureTextureDir, CreatureTextureNames);

            if (errors == 0)
            {
                Debug.Log("[RoobsWorld] Blender asset wiring validation passed.");
            }
            else
            {
                Debug.LogError($"[RoobsWorld] Blender asset wiring validation found {errors} issue(s).");
            }
        }

        [MenuItem("RoobsWorld/Tools/Blender Assets/Sync And Validate")]
        public static void SyncAndValidate()
        {
            SyncModelsToResources();
            ValidateWiring();
        }

        private static int SyncCategory(string modelDir, string resourceDir, IReadOnlyList<string> modelNames)
        {
            int synced = 0;
            for (int i = 0; i < modelNames.Count; i++)
            {
                string name = modelNames[i];
                string modelPath = $"{modelDir}/{name}.fbx";
                string prefabPath = $"{resourceDir}/{name}.prefab";

                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null)
                {
                    Debug.LogWarning($"[RoobsWorld] Missing model asset for sync: {modelPath}");
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                if (instance == null)
                {
                    Debug.LogWarning($"[RoobsWorld] Failed to instantiate model asset: {modelPath}");
                    continue;
                }

                try
                {
                    instance.name = name;
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    synced++;
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            return synced;
        }

        private static int ValidateModelCategory(string modelDir, string resourceDir, IReadOnlyList<string> modelNames)
        {
            int errors = 0;
            for (int i = 0; i < modelNames.Count; i++)
            {
                string name = modelNames[i];
                string modelPath = $"{modelDir}/{name}.fbx";
                string prefabPath = $"{resourceDir}/{name}.prefab";

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    Debug.LogError($"[RoobsWorld] Missing Blender model: {modelPath}");
                    errors++;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[RoobsWorld] Missing runtime resource prefab: {prefabPath}");
                    errors++;
                    continue;
                }

                if (!ContainsNamedChild(prefab.transform, $"{name}_VIS"))
                {
                    Debug.LogError($"[RoobsWorld] Prefab missing visible mesh child '{name}_VIS': {prefabPath}");
                    errors++;
                }

                if (!ContainsNamedChild(prefab.transform, $"{name}_COL"))
                {
                    Debug.LogError($"[RoobsWorld] Prefab missing collider mesh child '{name}_COL': {prefabPath}");
                    errors++;
                }
            }

            return errors;
        }

        private static int SyncGeneratedCreatureParts(string modelDir, string resourceDir)
        {
            int synced = 0;
            string[] guids = AssetDatabase.FindAssets("t:Model animal_", new[] { modelDir });
            for (int i = 0; i < guids.Length; i++)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!modelPath.EndsWith(".fbx"))
                    continue;

                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelAsset == null)
                    continue;

                string modelName = Path.GetFileNameWithoutExtension(modelPath);
                string prefabPath = $"{resourceDir}/{modelName}.prefab";

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                if (instance == null)
                    continue;

                try
                {
                    instance.name = modelName;
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    synced++;
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            return synced;
        }

        private static void RemoveLegacyCreaturePrefabs(string resourceDir)
        {
            for (int i = 0; i < CreatureModelNames.Length; i++)
            {
                string legacyPrefabPath = $"{resourceDir}/{CreatureModelNames[i]}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(legacyPrefabPath) != null)
                {
                    AssetDatabase.DeleteAsset(legacyPrefabPath);
                }
            }
        }

        private static int ValidateTextureCategory(string textureDir, IReadOnlyList<string> textureNames)
        {
            int errors = 0;
            for (int i = 0; i < textureNames.Count; i++)
            {
                string texturePath = $"{textureDir}/{textureNames[i]}.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture == null)
                {
                    Debug.LogError($"[RoobsWorld] Missing Blender texture: {texturePath}");
                    errors++;
                }
            }

            return errors;
        }

        private static bool ContainsNamedChild(Transform root, string expectedName)
        {
            if (root == null) return false;
            if (root.name == expectedName) return true;
            for (int i = 0; i < root.childCount; i++)
            {
                if (ContainsNamedChild(root.GetChild(i), expectedName))
                    return true;
            }

            return false;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
