using UnityEngine;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Generates runtime textures/sprites used by MainMenuUI.
    /// </summary>
    public static class MainMenuArtFactory
    {
        public static Texture2D CreateWaterTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1f);
                    float wave = Mathf.Sin(u * 23f) * 0.05f + Mathf.Cos(v * 17f) * 0.05f;
                    pixels[y * width + x] = Color.Lerp(
                        UIStyleGuide.Colors.DeepNavy,
                        UIStyleGuide.Colors.TideTeal,
                        Mathf.Clamp01(v * 0.75f + wave + 0.15f));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateDoodleTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Repeat;
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;

            var rng = new System.Random(4311);
            int marks = Mathf.Max(60, (width * height) / 26000);
            for (int i = 0; i < marks; i++)
            {
                int cx = rng.Next(0, width);
                int cy = rng.Next(0, height);
                int radius = rng.Next(8, 22);
                var tint = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.35f);
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y > radius * radius)
                            continue;
                        int px = cx + x;
                        int py = cy + y;
                        if (px < 0 || px >= width || py < 0 || py >= height)
                            continue;
                        pixels[py * width + px] = Color.Lerp(pixels[py * width + px], tint, 0.2f);
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateGlowTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / maxR;
                    float a = Mathf.Clamp01(1f - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateRoundedTexture(int width, int height, int radius, Color fill, Color stroke, int strokeWidth)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, width, height, radius);
                    if (!inside)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    bool border = !IsInsideRoundedRect(x - strokeWidth, y - strokeWidth, width - strokeWidth * 2, height - strokeWidth * 2, Mathf.Max(0, radius - strokeWidth));
                    pixels[y * width + x] = border ? stroke : fill;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (width <= 0 || height <= 0)
                return false;
            if (x < 0 || y < 0 || x >= width || y >= height)
                return false;
            if (radius <= 0)
                return true;

            int rx = Mathf.Min(radius, width / 2);
            int ry = Mathf.Min(radius, height / 2);

            if (x >= rx && x < width - rx) return true;
            if (y >= ry && y < height - ry) return true;

            int cx = x < rx ? rx : width - rx - 1;
            int cy = y < ry ? ry : height - ry - 1;
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= rx * ry;
        }
    }
}
