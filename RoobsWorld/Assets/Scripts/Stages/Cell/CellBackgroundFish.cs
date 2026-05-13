using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Purely visual giant fish silhouettes that drift behind the cell-stage microbes.
    /// They give the arena a strong scale cue without adding gameplay collision.
    /// </summary>
    public class CellBackgroundFish : MonoBehaviour
    {
        public const int DefaultCount = 3;
        public const float DefaultArenaRadius = 50f;
        public const float MinLength = 34f;
        public const float MaxLength = 62f;
        public const float MinSpeed = 0.12f;
        public const float MaxSpeed = 0.34f;

        private struct Fish
        {
            public GameObject Go;
            public Vector3 Direction;
            public float Speed;
            public float BobPhase;
            public float BobAmplitude;
        }

        private Fish[] _fish;
        private Vector3 _center;
        private float _arenaRadius;
        private static Mesh s_fishMesh;

        public static CellBackgroundFish Spawn(Vector3 center, float arenaRadius, int count = DefaultCount)
        {
            var go = new GameObject("CellBackgroundFish");
            var comp = go.AddComponent<CellBackgroundFish>();
            comp.Initialise(center, arenaRadius, count);
            return comp;
        }

        private void Initialise(Vector3 center, float arenaRadius, int count)
        {
            _center = center;
            _arenaRadius = arenaRadius;
            _fish = new Fish[Mathf.Max(0, count)];

            for (int i = 0; i < _fish.Length; i++)
                _fish[i] = CreateFish(i);
        }

        private Fish CreateFish(int index)
        {
            var fishGo = new GameObject($"DistantFish_{index}");
            fishGo.transform.SetParent(transform, false);

            float length = Mathf.Lerp(MinLength, MaxLength, (index % 3) / 2f);
            float side = index % 2 == 0 ? -1f : 1f;
            float lane = Mathf.Lerp(-0.58f, 0.62f, (index + 1f) / (DefaultCount + 1f));
            var start = _center + new Vector3(
                side * _arenaRadius * Random.Range(0.55f, 0.92f),
                -0.018f - index * 0.006f,
                lane * _arenaRadius);
            var direction = new Vector3(-side, 0f, Random.Range(-0.08f, 0.08f)).normalized;

            fishGo.transform.position = start;
            fishGo.transform.rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
            fishGo.transform.localScale = new Vector3(length, 1f, length * Random.Range(0.32f, 0.43f));

            var meshFilter = fishGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = GetFishMesh();

            var renderer = fishGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateFishMaterial(index);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return new Fish
            {
                Go = fishGo,
                Direction = direction,
                Speed = Random.Range(MinSpeed, MaxSpeed),
                BobPhase = Random.Range(0f, Mathf.PI * 2f),
                BobAmplitude = Random.Range(0.035f, 0.075f),
            };
        }

        private static Mesh GetFishMesh()
        {
            if (s_fishMesh != null)
                return s_fishMesh;

            const int segments = 28;
            var vertices = new Vector3[1 + segments + 9];
            var triangles = new int[segments * 3 + 9 * 3];

            vertices[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float taper = 0.82f + 0.18f * Mathf.Cos(angle);
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.38f, 0f, Mathf.Sin(angle) * 0.5f * taper);
            }

            int t = 0;
            for (int i = 0; i < segments; i++)
            {
                triangles[t++] = 0;
                triangles[t++] = i + 1;
                triangles[t++] = (i + 1) % segments + 1;
            }

            int v = 1 + segments;
            AddTriangle(vertices, triangles, ref v, ref t,
                new Vector3(-0.32f, 0f, 0.24f),
                new Vector3(-0.32f, 0f, -0.24f),
                new Vector3(-0.62f, 0f, 0f));
            AddTriangle(vertices, triangles, ref v, ref t,
                new Vector3(0f, 0f, 0.38f),
                new Vector3(0.14f, 0f, 0.78f),
                new Vector3(0.23f, 0f, 0.28f));
            AddTriangle(vertices, triangles, ref v, ref t,
                new Vector3(0.06f, 0f, -0.3f),
                new Vector3(0.28f, 0f, -0.66f),
                new Vector3(0.34f, 0f, -0.22f));

            s_fishMesh = new Mesh
            {
                name = "DistantFishSilhouette",
                vertices = vertices,
                triangles = triangles
            };
            s_fishMesh.RecalculateNormals();
            s_fishMesh.RecalculateBounds();
            return s_fishMesh;
        }

        private static void AddTriangle(Vector3[] vertices, int[] triangles, ref int v, ref int t, Vector3 a, Vector3 b, Vector3 c)
        {
            vertices[v] = a;
            vertices[v + 1] = b;
            vertices[v + 2] = c;
            triangles[t++] = v;
            triangles[t++] = v + 1;
            triangles[t++] = v + 2;
            v += 3;
        }

        private static Material CreateFishMaterial(int index)
        {
            var tint = Color.Lerp(
                new Color(0.015f, 0.07f, 0.12f, 0.16f),
                new Color(0.05f, 0.12f, 0.22f, 0.1f),
                (index % 3) / 2f);
            var material = ProceduralTextureFactory.CreateTransparentMaterial(Texture2D.whiteTexture, tint);
            material.name = $"DistantFish_Mat_{index}";
            material.renderQueue = 2990;
            return material;
        }

        private void Update()
        {
            if (_fish == null || _fish.Length == 0)
                return;

            float dt = Time.deltaTime;
            float wrapDistance = _arenaRadius * 1.35f;

            for (int i = 0; i < _fish.Length; i++)
            {
                ref var fish = ref _fish[i];
                if (fish.Go == null)
                    continue;

                var pos = fish.Go.transform.position;
                pos += fish.Direction * (fish.Speed * dt);
                pos.y = -0.018f - i * 0.006f + Mathf.Sin(Time.time * 0.12f + fish.BobPhase) * fish.BobAmplitude;
                fish.Go.transform.position = pos;

                Vector3 flat = new Vector3(pos.x - _center.x, 0f, pos.z - _center.z);
                if (flat.magnitude > wrapDistance)
                    RecycleFish(ref fish, i);
            }
        }

        private void RecycleFish(ref Fish fish, int index)
        {
            float side = fish.Direction.x >= 0f ? -1f : 1f;
            float z = Random.Range(-_arenaRadius * 0.72f, _arenaRadius * 0.72f);
            fish.Go.transform.position = _center + new Vector3(side * _arenaRadius * 1.18f, -0.018f - index * 0.006f, z);
        }

        private void OnDisable()
        {
            if (_fish == null)
                return;

            foreach (var fish in _fish)
            {
                if (fish.Go == null)
                    continue;

                if (Application.isPlaying) Destroy(fish.Go);
                else DestroyImmediate(fish.Go);
            }
        }

        public int Count => _fish?.Length ?? 0;
        public float ArenaRadius => _arenaRadius;

        public void SetCenter(Vector3 center)
        {
            _center = center;
        }
    }
}
