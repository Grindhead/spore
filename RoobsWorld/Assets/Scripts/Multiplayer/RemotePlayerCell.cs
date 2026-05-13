using UnityEngine;
using TMPro;
using RoobsWorld.Utils;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Visual ghost representing a remote player's cell in the shared ocean.
    ///
    /// Driven entirely by received <see cref="PlayerNetState"/> snapshots — no physics, no input.
    /// Position is smoothly interpolated toward the last known server position to hide jitter.
    ///
    /// Lifecycle: created/destroyed by <see cref="NetworkTickManager"/> when players join/leave.
    /// </summary>
    public sealed class RemotePlayerCell : MonoBehaviour
    {
        // Remote player colour palette — index 0 = player 1, 1 = player 2, 2 = player 3.
        // Local player (id 0) keeps the default green assigned by CellStage.SpawnPlayer().
        private static readonly Color[] RemoteColors =
        {
            new Color(0.20f, 0.85f, 1.00f, 1f),  // Player 1 — cyan
            new Color(1.00f, 0.50f, 0.10f, 1f),  // Player 2 — orange
            new Color(0.90f, 0.20f, 0.80f, 1f),  // Player 3 — pink
        };

        /// <summary>Smooth-follow speed (units/s × lerp factor per frame). Higher = snappier.</summary>
        public const float LerpSpeed = 12f;
        public const float NameLabelBaseHeight = 1.35f;
        public const float NameLabelScaleHeightFactor = 0.45f;
        public const float NameLabelFontSize = 3.2f;
        public const int MaxDisplayNameLength = 24;
        public const float MassRelationThreshold = 1.15f;
        public const float CriticalMassThreshold = 1f;
        public const float LowMassThreshold = 2f;

        private int     _playerId;
        private string  _displayName;
        private Vector3 _targetPosition;
        private float   _targetScale = 1f;
        private float   _lastRemoteMass = -1f;
        private float   _lastLocalMass = -1f;
        private bool    _isAlive = true;
        private Transform   _nameLabelRoot;
        private TextMeshPro _nameLabel;

        public int PlayerId => _playerId;
        public string DisplayName => _displayName;
        public float LastKnownMass => Mathf.Max(0f, _lastRemoteMass);
        public bool IsAliveSnapshot => _isAlive && gameObject.activeSelf;

        // ---- factory ----

        /// <summary>
        /// Spawns a visual ghost cell for the given remote player at <paramref name="startPos"/>.
        /// </summary>
        public static RemotePlayerCell Create(int playerId, Vector3 startPos, string displayName = null)
        {
            Color color = GetColorForPlayer(playerId);
            var go = ModelFactory.CreateCell(startPos, color);
            go.name = $"RemotePlayer_{playerId}";

            // No physics — ghost representation only
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            foreach (var col in go.GetComponentsInChildren<Collider>())
                col.enabled = false;

            var comp = go.AddComponent<RemotePlayerCell>();
            comp._playerId       = playerId;
            comp._targetPosition = startPos;
            comp.SetDisplayName(displayName);
            comp.EnsureNameLabel();
            return comp;
        }

        /// <summary>Returns the display colour for the given player ID (0 = local/green, 1–3 = remote palette).</summary>
        public static Color GetColorForPlayer(int playerId)
        {
            if (playerId <= 0) return new Color(0.3f, 1f, 0.5f); // local player green
            int idx = Mathf.Clamp(playerId - 1, 0, RemoteColors.Length - 1);
            return RemoteColors[idx];
        }

        public static string NormalizeDisplayName(int playerId, string displayName)
        {
            string normalized = displayName?.Trim();
            if (string.IsNullOrEmpty(normalized))
                normalized = $"Player {playerId}";

            if (normalized.Length > MaxDisplayNameLength)
                normalized = normalized.Substring(0, MaxDisplayNameLength);

            return normalized;
        }

        // ---- state application ----

        public void SetDisplayName(string displayName)
        {
            _displayName = NormalizeDisplayName(_playerId, displayName);
            RefreshNameLabel();
        }

        /// <summary>
        /// Updates the ghost with the latest host snapshot for this player.
        /// Call from <see cref="NetworkTickManager"/> whenever a new <see cref="PlayerNetState"/> arrives.
        /// </summary>
        public void ApplyState(in PlayerNetState state)
        {
            ApplyState(state, localMass: -1f);
        }

        public void ApplyState(in PlayerNetState state, float localMass)
        {
            _targetPosition = new Vector3(state.PosX, transform.position.y, state.PosY);
            _lastRemoteMass = Mathf.Max(0f, state.Mass);
            _lastLocalMass  = localMass;
            _targetScale    = Mathf.Clamp(1f + _lastRemoteMass * 0.05f, 0.3f, 4f);
            _isAlive        = state.IsAlive;
            RefreshNameLabel();
            gameObject.SetActive(_isAlive);
        }

        public void ApplyMassSnapshot(float remoteMass, float localMass, bool isAlive = true)
        {
            _lastRemoteMass = Mathf.Max(0f, remoteMass);
            _lastLocalMass = localMass;
            _isAlive = isAlive;
            RefreshNameLabel();
            gameObject.SetActive(_isAlive);
        }

        /// <summary>
        /// Moves the host-side ghost immediately from validated remote input.
        /// </summary>
        public void MoveAuthoritative(Vector3 delta)
        {
            transform.position += delta;
            _targetPosition = transform.position;
        }

        private void EnsureNameLabel()
        {
            if (_nameLabel != null) return;

            var labelGo = new GameObject("UsernameLabel");
            labelGo.transform.SetParent(transform, false);
            _nameLabelRoot = labelGo.transform;

            _nameLabel = labelGo.AddComponent<TextMeshPro>();
            _nameLabel.text = _displayName;
            _nameLabel.fontSize = NameLabelFontSize;
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.color = Color.white;
            _nameLabel.outlineColor = new Color(0f, 0f, 0f, 0.86f);
            _nameLabel.outlineWidth = 0.22f;
            _nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _nameLabel.raycastTarget = false;
            _nameLabel.sortingOrder = 20;

            var rect = _nameLabel.rectTransform;
            rect.sizeDelta = new Vector2(8f, 1.8f);
            PositionNameLabel();
        }

        private void RefreshNameLabel()
        {
            if (_nameLabel == null) return;
            _nameLabel.text = BuildStatusLabel(_playerId, _displayName, _lastRemoteMass, _lastLocalMass, _isAlive);
        }

        public static string BuildStatusLabel(string displayName, float remoteMass, float localMass, bool isAlive)
        {
            return BuildStatusLabel(0, displayName, remoteMass, localMass, isAlive);
        }

        public static string BuildStatusLabel(int playerId, string displayName, float remoteMass, float localMass, bool isAlive)
        {
            string name = NormalizeDisplayName(playerId, displayName);
            string status = BuildMassStatusLine(remoteMass, localMass, isAlive);
            return string.IsNullOrWhiteSpace(status) ? name : $"{name}\n{status}";
        }

        public static string BuildMassStatusLine(float remoteMass, float localMass, bool isAlive)
        {
            if (!isAlive)
                return "Down";

            float safeRemoteMass = Mathf.Max(0f, remoteMass);
            string condition = BuildMassConditionLabel(safeRemoteMass, isAlive);
            string relation = BuildMassRelationLabel(safeRemoteMass, localMass);
            string mass = $"Mass {safeRemoteMass:0.0}";
            string suffix = CombineStatusSuffixes(condition, relation);
            return string.IsNullOrWhiteSpace(suffix)
                ? mass
                : $"{mass} - {suffix}";
        }

        public static string BuildMassConditionLabel(float remoteMass, bool isAlive)
        {
            if (!isAlive)
                return "Down";

            float safeRemoteMass = Mathf.Max(0f, remoteMass);
            if (safeRemoteMass <= CriticalMassThreshold)
                return "Critical";
            if (safeRemoteMass <= LowMassThreshold)
                return "Low mass";
            return string.Empty;
        }

        public static string CombineStatusSuffixes(string first, string second)
        {
            bool hasFirst = !string.IsNullOrWhiteSpace(first);
            bool hasSecond = !string.IsNullOrWhiteSpace(second);
            if (hasFirst && hasSecond) return $"{first} - {second}";
            if (hasFirst) return first;
            return hasSecond ? second : string.Empty;
        }

        public static string BuildMassRelationLabel(float remoteMass, float localMass)
        {
            float safeRemoteMass = Mathf.Max(0f, remoteMass);
            float safeLocalMass = Mathf.Max(0f, localMass);
            if (safeRemoteMass <= 0f || safeLocalMass <= 0f)
                return string.Empty;

            if (safeRemoteMass >= safeLocalMass * MassRelationThreshold)
                return "Larger";
            if (safeLocalMass >= safeRemoteMass * MassRelationThreshold)
                return "Smaller";
            return "Similar";
        }

        private void PositionNameLabel()
        {
            if (_nameLabelRoot == null) return;

            float visualScale = Mathf.Max(0.01f, transform.localScale.x);
            float scaleY = Mathf.Max(0.01f, transform.localScale.y);
            float height = NameLabelBaseHeight + Mathf.Max(0f, visualScale - 1f) * NameLabelScaleHeightFactor;
            _nameLabelRoot.localPosition = new Vector3(0f, height / scaleY, 0f);
            _nameLabelRoot.localScale = Vector3.one / visualScale;
        }

        private void FaceNameLabelToCamera()
        {
            if (_nameLabelRoot == null) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 toCamera = _nameLabelRoot.position - cam.transform.position;
            if (toCamera.sqrMagnitude <= 0.0001f) return;

            _nameLabelRoot.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        // ---- MonoBehaviour ----

        private void Update()
        {
            if (!_isAlive) return;
            float t = LerpSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
            float s = Mathf.Lerp(transform.localScale.x, _targetScale, t);
            transform.localScale = new Vector3(s, s, s);
            PositionNameLabel();
        }

        private void LateUpdate()
        {
            if (!_isAlive) return;
            FaceNameLabelToCamera();
        }
    }
}
