using System;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Serializable user settings for resolution, quality, audio, camera, HUD, and accessibility options.
    /// </summary>
    /// <summary>Fullscreen mode for desktop; maps to Unity FullScreenMode.</summary>
    public enum FullscreenMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3
    }

    /// <summary>
    /// Colour-blind simulation mode. Affects food, cell-type, and biome indicator colours
    /// via <see cref="AccessibilityPalette"/>.
    /// </summary>
    public enum ColorBlindMode
    {
        None = 0,
        Deuteranopia = 1,   // red-green blind (most common)
        Protanopia   = 2,   // red blind
        Tritanopia   = 3    // blue-yellow blind
    }

    [Serializable]
    public class GameSettings
    {
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        /// <summary>Refresh rate in Hz for the selected resolution; 0 = use default.</summary>
        public int RefreshRate = 0;
        public bool Fullscreen = true;
        /// <summary>0=ExclusiveFullScreen, 1=FullScreenWindow (borderless), 2=MaximizedWindow, 3=Windowed. Used when applying resolution.</summary>
        public int FullscreenModeIndex = (int)global::RoobsWorld.Core.FullscreenMode.FullScreenWindow;
        public int QualityLevel = 2;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float SFXVolume = 1f;
        public float AmbientVolume = 1f;
        public float CameraSensitivity = 1f;
        public bool InvertY = false;
        public bool ShowFPS = false;
        /// <summary>Enable VSync (recommended for desktop to avoid tearing).</summary>
        public bool VSync = true;
        /// <summary>Target frame rate: 0 = don't set (use VSync or default), 60, 120, etc. Ignored when VSync is on on many platforms.</summary>
        public int TargetFrameRate = 0;

        /// <summary>When true, subtitle text is shown for dialogue and key events when supported by the game.</summary>
        public bool SubtitlesEnabled = true;

        /// <summary>Scale factor for UI text (1 = 100%). Used by HUD and menus for readability.</summary>
        public float UITextScale = 1f;

        /// <summary>Colour-blind mode index (0=None, 1=Deuteranopia, 2=Protanopia, 3=Tritanopia).</summary>
        public int ColorBlindModeIndex = (int)ColorBlindMode.None;

        /// <summary>When true, hazards (predators, toxin clouds) and the player cell are highlighted with
        /// high-visibility emission colours to improve readability for players with visual impairments.</summary>
        public bool HighContrastMode = false;
    }

    /// <summary>
    /// Loads, applies, and persists <see cref="GameSettings"/> using Unity <see cref="PlayerPrefs"/>.
    /// </summary>
    public class SettingsManager : Singleton<SettingsManager>
    {
        private const string SETTINGS_KEY = "GameSettings";

        private GameSettings _current;

        public GameSettings Current => _current;

        /// <summary>
        /// Returns the current UI text scale factor (clamped 0.5–2). Use when building HUD or menu text so accessibility setting applies.
        /// </summary>
        public static float GetUITextScale()
        {
            if (!HasInstance || Instance._current == null) return 1f;
            return Mathf.Clamp(Instance._current.UITextScale, 0.5f, 2f);
        }

        /// <summary>
        /// Loads settings from disk and applies them on startup.
        /// </summary>
        protected override void OnInitialize()
        {
            Load();
        }

        /// <summary>
        /// Loads settings from <see cref="PlayerPrefs"/>, falling back to defaults when no data exists, and applies them.
        /// </summary>
        public void Load()
        {
            string json = PlayerPrefs.GetString(SETTINGS_KEY, "");
            _current = string.IsNullOrEmpty(json)
                ? CreateDefaultForCurrentDisplay()
                : JsonUtility.FromJson<GameSettings>(json);

            SanitizeLoadedSettings();

            Apply();
        }

        /// <summary>
        /// Serializes the current settings back into <see cref="PlayerPrefs"/>.
        /// </summary>
        public void Save()
        {
            string json = JsonUtility.ToJson(_current);
            PlayerPrefs.SetString(SETTINGS_KEY, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Applies resolution, fullscreen mode, VSync, target frame rate, quality, and audio volumes according to <see cref="Current"/>.
        /// </summary>
        public void Apply()
        {
            FullScreenMode mode = _current.Fullscreen
                ? (FullScreenMode)Mathf.Clamp(_current.FullscreenModeIndex, 0, 3)
                : FullScreenMode.Windowed;
            int width = Mathf.Max(640, _current.ResolutionWidth);
            int height = Mathf.Max(360, _current.ResolutionHeight);
            int refreshRate = _current.RefreshRate > 0 ? _current.RefreshRate : GetCurrentRefreshRate();

            // Borderless/maximized should track native desktop resolution to avoid OS upscaling blur.
            if (mode == FullScreenMode.FullScreenWindow || mode == FullScreenMode.MaximizedWindow)
            {
                var native = Screen.currentResolution;
                width = native.width;
                height = native.height;
                refreshRate = GetCurrentRefreshRate();
            }

            Screen.SetResolution(width, height, mode, new RefreshRate { numerator = (uint)refreshRate, denominator = 1 });
            QualitySettings.vSyncCount = _current.VSync ? 1 : 0;
            Application.targetFrameRate = _current.VSync ? 0 : (_current.TargetFrameRate > 0 ? _current.TargetFrameRate : -1);
            QualitySettings.SetQualityLevel(_current.QualityLevel);

            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.MasterVolume = _current.MasterVolume;
                AudioManager.Instance.MusicVolume = _current.MusicVolume;
                AudioManager.Instance.SFXVolume = _current.SFXVolume;
                AudioManager.Instance.AmbientVolume = _current.AmbientVolume;
            }
        }

        private static GameSettings CreateDefaultForCurrentDisplay()
        {
            var settings = new GameSettings();
            var native = Screen.currentResolution;
            settings.ResolutionWidth = native.width;
            settings.ResolutionHeight = native.height;
            settings.RefreshRate = GetCurrentRefreshRate();
            settings.Fullscreen = true;
            settings.FullscreenModeIndex = (int)global::RoobsWorld.Core.FullscreenMode.FullScreenWindow;
            return settings;
        }

        private void SanitizeLoadedSettings()
        {
            if (_current == null)
            {
                _current = CreateDefaultForCurrentDisplay();
                return;
            }

            _current.ResolutionWidth = Mathf.Max(640, _current.ResolutionWidth);
            _current.ResolutionHeight = Mathf.Max(360, _current.ResolutionHeight);
            _current.FullscreenModeIndex = Mathf.Clamp(_current.FullscreenModeIndex, 0, 3);
            _current.QualityLevel = Mathf.Clamp(_current.QualityLevel, 0, QualitySettings.names.Length - 1);
            _current.UITextScale = Mathf.Clamp(_current.UITextScale, 0.5f, 2f);
        }

        private static int GetCurrentRefreshRate()
        {
            var r = Screen.currentResolution;
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt((float)r.refreshRateRatio.value);
#else
            return r.refreshRate;
#endif
        }

        /// <summary>
        /// Restores default settings, applies them immediately, and saves them.
        /// </summary>
        public void ResetToDefaults()
        {
            _current = new GameSettings();
            Apply();
            Save();
        }
    }
}
