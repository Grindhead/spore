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
                  Loop = false, GravityModifier = 0.3f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.3f }},

            { VFXType.DamageHit, new VFXConfig
                { ParticleCount = 15, Lifetime = 0.6f, StartSpeed = 4f, StartSize = 0.2f,
                  StartColor = new Color(1f, 0.2f, 0.1f), EndColor = new Color(1f, 0f, 0f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.2f }},

            { VFXType.HealGlow, new VFXConfig
                { ParticleCount = 20, Lifetime = 1.5f, StartSpeed = 1f, StartSize = 0.3f,
                  StartColor = new Color(0.3f, 1f, 0.4f, 0.8f), EndColor = new Color(0.1f, 0.8f, 0.2f, 0f),
                  Loop = true, GravityModifier = -0.5f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.5f }},

            { VFXType.SpeedTrail, new VFXConfig
                { ParticleCount = 10, Lifetime = 0.8f, StartSpeed = 0.5f, StartSize = 0.15f,
                  StartColor = new Color(0.4f, 0.8f, 1f, 0.6f), EndColor = new Color(0.2f, 0.4f, 0.8f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.1f }},

            { VFXType.SpawnBurst, new VFXConfig
                { ParticleCount = 30, Lifetime = 1f, StartSpeed = 3f, StartSize = 0.4f,
                  StartColor = new Color(1f, 1f, 0.6f), EndColor = new Color(1f, 0.8f, 0.2f, 0f),
                  Loop = false, GravityModifier = 0.1f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.5f }},

            { VFXType.DeathPoof, new VFXConfig
                { ParticleCount = 25, Lifetime = 1.5f, StartSpeed = 2f, StartSize = 0.5f,
                  StartColor = new Color(0.5f, 0.5f, 0.5f, 0.8f), EndColor = new Color(0.3f, 0.3f, 0.3f, 0f),
                  Loop = false, GravityModifier = -0.2f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.4f }},

            { VFXType.FoodEat, new VFXConfig
                { ParticleCount = 12, Lifetime = 0.5f, StartSpeed = 2f, StartSize = 0.15f,
                  StartColor = new Color(0.2f, 0.9f, 0.3f), EndColor = new Color(0.1f, 0.6f, 0.1f, 0f),
                  Loop = false, GravityModifier = 0.1f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.2f }},

            { VFXType.DNAPickup, new VFXConfig
                { ParticleCount = 20, Lifetime = 1f, StartSpeed = 1.5f, StartSize = 0.2f,
                  StartColor = new Color(0.9f, 0.6f, 1f), EndColor = new Color(0.6f, 0.2f, 1f, 0f),
                  Loop = false, GravityModifier = -0.8f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.3f }},

            { VFXType.StageTransition, new VFXConfig
                { ParticleCount = 60, Lifetime = 3f, StartSpeed = 5f, StartSize = 0.6f,
                  StartColor = new Color(1f, 1f, 1f, 0.9f), EndColor = new Color(0.4f, 0.8f, 1f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 2f }},

            { VFXType.ShieldHit, new VFXConfig
                { ParticleCount = 18, Lifetime = 0.8f, StartSpeed = 3f, StartSize = 0.25f,
                  StartColor = new Color(0.3f, 0.6f, 1f, 0.9f), EndColor = new Color(0.1f, 0.3f, 0.8f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Hemisphere, ShapeRadius = 0.5f }},

            { VFXType.TerraformPulse, new VFXConfig
                { ParticleCount = 35, Lifetime = 2f, StartSpeed = 2f, StartSize = 2f,
                  StartColor = new Color(0.2f, 1f, 0.5f, 0.6f), EndColor = new Color(0.1f, 0.8f, 0.3f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 3f }},

            { VFXType.BuildingComplete, new VFXConfig
                { ParticleCount = 25, Lifetime = 1.2f, StartSpeed = 3f, StartSize = 0.3f,
                  StartColor = new Color(1f, 0.9f, 0.5f), EndColor = new Color(0.8f, 0.6f, 0.2f, 0f),
                  Loop = false, GravityModifier = 0.5f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.5f }},

            { VFXType.DiplomacyHeart, new VFXConfig
                { ParticleCount = 8, Lifetime = 1.5f, StartSpeed = 1f, StartSize = 0.3f,
                  StartColor = new Color(1f, 0.3f, 0.5f, 0.9f), EndColor = new Color(1f, 0.1f, 0.3f, 0f),
                  Loop = false, GravityModifier = -0.5f, Shape = ParticleSystemShapeType.Sphere, ShapeRadius = 0.3f }},

            { VFXType.CombatSlash, new VFXConfig
                { ParticleCount = 10, Lifetime = 0.4f, StartSpeed = 6f, StartSize = 0.15f,
                  StartColor = Color.white, EndColor = new Color(1f, 1f, 1f, 0f),
                  Loop = false, GravityModifier = 0f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.1f }},

            { VFXType.WaterSplash, new VFXConfig
                { ParticleCount = 20, Lifetime = 1f, StartSpeed = 4f, StartSize = 0.2f,
                  StartColor = new Color(0.4f, 0.7f, 1f, 0.8f), EndColor = new Color(0.2f, 0.5f, 0.9f, 0f),
                  Loop = false, GravityModifier = 1f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.3f }},

            { VFXType.FireEmbers, new VFXConfig
                { ParticleCount = 15, Lifetime = 2f, StartSpeed = 1.5f, StartSize = 0.1f,
                  StartColor = new Color(1f, 0.5f, 0.1f), EndColor = new Color(0.8f, 0.2f, 0f, 0f),
                  Loop = true, GravityModifier = -0.3f, Shape = ParticleSystemShapeType.Cone, ShapeRadius = 0.2f }},
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

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var vfxMat = new Material(Shader.Find("Particles/Standard Unlit"));
            vfxMat.color = config.StartColor;
            renderer.sharedMaterial = vfxMat;

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
    }
}
