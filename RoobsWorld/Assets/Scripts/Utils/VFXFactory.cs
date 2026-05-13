using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Utils
{
    /// <summary>
    /// Identifiers for procedural particle effects used across stages (combat, pickup, transition, etc.).
    /// </summary>
    public enum VFXType
    {
        Explosion,
        DamageHit,
        HealGlow,
        SpeedTrail,
        SpawnBurst,
        DeathPoof,
        FoodEat,
        DNAPickup,
        StageTransition,
        ShieldHit,
        TerraformPulse,
        BuildingComplete,
        DiplomacyHeart,
        CombatSlash,
        WaterSplash,
        FireEmbers
    }

    public enum VFXParticleStyle
    {
        SoftBlob,
        Spark,
        Spore,
        Droplet,
        Slash,
        Heart
    }

    /// <summary>
    /// Per-effect configuration for particle count, lifetime, speed, size, colors, and shape.
    /// </summary>
    public class VFXConfig
    {
        public int ParticleCount;
        public float Lifetime;
        public float StartSpeed;
        public float StartSize;
        public Color StartColor;
        public Color EndColor;
        public bool Loop;
        public float GravityModifier;
        public ParticleSystemShapeType Shape;
        public float ShapeRadius;
        public VFXParticleStyle ParticleStyle;
        public float RotationSpeed;
        public bool StretchByVelocity;
        public bool Trails;
    }

    /// <summary>
    /// Static catalog of <see cref="VFXConfig"/> entries for each <see cref="VFXType"/>. Used by <see cref="VFXFactory"/> to spawn particles.
    /// </summary>
    public static class VFXCatalog
    {
        private static readonly Dictionary<VFXType, VFXConfig> Configs = new Dictionary<VFXType, VFXConfig>
        {
            { VFXType.Explosion, new VFXConfig
                { ParticleCount = 40, Lifetime = 1.2f, StartSpeed = 8f, StartSize = 0.5f,
                  StartColor = new Color(1f, 0.6f, 0.1f), EndColor = new Color(0.3f, 0.1f, 0f, 0f),
                  Loop = false, GravityModifier = 0.3f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.3f,
                  ParticleStyle = VFXParticleStyle.Spark, RotationSpeed = 180f, StretchByVelocity = true, Trails = true }},

            { VFXType.DamageHit, new VFXConfig
                { ParticleCount = 15, Lifetime = 0.6f, StartSpeed = 4f, StartSize = 0.2f,
                  StartColor = new Color(1f, 0.2f, 0.1f), EndColor = new Color(1f, 0f, 0f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.2f,
                  ParticleStyle = VFXParticleStyle.Spark, RotationSpeed = 240f, StretchByVelocity = true, Trails = true }},

            { VFXType.HealGlow, new VFXConfig
                { ParticleCount = 20, Lifetime = 1.5f, StartSpeed = 1f, StartSize = 0.3f,
                  StartColor = new Color(0.3f, 1f, 0.4f, 0.8f), EndColor = new Color(0.1f, 0.8f, 0.2f, 0f),
                  Loop = true, GravityModifier = -0.5f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.5f,
                  ParticleStyle = VFXParticleStyle.SoftBlob, RotationSpeed = 28f }},

            { VFXType.SpeedTrail, new VFXConfig
                { ParticleCount = 10, Lifetime = 0.8f, StartSpeed = 0.5f, StartSize = 0.15f,
                  StartColor = new Color(0.4f, 0.8f, 1f, 0.6f), EndColor = new Color(0.2f, 0.4f, 0.8f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.1f,
                  ParticleStyle = VFXParticleStyle.Droplet, RotationSpeed = 35f, StretchByVelocity = true, Trails = true }},

            { VFXType.SpawnBurst, new VFXConfig
                { ParticleCount = 30, Lifetime = 1f, StartSpeed = 3f, StartSize = 0.4f,
                  StartColor = new Color(1f, 1f, 0.6f), EndColor = new Color(1f, 0.8f, 0.2f, 0f),
                  Loop = false, GravityModifier = 0.1f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.5f,
                  ParticleStyle = VFXParticleStyle.Spore, RotationSpeed = 90f }},

            { VFXType.DeathPoof, new VFXConfig
                { ParticleCount = 25, Lifetime = 1.5f, StartSpeed = 2f, StartSize = 0.5f,
                  StartColor = new Color(0.5f, 0.5f, 0.5f, 0.8f), EndColor = new Color(0.3f, 0.3f, 0.3f, 0f),
                  Loop = false, GravityModifier = -0.2f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.4f,
                  ParticleStyle = VFXParticleStyle.SoftBlob, RotationSpeed = 45f }},

            { VFXType.FoodEat, new VFXConfig
                { ParticleCount = 12, Lifetime = 0.5f, StartSpeed = 2f, StartSize = 0.15f,
                  StartColor = new Color(0.2f, 0.9f, 0.3f), EndColor = new Color(0.1f, 0.6f, 0.1f, 0f),
                  Loop = false, GravityModifier = 0.1f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.2f,
                  ParticleStyle = VFXParticleStyle.Spore, RotationSpeed = 110f }},

            { VFXType.DNAPickup, new VFXConfig
                { ParticleCount = 20, Lifetime = 1f, StartSpeed = 1.5f, StartSize = 0.2f,
                  StartColor = new Color(0.9f, 0.6f, 1f), EndColor = new Color(0.6f, 0.2f, 1f, 0f),
                  Loop = false, GravityModifier = -0.8f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.3f,
                  ParticleStyle = VFXParticleStyle.Spark, RotationSpeed = 160f, Trails = true }},

            { VFXType.StageTransition, new VFXConfig
                { ParticleCount = 60, Lifetime = 3f, StartSpeed = 5f, StartSize = 0.6f,
                  StartColor = new Color(1f, 1f, 1f, 0.9f), EndColor = new Color(0.4f, 0.8f, 1f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 2f,
                  ParticleStyle = VFXParticleStyle.SoftBlob, RotationSpeed = 70f, Trails = true }},

            { VFXType.ShieldHit, new VFXConfig
                { ParticleCount = 18, Lifetime = 0.8f, StartSpeed = 3f, StartSize = 0.25f,
                  StartColor = new Color(0.3f, 0.6f, 1f, 0.9f), EndColor = new Color(0.1f, 0.3f, 0.8f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Hemisphere, ShapeRadius = 0.5f,
                  ParticleStyle = VFXParticleStyle.Spark, RotationSpeed = 220f, StretchByVelocity = true, Trails = true }},

            { VFXType.TerraformPulse, new VFXConfig
                { ParticleCount = 35, Lifetime = 2f, StartSpeed = 2f, StartSize = 2f,
                  StartColor = new Color(0.2f, 1f, 0.5f, 0.6f), EndColor = new Color(0.1f, 0.8f, 0.3f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 3f,
                  ParticleStyle = VFXParticleStyle.SoftBlob, RotationSpeed = 18f }},

            { VFXType.BuildingComplete, new VFXConfig
                { ParticleCount = 25, Lifetime = 1.2f, StartSpeed = 3f, StartSize = 0.3f,
                  StartColor = new Color(1f, 0.9f, 0.5f), EndColor = new Color(0.8f, 0.6f, 0.2f, 0f),
                  Loop = false, GravityModifier = 0.5f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.5f,
                  ParticleStyle = VFXParticleStyle.Spark, RotationSpeed = 180f, StretchByVelocity = true, Trails = true }},

            { VFXType.DiplomacyHeart, new VFXConfig
                { ParticleCount = 8, Lifetime = 1.5f, StartSpeed = 1f, StartSize = 0.3f,
                  StartColor = new Color(1f, 0.3f, 0.5f, 0.9f), EndColor = new Color(1f, 0.1f, 0.3f, 0f),
                  Loop = false, GravityModifier = -0.5f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.3f,
                  ParticleStyle = VFXParticleStyle.Heart, RotationSpeed = 24f }},

            { VFXType.CombatSlash, new VFXConfig
                { ParticleCount = 10, Lifetime = 0.4f, StartSpeed = 6f, StartSize = 0.15f,
                  StartColor = Color.white, EndColor = new Color(1f, 1f, 1f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.1f,
                  ParticleStyle = VFXParticleStyle.Slash, RotationSpeed = 300f, StretchByVelocity = true, Trails = true }},

            { VFXType.WaterSplash, new VFXConfig
                { ParticleCount = 20, Lifetime = 1f, StartSpeed = 4f, StartSize = 0.2f,
                  StartColor = new Color(0.4f, 0.7f, 1f, 0.8f), EndColor = new Color(0.2f, 0.5f, 0.9f, 0f),
                  Loop = false, GravityModifier = 1f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.3f,
                  ParticleStyle = VFXParticleStyle.Droplet, RotationSpeed = 80f, StretchByVelocity = true }},

            { VFXType.FireEmbers, new VFXConfig
                { ParticleCount = 15, Lifetime = 2f, StartSpeed = 1.5f, StartSize = 0.1f,
                  StartColor = new Color(1f, 0.5f, 0.1f), EndColor = new Color(0.8f, 0.2f, 0f, 0f),
                  Loop = true, GravityModifier = -0.3f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.2f,
                  ParticleStyle = VFXParticleStyle.Spark, RotationSpeed = 120f, StretchByVelocity = true, Trails = true }},
        };

        /// <summary>Returns the particle config for the given effect type, or null if not defined.</summary>
        public static VFXConfig GetConfig(VFXType type)
        {
            return Configs.TryGetValue(type, out var config) ? config : null;
        }
    }

    /// <summary>
    /// Creates procedural particle systems from <see cref="VFXCatalog"/> configs. Use for one-shot or short-lived effects (eat, damage, death, transition, etc.).
    /// </summary>
    public static class VFXFactory
    {
        private static readonly Dictionary<VFXParticleStyle, string> ParticleTexturePaths =
            new Dictionary<VFXParticleStyle, string>
            {
                { VFXParticleStyle.SoftBlob, "VFX/Particles/particle_soft_blob" },
                { VFXParticleStyle.Spark, "VFX/Particles/particle_spark" },
                { VFXParticleStyle.Spore, "VFX/Particles/particle_spore" },
                { VFXParticleStyle.Droplet, "VFX/Particles/particle_droplet" },
                { VFXParticleStyle.Slash, "VFX/Particles/particle_slash" },
                { VFXParticleStyle.Heart, "VFX/Particles/particle_heart" },
            };

        private static readonly Dictionary<VFXParticleStyle, Texture2D> ParticleTextures =
            new Dictionary<VFXParticleStyle, Texture2D>();

        /// <summary>
        /// Spawns a particle effect at the given world position, optionally under a parent. Non-looping effects auto-destroy when finished.
        /// </summary>
        /// <param name="type">Effect type (e.g. FoodEat, DeathPoof, StageTransition).</param>
        /// <param name="position">World position for the effect.</param>
        /// <param name="parent">Optional parent transform; if set, position is still world-space but hierarchy is under parent.</param>
        /// <returns>The ParticleSystem, or null if type has no config.</returns>
        public static ParticleSystem Create(VFXType type, Vector3 position, Transform parent = null)
        {
            var config = VFXCatalog.GetConfig(type);
            if (config == null) return null;

            var go = new GameObject($"VFX_{type}");
            go.transform.position = position;
            if (parent != null) go.transform.SetParent(parent, true);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.maxParticles = config.ParticleCount;
            main.startLifetime = config.Lifetime;
            main.startSpeed = config.StartSpeed;
            main.startSize = config.StartSize;
            main.startColor = config.StartColor;
            main.loop = config.Loop;
            main.gravityModifier = config.GravityModifier;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            if (!config.Loop)
                main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)config.ParticleCount) });

            var shape = ps.shape;
            shape.shapeType = config.Shape;
            shape.radius = config.ShapeRadius;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(config.StartColor, 0f), new GradientColorKey(config.EndColor, 1f) },
                new[] { new GradientAlphaKey(config.StartColor.a, 0f), new GradientAlphaKey(config.EndColor.a, 1f) }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.12f, 1f),
                new Keyframe(0.72f, 0.62f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = Mathf.Abs(config.RotationSpeed) > 0.01f;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(
                -config.RotationSpeed * Mathf.Deg2Rad,
                config.RotationSpeed * Mathf.Deg2Rad);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = config.StretchByVelocity ? 0.16f : 0.08f;
            noise.frequency = 0.42f;
            noise.scrollSpeed = config.Loop ? 0.28f : 0.55f;

            var trails = ps.trails;
            trails.enabled = config.Trails;
            trails.ratio = config.StretchByVelocity ? 0.65f : 0.3f;
            trails.lifetime = 0.18f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.55f);
            trails.colorOverTrail = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var particleShader = Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                ?? Shader.Find("Sprites/Default");
            var vfxMat = new Material(particleShader);
            ConfigureParticleMaterial(vfxMat);
            vfxMat.color = config.StartColor;
            vfxMat.mainTexture = GetParticleTexture(config.ParticleStyle);
            vfxMat.SetColor("_EmissionColor", config.StartColor * 1.4f);
            renderer.sharedMaterial = vfxMat;
            renderer.renderMode = config.StretchByVelocity
                ? ParticleSystemRenderMode.Stretch
                : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = config.StretchByVelocity ? 0.2f : 0f;
            renderer.lengthScale = config.StretchByVelocity ? 2.4f : 1f;
            renderer.normalDirection = 0f;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.trailMaterial = config.Trails ? vfxMat : null;

            ps.Play();

            return ps;
        }

        /// <summary>
        /// Spawns a particle effect at the target's current position, parented to the target so it moves with it.
        /// </summary>
        public static ParticleSystem CreateAttached(VFXType type, Transform target)
        {
            return Create(type, target.position, target);
        }

        private static Texture2D GetParticleTexture(VFXParticleStyle style)
        {
            if (ParticleTextures.TryGetValue(style, out var texture) && texture != null)
                return texture;

            if (ParticleTexturePaths.TryGetValue(style, out string resourcePath))
                texture = Resources.Load<Texture2D>(resourcePath);

            if (texture == null)
                texture = CreateParticleTexture(style, 64);

            ParticleTextures[style] = texture;
            return texture;
        }

        private static void ConfigureParticleMaterial(Material material)
        {
            if (material == null) return;

            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static Texture2D CreateParticleTexture(VFXParticleStyle style, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"VFX_{style}_Sprite",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[size * size];
            float half = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - half) / half;
                    float ny = (y - half) / half;
                    float alpha = EvaluateSpriteAlpha(style, nx, ny);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float EvaluateSpriteAlpha(VFXParticleStyle style, float x, float y)
        {
            switch (style)
            {
                case VFXParticleStyle.Spark:
                    return SparkAlpha(x, y);
                case VFXParticleStyle.Spore:
                    return SporeAlpha(x, y);
                case VFXParticleStyle.Droplet:
                    return DropletAlpha(x, y);
                case VFXParticleStyle.Slash:
                    return SlashAlpha(x, y);
                case VFXParticleStyle.Heart:
                    return HeartAlpha(x, y);
                default:
                    return SoftBlobAlpha(x, y);
            }
        }

        private static float SoftBlobAlpha(float x, float y)
        {
            float angle = Mathf.Atan2(y, x);
            float radius = Mathf.Sqrt(x * x + y * y);
            float edge = 0.78f + 0.06f * Mathf.Sin(angle * 5f) + 0.04f * Mathf.Sin(angle * 9f + 1.7f);
            return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(edge * 0.42f, edge, radius));
        }

        private static float SporeAlpha(float x, float y)
        {
            float radius = Mathf.Sqrt(x * x + y * y);
            float shell = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.46f, 0.82f, radius));
            float nucleus = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.05f, 0.22f, Vector2.Distance(new Vector2(x, y), new Vector2(-0.18f, 0.16f))));
            float speckle = 0.72f + 0.18f * Mathf.Sin((x * 21f) + (y * 13f));
            return Mathf.Clamp01(shell * speckle + nucleus * 0.35f);
        }

        private static float SparkAlpha(float x, float y)
        {
            float core = Mathf.Exp(-(x * x * 18f + y * y * 18f));
            float cross = Mathf.Exp(-Mathf.Abs(x) * 10f) * Mathf.Exp(-Mathf.Abs(y) * 1.8f);
            float vertical = Mathf.Exp(-Mathf.Abs(y) * 10f) * Mathf.Exp(-Mathf.Abs(x) * 1.8f);
            return Mathf.Clamp01(core + cross * 0.75f + vertical * 0.75f);
        }

        private static float DropletAlpha(float x, float y)
        {
            float stretchedY = y + 0.18f;
            float body = Mathf.Sqrt((x * x * 1.7f) + (stretchedY * stretchedY * 0.9f));
            float tip = Mathf.SmoothStep(0.35f, 0f, Mathf.Abs(x)) * Mathf.SmoothStep(0.7f, 0.15f, y);
            return Mathf.Clamp01(Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.36f, 0.78f, body)) + tip * 0.35f);
        }

        private static float SlashAlpha(float x, float y)
        {
            float diagonal = Mathf.Abs((y * 0.72f) - (x * 0.28f));
            float length = Mathf.Abs((x * 0.72f) + (y * 0.28f));
            float blade = Mathf.SmoothStep(0.16f, 0.01f, diagonal) * Mathf.SmoothStep(0.94f, 0.12f, length);
            float core = Mathf.SmoothStep(0.04f, 0.005f, diagonal) * Mathf.SmoothStep(0.65f, 0.08f, length);
            return Mathf.Clamp01(blade * 0.58f + core);
        }

        private static float HeartAlpha(float x, float y)
        {
            float sx = x * 1.2f;
            float sy = y * 1.2f - 0.08f;
            float a = sx * sx + sy * sy - 0.32f;
            float heart = a * a * a - sx * sx * sy * sy * sy;
            float edge = Mathf.SmoothStep(0.02f, -0.02f, heart);
            float glow = SoftBlobAlpha(x, y) * 0.25f;
            return Mathf.Clamp01(edge + glow);
        }
    }
}
