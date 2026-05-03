using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Adds a non-physics swimming layer to cell-stage actors: body roll/yaw and appendage flutter.
    /// </summary>
    [DisallowMultipleComponent]
    public class CellSwimVisual : MonoBehaviour
    {
        public enum SwimProfile
        {
            Player,
            Npc,
            Predator,
            Leviathan,
            Plankton,
            Pickup,
            Viral
        }

        private struct AnimatedChild
        {
            public Transform Transform;
            public Quaternion BaseRotation;
            public Vector3 BaseScale;
            public float PhaseOffset;
            public float Weight;
        }

        [Header("Body Stroke")]
        [SerializeField] private SwimProfile _profile = SwimProfile.Npc;
        [SerializeField] private float _strokeFrequency = 2.8f;
        [SerializeField] private float _rollDegrees = 5.5f;
        [SerializeField] private float _yawDegrees = 2.5f;
        [SerializeField] private float _pitchDegrees = 2.2f;
        [SerializeField] private float _idleIntensity = 0.35f;
        [SerializeField] private float _speedForFullStroke = 4f;
        [SerializeField] private float _velocityTurnLerp = 5.5f;
        [SerializeField] private bool _alignToVelocity = true;

        [Header("Appendages")]
        [SerializeField] private float _appendageDegrees = 16f;
        [SerializeField] private float _appendageScalePulse = 0.05f;

        private readonly List<AnimatedChild> _animatedChildren = new List<AnimatedChild>(16);
        private Rigidbody _rb;
        private Quaternion _baseLocalRotation;
        private Quaternion _lastAppliedRotation;
        private Vector3 _lastPosition;
        private float _seed;
        private int _cachedChildCount = -1;

        public static CellSwimVisual Ensure(GameObject target, SwimProfile profile)
        {
            if (target == null)
                return null;

            var visual = target.GetComponent<CellSwimVisual>();
            if (visual == null)
                visual = target.AddComponent<CellSwimVisual>();

            visual.Configure(profile);
            return visual;
        }

        public void Configure(SwimProfile profile)
        {
            _profile = profile;
            _rb = GetComponent<Rigidbody>();
            ApplyProfile(profile);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _seed = Random.Range(0f, 1000f);
            _baseLocalRotation = transform.localRotation;
            _lastAppliedRotation = _baseLocalRotation;
            _lastPosition = transform.position;
            RefreshAnimatedChildren(force: true);
            ApplyProfile(_profile);
        }

        private void OnEnable()
        {
            _baseLocalRotation = transform.localRotation;
            _lastAppliedRotation = _baseLocalRotation;
            _lastPosition = transform.position;
            RefreshAnimatedChildren(force: true);
        }

        private void LateUpdate()
        {
            RefreshAnimatedChildren(force: false);
            CaptureExternalRotation();

            Vector3 planarVelocity = ResolvePlanarVelocity();
            float speed01 = Mathf.Clamp01(planarVelocity.magnitude / Mathf.Max(0.01f, _speedForFullStroke));
            float intensity = Mathf.Lerp(_idleIntensity, 1f, speed01);

            if (_alignToVelocity && planarVelocity.sqrMagnitude > 0.0025f)
            {
                Quaternion velocityRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
                _baseLocalRotation = Quaternion.Slerp(
                    _baseLocalRotation,
                    velocityRotation,
                    1f - Mathf.Exp(-_velocityTurnLerp * Time.deltaTime));
            }

            float phase = Time.time * _strokeFrequency + _seed;
            float roll = Mathf.Sin(phase) * _rollDegrees * intensity;
            float yaw = Mathf.Sin(phase * 0.63f + 1.7f) * _yawDegrees * intensity;
            float pitch = Mathf.Cos(phase * 0.82f) * _pitchDegrees * intensity;

            _lastAppliedRotation = _baseLocalRotation * Quaternion.Euler(pitch, yaw, roll);
            transform.localRotation = _lastAppliedRotation;

            AnimateChildren(phase, intensity);
            _lastPosition = transform.position;
        }

        private void CaptureExternalRotation()
        {
            if (Quaternion.Angle(transform.localRotation, _lastAppliedRotation) > 0.35f)
                _baseLocalRotation = transform.localRotation;
        }

        private Vector3 ResolvePlanarVelocity()
        {
            Vector3 velocity = Vector3.zero;
            if (_rb != null)
                velocity = _rb.linearVelocity;
            else if (Time.deltaTime > 0f)
                velocity = (transform.position - _lastPosition) / Time.deltaTime;

            velocity.y = 0f;
            return velocity;
        }

        private void AnimateChildren(float phase, float intensity)
        {
            for (int i = 0; i < _animatedChildren.Count; i++)
            {
                var child = _animatedChildren[i];
                if (child.Transform == null)
                    continue;

                float childPhase = phase + child.PhaseOffset;
                float swing = Mathf.Sin(childPhase) * _appendageDegrees * child.Weight * intensity;
                float curl = Mathf.Cos(childPhase * 1.35f) * _appendageDegrees * 0.35f * child.Weight * intensity;
                child.Transform.localRotation = child.BaseRotation * Quaternion.Euler(swing, curl, 0f);

                float pulse = 1f + Mathf.Sin(childPhase * 1.7f) * _appendageScalePulse * child.Weight * intensity;
                child.Transform.localScale = new Vector3(
                    child.BaseScale.x,
                    child.BaseScale.y,
                    child.BaseScale.z * pulse);
            }
        }

        private void RefreshAnimatedChildren(bool force)
        {
            if (!force && _cachedChildCount == transform.childCount && ChildrenStillValid())
                return;

            _cachedChildCount = transform.childCount;
            _animatedChildren.Clear();
            AddAnimatedChildren(transform);
        }

        private bool ChildrenStillValid()
        {
            for (int i = 0; i < _animatedChildren.Count; i++)
            {
                if (_animatedChildren[i].Transform == null)
                    return false;
            }

            return true;
        }

        private void AddAnimatedChildren(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                float weight = GetAppendageWeight(child.name);
                if (weight > 0f)
                {
                    _animatedChildren.Add(new AnimatedChild
                    {
                        Transform = child,
                        BaseRotation = child.localRotation,
                        BaseScale = child.localScale,
                        PhaseOffset = i * 0.73f + child.GetSiblingIndex() * 0.19f,
                        Weight = weight
                    });
                }

                AddAnimatedChildren(child);
            }
        }

        private static float GetAppendageWeight(string childName)
        {
            if (string.IsNullOrEmpty(childName))
                return 0f;

            if (childName.StartsWith("Flagella_") || childName.StartsWith("Loadout_Flagella"))
                return 1f;
            if (childName.StartsWith("Style_Tendril_") || childName.StartsWith("Loadout_Cilia"))
                return 0.75f;
            if (childName.StartsWith("Loadout_Jet"))
                return 0.55f;
            if (childName.StartsWith("CellCrest_"))
                return 0.35f;

            return 0f;
        }

        private void ApplyProfile(SwimProfile profile)
        {
            switch (profile)
            {
                case SwimProfile.Player:
                    SetTuning(3.6f, 4.5f, 2.4f, 2.6f, 0.28f, 5.2f, 7.5f, true, 18f, 0.045f);
                    break;
                case SwimProfile.Predator:
                    SetTuning(2.45f, 4.2f, 3.4f, 2f, 0.22f, 5.4f, 6.5f, true, 12f, 0.035f);
                    break;
                case SwimProfile.Leviathan:
                    SetTuning(1.15f, 2.2f, 1.7f, 1.2f, 0.18f, 7.5f, 3.2f, true, 8f, 0.025f);
                    break;
                case SwimProfile.Plankton:
                    SetTuning(1.55f, 10f, 8f, 5f, 0.85f, 1f, 1f, false, 4f, 0.02f);
                    break;
                case SwimProfile.Pickup:
                    SetTuning(1.25f, 7f, 10f, 4f, 0.75f, 1f, 1f, false, 5f, 0.02f);
                    break;
                case SwimProfile.Viral:
                    SetTuning(5.2f, 9f, 7f, 4f, 0.45f, 3.5f, 8f, true, 20f, 0.06f);
                    break;
                default:
                    SetTuning(2.85f, 5.8f, 2.8f, 2.4f, 0.35f, 4f, 5.5f, true, 16f, 0.05f);
                    break;
            }
        }

        private void SetTuning(
            float strokeFrequency,
            float rollDegrees,
            float yawDegrees,
            float pitchDegrees,
            float idleIntensity,
            float speedForFullStroke,
            float velocityTurnLerp,
            bool alignToVelocity,
            float appendageDegrees,
            float appendageScalePulse)
        {
            _strokeFrequency = strokeFrequency;
            _rollDegrees = rollDegrees;
            _yawDegrees = yawDegrees;
            _pitchDegrees = pitchDegrees;
            _idleIntensity = idleIntensity;
            _speedForFullStroke = speedForFullStroke;
            _velocityTurnLerp = velocityTurnLerp;
            _alignToVelocity = alignToVelocity;
            _appendageDegrees = appendageDegrees;
            _appendageScalePulse = appendageScalePulse;
        }
    }
}
