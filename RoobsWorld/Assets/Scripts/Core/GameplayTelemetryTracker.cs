using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Core
{
    public struct GameplayTelemetrySnapshot
    {
        public float RunDurationSeconds;
        public int PlayerDeaths;
        public int GoalsCompleted;
        public int MutationsChosen;
        public int PlantFoodEaten;
        public int MeatFoodEaten;
        public int ToxinFoodEaten;
        public int RivalKills;
        public int AlphaRivalKills;
        public int CellGoalCategoryRepeats;
        public int CreatureStaminaExhaustions;
        public int CreatureStaminaRestores;
        public int CreatureAbilityUses;
        public int CreatureStreakBonuses;
        public int CreatureObjectivesCompleted;
        public float FastestCreatureObjectiveSeconds;
        public float AverageFps;
    }

    /// <summary>
    /// Lightweight runtime telemetry for playtests. In editor/development builds it can draw a compact debug panel.
    /// </summary>
    public class GameplayTelemetryTracker : MonoBehaviour
    {
        private float _startedAt;
        private int _frameCount;
        private float _unscaledFrameSeconds;
        private bool _panelVisible;
        private GameplayTelemetrySnapshot _snapshot;

        public GameplayTelemetrySnapshot Snapshot => BuildSnapshot();

        public static GameplayTelemetryTracker Attach(string name, bool panelVisible = false)
        {
            var go = new GameObject(string.IsNullOrWhiteSpace(name) ? "GameplayTelemetry" : name);
            var tracker = go.AddComponent<GameplayTelemetryTracker>();
            tracker._panelVisible = panelVisible;
            return tracker;
        }

        private void Awake()
        {
            _startedAt = Time.unscaledTime;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CreatureDiedEvent>(OnCreatureDied);
            EventBus.Subscribe<CellGoalCompletedEvent>(OnCellGoalCompleted);
            EventBus.Subscribe<CellFoodConsumedEvent>(OnFoodConsumed);
            EventBus.Subscribe<CellMutationChosenEvent>(OnMutationChosen);
            EventBus.Subscribe<CreatureRivalDefeatedEvent>(OnRivalDefeated);
            EventBus.Subscribe<CreatureStaminaExhaustedEvent>(OnCreatureStaminaExhausted);
            EventBus.Subscribe<CreatureStaminaRestoredEvent>(OnCreatureStaminaRestored);
            EventBus.Subscribe<CreatureAbilityUsedEvent>(OnCreatureAbilityUsed);
            EventBus.Subscribe<CreatureStreakBonusEvent>(OnCreatureStreakBonus);
            EventBus.Subscribe<CreatureObjectiveCompletedEvent>(OnCreatureObjectiveCompleted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CreatureDiedEvent>(OnCreatureDied);
            EventBus.Unsubscribe<CellGoalCompletedEvent>(OnCellGoalCompleted);
            EventBus.Unsubscribe<CellFoodConsumedEvent>(OnFoodConsumed);
            EventBus.Unsubscribe<CellMutationChosenEvent>(OnMutationChosen);
            EventBus.Unsubscribe<CreatureRivalDefeatedEvent>(OnRivalDefeated);
            EventBus.Unsubscribe<CreatureStaminaExhaustedEvent>(OnCreatureStaminaExhausted);
            EventBus.Unsubscribe<CreatureStaminaRestoredEvent>(OnCreatureStaminaRestored);
            EventBus.Unsubscribe<CreatureAbilityUsedEvent>(OnCreatureAbilityUsed);
            EventBus.Unsubscribe<CreatureStreakBonusEvent>(OnCreatureStreakBonus);
            EventBus.Unsubscribe<CreatureObjectiveCompletedEvent>(OnCreatureObjectiveCompleted);
        }

        private void Update()
        {
            _frameCount++;
            _unscaledFrameSeconds += Time.unscaledDeltaTime;
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_panelVisible)
                return;

            var s = BuildSnapshot();
            GUI.Box(new Rect(12f, 120f, 300f, 170f),
                $"Telemetry\nTime {s.RunDurationSeconds:F0}s  FPS {s.AverageFps:F0}\nFood P/M/T {s.PlantFoodEaten}/{s.MeatFoodEaten}/{s.ToxinFoodEaten}\nGoals {s.GoalsCompleted}  Repeats {s.CellGoalCategoryRepeats}\nDeaths {s.PlayerDeaths}  Rivals {s.RivalKills}  Alpha {s.AlphaRivalKills}\nAbility {s.CreatureAbilityUses}  Stamina low {s.CreatureStaminaExhaustions}\nStamina + {s.CreatureStaminaRestores}  Streak bonus {s.CreatureStreakBonuses}\nObjectives {s.CreatureObjectivesCompleted}  Best {s.FastestCreatureObjectiveSeconds:F0}s");
#endif
        }

        private GameplayTelemetrySnapshot BuildSnapshot()
        {
            _snapshot.RunDurationSeconds = Mathf.Max(0f, Time.unscaledTime - _startedAt);
            _snapshot.AverageFps = _unscaledFrameSeconds > 0.001f ? _frameCount / _unscaledFrameSeconds : 0f;
            return _snapshot;
        }

        private void OnCreatureDied(CreatureDiedEvent evt)
        {
            if (evt.IsPlayer)
                _snapshot.PlayerDeaths++;
        }

        private void OnCellGoalCompleted(CellGoalCompletedEvent evt)
        {
            _snapshot.GoalsCompleted++;
            if (evt.RepeatedActionCategory)
                _snapshot.CellGoalCategoryRepeats++;
        }

        private void OnMutationChosen(CellMutationChosenEvent evt) => _snapshot.MutationsChosen++;

        private void OnRivalDefeated(CreatureRivalDefeatedEvent evt)
        {
            _snapshot.RivalKills++;
            if (evt.IsAlpha)
                _snapshot.AlphaRivalKills++;
        }

        private void OnCreatureStaminaExhausted(CreatureStaminaExhaustedEvent evt) => _snapshot.CreatureStaminaExhaustions++;

        private void OnCreatureStaminaRestored(CreatureStaminaRestoredEvent evt) => _snapshot.CreatureStaminaRestores++;

        private void OnCreatureAbilityUsed(CreatureAbilityUsedEvent evt) => _snapshot.CreatureAbilityUses++;

        private void OnCreatureStreakBonus(CreatureStreakBonusEvent evt) => _snapshot.CreatureStreakBonuses++;

        private void OnCreatureObjectiveCompleted(CreatureObjectiveCompletedEvent evt)
        {
            _snapshot.CreatureObjectivesCompleted++;
            if (evt.CompletionSeconds > 0f &&
                (_snapshot.FastestCreatureObjectiveSeconds <= 0f || evt.CompletionSeconds < _snapshot.FastestCreatureObjectiveSeconds))
                _snapshot.FastestCreatureObjectiveSeconds = evt.CompletionSeconds;
        }

        private void OnFoodConsumed(CellFoodConsumedEvent evt)
        {
            switch (evt.FoodType)
            {
                case FoodType.Meat:
                    _snapshot.MeatFoodEaten++;
                    break;
                case FoodType.Toxin:
                    _snapshot.ToxinFoodEaten++;
                    break;
                default:
                    _snapshot.PlantFoodEaten++;
                    break;
            }
        }
    }
}
