using NUnit.Framework;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Verifies archetype-to-biome spawn band mapping for initial cell-stage ecosystem composition.
    /// </summary>
    public class OrganismSpawnBandTests
    {
        [Test]
        public void FilterFeeder_Band_IsMidInner_AndNotForcedAlgae()
        {
            CellStage.GetArchetypeSpawnBand(0, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.18f, min, 0.0001f);
            Assert.AreEqual(0.62f, max, 0.0001f);
            Assert.IsFalse(preferAlgae);
        }

        [Test]
        public void Bacteria_Band_IsInner()
        {
            CellStage.GetArchetypeSpawnBand(1, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.12f, min, 0.0001f);
            Assert.AreEqual(0.42f, max, 0.0001f);
            Assert.IsFalse(preferAlgae);
        }

        [Test]
        public void Scavenger_Band_IsOuter()
        {
            CellStage.GetArchetypeSpawnBand(3, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.45f, min, 0.0001f);
            Assert.AreEqual(0.88f, max, 0.0001f);
            Assert.IsFalse(preferAlgae);
        }

        [Test]
        public void Parasite_Band_IsMid()
        {
            CellStage.GetArchetypeSpawnBand(5, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.30f, min, 0.0001f);
            Assert.AreEqual(0.72f, max, 0.0001f);
            Assert.IsFalse(preferAlgae);
        }

        [Test]
        public void Photosynthetic_Band_IsOuterAndPrefersAlgae()
        {
            CellStage.GetArchetypeSpawnBand(7, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.66f, min, 0.0001f);
            Assert.AreEqual(0.89f, max, 0.0001f);
            Assert.IsTrue(preferAlgae);
        }

        [Test]
        public void Colonial_Band_IsMidOuter_AndNotForcedAlgae()
        {
            CellStage.GetArchetypeSpawnBand(9, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.38f, min, 0.0001f);
            Assert.AreEqual(0.84f, max, 0.0001f);
            Assert.IsFalse(preferAlgae);
        }

        [Test]
        public void Default_Band_IsBroadAndNotForcedAlgae()
        {
            CellStage.GetArchetypeSpawnBand(2, out float min, out float max, out bool preferAlgae);
            Assert.AreEqual(0.08f, min, 0.0001f);
            Assert.AreEqual(0.92f, max, 0.0001f);
            Assert.IsFalse(preferAlgae);
        }

        [Test]
        public void AllBands_AreOrderedAndWithinUnitInterval()
        {
            for (int i = 0; i < 10; i++)
            {
                CellStage.GetArchetypeSpawnBand(i, out float min, out float max, out _);
                Assert.GreaterOrEqual(min, 0f, $"min for {i} must be >= 0");
                Assert.LessOrEqual(max, 1f, $"max for {i} must be <= 1");
                Assert.Less(min, max, $"min must be < max for {i}");
            }
        }
    }
}
