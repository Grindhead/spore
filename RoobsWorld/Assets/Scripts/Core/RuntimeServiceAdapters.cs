using UnityEngine;

namespace RoobsWorld.Core
{
    public sealed class StageRuntimeAdapter : IStageRuntime
    {
        public GameStageType CurrentStageType => StageManager.HasInstance ? StageManager.Instance.CurrentStageType : GameStageType.None;
        public bool IsTransitioning => StageManager.HasInstance && StageManager.Instance.IsTransitioning;

        public void TransitionTo(GameStageType targetStage)
        {
            if (StageManager.HasInstance)
                StageManager.Instance.TransitionTo(targetStage);
        }
    }

    public sealed class CameraEffectsAdapter : ICameraEffects
    {
        public void SetFollowTarget(Transform target)
        {
            if (CameraManager.HasInstance)
                CameraManager.Instance.SetFollowTarget(target);
        }

        public void RequestShake(float duration, float magnitude)
        {
            if (CameraManager.HasInstance)
                CameraManager.Instance.RequestShake(duration, magnitude);
        }

        public void ActivateRig(CameraRigType type)
        {
            if (CameraManager.HasInstance)
                CameraManager.Instance.ActivateRig(type);
        }
    }

    public sealed class GameSessionAdapter : IGameSession
    {
        public bool IsPaused => GameManager.HasInstance && GameManager.Instance.IsPaused;

        public void SetPaused(bool paused, bool suppressPauseMenu = false)
        {
            if (GameManager.HasInstance)
                GameManager.Instance.SetPaused(paused, suppressPauseMenu);
        }

        public void StartNewGame(long seed = 0)
        {
            if (GameManager.HasInstance)
                GameManager.Instance.StartNewGame(seed);
        }

        public void SaveGame(string slotName = null)
        {
            if (GameManager.HasInstance)
                GameManager.Instance.SaveGame(slotName);
        }

        public void QuitGame()
        {
            if (GameManager.HasInstance)
                GameManager.Instance.QuitGame();
        }
    }
}
