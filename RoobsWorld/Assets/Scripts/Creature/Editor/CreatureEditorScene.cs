using System;
using System.Collections.Generic;
using System.Linq;
using RoobsWorld.Core;
using RoobsWorld.Utils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using RoobsWorld.Creature;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// MonoBehaviour that builds the 3D editor environment, UI, and preview for editing <see cref="CreatureDNA"/>.
    /// </summary>
    public class CreatureEditorScene : MonoBehaviour
    {
        private const float PlacedPartDragStartThresholdPixels = 6f;

        [SerializeField] private float _orbitSpeed = 120f;
        [SerializeField] private float _orbitPitchSpeed = 80f;
        [SerializeField] private float _orbitDistance = 5f;
        [SerializeField] private float _orbitHeight = 2f;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _wheelZoomSpeed = 0.025f;
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _minPitch = 10f;
        [SerializeField] private float _maxPitch = 70f;

        private CreatureEditorState _state;
        private CreatureGallery _gallery;
        private GameObject _creaturePreview;
        private GameObject _editorRoot;
        private Canvas _uiCanvas;

        private float _currentAngle;
        private float _currentZoom;
        private float _currentPitch = 28f;
        private Text _statText;
        private Text _costText;
        private Text _nameText;
        private Text _statusText;
        private readonly List<GameObject> _partButtons = new List<GameObject>();
        private readonly List<GameObject> _categoryButtons = new List<GameObject>();
        private readonly List<Transform> _spineAnchors = new List<Transform>();
        private readonly Dictionary<string, Sprite> _partPreviewCache = new Dictionary<string, Sprite>();
        private RectTransform _partGridContent;
        private Text _partPanelTitle;
        private Text _partTooltipText;
        private PartCategory _selectedCategory;
        private bool _hasSelectedCategory;

        private string _draggingPartId;
        private bool _isDraggingPart;
        private bool _isMovingPlacedPart;
        private int _draggingPartIndex = -1;
        private bool _hasPendingPlacedPartDrag;
        private int _pendingPlacedPartDragIndex = -1;
        private Vector2 _pendingPlacedPartDragStartScreen;
        private bool _lastDropValid;
        private DropCandidate _lastDropCandidate;
        private GameObject _dragPreviewPart;
        private bool _isBodySculptDrag;
        private Vector2 _bodyDragStartScreen;
        private Vector3 _bodyDragStartScale;
        private float _bodyDragStartFrontPull;
        private int _bodyDragStartAnchorIndex;
        private float _selectedPartMoveSpeed = 0.9f;
        private float _selectedPartRotateSpeed = 90f;
        private float _selectedPartScaleSpeed = 0.6f;

        private Dictionary<string, CreaturePartDef> _partCatalog;
        public CreatureEditorState EditorState => _state;

        public void Initialize(CreatureDNA dna = null, Dictionary<string, CreaturePartDef> catalog = null)
        {
            _state = new CreatureEditorState(dna);
            _partCatalog = catalog ?? CreaturePartCatalog.GetVisibleCatalog();
            _gallery = new CreatureGallery();
            _currentZoom = _orbitDistance;
            _partPreviewCache.Clear();

            BuildEnvironment();
            BuildUI();
            RefreshCreaturePreview();
        }

        private void Update()
        {
            UpdateOrbitCamera();
            UpdatePartSelectionInput();
            UpdatePlacedPartDragInput();
            UpdateSelectedPartEditInput();
            UpdateBodySculptInput();
            UpdateDragPreview();
            UpdateStatDisplay();
        }

        private void UpdateOrbitCamera()
        {
            Vector2 look = InputManager.HasInstance ? InputManager.Instance.LookInput : Vector2.zero;
            look = FilterMouseLookWithoutRightClick(look);
            _currentAngle += look.x * _orbitSpeed * Time.deltaTime;
            _currentZoom = Mathf.Clamp(_currentZoom - look.y * _zoomSpeed * Time.deltaTime, _minZoom, _maxZoom);
            _currentZoom = ApplyMouseWheelZoom(_currentZoom, _wheelZoomSpeed, _minZoom, _maxZoom);

            if (Mouse.current != null && Mouse.current.middleButton.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                _currentPitch = Mathf.Clamp(
                    _currentPitch - mouseDelta.y * _orbitPitchSpeed * Time.deltaTime,
                    _minPitch,
                    _maxPitch);
            }

            float rad = _currentAngle * Mathf.Deg2Rad;
            float pitchRad = _currentPitch * Mathf.Deg2Rad;
            float horizontalDistance = Mathf.Cos(pitchRad) * _currentZoom;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(
                    Mathf.Sin(rad) * horizontalDistance,
                    Mathf.Sin(pitchRad) * _currentZoom + _orbitHeight,
                    Mathf.Cos(rad) * horizontalDistance
                );
                cam.transform.LookAt(Vector3.up * 0.5f);
            }
        }

        private static float ApplyMouseWheelZoom(float currentZoom, float wheelZoomSpeed, float minZoom, float maxZoom)
        {
            if (Mouse.current == null)
                return currentZoom;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return currentZoom;

            float wheelY = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheelY) <= 0.001f)
                return currentZoom;

            return Mathf.Clamp(currentZoom - wheelY * wheelZoomSpeed, minZoom, maxZoom);
        }

        private static Vector2 FilterMouseLookWithoutRightClick(Vector2 lookInput)
        {
            if (Mouse.current == null || Mouse.current.rightButton.isPressed)
                return lookInput;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            bool hasMouseMovement = mouseDelta.sqrMagnitude > 0.0001f;
            return hasMouseMovement ? Vector2.zero : lookInput;
        }

        private void UpdateStatDisplay()
        {
            if (_statText != null && _state != null)
            {
                var validation = _state.GetLiveValidation(_partCatalog);
                _statText.text = $"{validation.Stats}\n{validation.Summary}";
            }
            if (_costText != null && _state != null)
            {
                _costText.text = $"DNA Cost: {_state.GetDNACost(_partCatalog)}";
            }
        }

        private void BuildEnvironment()
        {
            _editorRoot = new GameObject("EditorEnvironment");

            var floorMat = ProceduralTextureFactory.CreateMaterialWithTexture(
                ProceduralTextureFactory.CreateEditorFloorTexture(),
                new Color(0.18f, 0.18f, 0.22f));
            var floor = ModelFactory.CreateGround(10f, floorMat);
            floor.name = "EditorFloor";
            floor.transform.SetParent(_editorRoot.transform);

            for (int i = -5; i <= 5; i++)
            {
                CreateGridLine(new Vector3(i, 0.01f, -5), new Vector3(i, 0.01f, 5));
                CreateGridLine(new Vector3(-5, 0.01f, i), new Vector3(5, 0.01f, i));
            }

            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = 40f;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }
        }

        private void CreateGridLine(Vector3 from, Vector3 to)
        {
            var lineObj = new GameObject("GridLine");
            lineObj.transform.SetParent(_editorRoot.transform);
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            lr.endColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
        }

        private void BuildUI()
        {
            EnsureEventSystem();

            var canvasObj = new GameObject("EditorCanvas");
            _uiCanvas = canvasObj.AddComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _uiCanvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            BuildPartPalette(canvasObj.transform);
            BuildStatsPanel(canvasObj.transform);
            BuildToolbar(canvasObj.transform);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }

        private void BuildPartPalette(Transform parent)
        {
            var palette = new GameObject("PartPalette");
            palette.transform.SetParent(parent, false);
            var rect = palette.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.34f, 0.96f);
            rect.offsetMin = new Vector2(12, 12);
            rect.offsetMax = new Vector2(-8, -8);

            var bg = palette.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.11f, 0.19f, 0.82f);

            CreateUIText(palette.transform, "PARTS", new Vector2(8, -6), 13);

            var categoryRow = new GameObject("CategoryRow");
            categoryRow.transform.SetParent(palette.transform, false);
            var categoryRect = categoryRow.AddComponent<RectTransform>();
            categoryRect.anchorMin = new Vector2(0f, 1f);
            categoryRect.anchorMax = new Vector2(1f, 1f);
            categoryRect.pivot = new Vector2(0.5f, 1f);
            categoryRect.anchoredPosition = new Vector2(0f, -24f);
            categoryRect.sizeDelta = new Vector2(-10f, 54f);

            var categoryScroll = categoryRow.AddComponent<ScrollRect>();
            categoryScroll.horizontal = true;
            categoryScroll.vertical = false;
            categoryScroll.scrollSensitivity = 50f;

            var categoryViewport = new GameObject("CategoryViewport");
            categoryViewport.transform.SetParent(categoryRow.transform, false);
            var categoryViewportRect = categoryViewport.AddComponent<RectTransform>();
            categoryViewportRect.anchorMin = new Vector2(0f, 0f);
            categoryViewportRect.anchorMax = new Vector2(1f, 1f);
            categoryViewportRect.offsetMin = new Vector2(3f, 3f);
            categoryViewportRect.offsetMax = new Vector2(-3f, -3f);
            categoryViewport.AddComponent<Image>().color = new Color(0.03f, 0.08f, 0.14f, 0.75f);
            categoryViewport.AddComponent<Mask>().showMaskGraphic = false;

            var categoryContentObj = new GameObject("CategoryContent");
            categoryContentObj.transform.SetParent(categoryViewport.transform, false);
            var categoryContentRect = categoryContentObj.AddComponent<RectTransform>();
            categoryContentRect.anchorMin = new Vector2(0f, 0f);
            categoryContentRect.anchorMax = new Vector2(0f, 1f);
            categoryContentRect.pivot = new Vector2(0f, 0.5f);
            categoryContentRect.anchoredPosition = Vector2.zero;
            categoryContentRect.sizeDelta = new Vector2(0f, 0f);

            categoryScroll.viewport = categoryViewportRect;
            categoryScroll.content = categoryContentRect;

            BuildCategoryButtons(categoryContentObj.transform, categoryContentRect);

            _partPanelTitle = CreateUIText(palette.transform, "Select a category", new Vector2(8, -82), 11);
            _partTooltipText = CreateUIText(palette.transform, "", new Vector2(8, -98), 10);
            if (_partTooltipText != null)
                _partTooltipText.color = new Color(0.80f, 0.90f, 1f, 0.95f);

            var viewportObj = new GameObject("PartsViewport");
            viewportObj.transform.SetParent(palette.transform, false);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(1f, 1f);
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -102f);
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0.04f, 0.09f, 0.15f, 0.58f);
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            var contentObj = new GameObject("PartsContent");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            _partGridContent = contentRect;

            var scrollRect = palette.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;

            SelectFirstCategoryWithParts();
        }

        private void BuildCategoryButtons(Transform parent, RectTransform contentRect)
        {
            _categoryButtons.Clear();
            var categories = GetSortedCategories();
            float x = 4f;
            for (int i = 0; i < categories.Count; i++)
            {
                PartCategory category = categories[i];
                int count = CountPartsInCategory(category);
                string label = $"{GetCategoryGlyph(category)}\n{count}";
                var btnObj = CreateCategoryButton(parent, label, x, category);
                AttachCategoryIcon(btnObj.transform, category);
                _categoryButtons.Add(btnObj);
                x += 56f;
            }
            contentRect.sizeDelta = new Vector2(x + 4f, 0f);
        }

        private static string GetCategoryGlyph(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Mouth: return "M";
                case PartCategory.Eye: return "E";
                case PartCategory.Leg: return "L";
                case PartCategory.Arm: return "A";
                case PartCategory.Wing: return "W";
                case PartCategory.Tail: return "T";
                case PartCategory.Horn: return "H";
                case PartCategory.Fin: return "F";
                case PartCategory.Shell: return "S";
                case PartCategory.Spike: return "P";
                default: return "?";
            }
        }

        private List<PartCategory> GetSortedCategories()
        {
            var categories = new HashSet<PartCategory>();
            if (_partCatalog != null)
            {
                foreach (var kvp in _partCatalog)
                    categories.Add(kvp.Value.Category);
            }
            var list = categories.ToList();
            list.Sort((a, b) => a.CompareTo(b));
            return list;
        }

        private int CountPartsInCategory(PartCategory category)
        {
            if (_partCatalog == null)
                return 0;
            int count = 0;
            foreach (var kvp in _partCatalog)
            {
                if (kvp.Value.Category == category)
                    count++;
            }
            return count;
        }

        private GameObject CreateCategoryButton(Transform parent, string label, float xOffset, PartCategory category)
        {
            var btnObj = new GameObject($"Category_{category}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(xOffset, 0f);
            rect.sizeDelta = new Vector2(52f, 0f);

            var bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.16f, 0.23f, 0.33f, 0.95f);
            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.45f, 0.65f, 0.85f, 0.35f);
            outline.effectDistance = new Vector2(1f, -1f);
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => SelectCategory(category));
            CreateUIText(btnObj.transform, label, new Vector2(0f, -2f), 9);
            return btnObj;
        }

        private void AttachCategoryIcon(Transform buttonTransform, PartCategory category)
        {
            string representativePartId = GetRepresentativePartIdForCategory(category);
            if (string.IsNullOrWhiteSpace(representativePartId))
                return;

            Sprite icon = GetOrCreatePartPreviewSprite(representativePartId);
            if (icon == null)
                return;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(buttonTransform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 8f);
            iconRect.sizeDelta = new Vector2(22f, 22f);

            var image = iconObj.AddComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.95f);
        }

        private string GetRepresentativePartIdForCategory(PartCategory category)
        {
            if (_partCatalog == null || _partCatalog.Count == 0)
                return null;

            return _partCatalog
                .Where(kvp => kvp.Value.Category == category)
                .Select(kvp => kvp.Key)
                .OrderBy(id => id)
                .FirstOrDefault();
        }

        private void SelectFirstCategoryWithParts()
        {
            var categories = GetSortedCategories();
            for (int i = 0; i < categories.Count; i++)
            {
                if (CountPartsInCategory(categories[i]) > 0)
                {
                    SelectCategory(categories[i]);
                    return;
                }
            }
        }

        private void SelectCategory(PartCategory category)
        {
            _selectedCategory = category;
            _hasSelectedCategory = true;
            UpdateCategoryButtonVisuals();
            RebuildPartsGrid();
        }

        private void UpdateCategoryButtonVisuals()
        {
            for (int i = 0; i < _categoryButtons.Count; i++)
            {
                var go = _categoryButtons[i];
                if (go == null)
                    continue;
                var image = go.GetComponent<Image>();
                if (image == null)
                    continue;

                bool selected = go.name == $"Category_{_selectedCategory}";
                image.color = selected
                    ? new Color(0.34f, 0.63f, 0.31f, 0.96f)
                    : new Color(0.16f, 0.23f, 0.33f, 0.95f);
            }
        }

        private void RebuildPartsGrid()
        {
            if (_partGridContent == null || _partPanelTitle == null)
                return;

            for (int i = _partGridContent.childCount - 1; i >= 0; i--)
                Destroy(_partGridContent.GetChild(i).gameObject);
            _partButtons.Clear();

            if (!_hasSelectedCategory || _partCatalog == null)
                return;

            var partIds = _partCatalog
                .Where(kvp => kvp.Value.Category == _selectedCategory)
                .Select(kvp => kvp.Key)
                .OrderBy(id => id)
                .ToList();

            _partPanelTitle.text = $"{_selectedCategory.ToString().ToUpperInvariant()}  ({partIds.Count})";

            const int columns = 4;
            const float cardW = 66f;
            const float cardH = 72f;
            const float spacing = 6f;

            for (int i = 0; i < partIds.Count; i++)
            {
                string partId = partIds[i];
                int row = i / columns;
                int col = i % columns;
                float x = 4f + col * (cardW + spacing);
                float y = -4f - row * (cardH + spacing);
                CreatePartCard(_partGridContent, partId, x, y, cardW, cardH);
            }

            int rows = Mathf.CeilToInt(partIds.Count / (float)columns);
            float contentHeight = 8f + rows * (cardH + spacing);
            _partGridContent.sizeDelta = new Vector2(0f, Mathf.Max(10f, contentHeight));
        }

        private void CreatePartCard(Transform parent, string partId, float x, float y, float width, float height)
        {
            var card = new GameObject($"Card_{partId}");
            card.transform.SetParent(parent, false);
            var rect = card.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);

            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.17f, 0.27f, 0.92f);
            var outline = card.AddComponent<Outline>();
            outline.effectColor = new Color(0.48f, 0.69f, 0.92f, 0.20f);
            outline.effectDistance = new Vector2(1f, -1f);
            var shadow = card.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            card.AddComponent<CardHoverAnimator>();
            var btn = card.AddComponent<Button>();
            string id = partId;
            btn.onClick.AddListener(() => OnPartSelected(id));
            var dragHandler = card.AddComponent<PalettePartDragHandler>();
            dragHandler.Initialize(this, id);
            var hover = card.AddComponent<PalettePartHoverHandler>();
            hover.Initialize(this, id);

            var previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(card.transform, false);
            var previewRect = previewObj.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0f, 1f);
            previewRect.anchorMax = new Vector2(1f, 1f);
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewRect.anchoredPosition = new Vector2(0f, -2f);
            previewRect.sizeDelta = new Vector2(-6f, 56f);

            var previewBgObj = new GameObject("PreviewBg");
            previewBgObj.transform.SetParent(previewObj.transform, false);
            var previewBgRect = previewBgObj.AddComponent<RectTransform>();
            previewBgRect.anchorMin = Vector2.zero;
            previewBgRect.anchorMax = Vector2.one;
            previewBgRect.offsetMin = Vector2.zero;
            previewBgRect.offsetMax = Vector2.zero;
            var previewBg = previewBgObj.AddComponent<Image>();
            previewBg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            var previewSpriteObj = new GameObject("PreviewSprite");
            previewSpriteObj.transform.SetParent(previewObj.transform, false);
            var previewSpriteRect = previewSpriteObj.AddComponent<RectTransform>();
            previewSpriteRect.anchorMin = new Vector2(0.05f, 0.05f);
            previewSpriteRect.anchorMax = new Vector2(0.95f, 0.95f);
            previewSpriteRect.offsetMin = Vector2.zero;
            previewSpriteRect.offsetMax = Vector2.zero;
            var previewImage = previewSpriteObj.AddComponent<Image>();
            previewImage.color = Color.white;
            previewImage.sprite = GetOrCreatePartPreviewSprite(partId);
            previewImage.preserveAspect = true;

            CreateUIText(card.transform, CompactPartLabel(partId), new Vector2(3f, -60f), 8);
            _partButtons.Add(card);
        }

        private string CompactPartLabel(string partId)
        {
            if (_partCatalog != null && _partCatalog.TryGetValue(partId, out var def) && !string.IsNullOrWhiteSpace(def.DisplayName))
                return def.DisplayName.Length <= 13 ? def.DisplayName : def.DisplayName.Substring(0, 13);

            if (string.IsNullOrWhiteSpace(partId))
                return "part";
            string[] parts = partId.Split('_');
            if (parts.Length >= 3)
                return $"{parts[1]} {parts[2]}";
            return partId.Replace("_", " ");
        }

        private void OnPartCardHover(string partId, bool entering, Transform cardTransform)
        {
            var hoverAnim = cardTransform != null ? cardTransform.GetComponent<CardHoverAnimator>() : null;
            if (hoverAnim != null)
                hoverAnim.SetHovered(entering);

            if (_partTooltipText != null)
            {
                _partTooltipText.text = entering ? BuildPartTooltip(partId) : string.Empty;
            }
        }

        private string BuildPartTooltip(string partId)
        {
            if (_partCatalog == null || !_partCatalog.TryGetValue(partId, out var def) || def == null)
                return $"Preview: {partId.Replace("_", " ")}";

            string verbs = def.Verbs != null && def.Verbs.Length > 0
                ? string.Join(", ", def.Verbs.Select(v => v.ToString()))
                : "none";
            return $"{def.DisplayName} | {def.Rarity} | DNA {def.DNACost} | {verbs}";
        }

        private Sprite GetOrCreatePartPreviewSprite(string partId)
        {
            if (_partPreviewCache.TryGetValue(partId, out var cached) && cached != null)
                return cached;

            var created = RenderPartPreviewSprite(partId);
            if (created == null && _partCatalog != null && _partCatalog.TryGetValue(partId, out var def) && def != null)
                created = def.Icon;

            _partPreviewCache[partId] = created;
            return created;
        }

        private Sprite RenderPartPreviewSprite(string partId)
        {
            if (_partCatalog == null || !_partCatalog.TryGetValue(partId, out var def) || def == null)
                return null;

            return CreatureThumbnail.CapturePartPreview(def, partId);
        }

        private void BuildStatsPanel(Transform parent)
        {
            var panel = new GameObject("StatsPanel");
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.85f, 0);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.offsetMin = new Vector2(5, 10);
            rect.offsetMax = new Vector2(-10, -10);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            _nameText = CreateUIText(panel.transform, "Name: Roob", new Vector2(5, -5), 14);
            _statText = CreateUIText(panel.transform, "Stats", new Vector2(5, -25), 12);
            _costText = CreateUIText(panel.transform, "DNA Cost: 0", new Vector2(5, -80), 12);
            _statusText = CreateUIText(panel.transform, "Drag body to sculpt, or drag a part onto the body.", new Vector2(5, -105), 10);
        }

        private void BuildToolbar(Transform parent)
        {
            var toolbar = new GameObject("Toolbar");
            toolbar.transform.SetParent(parent, false);
            var rect = toolbar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.9f);
            rect.anchorMax = new Vector2(0.8f, 1f);
            rect.offsetMin = new Vector2(5, 5);
            rect.offsetMax = new Vector2(-5, -5);

            var bg = toolbar.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            CreateToolButton(toolbar.transform, "Body", 0, () => SelectEditorTab(CreatureEditorTab.Body));
            CreateToolButton(toolbar.transform, "Parts", 60, () => SelectEditorTab(CreatureEditorTab.Parts));
            CreateToolButton(toolbar.transform, "Paint", 120, () => SelectEditorTab(CreatureEditorTab.Paint));
            CreateToolButton(toolbar.transform, "Test", 180, () => SelectEditorTab(CreatureEditorTab.TestDrive));
            CreateToolButton(toolbar.transform, "Undo", 240, () => { _state.Undo(); RefreshCreaturePreview(); });
            CreateToolButton(toolbar.transform, "Redo", 300, () => { _state.Redo(); RefreshCreaturePreview(); });
            CreateToolButton(toolbar.transform, "Sym", 360, CycleSymmetryMode);
            CreateToolButton(toolbar.transform, "Delete", 420, DeleteSelectedPart);
            CreateToolButton(toolbar.transform, "Save", 480, () => _gallery.Save(_state.DNA));
            CreateToolButton(toolbar.transform, "Done", 540, OnDone);
        }

        private void SelectEditorTab(CreatureEditorTab tab)
        {
            _state?.SetCurrentTab(tab);
            SetStatus(tab switch
            {
                CreatureEditorTab.Body => "Body tab: drag the body or sculpt spine segments.",
                CreatureEditorTab.Parts => "Parts tab: drag a part onto a valid body anchor.",
                CreatureEditorTab.Paint => "Paint tab: edit base, accent, pattern, material, and part colors.",
                CreatureEditorTab.TestDrive => "Test Drive: preview idle, walk, attack, social, and cooldown readiness.",
                CreatureEditorTab.Save => "Save tab: review validation, save, or return to Creature stage.",
                _ => "Editor tab selected."
            });
        }

        private void CycleSymmetryMode()
        {
            if (_state == null)
                return;

            SymmetryType next = _state.SymmetryMode switch
            {
                SymmetryType.None => SymmetryType.Bilateral,
                SymmetryType.Bilateral => SymmetryType.Radial,
                _ => SymmetryType.None
            };
            _state.SetSymmetryMode(next);
            SetStatus($"Symmetry: {next}");
        }

        private void CreatePartButton(Transform parent, string partId, float yOffset)
        {
            var btnObj = new GameObject($"Btn_{partId}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(5, yOffset);
            rect.sizeDelta = new Vector2(-10, 25);

            var bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.3f, 0.5f, 0.8f);

            var btn = btnObj.AddComponent<Button>();
            string id = partId;
            btn.onClick.AddListener(() => OnPartSelected(id));
            var dragHandler = btnObj.AddComponent<PalettePartDragHandler>();
            dragHandler.Initialize(this, id);

            CreateUIText(btnObj.transform, partId.Replace("_", " "), Vector2.zero, 11);
            _partButtons.Add(btnObj);
        }

        private void CreateToolButton(Transform parent, string label, float xOffset, System.Action onClick)
        {
            var btnObj = new GameObject($"Tool_{label}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(xOffset + 5, 0);
            rect.sizeDelta = new Vector2(55, -6);

            var bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            CreateUIText(btnObj.transform, label, Vector2.zero, 12);
        }

        private Text CreateUIText(Transform parent, string content, Vector2 pos, int fontSize)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(0, 20);

            var text = obj.AddComponent<Text>();
            text.font = RoobsWorld.UI.UIStyleGuide.GetDefaultFont();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private void OnPartSelected(string partId)
        {
            SetStatus($"Drag {partId.Replace("_", " ")} onto the creature.");
        }

        private void UpdatePartSelectionInput()
        {
            if (_isDraggingPart || _hasPendingPlacedPartDrag || _isBodySculptDrag || Mouse.current == null)
                return;

            if (!Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (TryGetPartHit(out _, out CreaturePartMount mount))
            {
                _state.SelectPart(mount.PartIndex);
                _hasPendingPlacedPartDrag = true;
                _pendingPlacedPartDragIndex = mount.PartIndex;
                _pendingPlacedPartDragStartScreen = Mouse.current.position.ReadValue();
                SetStatus($"Selected {mount.PartId.Replace("_", " ")}. Drag to move, or use WASD/RF, Q/E, +/- and Del.");
                return;
            }

            _state.DeselectPart();
        }

        private void UpdatePlacedPartDragInput()
        {
            if (!_hasPendingPlacedPartDrag && !_isMovingPlacedPart)
                return;

            if (Mouse.current == null)
            {
                ClearPendingPlacedPartDrag();
                if (_isMovingPlacedPart)
                    EndPartDrag();
                return;
            }

            if (_hasPendingPlacedPartDrag)
            {
                if (!Mouse.current.leftButton.isPressed)
                {
                    ClearPendingPlacedPartDrag();
                    return;
                }

                Vector2 current = Mouse.current.position.ReadValue();
                float dragDistance = (current - _pendingPlacedPartDragStartScreen).magnitude;
                if (dragDistance >= PlacedPartDragStartThresholdPixels)
                {
                    BeginPlacedPartDrag(_pendingPlacedPartDragIndex);
                    ClearPendingPlacedPartDrag();
                }
            }

            if (_isMovingPlacedPart)
            {
                if (Mouse.current.leftButton.isPressed)
                    UpdatePartDrag();

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    EndPartDrag();
            }
        }

        private void ClearPendingPlacedPartDrag()
        {
            _hasPendingPlacedPartDrag = false;
            _pendingPlacedPartDragIndex = -1;
            _pendingPlacedPartDragStartScreen = Vector2.zero;
        }

        private void UpdateSelectedPartEditInput()
        {
            if (_isDraggingPart || _hasPendingPlacedPartDrag || _isBodySculptDrag || Keyboard.current == null)
                return;

            int selected = _state.SelectedPartIndex;
            if (selected < 0 || selected >= _state.DNA.Parts.Count)
                return;

            if (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
            {
                DeleteSelectedPart();
                return;
            }

            bool changed = false;
            PartPlacement part = _state.DNA.Parts[selected];
            float dt = Time.deltaTime;

            Vector3 move = Vector3.zero;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.rKey.isPressed) move.y += 1f;
            if (Keyboard.current.fKey.isPressed) move.y -= 1f;
            if (Keyboard.current.wKey.isPressed) move.z += 1f;
            if (Keyboard.current.sKey.isPressed) move.z -= 1f;
            if (move.sqrMagnitude > 0.0001f)
            {
                Vector3 newOffset = part.Offset + move.normalized * (_selectedPartMoveSpeed * dt);
                changed |= _state.MovePart(selected, newOffset);
            }

            Vector3 rotation = part.Rotation;
            if (Keyboard.current.qKey.isPressed)
            {
                rotation.y -= _selectedPartRotateSpeed * dt;
                changed = true;
            }
            if (Keyboard.current.eKey.isPressed)
            {
                rotation.y += _selectedPartRotateSpeed * dt;
                changed = true;
            }
            if (changed && (Keyboard.current.qKey.isPressed || Keyboard.current.eKey.isPressed))
                _state.RotatePart(selected, rotation);

            float scaleDelta = 0f;
            if (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed) scaleDelta += 1f;
            if (Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed) scaleDelta -= 1f;
            if (Mathf.Abs(scaleDelta) > 0.01f)
                changed |= _state.ScalePart(selected, part.Scale + (scaleDelta * _selectedPartScaleSpeed * dt));

            if (changed)
                RefreshCreaturePreview();
        }

        private void DeleteSelectedPart()
        {
            int selected = _state.SelectedPartIndex;
            if (selected < 0)
            {
                SetStatus("Select a part first, then delete.");
                return;
            }

            if (_state.RemovePart(selected))
            {
                RefreshCreaturePreview();
                SetStatus("Part removed.");
            }
        }

        private void UpdateDragPreview()
        {
            if (!_isDraggingPart)
            {
                DestroyDragPreviewPart();
                return;
            }

            _lastDropValid = TryGetDropCandidate(out _lastDropCandidate, out string reason);
            string partLabel = _draggingPartId != null ? _draggingPartId.Replace("_", " ") : "part";
            string actionLabel = _isMovingPlacedPart ? "Move" : "Drop";
            if (_lastDropValid)
            {
                SetStatus($"{actionLabel} {partLabel}: valid snap");
            }
            else
            {
                SetStatus($"{actionLabel} {partLabel}: {reason}");
            }

            UpdateDragPreviewPart();
        }

        private void UpdateBodySculptInput()
        {
            if (_isDraggingPart || _hasPendingPlacedPartDrag || _state?.DNA?.Body == null || Mouse.current == null)
                return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryBeginBodySculptDrag();

            if (_isBodySculptDrag && Mouse.current.leftButton.isPressed)
                ContinueBodySculptDrag();

            if (_isBodySculptDrag && Mouse.current.leftButton.wasReleasedThisFrame)
                EndBodySculptDrag();
        }

        private void TryBeginBodySculptDrag()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (TryGetPartHit(out _, out CreaturePartMount mount))
            {
                _state.SelectPart(mount.PartIndex);
                return;
            }

            if (!TryGetBodyHit(out var hit, out int nearestAnchor))
                return;

            _isBodySculptDrag = true;
            _bodyDragStartScreen = Mouse.current.position.ReadValue();
            _bodyDragStartScale = _state.DNA.Body.BodyScale;
            _bodyDragStartFrontPull = _state.DNA.Body.FrontPull;
            _bodyDragStartAnchorIndex = nearestAnchor;
            SetStatus("Sculpting body: drag to stretch like plasticine.");
        }

        private void ContinueBodySculptDrag()
        {
            Vector2 current = Mouse.current.position.ReadValue();
            Vector2 delta = current - _bodyDragStartScreen;

            float widthDelta = delta.x * 0.0025f;
            float lengthDelta = delta.y * 0.0025f;
            float heightDelta = delta.y * 0.0012f;

            Vector3 newScale = new Vector3(
                _bodyDragStartScale.x + widthDelta,
                _bodyDragStartScale.y + heightDelta,
                _bodyDragStartScale.z + lengthDelta);

            float frontPullDelta = _bodyDragStartAnchorIndex == 0 ? (delta.y * 0.0018f) : (delta.y * 0.0006f);
            float newFrontPull = _bodyDragStartFrontPull + frontPullDelta;

            _state.SetBodyShape(newScale, newFrontPull);
            RefreshCreaturePreview();
        }

        private void EndBodySculptDrag()
        {
            _isBodySculptDrag = false;
            SetStatus("Body sculpt updated. Drag the body again or place parts.");
        }

        private bool TryGetBodyHit(out RaycastHit hit, out int nearestAnchor)
        {
            hit = default;
            nearestAnchor = 0;

            if (_creaturePreview == null || Camera.main == null || _spineAnchors.Count == 0)
                return false;

            Vector2 pointerPos = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : Vector2.zero;

            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            if (!Physics.Raycast(ray, out hit, 50f))
                return false;

            if (!IsMainBodyHit(hit.transform))
                return false;

            float bestSqr = float.MaxValue;
            for (int i = 0; i < _spineAnchors.Count; i++)
            {
                float sqr = (hit.point - _spineAnchors[i].position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearestAnchor = i;
                }
            }

            return true;
        }

        private void RefreshCreaturePreview()
        {
            if (_creaturePreview != null)
                Destroy(_creaturePreview);

            _creaturePreview = ModelFactory.CreateCreatureModel(Vector3.zero, _state.DNA, false);
            _creaturePreview.name = "CreaturePreview";
            CacheSpineAnchors();
            EnsurePreviewColliders();
            HighlightSelectedPart();
        }

        private void HighlightSelectedPart()
        {
            int selected = _state != null ? _state.SelectedPartIndex : -1;
            if (_creaturePreview == null || selected < 0)
                return;

            var mounts = _creaturePreview.GetComponentsInChildren<CreaturePartMount>(true);
            for (int i = 0; i < mounts.Length; i++)
            {
                var mount = mounts[i];
                if (mount == null || mount.PartIndex != selected)
                    continue;

                var renderers = mount.GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    var renderer = renderers[r];
                    if (renderer == null)
                        continue;

                    var materials = renderer.materials;
                    for (int m = 0; m < materials.Length; m++)
                    {
                        var mat = materials[m];
                        if (mat != null && mat.HasProperty("_Color"))
                            mat.color = Color.Lerp(mat.color, new Color(1f, 0.94f, 0.5f, mat.color.a), 0.45f);
                    }
                    renderer.materials = materials;
                }
            }
        }

        private void CacheSpineAnchors()
        {
            _spineAnchors.Clear();
            if (_creaturePreview == null)
                return;

            int spineSegments = _state?.DNA?.Body != null ? Mathf.Max(1, _state.DNA.Body.SpineSegments) : 1;
            for (int i = 0; i < spineSegments; i++)
            {
                var anchor = _creaturePreview.transform.Find($"Body/{CreatureModelBuilder.SpineAnchorPrefix}{i}");
                if (anchor != null)
                    _spineAnchors.Add(anchor);
            }
        }

        private void EnsurePreviewColliders()
        {
            if (_creaturePreview == null)
                return;

            var meshFilters = _creaturePreview.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var mf = meshFilters[i];
                if (mf == null || mf.sharedMesh == null)
                    continue;

                var existingCollider = mf.GetComponent<Collider>();
                if (existingCollider == null)
                {
                    var meshCollider = mf.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = mf.sharedMesh;
                    meshCollider.convex = false;
                }
            }
        }

        internal void BeginPartDrag(string partId)
        {
            ClearPendingPlacedPartDrag();
            _draggingPartId = partId;
            _isDraggingPart = true;
            _isMovingPlacedPart = false;
            _draggingPartIndex = -1;
            _lastDropValid = false;
        }

        private void BeginPlacedPartDrag(int partIndex)
        {
            if (_state?.DNA?.Parts == null || partIndex < 0 || partIndex >= _state.DNA.Parts.Count)
                return;

            var placement = _state.DNA.Parts[partIndex];
            if (placement == null || string.IsNullOrWhiteSpace(placement.PartId))
                return;

            _state.SelectPart(partIndex);
            _draggingPartId = placement.PartId;
            _draggingPartIndex = partIndex;
            _isMovingPlacedPart = true;
            _isDraggingPart = true;
            _lastDropValid = false;
            DestroyDragPreviewPart();
            SetStatus($"Moving {placement.PartId.Replace("_", " ")}. Release over the body to place it.");
        }

        internal void UpdatePartDrag()
        {
            if (!_isDraggingPart)
                return;

            _lastDropValid = TryGetDropCandidate(out _lastDropCandidate, out _);
        }

        internal void EndPartDrag()
        {
            if (!_isDraggingPart)
                return;

            if (_lastDropValid && !string.IsNullOrWhiteSpace(_draggingPartId))
            {
                if (_isMovingPlacedPart)
                {
                    if (_state.MovePart(
                        _draggingPartIndex,
                        _lastDropCandidate.AttachBone,
                        _lastDropCandidate.Offset,
                        _lastDropCandidate.RotationEuler))
                    {
                        RefreshCreaturePreview();
                        SetStatus($"Moved {_draggingPartId.Replace("_", " ")}.");
                    }
                    else
                    {
                        SetStatus("Move blocked. Select the part and try again.");
                    }
                }
                else
                {
                    _state.AddPart(
                        _draggingPartId,
                        _lastDropCandidate.AttachBone,
                        _lastDropCandidate.Offset,
                        1f,
                        _lastDropCandidate.RotationEuler);
                    RefreshCreaturePreview();
                    SetStatus($"Placed {_draggingPartId.Replace("_", " ")}.");
                }
            }
            else
            {
                SetStatus(_isMovingPlacedPart
                    ? "Move blocked. Release over a clear body location."
                    : "Drop blocked. Move to a clear body location.");
            }

            _isDraggingPart = false;
            _isMovingPlacedPart = false;
            _draggingPartId = null;
            _draggingPartIndex = -1;
            DestroyDragPreviewPart();
        }

        private bool TryGetDropCandidate(out DropCandidate candidate, out string reason)
        {
            candidate = default;
            reason = "hover over body";

            if (_creaturePreview == null || Camera.main == null)
                return false;

            Vector2 pointerPos;
            if (Mouse.current != null)
                pointerPos = Mouse.current.position.ReadValue();
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
            else
                return false;

            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            if (!TryRaycastMainBody(ray, out var hit))
                return false;

            if (_spineAnchors.Count == 0)
                return false;

            int nearestAnchor = 0;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _spineAnchors.Count; i++)
            {
                float sqr = (hit.point - _spineAnchors[i].position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearestAnchor = i;
                }
            }

            Transform anchor = _spineAnchors[nearestAnchor];
            float partScale = GetDraggingPartScale();
            float newPartRadius = EstimatePartRadius(_draggingPartId, partScale);
            Vector3 localHit = _creaturePreview.transform.InverseTransformPoint(hit.point);
            Vector3 localAnchor = _creaturePreview.transform.InverseTransformPoint(anchor.position);
            Vector3 localNormal = _creaturePreview.transform.InverseTransformDirection(hit.normal).normalized;
            Vector3 localBodyCenter = _creaturePreview.transform.InverseTransformPoint(_creaturePreview.transform.position);
            Vector3 radialOut = (localHit - localBodyCenter).normalized;
            if (Vector3.Dot(localNormal, radialOut) < 0f)
                localNormal = -localNormal;
            float surfacePush = Mathf.Max(0.04f, newPartRadius * 0.78f);
            Vector3 offset = localHit - localAnchor + (localNormal * surfacePush);

            float maxOffset = 1.25f;
            if (_state?.DNA?.Body != null)
                maxOffset = Mathf.Max(maxOffset, _state.DNA.Body.BodyScale.magnitude * 1.1f);

            if (offset.magnitude > maxOffset)
                offset = offset.normalized * maxOffset;

            int ignoredPartIndex = _isMovingPlacedPart ? _draggingPartIndex : -1;
            if (HasOverlap(_draggingPartId, nearestAnchor, offset, newPartRadius, ignoredPartIndex))
            {
                reason = "overlaps existing part";
                return false;
            }

            candidate = new DropCandidate
            {
                AttachBone = nearestAnchor,
                Offset = offset,
                SurfaceNormal = localNormal,
                RotationEuler = ComputePlacementRotationEuler(_draggingPartId, offset, localNormal)
            };
            reason = "valid";
            return true;
        }

        private bool IsMainBodyHit(Transform hitTransform)
        {
            if (_creaturePreview == null || hitTransform == null)
                return false;

            Transform bodyRoot = _creaturePreview.transform.Find("Body");
            if (bodyRoot == null)
                return false;

            return hitTransform == bodyRoot || hitTransform.IsChildOf(bodyRoot);
        }

        private bool TryRaycastMainBody(Ray ray, out RaycastHit bodyHit)
        {
            bodyHit = default;

            var hits = Physics.RaycastAll(ray, 50f);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (!IsMainBodyHit(hits[i].transform))
                    continue;

                bodyHit = hits[i];
                return true;
            }

            return false;
        }

        private bool TryGetPartHit(out RaycastHit hit, out CreaturePartMount mount)
        {
            hit = default;
            mount = null;

            if (_creaturePreview == null || Camera.main == null || Mouse.current == null)
                return false;

            Vector2 pointerPos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            var hits = Physics.RaycastAll(ray, 50f);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var candidateMount = hits[i].transform.GetComponentInParent<CreaturePartMount>();
                if (candidateMount == null)
                    continue;

                hit = hits[i];
                mount = candidateMount;
                return true;
            }

            return false;
        }

        private bool HasOverlap(string partId, int attachBone, Vector3 offset, float newRadius, int ignoredPartIndex = -1)
        {
            var parts = _state?.DNA?.Parts;
            if (parts == null)
                return false;

            for (int i = 0; i < parts.Count; i++)
            {
                if (i == ignoredPartIndex)
                    continue;

                var existing = parts[i];
                if (existing == null || existing.AttachBone != attachBone)
                    continue;

                float existingRadius = EstimatePartRadius(existing.PartId, existing.Scale);
                float minDistance = (newRadius + existingRadius) * 0.85f;
                if ((existing.Offset - offset).sqrMagnitude < (minDistance * minDistance))
                    return true;
            }

            return false;
        }

        private float GetDraggingPartScale()
        {
            if (!_isMovingPlacedPart || _state?.DNA?.Parts == null)
                return 1f;

            if (_draggingPartIndex < 0 || _draggingPartIndex >= _state.DNA.Parts.Count)
                return 1f;

            return Mathf.Max(0.1f, _state.DNA.Parts[_draggingPartIndex]?.Scale ?? 1f);
        }

        private float EstimatePartRadius(string partId, float scale)
        {
            float baseRadius = 0.16f;
            if (!string.IsNullOrWhiteSpace(partId) && _partCatalog != null && _partCatalog.TryGetValue(partId, out var def))
                baseRadius = Mathf.Max(0.12f, def.DefaultScale.magnitude * 0.12f);
            return Mathf.Max(0.06f, baseRadius * Mathf.Max(0.15f, scale));
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        private void UpdateDragPreviewPart()
        {
            if (_creaturePreview == null || string.IsNullOrWhiteSpace(_draggingPartId))
                return;

            if (_dragPreviewPart == null)
                _dragPreviewPart = CreateDragPreviewPart(_draggingPartId);

            if (_dragPreviewPart == null)
                return;

            if (_lastDropValid)
            {
                int anchorIndex = Mathf.Clamp(_lastDropCandidate.AttachBone, 0, Mathf.Max(0, _spineAnchors.Count - 1));
                Transform anchor = _spineAnchors.Count > 0 ? _spineAnchors[anchorIndex] : null;
                if (anchor == null)
                    return;

                Vector3 anchorLocal = _creaturePreview.transform.InverseTransformPoint(anchor.position);
                Vector3 localPos = anchorLocal + _lastDropCandidate.Offset + (_lastDropCandidate.SurfaceNormal * 0.015f);
                _dragPreviewPart.transform.position = _creaturePreview.transform.TransformPoint(localPos);
                Quaternion partLocalRotation = GetAutoRotation(_draggingPartId, _lastDropCandidate.Offset) * Quaternion.Euler(_lastDropCandidate.RotationEuler);
                _dragPreviewPart.transform.rotation = _creaturePreview.transform.rotation * partLocalRotation;
                return;
            }

            if (!TryGetPointerPosition(out Vector2 pointerPos) || Camera.main == null)
                return;

            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            float previewDistance = Mathf.Max(1.5f, _currentZoom * 0.45f);
            _dragPreviewPart.transform.position = ray.GetPoint(previewDistance);
            _dragPreviewPart.transform.rotation = Quaternion.LookRotation(-ray.direction, Vector3.up);
        }

        private static bool TryGetPointerPosition(out Vector2 pointerPos)
        {
            pointerPos = default;
            if (Mouse.current != null)
            {
                pointerPos = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            return false;
        }

        private GameObject CreateDragPreviewPart(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return null;

            GameObject previewObj = null;
            if (_partCatalog != null && _partCatalog.TryGetValue(partId, out var def) && def != null)
            {
                if (def.ModelPrefab != null)
                {
                    previewObj = Instantiate(def.ModelPrefab);
                    BlenderAssetLibrary.DisableColliders(previewObj);
                }
                else
                {
                    previewObj = new GameObject($"DragPreview_{partId}");
                    var mf = previewObj.AddComponent<MeshFilter>();
                    mf.mesh = CreaturePartMeshFactory.GetPartMesh(partId);
                    previewObj.AddComponent<MeshRenderer>();
                }

                Vector3 scale = def.DefaultScale == Vector3.zero ? Vector3.one : def.DefaultScale;
                scale *= GetDraggingPartScale();
                previewObj.transform.localScale = scale;
            }

            if (previewObj == null)
                return null;

            previewObj.name = $"DragPreview_{partId}";
            TintDragPreviewRenderers(previewObj);
            return previewObj;
        }

        private static void TintDragPreviewRenderers(GameObject previewObj)
        {
            var renderers = previewObj.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                var materials = renderer.materials;
                for (int m = 0; m < materials.Length; m++)
                {
                    var mat = materials[m];
                    if (mat == null || !mat.HasProperty("_Color"))
                        continue;

                    Color c = mat.color;
                    c.a = 0.55f;
                    mat.color = c;
                }
                renderer.materials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static Quaternion GetAutoRotation(string partId, Vector3 offset)
        {
            bool leftSide = offset.x < -0.001f;

            if ((partId.StartsWith("wing") || partId.StartsWith("fin")) && leftSide)
                return Quaternion.Euler(0f, 180f, 0f);

            if (partId.StartsWith("arm"))
                return Quaternion.Euler(0f, 0f, leftSide ? -18f : 18f);

            return Quaternion.identity;
        }

        private static Vector3 ComputePlacementRotationEuler(string partId, Vector3 offset, Vector3 surfaceNormal)
        {
            Vector3 outward = surfaceNormal.sqrMagnitude > 0.00001f ? surfaceNormal.normalized : Vector3.forward;
            Quaternion autoRotation = GetAutoRotation(partId, offset);
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.forward, outward);
            Quaternion userRotation = Quaternion.Inverse(autoRotation) * targetRotation;
            return userRotation.eulerAngles;
        }

        private void DestroyDragPreviewPart()
        {
            if (_dragPreviewPart != null)
            {
                Destroy(_dragPreviewPart);
                _dragPreviewPart = null;
            }
        }

        private void OnDone()
        {
            _gallery.Save(_state.DNA);
            if (GameManager.HasInstance)
                GameManager.Instance.SetPlayerCreatureDNA(_state.DNA, persistToDisk: true);
            if (StageManager.HasInstance)
                StageManager.Instance.TransitionTo(GameStageType.Creature);
            Debug.Log("[CreatureEditor] Creature saved. Returning to game.");
        }

        private void OnDestroy()
        {
            if (_editorRoot != null) Destroy(_editorRoot);
            if (_creaturePreview != null) Destroy(_creaturePreview);
            DestroyDragPreviewPart();
            if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        }

        private struct DropCandidate
        {
            public int AttachBone;
            public Vector3 Offset;
            public Vector3 SurfaceNormal;
            public Vector3 RotationEuler;
        }

        private sealed class PalettePartDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private CreatureEditorScene _scene;
            private string _partId;

            public void Initialize(CreatureEditorScene scene, string partId)
            {
                _scene = scene;
                _partId = partId;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                _scene?.BeginPartDrag(_partId);
            }

            public void OnDrag(PointerEventData eventData)
            {
                _scene?.UpdatePartDrag();
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                _scene?.EndPartDrag();
            }
        }

        private sealed class PalettePartHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private CreatureEditorScene _scene;
            private string _partId;

            public void Initialize(CreatureEditorScene scene, string partId)
            {
                _scene = scene;
                _partId = partId;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                _scene?.OnPartCardHover(_partId, true, transform);
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                _scene?.OnPartCardHover(_partId, false, transform);
            }
        }

        private sealed class CardHoverAnimator : MonoBehaviour
        {
            private Vector3 _targetScale = Vector3.one;
            private Image _image;
            private Color _baseColor;

            private void Awake()
            {
                _image = GetComponent<Image>();
                if (_image != null)
                    _baseColor = _image.color;
            }

            public void SetHovered(bool hovered)
            {
                _targetScale = hovered ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                if (_image != null)
                {
                    _image.color = hovered
                        ? new Color(_baseColor.r + 0.05f, _baseColor.g + 0.06f, _baseColor.b + 0.08f, _baseColor.a)
                        : _baseColor;
                }
            }

            private void Update()
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * 14f);
            }
        }
    }
}
