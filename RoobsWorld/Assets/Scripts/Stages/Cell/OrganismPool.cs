using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Fixed-size pool of <see cref="Organism"/> GameObjects that avoids per-spawn instantiation
    /// and per-death destroy calls. Pre-warms a set of inactive organisms on creation; when the
    /// pool is exhausted, new instances are instantiated normally (no hard cap on live count).
    ///
    /// Usage:
    ///   var pool = new OrganismPool(prewarm: 20, maxPooled: 40);
    ///   var org  = pool.Rent(pos, genome, energy, speciesId);
    ///   // org.Die() returns it to pool automatically; pool holds the reference internally.
    /// </summary>
    public class OrganismPool
    {
        private readonly Queue<Organism> _available;
        private readonly int _maxPooled;

        /// <summary>Number of organisms currently sitting idle in the pool.</summary>
        public int AvailableCount => _available.Count;

        public OrganismPool(int prewarm, int maxPooled)
        {
            _maxPooled = Mathf.Max(prewarm, maxPooled);
            _available = new Queue<Organism>(prewarm);

            for (int i = 0; i < prewarm; i++)
            {
                var org = CreateOrganism();
                org.gameObject.SetActive(false);
                _available.Enqueue(org);
            }
        }

        /// <summary>
        /// Returns an organism placed at <paramref name="position"/> with the given genome, energy, and species.
        /// The organism's pool-return callback is wired automatically; calling <see cref="Organism.Die"/> will
        /// return it to this pool (up to <c>maxPooled</c>) rather than destroying the GameObject.
        /// </summary>
        public Organism Rent(Vector3 position, Genome genome, float energy, int speciesId)
        {
            Organism org = null;

            while (_available.Count > 0 && org == null)
                org = _available.Dequeue(); // may be null if destroyed externally

            if (org == null)
                org = CreateOrganism();

            org.SetPoolCallback(Return);
            org.Reinitialize(position, genome, energy, speciesId);
            return org;
        }

        /// <summary>
        /// Deactivates all organisms currently in the pool and destroys them.
        /// Does not affect live (rented) organisms.
        /// </summary>
        public void Clear()
        {
            while (_available.Count > 0)
            {
                var org = _available.Dequeue();
                if (org != null)
                    Object.Destroy(org.gameObject);
            }
        }

        private void Return(Organism org)
        {
            if (org == null) return;
            org.SetPoolCallback(null);
            if (_available.Count < _maxPooled)
            {
                org.gameObject.SetActive(false);
                _available.Enqueue(org);
            }
            else
            {
                Object.Destroy(org.gameObject);
            }
        }

        private static Organism CreateOrganism()
        {
            var go = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, Genome.Default());
            go.name = "Organism_Pooled";
            var org = go.AddComponent<Organism>();
            // Ensure collider is trigger
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
            return org;
        }
    }
}
