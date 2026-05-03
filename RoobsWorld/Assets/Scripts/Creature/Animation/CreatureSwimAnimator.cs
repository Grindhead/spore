using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.Animation
{
    /// <summary>
    /// Adds a procedural swimming motion to generated creature visuals without moving gameplay colliders.
    /// </summary>
    public class CreatureSwimAnimator : MonoBehaviour
    {
        [Header("Body")]
        [SerializeField] private float _idleFrequency = 0.9f;
        [SerializeField] private float _moveFrequency = 2.1f;
        [SerializeField] private float _bodyWaveDegrees = 8f;
        [SerializeField] private float _bodyRollDegrees = 3f;
        [SerializeField] private float _bodyBobAmplitude = 0.06f;

        [Header("Appendages")]
        [SerializeField] private float _appendageStrokeDegrees = 18f;
        [SerializeField] private float _tailStrokeDegrees = 24f;
        [SerializeField] private float _finStrokeDegrees = 22f;

        private readonly List<AnimatedTransform> _spineSegments = new();
        private readonly List<AnimatedPart> _parts = new();
        private Transform _bodyRoot;
        private Transform _partsRoot;
        private Vector3 _lastPosition;
        private Vector3 _bodyRootLocalPosition;
        private float _clock;
        private float _seed;

        public void Initialize(CreatureDNA dna)
        {
            _spineSegments.Clear();
            _parts.Clear();

            _bodyRoot = transform.Find("Body");
            _partsRoot = transform.Find("Parts");

            if (_bodyRoot != null)
            {
                _bodyRootLocalPosition = _bodyRoot.localPosition;
                for (int i = 0; i < _bodyRoot.childCount; i++)
                    _spineSegments.Add(new AnimatedTransform(_bodyRoot.GetChild(i), i));
            }

            if (_partsRoot != null && dna?.Parts != null)
            {
                int count = Mathf.Min(_partsRoot.childCount, dna.Parts.Count);
                for (int i = 0; i < count; i++)
                    _parts.Add(new AnimatedPart(_partsRoot.GetChild(i), dna.Parts[i], i));
            }

            _lastPosition = transform.position;
            _seed = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            Vector3 planarDelta = transform.position - _lastPosition;
            planarDelta.y = 0f;

            float speed = planarDelta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            float movementBlend = Mathf.Clamp01(speed / 2f);
            float frequency = Mathf.Lerp(_idleFrequency, _moveFrequency, movementBlend);

            _clock += Time.deltaTime * frequency;
            float phase = _clock * Mathf.PI * 2f + _seed;

            AnimateBody(phase, movementBlend);
            AnimateParts(phase, movementBlend);

            _lastPosition = transform.position;
        }

        private void AnimateBody(float phase, float movementBlend)
        {
            float amplitude = Mathf.Lerp(0.45f, 1f, movementBlend);

            if (_bodyRoot != null)
            {
                float bob = Mathf.Sin(phase * 0.5f) * _bodyBobAmplitude * amplitude;
                _bodyRoot.localPosition = _bodyRootLocalPosition + Vector3.up * bob;
            }

            int segmentCount = Mathf.Max(1, _spineSegments.Count - 1);
            foreach (var segment in _spineSegments)
            {
                float segmentPhase = phase - segment.Index * 0.55f;
                float segmentWeight = segment.Index / (float)segmentCount;
                float yaw = Mathf.Sin(segmentPhase) * _bodyWaveDegrees * amplitude * Mathf.Lerp(0.55f, 1f, segmentWeight);
                float roll = Mathf.Cos(segmentPhase * 0.8f) * _bodyRollDegrees * amplitude;
                segment.Transform.localRotation = segment.LocalRotation * Quaternion.Euler(0f, yaw, roll);
            }
        }

        private void AnimateParts(float phase, float movementBlend)
        {
            float amplitude = Mathf.Lerp(0.55f, 1f, movementBlend);

            foreach (var part in _parts)
            {
                float sidePhase = part.IsLeftSide ? Mathf.PI : 0f;
                float stroke = Mathf.Sin(phase * 1.35f + sidePhase + part.Index * 0.2f);
                float flutter = Mathf.Cos(phase * 2.1f + part.Index * 0.4f);
                Vector3 euler = Vector3.zero;

                if (part.Kind == SwimPartKind.Tail)
                    euler.y = stroke * _tailStrokeDegrees * amplitude;
                else if (part.Kind == SwimPartKind.Fin || part.Kind == SwimPartKind.Wing)
                    euler.z = stroke * _finStrokeDegrees * amplitude;
                else if (part.Kind == SwimPartKind.Leg || part.Kind == SwimPartKind.Arm)
                {
                    euler.x = stroke * _appendageStrokeDegrees * amplitude;
                    euler.z = flutter * _appendageStrokeDegrees * 0.35f * amplitude;
                }
                else
                {
                    euler.x = flutter * 2f * amplitude;
                }

                part.Transform.localRotation = part.LocalRotation * Quaternion.Euler(euler);
            }
        }

        private readonly struct AnimatedTransform
        {
            public readonly Transform Transform;
            public readonly Quaternion LocalRotation;
            public readonly int Index;

            public AnimatedTransform(Transform transform, int index)
            {
                Transform = transform;
                LocalRotation = transform.localRotation;
                Index = index;
            }
        }

        private readonly struct AnimatedPart
        {
            public readonly Transform Transform;
            public readonly Quaternion LocalRotation;
            public readonly int Index;
            public readonly bool IsLeftSide;
            public readonly SwimPartKind Kind;

            public AnimatedPart(Transform transform, PartPlacement placement, int index)
            {
                Transform = transform;
                LocalRotation = transform.localRotation;
                Index = index;
                IsLeftSide = placement.Offset.x < 0f;
                Kind = GetKind(placement.PartId);
            }

            private static SwimPartKind GetKind(string partId)
            {
                if (string.IsNullOrEmpty(partId))
                    return SwimPartKind.Other;
                if (partId.StartsWith("tail"))
                    return SwimPartKind.Tail;
                if (partId.StartsWith("fin"))
                    return SwimPartKind.Fin;
                if (partId.StartsWith("wing"))
                    return SwimPartKind.Wing;
                if (partId.StartsWith("leg"))
                    return SwimPartKind.Leg;
                if (partId.StartsWith("arm"))
                    return SwimPartKind.Arm;
                return SwimPartKind.Other;
            }
        }

        private enum SwimPartKind
        {
            Other,
            Leg,
            Arm,
            Fin,
            Wing,
            Tail
        }
    }
}
