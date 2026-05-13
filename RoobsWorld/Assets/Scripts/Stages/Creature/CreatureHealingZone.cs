using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public class CreatureHealingZone : MonoBehaviour
    {
        [SerializeField] private float _healPerSecond = 5f;
        [SerializeField] private float _pulseFrequency = 2.4f;
        [SerializeField] private float _pulseScale = 0.12f;

        private readonly HashSet<CreatureInstance> _insideCreatures = new HashSet<CreatureInstance>();
        private Renderer _renderer;
        private Material _material;
        private Color _baseColor;
        private Vector3 _baseScale;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _baseScale = transform.localScale;
            if (_renderer != null && _renderer.material != null)
            {
                _material = _renderer.material;
                _baseColor = _material.color;
            }
            else
            {
                _baseColor = new Color(0.18f, 0.55f, 0.42f, 0.86f);
            }
        }

        private void Update()
        {
            float pulse = ComputePulseAlpha(Time.time, _pulseFrequency);
            Vector3 scale = _baseScale == Vector3.zero ? Vector3.one : _baseScale;
            transform.localScale = new Vector3(
                scale.x * (1f + pulse * _pulseScale),
                scale.y,
                scale.z * (1f + pulse * _pulseScale));

            if (_material != null)
                _material.color = Color.Lerp(_baseColor, Color.white, pulse * 0.28f);
        }

        private void OnTriggerEnter(Collider other)
        {
            var creature = other != null ? other.GetComponentInParent<CreatureInstance>() : null;
            if (creature == null || !creature.IsAlive)
                return;

            if (!_insideCreatures.Add(creature))
                return;

            EventBus.Publish(new CreatureHealingZoneEvent
            {
                Started = true,
                Position = transform.position,
                IsPlayer = creature.IsPlayer
            });
        }

        private void OnTriggerStay(Collider other)
        {
            var creature = other != null ? other.GetComponentInParent<CreatureInstance>() : null;
            if (creature == null || !creature.IsAlive)
                return;

            creature.Heal(Mathf.Max(0f, _healPerSecond) * Time.deltaTime);
        }

        private void OnTriggerExit(Collider other)
        {
            var creature = other != null ? other.GetComponentInParent<CreatureInstance>() : null;
            if (creature == null)
                return;

            if (!_insideCreatures.Remove(creature))
                return;

            EventBus.Publish(new CreatureHealingZoneEvent
            {
                Started = false,
                Position = transform.position,
                IsPlayer = creature.IsPlayer
            });
        }

        public static float ComputePulseAlpha(float time, float frequency)
        {
            float safeFrequency = Mathf.Max(0.1f, frequency);
            return (Mathf.Sin(Mathf.Max(0f, time) * safeFrequency) + 1f) * 0.5f;
        }
    }
}
