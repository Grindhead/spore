using NUnit.Framework;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class PlayerCellPredationRulesTests
    {
        [Test]
        public void ShouldDieFromFailedConsumeAttempt_ReturnsTrue_WhenTargetIsLarger()
        {
            bool shouldDie = PlayerCell.ShouldDieFromFailedConsumeAttempt(
                playerScale: 1f,
                targetScale: 1.1f,
                consumeSizeRatio: 0.8f);

            Assert.IsTrue(shouldDie);
        }

        [Test]
        public void ShouldDieFromFailedConsumeAttempt_ReturnsFalse_WhenTargetIsConsumable()
        {
            bool shouldDie = PlayerCell.ShouldDieFromFailedConsumeAttempt(
                playerScale: 1f,
                targetScale: 0.7f,
                consumeSizeRatio: 0.8f);

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void ShouldDieFromFailedConsumeAttempt_ReturnsFalse_WhenTargetIsNotConsumableButNotLarger()
        {
            bool shouldDie = PlayerCell.ShouldDieFromFailedConsumeAttempt(
                playerScale: 1f,
                targetScale: 0.9f,
                consumeSizeRatio: 0.8f);

            Assert.IsFalse(shouldDie);
        }

        [Test]
        public void CanConsumeGoalLargerPreyTarget_ReturnsTrue_ForSmallLargerMarkedPrey()
        {
            bool canConsume = PlayerCell.CanConsumeGoalLargerPreyTarget(
                playerScale: 1f,
                targetScale: 1.08f);

            Assert.IsTrue(canConsume);
        }

        [Test]
        public void CanConsumeGoalLargerPreyTarget_ReturnsFalse_ForSmallerOrTooLargeTargets()
        {
            Assert.IsFalse(PlayerCell.CanConsumeGoalLargerPreyTarget(1f, 0.95f));
            Assert.IsFalse(PlayerCell.CanConsumeGoalLargerPreyTarget(1f, 1.25f));
        }

        [Test]
        public void ComputePredatorEscapeBoostSeconds_AppliesMultipliersAndAllowsZero()
        {
            Assert.That(
                PlayerCell.ComputePredatorEscapeBoostSeconds(
                    baseBoostDuration: 0.42f,
                    feelMultiplier: 1f,
                    partMultiplier: 1.2f,
                    escapeMultiplier: 1.35f),
                Is.EqualTo(0.6804f).Within(0.0001f));
            Assert.That(
                PlayerCell.ComputePredatorEscapeBoostSeconds(
                    baseBoostDuration: 0f,
                    feelMultiplier: 1f,
                    partMultiplier: 1f,
                    escapeMultiplier: 1.35f),
                Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void PredatorEscapeSurgeText_ShowsBoostWindow()
        {
            Assert.AreEqual("Escape surge! Boost 0.7s", CellStageHUD.BuildPredatorEscapeSurgeText(0.68f));
        }
    }
}
