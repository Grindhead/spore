using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for ParasiteCell: tuning constants, genome defaults, trophic classification, and localization keys.
    /// </summary>
    public class ParasiteCellTests
    {
        [Test]
        public void HostSeekRadius_IsLargerThanAttachRadius()
        {
            Assert.Greater(ParasiteCell.HostSeekRadius, ParasiteCell.AttachRadius);
        }

        [Test]
        public void DetachRadius_IsLargerThanAttachRadius()
        {
            Assert.Greater(ParasiteCell.DetachRadius, ParasiteCell.AttachRadius);
        }

        [Test]
        public void DrainPerSecond_IsPositive()
        {
            Assert.Greater(ParasiteCell.DrainPerSecond, 0f);
        }

        [Test]
        public void MaxAttachDuration_IsPositive()
        {
            Assert.Greater(ParasiteCell.MaxAttachDuration, 0f);
        }

        [Test]
        public void SeekTickInterval_IsPositive()
        {
            Assert.Greater(ParasiteCell.SeekTickInterval, 0f);
        }

        [Test]
        public void WanderSpeedFraction_IsLowerThanChaseSpeed()
        {
            Assert.Greater(ParasiteCell.ChaseSpeedFraction, ParasiteCell.WanderSpeedFraction);
        }

        [Test]
        public void MinimumHostSizeRatio_IsGreaterThanOne()
        {
            Assert.Greater(ParasiteCell.MinimumHostSizeRatio, 1f);
        }

        [Test]
        public void BuildGenome_HasSmallBody()
        {
            var genome = ParasiteCell.BuildGenome();
            Assert.Greater(genome.Size, 0f);
            Assert.Less(genome.Size, 1f);
        }

        [Test]
        public void BuildGenome_HasModerateSpeedAndAttack()
        {
            var genome = ParasiteCell.BuildGenome();
            Assert.Greater(genome.Speed, 3f);
            Assert.Greater(genome.Attack, 1f);
        }

        [Test]
        public void BuildGenome_ClassifiesAsOmnivore()
        {
            var genome = ParasiteCell.BuildGenome();
            Assert.AreEqual(CellStage.TrophicRole.Omnivore, CellStage.ClassifyTrophicRole(genome));
        }

        [Test]
        public void ArchetypeColor_IsDistinctFromOtherArchetypes()
        {
            Assert.AreNotEqual(FilterFeederCell.ArchetypeColor, ParasiteCell.ArchetypeColor);
            Assert.AreNotEqual(BacteriaSwarmCell.ArchetypeColor, ParasiteCell.ArchetypeColor);
            Assert.AreNotEqual(ScavengerCell.ArchetypeColor, ParasiteCell.ArchetypeColor);
            Assert.AreNotEqual(new Color(0.5f, 0f, 0.5f), ParasiteCell.ArchetypeColor);
        }

        [Test]
        public void LocalizationKey_ParasiteName_Exists()
        {
            var value = Localization.Get("npc.parasite.name");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.parasite.name", value);
        }

        [Test]
        public void LocalizationKey_ParasiteTooltip_Exists()
        {
            var value = Localization.Get("npc.parasite.tooltip");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("npc.parasite.tooltip", value);
        }
    }
}
