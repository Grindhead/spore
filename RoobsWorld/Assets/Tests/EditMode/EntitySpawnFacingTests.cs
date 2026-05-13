using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class EntitySpawnFacingTests
    {
        [Test]
        public void IsPointInFrontOfPlayer_ReturnsTrue_ForForwardPoint()
        {
            Assert.IsTrue(CellStage.IsPointInFrontOfPlayer(
                Vector3.zero,
                Vector3.forward,
                new Vector3(0f, 0f, 4f)));
        }

        [Test]
        public void IsPointInFrontOfPlayer_ReturnsFalse_ForBehindPoint()
        {
            Assert.IsFalse(CellStage.IsPointInFrontOfPlayer(
                Vector3.zero,
                Vector3.forward,
                new Vector3(0f, 0f, -4f)));
        }

        [Test]
        public void MovePointBehindPlayer_MirrorsForwardPointBehindPlayer()
        {
            Vector3 result = CellStage.MovePointBehindPlayer(
                Vector3.zero,
                Vector3.forward,
                new Vector3(2f, 0f, 4f),
                1f);

            Assert.LessOrEqual(result.z, -4f);
            Assert.AreEqual(2f, result.x, 0.0001f);
        }

        [Test]
        public void CreatureSpawnPosition_WithForward_AvoidsFrontHemisphere()
        {
            Random.InitState(1234);

            for (int i = 0; i < 24; i++)
            {
                Vector3 pos = CreatureStageEncounterManager.GetRandomSpawnPosition(
                    Vector3.zero,
                    Vector3.forward,
                    8f,
                    20f,
                    48f);

                Assert.IsFalse(CellStage.IsPointInFrontOfPlayer(Vector3.zero, Vector3.forward, pos));
            }
        }
    }
}
