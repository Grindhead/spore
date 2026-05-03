using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for PhotosyntheticCell archetype tuning, genome defaults, trophic role, and localization keys.
    /// </summary>
    public class PhotosyntheticCellTests
    {
        [Test]
        public void PlantAbsorptionRadius_IsPositive()
        {
            Assert.Greater(PhotosyntheticCell.PlantAbsorptionRadius, 0f);
        }

        [Test]
        public void AbsorptionTickInterval_IsPositive()
        {
            Assert.Greater(PhotosyntheticCell.AbsorptionTickInterval, 0f);
        }

        [Test]
        public void WanderSpeedFraction_IsInReasonableRange()
        {
            Assert.Greater(PhotosyntheticCell.WanderSpeedFraction, 0f);
            Assert.Less(PhotosyntheticCell.WanderSpeedFraction, 1f);
        }

        [Test]
        public void LightSeekingStrength_IsInRange()
        {
            Assert.GreaterOrEqual(PhotosyntheticCell.LightSeekingStrength, 0f);
            Assert.LessOrEqual(PhotosyntheticCell.LightSeekingStrength, 1f);
        }

        [Test]
        public void DriftParameters_ArePositive()
        {
            Assert.Greater(PhotosyntheticCell.DriftOscillationRate, 0f);
            Assert.Greater(PhotosyntheticCell.DriftHeadingVariance, 0f);
        }

        [Test]
        public void BuildGenome_HasPhotosyntheticTrait()
        {
            var genome = PhotosyntheticCell.BuildGenome();
            Assert.IsTrue(genome.HasTrait(GeneticTraits.Photosynthetic));
        }

        [Test]
        public void BuildGenome_IsLowAttackAndLowMetabolism()
        {
            var genome = PhotosyntheticCell.BuildGenome();
            Assert.Less(genome.Attack, 1f);
            Assert.Less(genome.MetabolismCost, 1f);
        }

        [Test]
        public void BuildGenome_ClassifiesAsHerbivore()
        {
            var genome = PhotosyntheticCell.BuildGenome();
            Assert.AreEqual(CellStage.TrophicRole.Herbivore, CellStage.ClassifyTrophicRole(genome));
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromOtherProducerArchetypes()
        {
            Assert.AreNotEqual(FilterFeederCell.ArchetypeColor, PhotosyntheticCell.ArchetypeColor);
            Assert.AreNotEqual(BacteriaSwarmCell.ArchetypeColor, PhotosyntheticCell.ArchetypeColor);
        }

        [Test]
        public void BuildGenome_HasValidRanges()
        {
            var genome = PhotosyntheticCell.BuildGenome();
            Assert.Greater(genome.Speed, 0f);
            Assert.Greater(genome.Size, 0f);
            Assert.GreaterOrEqual(genome.Digestion, 0f);
        }

        [Test]
        public void LocalizationKey_PhotosyntheticName_Exists()
        {
            string value = Localization.Get("npc.photosynthetic.name");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.photosynthetic.name", value);
        }

        [Test]
        public void LocalizationKey_PhotosyntheticTooltip_Exists()
        {
            string value = Localization.Get("npc.photosynthetic.tooltip");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.photosynthetic.tooltip", value);
        }
    }
}
