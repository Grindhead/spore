#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// After importing a model from Assets/Art/Models: adds MeshCollider to children named *_COL
    /// (and disables their MeshRenderer), then creates a prefab at Assets/Prefabs/{name}.prefab.
    /// </summary>
    public class PrefabFromModelPostprocessor : AssetPostprocessor
    {
        private const string ModelsFolder = "Assets/Art/Models";
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string ColSuffix = "_COL";

        private void OnPostprocessModel(GameObject root)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(ModelsFolder))
                return;

            AddCollidersToColChildren(root);
            var path = assetPath;
            CreatePrefabFromModel(path, root);
        }

        private static void AddCollidersToColChildren(GameObject root)
        {
            var toProcess = new List<Transform>();
            CollectTransforms(root.transform, toProcess);
            foreach (var t in toProcess)
            {
                if (!t.name.Contains(ColSuffix)) continue;
                var go = t.gameObject;
                if (go.GetComponent<MeshCollider>() == null)
                    go.AddComponent<MeshCollider>();
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        private static void CollectTransforms(Transform parent, List<Transform> outList)
        {
            outList.Add(parent);
            for (int i = 0; i < parent.childCount; i++)
                CollectTransforms(parent.GetChild(i), outList);
        }

        private static void CreatePrefabFromModel(string modelAssetPath, GameObject modelRoot)
        {
            var modelName = System.IO.Path.GetFileNameWithoutExtension(modelAssetPath);
            var prefabPath = $"{PrefabsFolder}/{modelName}.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                if (!AssetDatabase.IsValidFolder("Assets"))
                    return;
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            EditorApplication.delayCall += () =>
            {
                var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
                if (modelAsset == null) return;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
                if (instance == null) return;
                try
                {
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    AssetDatabase.Refresh();
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            };
        }
    }
}
#endif
