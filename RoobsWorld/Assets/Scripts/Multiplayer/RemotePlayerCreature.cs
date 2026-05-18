using TMPro;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Visual representation of a remote player in Creature stage, driven by host snapshots.
    /// </summary>
    public sealed class RemotePlayerCreature : MonoBehaviour
    {
        public const float LerpSpeed = 12f;
        public const float NameLabelBaseHeight = 2.1f;
        public const float NameLabelFontSize = 2.8f;

        private int _playerId;
        private string _displayName;
        private string _creatureId;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation = Quaternion.identity;
        private float _health;
        private float _maxHealth = 1f;
        private float _stamina;
        private bool _isAlive = true;
        private string _sourceDnaJson;
        private CreatureInstance _creature;
        private Transform _nameLabelRoot;
        private TextMeshPro _nameLabel;

        public int PlayerId => _playerId;
        public string CreatureId => _creatureId;
        public bool IsAliveSnapshot => _isAlive;
        public float Health => Mathf.Max(0f, _health);
        public float MaxHealth => Mathf.Max(1f, _maxHealth);
        public float Stamina => Mathf.Max(0f, _stamina);
        public string SourceDnaJson => _sourceDnaJson;

        public static RemotePlayerCreature Create(in CreaturePlayerNetState state, string displayName = null)
        {
            Vector3 startPosition = new Vector3(state.PosX, 0f, state.PosY);
            var dna = ResolveSnapshotDna(state, displayName);

            var root = CreatureFactory.Instance.SpawnCreature(dna, startPosition, isPlayer: false);
            root.name = $"RemoteCreature_{state.PlayerId}";
            DisableLocalSimulation(root);

            var remote = root.AddComponent<RemotePlayerCreature>();
            remote._playerId = state.PlayerId;
            remote._creatureId = dna.CreatureId;
            remote._creature = root.GetComponent<CreatureInstance>();
            remote._displayName = RemotePlayerCell.NormalizeDisplayName(state.PlayerId, displayName);
            remote._sourceDnaJson = NormalizeDnaJson(state.CreatureDNAJson);
            remote.EnsureNameLabel();
            remote.ApplyState(state, displayName);
            return remote;
        }

        public void ApplyState(in CreaturePlayerNetState state, string displayName = null)
        {
            _playerId = state.PlayerId;
            if (!string.IsNullOrWhiteSpace(state.CreatureId))
                _creatureId = state.CreatureId.Trim();
            if (!string.IsNullOrWhiteSpace(displayName))
                _displayName = RemotePlayerCell.NormalizeDisplayName(state.PlayerId, displayName);
            string normalizedDna = NormalizeDnaJson(state.CreatureDNAJson);
            if (!string.IsNullOrWhiteSpace(normalizedDna))
                _sourceDnaJson = normalizedDna;

            _targetPosition = new Vector3(state.PosX, transform.position.y, state.PosY);
            _targetRotation = Quaternion.Euler(0f, state.RotY, 0f);
            _health = Mathf.Max(0f, state.Health);
            _maxHealth = Mathf.Max(1f, state.MaxHealth);
            _stamina = Mathf.Max(0f, state.Stamina);
            _isAlive = state.IsAlive;
            if (_creature == null)
                _creature = GetComponent<CreatureInstance>();
            _creature?.ApplyAuthoritativeHealth(_health, _isAlive);
            RefreshNameLabel();
        }

        public bool MatchesIdentity(in CreaturePlayerNetState state)
        {
            if (_playerId != state.PlayerId)
                return false;

            string incomingId = NormalizeCreatureId(state);
            if (!string.Equals(_creatureId, incomingId, System.StringComparison.Ordinal))
                return false;

            string incomingDna = NormalizeDnaJson(state.CreatureDNAJson);
            return string.IsNullOrWhiteSpace(incomingDna) ||
                string.Equals(_sourceDnaJson, incomingDna, System.StringComparison.Ordinal);
        }

        public void SetDisplayName(string displayName)
        {
            _displayName = RemotePlayerCell.NormalizeDisplayName(_playerId, displayName);
            RefreshNameLabel();
        }

        public void MoveAuthoritative(Vector3 delta, float rotationY)
        {
            transform.position += delta;
            transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
        }

        public CreaturePlayerNetState CreateSnapshot()
        {
            return new CreaturePlayerNetState
            {
                PlayerId = _playerId,
                CreatureId = _creatureId ?? string.Empty,
                CreatureDNAJson = _sourceDnaJson ?? string.Empty,
                PosX = transform.position.x,
                PosY = transform.position.z,
                RotY = transform.eulerAngles.y,
                Health = Health,
                MaxHealth = MaxHealth,
                Stamina = Stamina,
                IsAlive = _isAlive
            };
        }

        public static string BuildStatusLabel(string displayName, float health, float maxHealth, float stamina, bool isAlive)
        {
            string name = RemotePlayerCell.NormalizeDisplayName(0, displayName);
            if (!isAlive)
                return $"{name}\nDown";

            return $"{name}\nHP {Mathf.Max(0f, health):0}/{Mathf.Max(1f, maxHealth):0} - STA {Mathf.Max(0f, stamina):0}";
        }

        private static CreatureDNA ResolveSnapshotDna(in CreaturePlayerNetState state, string displayName)
        {
            string normalizedDna = NormalizeDnaJson(state.CreatureDNAJson);
            if (!string.IsNullOrWhiteSpace(normalizedDna))
            {
                try
                {
                    var parsed = CreatureDNA.FromJson(normalizedDna);
                    if (GameManager.TryValidateCreatureDNA(parsed, out _))
                    {
                        parsed.CreatureId = NormalizeCreatureId(state);
                        if (string.IsNullOrWhiteSpace(parsed.CreatureName))
                            parsed.CreatureName = RemotePlayerCell.NormalizeDisplayName(state.PlayerId, displayName);
                        return parsed;
                    }
                }
                catch
                {
                    // Fall through to a starter-body fallback when a remote snapshot is malformed.
                }
            }

            var fallback = CreatureDNA.CreateDefault();
            fallback.CreatureId = NormalizeCreatureId(state);
            fallback.CreatureName = RemotePlayerCell.NormalizeDisplayName(state.PlayerId, displayName);
            return fallback;
        }

        private static string NormalizeCreatureId(in CreaturePlayerNetState state)
        {
            return string.IsNullOrWhiteSpace(state.CreatureId)
                ? $"remote_creature_{state.PlayerId}"
                : state.CreatureId.Trim();
        }

        private static string NormalizeDnaJson(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? string.Empty : json.Trim();
        }

        private static void DisableLocalSimulation(GameObject root)
        {
            var rb = root.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            foreach (var collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }

        private void EnsureNameLabel()
        {
            if (_nameLabel != null)
                return;

            var labelGo = new GameObject("CreatureUsernameLabel");
            labelGo.transform.SetParent(transform, false);
            _nameLabelRoot = labelGo.transform;

            _nameLabel = labelGo.AddComponent<TextMeshPro>();
            _nameLabel.fontSize = NameLabelFontSize;
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.color = Color.white;
            if (Application.isPlaying)
            {
                _nameLabel.outlineColor = new Color(0f, 0f, 0f, 0.86f);
                _nameLabel.outlineWidth = 0.22f;
            }
            _nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            _nameLabel.raycastTarget = false;
            _nameLabel.sortingOrder = 22;
            _nameLabel.rectTransform.sizeDelta = new Vector2(9f, 1.8f);
            PositionNameLabel();
            RefreshNameLabel();
        }

        private void RefreshNameLabel()
        {
            if (_nameLabel == null)
                return;

            _nameLabel.text = BuildStatusLabel(_displayName, _health, _maxHealth, _stamina, _isAlive);
        }

        private void PositionNameLabel()
        {
            if (_nameLabelRoot == null)
                return;

            float scaleY = Mathf.Max(0.01f, transform.localScale.y);
            _nameLabelRoot.localPosition = new Vector3(0f, NameLabelBaseHeight / scaleY, 0f);
            _nameLabelRoot.localScale = Vector3.one / Mathf.Max(0.01f, transform.localScale.x);
        }

        private void FaceNameLabelToCamera()
        {
            if (_nameLabelRoot == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
                return;

            Vector3 toCamera = _nameLabelRoot.position - cam.transform.position;
            if (toCamera.sqrMagnitude <= 0.0001f)
                return;

            _nameLabelRoot.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        private void Update()
        {
            float t = LerpSpeed * Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);
            PositionNameLabel();
        }

        private void LateUpdate()
        {
            FaceNameLabelToCamera();
        }
    }
}
