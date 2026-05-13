using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Centralized audio controller for music, ambient loops, and a small pool of SFX AudioSources.
    /// Register SFX clips via <see cref="RegisterSFXClip"/> (e.g. from <see cref="AudioClipLoader"/>) then play by <see cref="SFXType"/> with <see cref="PlaySFX(SFXType)"/>.
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        private const int SFX_SOURCE_POOL_SIZE = 16;

        private AudioSource _musicSource;
        private AudioSource _ambientSource;
        private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
        private readonly Dictionary<SFXType, AudioClip> _sfxClipCache = new Dictionary<SFXType, AudioClip>();

        private float _masterVolume = 1f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 1f;
        private float _ambientVolume = 1f;
        private string _preferredOutputDeviceName = string.Empty;

        public float MasterVolume
        {
            get => _masterVolume;
            set { _masterVolume = Mathf.Clamp01(value); UpdateVolumes(); }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set { _musicVolume = Mathf.Clamp01(value); UpdateVolumes(); }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set { _sfxVolume = Mathf.Clamp01(value); }
        }

        public float AmbientVolume
        {
            get => _ambientVolume;
            set { _ambientVolume = Mathf.Clamp01(value); UpdateVolumes(); }
        }

        public string PreferredOutputDeviceName
        {
            get => _preferredOutputDeviceName;
            set => _preferredOutputDeviceName = value ?? string.Empty;
        }

        protected override void OnInitialize()
        {
            _musicSource = CreateAudioSource("Music");
            _musicSource.loop = true;

            _ambientSource = CreateAudioSource("Ambient");
            _ambientSource.loop = true;

            for (int i = 0; i < SFX_SOURCE_POOL_SIZE; i++)
            {
                var source = CreateAudioSource($"SFX_{i}");
                _sfxPool.Enqueue(source);
            }
        }

        public void PlayMusic(AudioClip clip, float fadeTime = 1f)
        {
            if (_musicSource.clip == clip && _musicSource.isPlaying)
                return;

            _musicSource.clip = clip;
            _musicSource.volume = _masterVolume * _musicVolume;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        public void PlayAmbient(AudioClip clip)
        {
            _ambientSource.clip = clip;
            _ambientSource.volume = _masterVolume * _ambientVolume;
            _ambientSource.Play();
        }

        /// <summary>
        /// Registers an SFX clip for the given type. Called by <see cref="AudioClipLoader"/> when loading from Resources/SFX/.
        /// </summary>
        public void RegisterSFXClip(SFXType type, AudioClip clip)
        {
            if (clip != null)
                _sfxClipCache[type] = clip;
        }

        /// <summary>
        /// Plays the SFX associated with the given type if a clip has been registered; no-op if not loaded.
        /// </summary>
        public void PlaySFX(SFXType type, float volumeScale = 1f)
        {
            if (_sfxClipCache.TryGetValue(type, out var clip))
                PlaySFX(clip, volumeScale);
        }

        /// <summary>
        /// Plays a one-shot SFX clip using the next available pooled source.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source = GetAvailableSFXSource();
            source.PlayOneShot(clip, _masterVolume * _sfxVolume * volumeScale);
        }

        /// <summary>
        /// Plays a one-shot SFX at a world position (3D spatial blend). Does not use the pool.
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, _masterVolume * _sfxVolume * volumeScale);
        }

        /// <summary>
        /// Plays the SFX for the given type at a world position (3D spatial blend) if a clip has been registered.
        /// </summary>
        public void PlaySFXAtPosition(SFXType type, Vector3 position, float volumeScale = 1f)
        {
            if (_sfxClipCache.TryGetValue(type, out var clip))
                PlaySFXAtPosition(clip, position, volumeScale);
        }

        private AudioSource GetAvailableSFXSource()
        {
            var source = _sfxPool.Dequeue();
            _sfxPool.Enqueue(source);
            return source;
        }

        private AudioSource CreateAudioSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform);
            return go.AddComponent<AudioSource>();
        }

        private void UpdateVolumes()
        {
            if (_musicSource != null)
                _musicSource.volume = _masterVolume * _musicVolume;
            if (_ambientSource != null)
                _ambientSource.volume = _masterVolume * _ambientVolume;
        }
    }
}
