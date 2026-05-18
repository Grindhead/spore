using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    public enum UIButtonVariant
    {
        Primary,
        Secondary,
        Danger,
        Tab
    }

    /// <summary>
    /// Shared UI design tokens and helpers used by runtime menus and overlays.
    /// Centralizing values keeps visuals consistent across screens.
    /// </summary>
    public static class UIStyleGuide
    {
        public static class Layout
        {
            public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
            public const float DefaultCanvasMatch = 0.5f;
            public const float TightSpacing = 4f;
            public const float StandardSpacing = 8f;
            public const float LargeSpacing = 20f;
            public const float MinimumTouchTarget = 44f;
            public const float ComfortableTouchTarget = 52f;
        }

        public static class Accessibility
        {
            public const float ReducedMotionMultiplier = 0f;
            public const float DefaultMotionMultiplier = 1f;
            public const float MinimumReadableTextScale = 0.85f;
            public const float MaximumReadableTextScale = 1.45f;

            public static float ClampTextScale(float scale)
            {
                return Mathf.Clamp(scale <= 0f ? 1f : scale, MinimumReadableTextScale, MaximumReadableTextScale);
            }

            public static float ApplyMotionPreference(float durationSeconds, bool reducedMotion)
            {
                return Mathf.Max(0f, durationSeconds * (reducedMotion ? ReducedMotionMultiplier : DefaultMotionMultiplier));
            }
        }

        public static class Typography
        {
            public const string DefaultFontName = "Roboto";
            public const string DefaultFontResourcePath = "Fonts/Roboto-Regular";

            public const int TitleXXL = 52;
            public const int TitleXL = 48;
            public const int TitleLarge = 34;
            public const int TitleMedium = 32;
            public const int BodyLarge = 26;
            public const int BodyMedium = 20;
            public const int BodySmall = 18;
            public const int Caption = 15;
        }

        private static Font _defaultFont;
        private static Texture2D _glowTexture;
        private static Sprite _paintedPanelSprite;
        private static Sprite _paintedButtonSprite;
        private static Sprite _paintedFieldSprite;

        public static class Colors
        {
            public static readonly Color DeepNavy = new Color(0f, 0.055f, 0.075f, 1f);
            public static readonly Color InkBlue = new Color(0f, 0.14f, 0.18f, 1f);
            public static readonly Color TideTeal = new Color(0f, 0.62f, 0.62f, 1f);
            public static readonly Color TideTealBright = new Color(0.32f, 0.93f, 0.89f, 1f);
            public static readonly Color FossilAmber = new Color(0.94f, 0.61f, 0.12f, 1f);
            public static readonly Color FossilGold = new Color(0.98f, 0.79f, 0.25f, 1f);
            public static readonly Color FossilGoldBright = new Color(1f, 0.9f, 0.49f, 1f);
            public static readonly Color Coral = new Color(0.95f, 0.34f, 0.31f, 1f);
            public static readonly Color CoralBright = new Color(1f, 0.5f, 0.42f, 1f);
            public static readonly Color Amethyst = new Color(0.56f, 0.38f, 0.83f, 1f);
            public static readonly Color AlgaeLime = new Color(0.72f, 0.84f, 0.13f, 1f);
            public static readonly Color ShellBrown = new Color(0.54f, 0.36f, 0.12f, 1f);

            public static readonly Color OverlayDim = new Color(0f, 0.012f, 0.018f, 0.7f);
            public static readonly Color MenuBackdropTint = new Color(0f, 0.08f, 0.105f, 0.94f);
            public static readonly Color ScreenBackground = new Color(0f, 0.09f, 0.12f, 0.98f);
            public static readonly Color PanelBackground = new Color(0f, 0.15f, 0.18f, 0.94f);
            public static readonly Color PanelBackgroundStrong = new Color(0f, 0.105f, 0.135f, 0.96f);
            public static readonly Color PanelBorder = new Color(0.32f, 0.93f, 0.89f, 0.34f);
            public static readonly Color Accent = TideTeal;
            public static readonly Color AccentStrong = TideTealBright;
            public static readonly Color AccentWarm = FossilAmber;
            public static readonly Color TabActive = new Color(0f, 0.38f, 0.42f, 1f);
            public static readonly Color TabInactive = new Color(0f, 0.18f, 0.22f, 1f);
            public static readonly Color Subtitle = new Color(0.82f, 0.94f, 0.92f, 1f);
            public static readonly Color BodyText = new Color(1f, 0.97f, 0.86f, 1f);
            public static readonly Color BodyTextMuted = new Color(0.82f, 0.88f, 0.82f, 1f);
            public static readonly Color BodyTextSubtle = new Color(0.58f, 0.69f, 0.67f, 1f);
            public static readonly Color Success = AlgaeLime;
            public static readonly Color Warning = FossilGoldBright;
            public static readonly Color Error = CoralBright;
            public static readonly Color RowStripe = new Color(0f, 0.19f, 0.23f, 0.62f);
            public static readonly Color SurfaceMuted = new Color(0f, 0.2f, 0.24f, 1f);
            public static readonly Color HudPanel = new Color(0f, 0.07f, 0.09f, 0.78f);
            public static readonly Color HudPanelStrong = new Color(0f, 0.095f, 0.12f, 0.9f);
            public static readonly Color HudPanelBorder = new Color(0.32f, 0.93f, 0.89f, 0.32f);
            public static readonly Color HudMeterBackground = new Color(0f, 0.035f, 0.045f, 0.84f);
            public static readonly Color HudAccentSoft = new Color(0.32f, 0.93f, 0.89f, 0.42f);
            public static readonly Color TransparentHitArea = new Color(0f, 0f, 0f, 0.01f);
            public static readonly Color UnlockedGold = new Color(0.98f, 0.79f, 0.25f, 0.92f);
            public static readonly Color LockedGray = new Color(0.26f, 0.34f, 0.34f, 0.62f);
            public static readonly Color TextOnWarm = new Color(0.09f, 0.045f, 0.02f, 1f);
            public static readonly Color TextOnCool = new Color(0.94f, 1f, 0.98f, 1f);
            public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.45f);

            public static Color WithAlpha(Color color, float alpha)
            {
                color.a = alpha;
                return color;
            }
        }

        public static Font GetDefaultFont()
        {
            if (_defaultFont != null)
                return _defaultFont;

            _defaultFont = Resources.Load<Font>(Typography.DefaultFontResourcePath);
            if (_defaultFont == null)
                _defaultFont = Font.CreateDynamicFontFromOSFont(Typography.DefaultFontName, Typography.BodyMedium);

            return _defaultFont;
        }

        public static void ApplyCanvasScalerDefaults(CanvasScaler scaler, float matchWidthOrHeight = Layout.DefaultCanvasMatch)
        {
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Layout.ReferenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        public static void EnsureMinimumHitTarget(RectTransform rectTransform, float minimumSize = Layout.MinimumTouchTarget)
        {
            if (rectTransform == null)
                return;

            Vector2 size = rectTransform.sizeDelta;
            size.x = Mathf.Max(size.x, minimumSize);
            size.y = Mathf.Max(size.y, minimumSize);
            rectTransform.sizeDelta = size;
        }

        public static Color GetButtonSurfaceColor(UIButtonVariant variant)
        {
            switch (variant)
            {
                case UIButtonVariant.Primary:
                    return Colors.FossilAmber;
                case UIButtonVariant.Danger:
                    return Colors.Coral;
                case UIButtonVariant.Tab:
                    return Colors.TabInactive;
                default:
                    return Colors.TideTeal;
            }
        }

        public static Color GetButtonTextColor(UIButtonVariant variant)
        {
            return variant == UIButtonVariant.Primary ? Colors.TextOnWarm : Colors.TextOnCool;
        }

        public static Color GetButtonAccentColor(UIButtonVariant variant)
        {
            switch (variant)
            {
                case UIButtonVariant.Primary:
                    return Colors.FossilGoldBright;
                case UIButtonVariant.Danger:
                    return Colors.CoralBright;
                case UIButtonVariant.Tab:
                    return Colors.TideTealBright;
                default:
                    return Colors.TideTealBright;
            }
        }

        public static Image ApplyPanelChrome(
            GameObject panelObject,
            bool strong = true,
            float alpha = 1f,
            bool outlined = true)
        {
            if (panelObject == null)
                return null;

            var image = panelObject.GetComponent<Image>();
            if (image == null)
                image = panelObject.AddComponent<Image>();

            image.color = Colors.WithAlpha(strong ? Colors.PanelBackgroundStrong : Colors.PanelBackground, alpha);

            if (outlined)
            {
                ApplyOutline(
                    panelObject,
                    Colors.WithAlpha(Colors.PanelBorder, strong ? 0.58f : 0.42f),
                    new Vector2(1.5f, -1.5f));
            }

            return image;
        }

        public static Image ApplyPaintedPanelChrome(
            GameObject panelObject,
            bool strong = true,
            float alpha = 1f,
            bool atmosphere = true)
        {
            if (panelObject == null)
                return null;

            var image = panelObject.GetComponent<Image>();
            if (image == null)
                image = panelObject.AddComponent<Image>();

            image.sprite = GetPaintedPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = Colors.WithAlpha(strong ? Colors.PanelBackgroundStrong : Colors.PanelBackground, alpha);
            image.raycastTarget = true;

            ApplyOutline(
                panelObject,
                Colors.WithAlpha(Colors.FossilGoldBright, strong ? 0.48f : 0.34f),
                new Vector2(1.6f, -1.6f));

            if (atmosphere)
            {
                CreateAtmosphereBand(
                    panelObject.transform,
                    "PaintedTopTide",
                    new Vector2(0f, 0.91f),
                    Vector2.one,
                    Colors.WithAlpha(Colors.TideTealBright, strong ? 0.13f : 0.08f));
                CreateAtmosphereBand(
                    panelObject.transform,
                    "PaintedWarmShelf",
                    Vector2.zero,
                    new Vector2(1f, 0.16f),
                    Colors.WithAlpha(Colors.FossilAmber, strong ? 0.1f : 0.06f));
                CreateAtmosphereBand(
                    panelObject.transform,
                    "PaintedInnerWash",
                    new Vector2(0.04f, 0.08f),
                    new Vector2(0.96f, 0.88f),
                    Colors.WithAlpha(Colors.TideTeal, 0.035f));
            }

            return image;
        }

        public static void ApplyPaintedButtonChrome(Button button, UIButtonVariant variant, bool animated = true)
        {
            if (button == null)
                return;

            var image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();

            image.sprite = GetPaintedButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = GetButtonSurfaceColor(variant);
            button.targetGraphic = image;
            ApplyButtonStyle(button, variant);

            ApplyOutline(
                button.gameObject,
                Colors.WithAlpha(GetButtonAccentColor(variant), variant == UIButtonVariant.Primary ? 0.46f : 0.34f),
                new Vector2(1.2f, -1.2f));

            if (animated)
            {
                var motion = button.GetComponent<UIPaintedButtonMotion>();
                if (motion == null)
                    motion = button.gameObject.AddComponent<UIPaintedButtonMotion>();
                motion.Configure(variant);
            }
        }

        public static void AddButtonPearl(GameObject buttonObject, UIButtonVariant variant, float size = 0f, float xAnchor = 0.055f)
        {
            if (buttonObject == null || buttonObject.transform.Find("ButtonPearl") != null)
                return;

            var pearl = new GameObject("ButtonPearl");
            pearl.transform.SetParent(buttonObject.transform, false);

            var rect = pearl.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(xAnchor, 0.5f);
            rect.sizeDelta = Vector2.one * (size > 0f ? size : (variant == UIButtonVariant.Primary ? 32f : 23f));

            var glow = pearl.AddComponent<RawImage>();
            glow.texture = GetGlowTexture();
            glow.color = Colors.WithAlpha(GetButtonAccentColor(variant), variant == UIButtonVariant.Primary ? 0.86f : 0.62f);
            glow.raycastTarget = false;

            var pulse = pearl.AddComponent<UIPulse>();
            pulse.Configure(variant == UIButtonVariant.Primary ? 0.15f : 0.09f, variant == UIButtonVariant.Primary ? 2.1f : 1.4f);
        }

        public static void AddScreenAtmosphere(Transform parent, string prefix, float intensity = 1f)
        {
            if (parent == null)
                return;

            CreateAtmosphereBand(parent, prefix + "DeepWash", Vector2.zero, Vector2.one,
                Colors.WithAlpha(Colors.DeepNavy, 0.16f * intensity));
            CreateAtmosphereBand(parent, prefix + "SurfaceTide", new Vector2(0f, 0.68f), Vector2.one,
                Colors.WithAlpha(Colors.TideTealBright, 0.08f * intensity));
            CreateAtmosphereBand(parent, prefix + "FossilShelf", Vector2.zero, new Vector2(1f, 0.26f),
                Colors.WithAlpha(Colors.FossilAmber, 0.065f * intensity));
            CreateAtmosphereBand(parent, prefix + "CoralVeil", new Vector2(0f, 0.24f), new Vector2(1f, 0.58f),
                Colors.WithAlpha(Colors.Coral, 0.035f * intensity));
        }

        public static Sprite GetPaintedPanelSprite()
        {
            if (_paintedPanelSprite == null)
            {
                _paintedPanelSprite = Sprite.Create(
                    RoundedTexture(224, 224, 60, Color.white, Colors.WithAlpha(Colors.FossilGoldBright, 0.82f), 10),
                    new Rect(0f, 0f, 224f, 224f),
                    new Vector2(0.5f, 0.5f),
                    200f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(60f, 60f, 60f, 60f));
            }

            return _paintedPanelSprite;
        }

        public static Sprite GetPaintedButtonSprite()
        {
            if (_paintedButtonSprite == null)
            {
                _paintedButtonSprite = Sprite.Create(
                    RoundedTexture(224, 104, 46, Color.white, Colors.WithAlpha(Colors.FossilGoldBright, 0.78f), 8),
                    new Rect(0f, 0f, 224f, 104f),
                    new Vector2(0.5f, 0.5f),
                    200f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(46f, 46f, 46f, 46f));
            }

            return _paintedButtonSprite;
        }

        public static Sprite GetPaintedFieldSprite()
        {
            if (_paintedFieldSprite == null)
            {
                _paintedFieldSprite = Sprite.Create(
                    RoundedTexture(224, 88, 28, Color.white, Colors.WithAlpha(Colors.TideTealBright, 0.58f), 6),
                    new Rect(0f, 0f, 224f, 88f),
                    new Vector2(0.5f, 0.5f),
                    200f,
                    0,
                    SpriteMeshType.FullRect,
                    new Vector4(28f, 28f, 28f, 28f));
            }

            return _paintedFieldSprite;
        }

        public static Texture2D GetGlowTexture()
        {
            if (_glowTexture == null)
                _glowTexture = GlowTexture(512);

            return _glowTexture;
        }

        public static Outline ApplyOutline(GameObject target, Color color, Vector2 distance)
        {
            if (target == null)
                return null;

            var outline = target.GetComponent<Outline>();
            if (outline == null)
                outline = target.AddComponent<Outline>();

            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
        }

        public static Shadow ApplyTextShadow(GameObject target, float alpha = 0.45f)
        {
            if (target == null)
                return null;

            var shadow = target.GetComponent<Shadow>();
            if (shadow == null)
                shadow = target.AddComponent<Shadow>();

            shadow.effectColor = Colors.WithAlpha(Colors.Shadow, alpha);
            shadow.effectDistance = new Vector2(1.4f, -1.4f);
            return shadow;
        }

        public static void ApplyButtonLabel(Text text, UIButtonVariant variant, bool bold = false)
        {
            if (text == null)
                return;

            text.color = GetButtonTextColor(variant);
            text.fontStyle = bold || variant == UIButtonVariant.Primary ? FontStyle.Bold : FontStyle.Normal;
        }

        public static ColorBlock BuildButtonColors(UIButtonVariant variant)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            colors.disabledColor = new Color(0.24f, 0.28f, 0.35f, 0.7f);

            switch (variant)
            {
                case UIButtonVariant.Primary:
                    colors.normalColor = GetButtonSurfaceColor(variant);
                    colors.highlightedColor = Colors.FossilGoldBright;
                    colors.pressedColor = Colors.ShellBrown;
                    break;
                case UIButtonVariant.Danger:
                    colors.normalColor = GetButtonSurfaceColor(variant);
                    colors.highlightedColor = Colors.CoralBright;
                    colors.pressedColor = new Color(0.26f, 0.045f, 0.045f, 1f);
                    break;
                case UIButtonVariant.Tab:
                    colors.normalColor = GetButtonSurfaceColor(variant);
                    colors.highlightedColor = Colors.TideTeal;
                    colors.pressedColor = new Color(0f, 0.27f, 0.31f, 1f);
                    break;
                default:
                    colors.normalColor = Colors.WithAlpha(GetButtonSurfaceColor(variant), 0.96f);
                    colors.highlightedColor = Colors.TideTeal;
                    colors.pressedColor = new Color(0f, 0.2f, 0.24f, 1f);
                    break;
            }

            colors.selectedColor = colors.highlightedColor;
            return colors;
        }

        public static void ApplyButtonStyle(Button button, UIButtonVariant variant)
        {
            if (button == null)
                return;

            button.colors = BuildButtonColors(variant);
        }

        private static void CreateAtmosphereBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            if (parent == null || parent.Find(name) != null)
                return;

            var band = new GameObject(name);
            band.transform.SetParent(parent, false);
            var rect = band.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = band.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            band.transform.SetAsFirstSibling();
        }

        private static Texture2D GlowTexture(int size)
        {
            var texture = NewTexture(size, size, TextureWrapMode.Clamp);
            var pixels = new Color[size * size];
            var center = Vector2.one * ((size - 1f) * 0.5f);
            float radius = (size - 1f) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float a = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), center) / radius);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D RoundedTexture(int width, int height, int radius, Color fill, Color stroke, int strokeWidth)
        {
            var texture = NewTexture(width, height, TextureWrapMode.Clamp);
            var pixels = new Color[width * height];
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;
            float innerWidth = halfWidth - radius;
            float innerHeight = halfHeight - radius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - halfWidth);
                    float py = Mathf.Abs(y + 0.5f - halfHeight);
                    float dx = Mathf.Max(px - innerWidth, 0f);
                    float dy = Mathf.Max(py - innerHeight, 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        pixels[y * width + x] = Color.clear;
                    }
                    else
                    {
                        bool isStroke = dist > radius - strokeWidth
                            || x < strokeWidth
                            || y < strokeWidth
                            || x >= width - strokeWidth
                            || y >= height - strokeWidth;
                        pixels[y * width + x] = isStroke ? stroke : fill;
                    }
                }
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
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }
    }

    internal sealed class UIPaintedButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 _baseScale = Vector3.one;
        private Quaternion _baseRotation = Quaternion.identity;
        private bool _active;
        private bool _pressed;
        private bool _primary;

        public void Configure(UIButtonVariant variant)
        {
            _primary = variant == UIButtonVariant.Primary;
            CaptureBaseTransform();
        }

        private void Awake()
        {
            CaptureBaseTransform();
        }

        private void CaptureBaseTransform()
        {
            _baseScale = transform.localScale;
            _baseRotation = transform.localRotation;
        }

        private void Update()
        {
            float hoverScale = _primary ? 1.032f : 1.018f;
            float targetScale = _pressed ? 0.988f : (_active ? hoverScale : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * targetScale, Time.unscaledDeltaTime * 13f);

            Quaternion targetRotation = _active && !_pressed
                ? _baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 8.5f) * (_primary ? 0.8f : 0.45f))
                : _baseRotation;
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.unscaledDeltaTime * 12f);
        }

        public void OnPointerEnter(PointerEventData eventData) => _active = true;
        public void OnPointerExit(PointerEventData eventData)
        {
            _active = false;
            _pressed = false;
        }

        public void OnSelect(BaseEventData eventData) => _active = true;
        public void OnDeselect(BaseEventData eventData)
        {
            _active = false;
            _pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => _pressed = true;
        public void OnPointerUp(PointerEventData eventData) => _pressed = false;
    }

    internal sealed class UIPulse : MonoBehaviour
    {
        private Vector3 _baseScale = Vector3.one;
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
            transform.localScale = _baseScale * (1f + Mathf.Sin(Time.unscaledTime * _speed + _phase) * _amount);
        }
    }
}
