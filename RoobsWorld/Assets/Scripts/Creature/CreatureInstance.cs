using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Runtime component representing a spawned creature, tracking health, stats, and death events.
    /// </summary>
    public class CreatureInstance : MonoBehaviour
    {
        private CreatureDNA _dna;
        private CreatureStats _stats;
        private bool _isPlayer;
        private float _currentHealth;
        private bool _isDead;
        private float _lastDamageTime = -999f;
        private float _invulnerableUntilTime = -1f;
        private float _nextLowHealthTipTime = -1f;

        public CreatureDNA DNA => _dna;
        public CreatureStats Stats => _stats;
        public bool IsPlayer => _isPlayer;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _stats.Health;
        public bool IsAlive => _currentHealth > 0;
        public bool IsInvulnerable => Time.time < _invulnerableUntilTime;

        /// <summary>
        /// Initializes this instance with immutable DNA, computed stats, and whether it represents the player.
        /// </summary>
        public void Initialize(CreatureDNA dna, CreatureStats stats, bool isPlayer)
        {
            _dna = dna;
            _stats = stats;
            _isPlayer = isPlayer;
            _currentHealth = stats.Health;
            _isDead = false;
            _lastDamageTime = -999f;
            _invulnerableUntilTime = -1f;
            _nextLowHealthTipTime = -1f;
        }

        /// <summary>
        /// Applies evolved DNA and stats to a living creature while preserving its current health ratio.
        /// </summary>
        public void ApplyEvolution(CreatureDNA dna, CreatureStats stats)
        {
            float previousMaxHealth = Mathf.Max(1f, _stats.Health);
            float healthRatio = Mathf.Clamp01(_currentHealth / previousMaxHealth);

            _dna = dna;
            _stats = stats;
            _currentHealth = Mathf.Clamp(stats.Health * healthRatio, 1f, stats.Health);
            _isDead = _currentHealth <= 0f;

            if (dna?.Body != null && dna.Body.BodyScale != Vector3.zero)
                transform.localScale = dna.Body.BodyScale;
        }

        /// <summary>
        /// Applies damage after defense mitigation, plays VFX, and publishes <see cref="CreatureDiedEvent"/> on death.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead || amount <= 0f)
                return;
            if (IsInvulnerable)
            {
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = CreatureCombatFeedbackType.Blocked,
                    TargetCreatureId = _dna?.CreatureId,
                    Position = transform.position,
                    IsPlayerTarget = _isPlayer
                });
                return;
            }

            float mitigated = Mathf.Max(0, amount - _stats.Defense * 0.5f);
            _currentHealth = Mathf.Max(0, _currentHealth - mitigated);

            if (mitigated > 0)
            {
                _lastDamageTime = Time.time;
                RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.DamageHit, transform.position);
                EventBus.Publish(new CreatureCombatFeedbackEvent
                {
                    Type = _isPlayer ? CreatureCombatFeedbackType.PlayerDamaged : CreatureCombatFeedbackType.Hit,
                    TargetCreatureId = _dna?.CreatureId,
                    Position = transform.position,
                    Amount = mitigated,
                    IsPlayerTarget = _isPlayer
                });

                if (_isPlayer)
                {
                    if (CameraManager.HasInstance)
                        CameraManager.Instance.RequestShake(0.1f, 0.09f);
                    if (InputManager.HasInstance)
                        InputManager.Instance.RequestHaptic(0.2f, 0.3f, 0.08f);

                    float hpRatio = _currentHealth / Mathf.Max(1f, _stats.Health);
                    if (hpRatio <= 0.3f && Time.time >= _nextLowHealthTipTime)
                    {
                        RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.player.low_health", 1.8f);
                        EventBus.Publish(new CreatureCombatFeedbackEvent
                        {
                            Type = CreatureCombatFeedbackType.LowHealth,
                            TargetCreatureId = _dna?.CreatureId,
                            Position = transform.position,
                            Amount = hpRatio,
                            IsPlayerTarget = true
                        });
                        _nextLowHealthTipTime = Time.time + 8f;
                    }
                }
            }

            if (_currentHealth <= 0)
                Die();
        }

        /// <summary>
        /// Restores health up to the maximum defined by <see cref="Stats"/>.
        /// </summary>
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(_stats.Health, _currentHealth + amount);
        }

        /// <summary>
        /// Grants temporary damage immunity for the given number of seconds.
        /// </summary>
        public void GrantInvulnerability(float durationSeconds)
        {
            float safeDuration = Mathf.Max(0f, durationSeconds);
            float until = Time.time + safeDuration;
            _invulnerableUntilTime = Mathf.Max(_invulnerableUntilTime, until);
        }

        /// <summary>
        /// Returns true when this creature took damage within the given time window.
        /// </summary>
        public bool WasDamagedWithin(float seconds)
        {
            float safeSeconds = Mathf.Max(0f, seconds);
            return Time.time - _lastDamageTime <= safeSeconds;
        }

        private void Die()
        {
            if (_isDead)
                return;

            _isDead = true;
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.DeathPoof, transform.position);
            EventBus.Publish(new CreatureDiedEvent
            {
                CreatureObject = gameObject,
                CreatureId = _dna.CreatureId,
                IsPlayer = _isPlayer
            });

            if (!_isPlayer)
                Destroy(gameObject, 1.25f);
        }
    }
}
