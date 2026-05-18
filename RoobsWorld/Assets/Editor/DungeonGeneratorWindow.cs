#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Editor
{
    /// <summary>
    /// Editor window to generate a simple grid of floor prefabs (dungeon/level layout).
    /// Menu: RoobsWorld > Tools > Dungeon Generator.
    /// </summary>
    public class DungeonGeneratorWindow : EditorWindow
    {
        private int _width = 10;
        private int _height = 10;
        private GameObject _floorPrefab;
        private GameObject _parent;
        private string _environmentPackFolder = "Assets/3D Game Kit Environment Pack";
        private float _propSpawnChancePerTile = 0.25f;
        private float _propYOffset = 0f;
        private bool _randomYawRotation = true;
        private readonly List<GameObject> _environmentPrefabs = new List<GameObject>();

        [MenuItem("RoobsWorld/Tools/Dungeon Generator")]
        public static void ShowWindow()
        {
            GetWindow<DungeonGeneratorWindow>("Dungeon Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Dungeon Generator", EditorStyles.boldLabel);
            _width = EditorGUILayout.IntField("Width", Mathf.Max(1, _width));
            _height = EditorGUILayout.IntField("Height", Mathf.Max(1, _height));
            _floorPrefab = (GameObject)EditorGUILayout.ObjectField("Floor Prefab", _floorPrefab, typeof(GameObject), false);
            _parent = (GameObject)EditorGUILayout.ObjectField("Parent (optional)", _parent, typeof(GameObject), true);
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Environment Pack Placement", EditorStyles.boldLabel);
            _environmentPackFolder = EditorGUILayout.TextField("Pack Folder", _environmentPackFolder);
            _propSpawnChancePerTile = EditorGUILayout.Slider("Prop Chance / Tile", _propSpawnChancePerTile, 0f, 1f);
            _propYOffset = EditorGUILayout.FloatField("Prop Y Offset", _propYOffset);
            _randomYawRotation = EditorGUILayout.Toggle("Random Y Rotation", _randomYawRotation);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto-Find Pack Prefabs"))
                AutoFindPackPrefabs();
            if (GUILayout.Button("Clear Found Prefabs"))
                _environmentPrefabs.Clear();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Found Prefabs: {_environmentPrefabs.Count}");

            EditorGUILayout.Space(4);
            GUI.enabled = _floorPrefab != null;
            if (GUILayout.Button("Generate Dungeon"))
                Generate();
            GUI.enabled = true;
        }

        private void Generate()
        {
            if (_floorPrefab == null)
            {
                Debug.LogWarning("Assign a Floor Prefab first.");
                return;
            }

            Transform root = _parent != null ? _parent.transform : null;
            if (root == null)
            {
                var go = new GameObject("DungeonRoot");
                root = go.transform;
                Undo.RegisterCreatedObjectUndo(go, "Dungeon Generator");
            }

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _height; z++)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(_floorPrefab, root);
                    instance.transform.position = new Vector3(x, 0, z);
                    Undo.RegisterCreatedObjectUndo(instance, "Dungeon Generator");

                    TryPlaceEnvironmentProp(root, x, z);
                }
            }

            Debug.Log($"Generated dungeon: {_width}x{_height} tiles with {_environmentPrefabs.Count} available environment prefabs.");
        }

        private void TryPlaceEnvironmentProp(Transform root, int x, int z)
        {
            if (_environmentPrefabs.Count == 0 || Random.value > _propSpawnChancePerTile)
                return;

            GameObject prefab = _environmentPrefabs[Random.Range(0, _environmentPrefabs.Count)];
            if (prefab == null)
                return;

            var prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            prop.transform.position = new Vector3(x, _propYOffset, z);
            prop.transform.rotation = _randomYawRotation
                ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
                : Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(prop, "Dungeon Generator");
        }

        private void AutoFindPackPrefabs()
        {
            _environmentPrefabs.Clear();

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { _environmentPackFolder });
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                // Filter out very complex scene prefabs to keep generation lightweight.
                if (prefab.GetComponentInChildren<Terrain>() != null)
                    continue;

                _environmentPrefabs.Add(prefab);
            }

            Debug.Log(_environmentPrefabs.Count == 0
                ? $"No prefabs found under '{_environmentPackFolder}'. Check imported folder path."
                : $"Found {_environmentPrefabs.Count} environment prefabs under '{_environmentPackFolder}'.");
        }
    }
}
#endif
