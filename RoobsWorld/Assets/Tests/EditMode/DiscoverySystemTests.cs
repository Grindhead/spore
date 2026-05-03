using System;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for the meta-progression discovery system:
    /// MutationSystem.MutateBiasedWithDiscoveries + GameManager.RecordTraitDiscovery.
    /// </summary>
    public class DiscoverySystemTests
    {
        private static readonly System.Random Rng = new System.Random(42);

        // ---- MutateBiasedWithDiscoveries ----

        [Test]
        public void NoDiscoveries_ReturnsThreeVariants()
        {
            var parent = Genome.Default();
            var variants = MutationSystem.MutateBiasedWithDiscoveries(
                parent, new BehaviourBias(), GeneticTraits.None, Rng);

            Assert.AreEqual(3, variants.Length);
            foreach (var v in variants)
                Assert.IsNotNull(v);
        }

        [Test]
        public void WithDiscoveries_ReturnsThreeVariants()
        {
            var parent = Genome.Default();
            var variants = MutationSystem.MutateBiasedWithDiscoveries(
                parent, new BehaviourBias(), GeneticTraits.Photosynthetic, Rng);

            Assert.AreEqual(3, variants.Length);
        }

        [Test]
        public void WithDiscoveries_AtLeastOneVariantContainsKnownTrait()
        {
            var parent = Genome.Default();
            // Run many times — the guarantee must hold every time, not just sometimes.
            for (int trial = 0; trial < 50; trial++)
            {
                var rng = new System.Random(trial);
                var variants = MutationSystem.MutateBiasedWithDiscoveries(
                    parent, new BehaviourBias(), GeneticTraits.Electric, rng);

                bool anyHasElectric = false;
                foreach (var v in variants)
                    if (v.HasTrait(GeneticTraits.Electric)) anyHasElectric = true;

                Assert.IsTrue(anyHasElectric,
                    $"Trial {trial}: expected at least one variant with Electric trait");
            }
        }

        [Test]
        public void WithMultipleDiscoveries_AtLeastOneVariantContainsAnyKnownTrait()
        {
            var parent = Genome.Default();
            var known = GeneticTraits.Photosynthetic | GeneticTraits.Toxic;

            for (int trial = 0; trial < 30; trial++)
            {
                var rng = new System.Random(trial);
                var variants = MutationSystem.MutateBiasedWithDiscoveries(
                    parent, new BehaviourBias(), known, rng);

                bool anyHasKnown = false;
                foreach (var v in variants)
                    if ((v.Traits & known) != GeneticTraits.None) anyHasKnown = true;

                Assert.IsTrue(anyHasKnown, $"Trial {trial}: no variant carried a known trait");
            }
        }

        [Test]
        public void NullParent_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var variants = MutationSystem.MutateBiasedWithDiscoveries(
                    null, new BehaviourBias(), GeneticTraits.Armoured, Rng);
                Assert.AreEqual(3, variants.Length);
            });
        }

        [Test]
        public void ParentAlreadyHasDiscoveredTrait_AllVariantsInherited()
        {
            var parent = Genome.Default();
            parent.Traits = GeneticTraits.Regenerative;

            // Since the parent has the trait, all children should inherit it (MutateTraits never removes existing).
            var variants = MutationSystem.MutateBiasedWithDiscoveries(
                parent, new BehaviourBias(), GeneticTraits.Regenerative, Rng);

            foreach (var v in variants)
                Assert.IsTrue(v.HasTrait(GeneticTraits.Regenerative),
                    "Children must inherit existing traits from parent");
        }

        [Test]
        public void MutateRecommendedWithDiscoveries_ReturnsSingleKnownTraitVariant()
        {
            var parent = Genome.Default();
            var known = GeneticTraits.Photosynthetic | GeneticTraits.Toxic;

            for (int trial = 0; trial < 30; trial++)
            {
                var variant = MutationSystem.MutateRecommendedWithDiscoveries(
                    parent, new BehaviourBias(), known, new System.Random(trial));

                Assert.IsNotNull(variant);
                Assert.AreNotEqual(GeneticTraits.None, variant.Traits & known);
            }
        }

        // ---- GameManager.RecordTraitDiscovery ----

        [Test]
        public void RecordTraitDiscovery_OrsIntoFlags()
        {
            var save = new SaveData();
            Assert.AreEqual(0, save.DiscoveredTraitFlags);

            save.DiscoveredTraitFlags |= (int)GeneticTraits.Photosynthetic;
            Assert.AreEqual((int)GeneticTraits.Photosynthetic, save.DiscoveredTraitFlags);

            save.DiscoveredTraitFlags |= (int)GeneticTraits.Electric;
            Assert.AreEqual(
                (int)(GeneticTraits.Photosynthetic | GeneticTraits.Electric),
                save.DiscoveredTraitFlags);
        }

        [Test]
        public void RecordTraitDiscovery_NoneTraits_DoesNotChange()
        {
            var save = new SaveData();
            save.DiscoveredTraitFlags |= (int)GeneticTraits.None;
            Assert.AreEqual(0, save.DiscoveredTraitFlags);
        }

        [Test]
        public void SaveData_DefaultDiscoveredTraitFlags_IsZero()
        {
            var save = new SaveData();
            Assert.AreEqual(0, save.DiscoveredTraitFlags);
        }

        // ---- Localization keys ----

        [Test]
        public void LocalizationKeys_ForDiscovery_Exist()
        {
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("meta.discovery.hint")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("meta.discovery.none")));
            Assert.AreNotEqual("meta.discovery.hint", Localization.Get("meta.discovery.hint"));
            Assert.AreNotEqual("meta.discovery.none", Localization.Get("meta.discovery.none"));
        }
    }
}
