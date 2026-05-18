namespace RoobsWorld.Core
{
    public interface IStageRuntime
    {
        GameStageType CurrentStageType { get; }
        bool IsTransitioning { get; }
        void TransitionTo(GameStageType targetStage);
    }

    public interface ICameraEffects
    {
        void SetFollowTarget(UnityEngine.Transform target);
        void RequestShake(float duration, float magnitude);
        void ActivateRig(CameraRigType type);
    }

    public interface IGameSession
    {
        bool IsPaused { get; }
        void SetPaused(bool paused, bool suppressPauseMenu = false);
        void StartNewGame(long seed = 0);
        void SaveGame(string slotName = null);
        void QuitGame();
    }
}
