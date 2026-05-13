using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class PlayerCellScaleTests
    {
        [Test]
        public void ComputeVisualScale_WithGenome_IncreasesWhenMassIncreases()
        {
            var genome = Genome.Default();
            float lowMassScale = PlayerCell.ComputeVisualScale(1f, genome);
            float highMassScale = PlayerCell.ComputeVisualScale(2f, genome);

            Assert.Greater(highMassScale, lowMassScale);
        }

        [Test]
        public void ComputeVisualScale_WithGenome_UsesGenomeBaseScaleAtStartMass()
        {
            var genome = Genome.Default();
            genome.Size = 2f;

            float scale = PlayerCell.ComputeVisualScale(1f, genome);

            Assert.AreEqual(0.18f, scale, 0.0001f);
        }

        [Test]
        public void ComputeVisualScale_WithoutGenome_UsesMassOnlyScale()
        {
            float scale = PlayerCell.ComputeVisualScale(2f, null);

            Assert.AreEqual(0.24f, scale, 0.0001f);
        }

        [Test]
        public void ComputeVisualScale_WithDefaultGenome_StartsSmall()
        {
            float scale = PlayerCell.ComputeVisualScale(1f, Genome.Default());

            Assert.LessOrEqual(scale, 0.15f);
        }

        [Test]
        public void ComputeConsumeProbeRadius_TracksActualPlayerCollider()
        {
            var go = new GameObject("PlayerProbeRadius_Test");
            try
            {
                go.transform.localScale = Vector3.one * 0.26f;
                var collider = go.AddComponent<SphereCollider>();
                collider.radius = 0.5f;

                float probeRadius = PlayerCell.ComputeConsumeProbeRadius(go.transform.lossyScale, collider);

                Assert.AreEqual(0.17f, probeRadius, 0.001f);
                Assert.Less(probeRadius, 0.75f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OrganismPredationProbeRadius_TracksActualCollider()
        {
            var go = new GameObject("OrganismPredationProbeRadius_Test");
            try
            {
                go.transform.localScale = Vector3.one * 1.2f;
                var collider = go.AddComponent<SphereCollider>();
                collider.radius = 0.5f;

                float probeRadius = Organism.ComputePredationProbeRadius(go.transform.lossyScale, collider);

                Assert.AreEqual(0.72f, probeRadius, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CellAIPredationProbeRadius_UsesMinimumForTinyCells()
        {
            float probeRadius = CellAI.ComputePredationProbeRadius(Vector3.one * 0.1f, null);

            Assert.AreEqual(0.26f, probeRadius, 0.001f);
        }
    }
}
