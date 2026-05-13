using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class ThreatFeedbackTests
    {
        [Test]
        public void ResolvePredatorMood_PrioritizesCombat_WhenChasing()
        {
            MusicMood mood = CellTutorialController.ResolvePredatorMood(anyChasing: true, anyNearby: true);
            Assert.AreEqual(MusicMood.Combat, mood);
        }

        [Test]
        public void ResolvePredatorMood_UsesTense_WhenNearbyButNotChasing()
        {
            MusicMood mood = CellTutorialController.ResolvePredatorMood(anyChasing: false, anyNearby: true);
            Assert.AreEqual(MusicMood.Tense, mood);
        }

        [Test]
        public void ResolvePredatorMood_ReturnsExploration_WhenNoThreat()
        {
            MusicMood mood = CellTutorialController.ResolvePredatorMood(anyChasing: false, anyNearby: false);
            Assert.AreEqual(MusicMood.Exploration, mood);
        }

        [Test]
        public void ComputeAlertInterval_DecreasesAsIntensityRises()
        {
            float low = ThreatAudioFeedback.ComputeAlertInterval(0f, minInterval: 0.6f, maxInterval: 2.4f);
            float high = ThreatAudioFeedback.ComputeAlertInterval(1f, minInterval: 0.6f, maxInterval: 2.4f);

            Assert.Greater(low, high);
            Assert.AreEqual(2.4f, low, 0.0001f);
            Assert.AreEqual(0.6f, high, 0.0001f);
        }

        [Test]
        public void ComputeAlertVolume_IncreasesAsIntensityRises()
        {
            float low = ThreatAudioFeedback.ComputeAlertVolume(0f, minVolume: 0.15f, maxVolume: 0.8f);
            float high = ThreatAudioFeedback.ComputeAlertVolume(1f, minVolume: 0.15f, maxVolume: 0.8f);

            Assert.Less(low, high);
            Assert.AreEqual(0.15f, low, 0.0001f);
            Assert.AreEqual(0.8f, high, 0.0001f);
        }

        [Test]
        public void ComputeAlertHelpers_HandleReversedBounds()
        {
            float interval = ThreatAudioFeedback.ComputeAlertInterval(0.5f, minInterval: 2f, maxInterval: 1f);
            float volume = ThreatAudioFeedback.ComputeAlertVolume(0.5f, minVolume: 0.9f, maxVolume: 0.1f);

            Assert.AreEqual(1.5f, interval, 0.0001f);
            Assert.AreEqual(0.5f, volume, 0.0001f);
        }

        [Test]
        public void EvaluateThreatIntensity_ReturnsOne_InsideDetectionRadius()
        {
            float intensity = ThreatIndicator.EvaluateThreatIntensity(
                distance: 3f,
                detectionRadius: 5f,
                warningRadius: 12f,
                warningZoneMaxIntensity: 0.6f);

            Assert.AreEqual(1f, intensity, 0.0001f);
        }

        [Test]
        public void EvaluateThreatIntensity_ReturnsZero_OutsideWarningRadius()
        {
            float intensity = ThreatIndicator.EvaluateThreatIntensity(
                distance: 15f,
                detectionRadius: 5f,
                warningRadius: 12f,
                warningZoneMaxIntensity: 0.6f);

            Assert.AreEqual(0f, intensity, 0.0001f);
        }

        [Test]
        public void EvaluateThreatIntensity_InterpolatesInWarningBand()
        {
            float intensity = ThreatIndicator.EvaluateThreatIntensity(
                distance: 8.5f,
                detectionRadius: 5f,
                warningRadius: 12f,
                warningZoneMaxIntensity: 0.6f);

            Assert.Greater(intensity, 0f);
            Assert.Less(intensity, 0.6f);
        }

        [Test]
        public void EvaluateThreatIntensityFromSqrDistance_MatchesDistanceVariant()
        {
            const float distance = 7.5f;
            float fromDistance = ThreatIndicator.EvaluateThreatIntensity(
                distance: distance,
                detectionRadius: 5f,
                warningRadius: 12f,
                warningZoneMaxIntensity: 0.6f);
            float fromSqr = ThreatIndicator.EvaluateThreatIntensityFromSqrDistance(
                sqrDistance: distance * distance,
                detectionRadius: 5f,
                warningRadius: 12f,
                warningZoneMaxIntensity: 0.6f);

            Assert.AreEqual(fromDistance, fromSqr, 0.0001f);
        }

        [Test]
        public void GetThreatCueLabel_DifferentiatesHazards()
        {
            Assert.AreEqual("Predator", ThreatIndicator.GetThreatCueLabel(CellThreatCueType.Predator));
            Assert.AreEqual("Virus", ThreatIndicator.GetThreatCueLabel(CellThreatCueType.Virus));
            Assert.AreEqual("Toxin", ThreatIndicator.GetThreatCueLabel(CellThreatCueType.ToxicHazard));
            Assert.AreEqual("Leviathan", ThreatIndicator.GetThreatCueLabel(CellThreatCueType.Leviathan));
        }

        [Test]
        public void ComputeTenseState_EntersTense_WhenIntensityCrossesEnterThreshold()
        {
            bool tense = ThreatAudioFeedback.ComputeTenseState(
                currentlyTense: false,
                threatIntensity: 0.4f,
                enterThreshold: 0.3f,
                exitThreshold: 0.15f,
                isThreatened: true);
            Assert.IsTrue(tense);
        }

        [Test]
        public void ComputeTenseState_HoldsTense_AboveExitThreshold()
        {
            bool tense = ThreatAudioFeedback.ComputeTenseState(
                currentlyTense: true,
                threatIntensity: 0.2f,
                enterThreshold: 0.3f,
                exitThreshold: 0.15f,
                isThreatened: true);
            Assert.IsTrue(tense);
        }

        [Test]
        public void ComputeTenseState_ExitsTense_WhenBelowExitThresholdOrNotThreatened()
        {
            bool belowExit = ThreatAudioFeedback.ComputeTenseState(
                currentlyTense: true,
                threatIntensity: 0.1f,
                enterThreshold: 0.3f,
                exitThreshold: 0.15f,
                isThreatened: true);
            bool notThreatened = ThreatAudioFeedback.ComputeTenseState(
                currentlyTense: true,
                threatIntensity: 0.8f,
                enterThreshold: 0.3f,
                exitThreshold: 0.15f,
                isThreatened: false);

            Assert.IsFalse(belowExit);
            Assert.IsFalse(notThreatened);
        }

        [Test]
        public void ShouldTriggerProximityPulse_RequiresNoCooldown_AndNearbyDistance()
        {
            bool shouldTrigger = CellTutorialController.ShouldTriggerProximityPulse(
                cooldownRemaining: 0f,
                distanceSqr: 9f,
                proximityRadiusSqr: 16f);
            bool blockedByCooldown = CellTutorialController.ShouldTriggerProximityPulse(
                cooldownRemaining: 0.2f,
                distanceSqr: 9f,
                proximityRadiusSqr: 16f);
            bool blockedByDistance = CellTutorialController.ShouldTriggerProximityPulse(
                cooldownRemaining: 0f,
                distanceSqr: 25f,
                proximityRadiusSqr: 16f);

            Assert.IsTrue(shouldTrigger);
            Assert.IsFalse(blockedByCooldown);
            Assert.IsFalse(blockedByDistance);
        }

        [Test]
        public void ShouldRestoreExploration_ReturnsTrue_WhenPreviouslyTenseAndNotCombat()
        {
            bool shouldRestore = ThreatAudioFeedback.ShouldRestoreExploration(
                currentMood: MusicMood.Tense,
                isCombatActive: false,
                tenseWasActive: true);

            Assert.IsTrue(shouldRestore);
        }

        [Test]
        public void ShouldRestoreExploration_ReturnsFalse_WhenCombatOrNotTense()
        {
            bool combatActive = ThreatAudioFeedback.ShouldRestoreExploration(
                currentMood: MusicMood.Tense,
                isCombatActive: true,
                tenseWasActive: true);
            bool notPreviouslyTense = ThreatAudioFeedback.ShouldRestoreExploration(
                currentMood: MusicMood.Exploration,
                isCombatActive: false,
                tenseWasActive: false);

            Assert.IsFalse(combatActive);
            Assert.IsFalse(notPreviouslyTense);
        }

        [Test]
        public void ShouldAttemptMusicControllerLookup_ReturnsTrue_WhenNullAndDue()
        {
            bool shouldLookup = ThreatAudioFeedback.ShouldAttemptMusicControllerLookup(
                musicController: null,
                now: 5f,
                nextLookupTime: 4.5f);

            Assert.IsTrue(shouldLookup);
        }

        [Test]
        public void ShouldAttemptMusicControllerLookup_ReturnsFalse_WhenNotDueOrAlreadyResolved()
        {
            bool notDue = ThreatAudioFeedback.ShouldAttemptMusicControllerLookup(
                musicController: null,
                now: 4f,
                nextLookupTime: 4.5f);

            var go = new UnityEngine.GameObject("MusicControllerStub");
            var controller = go.AddComponent<AdaptiveMusicController>();
            bool alreadyResolved = ThreatAudioFeedback.ShouldAttemptMusicControllerLookup(
                musicController: controller,
                now: 10f,
                nextLookupTime: 0f);
            UnityEngine.Object.DestroyImmediate(go);

            Assert.IsFalse(notDue);
            Assert.IsFalse(alreadyResolved);
        }
    }
}
