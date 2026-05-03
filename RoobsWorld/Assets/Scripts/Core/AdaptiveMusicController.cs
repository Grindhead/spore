using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Controls adaptive background music by stage and mood, crossfading between tracks and coordinating with ambient audio.
    /// </summary>
    public class AdaptiveMusicController : MonoBehaviour
    {
        private const float CROSSFADE_DURATION = 2f;
        private const float COMBAT_COOLDOWN = 8f;
        private const float VICTORY_COOLDOWN = 3.5f;
        private const float SOCIAL_COOLDOWN = 4f;

        private AudioSource _sourceA;
        private AudioSource _sourceB;
        private bool _aIsActive = true;

        private MusicTrackCatalog _catalog;
        private GameStageType _currentStage;
        private MusicMood _currentMood = MusicMood.Exploration;
        private string _currentTrackId;

        private float _combatTimer;
        private bool _inCombat;
        private float _temporaryMoodTimer;

        public MusicMood CurrentMood => _currentMood;
        public bool IsCombatActive => _inCombat;

        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            _catalog = new MusicTrackCatalog();

            _sourceA = CreateMusicSource("MusicA");
            _sourceB = CreateMusicSource("MusicB");
            _sourceB.volume = 0;

            EventBus.Subscribe<StageTransitionEvent>(OnStageTransition);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<StageTransitionEvent>(OnStageTransition);
        }

        private void Update()
        {
            if (_inCombat)
            {
                _combatTimer -= Time.deltaTime;
                if (_combatTimer <= 0)
                {
                    _inCombat = false;
                    _combatTimer = 0f;
                    if (_temporaryMoodTimer <= 0f)
                        ForceMood(MusicMood.Exploration);
                }
            }

            if (_temporaryMoodTimer > 0f)
            {
                _temporaryMoodTimer -= Time.deltaTime;
                if (_temporaryMoodTimer <= 0f)
                {
                    _temporaryMoodTimer = 0f;
                    if (!_inCombat && (_currentMood == MusicMood.Victory || _currentMood == MusicMood.Social))
                        ForceMood(MusicMood.Exploration);
                }
            }
        }

        public void RegisterClip(string trackId, AudioClip clip)
        {
            _clipCache[trackId] = clip;
        }

        public void SetStage(GameStageType stage)
        {
            _currentStage = stage;
            _currentMood = MusicMood.Exploration;
            PlayTrackForCurrentState();

            string ambientId = _catalog.GetAmbientId(stage);
            if (ambientId != null && AudioManager.HasInstance && _clipCache.TryGetValue(ambientId, out var ambClip))
                AudioManager.Instance.PlayAmbient(ambClip);
        }

        public void SetMood(MusicMood mood)
        {
            if (ShouldBlockMoodDowngrade(_currentMood, _temporaryMoodTimer, mood))
                return;

            if (mood == MusicMood.Combat)
            {
                NotifyCombat();
                return;
            }

            if (_currentMood == mood) return;
            ForceMood(mood);
        }

        public void NotifyCombat()
        {
            _inCombat = true;
            _combatTimer = COMBAT_COOLDOWN;
            _temporaryMoodTimer = 0f;
            if (_currentMood != MusicMood.Combat)
                ForceMood(MusicMood.Combat);
        }

        public void NotifyVictory()
        {
            _inCombat = false;
            _combatTimer = 0f;
            SetTemporaryMood(MusicMood.Victory, VICTORY_COOLDOWN);
        }

        public void NotifySocial()
        {
            if (!_inCombat)
                SetTemporaryMood(MusicMood.Social, SOCIAL_COOLDOWN);
        }

        private void SetTemporaryMood(MusicMood mood, float durationSeconds)
        {
            _temporaryMoodTimer = Mathf.Max(_temporaryMoodTimer, durationSeconds);
            ForceMood(mood);
        }

        private void ForceMood(MusicMood mood)
        {
            if (_currentMood == mood) return;
            _currentMood = mood;
            PlayTrackForCurrentState();
        }

        /// <summary>
        /// Blocks low-priority mood downgrades while a temporary mood (victory/social) is active.
        /// Allows equal-or-higher priority transitions (for example combat overriding victory).
        /// </summary>
        public static bool ShouldBlockMoodDowngrade(MusicMood currentMood, float temporaryMoodTimer, MusicMood requestedMood)
        {
            if (temporaryMoodTimer <= 0f) return false;
            int currentPriority = GetMoodPriority(currentMood);
            int requestedPriority = GetMoodPriority(requestedMood);
            return requestedPriority < currentPriority;
        }

        public static int GetMoodPriority(MusicMood mood)
        {
            switch (mood)
            {
                case MusicMood.Combat: return 4;
                case MusicMood.Tense: return 3;
                case MusicMood.Victory: return 2;
                case MusicMood.Social: return 1;
                default: return 0;
            }
        }

        private void PlayTrackForCurrentState()
        {
            string trackId = _catalog.GetStageTrack(_currentStage, _currentMood);
            if (trackId == null || trackId == _currentTrackId) return;

            _currentTrackId = trackId;

            if (!_clipCache.TryGetValue(trackId, out var clip))
            {
                Debug.Log($"[AdaptiveMusic] Track '{trackId}' not loaded, skipping");
                return;
            }

            StartCoroutine(CrossfadeTo(clip));
        }

        private IEnumerator CrossfadeTo(AudioClip clip)
        {
            var incoming = _aIsActive ? _sourceB : _sourceA;
            var outgoing = _aIsActive ? _sourceA : _sourceB;
            _aIsActive = !_aIsActive;

            incoming.clip = clip;
            incoming.Play();

            float maxVol = GetMusicVolume();
            float elapsed = 0f;

            while (elapsed < CROSSFADE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / CROSSFADE_DURATION;
                incoming.volume = Mathf.Lerp(0, maxVol, t);
                outgoing.volume = Mathf.Lerp(maxVol, 0, t);
                yield return null;
            }

            incoming.volume = maxVol;
            outgoing.volume = 0;
            outgoing.Stop();
        }

        private float GetMusicVolume()
        {
            if (!AudioManager.HasInstance) return 0.7f;
            return AudioManager.Instance.MasterVolume * AudioManager.Instance.MusicVolume;
        }

        private void OnStageTransition(StageTransitionEvent evt)
        {
            SetStage(evt.ToStage);
        }

        private AudioSource CreateMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            return source;
        }
    }
}
