# RoobsWorld UI Style Guide

Use `Assets/Scripts/UI/UIStyleGuide.cs` for runtime UI styling instead of hardcoded literals.

## Rules

1. Use `UIStyleGuide.GetDefaultFont()` for all built-in text.
2. Use `UIStyleGuide.ApplyCanvasScalerDefaults(...)` for every overlay canvas.
3. Use `UIStyleGuide.ApplyButtonStyle(...)` with `UIButtonVariant` (`Primary`, `Secondary`, `Danger`, `Tab`) for interactive buttons.
4. Pull colors from `UIStyleGuide.Colors` (overlay, panel, accent, text, status, rows).
5. Pull font sizes from `UIStyleGuide.Typography` for heading/body/caption consistency.
6. Prefer `UIStyleGuide.Layout` spacing tokens before adding one-off spacing values.

## Canonical Tokens

- Layout: `ReferenceResolution`, `DefaultCanvasMatch`, `TightSpacing`, `StandardSpacing`, `LargeSpacing`
- Typography: `TitleXXL`, `TitleXL`, `TitleLarge`, `TitleMedium`, `BodyLarge`, `BodyMedium`, `BodySmall`, `Caption`
- Colors: `OverlayDim`, `ScreenBackground`, `PanelBackground`, `PanelBorder`, `Accent`, `AccentStrong`, `Subtitle`, `BodyText`, `BodyTextMuted`, `BodyTextSubtle`, `Success`, `Warning`, `Error`, `RowStripe`, `SurfaceMuted`, `TransparentHitArea`

## Example

```csharp
var scaler = canvasObj.AddComponent<CanvasScaler>();
UIStyleGuide.ApplyCanvasScalerDefaults(scaler);

var buttonImage = buttonObj.AddComponent<Image>();
buttonImage.color = Color.white;
var button = buttonObj.AddComponent<Button>();
button.targetGraphic = buttonImage;
UIStyleGuide.ApplyButtonStyle(button, UIButtonVariant.Primary);
```
