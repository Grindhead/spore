using RoobsWorld.Multiplayer;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public enum CreatureArenaLandmarkType
    {
        Cover,
        HighGround,
        NarrowPassage,
        HealingZone
    }

    public readonly struct CreatureArenaLandmark
    {
        public readonly CreatureArenaLandmarkType Type;
        public readonly Vector3 Position;
        public readonly Vector3 Scale;

        public CreatureArenaLandmark(CreatureArenaLandmarkType type, Vector3 position, Vector3 scale)
        {
            Type = type;
            Position = position;
            Scale = scale;
        }
    }

    /// <summary>
    /// Builds a simple, reusable arena so Creature stage is playable even when loaded in an empty scene.
    /// </summary>
    public class CreatureStageArena : MonoBehaviour
    {
        [SerializeField] private float _arenaRadius = 48f;
        [SerializeField] private int _islandResolution = 72;
        [SerializeField] private float _islandHeight = 8f;
        [SerializeField] private float _islandNoiseScale = 0.055f;
        [SerializeField] private int _islandSeed = 1337;
        [SerializeField] private float _wallHeight = 5f;
        [SerializeField] private float _wallThickness = 2f;

        private const string FloorName = "Floor";
        private const string WallsName = "ArenaWalls";
        private const string LandmarksName = "ArenaLandmarks";
        private const string WorldMarkersName = "CreatureWorldMarkers";
        private CreatureArenaLandmark[] _configuredLandmarks;
        private CreatureWorldPlan _worldPlan;
        private bool _built;

        public int IslandSeed => _islandSeed;
        public float ArenaRadius => _arenaRadius;
        public int ConfiguredLandmarkCount => _configuredLandmarks?.Length ?? 0;
        public CreatureWorldPlan WorldPlan => _worldPlan;

        public void ApplyMapDefinition(SessionMapDefinition map)
        {
            if (!map.IsValid || !map.IsCreatureMap)
                return;

            _arenaRadius = Mathf.Max(10f, map.ArenaRadius);
            _islandResolution = Mathf.Max(16, map.IslandResolution);
            _islandHeight = Mathf.Max(0.1f, map.IslandHeight);
            _islandNoiseScale = Mathf.Max(0.001f, map.IslandNoiseScale);
            _islandSeed = Mathf.Max(1, map.Seed);
            _configuredLandmarks = ConvertLandmarks(map.Landmarks);

            if (_built)
            {
                ClearGeneratedArena();
                _built = false;
            }
        }

        public void ApplyWorldPlan(CreatureWorldPlan plan)
        {
            _worldPlan = plan;
            if (_built)
            {
                DestroyChild(WorldMarkersName);
                EnsureWorldMarkers();
            }
        }

        public void EnsureBuilt()
        {
            if (_built)
                return;

            EnsureFloor();
            EnsureWalls();
            EnsureLandmarks();
            EnsureWorldMarkers();
            EnsureDirectionalLight();
            _built = true;
        }

        private void EnsureFloor()
        {
            Transform existing = transform.Find(FloorName);
            if (existing != null)
                return;

            float radius = Mathf.Max(10f, _arenaRadius);
            int resolution = Mathf.Max(16, _islandResolution);

            var floor = new GameObject(FloorName);
            floor.name = FloorName;
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = Vector3.zero;

            var meshFilter = floor.AddComponent<MeshFilter>();
            var meshRenderer = floor.AddComponent<MeshRenderer>();
            var meshCollider = floor.AddComponent<MeshCollider>();
            Mesh islandMesh = BuildIslandMesh(radius, resolution);
            meshFilter.sharedMesh = islandMesh;
            meshCollider.sharedMesh = islandMesh;

            var renderer = meshRenderer;
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                var material = new Material(shader);
                material.color = new Color(0.2f, 0.3f, 0.19f, 1f);
                renderer.sharedMaterial = material;
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

        private void EnsureLandmarks()
        {
            Transform existing = transform.Find(LandmarksName);
            if (existing != null)
                return;

            var root = new GameObject(LandmarksName);
            root.transform.SetParent(transform, false);
            var landmarks = GetConfiguredOrDefaultLandmarks(Mathf.Max(10f, _arenaRadius));
            for (int i = 0; i < landmarks.Length; i++)
            {
                var landmark = landmarks[i];
                landmark = new CreatureArenaLandmark(
                    landmark.Type,
                    SnapToIslandSurface(landmark.Position),
                    landmark.Scale);
                CreateLandmark(root.transform, i, landmark);
            }
        }

        private void EnsureWorldMarkers()
        {
            Transform existing = transform.Find(WorldMarkersName);
            if (existing != null)
                return;

            var plan = _worldPlan.Points != null && _worldPlan.Points.Length > 0
                ? _worldPlan
                : CreatureWorldPlanner.CreatePlan(null, Mathf.Max(10f, _arenaRadius));

            var root = new GameObject(WorldMarkersName);
            root.transform.SetParent(transform, false);
            for (int i = 0; i < plan.Points.Length; i++)
            {
                var point = plan.Points[i];
                CreateWorldMarker(root.transform, point, i);
            }
        }

        private void CreateWorldMarker(Transform parent, CreatureWorldPoint point, int index)
        {
            PrimitiveType primitive = point.Type == CreatureWorldPointType.MigrationGate
                ? PrimitiveType.Cube
                : PrimitiveType.Cylinder;
            var go = GameObject.CreatePrimitive(primitive);
            go.name = $"{point.Type}_{index}_{point.Id}";
            go.transform.SetParent(parent, false);
            go.transform.position = SnapToIslandSurface(point.Position) + Vector3.up * 0.04f;
            float radius = Mathf.Max(0.2f, point.Radius);
            go.transform.localScale = point.Type == CreatureWorldPointType.MigrationGate
                ? new Vector3(radius * 0.55f, 1.8f, radius * 1.2f)
                : new Vector3(radius, 0.08f, radius);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial.color = GetWorldPointColor(point.Type);
            }

            var collider = go.GetComponent<Collider>();
            if (collider != null)
                collider.isTrigger = true;
        }

        private static Color GetWorldPointColor(CreatureWorldPointType type)
        {
            switch (type)
            {
                case CreatureWorldPointType.HomeNest: return new Color(0.54f, 0.36f, 0.22f, 0.88f);
                case CreatureWorldPointType.SpeciesNest: return new Color(0.42f, 0.34f, 0.20f, 0.82f);
                case CreatureWorldPointType.FoodSource: return new Color(0.22f, 0.58f, 0.24f, 0.72f);
                case CreatureWorldPointType.WaterSource: return new Color(0.18f, 0.42f, 0.68f, 0.68f);
                case CreatureWorldPointType.TerritoryCenter: return new Color(0.62f, 0.46f, 0.18f, 0.42f);
                case CreatureWorldPointType.MigrationGate: return new Color(0.56f, 0.52f, 0.84f, 0.82f);
                default: return Color.white;
            }
        }

        private CreatureArenaLandmark[] GetConfiguredOrDefaultLandmarks(float arenaRadius)
        {
            return _configuredLandmarks != null && _configuredLandmarks.Length > 0
                ? _configuredLandmarks
                : GetDefaultLandmarks(arenaRadius);
        }

        public static CreatureArenaLandmark[] GetDefaultLandmarks(float arenaRadius)
        {
            float r = Mathf.Max(10f, arenaRadius);
            return new[]
            {
                new CreatureArenaLandmark(CreatureArenaLandmarkType.Cover, new Vector3(r * 0.28f, 0.75f, r * 0.12f), new Vector3(4.5f, 1.5f, 3.2f)),
                new CreatureArenaLandmark(CreatureArenaLandmarkType.Cover, new Vector3(-r * 0.34f, 0.75f, -r * 0.18f), new Vector3(3.8f, 1.5f, 4.2f)),
                new CreatureArenaLandmark(CreatureArenaLandmarkType.HighGround, new Vector3(-r * 0.05f, 0.45f, r * 0.36f), new Vector3(8f, 0.9f, 6f)),
                new CreatureArenaLandmark(CreatureArenaLandmarkType.NarrowPassage, new Vector3(r * 0.08f, 1.0f, -r * 0.34f), new Vector3(2.2f, 2f, 9f)),
                new CreatureArenaLandmark(CreatureArenaLandmarkType.HealingZone, new Vector3(-r * 0.18f, 0.08f, r * 0.08f), new Vector3(4.8f, 0.12f, 4.8f))
            };
        }

        private static CreatureArenaLandmark[] ConvertLandmarks(SessionMapLandmark[] mapLandmarks)
        {
            if (mapLandmarks == null || mapLandmarks.Length == 0)
                return null;

            var landmarks = new CreatureArenaLandmark[mapLandmarks.Length];
            for (int i = 0; i < mapLandmarks.Length; i++)
            {
                var source = mapLandmarks[i];
                var type = TryParseLandmarkType(source.Type, out var parsed)
                    ? parsed
                    : CreatureArenaLandmarkType.Cover;
                landmarks[i] = new CreatureArenaLandmark(
                    type,
                    new Vector3(source.X, 0f, source.Z),
                    new Vector3(
                        Mathf.Max(0.05f, source.ScaleX),
                        Mathf.Max(0.05f, source.ScaleY),
                        Mathf.Max(0.05f, source.ScaleZ)));
            }

            return landmarks;
        }

        private static bool TryParseLandmarkType(string value, out CreatureArenaLandmarkType type)
        {
            if (!string.IsNullOrWhiteSpace(value)
                && System.Enum.TryParse(value, ignoreCase: true, out type)
                && System.Enum.IsDefined(typeof(CreatureArenaLandmarkType), type))
            {
                return true;
            }

            type = CreatureArenaLandmarkType.Cover;
            return false;
        }

        private static void CreateLandmark(Transform parent, int index, CreatureArenaLandmark landmark)
        {
            PrimitiveType primitive = landmark.Type == CreatureArenaLandmarkType.HealingZone
                ? PrimitiveType.Cylinder
                : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(primitive);
            go.name = $"{landmark.Type}_{index}";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = landmark.Position;
            go.transform.localScale = landmark.Scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial.color = GetLandmarkColor(landmark.Type);
            }

            if (landmark.Type == CreatureArenaLandmarkType.HealingZone)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                    collider.isTrigger = true;
                go.AddComponent<CreatureHealingZone>();
            }
        }

        private static Color GetLandmarkColor(CreatureArenaLandmarkType type)
        {
            switch (type)
            {
                case CreatureArenaLandmarkType.HighGround: return new Color(0.24f, 0.32f, 0.2f, 1f);
                case CreatureArenaLandmarkType.NarrowPassage: return new Color(0.12f, 0.16f, 0.12f, 1f);
                case CreatureArenaLandmarkType.HealingZone: return new Color(0.18f, 0.55f, 0.42f, 0.86f);
                default: return new Color(0.18f, 0.22f, 0.15f, 1f);
            }
        }

        private void ClearGeneratedArena()
        {
            DestroyChild(FloorName);
            DestroyChild(WallsName);
            DestroyChild(LandmarksName);
            DestroyChild(WorldMarkersName);
        }

        private void DestroyChild(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
                return;

            var childObject = child.gameObject;
            child.SetParent(null, false);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(childObject);
            else
                Destroy(childObject);
#else
            Destroy(childObject);
#endif
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

        public Vector3 SnapToIslandSurface(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            Vector2 flat = new Vector2(local.x, local.z);
            float radius = Mathf.Max(1f, _arenaRadius);
            if (flat.magnitude > radius)
                flat = flat.normalized * radius;

            float y = SampleIslandHeight(flat.x, flat.y);
            Vector3 snappedLocal = new Vector3(flat.x, y, flat.y);
            return transform.TransformPoint(snappedLocal);
        }

        private Mesh BuildIslandMesh(float radius, int resolution)
        {
            int vertCount = (resolution + 1) * (resolution + 1);
            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[resolution * resolution * 6];

            float diameter = radius * 2f;
            float step = diameter / resolution;
            int v = 0;
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float localX = -radius + x * step;
                    float localZ = -radius + z * step;
                    float y = SampleIslandHeight(localX, localZ);
                    vertices[v] = new Vector3(localX, y, localZ);
                    uvs[v] = new Vector2((float)x / resolution, (float)z / resolution);
                    v++;
                }
            }

            int t = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * (resolution + 1) + x;
                    triangles[t++] = i;
                    triangles[t++] = i + resolution + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + resolution + 1;
                    triangles[t++] = i + resolution + 2;
                }
            }

            var mesh = new Mesh { name = "CreatureIsland" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private float SampleIslandHeight(float localX, float localZ)
        {
            float radius = Mathf.Max(1f, _arenaRadius);
            float normalizedDistance = Mathf.Clamp01(new Vector2(localX, localZ).magnitude / radius);
            float edgeFalloff = Mathf.Pow(1f - normalizedDistance, 2.15f);

            float noise = FractalNoise(localX, localZ);
            float shorelineDepth = Mathf.Lerp(-1.6f, 0.8f, edgeFalloff);
            return shorelineDepth + edgeFalloff * _islandHeight + noise;
        }

        private float FractalNoise(float localX, float localZ)
        {
            float scale = Mathf.Max(0.001f, _islandNoiseScale);
            float nx = (localX + _islandSeed) * scale;
            float nz = (localZ + _islandSeed * 0.71f) * scale;
            float n0 = Mathf.PerlinNoise(nx, nz) * 2f - 1f;
            float n1 = Mathf.PerlinNoise(nx * 2f + 17.3f, nz * 2f + 31.1f) * 2f - 1f;
            float n2 = Mathf.PerlinNoise(nx * 4f + 47.7f, nz * 4f + 13.9f) * 2f - 1f;
            return n0 * 0.95f + n1 * 0.45f + n2 * 0.2f;
        }
    }
}
