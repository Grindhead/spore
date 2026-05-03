using UnityEngine;
using System;
using RoobsWorld.Utils;
using SystemRandom = System.Random;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Applies small random mutations to genomes during reproduction.
    /// Aligns with memory-bank/evolutionSimulationDesign.md (§3) and cellStageImplementationSteps.md Step 4.
    /// </summary>
    public static class MutationSystem
    {
        private const float TraitMin = 0f;
        private const float TraitMax = 10f;
        private const float DefaultTraitDelta = 0.15f;
        private const float MutationChancePerGene = 0.6f;
        private const float ColorDelta = 0.08f;

        /// <summary>Probability per mutation call that a new special trait is unlocked (stacks with existing traits).</summary>
        private const float SpecialTraitUnlockChance = 0.04f;
        private const int SpecialTraitCount = 5; // matches GeneticTraits enum flags

        /// <summary>
        /// Returns a new genome with small random changes from the parent.
        /// Uses optional rng for deterministic tests; when null uses UnityEngine.Random.
        /// </summary>
        public static Genome Mutate(Genome parent, SystemRandom rng = null)
        {
            if (parent == null) return Genome.Default();
            Genome g = parent.Clone();
            bool useUnity = rng == null;

            g.Speed = MutateGene(g.Speed, useUnity, rng);
            g.Size = MutateGene(g.Size, useUnity, rng);
            g.Attack = MutateGene(g.Attack, useUnity, rng);
            g.Defence = MutateGene(g.Defence, useUnity, rng);
            g.Digestion = MutateGene(g.Digestion, useUnity, rng);
            g.MetabolismCost = MutateGene(g.MetabolismCost, useUnity, rng);
            g.Spikes = MutateGene(g.Spikes, useUnity, rng);
            g.Flagella = MutateGene(g.Flagella, useUnity, rng);
            MutateColor(ref g.Color, useUnity, rng);
            g.Traits = MutateTraits(g.Traits, useUnity, rng);
            return g;
        }

        /// <summary>
        /// Potentially unlocks one new special trait (4% chance per call).
        /// Existing traits are always inherited — traits accumulate over generations.
        /// </summary>
        public static GeneticTraits MutateTraits(GeneticTraits current, bool useUnityRandom = true,
                                                  SystemRandom rng = null)
        {
            float roll = useUnityRandom ? UnityEngine.Random.value : (rng != null ? (float)rng.NextDouble() : 1f);
            if (roll >= SpecialTraitUnlockChance) return current;
            int bit = useUnityRandom
                ? UnityEngine.Random.Range(0, SpecialTraitCount)
                : (rng != null ? (int)(rng.NextDouble() * SpecialTraitCount) : 0);
            return current | (GeneticTraits)(1 << bit);
        }

        /// <summary>
        /// Returns a new genome with small random changes from the parent, biased toward
        /// traits that reflect the player's recent behaviour (see <see cref="BehaviourBias"/>).
        /// Bias values in [-1, +1] shift the mutation delta centre without removing randomness.
        /// Uses optional rng for deterministic tests; when null uses UnityEngine.Random.
        /// </summary>
        public static Genome MutateBiased(Genome parent, BehaviourBias bias, SystemRandom rng = null)
        {
            if (parent == null) return Genome.Default();
            Genome g = parent.Clone();
            bool useUnity = rng == null;

            g.Speed        = MutateGene(g.Speed,        useUnity, rng, bias.Speed       * DefaultTraitDelta);
            g.Size         = MutateGene(g.Size,         useUnity, rng, bias.Size        * DefaultTraitDelta);
            g.Attack       = MutateGene(g.Attack,       useUnity, rng, bias.Attack      * DefaultTraitDelta);
            g.Defence      = MutateGene(g.Defence,      useUnity, rng, bias.Defence     * DefaultTraitDelta);
            g.Digestion    = MutateGene(g.Digestion,    useUnity, rng, bias.Digestion   * DefaultTraitDelta);
            g.MetabolismCost = MutateGene(g.MetabolismCost, useUnity, rng);
            g.Spikes       = MutateGene(g.Spikes,       useUnity, rng, bias.Spikes      * DefaultTraitDelta);
            g.Flagella     = MutateGene(g.Flagella,     useUnity, rng, bias.Flagella    * DefaultTraitDelta);
            MutateColor(ref g.Color, useUnity, rng);
            g.Traits = MutateTraits(g.Traits, useUnity, rng);
            return g;
        }

        /// <summary>
        /// Generates three mutation variants biased by play-style, and guarantees that at least
        /// one variant carries a trait the player has previously discovered (meta-progression).
        /// If <paramref name="discovered"/> is <see cref="GeneticTraits.None"/>, falls back to
        /// standard biased generation with one unbiased wildcard in slot 2.
        /// </summary>
        public static Genome[] MutateBiasedWithDiscoveries(
            Genome parent, BehaviourBias bias, GeneticTraits discovered, SystemRandom rng = null)
        {
            if (parent == null) parent = Genome.Default();

            var variants = new Genome[]
            {
                MutateBiased(parent, bias, rng),
                MutateBiased(parent, bias, rng),
                Mutate(parent, rng)              // unbiased wildcard in slot 2
            };

            if (discovered == GeneticTraits.None) return variants;

            // Check whether any generated variant already carries a known trait.
            bool anyHasKnown = false;
            for (int i = 0; i < variants.Length; i++)
            {
                if ((variants[i].Traits & discovered) != GeneticTraits.None)
                {
                    anyHasKnown = true;
                    break;
                }
            }

            if (!anyHasKnown)
            {
                // Inject the lowest-bit discovered trait into slot 0 (keep other gene values).
                var boosted = variants[0].Clone();
                boosted.Traits |= LowestBit(discovered);
                variants[0] = boosted;
            }

            return variants;
        }

        /// <summary>
        /// Generates the single recommended upgrade variant for prompts that no longer offer choices.
        /// </summary>
        public static Genome MutateRecommendedWithDiscoveries(
            Genome parent, BehaviourBias bias, GeneticTraits discovered, SystemRandom rng = null)
        {
            if (parent == null) parent = Genome.Default();

            var variant = MutateBiased(parent, bias, rng);
            if (discovered != GeneticTraits.None && (variant.Traits & discovered) == GeneticTraits.None)
            {
                variant = variant.Clone();
                variant.Traits |= LowestBit(discovered);
            }

            return variant;
        }

        /// <summary>Isolates the lowest set bit of a flags value.</summary>
        private static GeneticTraits LowestBit(GeneticTraits flags)
        {
            int v = (int)flags;
            return (GeneticTraits)(v & -v);
        }

        /// <summary>
        /// Mutate a single trait: small delta, clamped 0–10. Optional rng for tests.
        /// </summary>
        public static float MutateGene(float value, bool useUnityRandom, SystemRandom rng = null,
                                        float biasOffset = 0f)
        {
            float delta;
            if (useUnityRandom)
            {
                if (UnityEngine.Random.value >= MutationChancePerGene) return value;
                delta = (UnityEngine.Random.value * 2f - 1f) * DefaultTraitDelta;
            }
            else
            {
                if (rng == null) return value;
                if (rng.NextDouble() >= MutationChancePerGene) return value;
                delta = ((float)rng.NextDouble() * 2f - 1f) * DefaultTraitDelta;
            }
            return Mathf.Clamp(value + delta + biasOffset, TraitMin, TraitMax);
        }

        private static void MutateColor(ref Color c, bool useUnityRandom, SystemRandom rng)
        {
            float dr, dg, db;
            if (useUnityRandom)
            {
                dr = (UnityEngine.Random.value * 2f - 1f) * ColorDelta;
                dg = (UnityEngine.Random.value * 2f - 1f) * ColorDelta;
                db = (UnityEngine.Random.value * 2f - 1f) * ColorDelta;
            }
            else
            {
                if (rng == null) return;
                dr = ((float)rng.NextDouble() * 2f - 1f) * ColorDelta;
                dg = ((float)rng.NextDouble() * 2f - 1f) * ColorDelta;
                db = ((float)rng.NextDouble() * 2f - 1f) * ColorDelta;
            }
            c.r = Mathf.Clamp01(c.r + dr);
            c.g = Mathf.Clamp01(c.g + dg);
            c.b = Mathf.Clamp01(c.b + db);
            c = ProceduralTextureFactory.Opaque(c);
        }
    }
}
