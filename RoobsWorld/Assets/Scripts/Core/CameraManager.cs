using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Types of camera rigs used by different evolutionary stages.
    /// </summary>
    public enum CameraRigType
    {
        Orthographic2D,
        ThirdPerson,
        Isometric,
        Strategic,
        FreeOrbit
    }

    /// <summary>
    /// Manages the main camera and switches between registered camera rigs (2D, third-person, isometric, strategic, free-orbit).
    /// </summary>
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private Camera _mainCamera;

        private readonly Dictionary<CameraRigType, CameraRigBase> _rigs = new Dictionary<CameraRigType, CameraRigBase>();
        private CameraRigBase _activeRig;
        private float _shakeUntil;
        private float _shakeStart;
        private float _shakeIntensity;
        private float _shakeSeed;
        private Vector3 _currentShakeOffset;
        private float _targetThreatIntensity;
        private float _smoothedThreatIntensity;
        private float _lastThreatPulseTime;

        [Header("Threat Camera Response")]
        [SerializeField] private float _maxThreatZoomOut = 0.8f;
        [SerializeField] private float _threatSmoothingSpeed = 4.5f;
        [SerializeField] private float _threatPulseThreshold = 0.78f;
        [SerializeField] private float _threatPulseCooldown = 1.1f;

        public Camera MainCamera => _mainCamera;
        public CameraRigType ActiveRigType => _activeRig?.RigType ?? CameraRigType.Orthographic2D;

        protected override void OnInitialize()
        {
            EnsureMainCamera();
            RegisterSceneRigs();
        }

        private static bool IsCameraRenderable(Camera camera)
        {
            return camera != null
                && camera.enabled
                && camera.gameObject.activeInHierarchy;
        }

        private void EnsureMainCamera()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (!IsCameraRenderable(_mainCamera))
            {
                var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (!IsCameraRenderable(cameras[i]))
                        continue;

                    _mainCamera = cameras[i];
                    break;
                }
            }

            if (!IsCameraRenderable(_mainCamera))
            {
                var cameraObj = new GameObject("Main Camera");
                _mainCamera = cameraObj.AddComponent<Camera>();
                _mainCamera.tag = "MainCamera"; // Ensure Camera.main finds it
                cameraObj.AddComponent<AudioListener>();
                if (Application.isPlaying)
                    DontDestroyOnLoad(cameraObj);
                Debug.Log("[CameraManager] Created persistent default camera for scene");
            }
        }

        public void RegisterRig(CameraRigBase rig)
        {
            _rigs[rig.RigType] = rig;
        }

        public void ActivateRig(CameraRigType rigType)
        {
            EnsureMainCamera();
            EnsureRigRegistered(rigType);

            if (_activeRig != null)
                _activeRig.Deactivate();

            if (_rigs.TryGetValue(rigType, out var rig))
            {
                _activeRig = rig;
                _activeRig.Activate(_mainCamera);
            }
            else
            {
                Debug.LogWarning($"[CameraManager] No rig registered for {rigType}");
            }
        }

        private void RegisterSceneRigs()
        {
            var rigs = FindObjectsByType<CameraRigBase>(FindObjectsInactive.Include);
            for (int i = 0; i < rigs.Length; i++)
            {
                if (rigs[i] != null)
                    RegisterRig(rigs[i]);
            }
        }

        private void EnsureRigRegistered(CameraRigType rigType)
        {
            if (_rigs.ContainsKey(rigType))
                return;

            RegisterSceneRigs();
            if (_rigs.ContainsKey(rigType))
                return;

            var createdRig = CreateRuntimeRig(rigType);
            if (createdRig != null)
            {
                RegisterRig(createdRig);
                Debug.Log($"[CameraManager] Auto-created runtime rig for {rigType}");
            }
        }

        private CameraRigBase CreateRuntimeRig(CameraRigType rigType)
        {
            switch (rigType)
            {
                case CameraRigType.Orthographic2D:
                    return gameObject.AddComponent<OrthographicCameraRig>();
                case CameraRigType.ThirdPerson:
                    return gameObject.AddComponent<ThirdPersonCameraRig>();
                case CameraRigType.Isometric:
                    return gameObject.AddComponent<OrthographicCameraRig>();
                case CameraRigType.Strategic:
                    return gameObject.AddComponent<OrthographicCameraRig>();
                case CameraRigType.FreeOrbit:
                    return gameObject.AddComponent<OrthographicCameraRig>();
                default:
                    return null;
            }
        }

        public void SetFollowTarget(Transform target)
        {
            _activeRig?.SetTarget(target);
        }

        /// <summary>Updates camera threat level (0..1). Higher values increase subtle zoom-out and pulse events.</summary>
        public void SetThreatIntensity(float intensity01)
        {
            _targetThreatIntensity = Mathf.Clamp01(intensity01);
        }

        /// <summary>Returns additional orthographic zoom-out from current smoothed threat intensity.</summary>
        public float GetThreatZoomOut()
        {
            return ComputeThreatZoomOut(_smoothedThreatIntensity, _maxThreatZoomOut);
        }

        /// <summary>Request a light screen shake (e.g. on eat, goal complete). Intensity in world units; duration in seconds.</summary>
        public void RequestShake(float intensity, float duration)
        {
            if (duration <= 0f || intensity <= 0f) return;
            float now = Time.time;
            bool idle = now >= _shakeUntil;
            if (idle)
            {
                _shakeStart = now;
                _shakeIntensity = intensity;
                _shakeUntil = now + duration;
                _shakeSeed = Random.value * 1000f;
                return;
            }

            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
            _shakeUntil = Mathf.Max(_shakeUntil, now + duration);
        }

        private void LateUpdate()
        {
            if (_mainCamera != null && _currentShakeOffset.sqrMagnitude > 0.000001f)
            {
                _mainCamera.transform.position -= _currentShakeOffset;
                _currentShakeOffset = Vector3.zero;
            }

            _smoothedThreatIntensity = Mathf.MoveTowards(
                _smoothedThreatIntensity,
                _targetThreatIntensity,
                Time.deltaTime * Mathf.Max(0.01f, _threatSmoothingSpeed));
            TryPulseThreatShake();

            _activeRig?.UpdateRig(Time.deltaTime);

            if (_mainCamera != null && Time.time < _shakeUntil)
            {
                float intensity = ComputeShakeIntensity(Time.time, _shakeStart, _shakeUntil, _shakeIntensity);
                float sampleTime = Time.unscaledTime * 31f;
                float noiseX = Mathf.PerlinNoise(_shakeSeed, sampleTime) * 2f - 1f;
                float noiseZ = Mathf.PerlinNoise(_shakeSeed + 19.37f, sampleTime + 7.11f) * 2f - 1f;
                _currentShakeOffset = new Vector3(
                    noiseX * intensity * 0.5f,
                    0f,
                    noiseZ * intensity * 0.5f
                );
                _mainCamera.transform.position += _currentShakeOffset;
            }
        }

        private void TryPulseThreatShake()
        {
            float elapsed = Time.time - _lastThreatPulseTime;
            if (!ShouldTriggerThreatPulse(_smoothedThreatIntensity, _threatPulseThreshold, elapsed, _threatPulseCooldown))
                return;

            _lastThreatPulseTime = Time.time;
            float t = Mathf.InverseLerp(_threatPulseThreshold, 1f, _smoothedThreatIntensity);
            float pulseIntensity = Mathf.Lerp(0.04f, 0.12f, t);
            float pulseDuration = Mathf.Lerp(0.06f, 0.13f, t);
            RequestShake(pulseIntensity, pulseDuration);
        }

        /// <summary>Returns shake amplitude at a given time using linear falloff over the active shake window.</summary>
        public static float ComputeShakeIntensity(float currentTime, float shakeStart, float shakeEnd, float baseIntensity)
        {
            float denom = Mathf.Max(0.0001f, shakeEnd - shakeStart);
            float t = Mathf.Clamp01((currentTime - shakeStart) / denom);
            return Mathf.Lerp(baseIntensity, 0f, t);
        }

        /// <summary>Computes additive zoom-out based on normalized threat and configured maximum.</summary>
        public static float ComputeThreatZoomOut(float threatIntensity, float maxZoomOut)
        {
            return Mathf.Clamp01(threatIntensity) * Mathf.Max(0f, maxZoomOut);
        }

        /// <summary>Returns true when a threat pulse shake should trigger based on threshold and cooldown.</summary>
        public static bool ShouldTriggerThreatPulse(float threatIntensity, float threshold, float elapsedSinceLastPulse, float cooldown)
        {
            return threatIntensity >= threshold && elapsedSinceLastPulse >= cooldown;
        }
    }

    /// <summary>
    /// Base class for stage-specific camera rigs controlled by <see cref="CameraManager"/>.
    /// </summary>
    public abstract class CameraRigBase : MonoBehaviour
    {
        public abstract CameraRigType RigType { get; }
        public abstract void Activate(Camera cam);
        public abstract void Deactivate();
        public abstract void SetTarget(Transform target);
        public abstract void UpdateRig(float deltaTime);
    }

    /// <summary>
    /// Simple orthographic follow camera used for top-down 2D views such as the Cell stage.
    /// </summary>
    public class OrthographicCameraRig : CameraRigBase
    {
        private const float EntityScreenScaleZoom = 0.82f;

        [Header("Classic Cell Follow")]
        [SerializeField] private float _followSpeed = 4.9f;
        [SerializeField] private float _defaultSize = 10.4f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 24f, 0f);
        [SerializeField] private float _lookPitch = 90f;
        [SerializeField] private float _rotationLerpSpeed = 4.2f;

        [Header("Lead / Zoom")]
        [SerializeField] private float _velocitySmoothing = 4.8f;
        [SerializeField] private float _leadPerSpeed = 0.12f;
        [SerializeField] private float _maxLeadDistance = 1.35f;
        [SerializeField] private float _speedZoomReference = 7.5f;
        [SerializeField] private float _speedZoomOut = 0.38f;
        [SerializeField] private float _massZoomOut = 1.2f;
        [SerializeField] private float _zoomLerpSpeed = 4.2f;

        [Header("Atmosphere")]
        [SerializeField] private float _driftAmplitude = 0.32f;
        [SerializeField] private float _driftFrequency = 0.22f;
        [SerializeField] private float _driftLerpSpeed = 1.8f;

        private Camera _camera;
        private Transform _target;
        private Vector3 _lastTargetPosition;
        private Vector3 _smoothedVelocity;
        private Vector3 _driftOffset;
        private bool _hasLastTargetSample;

        public override CameraRigType RigType => CameraRigType.Orthographic2D;

        public override void Activate(Camera cam)
        {
            _camera = cam;
            _camera.orthographic = true;
            _camera.orthographicSize = _defaultSize * EntityScreenScaleZoom;
            _camera.transform.rotation = Quaternion.Euler(_lookPitch, 0f, 0f);
            _smoothedVelocity = Vector3.zero;
            _driftOffset = Vector3.zero;
            _hasLastTargetSample = false;
        }

        public override void Deactivate() { }

        public override void SetTarget(Transform target) => _target = target;

        public override void UpdateRig(float deltaTime)
        {
            if (_target == null || _camera == null) return;

            if (!_hasLastTargetSample)
            {
                _lastTargetPosition = _target.position;
                _hasLastTargetSample = true;
            }

            float dt = Mathf.Max(deltaTime, 0.0001f);
            Vector3 rawVelocity = (_target.position - _lastTargetPosition) / dt;
            _lastTargetPosition = _target.position;
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, rawVelocity, 1f - Mathf.Exp(-_velocitySmoothing * dt));

            Vector3 lead = Vector3.zero;
            float speed = _smoothedVelocity.magnitude;
            float leadMultiplier = CellFeelDirector.CameraLeadMultiplier;
            if (speed > 0.05f)
            {
                float leadDistance = Mathf.Min(_maxLeadDistance * leadMultiplier, speed * _leadPerSpeed * leadMultiplier);
                lead = _smoothedVelocity.normalized * leadDistance;
            }

            float desiredSize = _defaultSize * CellFeelDirector.CameraZoomMultiplier * EntityScreenScaleZoom;
            desiredSize += Mathf.Clamp01(speed / Mathf.Max(0.01f, _speedZoomReference)) * _speedZoomOut;
            if (CameraManager.HasInstance)
                desiredSize += CameraManager.Instance.GetThreatZoomOut();

            var player = _target.GetComponent<RoobsWorld.Stages.Cell.PlayerCell>();
            if (player != null)
                desiredSize += Mathf.Clamp01(player.NormalizedMass) * _massZoomOut;

            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, desiredSize, 1f - Mathf.Exp(-_zoomLerpSpeed * dt));

            float phase = Time.time * _driftFrequency;
            var desiredDrift = new Vector3(
                Mathf.Sin(phase) * _driftAmplitude,
                0f,
                Mathf.Cos(phase * 0.83f) * _driftAmplitude * 0.75f
            );
            _driftOffset = Vector3.Lerp(_driftOffset, desiredDrift, 1f - Mathf.Exp(-_driftLerpSpeed * dt));

            var targetPos = _target.position + _offset + lead + _driftOffset;
            float followSpeed = _followSpeed * CellFeelDirector.CameraFollowMultiplier;
            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                targetPos,
                1f - Mathf.Exp(-followSpeed * dt)
            );

            _camera.transform.rotation = Quaternion.Slerp(
                _camera.transform.rotation,
                Quaternion.Euler(_lookPitch, 0f, 0f),
                1f - Mathf.Exp(-_rotationLerpSpeed * dt)
            );
        }

        public void SetZoom(float size)
        {
            if (_camera != null)
                _camera.orthographicSize = size;
        }
    }

    /// <summary>
    /// Over-the-shoulder follow camera that orbits the target using player look input.
    /// </summary>
    public class ThirdPersonCameraRig : CameraRigBase
    {
        [SerializeField] private float _distance = 8f;
        [SerializeField] private float _height = 4f;
        [SerializeField] private float _followSpeed = 8f;
        [SerializeField] private float _rotationSpeed = 120f;
        [SerializeField] private float _pitchSpeed = 70f;
        [SerializeField] private float _minPitch = 20f;
        [SerializeField] private float _maxPitch = 65f;

        private Camera _camera;
        private Transform _target;
        private float _currentAngle;
        private float _currentPitch = 35f;

        public override CameraRigType RigType => CameraRigType.ThirdPerson;

        public override void Activate(Camera cam)
        {
            _camera = cam;
            _camera.orthographic = false;
            _camera.fieldOfView = 60f;
        }

        public override void Deactivate() { }

        public override void SetTarget(Transform target) => _target = target;

        public override void UpdateRig(float deltaTime)
        {
            if (_target == null || _camera == null) return;

            Vector2 lookInput = InputManager.HasInstance ? InputManager.Instance.LookInput : Vector2.zero;
            lookInput = FilterMouseLookWithoutRightClick(lookInput);
            float sensitivity = 1f;
            bool invertY = false;
            if (SettingsManager.HasInstance && SettingsManager.Instance.Current != null)
            {
                sensitivity = SettingsManager.Instance.Current.CameraSensitivity;
                invertY = SettingsManager.Instance.Current.InvertY;
            }

            Vector2 lookDelta = ComputeLookDelta(
                lookInput,
                Mathf.Max(0.1f, sensitivity),
                invertY,
                _rotationSpeed,
                _pitchSpeed,
                deltaTime);

            float middleMousePitchDelta = 0f;
            if (Mouse.current != null && Mouse.current.middleButton.isPressed)
            {
                float pitchSign = invertY ? 1f : -1f;
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                middleMousePitchDelta = mouseDelta.y
                    * _pitchSpeed
                    * Mathf.Max(0.1f, sensitivity)
                    * pitchSign
                    * Mathf.Max(0f, deltaTime);
            }

            _currentAngle += lookDelta.x;
            _currentPitch = Mathf.Clamp(_currentPitch + lookDelta.y + middleMousePitchDelta, _minPitch, _maxPitch);

            float rad = _currentAngle * Mathf.Deg2Rad;
            float pitchRad = _currentPitch * Mathf.Deg2Rad;
            float horizontalDistance = Mathf.Cos(pitchRad) * _distance;
            var offset = new Vector3(
                Mathf.Sin(rad) * horizontalDistance,
                Mathf.Sin(pitchRad) * _distance + _height,
                Mathf.Cos(rad) * horizontalDistance
            );

            var targetPos = _target.position + offset;
            _camera.transform.position = Vector3.Lerp(
                _camera.transform.position,
                targetPos,
                _followSpeed * deltaTime
            );

            _camera.transform.LookAt(_target.position + Vector3.up * 1.5f);
        }

        private static Vector2 FilterMouseLookWithoutRightClick(Vector2 lookInput)
        {
            if (Mouse.current == null || Mouse.current.rightButton.isPressed)
                return lookInput;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            bool hasMouseMovement = mouseDelta.sqrMagnitude > 0.0001f;
            return hasMouseMovement ? Vector2.zero : lookInput;
        }

        public static Vector2 ComputeLookDelta(
            Vector2 lookInput,
            float sensitivity,
            bool invertY,
            float yawSpeed,
            float pitchSpeed,
            float deltaTime)
        {
            float safeSensitivity = Mathf.Max(0.1f, sensitivity);
            float safeDelta = Mathf.Max(0f, deltaTime);
            float pitchSign = invertY ? 1f : -1f;
            return new Vector2(
                lookInput.x * yawSpeed * safeSensitivity * safeDelta,
                lookInput.y * pitchSpeed * safeSensitivity * pitchSign * safeDelta);
        }
    }
}
