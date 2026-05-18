using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Stages.Cell.Services;

namespace RoobsWorld.Tests.EditMode
{
    public class CellStageServiceExtractionTests
    {
        [Test]
        public void ScalingService_MatchesCellStageFacade()
        {
            int scaledA = CellStageScaling.ComputeScaledCount(8, 4, 2, 6);
            int scaledB = CellStage.ComputeMultiplayerScaledCount(8, 4, 2, 6);
            Assert.AreEqual(scaledA, scaledB);

            int predatorsA = CellStageScaling.ComputePredatorCount(2, 5, 2, 3);
            int predatorsB = CellStage.ComputeMultiplayerPredatorCount(2, 5, 2, 3);
            Assert.AreEqual(predatorsA, predatorsB);

            float radiusA = CellStageScaling.ComputeArenaRadius(50f, 3, 0.1f, 0.3f);
            float radiusB = CellStage.ComputeMultiplayerArenaRadius(50f, 3, 0.1f, 0.3f);
            Assert.AreEqual(radiusA, radiusB, 0.0001f);
        }
    }
}
