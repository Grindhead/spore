using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class GameBalanceModifiersTests
    {
        [Test]
        public void Default_AllMultipliersAreOne()
        {
            var m = GameBalanceModifiers.Default;
            Assert.AreEqual(1f, m.FoodSpawnMultiplier,    0.001f);
            Assert.AreEqual(1f, m.PredatorMultiplier,     0.001f);
            Assert.AreEqual(1f, m.NPCCountMultiplier,     0.001f);
            Assert.AreEqual(1f, m.MetabolismMultiplier,   0.001f);
            Assert.AreEqual(1f, m.EvolutionThresholdMult, 0.001f);
            Assert.AreEqual(1f, m.DNARewardMultiplier,    0.001f);
        }

        [Test]
        public void Default_AppliesBaseCountsUnchanged()
        {
            var m = GameBalanceModifiers.Default;
            Assert.AreEqual(100, m.ApplyFoodCount(100));
            Assert.AreEqual(3, m.ApplyPredatorCount(3));
            Assert.AreEqual(8, m.ApplyNPCCount(8));
            Assert.AreEqual(10, m.ApplyDNAReward(10));
        }

        [Test]
        public void CountHelpers_ClampToPlayableMinimums()
        {
            var m = GameBalanceModifiers.Default;
            Assert.AreEqual(1, m.ApplyFoodCount(0));
            Assert.AreEqual(0, m.ApplyPredatorCount(0));
            Assert.AreEqual(1, m.ApplyNPCCount(0));
            Assert.AreEqual(1, m.ApplyDNAReward(0));
        }
    }
}
