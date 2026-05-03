using UnityEngine;
using System;
using SystemRandom = System.Random;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Handles reproduction: energy threshold, cost, and spawning offspring with mutated genome.
    /// Aligns with memory-bank/cellStageImplementationSteps.md Step 4 and evolutionSimulationDesign.md (§6).
    /// </summary>
    public static class ReproductionSystem
    {
        private const float SpawnOffsetRadius = 4.0f;

        /// <summary>Energy required to reproduce.</summary>
        public static float GetReproductionThreshold()
        {
            return BalanceConstants.CellReproductionEnergyThreshold;
        }

        /// <summary>Energy cost deducted from parent when reproducing.</summary>
        public static float GetReproductionCost()
        {
            return BalanceConstants.CellReproductionEnergyCost;
        }

        /// <summary>Initial energy given to the newborn offspring.</summary>
        public static float GetOffspringInitialEnergy()
        {
            return BalanceConstants.CellOffspringInitialEnergy;
        }

        /// <summary>Returns true if the organism has enough energy to reproduce.</summary>
        public static bool CanReproduce(Organism organism)
        {
            return organism != null && !organism.IsDead && organism.Energy >= GetReproductionThreshold();
        }

        /// <summary>Returns true if the given energy value is at or above reproduction threshold.</summary>
        public static bool CanReproduce(float energy)
        {
            return energy >= GetReproductionThreshold();
        }

        /// <summary>Creates a mutated genome for offspring. Pass optional rng for deterministic tests.</summary>
        public static Genome CreateOffspringGenome(Genome parent, SystemRandom rng = null)
        {
            return MutationSystem.Mutate(parent ?? Genome.Default(), rng);
        }

        /// <summary>
        /// Attempts to reproduce: deducts parent energy, spawns offspring at offset from parent, returns the child.
        /// When <paramref name="pool"/> is provided the offspring is rented from the pool instead of instantiated.
        /// Returns null if parent cannot reproduce (dead, insufficient energy).
        /// </summary>
        public static Organism TryReproduce(Organism parent, SystemRandom rng = null, OrganismPool pool = null)
        {
            if (!CanReproduce(parent)) return null;

            float cost = GetReproductionCost();
            parent.DeductEnergy(cost);

            Genome childGenome = MutationSystem.MutateBiased(parent.Genome, parent.GetAdaptiveMutationBias(), rng);
            float initialEnergy = GetOffspringInitialEnergy();
            Vector3 offset;
            if (rng != null)
            {
                double angle = rng.NextDouble() * Math.PI * 2;
                offset = new Vector3((float)Math.Cos(angle) * SpawnOffsetRadius, 0f, (float)Math.Sin(angle) * SpawnOffsetRadius);
            }
            else
            {
                var circle = UnityEngine.Random.insideUnitCircle.normalized * SpawnOffsetRadius;
                offset = new Vector3(circle.x, 0f, circle.y);
            }
            Vector3 spawnPos = parent.transform.position + offset;
            int speciesId = SpeciationSystem.GetSpeciesIdForOffspring(parent.SpeciesId, parent.Genome, childGenome);
            var child = pool != null
                ? pool.Rent(spawnPos, childGenome, initialEnergy, speciesId)
                : Organism.Create(spawnPos, childGenome, initialEnergy, speciesId);
            OrganismArchetypeConfigurator.Configure(child, OrganismArchetype.Generalist);
            parent.ResetAdaptiveMutationBias();
            SpeciationSystem.RegisterOrganism(speciesId);
            return child;
        }
    }
}
