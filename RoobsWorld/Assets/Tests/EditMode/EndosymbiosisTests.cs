using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for endosymbiosis trait selection and localization messaging.
    /// </summary>
    public class EndosymbiosisTests
    {
        [Test]
        public void SelectTraitForEndosymbiosis_NoneInput_ReturnsNone()
        {
            var selected = PlayerCell.SelectTraitForEndosymbiosis(GeneticTraits.None, new System.Random(7));
            Assert.AreEqual(GeneticTraits.None, selected);
        }

        [Test]
        public void SelectTraitForEndosymbiosis_SingleTrait_ReturnsThatTrait()
        {
            var selected = PlayerCell.SelectTraitForEndosymbiosis(GeneticTraits.Toxic, new System.Random(7));
            Assert.AreEqual(GeneticTraits.Toxic, selected);
        }

        [Test]
        public void SelectTraitForEndosymbiosis_MultiTrait_ReturnsSingleFlagFromSet()
        {
            var available = GeneticTraits.Photosynthetic | GeneticTraits.Electric | GeneticTraits.Regenerative;
            var selected = PlayerCell.SelectTraitForEndosymbiosis(available, new System.Random(13));
            Assert.AreNotEqual(GeneticTraits.None, selected);
            Assert.IsTrue((available & selected) != 0, "Selected trait must come from the available mask.");
            int selectedBits = (int)selected;
            Assert.AreEqual(0, selectedBits & (selectedBits - 1), "Selected value should be a single flag.");
        }

        [Test]
        public void SelectTraitForEndosymbiosis_SeededRng_IsDeterministic()
        {
            var available = GeneticTraits.Photosynthetic | GeneticTraits.Toxic | GeneticTraits.Electric;
            var a = PlayerCell.SelectTraitForEndosymbiosis(available, new System.Random(42));
            var b = PlayerCell.SelectTraitForEndosymbiosis(available, new System.Random(42));
            Assert.AreEqual(a, b);
        }

        [Test]
        public void SelectTraitForEndosymbiosis_PreferredTraitBias_CanChoosePreferred()
        {
            var available = GeneticTraits.Photosynthetic | GeneticTraits.Toxic | GeneticTraits.Electric;
            var selected = PlayerCell.SelectTraitForEndosymbiosis(
                available,
                GeneticTraits.Electric,
                new System.Random(1));
            Assert.AreEqual(GeneticTraits.Electric, selected);
        }

        [Test]
        public void DetermineSymbiosisPreferredTrait_ExplicitTrait_Wins()
        {
            var prey = Genome.Default();
            prey.Traits = GeneticTraits.Toxic;
            prey.Digestion = 9f; // would otherwise suggest photosynthetic in heuristic path

            var preferred = PlayerCell.DetermineSymbiosisPreferredTrait(prey);
            Assert.AreEqual(GeneticTraits.Toxic, preferred);
        }

        [Test]
        public void DetermineSymbiosisPreferredTrait_HighDigestionWithoutTrait_PrefersPhotosynthetic()
        {
            var prey = Genome.Default();
            prey.Traits = GeneticTraits.None;
            prey.Digestion = 6.8f;
            prey.Attack = 1f;
            prey.Spikes = 0f;
            prey.Defence = 1f;
            prey.Speed = 2f;

            var preferred = PlayerCell.DetermineSymbiosisPreferredTrait(prey);
            Assert.AreEqual(GeneticTraits.Photosynthetic, preferred);
        }

        [Test]
        public void DetermineSymbiosisPreferredTrait_BacteriaLikeGenome_PrefersRegenerative()
        {
            var prey = Genome.Default();
            prey.Traits = GeneticTraits.None;
            prey.Size = 0.35f;
            prey.Flagella = 6f;
            prey.Attack = 0.5f;
            prey.Digestion = 2f;

            var preferred = PlayerCell.DetermineSymbiosisPreferredTrait(prey);
            Assert.AreEqual(GeneticTraits.Regenerative, preferred);
        }

        [Test]
        public void BuildEndosymbiosisTransferPool_TraitlessPrey_IncludesPreferredFallback()
        {
            var prey = Genome.Default();
            prey.Traits = GeneticTraits.None;
            prey.Size = 0.35f;
            prey.Flagella = 6f;
            prey.Attack = 0.5f;

            var pool = PlayerCell.BuildEndosymbiosisTransferPool(prey, GeneticTraits.None);
            Assert.AreEqual(GeneticTraits.Regenerative, pool);
        }

        [Test]
        public void BuildEndosymbiosisTransferPool_DoesNotOfferAlreadyOwnedPreferredTrait()
        {
            var prey = Genome.Default();
            prey.Traits = GeneticTraits.None;
            prey.Size = 0.35f;
            prey.Flagella = 6f;
            prey.Attack = 0.5f;

            var pool = PlayerCell.BuildEndosymbiosisTransferPool(prey, GeneticTraits.Regenerative);
            Assert.AreEqual(GeneticTraits.None, pool);
        }

        [Test]
        public void GetDigestiveGainMultiplier_RegenerativeTrait_GivesBonus()
        {
            var noTrait = Genome.Default();
            noTrait.Traits = GeneticTraits.None;
            Assert.AreEqual(1f, PlayerCell.GetDigestiveGainMultiplier(noTrait), 0.0001f);

            var regenerative = Genome.Default();
            regenerative.Traits = GeneticTraits.Regenerative;
            Assert.Greater(PlayerCell.GetDigestiveGainMultiplier(regenerative), 1f);
        }

        [Test]
        public void GetDigestiveGainMultiplier_RegenerativeLevel_StacksBonus()
        {
            var regenerative = Genome.Default();
            regenerative.Traits = GeneticTraits.Regenerative;
            float baseBonus = PlayerCell.GetDigestiveGainMultiplier(regenerative, 0);
            float stackedBonus = PlayerCell.GetDigestiveGainMultiplier(regenerative, 2);
            Assert.Greater(stackedBonus, baseBonus);
        }

        [Test]
        public void UpgradeSymbiosisLevel_ClampsAtMaximum()
        {
            int lvl = 0;
            lvl = PlayerCell.UpgradeSymbiosisLevel(lvl);
            lvl = PlayerCell.UpgradeSymbiosisLevel(lvl);
            lvl = PlayerCell.UpgradeSymbiosisLevel(lvl);
            lvl = PlayerCell.UpgradeSymbiosisLevel(lvl);
            Assert.AreEqual(3, lvl);
            Assert.IsFalse(PlayerCell.CanUpgradeSymbiosisLevel(lvl));
        }

        [Test]
        public void GetTraitPassiveMultiplier_IncreasesWithLevel()
        {
            float m0 = PlayerCell.GetTraitPassiveMultiplier(0);
            float m2 = PlayerCell.GetTraitPassiveMultiplier(2);
            Assert.Greater(m2, m0);
        }

        [Test]
        public void GetElectricPulseInterval_DecreasesWithLevel()
        {
            float baseInterval = BalanceConstants.GeneticTraitElectricPulseInterval;
            float level0 = PlayerCell.GetElectricPulseInterval(baseInterval, 0);
            float level2 = PlayerCell.GetElectricPulseInterval(baseInterval, 2);
            Assert.Less(level2, level0);
            Assert.Greater(level2, 0.1f);
        }

        [Test]
        public void GetEndosymbiosisTipKey_ReturnsTraitSpecificKey()
        {
            Assert.AreEqual(
                "cell.endosymbiosis.acquired.photosynthetic",
                PlayerCell.GetEndosymbiosisTipKey(GeneticTraits.Photosynthetic));
            Assert.AreEqual(
                "cell.endosymbiosis.acquired.toxic",
                PlayerCell.GetEndosymbiosisTipKey(GeneticTraits.Toxic));
            Assert.AreEqual(
                "cell.endosymbiosis.acquired.armoured",
                PlayerCell.GetEndosymbiosisTipKey(GeneticTraits.Armoured));
            Assert.AreEqual(
                "cell.endosymbiosis.acquired.regenerative",
                PlayerCell.GetEndosymbiosisTipKey(GeneticTraits.Regenerative));
            Assert.AreEqual(
                "cell.endosymbiosis.acquired.electric",
                PlayerCell.GetEndosymbiosisTipKey(GeneticTraits.Electric));
            Assert.AreEqual(
                "cell.endosymbiosis.acquired",
                PlayerCell.GetEndosymbiosisTipKey(GeneticTraits.None));
        }

        [Test]
        public void Endosymbiosis_LocalizationKey_IsPresent()
        {
            string value = Localization.Get("cell.endosymbiosis.acquired");
            Assert.IsFalse(string.IsNullOrEmpty(value));
            Assert.AreNotEqual("cell.endosymbiosis.acquired", value);

            string upgraded = Localization.Get("cell.endosymbiosis.upgraded");
            Assert.IsFalse(string.IsNullOrEmpty(upgraded));
            Assert.AreNotEqual("cell.endosymbiosis.upgraded", upgraded);

            string hudLabel = Localization.Get("cell.symbiosis.label");
            Assert.IsFalse(string.IsNullOrEmpty(hudLabel));
            Assert.AreNotEqual("cell.symbiosis.label", hudLabel);
        }

        [Test]
        public void Endosymbiosis_TraitSpecificLocalizationKeys_ArePresent()
        {
            string[] keys =
            {
                "cell.endosymbiosis.acquired.photosynthetic",
                "cell.endosymbiosis.acquired.toxic",
                "cell.endosymbiosis.acquired.armoured",
                "cell.endosymbiosis.acquired.regenerative",
                "cell.endosymbiosis.acquired.electric"
            };

            for (int i = 0; i < keys.Length; i++)
            {
                string value = Localization.Get(keys[i]);
                Assert.IsFalse(string.IsNullOrEmpty(value), $"Missing localization for {keys[i]}");
                Assert.AreNotEqual(keys[i], value);
            }
        }
    }
}
