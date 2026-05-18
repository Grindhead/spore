using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

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

        [Test]
        public void CellRouteMemory_SelectsWanderDirectionAwayFromRecentRoute()
        {
            var recent = new Queue<Vector2Int>();
            CellRouteMemory.RecordRecentLocation(recent, Vector3.zero, 2f, 8);
            CellRouteMemory.RecordRecentLocation(recent, new Vector3(2f, 0f, 0f), 2f, 8);
            CellRouteMemory.RecordRecentLocation(recent, new Vector3(4f, 0f, 0f), 2f, 8);

            Vector3 direction = CellAI.SelectNovelWanderDirection(
                currentPosition: Vector3.zero,
                candidateDirections: new[]
                {
                    Vector3.right,
                    Vector3.left,
                    Vector3.forward
                },
                recentCells: recent,
                memoryCellSize: 2f,
                projectionDistance: 4f,
                previousDirection: Vector3.right);

            Assert.That(direction.z, Is.GreaterThan(0.7f));
            Assert.That(Mathf.Abs(direction.x), Is.LessThan(0.01f));
        }

        [Test]
        public void CellRouteMemory_CapsRecentCells()
        {
            var recent = new Queue<Vector2Int>();
            for (int i = 0; i < 12; i++)
                CellRouteMemory.RecordRecentLocation(recent, new Vector3(i, 0f, 0f), 1f, 5);

            Assert.That(recent.Count, Is.LessThanOrEqualTo(5));
            Assert.IsFalse(recent.Contains(new Vector2Int(0, 0)));
        }
    }
}
