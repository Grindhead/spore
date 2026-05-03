using UnityEngine;

namespace RoobsWorld.Utils
{
    /// <summary>
    /// Runtime version of the painted microbe art used on the main menu.
    /// It keeps gameplay colliders intact while replacing mesh visuals with a top-facing illustrated quad.
    /// </summary>
    public static class MenuMicrobeVisuals
    {
        private const string VisualName = "MenuMicrobeVisual";

        public static GameObject Apply(GameObject root, Color bodyColor, int seed, int variant, float aspect = 1.35f, bool hideExistingRenderers = true)
        {
            if (root == null)
                return null;

            if (hideExistingRenderers)
                HideExistingRenderers(root);

            RemoveExistingVisual(root);

            bodyColor = ProceduralTextureFactory.Opaque(bodyColor);
            var visual = new GameObject(VisualName);
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * 0.055f;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            var mf = visual.AddComponent<MeshFilter>();
            mf.sharedMesh = CreateQuadMesh(aspect);

            var mr = visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = CreateMaterial(CreateTexture(240, 180, seed, bodyColor, Color.Lerp(Color.black, bodyColor, 0.24f), variant));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return visual;
        }

        private static void HideExistingRenderers(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].gameObject.name != VisualName)
                    renderers[i].enabled = false;
            }
        }

        private static void RemoveExistingVisual(GameObject root)
        {
            var existing = root.transform.Find(VisualName);
            if (existing != null)
                UnityObjectUtil.Destroy(existing.gameObject);
        }

        private static Mesh CreateQuadMesh(float aspect)
        {
            float halfW = Mathf.Max(0.2f, aspect) * 0.5f;
            float halfH = 0.5f;
            var mesh = new Mesh { name = "MenuMicrobeQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-halfW, 0f, -halfH),
                new Vector3( halfW, 0f, -halfH),
                new Vector3(-halfW, 0f,  halfH),
                new Vector3( halfW, 0f,  halfH)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreateMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = "MenuMicrobeVisual_Mat", mainTexture = texture };
            mat.color = Color.white;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", texture);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return mat;
        }

        private static Texture2D CreateTexture(int width, int height, int seed, Color body, Color outline, int variant)
        {
            var rng = new System.Random(seed);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "MenuMicrobeVisual_Tex"
            };

            var pixels = ClearPixels(width, height);
            var center = new Vector2(width * 0.5f + Rand(rng, -5f, 5f), height * 0.52f + Rand(rng, -4f, 4f));
            float rx = Rand(rng, 54f, 74f);
            float ry = Rand(rng, 36f, 52f);
            float tilt = Rand(rng, -0.35f, 0.35f);

            int tendrils = 5 + (variant % 6);
            for (int i = 0; i < tendrils; i++)
            {
                float a = Mathf.PI * 2f * i / tendrils + Rand(rng, -0.24f, 0.24f);
                var start = center + new Vector2(Mathf.Cos(a) * rx * 0.78f, Mathf.Sin(a) * ry * 0.78f);
                var end = center + new Vector2(Mathf.Cos(a) * rx * Rand(rng, 1.22f, 1.75f), Mathf.Sin(a) * ry * Rand(rng, 1.18f, 1.68f));
                var mid = Vector2.Lerp(start, end, 0.55f) + new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * Rand(rng, -16f, 16f);
                Bezier(pixels, width, height, start, mid, end, 4, new Color(0.66f, 0.24f, 0.18f, 0.92f));
                Bezier(pixels, width, height, start + Vector2.one, mid, end, 2, new Color(1f, 0.74f, 0.62f, 0.9f));
            }

            Ellipse(pixels, width, height, center, rx + 5f, ry + 5f, tilt, outline, true, 3);
            Ellipse(pixels, width, height, center, rx, ry, tilt, body, true, 3);
            Ellipse(pixels, width, height, center + new Vector2(-rx * 0.18f, ry * 0.16f), rx * 0.58f, ry * 0.32f, tilt, new Color(1f, 1f, 1f, 0.16f), true, 3);

            int marks = 12 + variant;
            for (int i = 0; i < marks; i++)
            {
                var p = center + new Vector2(Rand(rng, -rx * 0.65f, rx * 0.65f), Rand(rng, -ry * 0.62f, ry * 0.62f));
                float s = Rand(rng, 2.5f, 7f);
                Ellipse(pixels, width, height, p, s, s * Rand(rng, 0.7f, 1.4f), tilt, new Color(1f, 0.93f, 0.55f, 0.42f), variant % 2 == 0, 2);
            }

            int eyes = Mathf.Clamp(variant + 1, 2, 7);
            for (int i = 0; i < eyes; i++)
            {
                float a = Mathf.PI * 2f * (i + 0.2f) / eyes;
                var eye = center + new Vector2(Mathf.Cos(a) * rx * 0.58f, Mathf.Sin(a) * ry * 0.55f);
                Ellipse(pixels, width, height, eye, 12f, 12f, 0f, outline, true, 2);
                Ellipse(pixels, width, height, eye, 8.5f, 8.5f, 0f, new Color(0.92f, 0.98f, 1f), true, 2);
                Ellipse(pixels, width, height, eye + new Vector2(1.5f, -0.5f), 4.2f, 4.2f, 0f, new Color(0.04f, 0.04f, 0.12f), true, 2);
                Ellipse(pixels, width, height, eye + new Vector2(-1.5f, 1.5f), 1.7f, 1.7f, 0f, Color.white, true, 1);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Color[] ClearPixels(int width, int height)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            return pixels;
        }

        private static void Ellipse(Color[] pixels, int width, int height, Vector2 center, float rx, float ry, float angle, Color color, bool filled, int thickness)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - rx - thickness - 2));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + rx + thickness + 2));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - ry - thickness - 2));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + ry + thickness + 2));
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float px = dx * cos + dy * sin;
                    float py = -dx * sin + dy * cos;
                    float d = px * px / (rx * rx) + py * py / (ry * ry);
                    if ((filled && d <= 1f) || (!filled && Mathf.Abs(d - 1f) <= thickness * 0.055f))
                        Blend(pixels, width, height, x, y, color);
                }
            }
        }

        private static void Bezier(Color[] pixels, int width, int height, Vector2 a, Vector2 b, Vector2 c, int thickness, Color color)
        {
            Vector2 previous = a;
            for (int i = 1; i <= 28; i++)
            {
                float t = i / 28f;
                Vector2 p = (1f - t) * (1f - t) * a + 2f * (1f - t) * t * b + t * t * c;
                Line(pixels, width, height, previous, p, thickness, color);
                previous = p;
            }
        }

        private static void Line(Color[] pixels, int width, int height, Vector2 a, Vector2 b, int thickness, Color color)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                Disc(pixels, width, height, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), thickness, color);
            }
        }

        private static void Disc(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                        Blend(pixels, width, height, x, y, color);
        }

        private static void Blend(Color[] pixels, int width, int height, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;

            int index = y * width + x;
            Color dst = pixels[index];
            float inv = 1f - color.a;
            pixels[index] = new Color(
                color.r * color.a + dst.r * inv,
                color.g * color.a + dst.g * inv,
                color.b * color.a + dst.b * inv,
                color.a + dst.a * inv);
        }

        private static float Rand(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
