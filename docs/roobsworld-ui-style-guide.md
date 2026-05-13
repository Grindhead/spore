# RoobsWorld UI Style Guide

Use `Assets/Scripts/UI/UIStyleGuide.cs` for runtime UI styling instead of hardcoded literals. The canonical palette is sampled from `Assets/Resources/UI/roobs-world-logo.png`: fossil gold lettering, luminous aqua drift text, deep ocean outlines, coral organisms, violet accents, algae-lime highlights, and shell-brown shadows.

## Rules

1. Use `UIStyleGuide.GetDefaultFont()` for all built-in text.
2. Use `UIStyleGuide.ApplyCanvasScalerDefaults(...)` for every overlay canvas.
3. Use `UIStyleGuide.ApplyButtonStyle(...)` with `UIButtonVariant` (`Primary`, `Secondary`, `Danger`, `Tab`) for interactive buttons.
4. Use `UIStyleGuide.ApplyPanelChrome(...)`, `ApplyOutline(...)`, `ApplyTextShadow(...)`, and `ApplyButtonLabel(...)` before adding one-off panel, text, or button styling.
5. Pull font sizes from `UIStyleGuide.Typography` for heading/body/caption consistency.
6. Prefer `UIStyleGuide.Layout` spacing tokens before adding one-off spacing values.
7. Pull colors from `UIStyleGuide.Colors` (logo palette, overlay, panel, accent, text, status, rows).

## Canonical Tokens

- Layout: `ReferenceResolution`, `DefaultCanvasMatch`, `TightSpacing`, `StandardSpacing`, `LargeSpacing`
- Typography: `TitleXXL`, `TitleXL`, `TitleLarge`, `TitleMedium`, `BodyLarge`, `BodyMedium`, `BodySmall`, `Caption`
- Logo palette: `DeepNavy`, `InkBlue`, `TideTeal`, `TideTealBright`, `FossilAmber`, `FossilGold`, `FossilGoldBright`, `Coral`, `CoralBright`, `Amethyst`, `AlgaeLime`, `ShellBrown`
- Semantic colors: `OverlayDim`, `ScreenBackground`, `PanelBackground`, `PanelBackgroundStrong`, `PanelBorder`, `Accent`, `AccentStrong`, `AccentWarm`, `Subtitle`, `BodyText`, `BodyTextMuted`, `BodyTextSubtle`, `Success`, `Warning`, `Error`, `RowStripe`, `SurfaceMuted`, `HudPanel`, `HudPanelStrong`, `HudPanelBorder`, `HudMeterBackground`, `HudAccentSoft`, `TransparentHitArea`, `UnlockedGold`, `LockedGray`, `TextOnWarm`, `TextOnCool`, `Shadow`
- Helpers: `GetButtonSurfaceColor`, `GetButtonTextColor`, `GetButtonAccentColor`, `ApplyPanelChrome`, `ApplyOutline`, `ApplyTextShadow`, `ApplyButtonLabel`

## Color Use

- Primary action: fossil gold button (`UIButtonVariant.Primary`) with dark warm text when the button supplies its own label colour.
- Secondary action and tabs: tide teal on deep ocean panels.
- Danger/destructive action: coral.
- Positive/success states: algae-lime.
- Warnings/rewards/points: fossil gold.
- Panels and HUD chrome: deep navy/ink blue with aqua borders.
- Body copy: warm off-white, muted copy in grey-aqua.

## Example

```csharp
var scaler = canvasObj.AddComponent<CanvasScaler>();
UIStyleGuide.ApplyCanvasScalerDefaults(scaler);

var buttonImage = buttonObj.AddComponent<Image>();
buttonImage.color = UIStyleGuide.Colors.TextOnCool;
var button = buttonObj.AddComponent<Button>();
button.targetGraphic = buttonImage;
UIStyleGuide.ApplyButtonStyle(button, UIButtonVariant.Primary);
```
