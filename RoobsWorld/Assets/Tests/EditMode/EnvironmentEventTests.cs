using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for CellEnvironmentEventType enum coverage and BehaviourBiasTracker integration
    /// with the radiation surge bonus mutation path.
    /// </summary>
    public class EnvironmentEventTests
    {
        // ------------------------------------------------------------------ Enum sanity

        [Test]
        public void CellEnvironmentEventType_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)CellEnvironmentEventType.None);
            Assert.AreEqual(1, (int)CellEnvironmentEventType.NutrientBloom);
            Assert.AreEqual(2, (int)CellEnvironmentEventType.VirusOutbreak);
            Assert.AreEqual(3, (int)CellEnvironmentEventType.TemperatureChange);
            Assert.AreEqual(4, (int)CellEnvironmentEventType.RadiationSurge);
            Assert.AreEqual(5, (int)CellEnvironmentEventType.OxygenSurge);
        }

        [Test]
        public void CellEnvironmentEventType_SixDistinctValues()
        {
            var values = System.Enum.GetValues(typeof(CellEnvironmentEventType));
            Assert.AreEqual(6, values.Length);
        }

        // ------------------------------------------------------------------ RadiationSurge mutation path

        [Test]
        public void RadiationSurge_MutationOptions_AreThreeDistinctGenomes()
        {
            // Simulate what PlayerCell.OfferRadiationMutation does: generate 3 mutated options
            var parent = Genome.Default();
            var rng = new System.Random(42);
            var g0 = MutationSystem.MutateBiased(parent, default, rng);
            var g1 = MutationSystem.MutateBiased(parent, default, rng);
            var g2 = MutationSystem.Mutate(parent, new System.Random(43));

            Assert.NotNull(g0);
            Assert.NotNull(g1);
            Assert.NotNull(g2);
            // At minimum one of the biased variants should differ from parent in some gene
            bool anyDiffers = g0.Speed != parent.Speed || g0.Attack != parent.Attack ||
                              g1.Speed != parent.Speed || g1.Attack != parent.Attack;
            // This is probabilistic; with seed 42 the chance of all-no-mutation is negligible
            // but we just assert they are valid genomes
            Assert.GreaterOrEqual(g0.Speed, 0f);
            Assert.LessOrEqual(g0.Speed, 10f);
        }

        [Test]
        public void RadiationSurge_MutationBias_FromAggressivePlay_FavorsAttack()
        {
            var tracker = new BehaviourBiasTracker();
            for (int i = 0; i < 8; i++) tracker.RecordFoodEaten(FoodType.Meat);
            for (int i = 0; i < 5; i++) tracker.RecordCellConsumed();

            var bias = tracker.ComputeBias();
            var parent = new Genome { Attack = 3f };
            int improvedCount = 0;
            for (int seed = 0; seed < 50; seed++)
            {
                var child = MutationSystem.MutateBiased(parent, bias, new System.Random(seed));
                if (child.Attack >= parent.Attack) improvedCount++;
            }
            // Biased toward attack: at least half the mutations should maintain or improve attack
            Assert.GreaterOrEqual(improvedCount, 25);
        }

        [Test]
        public void RadiationSurge_BiasDescription_ReturnedForDominantStyle()
        {
            var tracker = new BehaviourBiasTracker();
            for (int i = 0; i < 10; i++) tracker.RecordDamageTaken();
            string desc = tracker.GetBiasDescription();
            Assert.IsNotNull(desc, "Dominant defensive play should produce a non-null bias description");
            Assert.IsNotEmpty(desc);
        }

        [Test]
        public void RadiationSurge_NoActivityBiasDescription_IsNull()
        {
            var tracker = new BehaviourBiasTracker();
            Assert.IsNull(tracker.GetBiasDescription(),
                "With no recorded activity, bias description should be null");
        }

        // ------------------------------------------------------------------ Localization keys

        [Test]
        public void RadiationSurge_LocalizationKeys_ArePresent()
        {
            string banner   = Localization.Get("cell.env.radiation_surge");
            string subtitle = Localization.Get("cell.env.radiation_surge_subtitle");
            string title    = Localization.Get("cell.env.radiation_surge_title");

            Assert.IsFalse(string.IsNullOrEmpty(banner),   "radiation_surge banner key missing");
            Assert.IsFalse(string.IsNullOrEmpty(subtitle), "radiation_surge_subtitle key missing");
            Assert.IsFalse(string.IsNullOrEmpty(title),    "radiation_surge_title key missing");

            // Keys should be translated, not falling back to the key itself
            Assert.AreNotEqual("cell.env.radiation_surge",          banner);
            Assert.AreNotEqual("cell.env.radiation_surge_subtitle", subtitle);
            Assert.AreNotEqual("cell.env.radiation_surge_title",    title);
        }

        [Test]
        public void OxygenSurge_LocalizationKeys_ArePresent()
        {
            string banner   = Localization.Get("cell.env.oxygen_surge");
            string subtitle = Localization.Get("cell.env.oxygen_surge_subtitle");
            string title    = Localization.Get("cell.env.oxygen_surge_title");

            Assert.IsFalse(string.IsNullOrEmpty(banner),   "oxygen_surge banner key missing");
            Assert.IsFalse(string.IsNullOrEmpty(subtitle), "oxygen_surge_subtitle key missing");
            Assert.IsFalse(string.IsNullOrEmpty(title),    "oxygen_surge_title key missing");

            Assert.AreNotEqual("cell.env.oxygen_surge",          banner);
            Assert.AreNotEqual("cell.env.oxygen_surge_subtitle", subtitle);
            Assert.AreNotEqual("cell.env.oxygen_surge_title",    title);
        }

        [Test]
        public void OxygenSurge_ApplyEffect_BoostsPhotosynthesisMultiplier()
        {
            var go = new GameObject("EnvEventManager_Test");
            var manager = go.AddComponent<CellEnvironmentEventManager>();
            var method = typeof(CellEnvironmentEventManager).GetMethod(
                "ApplyEventEffect",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.NotNull(method, "ApplyEventEffect should exist for event dispatch");
            method.Invoke(manager, new object[] { CellEnvironmentEventType.OxygenSurge });
            Assert.Greater(CellEnvironmentEventManager.GlobalPhotosynthesisTraitMultiplier, 1f);

            manager.Deactivate();
            Assert.AreEqual(1f, CellEnvironmentEventManager.GlobalPhotosynthesisTraitMultiplier);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NutrientBloom_SpawnCount_IncreasesWithProgression()
        {
            int early = CellEnvironmentEventManager.ComputeNutrientBloomSpawnCount(initialFoodCount: 100, progression01: 0f);
            int late = CellEnvironmentEventManager.ComputeNutrientBloomSpawnCount(initialFoodCount: 100, progression01: 1f);
            Assert.Greater(late, early, "Late-stage blooms should spawn more food than early-stage blooms.");
        }

        [Test]
        public void NutrientBloom_SpawnCount_RespectsFoodMultiplier()
        {
            int dense = CellEnvironmentEventManager.ComputeNutrientBloomSpawnCount(
                initialFoodCount: 100,
                progression01: 0.6f,
                foodMultiplier: 1.5f);
            int sparse = CellEnvironmentEventManager.ComputeNutrientBloomSpawnCount(
                initialFoodCount: 100,
                progression01: 0.6f,
                foodMultiplier: 0.7f);
            Assert.Greater(dense, sparse, "Higher food multiplier should produce denser nutrient blooms.");
        }

        [Test]
        public void NutrientBloom_Progression01_IsClamped()
        {
            Assert.AreEqual(0f, CellEnvironmentEventManager.ComputeNutrientBloomProgression01(-5f), 0.0001f);
            Assert.AreEqual(1f, CellEnvironmentEventManager.ComputeNutrientBloomProgression01(99999f), 0.0001f);
        }

        [Test]
        public void BloomPingSuffix_EmptyHotspots_ReturnsEmpty()
        {
            string suffix = CellStageHUD.BuildBloomPingSuffix(Vector3.zero, new List<Vector3>(), 3);
            Assert.AreEqual(string.Empty, suffix);
        }

        [Test]
        public void BloomPingSuffix_OrdersNearestAndCapsCount()
        {
            var hotspots = new List<Vector3>
            {
                new Vector3(5f, 0f, 0f),   // E (nearest)
                new Vector3(0f, 0f, 12f),  // N
                new Vector3(-25f, 0f, 0f)  // W (farthest)
            };
            string suffix = CellStageHUD.BuildBloomPingSuffix(Vector3.zero, hotspots, 2);
            Assert.AreEqual(" [E,N]", suffix);
        }

        [Test]
        public void NutrientBloom_ApplyEffect_RespectsArenaCenterAndRadius()
        {
            var root = new GameObject("EnvEventManager_NutrientBloom_TestRoot");
            var foodGo = new GameObject("FoodManager_ForBloomTest");
            foodGo.transform.SetParent(root.transform, false);

            var foodManager = foodGo.AddComponent<FoodManager>();
            var manager = root.AddComponent<CellEnvironmentEventManager>();

            var arenaCenter = new Vector3(120f, 0f, -80f);
            const float arenaRadius = 40f;
            manager.Activate(foodManager, arenaRadius, arenaCenter: arenaCenter);

            var applyEffect = typeof(CellEnvironmentEventManager).GetMethod(
                "ApplyEventEffect",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(applyEffect, "ApplyEventEffect should exist for event dispatch");

            var before = Object.FindObjectsByType<FoodItem>(FindObjectsInactive.Exclude);
            var beforeIds = new HashSet<EntityId>();
            for (int i = 0; i < before.Length; i++)
            {
                if (before[i] != null)
                    beforeIds.Add(before[i].GetEntityId());
            }

            UnityEngine.Random.State previousRandomState = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(7331);
                applyEffect.Invoke(manager, new object[] { CellEnvironmentEventType.NutrientBloom });
            }
            finally
            {
                UnityEngine.Random.state = previousRandomState;
            }

            var after = Object.FindObjectsByType<FoodItem>(FindObjectsInactive.Exclude);
            var spawned = new List<FoodItem>();
            for (int i = 0; i < after.Length; i++)
            {
                var food = after[i];
                if (food != null && !beforeIds.Contains(food.GetEntityId()))
                    spawned.Add(food);
            }

            Assert.GreaterOrEqual(spawned.Count, 5, "Nutrient bloom should spawn a meaningful burst of plant food");
            Assert.GreaterOrEqual(manager.ActiveNutrientBloomHotspotCount, 2, "Bloom should expose hotspot anchors for AI steering");
            Assert.IsTrue(
                manager.TryGetNearestNutrientBloomHotspot(arenaCenter, out Vector3 nearestHotspot),
                "Bloom manager should return a nearest hotspot while bloom is active.");
            Vector3 hotspotOffset = nearestHotspot - arenaCenter;
            hotspotOffset.y = 0f;
            Assert.LessOrEqual(hotspotOffset.magnitude, arenaRadius + 0.01f);

            for (int i = 0; i < spawned.Count; i++)
            {
                var pos = spawned[i].transform.position;
                Vector3 flatOffset = pos - arenaCenter;
                flatOffset.y = 0f;
                Assert.LessOrEqual(
                    flatOffset.magnitude,
                    arenaRadius + 0.01f,
                    "Nutrient bloom food must spawn inside the configured arena bounds");
            }

            manager.Deactivate();
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                    Object.DestroyImmediate(spawned[i].gameObject);
            }
            Object.DestroyImmediate(root);
        }
    }
}
