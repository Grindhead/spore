using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for PredatorPackCoordinator.ComputeFlankOffset (pure math, no MonoBehaviour required).
    /// </summary>
    public class PredatorPackCoordinatorTests
    {
        private static readonly Vector3 LeaderAt   = Vector3.zero;
        private static readonly Vector3 PlayerAt   = new Vector3(10f, 0f, 0f);

        // Leader → player is along +X; perpendicular on the XZ plane is ±Z.

        [Test]
        public void ComputeFlankOffset_Index1_IsPerpendicularToChaseDirection()
        {
            var offset = PredatorPackCoordinator.ComputeFlankOffset(LeaderAt, PlayerAt, 1);
            // Should have no X component (perpendicular to leader→player which is +X)
            Assert.AreEqual(0f, offset.x, 0.001f, "Flank offset should have no X component");
            Assert.AreEqual(0f, offset.y, 0.001f, "Flank offset should be on the XZ plane");
            Assert.AreNotEqual(0f, offset.z, "Flank offset should have a Z component");
        }

        [Test]
        public void ComputeFlankOffset_Index2_IsOppositeToIndex1()
        {
            var offset1 = PredatorPackCoordinator.ComputeFlankOffset(LeaderAt, PlayerAt, 1);
            var offset2 = PredatorPackCoordinator.ComputeFlankOffset(LeaderAt, PlayerAt, 2);

            // They should be mirror images (same magnitude, opposite sign on Z)
            Assert.AreEqual(offset1.z, -offset2.z, 0.001f, "Index 1 and 2 should be mirrored");
        }

        [Test]
        public void ComputeFlankOffset_Index3_SameSignAsIndex1()
        {
            var offset1 = PredatorPackCoordinator.ComputeFlankOffset(LeaderAt, PlayerAt, 1);
            var offset3 = PredatorPackCoordinator.ComputeFlankOffset(LeaderAt, PlayerAt, 3);

            Assert.IsTrue(Mathf.Sign(offset1.z) == Mathf.Sign(offset3.z),
                "Index 3 should flank same side as index 1");
        }

        [Test]
        public void ComputeFlankOffset_Magnitude_EqualsFlankOffset()
        {
            var offset1 = PredatorPackCoordinator.ComputeFlankOffset(LeaderAt, PlayerAt, 1);
            Assert.AreEqual(PredatorPackCoordinator.FlankOffset, offset1.magnitude, 0.001f,
                "Flank offset magnitude should equal FlankOffset constant");
        }

        [Test]
        public void ComputeFlankOffset_ZeroDistance_ReturnsZero()
        {
            // When leader and player are at the same position, no meaningful perpendicular
            var offset = PredatorPackCoordinator.ComputeFlankOffset(Vector3.zero, Vector3.zero, 1);
            Assert.AreEqual(Vector3.zero, offset, "Zero leader-to-player distance should return zero offset");
        }

        [Test]
        public void ComputeFlankOffset_ChaseAlongZ_PerpIsAlongX()
        {
            // Leader at origin, player along +Z — perpendicular should be along ±X
            var offset = PredatorPackCoordinator.ComputeFlankOffset(Vector3.zero, new Vector3(0, 0, 10), 1);
            Assert.AreEqual(0f, offset.z, 0.001f, "When chasing along Z, flank should have no Z component");
            Assert.AreNotEqual(0f, offset.x, "When chasing along Z, flank should have an X component");
        }

        [Test]
        public void FlankOffset_Constant_IsPositive()
        {
            Assert.Greater(PredatorPackCoordinator.FlankOffset, 0f);
        }

        [Test]
        public void CopyRegisteredPredators_ReturnsOnlyActivePredators()
        {
            var activePredator = CreatePredator("Predator_Active");
            var inactivePredator = CreatePredator("Predator_Inactive");
            try
            {
                inactivePredator.gameObject.SetActive(false);

                var buffer = new List<PredatorCell>();
                int count = PredatorPackCoordinator.CopyRegisteredPredators(buffer);

                Assert.AreEqual(buffer.Count, count);
                Assert.Contains(activePredator, buffer);
                Assert.IsFalse(buffer.Contains(inactivePredator));
            }
            finally
            {
                CleanupPredator(activePredator);
                CleanupPredator(inactivePredator);
            }
        }

        [Test]
        public void CopyRegisteredPredators_NullBuffer_ReturnsZero()
        {
            int count = PredatorPackCoordinator.CopyRegisteredPredators(null);
            Assert.AreEqual(0, count);
        }

        private static PredatorCell CreatePredator(string name)
        {
            var go = new GameObject(name);
            return go.AddComponent<PredatorCell>();
        }

        private static void CleanupPredator(PredatorCell predator)
        {
            if (predator == null) return;
            PredatorPackCoordinator.Unregister(predator);
            Object.DestroyImmediate(predator.gameObject);
        }
    }
}
