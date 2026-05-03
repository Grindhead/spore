using UnityEngine;

namespace RoobsWorld.Utils
{
    public static class ProceduralMeshFactory
    {
        public static Mesh CreateCellMesh(int segments = 24)
        {
            var mesh = new Mesh { name = "CellMesh" };
            int vertCount = (segments + 1) * (segments / 2 + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int idx = 0;
            int halfSegs = segments / 2;
            for (int lat = 0; lat <= halfSegs; lat++)
            {
                float theta = Mathf.PI * lat / halfSegs;
                for (int lon = 0; lon <= segments; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / segments;
                    float wobble = 1f + 0.08f * Mathf.Sin(3f * phi) * Mathf.Sin(2f * theta);
                    float x = Mathf.Sin(theta) * Mathf.Cos(phi) * wobble;
                    float y = Mathf.Cos(theta) * wobble;
                    float z = Mathf.Sin(theta) * Mathf.Sin(phi) * wobble;

                    verts[idx] = new Vector3(x * 0.5f, y * 0.5f, z * 0.5f);
                    normals[idx] = new Vector3(x, y, z).normalized;
                    uvs[idx] = new Vector2((float)lon / segments, (float)lat / halfSegs);
                    idx++;
                }
            }

            int triCount = segments * halfSegs * 6;
            var tris = new int[triCount];
            int t = 0;
            int cols = segments + 1;
            for (int lat = 0; lat < halfSegs; lat++)
            {
                for (int lon = 0; lon < segments; lon++)
                {
                    int a = lat * cols + lon;
                    int b = a + cols;
                    tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            return mesh;
        }

        public static Mesh CreateOrganicSphere(float radius = 0.5f, int subdivisions = 16)
        {
            var mesh = new Mesh { name = "OrganicSphere" };
            int latSegs = subdivisions;
            int lonSegs = subdivisions * 2;
            int vertCount = (latSegs + 1) * (lonSegs + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int lat = 0; lat <= latSegs; lat++)
            {
                float theta = Mathf.PI * lat / latSegs;
                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    float x = Mathf.Sin(theta) * Mathf.Cos(phi);
                    float y = Mathf.Cos(theta);
                    float z = Mathf.Sin(theta) * Mathf.Sin(phi);

                    verts[v] = new Vector3(x, y, z) * radius;
                    normals[v] = new Vector3(x, y, z);
                    uvs[v] = new Vector2((float)lon / lonSegs, (float)lat / latSegs);
                    v++;
                }
            }

            int triCount = latSegs * lonSegs * 6;
            var tris = new int[triCount];
            int t = 0;
            int cols = lonSegs + 1;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int a = lat * cols + lon;
                    int b = a + cols;
                    tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            return mesh;
        }

        public static Mesh CreateTreeTrunk(float baseRadius = 0.15f, float topRadius = 0.08f, float height = 3f, int segments = 8)
        {
            var mesh = new Mesh { name = "TreeTrunk" };
            int rings = 6;
            int vertCount = (rings + 1) * (segments + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int ring = 0; ring <= rings; ring++)
            {
                float t = (float)ring / rings;
                float y = t * height;
                float r = Mathf.Lerp(baseRadius, topRadius, t);
                float wobble = 1f + 0.05f * Mathf.Sin(ring * 2.3f);
                r *= wobble;

                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    float x = Mathf.Cos(angle) * r;
                    float z = Mathf.Sin(angle) * r;

                    verts[v] = new Vector3(x, y, z);
                    normals[v] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    uvs[v] = new Vector2((float)seg / segments, t);
                    v++;
                }
            }

            int triCount = rings * segments * 6;
            var tris = new int[triCount];
            int ti = 0;
            int cols = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int a = ring * cols + seg;
                    int b = a + cols;
                    tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                    tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            return mesh;
        }

        public static Mesh CreateTreeCanopy(float radius = 1.5f, int subdivisions = 12)
        {
            var mesh = new Mesh { name = "TreeCanopy" };
            int latSegs = subdivisions;
            int lonSegs = subdivisions * 2;
            int vertCount = (latSegs + 1) * (lonSegs + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int lat = 0; lat <= latSegs; lat++)
            {
                float theta = Mathf.PI * lat / latSegs;
                for (int lon = 0; lon <= lonSegs; lon++)
                {
                    float phi = 2f * Mathf.PI * lon / lonSegs;
                    float bumps = 1f + 0.12f * Mathf.Sin(5f * phi) * Mathf.Sin(3f * theta);
                    float squash = (lat < latSegs / 2) ? 1f : 0.7f;
                    float x = Mathf.Sin(theta) * Mathf.Cos(phi) * bumps;
                    float y = Mathf.Cos(theta) * squash * bumps;
                    float z = Mathf.Sin(theta) * Mathf.Sin(phi) * bumps;

                    verts[v] = new Vector3(x, y, z) * radius;
                    normals[v] = new Vector3(x, y, z).normalized;
                    uvs[v] = new Vector2((float)lon / lonSegs, (float)lat / latSegs);
                    v++;
                }
            }

            int triCount = latSegs * lonSegs * 6;
            var tris = new int[triCount];
            int t = 0;
            int cols = lonSegs + 1;
            for (int lat = 0; lat < latSegs; lat++)
            {
                for (int lon = 0; lon < lonSegs; lon++)
                {
                    int a = lat * cols + lon;
                    int b = a + cols;
                    tris[t++] = a; tris[t++] = b; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = b; tris[t++] = b + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            return mesh;
        }

        public static Mesh CreateRock(float scale = 1f, int seed = 0)
        {
            var baseMesh = CreateOrganicSphere(scale * 0.5f, 8);
            var verts = baseMesh.vertices;
            var state = new System.Random(seed);

            for (int i = 0; i < verts.Length; i++)
            {
                float noise = 0.7f + (float)state.NextDouble() * 0.6f;
                verts[i].x *= noise * 1.2f;
                verts[i].y *= 0.5f + (float)state.NextDouble() * 0.4f;
                verts[i].z *= noise;
            }

            baseMesh.vertices = verts;
            baseMesh.RecalculateNormals();
            baseMesh.RecalculateBounds();
            baseMesh.name = "Rock";
            return baseMesh;
        }

        public static Mesh CreateHut(float width = 2f, float height = 2f)
        {
            var mesh = new Mesh { name = "Hut" };
            float hw = width * 0.5f;
            float wallH = height * 0.6f;
            float roofH = height;

            var verts = new Vector3[]
            {
                // Floor
                new(-hw, 0, -hw), new(hw, 0, -hw), new(hw, 0, hw), new(-hw, 0, hw),
                // Walls
                new(-hw, 0, -hw), new(hw, 0, -hw), new(hw, wallH, -hw), new(-hw, wallH, -hw),
                new(hw, 0, -hw), new(hw, 0, hw), new(hw, wallH, hw), new(hw, wallH, -hw),
                new(hw, 0, hw), new(-hw, 0, hw), new(-hw, wallH, hw), new(hw, wallH, hw),
                new(-hw, 0, hw), new(-hw, 0, -hw), new(-hw, wallH, -hw), new(-hw, wallH, hw),
                // Roof
                new(-hw * 1.15f, wallH, -hw * 1.15f), new(hw * 1.15f, wallH, -hw * 1.15f),
                new(0, roofH, 0),
                new(hw * 1.15f, wallH, -hw * 1.15f), new(hw * 1.15f, wallH, hw * 1.15f),
                new(0, roofH, 0),
                new(hw * 1.15f, wallH, hw * 1.15f), new(-hw * 1.15f, wallH, hw * 1.15f),
                new(0, roofH, 0),
                new(-hw * 1.15f, wallH, hw * 1.15f), new(-hw * 1.15f, wallH, -hw * 1.15f),
                new(0, roofH, 0),
            };

            var tris = new int[]
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21,
                23, 25, 24,
                26, 28, 27,
                29, 31, 30,
            };

            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < uvs.Length; i++)
                uvs[i] = new Vector2(verts[i].x / width + 0.5f, verts[i].z / width + 0.5f);

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateCityBuilding(float width = 3f, float height = 5f, int seed = 0)
        {
            var mesh = new Mesh { name = "CityBuilding" };
            float hw = width * 0.5f;
            var state = new System.Random(seed);
            float topTaper = 0.85f + (float)state.NextDouble() * 0.15f;

            var verts = new Vector3[]
            {
                // Bottom
                new(-hw, 0, -hw), new(hw, 0, -hw), new(hw, 0, hw), new(-hw, 0, hw),
                // Top
                new(-hw * topTaper, height, -hw * topTaper),
                new(hw * topTaper, height, -hw * topTaper),
                new(hw * topTaper, height, hw * topTaper),
                new(-hw * topTaper, height, hw * topTaper),
                // Front
                new(-hw, 0, -hw), new(hw, 0, -hw),
                new(hw * topTaper, height, -hw * topTaper), new(-hw * topTaper, height, -hw * topTaper),
                // Right
                new(hw, 0, -hw), new(hw, 0, hw),
                new(hw * topTaper, height, hw * topTaper), new(hw * topTaper, height, -hw * topTaper),
                // Back
                new(hw, 0, hw), new(-hw, 0, hw),
                new(-hw * topTaper, height, hw * topTaper), new(hw * topTaper, height, hw * topTaper),
                // Left
                new(-hw, 0, hw), new(-hw, 0, -hw),
                new(-hw * topTaper, height, -hw * topTaper), new(-hw * topTaper, height, hw * topTaper),
            };

            var tris = new int[]
            {
                0, 1, 2, 0, 2, 3,
                4, 7, 6, 4, 6, 5,
                8, 10, 9, 8, 11, 10,
                12, 14, 13, 12, 15, 14,
                16, 18, 17, 16, 19, 18,
                20, 22, 21, 20, 23, 22,
            };

            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < uvs.Length; i++)
                uvs[i] = new Vector2(verts[i].x / width + 0.5f, verts[i].y / height);

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateSpaceship()
        {
            var mesh = new Mesh { name = "Spaceship" };
            var verts = new Vector3[]
            {
                // Fuselage: tapered cylinder approximation
                new(0, 0, 2f),       // nose
                new(-0.4f, 0.2f, 1f), new(0.4f, 0.2f, 1f),
                new(-0.5f, 0.3f, 0f), new(0.5f, 0.3f, 0f),
                new(-0.5f, -0.1f, 0f), new(0.5f, -0.1f, 0f),
                new(-0.4f, 0.2f, -1f), new(0.4f, 0.2f, -1f),
                new(-0.3f, -0.1f, -1f), new(0.3f, -0.1f, -1f),
                new(0, 0.15f, -1.5f), // tail
                // Wings
                new(-0.5f, 0.1f, 0.2f), new(-2f, 0f, -0.3f), new(-0.5f, 0.1f, -0.5f),
                new(0.5f, 0.1f, 0.2f), new(2f, 0f, -0.3f), new(0.5f, 0.1f, -0.5f),
                // Engine pods
                new(-0.3f, -0.1f, -1.2f), new(-0.15f, 0.1f, -1.2f), new(-0.3f, -0.1f, -0.6f),
                new(0.3f, -0.1f, -1.2f), new(0.15f, 0.1f, -1.2f), new(0.3f, -0.1f, -0.6f),
            };

            var tris = new int[]
            {
                // Top fuselage
                0, 2, 1, 1, 2, 4, 1, 4, 3, 3, 4, 8, 3, 8, 7, 7, 8, 11,
                // Bottom fuselage
                0, 1, 5, 0, 5, 6, 0, 6, 2, 5, 9, 10, 5, 10, 6, 6, 10, 11,
                // Side panels
                1, 3, 5, 3, 9, 5, 2, 6, 4, 4, 6, 10, 3, 7, 9, 4, 10, 8,
                // Wings
                12, 13, 14,
                15, 17, 16,
                // Engines
                18, 19, 20,
                21, 23, 22,
            };

            var uvs = new Vector2[verts.Length];
            for (int i = 0; i < uvs.Length; i++)
                uvs[i] = new Vector2(verts[i].x * 0.5f + 0.5f, verts[i].z * 0.25f + 0.5f);

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateStarMesh(float radius = 1f, int points = 16)
        {
            return CreateOrganicSphere(radius, points);
        }

        public static Mesh CreateNestMesh(float radius = 2f)
        {
            var mesh = new Mesh { name = "Nest" };
            int segments = 16;
            int rings = 3;
            int vertCount = (rings + 1) * (segments + 1) + 1;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int ring = 0; ring <= rings; ring++)
            {
                float t = (float)ring / rings;
                float y = t * 0.4f;
                float r = radius * (0.8f + 0.2f * Mathf.Sin(t * Mathf.PI));

                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    float wobble = 1f + 0.05f * Mathf.Sin(seg * 3.7f);
                    verts[v] = new Vector3(Mathf.Cos(angle) * r * wobble, y, Mathf.Sin(angle) * r * wobble);
                    normals[v] = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle)).normalized;
                    uvs[v] = new Vector2((float)seg / segments, t);
                    v++;
                }
            }

            verts[v] = Vector3.zero;
            normals[v] = Vector3.up;
            uvs[v] = new Vector2(0.5f, 0.5f);
            int centerIdx = v;

            int triCount = rings * segments * 6 + segments * 3;
            var tris = new int[triCount];
            int ti = 0;
            int cols = segments + 1;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int a = ring * cols + seg;
                    int b = a + cols;
                    tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                    tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
                }
            }

            for (int seg = 0; seg < segments; seg++)
            {
                tris[ti++] = centerIdx;
                tris[ti++] = seg;
                tris[ti++] = seg + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateFoodBerry(float radius = 0.2f)
        {
            return CreateOrganicSphere(radius, 8);
        }

        public static Mesh CreateFirePit(float radius = 0.75f)
        {
            var mesh = new Mesh { name = "FirePit" };
            int segments = 16;
            int vertCount = (segments + 1) * 3 + 1;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            // Outer ring bottom
            for (int seg = 0; seg <= segments; seg++)
            {
                float angle = 2f * Mathf.PI * seg / segments;
                verts[v] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                normals[v] = Vector3.up;
                uvs[v] = new Vector2((float)seg / segments, 0);
                v++;
            }
            // Outer ring top
            for (int seg = 0; seg <= segments; seg++)
            {
                float angle = 2f * Mathf.PI * seg / segments;
                float r = radius * 0.9f;
                verts[v] = new Vector3(Mathf.Cos(angle) * r, 0.3f, Mathf.Sin(angle) * r);
                normals[v] = new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)).normalized;
                uvs[v] = new Vector2((float)seg / segments, 0.5f);
                v++;
            }
            // Inner ring
            for (int seg = 0; seg <= segments; seg++)
            {
                float angle = 2f * Mathf.PI * seg / segments;
                float r = radius * 0.6f;
                verts[v] = new Vector3(Mathf.Cos(angle) * r, 0.15f, Mathf.Sin(angle) * r);
                normals[v] = Vector3.up;
                uvs[v] = new Vector2((float)seg / segments, 1);
                v++;
            }

            verts[v] = new Vector3(0, 0.1f, 0);
            normals[v] = Vector3.up;
            uvs[v] = new Vector2(0.5f, 0.5f);
            int centerIdx = v;

            int triCount = segments * 6 * 2 + segments * 3;
            var tris = new int[triCount];
            int ti = 0;
            int cols = segments + 1;

            // Outer wall
            for (int seg = 0; seg < segments; seg++)
            {
                int a = seg;
                int b = a + cols;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }

            // Inner surface
            for (int seg = 0; seg < segments; seg++)
            {
                int a = cols + seg;
                int b = cols * 2 + seg;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }

            // Center floor
            for (int seg = 0; seg < segments; seg++)
            {
                tris[ti++] = centerIdx;
                tris[ti++] = cols * 2 + seg + 1;
                tris[ti++] = cols * 2 + seg;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateMountain(float baseRadius = 3f, float height = 6f, int seed = 0)
        {
            var mesh = new Mesh { name = "Mountain" };
            int rings = 5;
            int segments = 12;
            int vertCount = rings * (segments + 1) + 1;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var rng = new System.Random(seed);

            int v = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                float t = (float)ring / (rings - 1);
                float y = t * height;
                float r = baseRadius * (1f - t * 0.9f);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    float jitter = 1f + (float)(rng.NextDouble() - 0.5) * 0.3f * (1f - t);
                    verts[v] = new Vector3(Mathf.Cos(angle) * r * jitter, y, Mathf.Sin(angle) * r * jitter);
                    normals[v] = new Vector3(Mathf.Cos(angle), 0.5f, Mathf.Sin(angle)).normalized;
                    uvs[v] = new Vector2((float)seg / segments, t);
                    v++;
                }
            }

            // Peak
            verts[v] = new Vector3(0, height, 0);
            normals[v] = Vector3.up;
            uvs[v] = new Vector2(0.5f, 1);
            int peakIdx = v;

            int triCount = (rings - 1) * segments * 6 + segments * 3;
            var tris = new int[triCount];
            int ti = 0;
            int cols = segments + 1;

            for (int ring = 0; ring < rings - 1; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int a = ring * cols + seg;
                    int b = a + cols;
                    tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                    tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
                }
            }

            int lastRingStart = (rings - 1) * cols;
            for (int seg = 0; seg < segments; seg++)
            {
                tris[ti++] = lastRingStart + seg;
                tris[ti++] = peakIdx;
                tris[ti++] = lastRingStart + seg + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateFlagPole(float height = 5f)
        {
            var mesh = new Mesh { name = "FlagPole" };
            int segments = 6;
            float poleR = 0.05f;
            float flagW = 0.8f;
            float flagH = 0.5f;

            int poleVerts = (segments + 1) * 2;
            int flagVerts = 4;
            int totalVerts = poleVerts + flagVerts;

            var verts = new Vector3[totalVerts];
            var normals = new Vector3[totalVerts];
            var uvs = new Vector2[totalVerts];

            int v = 0;
            for (int i = 0; i <= 1; i++)
            {
                float y = i * height;
                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    verts[v] = new Vector3(Mathf.Cos(angle) * poleR, y, Mathf.Sin(angle) * poleR);
                    normals[v] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    uvs[v] = new Vector2((float)seg / segments, i);
                    v++;
                }
            }

            // Flag banner
            verts[v] = new Vector3(0, height, 0);
            normals[v] = Vector3.forward;
            uvs[v] = new Vector2(0, 1);
            v++;
            verts[v] = new Vector3(flagW, height, 0);
            normals[v] = Vector3.forward;
            uvs[v] = new Vector2(1, 1);
            v++;
            verts[v] = new Vector3(flagW, height - flagH, 0);
            normals[v] = Vector3.forward;
            uvs[v] = new Vector2(1, 0.5f);
            v++;
            verts[v] = new Vector3(0, height - flagH, 0);
            normals[v] = Vector3.forward;
            uvs[v] = new Vector2(0, 0.5f);

            int poleTris = segments * 6;
            int flagTris = 6;
            var tris = new int[poleTris + flagTris];
            int ti = 0;
            int cols = segments + 1;

            for (int seg = 0; seg < segments; seg++)
            {
                int a = seg;
                int b = a + cols;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }

            int fStart = poleVerts;
            tris[ti++] = fStart; tris[ti++] = fStart + 1; tris[ti++] = fStart + 2;
            tris[ti++] = fStart; tris[ti++] = fStart + 2; tris[ti++] = fStart + 3;

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateWallRing(float radius = 10f, float height = 1f, float thickness = 0.3f)
        {
            var mesh = new Mesh { name = "WallRing" };
            int segments = 24;
            int vertCount = (segments + 1) * 4;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            float outerR = radius;
            float innerR = radius - thickness;

            // Outer bottom, outer top, inner bottom, inner top
            float[] radii = { outerR, outerR, innerR, innerR };
            float[] heights = { 0, height, 0, height };
            float[] normDir = { 1, 1, -1, -1 };

            for (int ring = 0; ring < 4; ring++)
            {
                float r = radii[ring];
                float y = heights[ring];
                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    verts[v] = new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
                    normals[v] = new Vector3(Mathf.Cos(angle) * normDir[ring], 0, Mathf.Sin(angle) * normDir[ring]);
                    uvs[v] = new Vector2((float)seg / segments, ring * 0.25f);
                    v++;
                }
            }

            int triCount = segments * 6 * 3;
            var tris = new int[triCount];
            int ti = 0;
            int cols = segments + 1;

            // Outer face
            for (int seg = 0; seg < segments; seg++)
            {
                int a = seg;
                int b = a + cols;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }

            // Inner face
            for (int seg = 0; seg < segments; seg++)
            {
                int a = cols * 2 + seg;
                int b = a + cols;
                tris[ti++] = a; tris[ti++] = a + 1; tris[ti++] = b;
                tris[ti++] = a + 1; tris[ti++] = b + 1; tris[ti++] = b;
            }

            // Top face
            for (int seg = 0; seg < segments; seg++)
            {
                int outer = cols + seg;
                int inner = cols * 3 + seg;
                tris[ti++] = outer; tris[ti++] = inner; tris[ti++] = outer + 1;
                tris[ti++] = outer + 1; tris[ti++] = inner; tris[ti++] = inner + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
