using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Builds a simple, reusable arena so Creature stage is playable even when loaded in an empty scene.
    /// </summary>
    public class CreatureStageArena : MonoBehaviour
    {
        [SerializeField] private float _arenaRadius = 48f;
        [SerializeField] private float _floorThickness = 2f;
        [SerializeField] private float _wallHeight = 4f;
        [SerializeField] private float _wallThickness = 2f;

        private const string FloorName = "Floor";
        private const string WallsName = "ArenaWalls";
        private bool _built;

        public void EnsureBuilt()
        {
            if (_built)
                return;

            EnsureFloor();
            EnsureWalls();
            EnsureDirectionalLight();
            _built = true;
        }

        private void EnsureFloor()
        {
            Transform existing = transform.Find(FloorName);
            if (existing != null)
                return;

            float radius = Mathf.Max(10f, _arenaRadius);
            float thickness = Mathf.Max(0.2f, _floorThickness);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = FloorName;
            floor.transform.SetParent(transform, false);
            floor.transform.localScale = new Vector3(radius * 2f, thickness, radius * 2f);
            floor.transform.localPosition = new Vector3(0f, -thickness * 0.5f, 0f);

            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial.color = new Color(0.17f, 0.24f, 0.18f, 1f);
            }
        }

        private void EnsureWalls()
        {
            Transform wallsRoot = transform.Find(WallsName);
            if (wallsRoot != null)
                return;

            float radius = Mathf.Max(10f, _arenaRadius);
            float wallHeight = Mathf.Max(1f, _wallHeight);
            float wallThickness = Mathf.Max(0.2f, _wallThickness);

            var walls = new GameObject(WallsName);
            walls.transform.SetParent(transform, false);

            CreateWall(walls.transform, "NorthWall", new Vector3(0f, wallHeight * 0.5f, radius), new Vector3(radius * 2f, wallHeight, wallThickness));
            CreateWall(walls.transform, "SouthWall", new Vector3(0f, wallHeight * 0.5f, -radius), new Vector3(radius * 2f, wallHeight, wallThickness));
            CreateWall(walls.transform, "EastWall", new Vector3(radius, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, radius * 2f));
            CreateWall(walls.transform, "WestWall", new Vector3(-radius, wallHeight * 0.5f, 0f), new Vector3(wallThickness, wallHeight, radius * 2f));
        }

        private static void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = localPos;
            wall.transform.localScale = localScale;

            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial.color = new Color(0.12f, 0.19f, 0.14f, 1f);
            }
        }

        private static void EnsureDirectionalLight()
        {
            if (Object.FindAnyObjectByType<Light>() != null)
                return;

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
