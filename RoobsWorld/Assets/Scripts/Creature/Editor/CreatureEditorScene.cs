using System.Collections.Generic;
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
        [SerializeField] private float _orbitSpeed = 120f;
        [SerializeField] private float _orbitPitchSpeed = 80f;
        [SerializeField] private float _orbitDistance = 5f;
        [SerializeField] private float _orbitHeight = 2f;
        [SerializeField] private float _zoomSpeed = 2f;
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
        private readonly List<Transform> _spineAnchors = new List<Transform>();

        private string _draggingPartId;
        private bool _isDraggingPart;
        private bool _lastDropValid;
        private DropCandidate _lastDropCandidate;
        private GameObject _dragPreviewPart;
        private bool _isBodySculptDrag;
        private Vector2 _bodyDragStartScreen;
        private Vector3 _bodyDragStartScale;
        private float _bodyDragStartFrontPull;
        private int _bodyDragStartAnchorIndex;

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
            _partCatalog = catalog ?? CreaturePartCatalog.GetDefaultCatalog();
            _gallery = new CreatureGallery();
            _currentZoom = _orbitDistance;

            BuildEnvironment();
            BuildUI();
            RefreshCreaturePreview();
        }

        private void Update()
        {
            UpdateOrbitCamera();
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

        private void UpdateDragPreview()
        {
            if (!_isDraggingPart)
            {
                DestroyDragPreviewPart();
                return;
            }

            _lastDropValid = TryGetDropCandidate(out _lastDropCandidate, out string reason);
            string partLabel = _draggingPartId != null ? _draggingPartId.Replace("_", " ") : "part";
            if (_lastDropValid)
            {
                SetStatus($"Drop {partLabel}: valid snap");
                UpdateDragPreviewPart();
            }
            else
            {
                SetStatus($"Drop {partLabel}: {reason}");
                DestroyDragPreviewPart();
            }
        }

        private void UpdateBodySculptInput()
        {
            if (_isDraggingPart || _state?.DNA?.Body == null || Mouse.current == null)
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

            if (!hit.transform.IsChildOf(_creaturePreview.transform))
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

            _creaturePreview = ModelFactory.CreateCreatureModel(Vector3.zero, _state.DNA);
            _creaturePreview.name = "CreaturePreview";
            CacheSpineAnchors();
            EnsurePreviewColliders();
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
            _draggingPartId = partId;
            _isDraggingPart = true;
            _lastDropValid = false;
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
                _state.AddPart(_draggingPartId, _lastDropCandidate.AttachBone, _lastDropCandidate.Offset);
                RefreshCreaturePreview();
                SetStatus($"Placed {_draggingPartId.Replace("_", " ")}.");
            }
            else
            {
                SetStatus("Drop blocked. Move to a clear body location.");
            }

            _isDraggingPart = false;
            _draggingPartId = null;
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
            if (!Physics.Raycast(ray, out var hit, 50f))
                return false;

            if (!hit.transform.IsChildOf(_creaturePreview.transform))
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
            Vector3 localHit = _creaturePreview.transform.InverseTransformPoint(hit.point);
            Vector3 localAnchor = _creaturePreview.transform.InverseTransformPoint(anchor.position);
            Vector3 localNormal = _creaturePreview.transform.InverseTransformDirection(hit.normal).normalized;
            Vector3 offset = localHit - localAnchor + (localNormal * 0.04f);

            if (offset.magnitude > 1.25f)
                offset = offset.normalized * 1.25f;

            float newPartRadius = EstimatePartRadius(_draggingPartId, 1f);
            if (HasOverlap(_draggingPartId, nearestAnchor, offset, newPartRadius))
            {
                reason = "overlaps existing part";
                return false;
            }

            candidate = new DropCandidate
            {
                AttachBone = nearestAnchor,
                Offset = offset
            };
            reason = "valid";
            return true;
        }

        private bool HasOverlap(string partId, int attachBone, Vector3 offset, float newRadius)
        {
            var parts = _state?.DNA?.Parts;
            if (parts == null)
                return false;

            for (int i = 0; i < parts.Count; i++)
            {
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

            int anchorIndex = Mathf.Clamp(_lastDropCandidate.AttachBone, 0, Mathf.Max(0, _spineAnchors.Count - 1));
            Transform anchor = _spineAnchors.Count > 0 ? _spineAnchors[anchorIndex] : null;
            if (anchor == null)
                return;

            Vector3 anchorLocal = _creaturePreview.transform.InverseTransformPoint(anchor.position);
            Vector3 localPos = anchorLocal + _lastDropCandidate.Offset;
            _dragPreviewPart.transform.position = _creaturePreview.transform.TransformPoint(localPos);
            _dragPreviewPart.transform.rotation = _creaturePreview.transform.rotation * GetAutoRotation(_draggingPartId, _lastDropCandidate.Offset);
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
    }
}
