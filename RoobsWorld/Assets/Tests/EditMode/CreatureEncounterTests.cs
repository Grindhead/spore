using NUnit.Framework;
using RoobsWorld.Creature;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureEncounterTests
    {
        [Test]
        public void ComputeDesiredDirection_UsesPlayer_WhenInsideAggro()
        {
            Vector3 dir = CreatureRivalAI.ComputeDesiredDirection(
                selfPosition: Vector3.zero,
                playerPosition: new Vector3(2f, 0f, 0f),
                wanderTarget: new Vector3(0f, 0f, 9f),
                aggroRange: 5f);

            Assert.That(dir.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(dir.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ComputeDesiredDirection_UsesWander_WhenOutsideAggro()
        {
            Vector3 dir = CreatureRivalAI.ComputeDesiredDirection(
                selfPosition: Vector3.zero,
                playerPosition: new Vector3(12f, 0f, 0f),
                wanderTarget: new Vector3(0f, 0f, 7f),
                aggroRange: 5f);

            Assert.That(dir.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(dir.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CreateRandomPlanarOffset_StaysWithinRadius()
        {
            const float radius = 10f;
            for (int i = 0; i < 40; i++)
            {
                Vector3 offset = CreatureRivalAI.CreateRandomPlanarOffset(radius);
                float mag = offset.magnitude;
                Assert.That(mag, Is.LessThanOrEqualTo(radius + 0.001f));
                Assert.That(mag, Is.GreaterThanOrEqualTo(radius * 0.35f - 0.001f));
                Assert.That(offset.y, Is.EqualTo(0f).Within(0.0001f));
            }
        }

        [Test]
        public void GetRandomSpawnPosition_RespectsClampRadius()
        {
            const float clamp = 18f;
            for (int i = 0; i < 40; i++)
            {
                Vector3 pos = CreatureStageEncounterManager.GetRandomSpawnPosition(
                    center: Vector3.zero,
                    minRadius: 8f,
                    maxRadius: 25f,
                    clampRadius: clamp);

                float dist = new Vector3(pos.x, 0f, pos.z).magnitude;
                Assert.That(dist, Is.LessThanOrEqualTo(clamp + 0.001f));
            }
        }

        [Test]
        public void ShouldSpawnReplacement_ReturnsTrue_WhenBelowDesiredAndUnderEffectiveCap()
        {
            bool shouldSpawn = CreatureStageEncounterManager.ShouldSpawnReplacement(
                activeRivals: 2,
                desiredActiveRivals: 3,
                defeatedRivals: 1,
                objectiveDefeats: 5,
                totalSpawnedRivals: 4,
                maxSpawnedRivals: 6,
                respawnCooldownTimer: 0f);

            Assert.IsTrue(shouldSpawn);
        }

        [Test]
        public void ShouldSpawnReplacement_UsesEffectiveCapSoObjectiveRemainsPossible()
        {
            bool shouldSpawn = CreatureStageEncounterManager.ShouldSpawnReplacement(
                activeRivals: 0,
                desiredActiveRivals: 3,
                defeatedRivals: 1,
                objectiveDefeats: 5,
                totalSpawnedRivals: 7,
                maxSpawnedRivals: 4,
                respawnCooldownTimer: 0f);

            Assert.IsTrue(shouldSpawn);
        }

        [Test]
        public void ShouldSpawnReplacement_ReturnsFalse_WhenAtEffectiveCap()
        {
            bool shouldSpawn = CreatureStageEncounterManager.ShouldSpawnReplacement(
                activeRivals: 0,
                desiredActiveRivals: 3,
                defeatedRivals: 1,
                objectiveDefeats: 5,
                totalSpawnedRivals: 8,
                maxSpawnedRivals: 4,
                respawnCooldownTimer: 0f);

            Assert.IsFalse(shouldSpawn);
        }

        [Test]
        public void ShouldSpawnReplacement_ReturnsFalse_WhenCooldownActiveOrObjectiveComplete()
        {
            bool cooldownBlocks = CreatureStageEncounterManager.ShouldSpawnReplacement(
                activeRivals: 0,
                desiredActiveRivals: 3,
                defeatedRivals: 2,
                objectiveDefeats: 5,
                totalSpawnedRivals: 3,
                maxSpawnedRivals: 12,
                respawnCooldownTimer: 1f);
            Assert.IsFalse(cooldownBlocks);

            bool objectiveCompleteBlocks = CreatureStageEncounterManager.ShouldSpawnReplacement(
                activeRivals: 0,
                desiredActiveRivals: 3,
                defeatedRivals: 5,
                objectiveDefeats: 5,
                totalSpawnedRivals: 3,
                maxSpawnedRivals: 12,
                respawnCooldownTimer: 0f);
            Assert.IsFalse(objectiveCompleteBlocks);
        }

        [Test]
        public void ComputeRivalTier_ScalesByDefeatStep()
        {
            Assert.AreEqual(0, CreatureStageEncounterManager.ComputeRivalTier(0, 2));
            Assert.AreEqual(0, CreatureStageEncounterManager.ComputeRivalTier(1, 2));
            Assert.AreEqual(1, CreatureStageEncounterManager.ComputeRivalTier(2, 2));
            Assert.AreEqual(2, CreatureStageEncounterManager.ComputeRivalTier(5, 2));
        }

        [Test]
        public void ComputeRivalTier_ClampsInvalidInputs()
        {
            Assert.AreEqual(0, CreatureStageEncounterManager.ComputeRivalTier(-2, 2));
            Assert.AreEqual(3, CreatureStageEncounterManager.ComputeRivalTier(3, 0));
        }

        [Test]
        public void ComputeRivalBodyScale_TracksPlayerScale()
        {
            var playerDna = CreatureDNA.CreateDefault();
            playerDna.Body.BodyScale = new Vector3(1.2f, 0.9f, 1.35f);

            Vector3 rivalScale = CreatureStageEncounterManager.ComputeRivalBodyScale(playerDna, rivalTier: 0);

            Assert.That(rivalScale.x, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(rivalScale.y, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(rivalScale.z, Is.EqualTo(1.35f).Within(0.001f));
        }

        [Test]
        public void ComputeRivalBodyScale_AllowsOnlySmallTierVariance()
        {
            var playerDna = CreatureDNA.CreateDefault();
            playerDna.Body.BodyScale = new Vector3(1.1f, 1f, 1.25f);

            Vector3 rivalScale = CreatureStageEncounterManager.ComputeRivalBodyScale(playerDna, rivalTier: 99);

            Assert.That(rivalScale.x, Is.EqualTo(1.1f * 1.12f).Within(0.001f));
            Assert.That(rivalScale.y, Is.EqualTo(1f * 1.12f).Within(0.001f));
            Assert.That(rivalScale.z, Is.EqualTo(1.25f * 1.12f).Within(0.001f));
        }

        [Test]
        public void ComputeDesiredActiveRivals_ScalesByProgress()
        {
            Assert.AreEqual(2, CreatureStage.ComputeDesiredActiveRivals(0, 5, 2, 4));
            Assert.AreEqual(2, CreatureStage.ComputeDesiredActiveRivals(1, 5, 2, 4));
            Assert.AreEqual(3, CreatureStage.ComputeDesiredActiveRivals(2, 5, 2, 4));
            Assert.AreEqual(4, CreatureStage.ComputeDesiredActiveRivals(4, 5, 2, 4));
        }

        [Test]
        public void ComputeDesiredActiveRivals_ClampsInvalidInputs()
        {
            Assert.AreEqual(0, CreatureStage.ComputeDesiredActiveRivals(-3, 0, -1, -1));
            Assert.AreEqual(3, CreatureStage.ComputeDesiredActiveRivals(99, 1, 3, 1));
        }

        [Test]
        public void ComputeNextUpgradeMilestone_ScalesFromCompletedUpgrades()
        {
            Assert.AreEqual(2, CreatureStage.ComputeNextUpgradeMilestone(0, 2));
            Assert.AreEqual(4, CreatureStage.ComputeNextUpgradeMilestone(1, 2));
            Assert.AreEqual(3, CreatureStage.ComputeNextUpgradeMilestone(-2, 3));
            Assert.AreEqual(3, CreatureStage.ComputeNextUpgradeMilestone(2, 0));
        }

        [Test]
        public void CreatureInstance_InvulnerabilityBlocksDamage()
        {
            var creatureObject = new GameObject("Creature_InvulnTest");
            try
            {
                var instance = creatureObject.AddComponent<CreatureInstance>();
                var dna = CreatureDNA.CreateDefault();
                dna.CreatureId = "invuln01";

                var stats = CreatureStats.BaseStats;
                stats.Health = 100f;
                stats.Defense = 0f;

                instance.Initialize(dna, stats, isPlayer: true);
                instance.GrantInvulnerability(2f);
                instance.TakeDamage(30f);

                Assert.That(instance.CurrentHealth, Is.EqualTo(100f).Within(0.001f));
                Assert.IsTrue(instance.IsInvulnerable);
            }
            finally
            {
                Object.DestroyImmediate(creatureObject);
            }
        }

        [Test]
        public void HudStatusKey_PrioritizesShieldThenObjectiveThenHealth()
        {
            Assert.AreEqual(
                "creature.status.shielded",
                CreatureStageHUD.GetStatusLocalizationKey(
                    isInvulnerable: true,
                    healthRatio: 0.1f,
                    defeatedRivals: 0,
                    targetDefeats: 5));

            Assert.AreEqual(
                "creature.status.objective_done",
                CreatureStageHUD.GetStatusLocalizationKey(
                    isInvulnerable: false,
                    healthRatio: 0.2f,
                    defeatedRivals: 5,
                    targetDefeats: 5));

            Assert.AreEqual(
                "creature.status.low_health",
                CreatureStageHUD.GetStatusLocalizationKey(
                    isInvulnerable: false,
                    healthRatio: 0.2f,
                    defeatedRivals: 1,
                    targetDefeats: 5));

            Assert.AreEqual(
                "creature.status.hunt",
                CreatureStageHUD.GetStatusLocalizationKey(
                    isInvulnerable: false,
                    healthRatio: 0.8f,
                    defeatedRivals: 1,
                    targetDefeats: 5));
        }

        [Test]
        public void ComputeNextStreak_ContinuesWhenTimerActive_OtherwiseResets()
        {
            Assert.AreEqual(4, CreatureStageEncounterManager.ComputeNextStreak(3, 0.4f));
            Assert.AreEqual(1, CreatureStageEncounterManager.ComputeNextStreak(7, 0f));
            Assert.AreEqual(1, CreatureStageEncounterManager.ComputeNextStreak(7, -1f));
        }

        [Test]
        public void ComputeStreakBonusDNA_AwardsAtThresholdAndStepMilestones()
        {
            Assert.AreEqual(0, CreatureStageEncounterManager.ComputeStreakBonusDNA(2, 3, 3, 2));
            Assert.AreEqual(3, CreatureStageEncounterManager.ComputeStreakBonusDNA(3, 3, 3, 2));
            Assert.AreEqual(0, CreatureStageEncounterManager.ComputeStreakBonusDNA(4, 3, 3, 2));
            Assert.AreEqual(4, CreatureStageEncounterManager.ComputeStreakBonusDNA(5, 3, 3, 2));
            Assert.AreEqual(5, CreatureStageEncounterManager.ComputeStreakBonusDNA(7, 3, 3, 2));
        }

        [Test]
        public void SelectBestRespawnCandidate_PicksFarthestCandidateFromRivals()
        {
            Vector3[] candidates =
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(18f, 0f, 0f),
                new Vector3(-20f, 0f, 0f)
            };
            Vector3[] rivals =
            {
                new Vector3(16f, 0f, 0f),
                new Vector3(14f, 0f, 0f)
            };

            Vector3 best = CreatureStageEncounterManager.SelectBestRespawnCandidate(candidates, rivals, Vector3.zero);
            Assert.That(best.x, Is.EqualTo(-20f).Within(0.001f));
        }

        [Test]
        public void SelectBestRespawnCandidate_FallsBackWhenNoCandidates()
        {
            Vector3 fallback = new Vector3(3f, 0f, 4f);
            Vector3 best = CreatureStageEncounterManager.SelectBestRespawnCandidate(null, new[] { Vector3.zero }, fallback);
            Assert.That(best.x, Is.EqualTo(3f).Within(0.001f));
            Assert.That(best.z, Is.EqualTo(4f).Within(0.001f));
        }
    }
}
