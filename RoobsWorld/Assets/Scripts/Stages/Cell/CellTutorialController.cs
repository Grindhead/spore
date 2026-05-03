using RoobsWorld.Core;
using RoobsWorld.UI;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Fires one-off contextual tutorial tips in response to key cell-stage events.
    /// Each tip shows at most once per run (flags reset when the controller is destroyed on stage exit).
    /// Attach to the CellStage GameObject; call <see cref="Activate"/> / <see cref="Deactivate"/> from CellStage.
    /// </summary>
    public class CellTutorialController : MonoBehaviour
    {
        private bool _shownFirstEat;
        private bool _shownEvolution;
        private bool _shownReproduction;
        private bool _shownSpeciation;
        private bool _shownFirstExtinction;
        private bool _shownTenSpecies;
        private bool _shownPredatorWarning;
        private bool _shownFirstPart;
        private bool _active;

        private PlayerCell _player;
        private AdaptiveMusicController _musicController;
        private float _predatorScanInterval = 2f;
        private float _predatorScanTimer;
        private float _proximityHapticCooldown;
        private const float ProximityHapticInterval = 3f;
        private const float ProximityHapticRadius   = 10f;
        private readonly List<PredatorCell> _predatorBuffer = new List<PredatorCell>(16);

        public void Activate(PlayerCell player)
        {
            _player = player;
            _active = true;
            _predatorScanTimer = _predatorScanInterval;
            _musicController = FindAnyObjectByType<AdaptiveMusicController>();

            EventBus.Subscribe<CellFoodConsumedEvent>(OnFoodConsumed);
            EventBus.Subscribe<CellEvolutionTriggeredEvent>(OnEvolutionTriggered);
            EventBus.Subscribe<CellPlayerReproducedEvent>(OnPlayerReproduced);
            EventBus.Subscribe<CellNewSpeciesEvent>(OnNewSpecies);
            EventBus.Subscribe<CellSpeciesExtinctEvent>(OnSpeciesExtinct);
            EventBus.Subscribe<CellGoalCompletedEvent>(OnGoalCompleted);
            EventBus.Subscribe<CellPartCollectedEvent>(OnPartCollected);
        }

        public void Deactivate()
        {
            _active = false;
            EventBus.Unsubscribe<CellFoodConsumedEvent>(OnFoodConsumed);
            EventBus.Unsubscribe<CellEvolutionTriggeredEvent>(OnEvolutionTriggered);
            EventBus.Unsubscribe<CellPlayerReproducedEvent>(OnPlayerReproduced);
            EventBus.Unsubscribe<CellNewSpeciesEvent>(OnNewSpecies);
            EventBus.Unsubscribe<CellSpeciesExtinctEvent>(OnSpeciesExtinct);
            EventBus.Unsubscribe<CellGoalCompletedEvent>(OnGoalCompleted);
            EventBus.Unsubscribe<CellPartCollectedEvent>(OnPartCollected);
        }

        private void Update()
        {
            if (!_active || _player == null) return;

            _predatorScanTimer -= Time.deltaTime;
            _proximityHapticCooldown -= Time.deltaTime;

            if (_predatorScanTimer <= 0f)
            {
                _predatorScanTimer = _predatorScanInterval;
                ScanPredators();
            }
        }

        private void ScanPredators()
        {
            const float warnRadius = 20f;
            float warnRadiusSqr = warnRadius * warnRadius;
            float proximityRadiusSqr = ProximityHapticRadius * ProximityHapticRadius;
            FillActivePredatorBuffer();
            bool anyChasing = false;
            bool anyNearby = false;

            for (int i = 0; i < _predatorBuffer.Count; i++)
            {
                var p = _predatorBuffer[i];
                if (p == null) continue;
                float distSqr = (p.transform.position - _player.transform.position).sqrMagnitude;

                if (distSqr < warnRadiusSqr)
                {
                    anyNearby = true;
                    if (p.IsChasing) anyChasing = true;

                    // One-off tutorial tip on first close encounter
                    if (!_shownPredatorWarning)
                    {
                        _shownPredatorWarning = true;
                        ShowTip("tutorial.cell.tip.predator_warning", 4f);
                    }

                    // Repeating proximity haptic when a predator is very close
                    if (ShouldTriggerProximityPulse(_proximityHapticCooldown, distSqr, proximityRadiusSqr))
                    {
                        _proximityHapticCooldown = ProximityHapticInterval;
                        InputManager.Instance?.RequestHaptic(0.35f, 0.15f, 0.12f);
                    }
                }
            }

            // Adaptive music: combat when chased, tense when nearby, exploration when clear.
            if (_musicController != null)
            {
                MusicMood mood = ResolvePredatorMood(anyChasing, anyNearby);
                if (mood == MusicMood.Combat)
                    _musicController.NotifyCombat();
                else
                    _musicController.SetMood(mood);
            }
        }

        private void FillActivePredatorBuffer()
        {
            _predatorBuffer.Clear();
            int fromRegistry = PredatorPackCoordinator.CopyRegisteredPredators(_predatorBuffer);
            if (fromRegistry > 0)
                return;

            var predators = FindObjectsByType<PredatorCell>(FindObjectsInactive.Exclude);
            for (int i = 0; i < predators.Length; i++)
            {
                if (predators[i] != null)
                    _predatorBuffer.Add(predators[i]);
            }
        }

        /// <summary>
        /// Determines ambient mood from predator state.
        /// Chasing has top priority, then proximity warning, else exploration.
        /// </summary>
        public static MusicMood ResolvePredatorMood(bool anyChasing, bool anyNearby)
        {
            if (anyChasing) return MusicMood.Combat;
            return anyNearby ? MusicMood.Tense : MusicMood.Exploration;
        }

        /// <summary>
        /// Returns true when predator proximity haptic should trigger based on cooldown and distance.
        /// </summary>
        public static bool ShouldTriggerProximityPulse(
            float cooldownRemaining,
            float distanceSqr,
            float proximityRadiusSqr)
        {
            if (cooldownRemaining > 0f) return false;
            if (proximityRadiusSqr <= 0f) return false;
            return distanceSqr < proximityRadiusSqr;
        }

        private void OnFoodConsumed(CellFoodConsumedEvent evt)
        {
            if (_shownFirstEat) return;
            _shownFirstEat = true;
            ShowTip("tutorial.cell.tip.first_eat", 4f);
        }

        private void OnEvolutionTriggered(CellEvolutionTriggeredEvent evt)
        {
            if (_shownEvolution) return;
            _shownEvolution = true;
            ShowTip("tutorial.cell.tip.first_evolution", 5f);
        }

        private void OnPlayerReproduced(CellPlayerReproducedEvent evt)
        {
            if (_shownReproduction) return;
            _shownReproduction = true;
            ShowTip("tutorial.cell.tip.first_reproduction", 5f);
        }

        private void OnNewSpecies(CellNewSpeciesEvent evt)
        {
            if (!_shownSpeciation)
            {
                _shownSpeciation = true;
                ShowTip("tutorial.cell.tip.first_speciation", 4f);
            }

            if (!_shownTenSpecies && SpeciationSystem.GetAllSpecies().Count >= 10)
            {
                _shownTenSpecies = true;
                ShowTip("tutorial.cell.tip.ten_species", 5f);
            }
        }

        private void OnSpeciesExtinct(CellSpeciesExtinctEvent evt)
        {
            if (_shownFirstExtinction) return;
            _shownFirstExtinction = true;
            ShowTip("tutorial.cell.tip.first_extinction", 5f);
        }

        private void OnGoalCompleted(CellGoalCompletedEvent evt)
        {
            if (_musicController == null)
                _musicController = FindAnyObjectByType<AdaptiveMusicController>();

            if (_musicController == null)
                return;

            _musicController.NotifyVictory();
        }

        private void OnPartCollected(CellPartCollectedEvent evt)
        {
            if (_shownFirstPart) return;
            _shownFirstPart = true;
            ShowTip("tutorial.cell.tip.first_part", 4f);
        }

        private static void ShowTip(string key, float duration)
        {
            TutorialOverlay.Instance?.ShowContextualTip(key, duration);
        }
    }
}

