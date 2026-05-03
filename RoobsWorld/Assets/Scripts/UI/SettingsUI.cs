using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// In-game settings dialog for graphics, audio, and controls, backed by <see cref="GameSettings"/>.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        private const float PANEL_WIDTH = 700f;
        private const float PANEL_HEIGHT = 600f;
        private const float ROW_HEIGHT = 40f;
        private const float LABEL_WIDTH = 220f;
        private const float SLIDER_WIDTH = 300f;
        private const float TAB_HEIGHT = 40f;

        private Canvas _canvas;
        private GameObject _panelRoot;
        private GameObject _graphicsTab;
        private GameObject _audioTab;
        private GameObject _controlsTab;

        private Button _graphicsTabBtn;
        private Button _audioTabBtn;
        private Button _controlsTabBtn;

        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private Slider _ambientSlider;
        private Slider _sensitivitySlider;
        private Toggle _invertYToggle;
        private Toggle _fullscreenToggle;
        private Toggle _fpsToggle;
        private Toggle _vsyncToggle;
        private Text _qualityLabel;
        private int _qualityIndex;
        private Text _resolutionLabel;
        private int _resolutionIndex;
        private Text _fullscreenModeLabel;
        private int _fullscreenModeIndex;
        private Text _fpsLimitLabel;
        private int _fpsLimitIndex;
        private Text _colorBlindLabel;
        private int _colorBlindIndex;
        private Toggle _highContrastToggle;

        private GameSettings _editingSettings;

        private static readonly string[] QualityKeys = { "settings.quality_low", "settings.quality_medium", "settings.quality_high", "settings.quality_ultra" };
        private static readonly string[] ColorBlindKeys = { "settings.colorblind_none", "settings.colorblind_deuteranopia", "settings.colorblind_protanopia", "settings.colorblind_tritanopia" };
        private static readonly Vector2Int[] FallbackResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };

        private struct ResolutionOption
        {
            public int Width;
            public int Height;
            public int RefreshRate;
        }
        private ResolutionOption[] _resolutionOptions;
        private static readonly int[] FpsLimitValues = { 0, 60, 120, -1 };
        private static readonly string[] FpsLimitKeys = { "settings.fps_default", "settings.fps_60", "settings.fps_120", "settings.fps_unlimited" };

        public System.Action OnClosed;

        private Font _font;

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                RefreshFromSettings();
                return;
            }
            BuildUI();
        }

        public void Hide()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        private static float GetCanvasMatch()
        {
            float w = Screen.width;
            float h = Screen.height;
            if (w <= 0 || h <= 0) return 0.5f;
            float aspect = w / h;
            if (aspect > 2f) return 0f;
            if (aspect < 0.5f) return 1f;
            return 0.5f;
        }

        private void RefreshFromSettings()
        {
            if (!SettingsManager.HasInstance) return;
            _editingSettings = JsonUtility.FromJson<GameSettings>(
                JsonUtility.ToJson(SettingsManager.Instance.Current));
            SyncUIFromSettings();
        }

        private void BuildUI()
        {
            _font = UIStyleGuide.GetDefaultFont();

            if (SettingsManager.HasInstance)
                _editingSettings = JsonUtility.FromJson<GameSettings>(
                    JsonUtility.ToJson(SettingsManager.Instance.Current));
            else
                _editingSettings = new GameSettings();

            var canvasObj = new GameObject("SettingsCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 250;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler, GetCanvasMatch());
            canvasObj.AddComponent<GraphicRaycaster>();

            var dimmer = CreateFullRect(canvasObj.transform, "Dimmer");
            dimmer.AddComponent<Image>().color = UIStyleGuide.Colors.OverlayDim;

            _panelRoot = CreateCenteredPanel(canvasObj.transform);
            CreateTitle(_panelRoot.transform);
            CreateTabs(_panelRoot.transform);
            CreateGraphicsTab(_panelRoot.transform);
            CreateAudioTab(_panelRoot.transform);
            CreateControlsTab(_panelRoot.transform);
            CreateBottomButtons(_panelRoot.transform);

            ShowTab("graphics");
            SyncUIFromSettings();
        }

        private GameObject CreateCenteredPanel(Transform parent)
        {
            var panel = new GameObject("SettingsPanel");
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);
            var img = panel.AddComponent<Image>();
            img.color = UIStyleGuide.Colors.PanelBackgroundStrong;
            return panel;
        }

        private void CreateTitle(Transform parent)
        {
            var obj = new GameObject("Title");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(0, 40);

            var text = obj.AddComponent<Text>();
            text.font = _font;
            text.text = Localization.Get("settings.title");
            text.fontSize = UIStyleGuide.Typography.TitleMedium;
            text.color = UIStyleGuide.Colors.Accent;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
        }

        private void CreateTabs(Transform parent)
        {
            var tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(parent, false);
            var barRect = tabBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 1);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.pivot = new Vector2(0.5f, 1);
            barRect.anchoredPosition = new Vector2(0, -55);
            barRect.sizeDelta = new Vector2(-40, TAB_HEIGHT);

            var layout = tabBar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = UIStyleGuide.Layout.TightSpacing;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _graphicsTabBtn = CreateTabButton(tabBar.transform, Localization.Get("settings.graphics"), () => ShowTab("graphics"));
            _audioTabBtn = CreateTabButton(tabBar.transform, Localization.Get("settings.audio"), () => ShowTab("audio"));
            _controlsTabBtn = CreateTabButton(tabBar.transform, Localization.Get("settings.controls"), () => ShowTab("controls"));
        }

        private Button CreateTabButton(Transform parent, string label, System.Action onClick)
        {
            var obj = new GameObject($"Tab_{label}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var img = obj.AddComponent<Image>();
            img.color = UIStyleGuide.Colors.TabInactive;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            UIStyleGuide.ApplyButtonStyle(btn, UIButtonVariant.Tab);
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = UIStyleGuide.Typography.BodyMedium;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        private void ShowTab(string tabName)
        {
            if (_graphicsTab != null) _graphicsTab.SetActive(tabName == "graphics");
            if (_audioTab != null) _audioTab.SetActive(tabName == "audio");
            if (_controlsTab != null) _controlsTab.SetActive(tabName == "controls");

            SetTabColor(_graphicsTabBtn, tabName == "graphics");
            SetTabColor(_audioTabBtn, tabName == "audio");
            SetTabColor(_controlsTabBtn, tabName == "controls");
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? UIStyleGuide.Colors.TabActive : UIStyleGuide.Colors.TabInactive;
        }

        #region Graphics Tab

        private void CreateGraphicsTab(Transform parent)
        {
            _graphicsTab = CreateTabContent(parent, "GraphicsContent", -140);
            float y = 0;
            BuildResolutionOptions();

            (_resolutionLabel, _) = CreateCycleRow(_graphicsTab.transform, Localization.Get("settings.resolution"), ref y,
                () => CycleResolution(-1), () => CycleResolution(1));

            (_fullscreenModeLabel, _) = CreateCycleRow(_graphicsTab.transform, Localization.Get("settings.fullscreen_mode"), ref y,
                () => CycleFullscreenMode(-1), () => CycleFullscreenMode(1));

            (_qualityLabel, _) = CreateCycleRow(_graphicsTab.transform, Localization.Get("settings.quality"), ref y,
                () => CycleQuality(-1), () => CycleQuality(1));

            _fullscreenToggle = CreateToggleRow(_graphicsTab.transform, Localization.Get("settings.fullscreen"), ref y, OnFullscreenChanged);
            _vsyncToggle = CreateToggleRow(_graphicsTab.transform, Localization.Get("settings.vsync"), ref y, OnVSyncChanged);
            (_fpsLimitLabel, _) = CreateCycleRow(_graphicsTab.transform, Localization.Get("settings.target_fps"), ref y,
                () => CycleFpsLimit(-1), () => CycleFpsLimit(1));
            _fpsToggle = CreateToggleRow(_graphicsTab.transform, Localization.Get("settings.show_fps"), ref y, OnFPSChanged);
            (_colorBlindLabel, _) = CreateCycleRow(_graphicsTab.transform, Localization.Get("settings.colorblind_mode"), ref y,
                () => CycleColorBlindMode(-1), () => CycleColorBlindMode(1));
            _highContrastToggle = CreateToggleRow(_graphicsTab.transform, Localization.Get("settings.high_contrast"), ref y,
                val => _editingSettings.HighContrastMode = val);
        }

        private void BuildResolutionOptions()
        {
            var resolutions = Screen.resolutions;
            if (resolutions != null && resolutions.Length > 0)
            {
                var dict = new System.Collections.Generic.Dictionary<int, int>();
                foreach (var r in resolutions)
                {
                    int key = r.width * 10000 + r.height;
                    int rate = GetRefreshRate(r);
                    if (!dict.ContainsKey(key) || dict[key] < rate)
                        dict[key] = rate;
                }
                var list = new System.Collections.Generic.List<ResolutionOption>();
                foreach (var kv in dict)
                {
                    int w = kv.Key / 10000;
                    int h = kv.Key % 10000;
                    list.Add(new ResolutionOption { Width = w, Height = h, RefreshRate = kv.Value });
                }
                list.Sort((a, b) => (a.Width != b.Width) ? (a.Width.CompareTo(b.Width)) : (a.Height.CompareTo(b.Height)));
                _resolutionOptions = list.ToArray();
            }
            if (_resolutionOptions == null || _resolutionOptions.Length == 0)
            {
                _resolutionOptions = new ResolutionOption[FallbackResolutions.Length];
                for (int i = 0; i < FallbackResolutions.Length; i++)
                    _resolutionOptions[i] = new ResolutionOption { Width = FallbackResolutions[i].x, Height = FallbackResolutions[i].y, RefreshRate = 0 };
            }
        }

        private static int GetRefreshRate(Resolution r)
        {
#if UNITY_2022_2_OR_NEWER
            return Mathf.RoundToInt((float)r.refreshRateRatio.value);
#else
            return r.refreshRate;
#endif
        }

        private void CycleResolution(int dir)
        {
            if (_resolutionOptions == null || _resolutionOptions.Length == 0) return;
            _resolutionIndex = Mathf.Clamp(_resolutionIndex + dir, 0, _resolutionOptions.Length - 1);
            var r = _resolutionOptions[_resolutionIndex];
            _editingSettings.ResolutionWidth = r.Width;
            _editingSettings.ResolutionHeight = r.Height;
            _editingSettings.RefreshRate = r.RefreshRate > 0 ? r.RefreshRate : 0;
            _resolutionLabel.text = r.RefreshRate > 0 ? $"{r.Width} × {r.Height} @ {r.RefreshRate} Hz" : $"{r.Width} × {r.Height}";
        }

        private void CycleFullscreenMode(int dir)
        {
            _fullscreenModeIndex = Mathf.Clamp(_fullscreenModeIndex + dir, 0, 3);
            _editingSettings.FullscreenModeIndex = _fullscreenModeIndex;
            _editingSettings.Fullscreen = _fullscreenModeIndex != 3;
            if (_fullscreenToggle != null) _fullscreenToggle.isOn = _editingSettings.Fullscreen;
            _fullscreenModeLabel.text = Localization.Get(GetFullscreenModeKey(_fullscreenModeIndex));
        }

        private static string GetFullscreenModeKey(int index)
        {
            return index switch { 0 => "settings.fullscreen_exclusive", 1 => "settings.fullscreen_borderless", 2 => "settings.fullscreen_maximized", _ => "settings.fullscreen_windowed" };
        }

        private void CycleFpsLimit(int dir)
        {
            _fpsLimitIndex = Mathf.Clamp(_fpsLimitIndex + dir, 0, FpsLimitValues.Length - 1);
            _editingSettings.TargetFrameRate = FpsLimitValues[_fpsLimitIndex];
            _fpsLimitLabel.text = Localization.Get(FpsLimitKeys[_fpsLimitIndex]);
        }

        private void OnVSyncChanged(bool val)
        {
            _editingSettings.VSync = val;
        }

        private void CycleQuality(int dir)
        {
            _qualityIndex = Mathf.Clamp(_qualityIndex + dir, 0, QualityKeys.Length - 1);
            _editingSettings.QualityLevel = _qualityIndex;
            _qualityLabel.text = Localization.Get(QualityKeys[_qualityIndex]);
        }

        private void CycleColorBlindMode(int dir)
        {
            _colorBlindIndex = Mathf.Clamp(_colorBlindIndex + dir, 0, ColorBlindKeys.Length - 1);
            _editingSettings.ColorBlindModeIndex = _colorBlindIndex;
            if (_colorBlindLabel != null)
                _colorBlindLabel.text = Localization.Get(ColorBlindKeys[_colorBlindIndex]);
        }

        private void OnFullscreenChanged(bool val)
        {
            _editingSettings.Fullscreen = val;
            if (!val)
            {
                _fullscreenModeIndex = 3;
                _editingSettings.FullscreenModeIndex = 3;
                if (_fullscreenModeLabel != null) _fullscreenModeLabel.text = Localization.Get("settings.fullscreen_windowed");
            }
            else if (_fullscreenModeIndex == 3)
            {
                _fullscreenModeIndex = 1;
                _editingSettings.FullscreenModeIndex = 1;
                if (_fullscreenModeLabel != null) _fullscreenModeLabel.text = Localization.Get("settings.fullscreen_borderless");
            }
        }

        private void OnFPSChanged(bool val)
        {
            _editingSettings.ShowFPS = val;
        }

        #endregion

        #region Audio Tab

        private void CreateAudioTab(Transform parent)
        {
            _audioTab = CreateTabContent(parent, "AudioContent", -100);
            float y = 0;

            _masterSlider = CreateSliderRow(_audioTab.transform, Localization.Get("settings.volume_master"), ref y, OnMasterChanged);
            _musicSlider = CreateSliderRow(_audioTab.transform, Localization.Get("settings.volume_music"), ref y, OnMusicChanged);
            _sfxSlider = CreateSliderRow(_audioTab.transform, Localization.Get("settings.volume_sfx"), ref y, OnSFXChanged);
            _ambientSlider = CreateSliderRow(_audioTab.transform, Localization.Get("settings.volume_ambient"), ref y, OnAmbientChanged);
        }

        private void OnMasterChanged(float val) => _editingSettings.MasterVolume = val;
        private void OnMusicChanged(float val) => _editingSettings.MusicVolume = val;
        private void OnSFXChanged(float val) => _editingSettings.SFXVolume = val;
        private void OnAmbientChanged(float val) => _editingSettings.AmbientVolume = val;

        #endregion

        #region Controls Tab

        private void CreateControlsTab(Transform parent)
        {
            _controlsTab = CreateTabContent(parent, "ControlsContent", -100);
            float y = 0;

            _sensitivitySlider = CreateSliderRow(_controlsTab.transform, Localization.Get("settings.camera_sensitivity"), ref y,
                val => _editingSettings.CameraSensitivity = val, 0.1f, 3f);
            _invertYToggle = CreateToggleRow(_controlsTab.transform, Localization.Get("settings.invert_y"), ref y,
                val => _editingSettings.InvertY = val);
        }

        #endregion

        #region Bottom Buttons

        private void CreateBottomButtons(Transform parent)
        {
            var bar = new GameObject("ButtonBar");
            bar.transform.SetParent(parent, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, 15);
            barRect.sizeDelta = new Vector2(-40, 45);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;

            CreateActionButton(bar.transform, Localization.Get("settings.apply"), OnApply);
            CreateActionButton(bar.transform, Localization.Get("settings.reset"), OnResetDefaults);
            CreateActionButton(bar.transform, Localization.Get("tutorial.reset"), OnResetTutorial);
            CreateActionButton(bar.transform, Localization.Get("settings.back"), OnBack);
        }

        private void OnApply()
        {
            if (!SettingsManager.HasInstance) return;
            var current = SettingsManager.Instance.Current;
            current.ResolutionWidth = _editingSettings.ResolutionWidth;
            current.ResolutionHeight = _editingSettings.ResolutionHeight;
            current.RefreshRate = _editingSettings.RefreshRate;
            current.Fullscreen = _editingSettings.Fullscreen;
            current.FullscreenModeIndex = _editingSettings.FullscreenModeIndex;
            current.QualityLevel = _editingSettings.QualityLevel;
            current.VSync = _editingSettings.VSync;
            current.TargetFrameRate = _editingSettings.TargetFrameRate;
            current.MasterVolume = _editingSettings.MasterVolume;
            current.MusicVolume = _editingSettings.MusicVolume;
            current.SFXVolume = _editingSettings.SFXVolume;
            current.AmbientVolume = _editingSettings.AmbientVolume;
            current.CameraSensitivity = _editingSettings.CameraSensitivity;
            current.InvertY = _editingSettings.InvertY;
            current.ShowFPS = _editingSettings.ShowFPS;
            current.ColorBlindModeIndex = _editingSettings.ColorBlindModeIndex;
            current.HighContrastMode = _editingSettings.HighContrastMode;

            SettingsManager.Instance.Apply();
            SettingsManager.Instance.Save();
            Debug.Log("[SettingsUI] Settings applied and saved");
        }

        private void OnResetDefaults()
        {
            _editingSettings = new GameSettings();
            SyncUIFromSettings();

            if (SettingsManager.HasInstance)
                SettingsManager.Instance.ResetToDefaults();

            Debug.Log("[SettingsUI] Settings reset to defaults");
        }

        private void OnResetTutorial()
        {
            TutorialOverlay.Instance?.ResetProgress();
            Debug.Log("[SettingsUI] Tutorial progress reset");
        }

        private void OnBack()
        {
            Hide();
            OnClosed?.Invoke();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnBack();
            }
        }

        #endregion

        #region UI Helpers

        private void SyncUIFromSettings()
        {
            if (_editingSettings == null) return;

            if (_resolutionOptions != null && _resolutionOptions.Length > 0)
            {
                _resolutionIndex = FindResolutionIndex(_editingSettings.ResolutionWidth, _editingSettings.ResolutionHeight);
                var r = _resolutionOptions[_resolutionIndex];
                if (_resolutionLabel != null)
                    _resolutionLabel.text = r.RefreshRate > 0 ? $"{r.Width} × {r.Height} @ {r.RefreshRate} Hz" : $"{r.Width} × {r.Height}";
            }

            _fullscreenModeIndex = Mathf.Clamp(_editingSettings.FullscreenModeIndex, 0, 3);
            if (_fullscreenModeLabel != null)
                _fullscreenModeLabel.text = Localization.Get(GetFullscreenModeKey(_fullscreenModeIndex));

            _qualityIndex = Mathf.Clamp(_editingSettings.QualityLevel, 0, QualityKeys.Length - 1);
            if (_qualityLabel != null)
                _qualityLabel.text = Localization.Get(QualityKeys[_qualityIndex]);

            if (_fullscreenToggle != null) _fullscreenToggle.isOn = _editingSettings.Fullscreen;
            if (_vsyncToggle != null) _vsyncToggle.isOn = _editingSettings.VSync;
            _fpsLimitIndex = FindFpsLimitIndex(_editingSettings.TargetFrameRate);
            if (_fpsLimitLabel != null)
                _fpsLimitLabel.text = Localization.Get(FpsLimitKeys[_fpsLimitIndex]);
            if (_fpsToggle != null) _fpsToggle.isOn = _editingSettings.ShowFPS;

            _colorBlindIndex = Mathf.Clamp(_editingSettings.ColorBlindModeIndex, 0, ColorBlindKeys.Length - 1);
            if (_colorBlindLabel != null)
                _colorBlindLabel.text = Localization.Get(ColorBlindKeys[_colorBlindIndex]);
            if (_highContrastToggle != null) _highContrastToggle.isOn = _editingSettings.HighContrastMode;

            if (_masterSlider != null) _masterSlider.value = _editingSettings.MasterVolume;
            if (_musicSlider != null) _musicSlider.value = _editingSettings.MusicVolume;
            if (_sfxSlider != null) _sfxSlider.value = _editingSettings.SFXVolume;
            if (_ambientSlider != null) _ambientSlider.value = _editingSettings.AmbientVolume;

            if (_sensitivitySlider != null) _sensitivitySlider.value = _editingSettings.CameraSensitivity;
            if (_invertYToggle != null) _invertYToggle.isOn = _editingSettings.InvertY;
        }

        private int FindResolutionIndex(int w, int h)
        {
            if (_resolutionOptions == null) return 0;
            for (int i = 0; i < _resolutionOptions.Length; i++)
                if (_resolutionOptions[i].Width == w && _resolutionOptions[i].Height == h) return i;
            return Mathf.Clamp(_resolutionOptions.Length / 2, 0, _resolutionOptions.Length - 1);
        }

        private static int FindFpsLimitIndex(int value)
        {
            for (int i = 0; i < FpsLimitValues.Length; i++)
                if (FpsLimitValues[i] == value) return i;
            return 0;
        }

        private GameObject CreateTabContent(Transform parent, string name, float topOffset)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(20, 70);
            rect.offsetMax = new Vector2(-20, topOffset);
            return obj;
        }

        private Slider CreateSliderRow(Transform parent, string label, ref float y,
            System.Action<float> onChange, float min = 0f, float max = 1f)
        {
            var row = CreateRow(parent, label, ref y);

            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(0, 0.5f);
            sliderRect.pivot = new Vector2(0, 0.5f);
            sliderRect.anchoredPosition = new Vector2(LABEL_WIDTH, 0);
            sliderRect.sizeDelta = new Vector2(SLIDER_WIDTH, 20);

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = UIStyleGuide.Colors.SurfaceMuted;

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = UIStyleGuide.Colors.Accent;

            var handleArea = new GameObject("HandleSlideArea");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);
            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, 24);
            var handleImg = handleObj.AddComponent<Image>();
            handleImg.color = Color.white;

            var slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(val => onChange?.Invoke(val));

            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(row.transform, false);
            var valRect = valueObj.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0, 0.5f);
            valRect.anchorMax = new Vector2(0, 0.5f);
            valRect.pivot = new Vector2(0, 0.5f);
            valRect.anchoredPosition = new Vector2(LABEL_WIDTH + SLIDER_WIDTH + 10, 0);
            valRect.sizeDelta = new Vector2(60, ROW_HEIGHT);
            var valText = valueObj.AddComponent<Text>();
            valText.font = _font;
            valText.fontSize = 16;
            valText.color = UIStyleGuide.Colors.BodyText;
            valText.alignment = TextAnchor.MiddleLeft;

            slider.onValueChanged.AddListener(v =>
            {
                if (max <= 1f) valText.text = Mathf.RoundToInt(v * 100) + "%";
                else valText.text = v.ToString("F1");
            });

            return slider;
        }

        private Toggle CreateToggleRow(Transform parent, string label, ref float y,
            System.Action<bool> onChange)
        {
            var row = CreateRow(parent, label, ref y);

            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(row.transform, false);
            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 0.5f);
            toggleRect.anchorMax = new Vector2(0, 0.5f);
            toggleRect.pivot = new Vector2(0, 0.5f);
            toggleRect.anchoredPosition = new Vector2(LABEL_WIDTH, 0);
            toggleRect.sizeDelta = new Vector2(30, 30);

            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = UIStyleGuide.Colors.SurfaceMuted;

            var checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);
            var checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImg = checkObj.AddComponent<Image>();
            checkImg.color = UIStyleGuide.Colors.Accent;

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.onValueChanged.AddListener(val => onChange?.Invoke(val));

            return toggle;
        }

        private (Text valueText, GameObject row) CreateCycleRow(Transform parent, string label, ref float y,
            System.Action onPrev, System.Action onNext)
        {
            var row = CreateRow(parent, label, ref y);

            CreateArrowButton(row.transform, "<", LABEL_WIDTH - 5, onPrev);

            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(row.transform, false);
            var valRect = valueObj.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0, 0.5f);
            valRect.anchorMax = new Vector2(0, 0.5f);
            valRect.pivot = new Vector2(0, 0.5f);
            valRect.anchoredPosition = new Vector2(LABEL_WIDTH + 35, 0);
            valRect.sizeDelta = new Vector2(200, ROW_HEIGHT);
            var valText = valueObj.AddComponent<Text>();
            valText.font = _font;
            valText.fontSize = 18;
            valText.color = UIStyleGuide.Colors.BodyText;
            valText.alignment = TextAnchor.MiddleCenter;

            CreateArrowButton(row.transform, ">", LABEL_WIDTH + 245, onNext);

            return (valText, row);
        }

        private void CreateArrowButton(Transform parent, string label, float x, System.Action onClick)
        {
            var obj = new GameObject($"Btn_{label}");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(0, 0.5f);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0);
            rect.sizeDelta = new Vector2(30, 30);

            var img = obj.AddComponent<Image>();
            img.color = Color.white;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            UIStyleGuide.ApplyButtonStyle(btn, UIButtonVariant.Secondary);
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = UIStyleGuide.Typography.BodyMedium;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateActionButton(Transform parent, string label, System.Action onClick)
        {
            var obj = new GameObject($"Btn_{label}");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<LayoutElement>();

            var img = obj.AddComponent<Image>();
            img.color = Color.white;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            UIStyleGuide.ApplyButtonStyle(btn, UIButtonVariant.Secondary);
            btn.onClick.AddListener(() => onClick?.Invoke());

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = UIStyleGuide.Typography.BodyMedium;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
        }

        private GameObject CreateRow(Transform parent, string label, ref float y)
        {
            var row = new GameObject($"Row_{label}");
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -y);
            rect.sizeDelta = new Vector2(0, ROW_HEIGHT);

            var bg = row.AddComponent<Image>();
            bg.color = (Mathf.FloorToInt(y / ROW_HEIGHT) % 2 == 0) ? UIStyleGuide.Colors.RowStripe : Color.clear;

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(10, 0);
            labelRect.sizeDelta = new Vector2(LABEL_WIDTH, 0);
            var text = labelObj.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = UIStyleGuide.Typography.BodySmall;
            text.color = UIStyleGuide.Colors.BodyTextMuted;
            text.alignment = TextAnchor.MiddleLeft;

            y += ROW_HEIGHT + 5;
            return row;
        }

        private GameObject CreateFullRect(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return obj;
        }

        #endregion

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
