using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.World
{
    /// <summary>
    /// Chunk-based procedural terrain: generates and unloads chunks by player position and view distance.
    /// Use for large or "infinite" terrain; ProceduralTerrain remains for fixed arenas.
    /// </summary>
    public class ChunkedWorldGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _player;

        [Header("Chunk settings")]
        [SerializeField] private int _chunkSize = 32;
        [SerializeField] private int _viewDistance = 3;
        [SerializeField] private int _resolutionPerChunk = 16;
        [SerializeField] private float _heightScale = 10f;
        [SerializeField] private float _noiseScale = 0.05f;
        [SerializeField] private int _seed;

        private readonly Dictionary<Vector2Int, GameObject> _chunks = new Dictionary<Vector2Int, GameObject>();

        private void Start()
        {
            if (_player == null)
                _player = Camera.main != null ? Camera.main.transform : transform;
        }

        private void Update()
        {
            if (_player == null) return;

            Vector3 pos = _player.position;
            int cx = Mathf.FloorToInt(pos.x / _chunkSize);
            int cz = Mathf.FloorToInt(pos.z / _chunkSize);
            var playerChunk = new Vector2Int(cx, cz);

            for (int x = -_viewDistance; x <= _viewDistance; x++)
            {
                for (int z = -_viewDistance; z <= _viewDistance; z++)
                {
                    var coord = playerChunk + new Vector2Int(x, z);
                    if (!_chunks.ContainsKey(coord))
                        CreateChunk(coord);
                }
            }

            var toRemove = new List<Vector2Int>();
            foreach (var kv in _chunks)
            {
                int dx = Mathf.Abs(kv.Key.x - playerChunk.x);
                int dz = Mathf.Abs(kv.Key.y - playerChunk.y);
                if (dx > _viewDistance || dz > _viewDistance)
                    toRemove.Add(kv.Key);
            }
            foreach (var coord in toRemove)
            {
                if (_chunks.TryGetValue(coord, out var go))
                {
                    Destroy(go);
                    _chunks.Remove(coord);
                }
            }
        }

        private void CreateChunk(Vector2Int coord)
        {
            var go = new GameObject($"Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(coord.x * _chunkSize, 0, coord.y * _chunkSize);

            var mesh = BuildChunkMesh(coord);
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mr.material = new Material(shader) { name = "ChunkMat" };
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            _chunks[coord] = go;
        }

        private Mesh BuildChunkMesh(Vector2Int coord)
        {
            int res = Mathf.Max(2, _resolutionPerChunk);
            int vertCount = (res + 1) * (res + 1);
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[res * res * 6];

            float step = (float)_chunkSize / res;
            float baseX = coord.x * _chunkSize;
            float baseZ = coord.y * _chunkSize;

            for (int z = 0; z <= res; z++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int i = z * (res + 1) + x;
                    float wx = baseX + x * step;
                    float wz = baseZ + z * step;
                    float height = SampleHeight(wx, wz);
                    vertices[i] = new Vector3(x * step, height, z * step);
                    normals[i] = SampleNormal(wx, wz, step);
                    uvs[i] = new Vector2(wx / _chunkSize, wz / _chunkSize);
                }
            }

            int tri = 0;
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    int i = z * (res + 1) + x;
                    triangles[tri++] = i;
                    triangles[tri++] = i + res + 1;
                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + res + 1;
                    triangles[tri++] = i + res + 2;
                }
            }

            var mesh = new Mesh { name = $"Chunk_{coord.x}_{coord.y}" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.RecalculateBounds();
            return mesh;
        }

        private float SampleHeight(float x, float z)
        {
            float nx = (x + _seed) * _noiseScale;
            float nz = (z + _seed) * _noiseScale;
            float h = Mathf.PerlinNoise(nx, nz);
            h += Mathf.PerlinNoise(nx * 2f, nz * 2f) * 0.5f;
            return h * _heightScale;
        }

        private Vector3 SampleNormal(float x, float z, float step)
        {
            float sampleStep = Mathf.Max(0.01f, step);
            float left = SampleHeight(x - sampleStep, z);
            float right = SampleHeight(x + sampleStep, z);
            float back = SampleHeight(x, z - sampleStep);
            float forward = SampleHeight(x, z + sampleStep);
            return new Vector3(left - right, sampleStep * 2f, back - forward).normalized;
        }
    }
}
