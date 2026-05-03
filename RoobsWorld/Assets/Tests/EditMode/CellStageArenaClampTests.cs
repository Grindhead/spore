using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class CellStageArenaClampTests
    {
        [Test]
        public void ClampPositionToArena_LeavesInteriorPointUnchanged()
        {
            var center = new Vector3(10f, 0f, -4f);
            float radius = 50f;
            var inside = center + new Vector3(5f, 2f, -8f);

            var clamped = CellStage.ClampPositionToArena(center, radius, inside);

            Assert.AreEqual(inside.x, clamped.x, 0.001f);
            Assert.AreEqual(inside.z, clamped.z, 0.001f);
            Assert.AreEqual(0f, clamped.y, 0.001f);
        }

        [Test]
        public void ClampPositionToArena_ClampsOutsidePointToSafeRing()
        {
            var center = Vector3.zero;
            float radius = 40f;
            var outside = new Vector3(120f, 3f, 0f);

            var clamped = CellStage.ClampPositionToArena(center, radius, outside);
            float maxRadius = radius * 0.95f;

            Assert.AreEqual(0f, clamped.y, 0.001f);
            Assert.AreEqual(maxRadius, clamped.magnitude, 0.001f);
        }

        [Test]
        public void ClampPositionToArena_ZeroRadius_ReturnsArenaCenterOnPlane()
        {
            var center = new Vector3(2f, 0f, -6f);
            var point = new Vector3(8f, 5f, 9f);

            var clamped = CellStage.ClampPositionToArena(center, 0f, point);

            Assert.AreEqual(center.x, clamped.x, 0.001f);
            Assert.AreEqual(center.z, clamped.z, 0.001f);
            Assert.AreEqual(0f, clamped.y, 0.001f);
        }
    }
}
