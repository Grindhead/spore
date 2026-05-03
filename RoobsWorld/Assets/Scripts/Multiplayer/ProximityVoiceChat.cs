using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Multiplayer
{
    /// <summary>
    /// Microphone capture and local-area playback for multiplayer sessions.
    /// Voice packets are relayed like normal gameplay packets, then attenuated by distance locally.
    /// </summary>
    public sealed class ProximityVoiceChat : MonoBehaviour
    {
        public const int   SampleRate = 16000;
        public const int   MaxSamplesPerPacket = 1024;
        public const float PacketInterval = 0.05f;
        public const float VoiceRadius = 14f;
        public const float FullVolumeRadius = 2.5f;

        private MultiplayerSessionManager _session;
        private INetworkTransport _transport;
        private Transform _localPlayer;
        private Func<int, Transform> _remoteTransformProvider;

        private string _microphoneDevice;
        private AudioClip _microphoneClip;
        private int _lastMicPosition;
        private float _sendTimer;
        private uint _sequence;
        private bool _captureStarted;

        private readonly Dictionary<int, VoicePlayback> _playbacks = new();

        public void Activate(
            MultiplayerSessionManager session,
            INetworkTransport transport,
            Transform localPlayer,
            Func<int, Transform> remoteTransformProvider)
        {
            _session = session;
            _transport = transport;
            _localPlayer = localPlayer;
            _remoteTransformProvider = remoteTransformProvider;

            if (_session == null || _transport == null || _session.State == SessionState.Offline)
                return;

            StartCapture();
        }

        public void Deactivate()
        {
            StopCapture();

            foreach (var playback in _playbacks.Values)
                playback.Dispose();
            _playbacks.Clear();

            _session = null;
            _transport = null;
            _localPlayer = null;
            _remoteTransformProvider = null;
        }

        public void Receive(in VoiceChatPacket packet)
        {
            if (_session == null || packet.PlayerId == _session.LocalPlayerId)
                return;
            if (packet.Pcm16 == null || packet.Pcm16.Length == 0 || packet.SampleRate <= 0)
                return;

            if (!_playbacks.TryGetValue(packet.PlayerId, out var playback))
            {
                var sourceObject = new GameObject($"Voice_Player_{packet.PlayerId}");
                sourceObject.transform.SetParent(transform, false);
                playback = new VoicePlayback(sourceObject, packet.SampleRate);
                _playbacks[packet.PlayerId] = playback;
            }

            playback.Enqueue(packet.Pcm16);
            UpdatePlaybackVolume(packet.PlayerId, playback);
        }

        private void Update()
        {
            if (_session == null || _transport == null || _session.State == SessionState.Offline)
                return;

            CaptureAndSend();

            foreach (var kv in _playbacks)
                UpdatePlaybackVolume(kv.Key, kv.Value);
        }

        private void StartCapture()
        {
            if (_captureStarted)
                return;

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[VoiceChat] No microphone device found.");
                return;
            }

            _microphoneDevice = Microphone.devices[0];
            _microphoneClip = Microphone.Start(_microphoneDevice, loop: true, lengthSec: 1, frequency: SampleRate);
            _lastMicPosition = 0;
            _captureStarted = _microphoneClip != null;
        }

        private void StopCapture()
        {
            if (!string.IsNullOrEmpty(_microphoneDevice) && Microphone.IsRecording(_microphoneDevice))
                Microphone.End(_microphoneDevice);

            _microphoneDevice = null;
            _microphoneClip = null;
            _lastMicPosition = 0;
            _captureStarted = false;
        }

        private void CaptureAndSend()
        {
            if (!_captureStarted || _microphoneClip == null)
                return;

            _sendTimer += Time.unscaledDeltaTime;
            if (_sendTimer < PacketInterval)
                return;
            _sendTimer = 0f;

            int micPosition = Microphone.GetPosition(_microphoneDevice);
            if (micPosition < 0 || micPosition == _lastMicPosition)
                return;

            int available = micPosition > _lastMicPosition
                ? micPosition - _lastMicPosition
                : (_microphoneClip.samples - _lastMicPosition) + micPosition;
            if (available <= 0)
                return;

            int count = Mathf.Min(available, MaxSamplesPerPacket);
            var samples = new float[count];
            ReadMicrophoneSamples(samples, count);

            var pcm = new short[count];
            for (int i = 0; i < count; i++)
                pcm[i] = FloatToPcm16(samples[i]);

            var packet = new VoiceChatPacket
            {
                PlayerId = _session.LocalPlayerId,
                Sequence = _sequence++,
                SampleRate = SampleRate,
                Pcm16 = pcm
            };

            _transport.Broadcast(NetworkTickManager.Prepend(
                NetworkTickManager.TagVoice,
                NetworkSerializer.Serialize(packet)));
        }

        private void ReadMicrophoneSamples(float[] destination, int count)
        {
            int clipSamples = _microphoneClip.samples;
            int firstCount = Mathf.Min(count, clipSamples - _lastMicPosition);
            if (firstCount == count)
            {
                _microphoneClip.GetData(destination, _lastMicPosition);
                _lastMicPosition = (_lastMicPosition + count) % clipSamples;
                return;
            }

            var first = new float[firstCount];
            _microphoneClip.GetData(first, _lastMicPosition);
            Array.Copy(first, 0, destination, 0, firstCount);

            var wrapped = new float[count - firstCount];
            _microphoneClip.GetData(wrapped, 0);
            Array.Copy(wrapped, 0, destination, firstCount, wrapped.Length);

            _lastMicPosition = (_lastMicPosition + count) % clipSamples;
        }

        private void UpdatePlaybackVolume(int playerId, VoicePlayback playback)
        {
            if (playback == null || _localPlayer == null)
                return;

            Transform remote = _remoteTransformProvider?.Invoke(playerId);
            if (remote == null)
            {
                playback.Volume = 0f;
                return;
            }

            float distance = Vector3.Distance(_localPlayer.position, remote.position);
            playback.Volume = CalculateVolume(distance);
        }

        public static float CalculateVolume(float distance)
        {
            if (distance <= FullVolumeRadius) return 1f;
            if (distance >= VoiceRadius) return 0f;
            return 1f - Mathf.InverseLerp(FullVolumeRadius, VoiceRadius, distance);
        }

        private static short FloatToPcm16(float sample)
        {
            sample = Mathf.Clamp(sample, -1f, 1f);
            return (short)Mathf.RoundToInt(sample * short.MaxValue);
        }

        private static float Pcm16ToFloat(short sample) => sample / 32768f;

        private sealed class VoicePlayback : IDisposable
        {
            private const int BufferSeconds = 2;

            private readonly Queue<float> _samples = new();
            private readonly object _lock = new();
            private readonly AudioSource _source;
            private readonly int _maxQueuedSamples;

            public float Volume
            {
                get => _source != null ? _source.volume : 0f;
                set { if (_source != null) _source.volume = Mathf.Clamp01(value); }
            }

            public VoicePlayback(GameObject owner, int sampleRate)
            {
                _maxQueuedSamples = Mathf.Max(sampleRate, sampleRate * BufferSeconds);
                _source = owner.AddComponent<AudioSource>();
                _source.loop = true;
                _source.playOnAwake = false;
                _source.spatialBlend = 0f;
                _source.clip = AudioClip.Create(
                    owner.name + "_Stream",
                    sampleRate * BufferSeconds,
                    1,
                    sampleRate,
                    stream: true,
                    OnAudioRead);
                _source.Play();
            }

            public void Enqueue(short[] pcm)
            {
                lock (_lock)
                {
                    for (int i = 0; i < pcm.Length; i++)
                        _samples.Enqueue(Pcm16ToFloat(pcm[i]));

                    while (_samples.Count > _maxQueuedSamples)
                        _samples.Dequeue();
                }
            }

            private void OnAudioRead(float[] data)
            {
                lock (_lock)
                {
                    for (int i = 0; i < data.Length; i++)
                        data[i] = _samples.Count > 0 ? _samples.Dequeue() : 0f;
                }
            }

            public void Dispose()
            {
                if (_source != null)
                    Destroy(_source.gameObject);
            }
        }
    }
}
