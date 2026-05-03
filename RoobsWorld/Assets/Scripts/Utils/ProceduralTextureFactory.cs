using UnityEngine;

namespace RoobsWorld.Utils
{
    public static class ProceduralTextureFactory
    {
        public static Color Opaque(Color color)
        {
            color.a = 1f;
            return color;
        }

        private static float Noise2D(float x, float y)
        {
            float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        private static float SmoothNoise(float x, float y)
        {
            int ix = Mathf.FloorToInt(x);
            int iy = Mathf.FloorToInt(y);
            float fx = x - ix;
            float fy = y - iy;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Noise2D(ix, iy);
            float b = Noise2D(ix + 1, iy);
            float c = Noise2D(ix, iy + 1);
            float d = Noise2D(ix + 1, iy + 1);

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float FBM(float x, float y, int octaves = 4)
        {
            float value = 0f;
            float amplitude = 0.5f;
            float frequency = 1f;
            for (int i = 0; i < octaves; i++)
            {
                value += amplitude * SmoothNoise(x * frequency, y * frequency);
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            return value;
        }

        public static Texture2D CreateGrassTexture(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "GrassTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 4f;
                    float ny = (float)y / size * 4f;
                    float n = FBM(nx, ny, 4);
                    float r = 0.2f + n * 0.15f;
                    float g = 0.4f + n * 0.3f;
                    float b = 0.1f + n * 0.1f;
                    pixels[y * size + x] = new Color(r, g, b);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateWaterTexture(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "WaterTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 6f;
                    float ny = (float)y / size * 6f;
                    float n = FBM(nx, ny, 3);
                    float wave = 0.5f + 0.5f * Mathf.Sin(nx * 3f + n * 2f);
                    float r = 0.05f + wave * 0.1f;
                    float g = 0.2f + wave * 0.15f + n * 0.1f;
                    float b2 = 0.5f + wave * 0.3f + n * 0.15f;
                    pixels[y * size + x] = new Color(r, g, b2, 0.85f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateCellArenaTexture(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "CellArenaTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;
                    float nx = u * 7.5f;
                    float ny = v * 7.5f;

                    float n1 = FBM(nx, ny, 5);
                    float n2 = FBM(nx * 0.55f + 21.7f, ny * 0.55f - 13.9f, 4);
                    float flow = Mathf.Sin((nx + n2 * 2.3f) * 2.2f) * Mathf.Cos((ny - n2 * 1.8f) * 1.7f);
                    float caustic = Mathf.Clamp01(0.45f + n1 * 0.35f + flow * 0.2f);

                    float cx = u - 0.5f;
                    float cy = v - 0.5f;
                    float radial = Mathf.Clamp01(1f - Mathf.Sqrt(cx * cx + cy * cy) * 1.15f);
                    radial = 0.7f + radial * 0.3f;

                    float r = (0.12f + caustic * 0.08f + n2 * 0.02f) * radial;
                    float g = (0.33f + caustic * 0.22f + n1 * 0.04f) * radial;
                    float b2 = (0.42f + caustic * 0.3f + n2 * 0.05f) * radial;
                    float a = 0.78f + caustic * 0.12f;
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(r),
                        Mathf.Clamp01(g),
                        Mathf.Clamp01(b2),
                        Mathf.Clamp01(a));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateRockTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "RockTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 6f;
                    float ny = (float)y / size * 6f;
                    float n = FBM(nx, ny, 5);
                    float base_v = 0.4f + n * 0.2f;
                    pixels[y * size + x] = new Color(base_v, base_v * 0.95f, base_v * 0.9f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateBarkTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "BarkTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 3f;
                    float ny = (float)y / size * 12f;
                    float n = FBM(nx, ny, 4);
                    float streak = 0.5f + 0.5f * Mathf.Sin(ny * 6f + n * 3f);
                    float r = 0.3f + streak * 0.15f;
                    float g = 0.2f + streak * 0.1f;
                    float b = 0.08f + streak * 0.05f;
                    pixels[y * size + x] = new Color(r, g, b);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateLeafTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "LeafTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 5f;
                    float ny = (float)y / size * 5f;
                    float n = FBM(nx, ny, 3);
                    float r = 0.1f + n * 0.1f;
                    float g = 0.35f + n * 0.3f;
                    float b = 0.08f + n * 0.08f;
                    pixels[y * size + x] = new Color(r, g, b);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateCreatureSkinTexture(Color primary, Color secondary, int size = 64)
        {
            primary = Opaque(primary);
            secondary = Opaque(secondary);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "CreatureSkin" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 4f;
                    float ny = (float)y / size * 4f;
                    float n = FBM(nx, ny, 3);
                    float pattern = 0.5f + 0.5f * Mathf.Sin(nx * 3f + n * 2f) * Mathf.Cos(ny * 2f + n);
                    pixels[y * size + x] = Opaque(Color.Lerp(primary, secondary, pattern));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateBuildingSurfaceTexture(Color baseColor, int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "BuildingSurface" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size;
                    float ny = (float)y / size;

                    float brickX = nx * 8f;
                    float brickY = ny * 16f;
                    int row = Mathf.FloorToInt(brickY);
                    if (row % 2 == 1) brickX += 0.5f;
                    float mortarX = Mathf.Abs(brickX - Mathf.Round(brickX));
                    float mortarY = Mathf.Abs(brickY - Mathf.Round(brickY));
                    float mortar = (mortarX < 0.05f || mortarY < 0.05f) ? 0.7f : 1f;

                    float n = FBM(nx * 8f, ny * 8f, 2) * 0.1f;
                    Color c = baseColor * (mortar + n);
                    c.a = 1f;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateMountainTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "MountainTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 6f;
                    float ny = (float)y / size * 6f;
                    float n = FBM(nx, ny, 4);
                    float height = (float)y / size;
                    float snow = height > 0.75f ? Mathf.Lerp(0f, 0.4f, (height - 0.75f) * 4f) : 0f;
                    float base_v = 0.35f + n * 0.2f;
                    float r = base_v + snow;
                    float g = base_v * 0.9f + snow;
                    float b = base_v * 0.8f + snow;
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateSpiceTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "SpiceTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 5f;
                    float ny = (float)y / size * 5f;
                    float n = FBM(nx, ny, 3);
                    float sparkle = Noise2D(x * 7.3f, y * 11.7f) > 0.92f ? 0.3f : 0f;
                    float r = 0.8f + n * 0.15f + sparkle;
                    float g = 0.4f + n * 0.15f + sparkle * 0.5f;
                    float b = 0.05f + n * 0.05f;
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateStarTexture(Color starColor, int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "StarTexture" };
            var pixels = new Color[size * size];
            float center = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    float glow = Mathf.Clamp01(1f - dist);
                    glow = glow * glow * glow;
                    float corona = Mathf.Clamp01(1f - dist * 0.8f) * 0.3f;
                    float n = FBM(x * 0.1f, y * 0.1f, 2) * 0.15f;
                    Color c = starColor * (glow + corona + n);
                    c.a = Mathf.Clamp01(glow + corona * 0.5f);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        public static Texture2D CreateEditorFloorTexture(int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "EditorFloor" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float gridSize = size / 10f;
                    int gx = Mathf.FloorToInt(x / gridSize);
                    int gy = Mathf.FloorToInt(y / gridSize);
                    bool dark = (gx + gy) % 2 == 0;
                    float base_v = dark ? 0.12f : 0.18f;
                    float n = FBM(x * 0.05f, y * 0.05f, 2) * 0.03f;
                    pixels[y * size + x] = new Color(base_v + n, base_v + n, base_v + n * 1.3f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreateNestTexture(int size = 64)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "NestTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 8f;
                    float ny = (float)y / size * 8f;
                    float n = FBM(nx, ny, 3);
                    float straw = 0.5f + 0.5f * Mathf.Sin(nx * 10f + n * 3f);
                    float r = 0.6f + straw * 0.2f;
                    float g = 0.45f + straw * 0.15f;
                    float b = 0.2f + straw * 0.05f;
                    pixels[y * size + x] = new Color(r, g, b);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Texture2D CreatePlanetTexture(Color baseColor, int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "PlanetTexture" };
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * 6f;
                    float ny = (float)y / size * 3f;
                    float n = FBM(nx, ny, 4);
                    float band = 0.5f + 0.5f * Mathf.Sin(ny * 4f + n * 2f);
                    Color c = Color.Lerp(baseColor * 0.7f, baseColor * 1.3f, band);
                    c += new Color(n * 0.1f, n * 0.1f, n * 0.1f);
                    c.a = 1f;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            return tex;
        }

        public static Material CreateMaterialWithTexture(Texture2D texture, Color tint)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = texture.name + "_Mat" };
            mat.mainTexture = texture;
            mat.color = tint;
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Smoothness", 0.3f);
            return mat;
        }

        public static Material CreateTransparentMaterial(Texture2D texture, Color tint)
        {
            var mat = CreateMaterialWithTexture(texture, tint);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return mat;
        }
    }
}
