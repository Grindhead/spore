using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class BiomeTests
    {
        private const float ArenaRadius = 100f;
        private static readonly Vector3 ArenaCenter = Vector3.zero;

        [Test]
        public void GetBiomeAt_AtCenter_ReturnsDeepZone()
        {
            var biome = Biome.GetBiomeAt(ArenaCenter, ArenaCenter, ArenaRadius);
            Assert.AreEqual(Biome.BiomeType.DeepZone, biome);
        }

        [Test]
        public void GetBiomeAt_InsideDeepBand_ReturnsDeepZone()
        {
            float r = ArenaRadius * 0.1f; // 10% of R, inside 20% deep band
            var pos = new Vector3(r, 0f, 0f);
            Assert.AreEqual(Biome.BiomeType.DeepZone, Biome.GetBiomeAt(ArenaCenter, pos, ArenaRadius));
        }

        [Test]
        public void GetBiomeAt_InsideOpenWater_ReturnsOpenWater()
        {
            float r = ArenaRadius * 0.4f; // 40%, between 20% and 60%
            var pos = new Vector3(r, 0f, 0f);
            Assert.AreEqual(Biome.BiomeType.OpenWater, Biome.GetBiomeAt(ArenaCenter, pos, ArenaRadius));
        }

        [Test]
        public void GetBiomeAt_InsideAlgaeForest_ReturnsAlgaeForest()
        {
            float r = ArenaRadius * 0.75f; // 75%, between 60% and 90%
            var pos = new Vector3(r, 0f, 0f);
            Assert.AreEqual(Biome.BiomeType.AlgaeForest, Biome.GetBiomeAt(ArenaCenter, pos, ArenaRadius));
        }

        [Test]
        public void GetBiomeAt_OutsideAlgaeForest_ReturnsOpenWater()
        {
            float r = ArenaRadius * 0.95f; // 95%, beyond 90%
            var pos = new Vector3(r, 0f, 0f);
            Assert.AreEqual(Biome.BiomeType.OpenWater, Biome.GetBiomeAt(ArenaCenter, pos, ArenaRadius));
        }

        [Test]
        public void GetBiomeAt_InsideToxicCircle_ReturnsToxicZone()
        {
            float offset = ArenaRadius * BalanceConstants.CellBiomeToxicZoneCenterOffsetFraction;
            var toxicCenter = ArenaCenter + new Vector3(offset, 0f, offset);
            var pos = toxicCenter; // center of toxic zone
            Assert.AreEqual(Biome.BiomeType.ToxicZone, Biome.GetBiomeAt(ArenaCenter, pos, ArenaRadius));
        }

        [Test]
        public void GetBiomeAt_ZeroRadius_ReturnsOpenWater()
        {
            Assert.AreEqual(Biome.BiomeType.OpenWater, Biome.GetBiomeAt(ArenaCenter, ArenaCenter, 0f));
        }

        [Test]
        public void GetMetabolismMultiplier_Deep_ReturnsLessThanOne()
        {
            Assert.Less(Biome.GetMetabolismMultiplier(Biome.BiomeType.DeepZone), 1f);
            Assert.AreEqual(BalanceConstants.CellBiomeMetabolismMultiplierDeep, Biome.GetMetabolismMultiplier(Biome.BiomeType.DeepZone), 0.0001f);
        }

        [Test]
        public void GetMetabolismMultiplier_OpenWater_ReturnsOne()
        {
            Assert.AreEqual(1f, Biome.GetMetabolismMultiplier(Biome.BiomeType.OpenWater), 0.0001f);
        }

        [Test]
        public void GetMetabolismMultiplier_Toxic_ReturnsGreaterThanOne()
        {
            Assert.Greater(Biome.GetMetabolismMultiplier(Biome.BiomeType.ToxicZone), 1f);
            Assert.AreEqual(BalanceConstants.CellBiomeMetabolismMultiplierToxic, Biome.GetMetabolismMultiplier(Biome.BiomeType.ToxicZone), 0.0001f);
        }

        [Test]
        public void GetSpeedMultiplier_Deep_ReturnsLessThanOne()
        {
            Assert.Less(Biome.GetSpeedMultiplier(Biome.BiomeType.DeepZone), 1f);
        }

        [Test]
        public void GetSpeedMultiplier_OpenWater_ReturnsOne()
        {
            Assert.AreEqual(1f, Biome.GetSpeedMultiplier(Biome.BiomeType.OpenWater), 0.0001f);
        }

        [Test]
        public void GetFoodEfficiencyMultiplier_Toxic_ReturnsLessThanOne()
        {
            Assert.Less(Biome.GetFoodEfficiencyMultiplier(Biome.BiomeType.ToxicZone), 1f);
            Assert.AreEqual(BalanceConstants.CellBiomeFoodEfficiencyMultiplierToxic, Biome.GetFoodEfficiencyMultiplier(Biome.BiomeType.ToxicZone), 0.0001f);
        }

        [Test]
        public void GetFoodEfficiencyMultiplier_OpenWater_ReturnsOne()
        {
            Assert.AreEqual(1f, Biome.GetFoodEfficiencyMultiplier(Biome.BiomeType.OpenWater), 0.0001f);
        }

        [Test]
        public void Organism_ApplyMetabolism_WithMultiplier_DrainsScaled()
        {
            var org = Organism.Create(Vector3.zero, new Genome { MetabolismCost = 10f });
            org.Eat(1000f); // full energy
            float eBefore = org.Energy;
            org.ApplyMetabolism(1f, 1f);
            float drainFull = eBefore - org.Energy;
            float eAfterFull = org.Energy;
            org.ApplyMetabolism(1f, 0.5f);
            float drainHalf = eAfterFull - org.Energy;
            Assert.AreEqual(drainFull * 0.5f, drainHalf, 0.01f, "Half multiplier should drain half as much per tick");
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Organism_Move_WithSpeedMultiplier_ScalesDistance()
        {
            var org = Organism.Create(Vector3.zero, new Genome { Speed = 1f });
            Vector3 dir = Vector3.forward;
            org.Move(dir, 1f, 1f);
            float distFull = org.transform.position.z;
            org.transform.position = Vector3.zero;
            org.Move(dir, 1f, 0.5f);
            float distHalf = org.transform.position.z;
            Assert.Greater(distFull, 0f);
            Assert.AreEqual(distFull * 0.5f, distHalf, 0.01f, "Half speed multiplier should move half distance");
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Organism_Eat_WithFoodEfficiencyMultiplier_ScalesGain()
        {
            var org = Organism.Create(Vector3.zero, new Genome { Digestion = 1f });
            float gainedFull = org.Eat(10f, 1f);
            var org2 = Organism.Create(Vector3.zero, new Genome { Digestion = 1f });
            float gainedHalf = org2.Eat(10f, 0.5f);
            Assert.AreEqual(gainedFull * 0.5f, gainedHalf, 0.01f);
            Assert.AreEqual(50f + 5f, org2.Energy, 0.01f); // InitialEnergy + 10*1*0.5
            Object.DestroyImmediate(org.gameObject);
            Object.DestroyImmediate(org2.gameObject);
        }
    }
}
