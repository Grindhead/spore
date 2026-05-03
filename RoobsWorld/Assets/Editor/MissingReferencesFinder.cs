#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Editor tool that finds all missing (null) object references in prefabs and optionally in scenes.
    /// Access via RoobsWorld > Tools > Find Missing References in Prefabs.
    /// </summary>
    public static class MissingReferencesFinder
    {
        private const string PrefabSearchFilter = "t:Prefab";
        private const string DefaultSearchPath = "Assets";

        [MenuItem("RoobsWorld/Tools/Find Missing References in Prefabs")]
        public static void FindMissingReferencesInPrefabs()
        {
            var results = ScanPrefabs(DefaultSearchPath);
            MissingReferencesWindow.Show(results);
        }

        /// <summary>
        /// Scans all prefabs under the given path and returns entries for every null object reference.
        /// </summary>
        public static List<MissingRefEntry> ScanPrefabs(string searchPath = DefaultSearchPath)
        {
            var guids = AssetDatabase.FindAssets(PrefabSearchFilter, new[] { searchPath });
            var results = new List<MissingRefEntry>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;

                EditorUtility.DisplayProgressBar("Scanning prefabs", path, (float)i / guids.Length);

                CollectMissingRefsInPrefab(path, results);
            }

            EditorUtility.ClearProgressBar();
            return results;
        }

        private static void CollectMissingRefsInPrefab(string assetPath, List<MissingRefEntry> results)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabRoot == null) return;

            var components = prefabRoot.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                {
                    results.Add(new MissingRefEntry
                    {
                        AssetPath = assetPath,
                        PropertyPath = "(Missing Script)",
                        ComponentTypeName = "(Missing Script)",
                        GameObjectPath = GetGameObjectPath(prefabRoot, null)
                    });
                    continue;
                }

                var so = new SerializedObject(component);
                var it = so.GetIterator();
                if (!it.Next(true)) continue;

                do
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null)
                    {
                        results.Add(new MissingRefEntry
                        {
                            AssetPath = assetPath,
                            PropertyPath = it.propertyPath,
                            ComponentTypeName = component.GetType().Name,
                            GameObjectPath = GetGameObjectPath(prefabRoot, component.gameObject)
                        });
                    }
                }
                while (it.Next(it.hasChildren));
            }
        }

        private static string GetGameObjectPath(GameObject root, GameObject target)
        {
            if (target == null) return root.name;
            var parts = new List<string>();
            var current = target;
            while (current != null)
            {
                parts.Add(current.name);
                if (current == root) break;
                current = current.transform.parent != null ? current.transform.parent.gameObject : null;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }
    }

    [System.Serializable]
    public class MissingRefEntry
    {
        public string AssetPath;
        public string PropertyPath;
        public string ComponentTypeName;
        public string GameObjectPath;

        public override string ToString() => $"[{AssetPath}] {GameObjectPath} > {ComponentTypeName}.{PropertyPath}";
    }

    /// <summary>
    /// Editor window that displays missing reference scan results and allows jumping to assets.
    /// </summary>
    public class MissingReferencesWindow : EditorWindow
    {
        private List<MissingRefEntry> _entries = new List<MissingRefEntry>();
        private Vector2 _scroll;
        private string _searchPath = "Assets";
        private string _filter = "";
        private bool _scanning;

        public static void Show(List<MissingRefEntry> entries)
        {
            var window = GetWindow<MissingReferencesWindow>("Missing References");
            window._entries = entries ?? new List<MissingRefEntry>();
            window._scanning = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Missing References in Prefabs", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _searchPath = EditorGUILayout.TextField("Search path", _searchPath);
            GUI.enabled = !_scanning;
            if (GUILayout.Button("Scan", GUILayout.Width(60)))
            {
                _scanning = true;
                EditorApplication.delayCall += () =>
                {
                    _entries = MissingReferencesFinder.ScanPrefabs(_searchPath);
                    _scanning = false;
                    Repaint();
                };
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _filter = EditorGUILayout.TextField("Filter", _filter);

            int total = _entries.Count;
            var filtered = string.IsNullOrWhiteSpace(_filter)
                ? _entries
                : _entries.Where(e =>
                    (e.AssetPath?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.ComponentTypeName?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.PropertyPath?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.GameObjectPath?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0).ToList();

            EditorGUILayout.LabelField($"Results: {filtered.Count} missing refs" + (filtered.Count != total ? $" (of {total})" : ""));

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (var entry in filtered)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping", GUILayout.Width(40)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.AssetPath);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                }
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                    if (obj != null)
                        Selection.activeObject = obj;
                }
                EditorGUILayout.LabelField(entry.AssetPath, GUILayout.MinWidth(120));
                EditorGUILayout.LabelField(entry.GameObjectPath, GUILayout.MinWidth(100));
                EditorGUILayout.LabelField(entry.ComponentTypeName, GUILayout.Width(120));
                EditorGUILayout.LabelField(entry.PropertyPath, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
