using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class GeneticTraitsTests
    {
        // ---- Genome helpers ----

        [Test]
        public void HasTrait_ReturnsFalse_WhenNone()
        {
            var g = new Genome { Traits = GeneticTraits.None };
            Assert.IsFalse(g.HasTrait(GeneticTraits.Photosynthetic));
            Assert.IsFalse(g.HasTrait(GeneticTraits.Electric));
        }

        [Test]
        public void HasTrait_ReturnsTrue_WhenFlagSet()
        {
            var g = new Genome { Traits = GeneticTraits.Armoured | GeneticTraits.Regenerative };
            Assert.IsTrue(g.HasTrait(GeneticTraits.Armoured));
            Assert.IsTrue(g.HasTrait(GeneticTraits.Regenerative));
            Assert.IsFalse(g.HasTrait(GeneticTraits.Toxic));
        }

        [Test]
        public void TraitCount_MatchesSetBits()
        {
            var g = new Genome { Traits = GeneticTraits.Toxic | GeneticTraits.Electric | GeneticTraits.Armoured };
            Assert.AreEqual(3, g.TraitCount);
        }

        [Test]
        public void TraitCount_Zero_WhenNone()
        {
            var g = new Genome { Traits = GeneticTraits.None };
            Assert.AreEqual(0, g.TraitCount);
        }

        [Test]
        public void Clone_CopiesTraits()
        {
            var orig = new Genome { Traits = GeneticTraits.Photosynthetic | GeneticTraits.Toxic };
            var clone = orig.Clone();
            Assert.AreEqual(orig.Traits, clone.Traits);
        }

        // ---- FormatTraits display ----

        [Test]
        public void FormatTraits_ShowsStatsOnly_WhenNoTraits()
        {
            var g = new Genome { Speed = 3f, Size = 2f, Attack = 1f, Defence = 4f, Traits = GeneticTraits.None };
            string result = EvolutionStatsUI.FormatTraits(g);
            StringAssert.Contains("Spd", result);
            StringAssert.DoesNotContain("[", result);
        }

        [Test]
        public void FormatTraits_ShowsTraitBrackets_WhenTraitsSet()
        {
            var g = new Genome { Traits = GeneticTraits.Electric | GeneticTraits.Armoured };
            string result = EvolutionStatsUI.FormatTraits(g);
            StringAssert.Contains("[Electric]", result);
            StringAssert.Contains("[Armoured]", result);
            StringAssert.DoesNotContain("[Toxic]", result);
        }

        [Test]
        public void FormatTraits_ShowsAllFiveTraits_WhenAllSet()
        {
            var g = new Genome
            {
                Traits = GeneticTraits.Photosynthetic | GeneticTraits.Toxic |
                         GeneticTraits.Armoured | GeneticTraits.Regenerative | GeneticTraits.Electric
            };
            string result = EvolutionStatsUI.FormatTraits(g);
            StringAssert.Contains("[Photosynthetic]", result);
            StringAssert.Contains("[Toxic]",          result);
            StringAssert.Contains("[Armoured]",       result);
            StringAssert.Contains("[Regenerative]",   result);
            StringAssert.Contains("[Electric]",       result);
        }

        [Test]
        public void FormatTraits_ReturnsEmpty_WhenGenomeNull()
        {
            Assert.AreEqual("", EvolutionStatsUI.FormatTraits(null));
        }

        // ---- BalanceConstants trait values ----

        [Test]
        public void BalanceConstants_TraitValues_ArePositive()
        {
            Assert.Greater(BalanceConstants.GeneticTraitPhotosyntheticEnergyRate, 0f);
            Assert.Greater(BalanceConstants.GeneticTraitToxicContactDrain,        0f);
            Assert.Greater(BalanceConstants.GeneticTraitArmouredDamageReduction,  0f);
            Assert.Less(BalanceConstants.GeneticTraitArmouredDamageReduction,     1f);  // < 1 = not full immunity
            Assert.Greater(BalanceConstants.GeneticTraitRegenerativeRate,         0f);
            Assert.Greater(BalanceConstants.GeneticTraitElectricPulseRadius,      0f);
            Assert.Greater(BalanceConstants.GeneticTraitElectricPulseInterval,    0f);
            Assert.Greater(BalanceConstants.GeneticTraitElectricSlowDuration,     0f);
        }
    }
}
