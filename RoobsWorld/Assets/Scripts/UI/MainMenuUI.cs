using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Startup title screen with Play Game, Settings, Achievements, and Quit actions.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        private const float TitleLogoScale = 2f;

        private Canvas _canvas;
        private Font _font;
        private TMP_FontAsset _tmpFont;
        private SettingsUI _settingsUI;
        private AchievementsUI _achievementsUI;
        private MultiplayerLobbyUI _multiplayerLobbyUI;
        private MainMenuController _controller;
        private Sprite _panelSprite;
        private Sprite _buttonSprite;
        private Texture2D _waterTexture;
        private Texture2D _doodleTexture;
        private Texture2D _glowTexture;
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly List<Sprite> _sprites = new List<Sprite>();
        private readonly List<Material> _materials = new List<Material>();

        private struct MenuMicrobeLayout
        {
            public Vector2 Anchor;
            public Vector2 Size;

            public MenuMicrobeLayout(Vector2 anchor, Vector2 size)
            {
                Anchor = anchor;
                Size = size;
            }
        }

        private void Start()
        {
            _controller = new MainMenuController(new GameSessionAdapter());
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);

            foreach (var sprite in _sprites)
                if (sprite != null)
                    Destroy(sprite);
            foreach (var texture in _textures)
                if (texture != null)
                    Destroy(texture);
            foreach (var material in _materials)
                if (material != null)
                    Destroy(material);
        }

        private void BuildUI()
        {
            _font = UIStyleGuide.GetDefaultFont();
            _tmpFont = ResolveTMPFontAsset();
            EnsureArt();

            var canvasObj = new GameObject("MainMenuCanvas");
            canvasObj.transform.SetParent(transform, false);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = true;
            _canvas.sortingOrder = 220;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            scaler.dynamicPixelsPerUnit = 2f;
            canvasObj.AddComponent<GraphicRaycaster>();

            CreateBackdrop(canvasObj.transform);
            CreateCreatureCollage(canvasObj.transform);
            CreateMenuPanel(canvasObj.transform);
        }

        private TMP_FontAsset ResolveTMPFontAsset()
        {
            try
            {
                var defaultAsset = TMP_Settings.defaultFontAsset;
                if (defaultAsset != null)
                    return defaultAsset;
            }
            catch
            {
                // TMP settings may not be initialized yet in some startup paths.
            }

            if (_font != null)
            {
                var createdFromUiFont = TMP_FontAsset.CreateFontAsset(_font);
                if (createdFromUiFont != null)
                    return createdFromUiFont;
            }

            var resourceFont = Resources.Load<Font>(UIStyleGuide.Typography.DefaultFontResourcePath);
            return resourceFont != null ? TMP_FontAsset.CreateFontAsset(resourceFont) : null;
        }

        private void CreateBackdrop(Transform parent)
        {
            var waterObj = CreateFullRect(parent, "PaintedAquariumBackdrop");
            var water = waterObj.AddComponent<RawImage>();
            water.texture = _waterTexture;
            water.material = CreateLivingMenuMaterial(
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TextOnCool, 0.98f),
                UIStyleGuide.Colors.DeepNavy,
                UIStyleGuide.Colors.TideTealBright,
                UIStyleGuide.Colors.FossilAmber,
                0.13f,
                0.36f,
                0.58f,
                0.38f,
                12.7f);
            water.raycastTarget = false;

            CreateGlow(parent, "NorthernBloom", new Vector2(0.18f, 0.82f), new Vector2(760f, 430f), UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.18f));
            CreateGlow(parent, "EasternBloom", new Vector2(0.88f, 0.5f), new Vector2(640f, 390f), UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.18f));
            CreateGlow(parent, "SouthernBloom", new Vector2(0.44f, 0.08f), new Vector2(880f, 360f), UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.Amethyst, 0.13f));
            CreateLightShaft(parent, "SurfaceRayLeft", new Vector2(0.08f, 0.48f), new Vector2(360f, 980f), -19f, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.1f), 0.2f);
            CreateLightShaft(parent, "SurfaceRayCenter", new Vector2(0.46f, 0.5f), new Vector2(420f, 1100f), 8f, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.1f), 0.3f);
            CreateLightShaft(parent, "SurfaceRayRight", new Vector2(0.83f, 0.52f), new Vector2(330f, 920f), 17f, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.CoralBright, 0.08f), 0.4f);

            var doodleObj = CreateFullRect(parent, "HandDrawnSporePattern");
            var doodles = doodleObj.AddComponent<RawImage>();
            doodles.texture = _doodleTexture;
            doodles.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TextOnCool, 0.38f);
            doodles.material = CreateLivingMenuMaterial(
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.36f),
                Color.clear,
                UIStyleGuide.Colors.TideTealBright,
                UIStyleGuide.Colors.Coral,
                0.22f,
                0.14f,
                0.12f,
                0.24f,
                51.1f);
            doodles.raycastTarget = false;
            doodleObj.AddComponent<MenuFloat>().Configure(5f, 0.15f, 0.2f);

            CreateBubbleField(parent);
            CreateCausticRings(parent);

            var wash = CreateFullRect(parent, "MenuReadabilityWash");
            wash.AddComponent<Image>().color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.DeepNavy, 0.18f);
        }

        private void CreateCreatureCollage(Transform parent)
        {
            var rng = new System.Random(4428);
            var palette = new[]
            {
                UIStyleGuide.Colors.Coral, UIStyleGuide.Colors.FossilAmber, UIStyleGuide.Colors.FossilGold,
                UIStyleGuide.Colors.TideTeal, UIStyleGuide.Colors.TideTealBright, UIStyleGuide.Colors.Amethyst,
                UIStyleGuide.Colors.InkBlue, UIStyleGuide.Colors.AlgaeLime, UIStyleGuide.Colors.ShellBrown
            };

            for (int i = 0; i < 32; i++)
            {
                MenuMicrobeLayout layout = CreateSeparatedMicrobeLayout(rng, i);
                var creature = new GameObject($"MenuMicrobe_{i:00}");
                creature.transform.SetParent(parent, false);
                var rect = creature.AddComponent<RectTransform>();
                rect.anchorMin = layout.Anchor;
                rect.anchorMax = layout.Anchor;
                rect.sizeDelta = layout.Size;
                rect.localRotation = Quaternion.Euler(0f, 0f, Rand(rng, -32f, 32f));

                var color = palette[rng.Next(palette.Length)];
                var image = creature.AddComponent<Image>();
                image.sprite = CreateSprite(MicrobeTexture(480, 360, 9000 + i * 97, color, Color.Lerp(Color.black, color, 0.22f), rng.Next(1, 7)));
                image.preserveAspect = true;
                image.raycastTarget = false;
                creature.AddComponent<MenuCreatureDrift>().Configure(
                    Rand(rng, 18f, 46f),
                    Rand(rng, 14f, 38f),
                    Rand(rng, 10f, 26f),
                    Rand(rng, 55f, 110f),
                    Rand(rng, 0f, Mathf.PI * 2f));
            }
        }

        private static MenuMicrobeLayout CreateSeparatedMicrobeLayout(System.Random rng, int index)
        {
            Vector2[] anchors =
            {
                new Vector2(0.055f, 0.13f), new Vector2(0.055f, 0.31f), new Vector2(0.055f, 0.49f), new Vector2(0.055f, 0.67f), new Vector2(0.055f, 0.85f),
                new Vector2(0.155f, 0.21f), new Vector2(0.155f, 0.39f), new Vector2(0.155f, 0.57f), new Vector2(0.155f, 0.75f),
                new Vector2(0.845f, 0.15f), new Vector2(0.845f, 0.33f), new Vector2(0.845f, 0.51f), new Vector2(0.845f, 0.69f), new Vector2(0.845f, 0.87f),
                new Vector2(0.945f, 0.23f), new Vector2(0.945f, 0.41f), new Vector2(0.945f, 0.59f), new Vector2(0.945f, 0.77f),
                new Vector2(0.26f, 0.07f), new Vector2(0.42f, 0.07f), new Vector2(0.58f, 0.07f), new Vector2(0.74f, 0.07f),
                new Vector2(0.25f, 0.94f), new Vector2(0.41f, 0.94f), new Vector2(0.57f, 0.94f), new Vector2(0.73f, 0.94f),
                new Vector2(0.09f, 1.04f), new Vector2(0.31f, 1.045f), new Vector2(0.69f, 1.045f), new Vector2(0.91f, 1.04f),
                new Vector2(0.08f, -0.04f), new Vector2(0.92f, -0.04f)
            };

            Vector2 anchor = anchors[index % anchors.Length];
            anchor += new Vector2(Rand(rng, -0.018f, 0.018f), Rand(rng, -0.014f, 0.014f));
            Vector2 size = new Vector2(Rand(rng, 92f, 154f), Rand(rng, 68f, 118f));
            return new MenuMicrobeLayout(anchor, size);
        }

        private void CreateMenuPanel(Transform parent)
        {
            var rim = new GameObject("MainMenuPanelPaintedRim");
            rim.transform.SetParent(parent, false);
            var rimRect = rim.AddComponent<RectTransform>();
            rimRect.anchorMin = rimRect.anchorMax = new Vector2(0.5f, 0.5f);
            rimRect.sizeDelta = new Vector2(670f, 790f);
            var rimImage = rim.AddComponent<Image>();
            rimImage.sprite = _panelSprite;
            rimImage.type = Image.Type.Sliced;
            rimImage.color = Color.clear;
            rimImage.raycastTarget = false;

            var panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(632f, 752f);
            var panelImage = panel.AddComponent<Image>();
            panelImage.sprite = _panelSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.clear;
            panelImage.raycastTarget = false;

            float startY = 0.335f;
            float spacing = 0.09f;
            CreateMenuButton(panel.transform, "Play Game", startY, true, OnMultiplayer);
            CreateMenuButton(panel.transform, Localization.Get("menu.settings"), startY - spacing, false, OnSettings);
            CreateMenuButton(panel.transform, Localization.Get("menu.achievements"), startY - spacing * 2f, false, OnAchievements);
            CreateMenuButton(panel.transform, Localization.Get("menu.quit"), startY - spacing * 3f, false, OnQuit);

            CreateTitleLogo(panel.transform);
        }

        private void CreateTitleLogo(Transform parent)
        {
            var logoTexture = Resources.Load<Texture2D>("UI/roobs-world-logo");
            if (logoTexture == null)
            {
                var titleObj = AddText(parent, "Title", Localization.Get("menu.title"), new Vector2(0.1f, 0.61f), new Vector2(0.9f, 0.985f), 108, UIStyleGuide.Colors.CoralBright, FontStyle.Bold);
                var titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
                titleRect.localScale = Vector3.one * TitleLogoScale;
                var titleText = titleObj.GetComponent<TextMeshProUGUI>();
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.fontSizeMin = 72f;
                titleText.color = UIStyleGuide.Colors.CoralBright;
                titleText.outlineColor = UIStyleGuide.Colors.DeepNavy;
                titleText.outlineWidth = 0.22f;
                titleText.characterSpacing = 0.8f;
                return;
            }

            var logoObj = new GameObject("TitleLogo");
            logoObj.transform.SetParent(parent, false);
            var rect = logoObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.61f);
            rect.anchorMax = new Vector2(0.9f, 0.985f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one * TitleLogoScale;

            var image = logoObj.AddComponent<Image>();
            image.sprite = Sprite.Create(
                logoTexture,
                new Rect(0f, 0f, logoTexture.width, logoTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void CreateMenuButton(Transform parent, string label, float yCenter, bool primary, System.Action onClick)
        {
            var buttonObj = new GameObject($"Btn_{label}");
            buttonObj.transform.SetParent(parent, false);
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, yCenter - 0.038f);
            rect.anchorMax = new Vector2(0.85f, yCenter + 0.038f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = buttonObj.AddComponent<Image>();
            image.sprite = _buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = primary
                ? UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.96f)
                : UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.92f);

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            UIStyleGuide.ApplyButtonStyle(button, primary ? UIButtonVariant.Primary : UIButtonVariant.Secondary);
            button.onClick.AddListener(() => onClick?.Invoke());
            buttonObj.AddComponent<MenuButtonWiggle>().Configure(image, primary);

            var pearl = new GameObject("Pearl");
            pearl.transform.SetParent(buttonObj.transform, false);
            var pearlRect = pearl.AddComponent<RectTransform>();
            pearlRect.anchorMin = pearlRect.anchorMax = new Vector2(0.055f, 0.5f);
            pearlRect.sizeDelta = Vector2.one * (primary ? 34f : 24f);
            var pearlImage = pearl.AddComponent<RawImage>();
            pearlImage.texture = _glowTexture;
            pearlImage.color = primary
                ? UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.9f)
                : UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.66f);
            pearlImage.raycastTarget = false;
            pearl.AddComponent<MenuPulse>().Configure(primary ? 0.16f : 0.1f, primary ? 2.2f : 1.4f);

            var textObj = AddText(buttonObj.transform, "Text", label, new Vector2(0.08f, 0f), new Vector2(0.94f, 1f), UIStyleGuide.Typography.BodyLarge, UIStyleGuide.GetButtonTextColor(primary ? UIButtonVariant.Primary : UIButtonVariant.Secondary), primary ? FontStyle.Bold : FontStyle.Normal);
            UIStyleGuide.ApplyTextShadow(textObj, primary ? 0.18f : 0.34f);
        }

        private void OnMultiplayer()
        {
            if (_multiplayerLobbyUI == null)
            {
                var lobbyObj = new GameObject("MainMenuMultiplayerLobby");
                lobbyObj.transform.SetParent(transform, false);
                _multiplayerLobbyUI = lobbyObj.AddComponent<MultiplayerLobbyUI>();
                _multiplayerLobbyUI.Bind(GameManager.HasInstance ? GameManager.Instance.Session : null);
                _multiplayerLobbyUI.OnStartGameRequested = StartMultiplayerGame;
                _multiplayerLobbyUI.OnClosed = () =>
                {
                    if (_multiplayerLobbyUI != null)
                    {
                        Destroy(_multiplayerLobbyUI.gameObject);
                        _multiplayerLobbyUI = null;
                    }
                };
            }

            _multiplayerLobbyUI.Open();
        }

        private void StartMultiplayerGame()
        {
            if (!GameManager.HasInstance || GameManager.Instance.Session == null)
                return;

            var lobby = _multiplayerLobbyUI;
            if (lobby != null)
            {
                // Prevent OnClosed from nulling the field between Close() and teardown steps.
                lobby.OnClosed = null;
                lobby.Close();
                lobby.transform.SetParent(null, false);
                Destroy(lobby.gameObject);
                if (ReferenceEquals(_multiplayerLobbyUI, lobby))
                    _multiplayerLobbyUI = null;
            }

            _controller.StartSinglePlayer();
            Destroy(gameObject);
        }

        private void OnSettings()
        {
            if (_settingsUI == null)
            {
                var settingsObj = new GameObject("MainMenuSettingsUI");
                settingsObj.transform.SetParent(transform, false);
                _settingsUI = settingsObj.AddComponent<SettingsUI>();
                _settingsUI.OnClosed = () => _settingsUI.Hide();
            }
            _settingsUI.Show();
        }

        private void OnAchievements()
        {
            if (_achievementsUI == null)
            {
                var achievementsObj = new GameObject("MainMenuAchievementsUI");
                achievementsObj.transform.SetParent(transform, false);
                _achievementsUI = achievementsObj.AddComponent<AchievementsUI>();
                _achievementsUI.OnClosed = () => _achievementsUI.Hide();
            }
            _achievementsUI.Show();
        }

        private void OnQuit()
        {
            _controller?.Quit();
        }

        private void EnsureArt()
        {
            _waterTexture = _waterTexture ?? Track(MainMenuArtFactory.CreateWaterTexture(1920, 1080));
            _doodleTexture = _doodleTexture ?? Track(MainMenuArtFactory.CreateDoodleTexture(1920, 1080));
            _glowTexture = _glowTexture ?? Track(MainMenuArtFactory.CreateGlowTexture(512));
            _panelSprite = _panelSprite ?? Track(Sprite.Create(Track(MainMenuArtFactory.CreateRoundedTexture(224, 224, 60, UIStyleGuide.Colors.TextOnCool, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.82f), 10)), new Rect(0f, 0f, 224f, 224f), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(60f, 60f, 60f, 60f)));
            _buttonSprite = _buttonSprite ?? Track(Sprite.Create(Track(MainMenuArtFactory.CreateRoundedTexture(224, 104, 46, UIStyleGuide.Colors.TextOnCool, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.78f), 8)), new Rect(0f, 0f, 224f, 104f), new Vector2(0.5f, 0.5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(46f, 46f, 46f, 46f)));
        }

        private Texture2D Track(Texture2D texture)
        {
            _textures.Add(texture);
            return texture;
        }

        private Sprite Track(Sprite sprite)
        {
            _sprites.Add(sprite);
            return sprite;
        }

        private Sprite CreateSprite(Texture2D texture)
        {
            return Track(Sprite.Create(Track(texture), new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f));
        }

        private void CreateGlow(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            var image = obj.AddComponent<RawImage>();
            image.texture = _glowTexture;
            image.color = color;
            image.raycastTarget = false;
        }

        private Material CreateLivingMenuMaterial(Color tint, Color deep, Color bloom, Color accent, float speed, float warp, float caustics, float plankton, float seed)
        {
            var shader = Shader.Find("RoobsWorld/UI/MenuLivingBackdrop");
            if (shader == null)
                return null;

            var material = new Material(shader);
            material.SetColor("_Tint", tint);
            material.SetColor("_DeepColor", deep);
            material.SetColor("_BloomColor", bloom);
            material.SetColor("_AccentColor", accent);
            material.SetFloat("_FlowSpeed", speed);
            material.SetFloat("_Warp", warp);
            material.SetFloat("_CausticStrength", caustics);
            material.SetFloat("_PlanktonStrength", plankton);
            material.SetFloat("_VignetteStrength", 0.24f);
            material.SetFloat("_Seed", seed);
            _materials.Add(material);
            return material;
        }

        private void CreateLightShaft(Transform parent, string name, Vector2 anchor, Vector2 size, float rotation, Color color, float phase)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

            var image = obj.AddComponent<RawImage>();
            image.texture = _glowTexture;
            image.color = color;
            image.raycastTarget = false;
            obj.AddComponent<MenuFloat>().Configure(8f, 0.35f, 0.16f + phase);
            obj.AddComponent<MenuPulse>().Configure(0.08f, 0.32f + phase);
        }

        private void CreateBubbleField(Transform parent)
        {
            var rng = new System.Random(3108);
            for (int i = 0; i < 44; i++)
            {
                float x = Rand(rng, 0.02f, 0.98f);
                float y = Rand(rng, -0.08f, 1.08f);
                float size = Rand(rng, 5f, 21f);

                var obj = new GameObject($"MenuBubble_{i:00}");
                obj.transform.SetParent(parent, false);
                var rect = obj.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(x, y);
                rect.sizeDelta = Vector2.one * size;

                var image = obj.AddComponent<RawImage>();
                image.texture = _glowTexture;
                image.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, Rand(rng, 0.08f, 0.22f));
                image.raycastTarget = false;
                obj.AddComponent<MenuBubbleDrift>().Configure(Rand(rng, 18f, 62f), Rand(rng, 5f, 28f), Rand(rng, 0.1f, 0.34f), Rand(rng, 0f, 20f));
            }
        }

        private void CreateCausticRings(Transform parent)
        {
            var rng = new System.Random(9012);
            for (int i = 0; i < 9; i++)
            {
                var obj = new GameObject($"MenuCausticRing_{i:00}");
                obj.transform.SetParent(parent, false);
                var rect = obj.AddComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(Rand(rng, 0.04f, 0.96f), Rand(rng, 0.05f, 0.95f));
                float size = Rand(rng, 96f, 230f);
                rect.sizeDelta = new Vector2(size, size * Rand(rng, 0.55f, 0.9f));
                rect.localRotation = Quaternion.Euler(0f, 0f, Rand(rng, -35f, 35f));

                var image = obj.AddComponent<Image>();
                image.sprite = CreateSprite(RingTexture(180, 120, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, Rand(rng, 0.12f, 0.24f))));
                image.preserveAspect = true;
                image.raycastTarget = false;
                obj.AddComponent<MenuFloat>().Configure(Rand(rng, 2f, 8f), Rand(rng, 0.7f, 2.8f), Rand(rng, 0.16f, 0.5f));
                obj.AddComponent<MenuPulse>().Configure(Rand(rng, 0.08f, 0.18f), Rand(rng, 0.28f, 0.55f));
            }
        }

        private void CreateHeroAura(Transform parent)
        {
            var aura = new GameObject("HeroMicrobeBioAura");
            aura.transform.SetParent(parent, false);
            var rect = aura.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.61f);
            rect.sizeDelta = new Vector2(286f, 210f);
            var image = aura.AddComponent<Image>();
            image.sprite = CreateSprite(RingTexture(260, 190, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.28f)));
            image.preserveAspect = true;
            image.raycastTarget = false;
            aura.transform.SetAsFirstSibling();
            aura.AddComponent<MenuFloat>().Configure(5f, 2.5f, 0.42f);
            aura.AddComponent<MenuPulse>().Configure(0.1f, 0.78f);
        }

        private GameObject AddText(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color, FontStyle fontStyle)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var tmpText = obj.AddComponent<TextMeshProUGUI>();
            if (_tmpFont != null)
                tmpText.font = _tmpFont;
            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.enableAutoSizing = true;
            tmpText.fontSizeMax = fontSize;
            tmpText.fontSizeMin = Mathf.Max(10f, fontSize * 0.65f);
            tmpText.color = color;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.textWrappingMode = TextWrappingModes.Normal;
            tmpText.overflowMode = TextOverflowModes.Truncate;
            tmpText.fontStyle = ToTMPFontStyle(fontStyle);
            return obj;
        }

        private static FontStyles ToTMPFontStyle(FontStyle style)
        {
            return style switch
            {
                FontStyle.Bold => FontStyles.Bold,
                FontStyle.Italic => FontStyles.Italic,
                FontStyle.BoldAndItalic => FontStyles.Bold | FontStyles.Italic,
                _ => FontStyles.Normal
            };
        }

        private static GameObject CreateFullRect(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return obj;
        }

        private static Texture2D WaterTexture(int width, int height)
        {
            var texture = NewTexture(width, height, TextureWrapMode.Clamp);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = y / (height - 1f);
                for (int x = 0; x < width; x++)
                {
                    float u = x / (width - 1f);
                    var color = Color.Lerp(UIStyleGuide.Colors.DeepNavy, UIStyleGuide.Colors.InkBlue, Mathf.Sin(v * Mathf.PI));
                    color = Color.Lerp(color, UIStyleGuide.Colors.TideTeal, v * 0.16f);
                    color += UIStyleGuide.Colors.TideTealBright * Mathf.PerlinNoise(u * 2.4f + 1.2f, v * 2.2f) * 0.08f;
                    color += UIStyleGuide.Colors.FossilGold * Mathf.PerlinNoise(u * 5f - v, v * 3.6f + 6f) * 0.025f;
                    color.a = 1f;
                    pixels[y * width + x] = Clamp(color);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D DoodleTexture(int width, int height, int count)
        {
            var texture = NewTexture(width, height, TextureWrapMode.Repeat);
            var pixels = ClearPixels(width, height);
            var rng = new System.Random(8732);
            var line = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.86f);
            for (int i = 0; i < count; i++)
            {
                int x = rng.Next(18, width - 18);
                int y = rng.Next(18, height - 18);
                int size = rng.Next(8, 22);
                int type = rng.Next(3);
                if (type == 0)
                    Ellipse(pixels, width, height, new Vector2(x, y), size, size, 0f, line, false, 2);
                else if (type == 1)
                    Flower(pixels, width, height, x, y, size, line);
                else
                    Ellipse(pixels, width, height, new Vector2(x, y), size * 0.45f, size, rng.Next(-45, 46) * Mathf.Deg2Rad, line, false, 2);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D MicrobeTexture(int width, int height, int seed, Color body, Color outline, int variant)
        {
            var texture = NewTexture(width, height, TextureWrapMode.Clamp);
            var pixels = ClearPixels(width, height);
            var rng = new System.Random(seed);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            float rx = Rand(rng, width * 0.24f, width * 0.34f);
            float ry = Rand(rng, height * 0.25f, height * 0.38f);
            float tilt = Rand(rng, -0.5f, 0.5f);

            int limbs = 8 + rng.Next(8);
            for (int i = 0; i < limbs; i++)
            {
                float a = Mathf.PI * 2f * i / limbs + Rand(rng, -0.18f, 0.18f);
                var start = center + new Vector2(Mathf.Cos(a) * rx * 0.8f, Mathf.Sin(a) * ry * 0.8f);
                var end = center + new Vector2(Mathf.Cos(a) * rx * Rand(rng, 1.25f, 1.75f), Mathf.Sin(a) * ry * Rand(rng, 1.2f, 1.75f));
                var mid = Vector2.Lerp(start, end, 0.55f) + new Vector2(-Mathf.Sin(a), Mathf.Cos(a)) * Rand(rng, -16f, 16f);
                Bezier(pixels, width, height, start, mid, end, 4, new Color(0.7f, 0.26f, 0.18f, 0.9f));
                Bezier(pixels, width, height, start + Vector2.one, mid, end, 2, new Color(1f, 0.72f, 0.6f, 0.88f));
            }

            Ellipse(pixels, width, height, center, rx + 5f, ry + 5f, tilt, outline, true, 3);
            Ellipse(pixels, width, height, center, rx, ry, tilt, body, true, 3);
            Ellipse(pixels, width, height, center + new Vector2(-rx * 0.18f, ry * 0.16f), rx * 0.58f, ry * 0.32f, tilt, new Color(1f, 1f, 1f, 0.16f), true, 3);

            for (int i = 0; i < 18; i++)
            {
                var p = center + new Vector2(Rand(rng, -rx * 0.65f, rx * 0.65f), Rand(rng, -ry * 0.65f, ry * 0.65f));
                float s = Rand(rng, 2.5f, 7f);
                Ellipse(pixels, width, height, p, s, s * Rand(rng, 0.7f, 1.4f), tilt, new Color(1f, 0.93f, 0.55f, 0.42f), variant % 2 == 0, 2);
            }

            int eyes = Mathf.Clamp(variant + 1, 2, 7);
            for (int i = 0; i < eyes; i++)
            {
                float a = Mathf.PI * 2f * (i + 0.2f) / eyes;
                var eye = center + new Vector2(Mathf.Cos(a) * rx * 0.58f, Mathf.Sin(a) * ry * 0.55f);
                Ellipse(pixels, width, height, eye, 12f, 12f, 0f, outline, true, 2);
                Ellipse(pixels, width, height, eye, 8.5f, 8.5f, 0f, new Color(0.92f, 0.98f, 1f), true, 2);
                Ellipse(pixels, width, height, eye + new Vector2(1.5f, -0.5f), 4.2f, 4.2f, 0f, new Color(0.04f, 0.04f, 0.12f), true, 2);
                Ellipse(pixels, width, height, eye + new Vector2(-1.5f, 1.5f), 1.7f, 1.7f, 0f, Color.white, true, 1);
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D GlowTexture(int size)
        {
            var texture = NewTexture(size, size, TextureWrapMode.Clamp);
            var pixels = new Color[size * size];
            var center = Vector2.one * ((size - 1f) * 0.5f);
            float radius = (size - 1f) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), center) / radius);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D RoundedTexture(int width, int height, int radius, Color fill, Color stroke, int strokeWidth)
        {
            var texture = NewTexture(width, height, TextureWrapMode.Clamp);
            var pixels = new Color[width * height];
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float iw = hw - radius;
            float ih = hh - radius;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - hw);
                    float py = Mathf.Abs(y + 0.5f - hh);
                    float dx = Mathf.Max(px - iw, 0f);
                    float dy = Mathf.Max(py - ih, 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                        pixels[y * width + x] = Color.clear;
                    else
                        pixels[y * width + x] = dist > radius - strokeWidth || x < strokeWidth || y < strokeWidth || x >= width - strokeWidth || y >= height - strokeWidth ? stroke : fill;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D RingTexture(int width, int height, Color color)
        {
            var texture = NewTexture(width, height, TextureWrapMode.Clamp);
            var pixels = ClearPixels(width, height);
            var center = new Vector2(width * 0.5f, height * 0.5f);
            float rx = width * 0.39f;
            float ry = height * 0.36f;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - center.x) / rx;
                    float dy = (y - center.y) / ry;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.SmoothStep(0.08f, 0f, Mathf.Abs(dist - 1f));
                    float inner = Mathf.SmoothStep(0.7f, 1f, dist);
                    float shimmer = 0.68f + 0.32f * Mathf.PerlinNoise(x * 0.07f, y * 0.09f);
                    var pixel = color;
                    pixel.a *= ring * inner * shimmer;
                    pixels[y * width + x] = pixel;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D NewTexture(int width, int height, TextureWrapMode wrapMode)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = wrapMode;
            texture.filterMode = FilterMode.Bilinear;
            return texture;
        }

        private static Color[] ClearPixels(int width, int height)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            return pixels;
        }

        private static void Flower(Color[] pixels, int width, int height, int x, int y, int size, Color color)
        {
            Ellipse(pixels, width, height, new Vector2(x, y), size, size, 0f, color, false, 2);
            Ellipse(pixels, width, height, new Vector2(x + size / 2f, y), size / 2f, size / 2f, 0f, color, false, 2);
            Ellipse(pixels, width, height, new Vector2(x - size / 2f, y), size / 2f, size / 2f, 0f, color, false, 2);
            Ellipse(pixels, width, height, new Vector2(x, y + size / 2f), size / 2f, size / 2f, 0f, color, false, 2);
            Ellipse(pixels, width, height, new Vector2(x, y - size / 2f), size / 2f, size / 2f, 0f, color, false, 2);
        }

        private static void Ellipse(Color[] pixels, int width, int height, Vector2 center, float rx, float ry, float angle, Color color, bool filled, int thickness)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - rx - thickness - 2));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(center.x + rx + thickness + 2));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - ry - thickness - 2));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(center.y + ry + thickness + 2));
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - center.x;
                    float dy = y - center.y;
                    float px = dx * cos + dy * sin;
                    float py = -dx * sin + dy * cos;
                    float d = px * px / (rx * rx) + py * py / (ry * ry);
                    if ((filled && d <= 1f) || (!filled && Mathf.Abs(d - 1f) <= thickness * 0.055f))
                        Blend(pixels, width, height, x, y, color);
                }
        }

        private static void Bezier(Color[] pixels, int width, int height, Vector2 a, Vector2 b, Vector2 c, int thickness, Color color)
        {
            Vector2 previous = a;
            for (int i = 1; i <= 28; i++)
            {
                float t = i / 28f;
                Vector2 p = (1f - t) * (1f - t) * a + 2f * (1f - t) * t * b + t * t * c;
                Line(pixels, width, height, previous, p, thickness, color);
                previous = p;
            }
        }

        private static void Line(Color[] pixels, int width, int height, Vector2 a, Vector2 b, int thickness, Color color)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                Disc(pixels, width, height, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), thickness, color);
            }
        }

        private static void Disc(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
                for (int x = cx - radius; x <= cx + radius; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                        Blend(pixels, width, height, x, y, color);
        }

        private static void Blend(Color[] pixels, int width, int height, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            int index = y * width + x;
            Color dst = pixels[index];
            float inv = 1f - color.a;
            pixels[index] = new Color(color.r * color.a + dst.r * inv, color.g * color.a + dst.g * inv, color.b * color.a + dst.b * inv, color.a + dst.a * inv);
        }

        private static Color Clamp(Color color)
        {
            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }

        private static float Rand(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private sealed class MenuFloat : MonoBehaviour
        {
            private RectTransform _rect;
            private Vector2 _origin;
            private Quaternion _baseRotation;
            private float _xPixels;
            private float _yPixels;
            private float _turnDegrees;
            private float _speed;
            private float _phase;
            private bool _initialized;

            public void Configure(float bobPixels, float turnDegrees, float speed)
            {
                Configure(bobPixels * 0.35f, bobPixels, turnDegrees, speed);
            }

            public void Configure(float xPixels, float yPixels, float turnDegrees, float speed)
            {
                _xPixels = xPixels;
                _yPixels = yPixels;
                _turnDegrees = turnDegrees;
                _speed = speed;
                _phase = Random.value * 20f;
                CaptureOrigin();
            }

            private void Awake()
            {
                CaptureOrigin();
            }

            private void CaptureOrigin()
            {
                if (_initialized)
                    return;

                _rect = GetComponent<RectTransform>();
                if (_rect != null)
                {
                    _origin = _rect.anchoredPosition;
                    _baseRotation = _rect.localRotation;
                    _initialized = true;
                }
            }

            private void Update()
            {
                CaptureOrigin();
                if (_rect == null)
                    return;

                float wave = Mathf.Sin(Time.unscaledTime * _speed + _phase);
                float drift = Mathf.Cos(Time.unscaledTime * _speed * 0.63f + _phase * 1.37f);
                _rect.anchoredPosition = _origin + new Vector2(drift * _xPixels, wave * _yPixels);
                _rect.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, wave * _turnDegrees);
            }
        }

        private sealed class MenuCreatureDrift : MonoBehaviour
        {
            private RectTransform _rect;
            private Vector2 _origin;
            private Vector2 _offset;
            private Vector2 _velocity;
            private float _xLimit;
            private float _yLimit;
            private float _speed;
            private float _turnRate;
            private float _heading;
            private float _turnTimer;
            private float _phase;
            private bool _initialized;

            public void Configure(float xLimit, float yLimit, float speed, float turnRateDegrees, float heading)
            {
                _xLimit = xLimit;
                _yLimit = yLimit;
                _speed = speed;
                _turnRate = turnRateDegrees * Mathf.Deg2Rad;
                _heading = heading;
                _phase = heading * 3.17f;
                _velocity = HeadingToVector(_heading) * _speed;
                PickNextTurn();
                CaptureOrigin();
            }

            private void Awake()
            {
                CaptureOrigin();
            }

            private void CaptureOrigin()
            {
                if (_initialized)
                    return;

                _rect = GetComponent<RectTransform>();
                if (_rect == null)
                    return;

                _origin = _rect.anchoredPosition;
                _initialized = true;
            }

            private void Update()
            {
                CaptureOrigin();
                if (_rect == null)
                    return;

                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
                _turnTimer -= dt;
                if (_turnTimer <= 0f)
                    PickNextTurn();

                Vector2 desired = HeadingToVector(_heading) * _speed;
                _velocity = Vector2.MoveTowards(_velocity, desired, _turnRate * _speed * dt);
                _offset += _velocity * dt;

                if (Mathf.Abs(_offset.x) > _xLimit)
                {
                    _offset.x = Mathf.Sign(_offset.x) * _xLimit;
                    _velocity.x *= -0.75f;
                    _heading = Mathf.Atan2(_velocity.y, _velocity.x);
                    PickNextTurn();
                }

                if (Mathf.Abs(_offset.y) > _yLimit)
                {
                    _offset.y = Mathf.Sign(_offset.y) * _yLimit;
                    _velocity.y *= -0.75f;
                    _heading = Mathf.Atan2(_velocity.y, _velocity.x);
                    PickNextTurn();
                }

                float swim = Mathf.Sin(Time.unscaledTime * 3.4f + _phase);
                _rect.anchoredPosition = _origin + _offset + new Vector2(-_velocity.y, _velocity.x).normalized * swim * 1.6f;

                if (_velocity.sqrMagnitude > 0.01f)
                {
                    float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                    _rect.localRotation = Quaternion.Euler(0f, 0f, angle + swim * 4f);
                }
            }

            private void PickNextTurn()
            {
                _heading += Random.Range(-0.8f, 0.8f);
                _turnTimer = Random.Range(0.8f, 1.9f);
            }

            private static Vector2 HeadingToVector(float heading)
            {
                return new Vector2(Mathf.Cos(heading), Mathf.Sin(heading));
            }
        }

        private sealed class MenuPulse : MonoBehaviour
        {
            private Vector3 _baseScale;
            private float _amount;
            private float _speed;
            private float _phase;

            public void Configure(float amount, float speed)
            {
                _amount = amount;
                _speed = speed;
                _phase = Random.value * 20f;
            }

            private void Awake()
            {
                _baseScale = transform.localScale;
            }

            private void Update()
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * _speed + _phase) * _amount;
                transform.localScale = _baseScale * pulse;
            }
        }

        private sealed class MenuBubbleDrift : MonoBehaviour
        {
            private RectTransform _rect;
            private Vector2 _origin;
            private float _risePixels;
            private float _swayPixels;
            private float _speed;
            private float _phase;

            public void Configure(float risePixels, float swayPixels, float speed, float phase)
            {
                _risePixels = risePixels;
                _swayPixels = swayPixels;
                _speed = speed;
                _phase = phase;
            }

            private void Awake()
            {
                _rect = GetComponent<RectTransform>();
                if (_rect != null)
                    _origin = _rect.anchoredPosition;
            }

            private void Update()
            {
                if (_rect == null)
                    return;

                float t = Mathf.Repeat(Time.unscaledTime * _speed + _phase, 1f);
                float ease = Mathf.SmoothStep(0f, 1f, t);
                float sway = Mathf.Sin(Time.unscaledTime * (_speed * 7f + 0.6f) + _phase) * _swayPixels;
                _rect.anchoredPosition = _origin + new Vector2(sway, Mathf.Lerp(-_risePixels * 0.5f, _risePixels, ease));
                _rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.15f, ease);
            }
        }

        private sealed class MenuButtonWiggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
        {
            private Image _image;
            private RectTransform _rect;
            private Vector3 _baseScale;
            private Color _normal;
            private Color _hover;
            private bool _primary;
            private bool _active;

            public void Configure(Image image, bool primary)
            {
                _image = image;
                _primary = primary;
                _normal = primary
                    ? UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.96f)
                    : UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTeal, 0.92f);
                _hover = primary
                    ? UIStyleGuide.Colors.FossilGoldBright
                    : UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.96f);
            }

            private void Awake()
            {
                _rect = GetComponent<RectTransform>();
                _baseScale = transform.localScale;
            }

            private void Update()
            {
                if (_image == null || _rect == null)
                    return;
                float target = _active ? 1.045f : 1f;
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * target, Time.unscaledDeltaTime * 12f);
                _image.color = Color.Lerp(_image.color, _active ? _hover : _normal, Time.unscaledDeltaTime * 10f);
                _rect.localRotation = _active ? Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 9f) * (_primary ? 0.9f : 0.55f)) : Quaternion.Lerp(_rect.localRotation, Quaternion.identity, Time.unscaledDeltaTime * 10f);
            }

            public void OnPointerEnter(PointerEventData eventData) => _active = true;
            public void OnPointerExit(PointerEventData eventData) => _active = false;
            public void OnSelect(BaseEventData eventData) => _active = true;
            public void OnDeselect(BaseEventData eventData) => _active = false;
        }
    }
}
