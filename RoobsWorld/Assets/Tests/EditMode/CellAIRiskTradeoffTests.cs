using NUnit.Framework;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class CellAIRiskTradeoffTests
    {
        [Test]
        public void ShouldFleeUnderPredationRisk_NoThreat_ReturnsFalse()
        {
            bool result = CellAI.ShouldFleeUnderPredationRisk(
                hasThreat: false,
                hasFood: true,
                currentScale: 0.3f,
                threatDistance: 2f,
                foodDistance: 1f);

            Assert.IsFalse(result);
        }

        [Test]
        public void ShouldFleeUnderPredationRisk_NotHungry_ReturnsTrue()
        {
            bool result = CellAI.ShouldFleeUnderPredationRisk(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.9f,
                threatDistance: 4f,
                foodDistance: 1f);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldFleeUnderPredationRisk_HungryButNotCritical_ReturnsTrue()
        {
            bool result = CellAI.ShouldFleeUnderPredationRisk(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.55f,
                threatDistance: 4f,
                foodDistance: 1f);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldFleeUnderPredationRisk_CriticallyHungryImmediateThreat_ReturnsTrue()
        {
            bool result = CellAI.ShouldFleeUnderPredationRisk(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.3f,
                threatDistance: 1f,
                foodDistance: 1f);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldFleeUnderPredationRisk_CriticallyHungryFoodSafer_ReturnsFalse()
        {
            bool result = CellAI.ShouldFleeUnderPredationRisk(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.3f,
                threatDistance: 4f,
                foodDistance: 1f);

            Assert.IsFalse(result);
        }

        [Test]
        public void EvaluateRiskTradeoffScores_ImmediateThreat_IncreasesFleeDrive()
        {
            var nearThreat = CellAI.EvaluateRiskTradeoffScores(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.3f,
                threatDistance: 1f,
                foodDistance: 1f);

            var farThreat = CellAI.EvaluateRiskTradeoffScores(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.3f,
                threatDistance: 5f,
                foodDistance: 1f);

            Assert.Greater(nearThreat.FleeDrive, farThreat.FleeDrive);
        }

        [Test]
        public void EvaluateRiskTradeoffScores_Hunger_IncreasesForageDrive()
        {
            var lowHunger = CellAI.EvaluateRiskTradeoffScores(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.6f,
                threatDistance: 5f,
                foodDistance: 1f);

            var highHunger = CellAI.EvaluateRiskTradeoffScores(
                hasThreat: true,
                hasFood: true,
                currentScale: 0.25f,
                threatDistance: 5f,
                foodDistance: 1f);

            Assert.Greater(highHunger.ForageDrive, lowHunger.ForageDrive);
        }

        [Test]
        public void IsThreatEntity_PlayerSlightlyBigger_ReturnsTrue()
        {
            bool result = CellAI.IsThreatEntity(
                mySize: 1.0f,
                otherSize: 1.01f,
                isPlayerEntity: true);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsThreatEntity_NonPlayerSlightlyBigger_ReturnsFalse()
        {
            bool result = CellAI.IsThreatEntity(
                mySize: 1.0f,
                otherSize: 1.01f,
                isPlayerEntity: false);

            Assert.IsFalse(result);
        }

        [Test]
        public void CellOrganismAI_IsThreatEntity_PlayerSlightlyBigger_ReturnsTrue()
        {
            bool result = CellOrganismAI.IsThreatEntity(
                mySize: 1.0f,
                otherSize: 1.01f,
                isPlayerEntity: true);

            Assert.IsTrue(result);
        }
    }
}
