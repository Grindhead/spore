using UnityEngine;

namespace RoobsWorld.Utils
{
    public static class CreaturePartMeshFactory
    {
        public static Mesh CreateEye(float radius = 0.06f)
        {
            var mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 10);
            mesh.name = "EyeMesh";
            return mesh;
        }

        public static Mesh CreatePupil(float radius = 0.03f)
        {
            var mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 8);
            mesh.name = "PupilMesh";
            return mesh;
        }

        public static Mesh CreateMouth(float width = 0.3f, float height = 0.08f, float depth = 0.15f)
        {
            var mesh = new Mesh { name = "MouthMesh" };
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float hd = depth * 0.5f;

            int segments = 10;
            int vertCount = (segments + 1) * 4 + 2;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            // Upper jaw - curved
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.PI * t;
                float x = Mathf.Cos(angle) * hw;
                float z = -hd + t * depth;
                float y = hh * (1f + 0.3f * Mathf.Sin(angle));
                verts[v] = new Vector3(x, y, z);
                normals[v] = Vector3.up;
                uvs[v] = new Vector2(t, 1);
                v++;
            }

            // Lower jaw
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = Mathf.PI * t;
                float x = Mathf.Cos(angle) * hw;
                float z = -hd + t * depth;
                float y = -hh * (1f + 0.2f * Mathf.Sin(angle));
                verts[v] = new Vector3(x, y, z);
                normals[v] = Vector3.down;
                uvs[v] = new Vector2(t, 0);
                v++;
            }

            // Left side
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float z = -hd + t * depth;
                verts[v] = new Vector3(-hw * Mathf.Cos(Mathf.PI * t), 0, z);
                normals[v] = Vector3.left;
                uvs[v] = new Vector2(t, 0.5f);
                v++;
            }

            // Right side
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float z = -hd + t * depth;
                verts[v] = new Vector3(hw * Mathf.Cos(Mathf.PI * t), 0, z);
                normals[v] = Vector3.right;
                uvs[v] = new Vector2(t, 0.5f);
                v++;
            }

            // Front / back points
            verts[v] = new Vector3(0, 0, hd);
            normals[v] = Vector3.forward;
            uvs[v] = new Vector2(0.5f, 0.5f);
            int frontIdx = v++;

            verts[v] = new Vector3(0, 0, -hd);
            normals[v] = Vector3.back;
            uvs[v] = new Vector2(0.5f, 0.5f);

            int cols = segments + 1;
            int triCount = segments * 6 * 2;
            var tris = new int[triCount];
            int ti = 0;

            // Upper to lower
            for (int i = 0; i < segments; i++)
            {
                int a = i;
                int b = cols + i;
                tris[ti++] = a; tris[ti++] = a + 1; tris[ti++] = b;
                tris[ti++] = a + 1; tris[ti++] = b + 1; tris[ti++] = b;
            }

            // Sides
            for (int i = 0; i < segments; i++)
            {
                int a = cols * 2 + i;
                int b = cols * 3 + i;
                tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateLeg(float length = 0.5f, float radius = 0.05f)
        {
            var mesh = new Mesh { name = "LegMesh" };
            int segments = 8;
            int rings = 4;
            int vertCount = (rings + 1) * (segments + 1) + 1;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            float kneePos = 0.55f;
            float kneeBulge = 1.25f;

            for (int ring = 0; ring <= rings; ring++)
            {
                float t = (float)ring / rings;
                float y = -t * length;
                float r = radius;

                if (Mathf.Abs(t - kneePos) < 0.2f)
                    r *= Mathf.Lerp(1f, kneeBulge, 1f - Mathf.Abs(t - kneePos) / 0.2f);

                float taper = 1f - t * 0.3f;
                r *= taper;

                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    verts[v] = new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
                    normals[v] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    uvs[v] = new Vector2((float)seg / segments, t);
                    v++;
                }
            }

            // Foot cap
            verts[v] = new Vector3(0, -length, 0);
            normals[v] = Vector3.down;
            uvs[v] = new Vector2(0.5f, 1);
            int footIdx = v;

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

            int lastRing = rings * cols;
            for (int seg = 0; seg < segments; seg++)
            {
                tris[ti++] = lastRing + seg;
                tris[ti++] = footIdx;
                tris[ti++] = lastRing + seg + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateArm(float length = 0.4f, float radius = 0.04f)
        {
            var leg = CreateLeg(length, radius);
            leg.name = "ArmMesh";
            return leg;
        }

        public static Mesh CreateWing(float span = 0.6f, float chord = 0.3f)
        {
            var mesh = new Mesh { name = "WingMesh" };
            int spanSegs = 6;
            int chordSegs = 4;
            int vertCount = (spanSegs + 1) * (chordSegs + 1);
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int s = 0; s <= spanSegs; s++)
            {
                float st = (float)s / spanSegs;
                float x = st * span;
                float localChord = chord * (1f - st * 0.6f);
                float camber = 0.03f * Mathf.Sin(Mathf.PI * st);

                for (int c = 0; c <= chordSegs; c++)
                {
                    float ct = (float)c / chordSegs;
                    float z = (ct - 0.5f) * localChord;
                    float y = camber * Mathf.Sin(Mathf.PI * ct);
                    float thickness = 0.01f * (1f - st * 0.5f) * Mathf.Sin(Mathf.PI * ct);
                    y += thickness;

                    verts[v] = new Vector3(x, y, z);
                    normals[v] = Vector3.up;
                    uvs[v] = new Vector2(st, ct);
                    v++;
                }
            }

            int triCount = spanSegs * chordSegs * 6;
            var tris = new int[triCount];
            int ti = 0;
            int cols = chordSegs + 1;

            for (int s = 0; s < spanSegs; s++)
            {
                for (int c = 0; c < chordSegs; c++)
                {
                    int a = s * cols + c;
                    int b = a + cols;
                    tris[ti++] = a; tris[ti++] = b; tris[ti++] = a + 1;
                    tris[ti++] = a + 1; tris[ti++] = b; tris[ti++] = b + 1;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateTail(float length = 0.5f, float baseRadius = 0.06f)
        {
            var mesh = new Mesh { name = "TailMesh" };
            int segments = 8;
            int rings = 6;
            int vertCount = (rings + 1) * (segments + 1) + 1;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int ring = 0; ring <= rings; ring++)
            {
                float t = (float)ring / rings;
                float z = -t * length;
                float r = baseRadius * (1f - t * 0.85f);
                float wave = 0.02f * Mathf.Sin(t * 3f * Mathf.PI);

                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments;
                    float x = Mathf.Cos(angle) * r;
                    float y = Mathf.Sin(angle) * r + wave;
                    verts[v] = new Vector3(x, y, z);
                    normals[v] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                    uvs[v] = new Vector2((float)seg / segments, t);
                    v++;
                }
            }

            verts[v] = new Vector3(0, 0, -length);
            normals[v] = Vector3.back;
            uvs[v] = new Vector2(0.5f, 1);
            int tipIdx = v;

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

            int lastRing = rings * cols;
            for (int seg = 0; seg < segments; seg++)
            {
                tris[ti++] = lastRing + seg;
                tris[ti++] = tipIdx;
                tris[ti++] = lastRing + seg + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateHorn(float length = 0.2f, float baseRadius = 0.04f)
        {
            var mesh = new Mesh { name = "HornMesh" };
            int segments = 6;
            int rings = 5;
            int vertCount = rings * (segments + 1) + 1;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                float t = (float)ring / rings;
                float y = t * length;
                float r = baseRadius * (1f - t * t);
                float twist = t * 15f * Mathf.Deg2Rad;

                for (int seg = 0; seg <= segments; seg++)
                {
                    float angle = 2f * Mathf.PI * seg / segments + twist;
                    verts[v] = new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r);
                    normals[v] = new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)).normalized;
                    uvs[v] = new Vector2((float)seg / segments, t);
                    v++;
                }
            }

            verts[v] = new Vector3(0, length, 0);
            normals[v] = Vector3.up;
            uvs[v] = new Vector2(0.5f, 1);
            int tipIdx = v;

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

            int lastRing = (rings - 1) * cols;
            for (int seg = 0; seg < segments; seg++)
            {
                tris[ti++] = lastRing + seg;
                tris[ti++] = tipIdx;
                tris[ti++] = lastRing + seg + 1;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateFin(float height = 0.3f, float length = 0.2f)
        {
            var mesh = new Mesh { name = "FinMesh" };
            int segs = 6;
            int vertCount = (segs + 1) * 2;
            var verts = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            int v = 0;
            for (int i = 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                float z = (t - 0.5f) * length;
                float h = height * Mathf.Sin(Mathf.PI * t);
                float x = 0.005f;

                verts[v] = new Vector3(x, 0, z);
                normals[v] = Vector3.right;
                uvs[v] = new Vector2(t, 0);
                v++;

                verts[v] = new Vector3(x, h, z);
                normals[v] = Vector3.right;
                uvs[v] = new Vector2(t, 1);
                v++;
            }

            int triCount = segs * 6;
            var tris = new int[triCount];
            int ti = 0;
            for (int i = 0; i < segs; i++)
            {
                int a = i * 2;
                tris[ti++] = a; tris[ti++] = a + 1; tris[ti++] = a + 2;
                tris[ti++] = a + 1; tris[ti++] = a + 3; tris[ti++] = a + 2;
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh CreateShell(float radius = 0.15f)
        {
            var mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 10);
            var verts = mesh.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                if (verts[i].y < 0)
                    verts[i].y *= 0.3f;
                else
                    verts[i].y *= 1.2f;

                float ridges = 1f + 0.06f * Mathf.Sin(verts[i].y * 30f);
                verts[i].x *= ridges;
                verts[i].z *= ridges;
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "ShellMesh";
            return mesh;
        }

        public static Mesh CreateSpike(float length = 0.15f, float baseRadius = 0.03f)
        {
            return CreateHorn(length, baseRadius);
        }

        public static Mesh CreateSpineSegment(float radius = 0.3f)
        {
            var mesh = ProceduralMeshFactory.CreateOrganicSphere(radius, 10);
            var verts = mesh.vertices;

            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].z *= 1.3f;
                verts[i].x *= 0.9f;
            }

            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = "SpineSegment";
            return mesh;
        }

        public static Mesh GetPartMesh(string partId)
        {
            if (partId.StartsWith("eye")) return CreateEye();
            if (partId.StartsWith("mouth")) return CreateMouth();
            if (partId.StartsWith("leg")) return CreateLeg();
            if (partId.StartsWith("arm")) return CreateArm();
            if (partId.StartsWith("wing")) return CreateWing();
            if (partId.StartsWith("tail")) return CreateTail();
            if (partId.StartsWith("horn")) return CreateHorn();
            if (partId.StartsWith("fin")) return CreateFin();
            if (partId.StartsWith("shell")) return CreateShell();
            if (partId.StartsWith("spike")) return CreateSpike();
            return CreateOrganicSphere(0.1f, 6);
        }

        private static Mesh CreateOrganicSphere(float radius, int subdivisions)
        {
            return ProceduralMeshFactory.CreateOrganicSphere(radius, subdivisions);
        }

        public static Color GetPartColor(string partId)
        {
            if (partId.StartsWith("eye")) return Color.white;
            if (partId.StartsWith("mouth")) return new Color(0.8f, 0.2f, 0.2f);
            if (partId.StartsWith("leg")) return new Color(0.5f, 0.4f, 0.3f);
            if (partId.StartsWith("arm")) return new Color(0.6f, 0.5f, 0.3f);
            if (partId.StartsWith("wing")) return new Color(0.8f, 0.8f, 0.9f);
            if (partId.StartsWith("tail")) return new Color(0.4f, 0.6f, 0.3f);
            if (partId.StartsWith("horn")) return new Color(0.7f, 0.7f, 0.65f);
            if (partId.StartsWith("fin")) return new Color(0.3f, 0.6f, 0.8f);
            if (partId.StartsWith("shell")) return new Color(0.6f, 0.5f, 0.3f);
            if (partId.StartsWith("spike")) return new Color(0.6f, 0.6f, 0.55f);
            return Color.magenta;
        }
    }
}
