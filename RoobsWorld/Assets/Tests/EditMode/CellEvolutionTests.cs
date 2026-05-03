using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class CellEvolutionTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        [Test]
        public void Tracker_Does_Not_Fire_Below_Threshold()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 3);
            bool fired = false;
            tracker.OnEvolutionReached += _ => fired = true;

            tracker.UpdateMass(4.9f);

            Assert.IsFalse(fired);
            Assert.AreEqual(0, tracker.CurrentLevel);
        }

        [Test]
        public void Tracker_Fires_At_Threshold()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 3);
            int firedLevel = -1;
            tracker.OnEvolutionReached += level => firedLevel = level;

            tracker.UpdateMass(5f);

            Assert.AreEqual(1, firedLevel);
            Assert.AreEqual(1, tracker.CurrentLevel);
        }

        [Test]
        public void Tracker_Does_Not_Fire_Same_Level_Twice()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 3);
            int fireCount = 0;
            tracker.OnEvolutionReached += _ => fireCount++;

            tracker.UpdateMass(5f);
            tracker.UpdateMass(6f);

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void Tracker_Fires_At_Each_Threshold_Multiple()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 3);
            int fireCount = 0;
            tracker.OnEvolutionReached += _ => fireCount++;

            tracker.UpdateMass(5f);
            tracker.UpdateMass(10f);
            tracker.UpdateMass(15f);

            Assert.AreEqual(3, fireCount);
            Assert.AreEqual(3, tracker.CurrentLevel);
        }

        [Test]
        public void Tracker_Respects_Max_Evolutions()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 2);
            int fireCount = 0;
            tracker.OnEvolutionReached += _ => fireCount++;

            tracker.UpdateMass(5f);
            tracker.UpdateMass(10f);
            tracker.UpdateMass(15f);

            Assert.AreEqual(2, fireCount);
            Assert.AreEqual(2, tracker.CurrentLevel);
        }

        [Test]
        public void Tracker_IsFinalEvolution_Correct()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 2);

            tracker.UpdateMass(5f);
            Assert.IsFalse(tracker.IsFinalEvolution);

            tracker.UpdateMass(10f);
            Assert.IsTrue(tracker.IsFinalEvolution);
        }

        [Test]
        public void Tracker_Does_Not_Fire_When_Food_Is_Insufficient()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 3);
            int fireCount = 0;
            tracker.OnEvolutionReached += _ => fireCount++;

            tracker.UpdateMass(currentMass: 15f, consumedFoodCount: 5, foodRequiredPerEvolution: 6);

            Assert.AreEqual(0, fireCount);
            Assert.AreEqual(0, tracker.CurrentLevel);
        }

        [Test]
        public void Tracker_Uses_Minimum_Of_Mass_And_Food_Progress()
        {
            var tracker = new EvolutionTracker(thresholdMass: 5f, maxEvolutions: 3);
            int fireCount = 0;
            tracker.OnEvolutionReached += _ => fireCount++;

            tracker.UpdateMass(currentMass: 15f, consumedFoodCount: 6, foodRequiredPerEvolution: 6);
            Assert.AreEqual(1, fireCount);
            Assert.AreEqual(1, tracker.CurrentLevel);

            tracker.UpdateMass(currentMass: 15f, consumedFoodCount: 12, foodRequiredPerEvolution: 6);
            Assert.AreEqual(2, fireCount);
            Assert.AreEqual(2, tracker.CurrentLevel);
        }
    }
}
