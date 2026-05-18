using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Explicit runtime dependencies for Cell stage systems to avoid scene-wide discovery patterns.
    /// </summary>
    public sealed class CellStageContext
    {
        public CellStage Stage { get; }
        public GameManager GameManager { get; }
        public StageManager StageManager { get; }
        public CameraManager CameraManager { get; }
        public MultiplayerSessionManager Session { get; }

        private CellStageContext(
            CellStage stage,
            GameManager gameManager,
            StageManager stageManager,
            CameraManager cameraManager,
            MultiplayerSessionManager session)
        {
            Stage = stage;
            GameManager = gameManager;
            StageManager = stageManager;
            CameraManager = cameraManager;
            Session = session;
        }

        public static CellStageContext Create(CellStage stage)
        {
            var game = GameManager.HasInstance ? GameManager.Instance : null;
            var stageMgr = StageManager.HasInstance ? StageManager.Instance : null;
            var cameraMgr = CameraManager.HasInstance ? CameraManager.Instance : null;
            var session = game?.Session;
            return new CellStageContext(stage, game, stageMgr, cameraMgr, session);
        }
    }
}
