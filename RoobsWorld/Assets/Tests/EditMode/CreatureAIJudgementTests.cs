using NUnit.Framework;
using RoobsWorld.Creature;
using RoobsWorld.Creature.AI;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureAIJudgementTests
    {
        [Test]
        public void ThreatUrgency_IncreasesWhenCreatureIsInjured()
        {
            var self = CreatureStats.BaseStats;
            var other = CreatureStats.BaseStats;
            other.Attack = 12f;

            float healthy = CreatureAIJudgement.EvaluateThreatUrgency(
                self, 1f, other, 1f, distance: 6f, sizeThreat: false);
            float injured = CreatureAIJudgement.EvaluateThreatUrgency(
                self, 0.2f, other, 1f, distance: 6f, sizeThreat: false);

            Assert.Greater(injured, healthy);
        }

        [Test]
        public void PreyAppeal_PrefersWeakNearbyPreyOverHealthyFarPrey()
        {
            var self = CreatureStats.BaseStats;
            self.Attack = 20f;
            var prey = CreatureStats.BaseStats;
            prey.Defense = 1f;

            float weakNearby = CreatureAIJudgement.EvaluatePreyAppeal(
                self, prey, preyHealthRatio: 0.15f, distance: 3f, sizeThreat: false);
            float healthyFar = CreatureAIJudgement.EvaluatePreyAppeal(
                self, prey, preyHealthRatio: 1f, distance: 16f, sizeThreat: false);

            Assert.Greater(weakNearby, healthyFar);
        }

        [Test]
        public void PreyAppeal_ReturnsZeroForLargerThreat()
        {
            var self = CreatureStats.BaseStats;
            self.Attack = 50f;
            var prey = CreatureStats.BaseStats;

            float appeal = CreatureAIJudgement.EvaluatePreyAppeal(
                self, prey, preyHealthRatio: 0.1f, distance: 1f, sizeThreat: true);

            Assert.AreEqual(0f, appeal);
        }

        [Test]
        public void ShouldReturnToTerritory_WhenChasePullsTooFarFromHome()
        {
            bool result = CreatureAIJudgement.ShouldReturnToTerritory(
                selfPosition: new Vector3(18f, 0f, 0f),
                anchorPosition: Vector3.zero,
                playerPosition: new Vector3(26f, 0f, 0f),
                wanderRadius: 10f,
                aggroRange: 14f);

            Assert.IsTrue(result);
        }

        [Test]
        public void ShouldReturnToTerritory_FalseNearHome()
        {
            bool result = CreatureAIJudgement.ShouldReturnToTerritory(
                selfPosition: new Vector3(4f, 0f, 0f),
                anchorPosition: Vector3.zero,
                playerPosition: new Vector3(6f, 0f, 0f),
                wanderRadius: 10f,
                aggroRange: 14f);

            Assert.IsFalse(result);
        }

        [Test]
        public void ChooseRivalIntent_FleesWhenInjuredAndOutmatched()
        {
            var self = CreatureStats.BaseStats;
            self.Defense = 2f;
            var player = CreatureStats.BaseStats;
            player.Attack = 35f;

            var intent = CreatureAIJudgement.ChooseRivalIntent(
                self,
                selfHealthRatio: 0.18f,
                player,
                playerHealthRatio: 1f,
                playerDistance: 3f,
                sizeThreat: true,
                selfPosition: Vector3.zero,
                anchorPosition: Vector3.zero,
                playerPosition: new Vector3(3f, 0f, 0f),
                wanderRadius: 10f,
                aggroRange: 14f);

            Assert.AreEqual(CreatureRivalIntent.Flee, intent);
        }

        [Test]
        public void ChooseRivalIntent_StalksWhenOpportunityIsModerate()
        {
            var self = CreatureStats.BaseStats;
            self.Attack = 11f;
            var player = CreatureStats.BaseStats;
            player.Health = 90f;

            var intent = CreatureAIJudgement.ChooseRivalIntent(
                self,
                selfHealthRatio: 0.9f,
                player,
                playerHealthRatio: 0.65f,
                playerDistance: 8f,
                sizeThreat: false,
                selfPosition: Vector3.zero,
                anchorPosition: Vector3.zero,
                playerPosition: new Vector3(8f, 0f, 0f),
                wanderRadius: 10f,
                aggroRange: 14f);

            Assert.AreEqual(CreatureRivalIntent.Stalk, intent);
        }

        [Test]
        public void ChooseRivalIntent_ReturnsHomeBeforeChasingDistantPlayer()
        {
            var intent = CreatureAIJudgement.ChooseRivalIntent(
                CreatureStats.BaseStats,
                selfHealthRatio: 1f,
                CreatureStats.BaseStats,
                playerHealthRatio: 0.2f,
                playerDistance: 5f,
                sizeThreat: false,
                selfPosition: new Vector3(18f, 0f, 0f),
                anchorPosition: Vector3.zero,
                playerPosition: new Vector3(23f, 0f, 0f),
                wanderRadius: 10f,
                aggroRange: 14f);

            Assert.AreEqual(CreatureRivalIntent.ReturnToTerritory, intent);
        }

        [Test]
        public void ComputeCautiousDirection_MovesAwayFromPlayerWithAnchorBias()
        {
            Vector3 direction = CreatureAIJudgement.ComputeCautiousDirection(
                selfPosition: new Vector3(5f, 0f, 0f),
                playerPosition: new Vector3(2f, 0f, 0f),
                anchorPosition: Vector3.zero);

            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
        }
    }
}
