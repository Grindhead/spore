#if UNITY_EDITOR
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
                }
            }

            Debug.Log($"Generated dungeon: {_width}x{_height} tiles.");
        }
    }
}
#endif
