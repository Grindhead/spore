using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for ScavengerCell: constants, BuildGenome(), trophic classification, and
    /// FoodTracker type-filtered lookup. Implements cellStageOrganismDesign §5 (food web).
    /// </summary>
    public class ScavengerCellTests
    {
        // ---- Constants ----

        [Test]
        public void MeatSeekRadius_IsLargerThanConsumeRadius()
        {
            Assert.Greater(ScavengerCell.MeatSeekRadius, ScavengerCell.ConsumeRadius,
                "Scavengers must detect meat further than they need to be to eat it");
        }

        [Test]
        public void ConsumeRadius_IsPositiveAndSmall()
        {
            Assert.Greater(ScavengerCell.ConsumeRadius, 0f);
            Assert.Less(ScavengerCell.ConsumeRadius, 5f, "Consume radius should be small — scavenger must reach the food");
        }

        [Test]
        public void ChaseSpeedFraction_IsOne()
        {
            Assert.AreEqual(1.0f, ScavengerCell.ChaseSpeedFraction,
                "Scavengers should sprint at full genome speed when chasing meat");
        }

        [Test]
        public void WanderSpeedFraction_IsLessThanChaseSpeed()
        {
            Assert.Less(ScavengerCell.WanderSpeedFraction, ScavengerCell.ChaseSpeedFraction);
            Assert.Greater(ScavengerCell.WanderSpeedFraction, 0f);
        }

        [Test]
        public void SeekTickInterval_IsPositive()
        {
            Assert.Greater(ScavengerCell.SeekTickInterval, 0f);
        }

        // ---- Genome ----

        [Test]
        public void BuildGenome_HasHighDigestion()
        {
            var g = ScavengerCell.BuildGenome();
            Assert.GreaterOrEqual(g.Digestion, 5f, "Scavengers need high Digestion to extract energy from carrion");
        }

        [Test]
        public void BuildGenome_HasHighSpeed()
        {
            var g = ScavengerCell.BuildGenome();
            Assert.GreaterOrEqual(g.Speed, 3f, "Scavengers must be fast enough to reach dead cells before competitors");
        }

        [Test]
        public void BuildGenome_HasLowAttack()
        {
            var g = ScavengerCell.BuildGenome();
            Assert.Less(g.Attack, 3f, "Scavengers are not predators — low Attack");
        }

        [Test]
        public void BuildGenome_HasSomeSpikes()
        {
            var g = ScavengerCell.BuildGenome();
            Assert.GreaterOrEqual(g.Spikes, 0f); // may be 0 or small — just must not be negative
        }

        [Test]
        public void BuildGenome_ClassifiesAsHerbivore()
        {
            // High Digestion, low Attack → Digestion dominates → Herbivore
            var g = ScavengerCell.BuildGenome();
            Assert.AreEqual(CellStage.TrophicRole.Herbivore, CellStage.ClassifyTrophicRole(g),
                "Scavenger genome (high Digestion, low Attack) must classify as Herbivore for niche-fill compatibility");
        }

        // ---- Colour ----

        [Test]
        public void ArchetypeColor_IsDistinctFromFilterFeeder()
        {
            Assert.AreNotEqual(FilterFeederCell.ArchetypeColor, ScavengerCell.ArchetypeColor);
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromPredatorColor()
        {
            var predatorColor = new Color(0.5f, 0f, 0.5f);
            Assert.AreNotEqual(predatorColor, ScavengerCell.ArchetypeColor);
        }

        // ---- FoodTracker.GetNearestOfType ----

        [Test]
        public void FoodTracker_GetNearestOfType_ReturnsNullWhenEmpty()
        {
            var tracker = new FoodTracker();
            var result = tracker.GetNearestOfType(Vector3.zero, 100f, FoodType.Meat);
            Assert.IsNull(result);
        }

        [Test]
        public void FoodTracker_GetNearestOfType_IgnoresWrongType()
        {
            var tracker = new FoodTracker();
            tracker.Add(new Vector3(1f, 0f, 0f), FoodType.Plant, 5f);
            var result = tracker.GetNearestOfType(Vector3.zero, 100f, FoodType.Meat);
            Assert.IsNull(result, "Plant food must not be returned when querying for Meat");
        }

        [Test]
        public void FoodTracker_GetNearestOfType_ReturnsCorrectType()
        {
            var tracker = new FoodTracker();
            tracker.Add(new Vector3(5f, 0f, 0f), FoodType.Plant, 5f);
            tracker.Add(new Vector3(3f, 0f, 0f), FoodType.Meat, 10f);
            var result = tracker.GetNearestOfType(Vector3.zero, 100f, FoodType.Meat);
            Assert.IsNotNull(result);
            Assert.AreEqual(FoodType.Meat, result.Value.Type);
        }

        [Test]
        public void FoodTracker_GetNearestOfType_ReturnsClosest()
        {
            var tracker = new FoodTracker();
            tracker.Add(new Vector3(10f, 0f, 0f), FoodType.Meat, 5f);
            int closeId = tracker.Add(new Vector3(2f, 0f, 0f), FoodType.Meat, 5f);
            var result = tracker.GetNearestOfType(Vector3.zero, 100f, FoodType.Meat);
            Assert.IsNotNull(result);
            Assert.AreEqual(closeId, result.Value.Id, "Must return the closest Meat item, not the first registered");
        }

        [Test]
        public void FoodTracker_GetNearestOfType_RespectsMaxDistance()
        {
            var tracker = new FoodTracker();
            tracker.Add(new Vector3(50f, 0f, 0f), FoodType.Meat, 5f);
            var result = tracker.GetNearestOfType(Vector3.zero, 10f, FoodType.Meat);
            Assert.IsNull(result, "Food outside maxDistance must not be returned");
        }

        [Test]
        public void FoodTracker_GetNearestOfType_FindsPlantWhenQueried()
        {
            var tracker = new FoodTracker();
            tracker.Add(new Vector3(3f, 0f, 0f), FoodType.Plant, 5f);
            var result = tracker.GetNearestOfType(Vector3.zero, 100f, FoodType.Plant);
            Assert.IsNotNull(result);
            Assert.AreEqual(FoodType.Plant, result.Value.Type);
        }
    }
}
