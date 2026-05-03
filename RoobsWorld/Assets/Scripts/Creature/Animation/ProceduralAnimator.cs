using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.Animation
{
    /// <summary>
    /// Drives limb IK rigs and subtle body motion to produce procedural walking gaits for creatures.
    /// </summary>
    public class ProceduralAnimator : MonoBehaviour
    {
        [Header("Gait")]
        [SerializeField] private float _stepHeight = 0.3f;
        [SerializeField] private float _strideDuration = 0.5f;
        [SerializeField] private float _strideLength = 1.2f;
        [SerializeField] private float _bodyBobAmplitude = 0.04f;
        [SerializeField] private float _bodyBobFrequency = 2f;

        [Header("Ground Detection")]
        [SerializeField] private float _raycastHeight = 2f;
        [SerializeField] private float _raycastDistance = 4f;
        [SerializeField] private LayerMask _groundLayer = ~0;

        private readonly List<LimbIKRig> _limbs = new();
        private GaitData _gait;
        private float _gaitClock;
        private Vector3 _lastBodyPos;
        private float _bodyYOffset;

        public IReadOnlyList<LimbIKRig> Limbs => _limbs;

        public void Initialize(LimbConfig[] limbConfigs, Transform[][] limbJointChains)
        {
            _gait = GaitGenerator.Generate(limbConfigs);
            _gait.StepHeight = _stepHeight;
            _gait.StrideDuration = _strideDuration;
            _gait.StrideLength = _strideLength;

            _limbs.Clear();
            for (int i = 0; i < limbConfigs.Length; i++)
            {
                var rig = new LimbIKRig
                {
                    Joints = limbJointChains[i],
                    PhaseOffset = _gait.LimbPhases[i],
                    FootRestPosition = limbJointChains[i][^1].position,
                    FootTarget = limbJointChains[i][^1].position,
                    IsStepping = false,
                    StepProgress = 0f
                };
                _limbs.Add(rig);
            }

            _lastBodyPos = transform.position;
        }

        private void Update()
        {
            if (_limbs.Count == 0) return;

            Vector3 velocity = (transform.position - _lastBodyPos) / Mathf.Max(Time.deltaTime, 0.001f);
            float speed = new Vector2(velocity.x, velocity.z).magnitude;
            bool isMoving = speed > 0.1f;

            if (isMoving)
                _gaitClock += Time.deltaTime / _gait.StrideDuration;

            UpdateBodyBob(isMoving, speed);
            UpdateLimbs(isMoving, speed);

            _lastBodyPos = transform.position;
        }

        private void UpdateBodyBob(bool isMoving, float speed)
        {
            if (isMoving)
            {
                float bob = Mathf.Sin(_gaitClock * Mathf.PI * 2f * _bodyBobFrequency) * _bodyBobAmplitude;
                _bodyYOffset = Mathf.Lerp(_bodyYOffset, bob, Time.deltaTime * 10f);
            }
            else
            {
                _bodyYOffset = Mathf.Lerp(_bodyYOffset, 0f, Time.deltaTime * 5f);
            }
        }

        private void UpdateLimbs(bool isMoving, float speed)
        {
            for (int i = 0; i < _limbs.Count; i++)
            {
                var limb = _limbs[i];
                Vector3 desiredFootPos = GetGroundPosition(
                    transform.TransformPoint(
                        transform.InverseTransformPoint(limb.FootRestPosition)));

                float distFromRest = Vector3.Distance(limb.FootTarget, desiredFootPos);
                float phase = (_gaitClock + limb.PhaseOffset) % 1f;

                if (isMoving && !limb.IsStepping && distFromRest > _gait.StrideLength * 0.5f && phase < 0.1f)
                {
                    limb.IsStepping = true;
                    limb.StepProgress = 0f;
                    limb.StepOrigin = limb.FootTarget;
                    limb.StepDestination = desiredFootPos;
                }

                if (limb.IsStepping)
                {
                    limb.StepProgress += Time.deltaTime / (_gait.StrideDuration * 0.5f);
                    if (limb.StepProgress >= 1f)
                    {
                        limb.StepProgress = 1f;
                        limb.IsStepping = false;
                        limb.FootTarget = limb.StepDestination;
                    }
                    else
                    {
                        float t = limb.StepProgress;
                        float smoothT = t * t * (3f - 2f * t);
                        Vector3 lerped = Vector3.Lerp(limb.StepOrigin, limb.StepDestination, smoothT);
                        float arc = Mathf.Sin(smoothT * Mathf.PI) * _gait.StepHeight;
                        limb.FootTarget = lerped + Vector3.up * arc;
                    }
                }

                limb.UpdateIK();
            }
        }

        private Vector3 GetGroundPosition(Vector3 worldPos)
        {
            Vector3 rayOrigin = worldPos + Vector3.up * _raycastHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, _raycastDistance, _groundLayer))
                return hit.point;
            return new Vector3(worldPos.x, transform.position.y, worldPos.z);
        }

        public float GetBodyYOffset() => _bodyYOffset;
    }
}
