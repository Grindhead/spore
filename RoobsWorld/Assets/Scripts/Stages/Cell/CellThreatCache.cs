using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Shared cache container for common Cell-stage threat/resource lookups.
    /// </summary>
    public sealed class CellThreatCache
    {
        public readonly List<ArenaCurrent> Currents = new List<ArenaCurrent>(8);
        public readonly List<PredatorCell> Predators = new List<PredatorCell>(24);
        public readonly List<Organism> Organisms = new List<Organism>(64);

        public void ClearAll()
        {
            Currents.Clear();
            Predators.Clear();
            Organisms.Clear();
        }
    }
}
