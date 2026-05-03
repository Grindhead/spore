#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Creates or updates LOD groups for imported models (FBX/prefabs).
    /// Supports: single-mesh models (one LOD level), multiple renderers, or _LOD0/_LOD1 naming for pre-made LOD meshes.
    /// </summary>
    public static class CreateLODGroups
    {
        private const string MenuCreateSelected = "RoobsWorld/Create LOD Groups for Selected";
        private const string MenuCreateInFolder = "RoobsWorld/Create LOD Groups in Folder...";

        /// <summary>Default screen-relative transition heights when multiple LOD levels are used (LOD0..LOD3).</summary>
        private static readonly float[] DefaultLODThresholds = { 0.5f, 0.25f, 0.1f, 0.05f };

        /// <summary>Suffix/pattern to detect LOD level in GameObject name, e.g. "_LOD0", "LOD1".</summary>
        private static readonly Regex LodNameRegex = new Regex(@"_?LOD\s*(\d+)", RegexOptions.IgnoreCase);

        [MenuItem(MenuCreateSelected, true)]
        private static bool ValidateCreateForSelected()
        {
            return Selection.gameObjects.Length > 0 || Selection.assetGUIDs.Length > 0;
        }

        [MenuItem(MenuCreateSelected, false, 1)]
        public static void CreateForSelected()
        {
            int applied = 0;
            int total = 0;

            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null) continue;
                total++;
                GameObject root = GetRootForLOD(go);
                if (ApplyLODGroup(root))
                    applied++;
                if (root.scene.IsValid())
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
            }

            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                total++;
                try
                {
                    using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                    {
                        if (ApplyLODGroup(scope.prefabContentsRoot))
                            applied++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RoobsWorld] Failed to edit prefab {path}: {ex.Message}");
                }
            }

            if (total == 0)
                Debug.LogWarning("[RoobsWorld] No valid model roots selected. Select GameObjects in Hierarchy or Prefab/Model assets in Project.");
            else
                Debug.Log($"[RoobsWorld] LOD groups applied to {applied} of {total} selected.");
        }

        [MenuItem(MenuCreateInFolder, false, 2)]
        public static void CreateInFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select folder containing models", "Assets", "");
            if (string.IsNullOrEmpty(path)) return;

            path = path.Replace('\\', '/');
            if (!path.StartsWith(Application.dataPath))
            {
                Debug.LogWarning("[RoobsWorld] Folder must be under Assets.");
                return;
            }

            string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
            string[] guids = AssetDatabase.FindAssets("t:GameObject t:Model", new[] { relativePath });
            int applied = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) == null) continue;
                try
                {
                    using (var scope = new PrefabUtility.EditPrefabContentsScope(assetPath))
                    {
                        if (ApplyLODGroup(scope.prefabContentsRoot))
                            applied++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[RoobsWorld] Failed to edit {assetPath}: {ex.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[RoobsWorld] LOD groups applied in folder: {applied} of {guids.Length} assets updated.");
        }

        private static GameObject GetRootForLOD(GameObject go)
        {
            if (go.GetComponent<LODGroup>() != null)
                return go;
            if (go.GetComponentInParent<LODGroup>() is LODGroup existing && existing.gameObject != go)
                return existing.gameObject;
            return go.transform.root.gameObject;
        }

        /// <summary>
        /// Applies or updates LODGroup on the given root. Returns true if changed.
        /// </summary>
        public static bool ApplyLODGroup(GameObject root)
        {
            if (root == null) return false;

            var lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup == null)
                lodGroup = root.AddComponent<LODGroup>();

            var renderersByLod = CollectRenderersByLOD(root);
            if (renderersByLod.Count == 0)
            {
                var fallback = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                if (fallback.Length == 0)
                {
                    Debug.LogWarning($"[RoobsWorld] No renderers under {root.name}; skipping LOD group.");
                    return false;
                }
                renderersByLod[0] = fallback;
            }

            int levelCount = renderersByLod.Count;
            var lods = new LOD[levelCount];
            for (int i = 0; i < levelCount; i++)
            {
                float threshold = i < DefaultLODThresholds.Length ? DefaultLODThresholds[i] : DefaultLODThresholds[DefaultLODThresholds.Length - 1] * Mathf.Pow(0.5f, i - DefaultLODThresholds.Length + 1);
                lods[i] = new LOD(threshold, renderersByLod[i]);
            }

            lodGroup.SetLODs(lods);
            lodGroup.animateCrossFading = false;
            lodGroup.fadeMode = LODFadeMode.None;

            if (PrefabUtility.IsPartOfPrefabInstance(root))
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            else if (PrefabUtility.IsPartOfPrefabAsset(root))
                EditorUtility.SetDirty(root);

            return true;
        }

        /// <summary>
        /// Collects renderers grouped by LOD level. Uses _LOD0, _LOD1 naming on GameObjects; otherwise all renderers go to LOD0.
        /// </summary>
        private static Dictionary<int, Renderer[]> CollectRenderersByLOD(GameObject root)
        {
            var byLevel = new Dictionary<int, List<Renderer>>();
            var allRenderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer r in allRenderers)
            {
                if (r == null || !r.enabled) continue;

                int level = GetLODLevelFromName(r.gameObject.name);
                if (level < 0)
                    level = 0;

                if (!byLevel.TryGetValue(level, out var list))
                {
                    list = new List<Renderer>();
                    byLevel[level] = list;
                }
                list.Add(r);
            }

            return byLevel.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());
        }

        private static int GetLODLevelFromName(string gameObjectName)
        {
            var m = LodNameRegex.Match(gameObjectName);
            return m.Success && int.TryParse(m.Groups[1].Value, out int level) ? level : -1;
        }
    }
}
#endif
