using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for CellStage.ClassifyTrophicRole and the trophic balance logic
    /// that drives stronger niche filling.
    /// </summary>
    public class TrophicRoleTests
    {
        // ------------------------------------------------------------------ ClassifyTrophicRole

        [Test]
        public void HighDigestion_LowAttack_IsHerbivore()
        {
            var g = Genome.Default();
            g.Digestion = 8f;
            g.Attack    = 2f;
            Assert.AreEqual(CellStage.TrophicRole.Herbivore, CellStage.ClassifyTrophicRole(g));
        }

        [Test]
        public void HighAttack_LowDigestion_IsCarnivore()
        {
            var g = Genome.Default();
            g.Attack    = 8f;
            g.Digestion = 2f;
            Assert.AreEqual(CellStage.TrophicRole.Carnivore, CellStage.ClassifyTrophicRole(g));
        }

        [Test]
        public void BalancedStats_IsOmnivore()
        {
            var g = Genome.Default();
            g.Attack    = 5f;
            g.Digestion = 5f;
            Assert.AreEqual(CellStage.TrophicRole.Omnivore, CellStage.ClassifyTrophicRole(g));
        }

        [Test]
        public void ExactThresholdBoundary_IsOmnivore()
        {
            // Digestion only 1.9 above Attack — does not reach the 2.0 threshold.
            var g = Genome.Default();
            g.Digestion = 6.9f;
            g.Attack    = 5f;
            Assert.AreEqual(CellStage.TrophicRole.Omnivore, CellStage.ClassifyTrophicRole(g));
        }

        [Test]
        public void ExactThreshold_IsHerbivore()
        {
            var g = Genome.Default();
            g.Digestion = 7f;
            g.Attack    = 5f;  // 7 - 5 = 2, exactly at threshold
            Assert.AreEqual(CellStage.TrophicRole.Herbivore, CellStage.ClassifyTrophicRole(g));
        }

        [Test]
        public void DefaultGenome_Classifies()
        {
            // Default genome should classify without throwing.
            var g = Genome.Default();
            var role = CellStage.ClassifyTrophicRole(g);
            Assert.IsTrue(
                role == CellStage.TrophicRole.Herbivore ||
                role == CellStage.TrophicRole.Carnivore ||
                role == CellStage.TrophicRole.Omnivore,
                "Default genome should classify into a known TrophicRole");
        }

        // ------------------------------------------------------------------ TrophicRole enum coverage

        [Test]
        public void TrophicRole_HasThreeValues()
        {
            var values = System.Enum.GetValues(typeof(CellStage.TrophicRole));
            Assert.AreEqual(3, values.Length, "TrophicRole should have Herbivore, Carnivore, Omnivore");
        }
    }
}
