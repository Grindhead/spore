using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests.EditMode
{
    public class CellPartPickupPlannerTests
    {
        [Test]
        public void BuildPlan_ZeroCount_ReturnsEmpty()
        {
            var plan = CellPartPickupPlanner.BuildPlan(0, new System.Random(7));
            Assert.AreEqual(0, plan.Count);
        }

        [Test]
        public void BuildPlan_CountAtLeastPartTypes_ContainsAllPartTypes()
        {
            int partTypeCount = System.Enum.GetValues(typeof(CellPartType)).Length;
            var plan = CellPartPickupPlanner.BuildPlan(partTypeCount, new System.Random(7));
            var set = new HashSet<CellPartType>(plan);

            Assert.AreEqual(partTypeCount, plan.Count);
            Assert.AreEqual(partTypeCount, set.Count);
            foreach (CellPartType partType in System.Enum.GetValues(typeof(CellPartType)))
                Assert.IsTrue(set.Contains(partType), $"Missing {partType}");
        }

        [Test]
        public void BuildPlan_CountBelowPartTypes_ContainsUniqueSubset()
        {
            var plan = CellPartPickupPlanner.BuildPlan(3, new System.Random(9));
            var set = new HashSet<CellPartType>(plan);

            Assert.AreEqual(3, plan.Count);
            Assert.AreEqual(3, set.Count);
        }

        [Test]
        public void BuildPlan_CountAbovePartTypes_ReturnsRequestedCountAndCoverage()
        {
            int partTypeCount = System.Enum.GetValues(typeof(CellPartType)).Length;
            var plan = CellPartPickupPlanner.BuildPlan(partTypeCount + 4, new System.Random(11));
            var set = new HashSet<CellPartType>(plan);

            Assert.AreEqual(partTypeCount + 4, plan.Count);
            Assert.AreEqual(partTypeCount, set.Count);
        }

        [Test]
        public void GetStrategicSpawnBand_PutsHighImpactPartsInRiskierOuterBands()
        {
            var cilia = CellPartPickupPlanner.GetStrategicSpawnBand(CellPartType.Cilia);
            var jet = CellPartPickupPlanner.GetStrategicSpawnBand(CellPartType.Jet);

            Assert.Less(cilia.MaxRadiusFraction, jet.MaxRadiusFraction);
            Assert.IsFalse(cilia.AllowToxicZone);
            Assert.IsTrue(jet.AllowToxicZone);
        }

        [Test]
        public void GetPreviewText_ExplainsGameplayRole()
        {
            string spike = CellPartPickupPlanner.GetPreviewText(CellPartType.Spike);
            string jet = CellPartPickupPlanner.GetPreviewText(CellPartType.Jet);

            Assert.That(spike, Does.Contain("contact"));
            Assert.That(jet, Does.Contain("Burst"));
        }

        [Test]
        public void SelectGoalPartType_PrefersLockedUtilityParts()
        {
            var loadout = new CellPartLoadout();

            Assert.AreEqual(CellPartType.Cilia, CellPartPickupPlanner.SelectGoalPartType(loadout));

            loadout.TryUnlock(CellPartType.Cilia);
            Assert.AreEqual(CellPartType.HerbivoreMouth, CellPartPickupPlanner.SelectGoalPartType(loadout));

            loadout.TryUnlock(CellPartType.HerbivoreMouth);
            Assert.AreEqual(CellPartType.CarnivoreMouth, CellPartPickupPlanner.SelectGoalPartType(loadout));
        }

        [Test]
        public void SelectGoalPartType_FallsBackToCilia_WhenAllPartsUnlocked()
        {
            var loadout = new CellPartLoadout();
            foreach (CellPartType partType in System.Enum.GetValues(typeof(CellPartType)))
                loadout.TryUnlock(partType);

            Assert.AreEqual(CellPartType.Cilia, CellPartPickupPlanner.SelectGoalPartType(loadout));
        }
    }
}
