using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Audio feedback system that plays threat-related sound cues when predators are detected.
    /// Integrates with the ThreatIndicator to provide audio warnings.
    /// </summary>
    public class ThreatAudioFeedback : MonoBehaviour
    {
        [Header("Audio Feedback")]
        [SerializeField] private float _minThreatAlertInterval = 0.65f;
        [SerializeField] private float _maxThreatAlertInterval = 2.2f;
        [SerializeField] private float _minThreatAlertVolume = 0.2f;
        [SerializeField] private float _maxThreatAlertVolume = 0.7f;
        [SerializeField] private float _highThreatHapticThreshold = 0.75f;
        [SerializeField] private float _highThreatHapticInterval = 1.2f;
        [SerializeField] private float _musicUpdateInterval = 0.25f;
        [SerializeField] private float _tenseEnterThreshold = 0.22f;
        [SerializeField] private float _tenseExitThreshold = 0.12f;
        private const float MusicControllerLookupInterval = 1f;

        private ThreatIndicator _threatIndicator;
        private float _lastThreatAlertTime;
        private float _lastHighThreatHapticTime;
        private float _lastMusicUpdateTime;
        private float _nextMusicControllerLookupTime;
        private bool _wasThreatenedLastFrame;
        private bool _musicTenseActive;
        private AdaptiveMusicController _musicController;

        public static ThreatAudioFeedback CreateRuntime(ThreatIndicator threatIndicator)
        {
            var go = new GameObject("ThreatAudioFeedback");
            var feedback = go.AddComponent<ThreatAudioFeedback>();
            feedback._threatIndicator = threatIndicator;
            feedback._musicController = FindAnyObjectByType<AdaptiveMusicController>();
            return feedback;
        }

        private void Update()
        {
            if (_threatIndicator == null) return;

            float intensity = Mathf.Clamp01(_threatIndicator.ThreatIntensity);
            bool isThreatened = intensity > 0.05f;

            // Play alert sound when threat is first detected
            if (isThreatened && !_wasThreatenedLastFrame)
            {
                PlayThreatAlert(intensity);
                _lastThreatAlertTime = Time.time;
            }

            // Periodically play ambient threat sound while threatened
            if (isThreatened)
            {
                float alertInterval = ComputeAlertInterval(intensity, _minThreatAlertInterval, _maxThreatAlertInterval);
                if (Time.time - _lastThreatAlertTime >= alertInterval)
                {
                    PlayThreatAmbient(intensity);
                    _lastThreatAlertTime = Time.time;
                }
            }

            if (isThreatened && intensity >= _highThreatHapticThreshold)
            {
                if (Time.time - _lastHighThreatHapticTime >= _highThreatHapticInterval)
                {
                    _lastHighThreatHapticTime = Time.time;
                    InputManager.Instance?.RequestHaptic(
                        Mathf.Lerp(0.18f, 0.45f, intensity),
                        Mathf.Lerp(0.1f, 0.35f, intensity),
                        0.08f);
                }
            }

            if (CameraManager.HasInstance)
                CameraManager.Instance.SetThreatIntensity(intensity);

            UpdateAdaptiveMusic(intensity, isThreatened);

            _wasThreatenedLastFrame = isThreatened;
        }

        private void OnDisable()
        {
            ResetThreatState();
        }

        private void OnDestroy()
        {
            ResetThreatState();
        }

        private void ResetThreatState()
        {
            if (CameraManager.HasInstance)
                CameraManager.Instance.SetThreatIntensity(0f);

            if (_musicController != null &&
                ShouldRestoreExploration(_musicController.CurrentMood, _musicController.IsCombatActive, _musicTenseActive))
            {
                _musicController.SetMood(MusicMood.Exploration);
            }

            _musicTenseActive = false;
            _wasThreatenedLastFrame = false;
        }

        private void UpdateAdaptiveMusic(float intensity, bool isThreatened)
        {
            float now = Time.time;
            if (ShouldAttemptMusicControllerLookup(_musicController, now, _nextMusicControllerLookupTime))
            {
                _musicController = FindAnyObjectByType<AdaptiveMusicController>();
                _nextMusicControllerLookupTime = now + MusicControllerLookupInterval;
            }

            if (_musicController == null) return;

            _musicTenseActive = ComputeTenseState(
                _musicTenseActive,
                intensity,
                _tenseEnterThreshold,
                _tenseExitThreshold,
                isThreatened);

            if (now - _lastMusicUpdateTime < _musicUpdateInterval)
                return;
            _lastMusicUpdateTime = now;

            // Respect explicit combat handling from chase logic.
            if (_musicController.CurrentMood == MusicMood.Combat || _musicController.IsCombatActive)
                return;

            _musicController.SetMood(_musicTenseActive ? MusicMood.Tense : MusicMood.Exploration);
        }

        private void PlayThreatAlert(float intensity)
        {
            if (AudioManager.HasInstance)
            {
                float volume = ComputeAlertVolume(intensity, _minThreatAlertVolume, _maxThreatAlertVolume);
                AudioManager.Instance.PlaySFX(SFXType.ShieldHit, volume);
            }
        }

        private void PlayThreatAmbient(float intensity)
        {
            if (AudioManager.HasInstance)
            {
                float volume = ComputeAlertVolume(intensity, _minThreatAlertVolume * 0.7f, _maxThreatAlertVolume * 0.8f);
                AudioManager.Instance.PlaySFX(SFXType.AttackMiss, volume);
            }
        }

        /// <summary>Maps threat intensity to cadence: higher intensity means more frequent pulses.</summary>
        public static float ComputeAlertInterval(float threatIntensity, float minInterval, float maxInterval)
        {
            if (maxInterval < minInterval)
            {
                float swap = minInterval;
                minInterval = maxInterval;
                maxInterval = swap;
            }
            float t = Mathf.Clamp01(threatIntensity);
            return Mathf.Lerp(maxInterval, minInterval, t);
        }

        /// <summary>Maps threat intensity to volume while keeping output within configured bounds.</summary>
        public static float ComputeAlertVolume(float threatIntensity, float minVolume, float maxVolume)
        {
            if (maxVolume < minVolume)
            {
                float swap = minVolume;
                minVolume = maxVolume;
                maxVolume = swap;
            }
            float t = Mathf.Clamp01(threatIntensity);
            return Mathf.Lerp(minVolume, maxVolume, t);
        }

        /// <summary>
        /// Hysteresis for music tension: use separate thresholds for entering/exiting tense.
        /// </summary>
        public static bool ComputeTenseState(
            bool currentlyTense,
            float threatIntensity,
            float enterThreshold,
            float exitThreshold,
            bool isThreatened)
        {
            float intensity = Mathf.Clamp01(threatIntensity);
            if (!isThreatened || intensity <= Mathf.Min(enterThreshold, exitThreshold))
                return false;
            if (currentlyTense)
                return intensity > exitThreshold;
            return intensity >= enterThreshold;
        }

        /// <summary>
        /// Returns true when cleanup should restore exploration music.
        /// </summary>
        public static bool ShouldRestoreExploration(MusicMood currentMood, bool isCombatActive, bool tenseWasActive)
        {
            if (!tenseWasActive) return false;
            if (isCombatActive) return false;
            return currentMood == MusicMood.Tense;
        }

        public static bool ShouldAttemptMusicControllerLookup(AdaptiveMusicController musicController, float now, float nextLookupTime)
        {
            return musicController == null && now >= nextLookupTime;
        }
    }
}
