using UnityEngine;

namespace RoobsWorld.World
{
    /// <summary>
    /// Generates a simple Perlin-noise-based heightfield mesh at runtime, including collider and vertex colors.
    /// </summary>
    public class ProceduralTerrain : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int _resolution = 64;
        [SerializeField] private float _size = 100f;
        [SerializeField] private float _heightScale = 10f;
        [SerializeField] private float _noiseScale = 0.02f;
        [SerializeField] private int _seed;

        [Header("Colors")]
        [SerializeField] private Color _lowColor = new Color(0.2f, 0.4f, 0.1f);
        [SerializeField] private Color _highColor = new Color(0.5f, 0.4f, 0.3f);
        [SerializeField] private Color _waterColor = new Color(0.1f, 0.3f, 0.6f);

        private Mesh _mesh;

        public void Generate(int seed = 0)
        {
            _seed = seed != 0 ? seed : _seed;

            _mesh = new Mesh { name = "ProceduralTerrain" };

            int vertCount = (_resolution + 1) * (_resolution + 1);
            var vertices = new Vector3[vertCount];
            var colors = new Color[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[_resolution * _resolution * 6];

            float halfSize = _size * 0.5f;
            float step = _size / _resolution;

            for (int z = 0; z <= _resolution; z++)
            {
                for (int x = 0; x <= _resolution; x++)
                {
                    int i = z * (_resolution + 1) + x;

                    float worldX = x * step - halfSize;
                    float worldZ = z * step - halfSize;

                    float height = SampleHeight(worldX, worldZ);

                    vertices[i] = new Vector3(worldX, height, worldZ);
                    uvs[i] = new Vector2((float)x / _resolution, (float)z / _resolution);

                    float t = Mathf.InverseLerp(0, _heightScale, height);
                    colors[i] = height < 0.5f
                        ? _waterColor
                        : Color.Lerp(_lowColor, _highColor, t);
                }
            }

            int tri = 0;
            for (int z = 0; z < _resolution; z++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    int i = z * (_resolution + 1) + x;

                    triangles[tri++] = i;
                    triangles[tri++] = i + _resolution + 1;
                    triangles[tri++] = i + 1;

                    triangles[tri++] = i + 1;
                    triangles[tri++] = i + _resolution + 1;
                    triangles[tri++] = i + _resolution + 2;
                }
            }

            _mesh.vertices = vertices;
            _mesh.triangles = triangles;
            _mesh.colors = colors;
            _mesh.uv = uvs;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            var mf = gameObject.GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mf.mesh = _mesh;

            var mr = gameObject.GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mr.material = new Material(shader) { name = "TerrainMat" };

            var mc = gameObject.GetComponent<MeshCollider>();
            if (mc == null) mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = _mesh;
        }

        private float SampleHeight(float x, float z)
        {
            float nx = (x + _seed) * _noiseScale;
            float nz = (z + _seed) * _noiseScale;

            float height = Mathf.PerlinNoise(nx, nz);
            height += Mathf.PerlinNoise(nx * 2f, nz * 2f) * 0.5f;
            height += Mathf.PerlinNoise(nx * 4f, nz * 4f) * 0.25f;

            return height * _heightScale;
        }
    }
}
