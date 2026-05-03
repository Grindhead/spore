using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace RoobsWorld.Creature.Editor
{
    /// <summary>
    /// MonoBehaviour that builds the 3D editor environment, UI, and preview for editing <see cref="CreatureDNA"/>.
    /// </summary>
    public class CreatureEditorScene : MonoBehaviour
    {
        [SerializeField] private float _orbitSpeed = 120f;
        [SerializeField] private float _orbitDistance = 5f;
        [SerializeField] private float _orbitHeight = 2f;
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 10f;

        private CreatureEditorState _state;
        private CreatureGallery _gallery;
        private GameObject _creaturePreview;
        private GameObject _editorRoot;
        private Canvas _uiCanvas;

        private float _currentAngle;
        private float _currentZoom;
        private Text _statText;
        private Text _costText;
        private Text _nameText;
        private readonly List<GameObject> _partButtons = new List<GameObject>();

        private Dictionary<string, CreaturePartDef> _partCatalog;
        private static readonly string[] DEFAULT_PART_IDS =
        {
            "mouth_basic", "eye_basic", "leg_basic", "arm_basic", "wing_basic",
            "tail_basic", "horn_basic", "fin_basic", "shell_basic", "spike_basic"
        };

        public CreatureEditorState EditorState => _state;

        public void Initialize(CreatureDNA dna = null, Dictionary<string, CreaturePartDef> catalog = null)
        {
            _state = new CreatureEditorState(dna);
            _partCatalog = catalog ?? new Dictionary<string, CreaturePartDef>();
            _gallery = new CreatureGallery();
            _currentZoom = _orbitDistance;

            BuildEnvironment();
            BuildUI();
            RefreshCreaturePreview();
        }

        private void Update()
        {
            UpdateOrbitCamera();
            UpdateStatDisplay();
        }

        private void UpdateOrbitCamera()
        {
            Vector2 look = InputManager.HasInstance ? InputManager.Instance.LookInput : Vector2.zero;
            _currentAngle += look.x * _orbitSpeed * Time.deltaTime;
            _currentZoom = Mathf.Clamp(_currentZoom - look.y * _zoomSpeed * Time.deltaTime, _minZoom, _maxZoom);

            float rad = _currentAngle * Mathf.Deg2Rad;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(
                    Mathf.Sin(rad) * _currentZoom,
                    _orbitHeight,
                    Mathf.Cos(rad) * _currentZoom
                );
                cam.transform.LookAt(Vector3.up * 0.5f);
            }
        }

        private void UpdateStatDisplay()
        {
            if (_statText != null && _state != null)
            {
                var stats = StatCalculator.Calculate(_state.DNA, _partCatalog);
                _statText.text = stats.ToString();
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

        private void BuildPartPalette(Transform parent)
        {
            var palette = new GameObject("PartPalette");
            palette.transform.SetParent(parent, false);
            var rect = palette.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0.15f, 0.8f);
            rect.offsetMin = new Vector2(10, 10);
            rect.offsetMax = new Vector2(-5, -10);

            var bg = palette.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            var titleObj = CreateUIText(palette.transform, "Parts", new Vector2(5, -5), 16);

            float yOffset = -30f;
            foreach (string partId in DEFAULT_PART_IDS)
            {
                CreatePartButton(palette.transform, partId, yOffset);
                yOffset -= 28f;
            }
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

            CreateToolButton(toolbar.transform, "Undo", 0, () => { _state.Undo(); RefreshCreaturePreview(); });
            CreateToolButton(toolbar.transform, "Redo", 60, () => { _state.Redo(); RefreshCreaturePreview(); });
            CreateToolButton(toolbar.transform, "Sym", 120, () => _state.SetSymmetry(!_state.SymmetryEnabled));
            CreateToolButton(toolbar.transform, "Save", 180, () => _gallery.Save(_state.DNA));
            CreateToolButton(toolbar.transform, "Done", 240, OnDone);
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
            int bone = _state.DNA.Body.SpineSegments > 1 ? 1 : 0;
            _state.AddPart(partId, bone, new Vector3(0.3f, 0, 0));
            RefreshCreaturePreview();
        }

        private void RefreshCreaturePreview()
        {
            if (_creaturePreview != null)
                Destroy(_creaturePreview);

            _creaturePreview = ModelFactory.CreateCreatureModel(
                Vector3.zero, _state.DNA.Colors.PrimaryColor);
            _creaturePreview.name = "CreaturePreview";
        }

        private void OnDone()
        {
            _gallery.Save(_state.DNA);
            if (GameManager.HasInstance)
                GameManager.Instance.SetPlayerCreatureDNA(_state.DNA, persistToDisk: true);
            Debug.Log("[CreatureEditor] Creature saved. Returning to game.");
        }

        private void OnDestroy()
        {
            if (_editorRoot != null) Destroy(_editorRoot);
            if (_creaturePreview != null) Destroy(_creaturePreview);
            if (_uiCanvas != null) Destroy(_uiCanvas.gameObject);
        }
    }
}
