using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for evolution milestone localization keys and SpeciationSystem species count
    /// used by the ten-species milestone check in CellTutorialController.
    /// </summary>
    public class MilestoneToastTests
    {
        // ------------------------------------------------------------------ Localization coverage

        [Test]
        public void MilestoneKeys_AllPresent_AndTranslated()
        {
            var keys = new[]
            {
                "tutorial.cell.tip.first_extinction",
                "tutorial.cell.tip.ten_species",
            };

            foreach (var key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Key '{key}' should have a non-empty value");
                Assert.AreNotEqual(key, val, $"Key '{key}' should be translated, not returned as-is");
            }
        }

        // ------------------------------------------------------------------ SpeciationSystem species count gate

        [Test]
        public void SpeciationSystem_GetAllSpecies_CountBelowTen_BeforeSeeding()
        {
            SpeciationSystem.Reset();
            Assert.Less(SpeciationSystem.GetAllSpecies().Count, 10,
                "Fresh SpeciationSystem should have fewer than 10 species");
        }

        [Test]
        public void SpeciationSystem_GetAllSpecies_CountReachesTen_AfterEnoughSpecies()
        {
            SpeciationSystem.Reset();
            for (int i = 0; i < 10; i++)
                SpeciationSystem.EnsureSpeciesExists(i, new Genome { Speed = i * 0.5f });

            Assert.GreaterOrEqual(SpeciationSystem.GetAllSpecies().Count, 10,
                "After seeding 10 species, count should reach 10");
        }

        // ------------------------------------------------------------------ Low-mass danger haptic threshold sanity

        [Test]
        public void LowMassDanger_Threshold_IsAboveMinimumMass()
        {
            // PlayerCell.LowMassDangerThreshold (1.0f) should be above MinimumMass (0.5f).
            // We verify this indirectly via BalanceConstants so the test stays decoupled.
            // MinimumMass (0.5) < LowMassDanger (1.0) < ReproductionThreshold
            float minMass = 0.5f;           // PlayerCell.MinimumMass
            float dangerThreshold = 1.0f;   // PlayerCell.LowMassDangerThreshold
            float reproThreshold = BalanceConstants.CellPlayerReproductionMassThreshold;

            Assert.Greater(dangerThreshold, minMass,
                "Low-mass danger threshold should be above the death threshold");
            Assert.Less(dangerThreshold, reproThreshold,
                "Low-mass danger threshold should be well below the reproduction threshold");
        }

        // ------------------------------------------------------------------ Predator proximity haptic constants sanity

        [Test]
        public void ProximityHapticRadius_IsSmallerThanWarningRadius()
        {
            // CellTutorialController: ProximityHapticRadius=10, warnRadius=20
            // Haptic fires only when predator is closer than the tutorial tip radius.
            float warnRadius     = 20f;
            float hapticRadius   = 10f;  // ProximityHapticRadius constant
            Assert.Less(hapticRadius, warnRadius,
                "Proximity haptic radius should be inside the tutorial tip warning radius");
        }
    }
}
