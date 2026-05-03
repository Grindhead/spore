using System.Collections.Generic;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Compares genomes and creates new species when genetic distance exceeds threshold.
    /// Tracks species registry (lineage, base genome, population). Step 5 cell-stage evolution.
    /// Aligns with memory-bank/cellStageImplementationSteps.md and evolutionSimulationDesign.md (§10–11).
    /// </summary>
    public static class SpeciationSystem
    {
        private static readonly Dictionary<int, Species> SpeciesById = new Dictionary<int, Species>();
        private static int _nextSpeciesId = 1;

        /// <summary>Genetic distance threshold from BalanceConstants.</summary>
        public static float GetGeneticDistanceThreshold()
        {
            return BalanceConstants.CellSpeciationGeneticDistanceThreshold;
        }

        /// <summary>
        /// Sum of absolute differences for numeric traits (and normalized color difference).
        /// Used to decide if offspring is same species or a new species.
        /// </summary>
        public static float GeneticDistance(Genome a, Genome b)
        {
            if (a == null || b == null) return 0f;
            float d = 0f;
            d += UnityEngine.Mathf.Abs(a.Speed - b.Speed);
            d += UnityEngine.Mathf.Abs(a.Size - b.Size);
            d += UnityEngine.Mathf.Abs(a.Attack - b.Attack);
            d += UnityEngine.Mathf.Abs(a.Defence - b.Defence);
            d += UnityEngine.Mathf.Abs(a.Digestion - b.Digestion);
            d += UnityEngine.Mathf.Abs(a.MetabolismCost - b.MetabolismCost);
            d += UnityEngine.Mathf.Abs(a.Spikes - b.Spikes);
            d += UnityEngine.Mathf.Abs(a.Flagella - b.Flagella);
            float cr = UnityEngine.Mathf.Abs(a.Color.r - b.Color.r);
            float cg = UnityEngine.Mathf.Abs(a.Color.g - b.Color.g);
            float cb = UnityEngine.Mathf.Abs(a.Color.b - b.Color.b);
            d += (cr + cg + cb) * 10f; // scale 0–3 color diff to ~0–30 so it can contribute meaningfully
            return d;
        }

        /// <summary>
        /// Returns the species id for an offspring: same as parent if genetic distance to parent is within threshold, otherwise a new species.
        /// Ensures species 0 exists when parentSpeciesId is 0.
        /// </summary>
        public static int GetSpeciesIdForOffspring(int parentSpeciesId, Genome parentGenome, Genome offspringGenome)
        {
            EnsureSpeciesExists(parentSpeciesId, parentGenome ?? Genome.Default());
            float distance = GeneticDistance(parentGenome, offspringGenome);
            if (distance <= GetGeneticDistanceThreshold())
                return parentSpeciesId;
            var newSpecies = new Species(_nextSpeciesId, parentSpeciesId, offspringGenome != null ? offspringGenome.Clone() : Genome.Default());
            SpeciesById[_nextSpeciesId] = newSpecies;
            int createdId = _nextSpeciesId++;
            RoobsWorld.Core.EventBus.Publish(new RoobsWorld.Core.CellNewSpeciesEvent
            {
                NewSpeciesId = createdId,
                ParentSpeciesId = parentSpeciesId,
                TotalSpeciesCount = SpeciesById.Count
            });
            return createdId;
        }

        /// <summary>Ensures a species record exists (e.g. species 0 for the first organism).</summary>
        public static void EnsureSpeciesExists(int speciesId, Genome baseGenome)
        {
            if (SpeciesById.ContainsKey(speciesId)) return;
            int parentId = speciesId == 0 ? -1 : 0;
            SpeciesById[speciesId] = new Species(speciesId, parentId, baseGenome != null ? baseGenome.Clone() : Genome.Default());
        }

        /// <summary>Increments population for the given species. Call when an organism is spawned.</summary>
        public static void RegisterOrganism(int speciesId)
        {
            if (!SpeciesById.TryGetValue(speciesId, out var species)) return;
            species.Population++;
        }

        /// <summary>Decrements population for the given species. Call when an organism dies (or subscribe to CellOrganismDiedEvent).</summary>
        public static void UnregisterOrganism(int speciesId)
        {
            if (!SpeciesById.TryGetValue(speciesId, out var species)) return;
            species.Population = UnityEngine.Mathf.Max(0, species.Population - 1);
            if (species.Population == 0)
            {
                RoobsWorld.Core.EventBus.Publish(new RoobsWorld.Core.CellSpeciesExtinctEvent
                {
                    ExtinctSpeciesId = speciesId,
                    TotalSpeciesCount = SpeciesById.Count
                });
            }
        }

        /// <summary>Returns the species record, or null if not found.</summary>
        public static Species GetSpecies(int speciesId)
        {
            return SpeciesById.TryGetValue(speciesId, out var s) ? s : null;
        }

        /// <summary>Returns all registered species (for UI / phylogenetic tree).</summary>
        public static IReadOnlyList<Species> GetAllSpecies()
        {
            var list = new List<Species>(SpeciesById.Values);
            list.Sort((a, b) => a.SpeciesId.CompareTo(b.SpeciesId));
            return list;
        }

        /// <summary>Resets the registry and next id (e.g. for tests or new game).</summary>
        public static void Reset()
        {
            SpeciesById.Clear();
            _nextSpeciesId = 1;
        }
    }
}
