using UnityEngine;
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

        public static class Colors
        {
            public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.6f);
            public static readonly Color MenuBackdropTint = new Color(0.02f, 0.05f, 0.1f, 0.92f);
            public static readonly Color ScreenBackground = new Color(0.06f, 0.08f, 0.14f, 0.98f);
            public static readonly Color PanelBackground = new Color(0.06f, 0.1f, 0.18f, 0.93f);
            public static readonly Color PanelBackgroundStrong = new Color(0.08f, 0.08f, 0.15f, 0.95f);
            public static readonly Color PanelBorder = new Color(0.22f, 0.55f, 0.82f, 0.32f);
            public static readonly Color Accent = new Color(0.4f, 0.8f, 1f);
            public static readonly Color AccentStrong = new Color(0.52f, 0.86f, 1f);
            public static readonly Color TabActive = new Color(0.2f, 0.35f, 0.55f);
            public static readonly Color TabInactive = new Color(0.12f, 0.15f, 0.25f);
            public static readonly Color Subtitle = new Color(0.68f, 0.78f, 0.9f);
            public static readonly Color BodyText = new Color(0.95f, 0.96f, 1f);
            public static readonly Color BodyTextMuted = new Color(0.8f, 0.85f, 0.9f);
            public static readonly Color BodyTextSubtle = new Color(0.56f, 0.62f, 0.68f);
            public static readonly Color Success = new Color(0.3f, 1f, 0.45f);
            public static readonly Color Warning = new Color(1f, 0.85f, 0.3f);
            public static readonly Color Error = new Color(1f, 0.6f, 0.55f);
            public static readonly Color RowStripe = new Color(0.1f, 0.12f, 0.2f, 0.6f);
            public static readonly Color SurfaceMuted = new Color(0.15f, 0.15f, 0.25f);
            public static readonly Color HudPanel = new Color(0.025f, 0.045f, 0.075f, 0.72f);
            public static readonly Color HudPanelStrong = new Color(0.025f, 0.055f, 0.09f, 0.86f);
            public static readonly Color HudPanelBorder = new Color(0.42f, 0.82f, 1f, 0.28f);
            public static readonly Color HudMeterBackground = new Color(0.01f, 0.025f, 0.04f, 0.78f);
            public static readonly Color HudAccentSoft = new Color(0.34f, 0.84f, 1f, 0.42f);
            public static readonly Color TransparentHitArea = new Color(0f, 0f, 0f, 0.01f);
            public static readonly Color UnlockedGold = new Color(1f, 0.82f, 0.2f, 0.9f);
            public static readonly Color LockedGray = new Color(0.3f, 0.3f, 0.35f, 0.6f);
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

        public static ColorBlock BuildButtonColors(UIButtonVariant variant)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            colors.disabledColor = new Color(0.24f, 0.28f, 0.35f, 0.7f);

            switch (variant)
            {
                case UIButtonVariant.Primary:
                    colors.normalColor = new Color(0.14f, 0.42f, 0.22f, 1f);
                    colors.highlightedColor = new Color(0.2f, 0.56f, 0.3f, 1f);
                    colors.pressedColor = new Color(0.11f, 0.29f, 0.16f, 1f);
                    break;
                case UIButtonVariant.Danger:
                    colors.normalColor = new Color(0.28f, 0.1f, 0.1f, 1f);
                    colors.highlightedColor = new Color(0.37f, 0.15f, 0.15f, 1f);
                    colors.pressedColor = new Color(0.2f, 0.07f, 0.07f, 1f);
                    break;
                case UIButtonVariant.Tab:
                    colors.normalColor = new Color(0.12f, 0.15f, 0.25f, 1f);
                    colors.highlightedColor = new Color(0.25f, 0.4f, 0.6f, 1f);
                    colors.pressedColor = new Color(0.15f, 0.25f, 0.4f, 1f);
                    break;
                default:
                    colors.normalColor = new Color(0.12f, 0.23f, 0.4f, 0.95f);
                    colors.highlightedColor = new Color(0.2f, 0.37f, 0.62f, 1f);
                    colors.pressedColor = new Color(0.08f, 0.16f, 0.28f, 1f);
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
    }
}
