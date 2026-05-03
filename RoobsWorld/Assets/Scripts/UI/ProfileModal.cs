using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoobsWorld.Core;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Draggable profile modal overlay.
    ///
    /// Shows two sections:
    ///   1. Run Records — personal bests and aggregate stats from <see cref="RunRecord"/>.
    ///   2. Achievements — scrollable list with locked/unlocked state, same visual language as
    ///      <see cref="AchievementsUI"/> but inline inside the modal.
    ///
    /// The modal panel can be dragged freely (both axes) by clicking and dragging the header bar.
    /// Dragging is clamped so the panel cannot leave the screen.
    ///
    /// Opened from the pause menu via the Profile button.
    /// </summary>
    public sealed class ProfileModal : MonoBehaviour
    {
        // ---- layout constants ----
        private const float PanelW      = 820f;
        private const float PanelH      = 680f;
        private const float HeaderH     = 52f;
        private const float StatsH      = 220f;
        private const float DividerH    = 2f;
        private const float AchievRowH  = 58f;
        private const int   TitleFontSz = 28;
        private const int   LabelFontSz = 16;
        private const int   ValueFontSz = 16;

        // ---- runtime refs ----
        private Canvas          _canvas;
        private RectTransform   _panel;
        private Font            _font;
        private ScrollRect      _scroll;

        public Action OnClosed;

        // ---- public API ----

        public void Show()
        {
            if (_panel == null) Build();
            _panel.anchoredPosition = Vector2.zero; // reset to centre each open
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        // ---- construction ----

        private void Build()
        {
            _font = UIStyleGuide.GetDefaultFont();

            var canvasGo = new GameObject("ProfileModalCanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 300;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Dim backdrop — click to close
            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvasGo.transform, false);
            var bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.offsetMin = Vector2.zero;
            bdRect.offsetMax = Vector2.zero;
            var bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.55f);
            var bdBtn = backdrop.AddComponent<Button>();
            bdBtn.onClick.AddListener(Hide);

            // Modal panel (does not bubble clicks to backdrop)
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            _panel = panelGo.AddComponent<RectTransform>();
            _panel.anchorMin  = new Vector2(0.5f, 0.5f);
            _panel.anchorMax  = new Vector2(0.5f, 0.5f);
            _panel.pivot      = new Vector2(0.5f, 0.5f);
            _panel.sizeDelta  = new Vector2(PanelW, PanelH);
            _panel.anchoredPosition = Vector2.zero;
            panelGo.AddComponent<Image>().color = new Color(UIStyleGuide.Colors.ScreenBackground.r, UIStyleGuide.Colors.ScreenBackground.g, UIStyleGuide.Colors.ScreenBackground.b, 0.99f);
            // Block clicks from reaching backdrop
            panelGo.AddComponent<Button>().onClick.AddListener(() => { });

            BuildHeader(panelGo.transform);
            BuildStats(panelGo.transform);
            BuildDivider(panelGo.transform);
            BuildAchievements(panelGo.transform);
        }

        // ---- header (drag handle + title + close) ----

        private void BuildHeader(Transform parent)
        {
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent, false);
            var rect = headerGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, HeaderH);
            headerGo.AddComponent<Image>().color = new Color(0.09f, 0.14f, 0.26f, 1f);

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(16, 0);
            titleRect.offsetMax = new Vector2(-52, 0);
            var titleText = titleGo.AddComponent<Text>();
            titleText.font      = _font;
            titleText.text      = Localization.Get("profile.title");
            titleText.fontSize  = TitleFontSz;
            titleText.color     = UIStyleGuide.Colors.AccentStrong;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;

            // Close button
            var closeBtnGo = new GameObject("CloseBtn");
            closeBtnGo.transform.SetParent(headerGo.transform, false);
            var cbRect = closeBtnGo.AddComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(1f, 0.5f);
            cbRect.anchorMax = new Vector2(1f, 0.5f);
            cbRect.pivot     = new Vector2(1f, 0.5f);
            cbRect.anchoredPosition = new Vector2(-10, 0);
            cbRect.sizeDelta = new Vector2(36, 36);
            var closeBtnBg = closeBtnGo.AddComponent<Image>();
            closeBtnBg.color = Color.white;
            var closeBtn = closeBtnGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBtnBg;
            UIStyleGuide.ApplyButtonStyle(closeBtn, UIButtonVariant.Danger);
            closeBtn.onClick.AddListener(() => { OnClosed?.Invoke(); Hide(); });
            var closeLabelGo = new GameObject("X");
            closeLabelGo.transform.SetParent(closeBtnGo.transform, false);
            var clRect = closeLabelGo.AddComponent<RectTransform>();
            clRect.anchorMin = Vector2.zero;
            clRect.anchorMax = Vector2.one;
            var clText = closeLabelGo.AddComponent<Text>();
            clText.font = _font; clText.text = "✕";
            clText.fontSize = 20; clText.alignment = TextAnchor.MiddleCenter;
            clText.color = UIStyleGuide.Colors.BodyText;

            // Drag handler — dragging the header moves the whole panel
            var drag = headerGo.AddComponent<ModalDragHandler>();
            drag.Target = _panel;
            drag.ParentCanvas = _canvas;
        }

        // ---- run records stats ----

        private void BuildStats(Transform parent)
        {
            var statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(parent, false);
            var rect = statsGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -HeaderH);
            rect.sizeDelta = new Vector2(0, StatsH);
            statsGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            // Section label
            var sectionLabel = new GameObject("SectionLabel");
            sectionLabel.transform.SetParent(statsGo.transform, false);
            var slRect = sectionLabel.AddComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0f, 1f);
            slRect.anchorMax = new Vector2(1f, 1f);
            slRect.pivot     = new Vector2(0.5f, 1f);
            slRect.anchoredPosition = new Vector2(0, -4);
            slRect.sizeDelta = new Vector2(0, 30);
            var slText = sectionLabel.AddComponent<Text>();
            slText.font = _font;
            slText.text = "  " + Localization.Get("profile.stats_header").ToUpperInvariant();
            slText.fontSize = 15; slText.color = new Color(0.45f, 0.78f, 1f);
            slText.fontStyle = FontStyle.Bold; slText.alignment = TextAnchor.MiddleLeft;

            var record = GameManager.HasInstance
                ? GameManager.Instance.GetRunRecord()
                : RunRecord.Empty();

            float rowTopY = -38f;
            float colW    = PanelW * 0.5f - 20;

            // Left column — personal bests
            AddStatRow(statsGo.transform, Localization.Get("profile.best_time"),
                FormatTime(record.BestTimeSurvived), rowTopY, 10, colW);
            AddStatRow(statsGo.transform, Localization.Get("profile.best_mass"),
                record.BestPeakMass.ToString("F1"), rowTopY - 44, 10, colW);
            AddStatRow(statsGo.transform, Localization.Get("profile.best_dna"),
                record.BestDNAEarned.ToString(), rowTopY - 88, 10, colW);
            AddStatRow(statsGo.transform, Localization.Get("profile.best_species"),
                record.BestSpeciesCount.ToString(), rowTopY - 132, 10, colW);
            AddStatRow(statsGo.transform, Localization.Get("profile.best_traits"),
                record.BestTraitCount.ToString(), rowTopY - 176, 10, colW);

            // Right column — aggregate
            float col2X = PanelW * 0.5f + 10;
            AddStatRow(statsGo.transform, Localization.Get("profile.runs_completed"),
                record.TotalRunsCompleted.ToString(), rowTopY, col2X, colW);
            AddStatRow(statsGo.transform, Localization.Get("profile.leviathans_survived"),
                record.TotalLeviathansSurvived.ToString(), rowTopY - 44, col2X, colW);
            AddStatRow(statsGo.transform, Localization.Get("profile.leviathans_defeated"),
                record.TotalLeviathanDefeated.ToString(), rowTopY - 88, col2X, colW);
        }

        private void AddStatRow(Transform parent, string label, string value, float yOffset, float xOffset, float width)
        {
            var row = new GameObject($"Stat_{label}");
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot     = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(xOffset, yOffset);
            rect.sizeDelta = new Vector2(width, 38);
            row.AddComponent<Image>().color = new Color(0.1f, 0.16f, 0.28f, 0.5f);

            // Label (left)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var lRect = labelGo.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero; lRect.anchorMax = new Vector2(0.65f, 1f);
            lRect.offsetMin = new Vector2(10, 0); lRect.offsetMax = Vector2.zero;
            var lText = labelGo.AddComponent<Text>();
            lText.font = _font; lText.text = label;
            lText.fontSize = LabelFontSz; lText.color = new Color(0.68f, 0.75f, 0.82f);
            lText.alignment = TextAnchor.MiddleLeft;

            // Value (right, gold)
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(row.transform, false);
            var vRect = valueGo.AddComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0.65f, 0f); vRect.anchorMax = Vector2.one;
            vRect.offsetMin = Vector2.zero; vRect.offsetMax = new Vector2(-10, 0);
            var vText = valueGo.AddComponent<Text>();
            vText.font = _font; vText.text = value;
            vText.fontSize = ValueFontSz; vText.color = new Color(1f, 0.85f, 0.3f);
            vText.alignment = TextAnchor.MiddleRight; vText.fontStyle = FontStyle.Bold;
        }

        private static string FormatTime(float seconds)
        {
            if (seconds <= 0) return "—";
            int m = (int)(seconds / 60);
            int s = (int)(seconds % 60);
            return m > 0 ? $"{m}m {s:00}s" : $"{s}s";
        }

        // ---- divider ----

        private void BuildDivider(Transform parent)
        {
            var div = new GameObject("Divider");
            div.transform.SetParent(parent, false);
            var rect = div.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 1f);
            rect.anchorMax = new Vector2(0.98f, 1f);
            rect.pivot     = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -(HeaderH + StatsH + 6));
            rect.sizeDelta = new Vector2(0, DividerH);
            div.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.7f, 0.5f);
        }

        // ---- achievements scroll ----

        private void BuildAchievements(Transform parent)
        {
            float topY  = -(HeaderH + StatsH + DividerH + 8);
            float availH = PanelH - HeaderH - StatsH - DividerH - 16;

            // Section label
            var labelGo = new GameObject("AchievLabel");
            labelGo.transform.SetParent(parent, false);
            var lRect = labelGo.AddComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 1f);
            lRect.anchorMax = new Vector2(1f, 1f);
            lRect.pivot     = new Vector2(0.5f, 1f);
            lRect.anchoredPosition = new Vector2(0, topY);
            lRect.sizeDelta = new Vector2(0, 28);
            var lText = labelGo.AddComponent<Text>();
            lText.font = _font;
            lText.text = "  " + Localization.Get("profile.achievements_header").ToUpperInvariant();
            lText.fontSize = 15; lText.color = new Color(0.45f, 0.78f, 1f);
            lText.fontStyle = FontStyle.Bold; lText.alignment = TextAnchor.MiddleLeft;

            float scrollTopY = topY - 28;
            float scrollH    = availH - 28;

            // ScrollView
            var scrollGo = new GameObject("AchievScroll");
            scrollGo.transform.SetParent(parent, false);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 1f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.pivot     = new Vector2(0.5f, 1f);
            scrollRect.anchoredPosition = new Vector2(0, scrollTopY);
            scrollRect.sizeDelta = new Vector2(0, scrollH);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical   = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollGo.transform, false);
            var vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero; vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;  vpRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot     = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0, 0);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing             = 5;
            layout.padding             = new RectOffset(12, 12, 6, 6);
            layout.childAlignment      = TextAnchor.UpperCenter;
            layout.childControlWidth   = true;
            layout.childControlHeight  = true;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content  = contentRect;
            scroll.viewport = vpRect;
            _scroll = scroll;

            // Populate achievement rows
            var tracker  = GameManager.HasInstance ? GameManager.Instance.Achievements : null;
            var cellDefs = AchievementCatalog.GetByCategory(AchievementCategory.Cell, includeHidden: true);

            // Progress summary row at top
            int unlocked = 0, points = 0, maxPoints = 0;
            foreach (var d in cellDefs)
            {
                maxPoints += d.Points;
                if (tracker != null && tracker.IsUnlocked(d.Id)) { unlocked++; points += d.Points; }
            }
            AddAchievSummaryRow(content.transform, unlocked, cellDefs.Length, points, maxPoints);

            foreach (var def in cellDefs)
            {
                bool isUnlocked = tracker != null && tracker.IsUnlocked(def.Id);
                AddAchievRow(content.transform, def, isUnlocked);
            }
        }

        private void AddAchievSummaryRow(Transform parent, int unlocked, int total, int points, int maxPoints)
        {
            var row = new GameObject("AchievSummary");
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 32);
            row.AddComponent<Image>().color = new Color(0.1f, 0.18f, 0.32f, 0.7f);
            var text = row.AddComponent<Text>();
            text.font = _font;
            text.text = $"  {unlocked} / {total} unlocked   —   {points} / {maxPoints} pts";
            text.fontSize = 14; text.color = new Color(0.65f, 0.75f, 0.85f);
            text.alignment = TextAnchor.MiddleLeft;
        }

        private void AddAchievRow(Transform parent, AchievementDef def, bool unlocked)
        {
            var row = new GameObject("Achiev_" + def.Id);
            row.transform.SetParent(parent, false);
            var rect = row.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, AchievRowH);
            row.AddComponent<Image>().color = unlocked
                ? new Color(0.11f, 0.20f, 0.33f, 0.85f)
                : new Color(0.09f, 0.09f, 0.14f, 0.85f);

            // Gold/grey side bar
            var bar = new GameObject("Bar");
            bar.transform.SetParent(row.transform, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0.12f); barRect.anchorMax = new Vector2(0f, 0.88f);
            barRect.pivot = new Vector2(0f, 0.5f);  barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(4, 0);
            bar.AddComponent<Image>().color = unlocked
                ? new Color(1f, 0.82f, 0.2f, 0.9f)
                : new Color(0.3f, 0.3f, 0.35f, 0.5f);

            const float leftPad = 14f;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(row.transform, false);
            var tRect = titleGo.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0f, 0.5f); tRect.anchorMax = new Vector2(1f, 1f);
            tRect.offsetMin = new Vector2(leftPad, 4); tRect.offsetMax = new Vector2(-80, -4);
            var tText = titleGo.AddComponent<Text>();
            tText.font = _font;
            tText.text = def.Hidden && !unlocked ? "???" : def.Title;
            tText.fontSize = 15;
            tText.color = unlocked ? new Color(0.95f, 0.96f, 1f) : new Color(0.48f, 0.5f, 0.56f);
            tText.fontStyle = unlocked ? FontStyle.Bold : FontStyle.Normal;

            // Description
            var descGo = new GameObject("Desc");
            descGo.transform.SetParent(row.transform, false);
            var dRect = descGo.AddComponent<RectTransform>();
            dRect.anchorMin = new Vector2(0f, 0f); dRect.anchorMax = new Vector2(1f, 0.5f);
            dRect.offsetMin = new Vector2(leftPad, 2); dRect.offsetMax = new Vector2(-80, 4);
            var dText = descGo.AddComponent<Text>();
            dText.font = _font;
            dText.text = def.Hidden && !unlocked ? "???" : def.Description;
            dText.fontSize = 11; dText.color = new Color(0.5f, 0.58f, 0.65f);

            // Points (right edge)
            var ptsGo = new GameObject("Pts");
            ptsGo.transform.SetParent(row.transform, false);
            var pRect = ptsGo.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(1f, 0f); pRect.anchorMax = new Vector2(1f, 1f);
            pRect.pivot = new Vector2(1f, 0.5f); pRect.anchoredPosition = new Vector2(-8, 0);
            pRect.sizeDelta = new Vector2(72, 0);
            var pText = ptsGo.AddComponent<Text>();
            pText.font = _font; pText.text = def.Points + " pts";
            pText.fontSize = 13;
            pText.color = unlocked ? new Color(1f, 0.85f, 0.3f) : new Color(0.42f, 0.42f, 0.48f);
            pText.alignment = TextAnchor.MiddleRight;
            pText.fontStyle = unlocked ? FontStyle.Bold : FontStyle.Normal;
        }

        // ---- refresh live data ----

        /// <summary>Re-reads run record and achievement state and rebuilds content in-place.</summary>
        public void Refresh()
        {
            // Simplest approach: destroy and rebuild the scroll content each time it's shown.
            // The panel itself is not rebuilt (header/stats/divider stay). Only the scroll content
            // is cheap enough to iterate. For a 30-achievement list this is imperceptible.
            // (Full rebuild could be done here if needed.)
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnClosed?.Invoke();
                Hide();
            }
        }
    }

    // ---- inner drag handler ----

    /// <summary>
    /// Allows a RectTransform panel to be dragged freely by this GameObject.
    /// Clamps the panel so it cannot be dragged entirely off-screen.
    /// </summary>
    public sealed class ModalDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler
    {
        /// <summary>The panel RectTransform to move when this element is dragged.</summary>
        public RectTransform Target;
        /// <summary>Required to convert screen-space delta to canvas-space delta.</summary>
        public Canvas ParentCanvas;

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null || ParentCanvas == null) return;

            // Convert screen-space pointer delta to canvas-space by dividing by the canvas scale.
            float scale = ParentCanvas.scaleFactor;
            Vector2 delta = eventData.delta / scale;

            Target.anchoredPosition += delta;

            // Clamp so at least 30 px of the panel stays on-screen in each axis.
            const float margin = 30f;
            Vector2 pos  = Target.anchoredPosition;
            Vector2 half = Target.sizeDelta * 0.5f;

            // Canvas reference resolution
            var canvasRect = ParentCanvas.GetComponent<RectTransform>();
            float canvasW = canvasRect != null ? canvasRect.rect.width : UIStyleGuide.Layout.ReferenceResolution.x;
            float canvasH = canvasRect != null ? canvasRect.rect.height : UIStyleGuide.Layout.ReferenceResolution.y;

            float maxX = canvasW  * 0.5f - margin;
            float maxY = canvasH  * 0.5f - margin;

            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
            Target.anchoredPosition = pos;
        }
    }
}
