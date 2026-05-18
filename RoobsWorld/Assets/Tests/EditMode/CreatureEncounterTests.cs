using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using RoobsWorld.Creature.AI;
using RoobsWorld.Multiplayer;
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
        public void ComputeStalkDirection_BlendsApproachWithCircling()
        {
            Vector3 dir = CreatureRivalAI.ComputeStalkDirection(
                selfPosition: Vector3.zero,
                playerPosition: new Vector3(10f, 0f, 0f),
                wanderTarget: new Vector3(0f, 0f, 7f));

            Assert.That(dir.x, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(dir.z), Is.GreaterThan(0.1f));
            Assert.That(dir.magnitude, Is.EqualTo(1f).Within(0.001f));
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
        public void GetActiveCreaturePlayerCount_DefaultsToSinglePlayerWhenSessionMissing()
        {
            Assert.AreEqual(1, CreatureStage.GetActiveCreaturePlayerCount(null));
        }

        [Test]
        public void GetActiveCreaturePlayerCount_UsesSessionPlayerCount()
        {
            var session = new MultiplayerSessionManager();
            session.CreateSession();
            session.AddRemotePlayer(1, "Player 1");
            session.AddRemotePlayer(2, "Player 2");

            Assert.AreEqual(3, CreatureStage.GetActiveCreaturePlayerCount(session));
        }

        [Test]
        public void ComputeMultiplayerScaledCount_AddsPerExtraPlayerAndCapsBonus()
        {
            Assert.AreEqual(5, CreatureStage.ComputeMultiplayerScaledCount(5, 1, 2, 10));
            Assert.AreEqual(9, CreatureStage.ComputeMultiplayerScaledCount(5, 3, 2, 10));
            Assert.AreEqual(15, CreatureStage.ComputeMultiplayerScaledCount(5, 8, 2, 10));
        }

        [Test]
        public void ComputeMultiplayerScaledCount_ClampsInvalidInputs()
        {
            Assert.AreEqual(0, CreatureStage.ComputeMultiplayerScaledCount(-4, 0, -2, -8));
            Assert.AreEqual(3, CreatureStage.ComputeMultiplayerScaledCount(3, 4, -1, 5));
        }

        [Test]
        public void CreatureObjectiveDefeatScaling_IncreasesPerExtraPlayer_AndCaps()
        {
            const int baseObjectiveDefeats = 5;
            const int perExtraPlayer = 2;
            const int maxBonus = 6;

            Assert.AreEqual(5, CreatureStage.ComputeMultiplayerScaledCount(baseObjectiveDefeats, 1, perExtraPlayer, maxBonus));
            Assert.AreEqual(7, CreatureStage.ComputeMultiplayerScaledCount(baseObjectiveDefeats, 2, perExtraPlayer, maxBonus));
            Assert.AreEqual(9, CreatureStage.ComputeMultiplayerScaledCount(baseObjectiveDefeats, 3, perExtraPlayer, maxBonus));
            Assert.AreEqual(11, CreatureStage.ComputeMultiplayerScaledCount(baseObjectiveDefeats, 4, perExtraPlayer, maxBonus));
            Assert.AreEqual(11, CreatureStage.ComputeMultiplayerScaledCount(baseObjectiveDefeats, 6, perExtraPlayer, maxBonus));
        }

        [Test]
        public void PackMateAssistedObjectiveTarget_ReducesCombatAndSocialTargetsOnly()
        {
            var rivalCull = new CreatureStageObjective(CreatureStageObjectiveType.RivalCull, 5, 1f, "creature.objective.rival_cull");
            var pacify = new CreatureStageObjective(CreatureStageObjectiveType.PacifyRivals, 4, 1f, "creature.objective.pacify");
            var holdGround = new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, 5, 45f, "creature.objective.hold_ground");

            Assert.AreEqual(4, CreatureStage.ComputePackMateAssistedObjectiveTarget(rivalCull, 5, hasPackMate: true));
            Assert.AreEqual(3, CreatureStage.ComputePackMateAssistedObjectiveTarget(pacify, 4, hasPackMate: true));
            Assert.AreEqual(5, CreatureStage.ComputePackMateAssistedObjectiveTarget(holdGround, 5, hasPackMate: true));
            Assert.AreEqual(1, CreatureStage.ComputePackMateAssistedObjectiveTarget(rivalCull, 1, hasPackMate: true));
            Assert.AreEqual(5, CreatureStage.ComputePackMateAssistedObjectiveTarget(rivalCull, 5, hasPackMate: false));
            Assert.AreEqual(1, CreatureStage.ComputePackMateObjectiveAssist(rivalCull, 5, hasPackMate: true));
            Assert.AreEqual(0, CreatureStage.ComputePackMateObjectiveAssist(holdGround, 5, hasPackMate: true));
            Assert.AreEqual(0, CreatureStage.ComputePackMateObjectiveAssist(rivalCull, 1, hasPackMate: true));
            Assert.AreEqual("   Pack assist: -1 target", CreatureStageHUD.BuildPackMateAssistSuffix(1));
            Assert.AreEqual(string.Empty, CreatureStageHUD.BuildPackMateAssistSuffix(0));
        }

        [Test]
        public void SocialPacifyAssist_DoublesControlProgressAndImprovesRelationshipGain()
        {
            var socialRatings = new[]
            {
                new CreatureVerbRating(CreatureVerb.Sing, 0.8f),
                new CreatureVerbRating(CreatureVerb.Charm, 0.8f)
            };
            var weakRatings = new[]
            {
                new CreatureVerbRating(CreatureVerb.Pose, 0.2f)
            };

            Assert.AreEqual(2, CreatureStage.ComputePacifyActionProgressIncrement(CreatureCombatFeedbackType.Guard, socialRatings));
            Assert.AreEqual(1, CreatureStage.ComputePacifyActionProgressIncrement(CreatureCombatFeedbackType.Guard, weakRatings));
            Assert.AreEqual(0, CreatureStage.ComputePacifyActionProgressIncrement(CreatureCombatFeedbackType.Hit, socialRatings));
            Assert.AreEqual(0.65f, CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Guard, hasSocialAssist: true), 0.001f);
            Assert.AreEqual(0.4f, CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Guard, hasSocialAssist: false), 0.001f);
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
        public void ComputeObjectiveDnaReward_ScalesForHigherRiskObjectives()
        {
            var rivalCull = new CreatureStageObjective(CreatureStageObjectiveType.RivalCull, 5, 1f, "creature.objective.rival_cull");
            var alphaHunt = new CreatureStageObjective(CreatureStageObjectiveType.AlphaHunt, 3, 1f, "creature.objective.alpha_hunt");
            var holdGround = new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, 5, 45f, "creature.objective.hold_ground");

            int baseReward = BalanceConstants.CreatureDNAPointsObjectiveComplete;

            Assert.AreEqual(baseReward, CreatureStage.ComputeObjectiveDnaReward(rivalCull, baseReward));
            Assert.AreEqual(baseReward + 6, CreatureStage.ComputeObjectiveDnaReward(alphaHunt, baseReward));
            Assert.AreEqual(baseReward + 4, CreatureStage.ComputeObjectiveDnaReward(holdGround, baseReward));
        }

        [Test]
        public void ComputeObjectivePaceTargetSeconds_UsesDefeatTargetsAndHoldGrace()
        {
            var rivalCull = new CreatureStageObjective(CreatureStageObjectiveType.RivalCull, 5, 1f, "creature.objective.rival_cull");
            var holdGround = new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, 5, 45f, "creature.objective.hold_ground");

            Assert.That(
                CreatureStage.ComputeObjectivePaceTargetSeconds(rivalCull, 18f, 0.2f),
                Is.EqualTo(90f).Within(0.001f));
            Assert.That(
                CreatureStage.ComputeObjectivePaceTargetSeconds(holdGround, 18f, 0.2f),
                Is.EqualTo(54f).Within(0.001f));
        }

        [Test]
        public void ComputeObjectivePaceBonusDna_RewardsFastClearsAndCaps()
        {
            var rivalCull = new CreatureStageObjective(CreatureStageObjectiveType.RivalCull, 5, 1f, "creature.objective.rival_cull");
            var holdGround = new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, 5, 45f, "creature.objective.hold_ground");

            Assert.AreEqual(6, CreatureStage.ComputeObjectivePaceBonusDna(rivalCull, 0f, 18f, 0.2f, 6));
            Assert.AreEqual(3, CreatureStage.ComputeObjectivePaceBonusDna(rivalCull, 45f, 18f, 0.2f, 6));
            Assert.AreEqual(0, CreatureStage.ComputeObjectivePaceBonusDna(rivalCull, 90f, 18f, 0.2f, 6));
            Assert.AreEqual(1, CreatureStage.ComputeObjectivePaceBonusDna(holdGround, 45f, 18f, 0.2f, 6));
            Assert.AreEqual(0, CreatureStage.ComputeObjectivePaceBonusDna(holdGround, 54f, 18f, 0.2f, 6));
        }

        [Test]
        public void ComputeObjectiveMomentumDelaySeconds_ShortensBetweenObjectivesAndCaps()
        {
            Assert.That(
                CreatureStage.ComputeObjectiveMomentumDelaySeconds(6f, 0, 0.45f, 3.5f),
                Is.EqualTo(6f).Within(0.001f));
            Assert.That(
                CreatureStage.ComputeObjectiveMomentumDelaySeconds(6f, 2, 0.45f, 3.5f),
                Is.EqualTo(5.1f).Within(0.001f));
            Assert.That(
                CreatureStage.ComputeObjectiveMomentumDelaySeconds(6f, 99, 0.45f, 3.5f),
                Is.EqualTo(3.5f).Within(0.001f));
        }

        [Test]
        public void ComputeObjectiveStaminaRestore_ScalesWithPaceBonusAndCaps()
        {
            Assert.That(
                CreatureStage.ComputeObjectiveStaminaRestore(0, 18f, 1.5f, 30f),
                Is.EqualTo(18f).Within(0.001f));
            Assert.That(
                CreatureStage.ComputeObjectiveStaminaRestore(6, 18f, 1.5f, 30f),
                Is.EqualTo(27f).Within(0.001f));
            Assert.That(
                CreatureStage.ComputeObjectiveStaminaRestore(99, 18f, 1.5f, 30f),
                Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void BuildObjectiveRewardLabel_ShowsDnaRewardBeforeCompletion()
        {
            var alphaHunt = new CreatureStageObjective(CreatureStageObjectiveType.AlphaHunt, 3, 1f, "creature.objective.alpha_hunt");

            Assert.AreEqual(
                "Reward: +26 DNA",
                CreatureStage.BuildObjectiveRewardLabel(alphaHunt, BalanceConstants.CreatureDNAPointsObjectiveComplete));
        }

        [Test]
        public void ComputeObjectiveDefeatProgress_UsesObjectiveStartBaseline()
        {
            Assert.AreEqual(3, CreatureStage.ComputeObjectiveDefeatProgress(8, 5));
            Assert.AreEqual(0, CreatureStage.ComputeObjectiveDefeatProgress(3, 5));
            Assert.AreEqual(4, CreatureStage.ComputeObjectiveDefeatProgress(4, -3));
        }

        [Test]
        public void ComputeObjectiveAlphaProgress_UsesObjectiveStartBaseline()
        {
            Assert.AreEqual(2, CreatureStage.ComputeObjectiveAlphaProgress(5, 3));
            Assert.AreEqual(0, CreatureStage.ComputeObjectiveAlphaProgress(3, 5));
            Assert.AreEqual(4, CreatureStage.ComputeObjectiveAlphaProgress(4, -2));
        }

        [Test]
        public void ComputeHoldObjectivePressureProgress_ScalesFromHoldTime()
        {
            Assert.AreEqual(0, CreatureStage.ComputeHoldObjectivePressureProgress(-1f, 45f, 5));
            Assert.AreEqual(0, CreatureStage.ComputeHoldObjectivePressureProgress(8.9f, 45f, 5));
            Assert.AreEqual(1, CreatureStage.ComputeHoldObjectivePressureProgress(9f, 45f, 5));
            Assert.AreEqual(3, CreatureStage.ComputeHoldObjectivePressureProgress(27f, 45f, 5));
            Assert.AreEqual(5, CreatureStage.ComputeHoldObjectivePressureProgress(45f, 45f, 5));
            Assert.AreEqual(5, CreatureStage.ComputeHoldObjectivePressureProgress(99f, 45f, 5));
        }

        [Test]
        public void BuildHoldObjectiveProgressText_ShowsWhetherPlayerIsSecuringGround()
        {
            Assert.AreEqual(
                "Reward: +24 DNA   Securing: 13/45s   Active Rivals: 2/3   Streak: x4",
                CreatureStage.BuildHoldObjectiveProgressText("Reward: +24 DNA", 13f, 45f, 2, 3, 4, true));
            Assert.AreEqual(
                "Reward: +24 DNA   Return to ground: 13/45s   Active Rivals: 2/3   Streak: x4",
                CreatureStage.BuildHoldObjectiveProgressText("Reward: +24 DNA", 13f, 45f, 2, 3, 4, false));
        }

        [Test]
        public void BuildStreakTimerSuffix_ShowsRemainingChainTimeOnlyWhenActive()
        {
            Assert.AreEqual("   Chain: 8s", CreatureStage.BuildStreakTimerSuffix(2, 7.2f));
            Assert.AreEqual("   Chain: 1s", CreatureStage.BuildStreakTimerSuffix(1, 0.1f));
            Assert.AreEqual(string.Empty, CreatureStage.BuildStreakTimerSuffix(0, 7.2f));
            Assert.AreEqual(string.Empty, CreatureStage.BuildStreakTimerSuffix(2, 0f));
        }

        [Test]
        public void ComputeHoldGroundDistanceToMarker_MeasuresDistanceToMarkerEdge()
        {
            Assert.That(CreatureStage.ComputeHoldGroundDistanceToMarker(Vector3.zero), Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                CreatureStage.ComputeHoldGroundDistanceToMarker(new Vector3(CreatureStageObjectivePlanner.FeedingGroundRadius + 7f, 2f, 0f)),
                Is.EqualTo(7f).Within(0.001f));
        }

        [Test]
        public void ComputeObjectiveSpawnProgress_UsesObjectiveStartBaseline()
        {
            Assert.AreEqual(3, CreatureStage.ComputeObjectiveSpawnProgress(19, 16));
            Assert.AreEqual(0, CreatureStage.ComputeObjectiveSpawnProgress(12, 12));
            Assert.AreEqual(7, CreatureStage.ComputeObjectiveSpawnProgress(7, -4));
        }

        [Test]
        public void ComputeObjectiveCompletionSeconds_UsesObjectiveStartTime()
        {
            Assert.That(CreatureStage.ComputeObjectiveCompletionSeconds(42f, 12f), Is.EqualTo(30f).Within(0.001f));
            Assert.That(CreatureStage.ComputeObjectiveCompletionSeconds(4f, 12f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ShouldSpawnReplacement_AllowsRepeatObjectivesWithLocalSpawnProgress()
        {
            int localSpawnProgress = CreatureStage.ComputeObjectiveSpawnProgress(
                totalSpawnedRivals: 19,
                objectiveStartSpawnedRivals: 16);

            bool shouldSpawn = CreatureStageEncounterManager.ShouldSpawnReplacement(
                activeRivals: 1,
                desiredActiveRivals: 3,
                defeatedRivals: 1,
                objectiveDefeats: 5,
                totalSpawnedRivals: localSpawnProgress,
                maxSpawnedRivals: 4,
                respawnCooldownTimer: 0f);

            Assert.IsTrue(shouldSpawn);
        }

        [Test]
        public void BuildObjectiveCooldownText_CeilsRemainingSeconds()
        {
            Assert.AreEqual("Objective complete. Next objective in 6s", CreatureStage.BuildObjectiveCooldownText(5.2f));
            Assert.AreEqual("Objective complete. Next objective in 0s", CreatureStage.BuildObjectiveCooldownText(-1f));
        }

        [Test]
        public void GetRivalArchetype_RotatesAcrossDistinctTactics()
        {
            Assert.AreEqual(CreatureRivalArchetype.Charger, CreatureStageEncounterManager.GetRivalArchetype(0, 0));
            Assert.AreEqual(CreatureRivalArchetype.Stalker, CreatureStageEncounterManager.GetRivalArchetype(1, 0));
            Assert.AreEqual(CreatureRivalArchetype.Tank, CreatureStageEncounterManager.GetRivalArchetype(2, 0));
            Assert.AreEqual(CreatureRivalArchetype.Skirmisher, CreatureStageEncounterManager.GetRivalArchetype(3, 0));
            Assert.AreEqual(CreatureRivalArchetype.Stalker, CreatureStageEncounterManager.GetRivalArchetype(0, 1));
        }

        [Test]
        public void AlphaHunt_SpawnsRecurringAlphaRivals()
        {
            Assert.IsTrue(CreatureStageEncounterManager.ShouldSpawnAlphaRival(CreatureStageObjectiveType.AlphaHunt, 0));
            Assert.IsFalse(CreatureStageEncounterManager.ShouldSpawnAlphaRival(CreatureStageObjectiveType.AlphaHunt, 1));
            Assert.IsTrue(CreatureStageEncounterManager.ShouldSpawnAlphaRival(CreatureStageObjectiveType.AlphaHunt, 2));
            Assert.IsFalse(CreatureStageEncounterManager.ShouldSpawnAlphaRival(CreatureStageObjectiveType.AlphaHunt, 3));
            Assert.IsFalse(CreatureStageEncounterManager.ShouldSpawnAlphaRival(CreatureStageObjectiveType.RivalCull, 0));
            Assert.AreEqual("Alpha Tank", CreatureStageEncounterManager.BuildRivalLabel(CreatureRivalArchetype.Tank, true));
        }

        [Test]
        public void PlayerGuard_CanStartOnlyWhenCooldownAndGuardAreClear()
        {
            Assert.IsTrue(CreaturePlayerController.CanStartGuard(0f, 0f));
            Assert.IsFalse(CreaturePlayerController.CanStartGuard(0.1f, 0f));
            Assert.IsFalse(CreaturePlayerController.CanStartGuard(0f, 0.1f));
        }

        [Test]
        public void CanResolveTelegraphedAttack_RequiresTargetStillInReach()
        {
            Assert.IsTrue(CreatureRivalAI.CanResolveTelegraphedAttack(1.8f, 2f));
            Assert.IsFalse(CreatureRivalAI.CanResolveTelegraphedAttack(2.2f, 2f));
        }

        [Test]
        public void TargetAssist_ScoresForwardTargetsAboveSideTargets()
        {
            float forward = CreatureTargetAssist.ComputeTargetScore(2f, 1f, overlapsAttackZone: true);
            float side = CreatureTargetAssist.ComputeTargetScore(2f, 0f, overlapsAttackZone: true);

            Assert.Greater(forward, side);
            Assert.IsTrue(CreatureTargetAssist.IsWithinAssistCone(Vector3.forward, Vector3.forward));
            Assert.IsFalse(CreatureTargetAssist.IsWithinAssistCone(Vector3.forward, Vector3.left, 0.5f));
        }

        [Test]
        public void CreatureArenaLandmarks_IncludeTacticalTerrainAndHealing()
        {
            var landmarks = CreatureStageArena.GetDefaultLandmarks(48f);
            Assert.That(landmarks, Has.Some.Matches<CreatureArenaLandmark>(l => l.Type == CreatureArenaLandmarkType.Cover));
            Assert.That(landmarks, Has.Some.Matches<CreatureArenaLandmark>(l => l.Type == CreatureArenaLandmarkType.HighGround));
            Assert.That(landmarks, Has.Some.Matches<CreatureArenaLandmark>(l => l.Type == CreatureArenaLandmarkType.NarrowPassage));
            Assert.That(landmarks, Has.Some.Matches<CreatureArenaLandmark>(l => l.Type == CreatureArenaLandmarkType.HealingZone));
        }

        [Test]
        public void ObjectivePlanner_CanSelectNonDefeatObjective()
        {
            var dna = new CreatureDNA
            {
                CreatureId = "holdtest",
                CreatureName = "Hold Test",
                Body = new BodyPlan { SpineSegments = 3, Symmetry = SymmetryType.Bilateral, BodyScale = Vector3.one },
                Parts = new System.Collections.Generic.List<PartPlacement>
                {
                    new PartPlacement { PartId = "tail_basic", AttachBone = 2, Offset = Vector3.back },
                    new PartPlacement { PartId = "shell_basic", AttachBone = 1, Offset = Vector3.zero }
                }
            };

            var objective = CreatureStageObjectivePlanner.CreateObjective(dna);

            Assert.AreEqual(CreatureStageObjectiveType.HoldFeedingGround, objective.Type);
            Assert.IsTrue(objective.UsesHoldTimer);
        }

        [Test]
        public void ObjectivePlanner_RotatesWithCompletedObjectiveCount()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Parts.Clear();

            Assert.AreEqual(0, CreatureStageObjectivePlanner.ComputeObjectiveSlot(0, 0, 0));
            Assert.AreEqual(1, CreatureStageObjectivePlanner.ComputeObjectiveSlot(0, 0, 1));
            Assert.AreEqual(2, CreatureStageObjectivePlanner.ComputeObjectiveSlot(0, 0, 2));
            Assert.AreEqual(3, CreatureStageObjectivePlanner.ComputeObjectiveSlot(0, 0, 3));
            Assert.AreEqual(0, CreatureStageObjectivePlanner.ComputeObjectiveSlot(0, 0, 4));
            Assert.AreEqual(CreatureStageObjectiveType.RivalCull, CreatureStageObjectivePlanner.CreateObjective(dna, 0).Type);
            Assert.AreEqual(CreatureStageObjectiveType.AlphaHunt, CreatureStageObjectivePlanner.CreateObjective(dna, 1).Type);
            Assert.AreEqual(CreatureStageObjectiveType.HoldFeedingGround, CreatureStageObjectivePlanner.CreateObjective(dna, 2).Type);
            Assert.AreEqual(CreatureStageObjectiveType.PacifyRivals, CreatureStageObjectivePlanner.CreateObjective(dna, 3).Type);
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
        public void HudFeedback_CoversCounterAndRivalArchetypeLabels()
        {
            Assert.AreEqual(
                "creature.feedback.counter",
                CreatureStageHUD.GetFeedbackLocalizationKey(CreatureCombatFeedbackType.Counter));
            Assert.AreEqual("Tank rival down", CreatureStageHUD.BuildRivalDefeatedLabel(" Tank "));
            Assert.AreEqual("Rival down", CreatureStageHUD.BuildRivalDefeatedLabel(" "));
            Assert.AreEqual("Objective complete: +24 DNA", CreatureStageHUD.BuildObjectiveCompletedLabel(24));
            Assert.AreEqual("Objective complete: +0 DNA", CreatureStageHUD.BuildObjectiveCompletedLabel(-4));
            Assert.AreEqual("Objective 2: +24 DNA in 31s  Streak x3", CreatureStageHUD.BuildObjectiveCompletedLabel(24, 2, 31.4f, 3));
            Assert.AreEqual("   Next: Find rivals", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.RivalCull, false));
            Assert.AreEqual("   Next: Target alpha", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.AlphaHunt, false));
            Assert.AreEqual("   Next: Reach marker", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.HoldFeedingGround, false));
            Assert.AreEqual("   Next: Hold ground", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.HoldFeedingGround, true));
            Assert.AreEqual("   Next: Find rivals (15m)", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.RivalCull, false, 14.2f));
            Assert.AreEqual("   Next: Target alpha (18m)", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.AlphaHunt, false, 17.1f));
            Assert.AreEqual("   Next: Reach marker (8m)", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.HoldFeedingGround, false, 7.4f));
            Assert.AreEqual("   Next: Hold ground", CreatureStageHUD.BuildObjectiveGuidanceSuffix(CreatureStageObjectiveType.HoldFeedingGround, true, 7.4f));
            StringAssert.Contains("Ability: Interact", CreatureStageHUD.BuildHintText("Legacy carried forward"));
            StringAssert.Contains("Legacy carried forward", CreatureStageHUD.BuildHintText("Legacy carried forward"));
            Assert.AreEqual("Need stamina to attack: 12/18", CreatureStageHUD.BuildStaminaExhaustedLabel("attack", 12f, 18f));
            Assert.AreEqual("Need stamina to use ability: 4/38", CreatureStageHUD.BuildStaminaExhaustedLabel("ability", 4f, 38f));
            Assert.AreEqual("Need stamina to act: 0/1", CreatureStageHUD.BuildStaminaExhaustedLabel(null, -2f, 1f));
            Assert.AreEqual("Momentum: +26 stamina", CreatureStageHUD.BuildStaminaRestoredLabel("kill", 26f));
            Assert.AreEqual("Counter: +14 stamina", CreatureStageHUD.BuildStaminaRestoredLabel("counter", 14f));
            Assert.AreEqual("Objective momentum: +18 stamina", CreatureStageHUD.BuildStaminaRestoredLabel("objective", 18f));
            Assert.AreEqual("Streak x5: +4 DNA", CreatureStageHUD.BuildStreakBonusLabel(5, 4));
            Assert.AreEqual("Charm Display", CreatureStage.BuildPacifyActionLabel(CreatureVerb.Charm));
            Assert.AreEqual("Pacify Actions", CreatureStage.BuildPacifyActionLabel(null));
        }

        [Test]
        public void HealingZonePulse_StaysNormalized()
        {
            Assert.That(CreatureHealingZone.ComputePulseAlpha(0f, 2f), Is.InRange(0f, 1f));
            Assert.That(CreatureHealingZone.ComputePulseAlpha(10f, 2f), Is.InRange(0f, 1f));
        }

        [Test]
        public void UpgradeChoiceLabels_SurfaceTagsAndTradeoffs()
        {
            var option = new CreatureUpgradeOption
            {
                Title = "Neural Overdrive",
                Description = "Recover stamina faster.",
                Tag = "control",
                Tradeoff = "Lower health."
            };

            Assert.AreEqual("Neural Overdrive [control]", CreatureUpgradeChoiceUI.BuildChoiceTitle(option));
            StringAssert.Contains("Tradeoff: Lower health.", CreatureUpgradeChoiceUI.BuildChoiceDescription(option));
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
        public void TryResolveDispositionIntentOverride_DeescalatesFriendlySpecies()
        {
            bool nearOverride = CreatureRivalAI.TryResolveDispositionIntentOverride(
                CreatureSpeciesDisposition.Friendly,
                playerDistance: 1.8f,
                aggroRange: 14f,
                attackRange: 2f,
                recentlyDamaged: false,
                out CreatureRivalIntent nearIntent);
            Assert.IsTrue(nearOverride);
            Assert.AreEqual(CreatureRivalIntent.KeepDistance, nearIntent);

            bool midOverride = CreatureRivalAI.TryResolveDispositionIntentOverride(
                CreatureSpeciesDisposition.Friendly,
                playerDistance: 6f,
                aggroRange: 14f,
                attackRange: 2f,
                recentlyDamaged: false,
                out CreatureRivalIntent midIntent);
            Assert.IsTrue(midOverride);
            Assert.AreEqual(CreatureRivalIntent.Wander, midIntent);
        }

        [Test]
        public void TryResolveDispositionIntentOverride_EscalatesHostileSpecies()
        {
            bool hostileOverride = CreatureRivalAI.TryResolveDispositionIntentOverride(
                CreatureSpeciesDisposition.Hostile,
                playerDistance: 12f,
                aggroRange: 10f,
                attackRange: 2f,
                recentlyDamaged: false,
                out CreatureRivalIntent hostileIntent);
            Assert.IsTrue(hostileOverride);
            Assert.AreEqual(CreatureRivalIntent.Chase, hostileIntent);
        }

        [Test]
        public void TryResolveDispositionIntentOverride_YieldsToRecentDamage()
        {
            bool overrideApplied = CreatureRivalAI.TryResolveDispositionIntentOverride(
                CreatureSpeciesDisposition.Allied,
                playerDistance: 1f,
                aggroRange: 12f,
                attackRange: 2f,
                recentlyDamaged: true,
                out CreatureRivalIntent intent);
            Assert.IsFalse(overrideApplied);
            Assert.AreEqual(CreatureRivalIntent.Wander, intent);
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
        public void ComputeNearestRivalDistanceFlat_IgnoresVerticalDifference()
        {
            Vector3[] rivals =
            {
                new Vector3(3f, 99f, 4f),
                new Vector3(12f, 0f, 0f)
            };

            float nearest = CreatureStageEncounterManager.ComputeNearestRivalDistanceFlat(Vector3.zero, rivals);

            Assert.That(nearest, Is.EqualTo(5f).Within(0.001f));
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
