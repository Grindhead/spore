using RoobsWorld.Core;
using RoobsWorld.Multiplayer;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Handles main-menu flow actions independent of runtime view construction.
    /// </summary>
    public sealed class MainMenuController
    {
        private readonly IGameSession _session;

        public MainMenuController(IGameSession session)
        {
            _session = session;
        }

        public void StartSinglePlayer()
        {
            _session?.SetPaused(false);
            _session?.StartNewGame();
        }

        public void Quit()
        {
            _session?.QuitGame();
        }

        public void AttachMultiplayerSession(MultiplayerSessionManager manager)
        {
            if (GameManager.HasInstance)
                GameManager.Instance.SetSession(manager, null);
        }
    }
}
