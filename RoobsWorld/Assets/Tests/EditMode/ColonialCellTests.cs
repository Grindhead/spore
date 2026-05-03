using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for ColonialCell archetype tuning, genome defaults, trophic role and localization.
    /// </summary>
    public class ColonialCellTests
    {
        [Test]
        public void NeighborRadius_IsGreaterThanSeparationRadius()
        {
            Assert.Greater(ColonialCell.ColonyNeighborRadius, ColonialCell.SeparationRadius);
        }

        [Test]
        public void EnergyShareRadius_IsPositive_AndWithinNeighborRadius()
        {
            Assert.Greater(ColonialCell.EnergyShareRadius, 0f);
            Assert.LessOrEqual(ColonialCell.EnergyShareRadius, ColonialCell.ColonyNeighborRadius);
        }

        [Test]
        public void ColonyWeights_ArePositive()
        {
            Assert.Greater(ColonialCell.CohesionWeight, 0f);
            Assert.Greater(ColonialCell.SeparationWeight, 0f);
            Assert.Greater(ColonialCell.AlignmentWeight, 0f);
            Assert.Greater(ColonialCell.WanderWeight, 0f);
        }

        [Test]
        public void BuildGenome_HasExpectedShapeAndStats()
        {
            var genome = ColonialCell.BuildGenome();
            Assert.NotNull(genome);
            Assert.Greater(genome.Size, 0.3f);
            Assert.Greater(genome.Speed, 2f);
            Assert.Greater(genome.Attack, 1f);
            Assert.Greater(genome.Digestion, 1f);
            Assert.AreEqual(GeneticTraits.None, genome.Traits);
        }

        [Test]
        public void BuildGenome_ClassifiesAsOmnivore()
        {
            var genome = ColonialCell.BuildGenome();
            Assert.AreEqual(CellStage.TrophicRole.Omnivore, CellStage.ClassifyTrophicRole(genome));
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromPhotosynthetic()
        {
            Assert.AreNotEqual(PhotosyntheticCell.ArchetypeColor, ColonialCell.ArchetypeColor);
            Assert.AreNotEqual(new Color(0f, 1f, 0f), ColonialCell.ArchetypeColor);
        }

        [Test]
        public void LocalizationKey_ColonialName_Exists()
        {
            var value = Localization.Get("npc.colonial.name");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.colonial.name", value);
        }

        [Test]
        public void LocalizationKey_ColonialTooltip_Exists()
        {
            var value = Localization.Get("npc.colonial.tooltip");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.colonial.tooltip", value);
        }
    }
}
