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
    }
}
