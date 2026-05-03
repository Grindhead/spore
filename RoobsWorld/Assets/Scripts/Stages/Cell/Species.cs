using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Tracks a single species in the cell-stage evolution tree: id, parent lineage, base genome, and population.
    /// Step 5 of cell-stage evolution roadmap. Aligns with memory-bank/cellStageImplementationSteps.md and evolutionSimulationDesign.md (§10–11).
    /// </summary>
    public class Species
    {
        /// <summary>Unique id for this species (0 = ancestor).</summary>
        public int SpeciesId { get; }

        /// <summary>Species id of the parent species (-1 if ancestor).</summary>
        public int ParentSpeciesId { get; }

        /// <summary>Archetype genome when this species was created (for display/UI).</summary>
        public Genome BaseGenome { get; }

        /// <summary>Current number of living organisms in this species.</summary>
        public int Population { get; set; }

        public Species(int speciesId, int parentSpeciesId, Genome baseGenome)
        {
            SpeciesId = speciesId;
            ParentSpeciesId = parentSpeciesId;
            BaseGenome = baseGenome != null ? baseGenome.Clone() : Genome.Default();
            Population = 0;
        }
    }
}
