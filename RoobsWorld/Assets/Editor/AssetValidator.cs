#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Validates project assets: prefabs with missing references, missing colliders, or missing materials.
    /// Menu: RoobsWorld > Tools > Validate Project.
    /// </summary>
    public static class AssetValidator
    {
        private const string PrefabFilter = "t:Prefab";
        private const string DefaultSearchPath = "Assets";

        [MenuItem("RoobsWorld/Tools/Validate Project")]
        public static void ValidateProject()
        {
            var results = RunValidation(DefaultSearchPath);
            AssetValidatorWindow.Show(results);
        }

        public static List<ValidationEntry> RunValidation(string searchPath)
        {
            var results = new List<ValidationEntry>();
            var guids = AssetDatabase.FindAssets(PrefabFilter, new[] { searchPath });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path)) continue;

                EditorUtility.DisplayProgressBar("Validating", path, (float)i / guids.Length);
                ValidatePrefab(path, results);
            }

            EditorUtility.ClearProgressBar();
            return results;
        }

        private static void ValidatePrefab(string assetPath, List<ValidationEntry> results)
        {
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefabRoot == null) return;

            var components = prefabRoot.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                {
                    results.Add(new ValidationEntry
                    {
                        AssetPath = assetPath,
                        Kind = ValidationKind.MissingScript,
                        Message = "Missing script component",
                        GameObjectPath = GetPath(prefabRoot, null)
                    });
                }
                else
                {
                    var so = new SerializedObject(component);
                    var it = so.GetIterator();
                    if (!it.Next(true)) continue;
                    do
                    {
                        if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == null && !string.IsNullOrEmpty(it.propertyPath))
                        {
                            results.Add(new ValidationEntry
                            {
                                AssetPath = assetPath,
                                Kind = ValidationKind.MissingReference,
                                Message = $"{component.GetType().Name}.{it.propertyPath}",
                                GameObjectPath = GetPath(prefabRoot, component.gameObject)
                            });
                        }
                    }
                    while (it.Next(it.hasChildren));
                }
            }

            var renderers = prefabRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial == null || r.sharedMaterials.Any(m => m == null))
                {
                    results.Add(new ValidationEntry
                    {
                        AssetPath = assetPath,
                        Kind = ValidationKind.MissingMaterial,
                        Message = "Missing material on renderer",
                        GameObjectPath = GetPath(prefabRoot, r.gameObject)
                    });
                }
            }

            var rootCollider = prefabRoot.GetComponentInChildren<Collider>(true);
            if (rootCollider == null && renderers.Length > 0)
            {
                results.Add(new ValidationEntry
                {
                    AssetPath = assetPath,
                    Kind = ValidationKind.MissingCollider,
                    Message = "Prefab has renderer(s) but no collider",
                    GameObjectPath = prefabRoot.name
                });
            }
        }

        private static string GetPath(GameObject root, GameObject target)
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

    public enum ValidationKind
    {
        MissingReference,
        MissingScript,
        MissingMaterial,
        MissingCollider
    }

    [System.Serializable]
    public class ValidationEntry
    {
        public string AssetPath;
        public ValidationKind Kind;
        public string Message;
        public string GameObjectPath;

        public override string ToString() => $"[{Kind}] {AssetPath} | {GameObjectPath} | {Message}";
    }

    public class AssetValidatorWindow : EditorWindow
    {
        private List<ValidationEntry> _entries = new List<ValidationEntry>();
        private Vector2 _scroll;
        private string _filter = "";

        public static void Show(List<ValidationEntry> entries)
        {
            var window = GetWindow<AssetValidatorWindow>("Asset Validator");
            window._entries = entries ?? new List<ValidationEntry>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Project validation results", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total issues: {_entries.Count}");

            _filter = EditorGUILayout.TextField("Filter", _filter);
            var filtered = string.IsNullOrWhiteSpace(_filter)
                ? _entries
                : _entries.Where(e =>
                    (e.AssetPath?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.Message?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (e.GameObjectPath?.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0).ToList();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var e in filtered)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping", GUILayout.Width(40)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(e.AssetPath);
                    if (obj != null) { EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
                }
                EditorGUILayout.LabelField(e.Kind.ToString(), GUILayout.Width(120));
                EditorGUILayout.LabelField(e.AssetPath, GUILayout.MinWidth(150));
                EditorGUILayout.LabelField(e.GameObjectPath, GUILayout.MinWidth(80));
                EditorGUILayout.LabelField(e.Message, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
