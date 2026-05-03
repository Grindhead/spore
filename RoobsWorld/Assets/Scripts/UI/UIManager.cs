using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Base class for full-screen UI screens that can be managed by <see cref="UIManager"/>.
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        public abstract string ScreenId { get; }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void OnBack()
        {
            UIManager.Instance.PopScreen();
        }
    }

    /// <summary>
    /// Manages UI screens by ID, providing a simple stack-based navigation model (Show/Pop/HideAll).
    /// </summary>
    public class UIManager : Core.Singleton<UIManager>
    {
        private readonly Dictionary<string, UIScreen> _screens = new Dictionary<string, UIScreen>();
        private readonly Stack<UIScreen> _screenStack = new Stack<UIScreen>();

        private UIScreen _activeScreen;

        public UIScreen ActiveScreen => _activeScreen;

        public void RegisterScreen(UIScreen screen)
        {
            _screens[screen.ScreenId] = screen;
            screen.Hide();
        }

        public void ShowScreen(string screenId)
        {
            if (!_screens.TryGetValue(screenId, out var screen))
            {
                Debug.LogWarning($"[UIManager] Screen '{screenId}' not registered.");
                return;
            }

            if (_activeScreen != null)
            {
                _screenStack.Push(_activeScreen);
                _activeScreen.Hide();
            }

            _activeScreen = screen;
            _activeScreen.Show();
        }

        public void PopScreen()
        {
            if (_activeScreen != null)
                _activeScreen.Hide();

            _activeScreen = _screenStack.Count > 0 ? _screenStack.Pop() : null;
            _activeScreen?.Show();
        }

        public void HideAll()
        {
            foreach (var screen in _screens.Values)
                screen.Hide();
            _screenStack.Clear();
            _activeScreen = null;
        }

        public T GetScreen<T>(string screenId) where T : UIScreen
        {
            return _screens.TryGetValue(screenId, out var screen) ? screen as T : null;
        }
    }
}
