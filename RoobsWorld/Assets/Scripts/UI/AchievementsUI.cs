using System;
using RoobsWorld.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Full-screen achievements browser: lists all achievements by category with locked/unlocked state and points.
    /// </summary>
    public class AchievementsUI : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _root;
        private ScrollRect _scrollRect;
        private Font _font;

        /// <summary>Invoked when the user closes the screen.</summary>
        public Action OnClosed;

        public void Show()
        {
            if (_root != null)
                _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Start()
        {
            _font = UIStyleGuide.GetDefaultFont();
            BuildUI();
        }

        private void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void BuildUI()
        {
            var canvasObj = new GameObject("AchievementsCanvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 250;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            UIStyleGuide.ApplyCanvasScalerDefaults(scaler);
            canvasObj.AddComponent<GraphicRaycaster>();

            _root = new GameObject("AchievementsRoot");
            _root.transform.SetParent(canvasObj.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var bg = _root.AddComponent<Image>();
            bg.color = UIStyleGuide.Colors.ScreenBackground;
            UIStyleGuide.AddScreenAtmosphere(_root.transform, "Achievements", 1f);
            AddBackgroundAtmosphere(_root.transform);

            CreateHeader(canvasObj.transform);
            CreateScrollContent(canvasObj.transform);
            CreateBackButton(canvasObj.transform);
        }

        private void CreateHeader(Transform parent)
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(parent, false);
            var rect = headerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.92f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(800, 80);

            var title = headerObj.AddComponent<Text>();
            title.font = _font;
            title.text = Localization.Get("menu.achievements");
            title.fontSize = UIStyleGuide.Typography.TitleXL;
            title.color = UIStyleGuide.Colors.AccentStrong;
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;
            UIStyleGuide.ApplyTextShadow(headerObj, 0.5f);

            var tracker = GameManager.HasInstance ? GameManager.Instance.Achievements : null;
            // Count only Cell-stage achievements (other stages are out of scope)
            var cellDefs = AchievementCatalog.GetByCategory(AchievementCategory.Cell, includeHidden: true);
            int total = cellDefs.Length;
            int unlocked = 0;
            int points = 0;
            int maxPoints = 0;
            foreach (var a in cellDefs)
            {
                maxPoints += a.Points;
                if (tracker != null && tracker.IsUnlocked(a.Id))
                {
                    unlocked++;
                    points += a.Points;
                }
            }

            var subObj = new GameObject("SubHeader");
            subObj.transform.SetParent(parent, false);
            var subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.86f);
            subRect.anchorMax = new Vector2(0.5f, 0.92f);
            subRect.sizeDelta = new Vector2(600, 36);

            var subText = subObj.AddComponent<Text>();
            subText.font = _font;
            subText.text = $"{unlocked} / {total}  -  {points} / {maxPoints} pts";
            subText.fontSize = 22;
            subText.color = UIStyleGuide.Colors.Subtitle;
            subText.alignment = TextAnchor.MiddleCenter;
            UIStyleGuide.ApplyTextShadow(subObj, 0.3f);
        }

        private void CreateScrollContent(Transform parent)
        {
            var scrollObj = new GameObject("ScrollView");
            scrollObj.transform.SetParent(parent, false);
            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.12f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.84f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;
            scrollObj.AddComponent<Image>();
            UIStyleGuide.ApplyPaintedPanelChrome(scrollObj, strong: false, alpha: 0.92f, atmosphere: false);

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = UIStyleGuide.Colors.TransparentHitArea;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(scrollRect.rect.width, 100);

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UIStyleGuide.Layout.StandardSpacing;
            int outerPadding = Mathf.RoundToInt(UIStyleGuide.Layout.LargeSpacing);
            layout.padding = new RectOffset(outerPadding, outerPadding, outerPadding, outerPadding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            _scrollRect = scroll;

            var tracker = GameManager.HasInstance ? GameManager.Instance.Achievements : null;

            // Only Cell-stage achievements — other stages are out of scope
            foreach (AchievementCategory cat in Enum.GetValues(typeof(AchievementCategory)))
            {
                if (cat != AchievementCategory.Cell) continue;
                var defs = AchievementCatalog.GetByCategory(cat, includeHidden: true);
                if (defs.Length == 0) continue;

                AddCategoryLabel(content.transform, GetCategoryLabel(cat));
                foreach (var def in defs)
                {
                    bool unlocked = tracker != null && tracker.IsUnlocked(def.Id);
                    AddAchievementRow(content.transform, def, unlocked);
                }
            }
        }

        private static string GetCategoryLabel(AchievementCategory cat)
        {
            return cat switch
            {
                AchievementCategory.Cell => Localization.Get("stage.cell"),
                _ => cat.ToString()
            };
        }

        private void AddCategoryLabel(Transform parent, string label)
        {
            var row = new GameObject("Category_" + label);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 40);
            var rowImage = row.AddComponent<Image>();
            rowImage.sprite = UIStyleGuide.GetPaintedFieldSprite();
            rowImage.type = Image.Type.Sliced;
            rowImage.color = UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.RowStripe, 0.92f);
            UIStyleGuide.ApplyOutline(row, UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilGoldBright, 0.18f), new Vector2(1f, -1f));

            var text = row.AddComponent<Text>();
            text.font = _font;
            text.text = "  " + label.ToUpperInvariant();
            text.fontSize = UIStyleGuide.Typography.BodyLarge - 2;
            text.color = UIStyleGuide.Colors.Accent;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
        }

        private void AddAchievementRow(Transform parent, AchievementDef def, bool unlocked)
        {
            var row = new GameObject("Achievement_" + def.Id);
            row.transform.SetParent(parent, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 72);

            var bg = row.AddComponent<Image>();
            bg.sprite = UIStyleGuide.GetPaintedFieldSprite();
            bg.type = Image.Type.Sliced;
            bg.color = unlocked
                ? UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.PanelBackground, 0.9f)
                : UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.DeepNavy, 0.9f);
            UIStyleGuide.ApplyOutline(row, UIStyleGuide.Colors.WithAlpha(unlocked ? UIStyleGuide.Colors.FossilGoldBright : UIStyleGuide.Colors.PanelBorder, unlocked ? 0.28f : 0.18f), new Vector2(1f, -1f));

            // Left colour bar: gold if unlocked, dim grey if not
            var sideBar = new GameObject("SideBar");
            sideBar.transform.SetParent(row.transform, false);
            var sbRect = sideBar.AddComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(0f, 0.1f);
            sbRect.anchorMax = new Vector2(0f, 0.9f);
            sbRect.pivot = new Vector2(0f, 0.5f);
            sbRect.anchoredPosition = Vector2.zero;
            sbRect.sizeDelta = new Vector2(4, 0);
            sideBar.AddComponent<Image>().color = unlocked
                ? UIStyleGuide.Colors.UnlockedGold
                : UIStyleGuide.Colors.LockedGray;

            var iconSize = 46f;
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(row.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(14, 0);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = UIStyleGuide.GetPaintedButtonSprite();
            iconImg.type = Image.Type.Sliced;
            iconImg.color = unlocked ? UIStyleGuide.Colors.UnlockedGold : UIStyleGuide.Colors.LockedGray;

            float left = 14f + iconSize + 12f;
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(row.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(left, 6);
            titleRect.offsetMax = new Vector2(-86, -4);
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = _font;
            titleText.text = def.Hidden && !unlocked ? "???" : def.Title;
            titleText.fontSize = UIStyleGuide.Typography.BodySmall + 1;
            titleText.color = unlocked ? UIStyleGuide.Colors.BodyText : UIStyleGuide.Colors.BodyTextSubtle;
            titleText.fontStyle = unlocked ? FontStyle.Bold : FontStyle.Normal;

            var descObj = new GameObject("Desc");
            descObj.transform.SetParent(row.transform, false);
            var descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 0f);
            descRect.anchorMax = new Vector2(1f, 0.5f);
            descRect.offsetMin = new Vector2(left, 4);
            descRect.offsetMax = new Vector2(-86, 6);
            var descText = descObj.AddComponent<Text>();
            descText.font = _font;
            descText.text = def.Hidden && !unlocked ? "???" : def.Description;
            descText.fontSize = 13;
            descText.color = UIStyleGuide.Colors.BodyTextSubtle;

            var ptsObj = new GameObject("Points");
            ptsObj.transform.SetParent(row.transform, false);
            var ptsRect = ptsObj.AddComponent<RectTransform>();
            ptsRect.anchorMin = new Vector2(1f, 0f);
            ptsRect.anchorMax = new Vector2(1f, 1f);
            ptsRect.pivot = new Vector2(1f, 0.5f);
            ptsRect.anchoredPosition = new Vector2(-10, 0);
            ptsRect.sizeDelta = new Vector2(78, 0);
            var ptsText = ptsObj.AddComponent<Text>();
            ptsText.font = _font;
            ptsText.text = def.Points + " pts";
            ptsText.fontSize = UIStyleGuide.Typography.Caption;
            ptsText.color = unlocked ? UIStyleGuide.Colors.Warning : UIStyleGuide.Colors.LockedGray;
            ptsText.alignment = TextAnchor.MiddleRight;
            ptsText.fontStyle = unlocked ? FontStyle.Bold : FontStyle.Normal;
        }

        private void CreateBackButton(Transform parent)
        {
            var btnObj = new GameObject("BackButton");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.02f);
            rect.anchorMax = new Vector2(0.5f, 0.08f);
            rect.sizeDelta = new Vector2(200, 44);

            var bg = btnObj.AddComponent<Image>();

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            UIStyleGuide.ApplyPaintedButtonChrome(btn, UIButtonVariant.Secondary);
            UIStyleGuide.AddButtonPearl(btnObj, UIButtonVariant.Secondary, 23f);
            btn.onClick.AddListener(() =>
            {
                OnClosed?.Invoke();
                Hide();
            });

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(26f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);

            var text = textObj.AddComponent<Text>();
            text.font = _font;
            text.text = Localization.Get("settings.back");
            text.fontSize = UIStyleGuide.Typography.BodyLarge - 2;
            UIStyleGuide.ApplyButtonLabel(text, UIButtonVariant.Secondary);
            text.alignment = TextAnchor.MiddleCenter;
            UIStyleGuide.ApplyTextShadow(textObj, 0.3f);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnClosed?.Invoke();
                Hide();
            }
        }

        private static void AddBackgroundAtmosphere(Transform parent)
        {
            CreateAtmosphereBand(parent, "AchievementsTopTide", new Vector2(0f, 0.72f), Vector2.one,
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.TideTealBright, 0.09f));
            CreateAtmosphereBand(parent, "AchievementsWarmShelf", Vector2.zero, new Vector2(1f, 0.28f),
                UIStyleGuide.Colors.WithAlpha(UIStyleGuide.Colors.FossilAmber, 0.06f));
        }

        private static void CreateAtmosphereBand(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
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
    }
}
