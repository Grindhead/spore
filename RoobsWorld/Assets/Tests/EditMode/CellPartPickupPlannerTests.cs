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
            var plan = CellPartPickupPlanner.BuildPlan(4, new System.Random(7));
            var set = new HashSet<CellPartType>(plan);

            Assert.AreEqual(4, plan.Count);
            Assert.IsTrue(set.Contains(CellPartType.Flagella));
            Assert.IsTrue(set.Contains(CellPartType.Cilia));
            Assert.IsTrue(set.Contains(CellPartType.Spike));
            Assert.IsTrue(set.Contains(CellPartType.Jet));
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
            var plan = CellPartPickupPlanner.BuildPlan(8, new System.Random(11));
            var set = new HashSet<CellPartType>(plan);

            Assert.AreEqual(8, plan.Count);
            Assert.AreEqual(4, set.Count);
        }
    }
}
