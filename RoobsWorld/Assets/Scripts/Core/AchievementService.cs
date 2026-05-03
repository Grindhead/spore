using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Subscribes to game events and unlocks achievements via GameManager.
    /// </summary>
    [ExecuteAlways]
    public class AchievementService : MonoBehaviour
    {
        private readonly HashSet<CellPartType> _collectedCellParts = new HashSet<CellPartType>();
        private static readonly int TotalCellPartTypes = System.Enum.GetValues(typeof(CellPartType)).Length;
        private static GameManager ManagerOrNull => GameManager.Instance;

        private void OnEnable()
        {
            EventBus.Subscribe<DNAPointsChangedEvent>(OnDNAPointsChanged);
            EventBus.Subscribe<CellGoalCompletedEvent>(OnCellGoalCompleted);
            EventBus.Subscribe<CellEvolutionCompleteEvent>(OnCellEvolutionComplete);
            EventBus.Subscribe<StageTransitionEvent>(OnStageTransition);
            EventBus.Subscribe<CreatureDiedEvent>(OnCreatureDied);
            EventBus.Subscribe<CellPlayerEscapedPredatorEvent>(OnPlayerEscapedPredator);
            EventBus.Subscribe<CellPartCollectedEvent>(OnCellPartCollected);
            EventBus.Subscribe<CellNewSpeciesEvent>(OnCellNewSpecies);
            EventBus.Subscribe<CellSpeciesExtinctEvent>(OnCellSpeciesExtinct);
            EventBus.Subscribe<CellLeviathanDefeatedEvent>(OnLeviathanDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DNAPointsChangedEvent>(OnDNAPointsChanged);
            EventBus.Unsubscribe<CellGoalCompletedEvent>(OnCellGoalCompleted);
            EventBus.Unsubscribe<CellEvolutionCompleteEvent>(OnCellEvolutionComplete);
            EventBus.Unsubscribe<StageTransitionEvent>(OnStageTransition);
            EventBus.Unsubscribe<CreatureDiedEvent>(OnCreatureDied);
            EventBus.Unsubscribe<CellPlayerEscapedPredatorEvent>(OnPlayerEscapedPredator);
            EventBus.Unsubscribe<CellPartCollectedEvent>(OnCellPartCollected);
            EventBus.Unsubscribe<CellNewSpeciesEvent>(OnCellNewSpecies);
            EventBus.Unsubscribe<CellSpeciesExtinctEvent>(OnCellSpeciesExtinct);
            EventBus.Unsubscribe<CellLeviathanDefeatedEvent>(OnLeviathanDefeated);
        }

        private void OnDNAPointsChanged(DNAPointsChangedEvent evt)
        {
            var manager = ManagerOrNull;
            if (manager == null) return;
            if (StageManager.Instance.CurrentStageType != GameStageType.Cell) return;
            if (evt.OldAmount == 0 && evt.NewAmount >= 1)
                manager.UnlockAchievement("cell_first_eat");
        }

        private void OnCellGoalCompleted(CellGoalCompletedEvent evt)
        {
            var manager = ManagerOrNull;
            if (manager == null) return;
            manager.UnlockAchievement("cell_first_goal");
        }

        private void OnCellEvolutionComplete(CellEvolutionCompleteEvent evt)
        {
            var manager = ManagerOrNull;
            if (manager == null) return;
            manager.UnlockAchievement("cell_evolve");
            // Big Fish: reach maximum cell size (final evolution tier)
            if (evt.EvolutionLevel >= 3)
                manager.UnlockAchievement("cell_grow_large");
        }

        private void OnStageTransition(StageTransitionEvent evt)
        {
            if (evt.ToStage == GameStageType.Cell)
                _collectedCellParts.Clear();
        }

        private void OnPlayerEscapedPredator(CellPlayerEscapedPredatorEvent evt)
        {
            var manager = ManagerOrNull;
            if (manager == null) return;
            manager.UnlockAchievement("cell_survive_predator");
        }

        private void OnCellPartCollected(CellPartCollectedEvent evt)
        {
            var manager = ManagerOrNull;
            if (manager == null) return;
            _collectedCellParts.Add(evt.PartType);
            if (_collectedCellParts.Count >= TotalCellPartTypes)
                manager.UnlockAchievement("cell_all_parts");
        }

        private void OnCreatureDied(CreatureDiedEvent evt)
        {
            var manager = ManagerOrNull;
            if (manager == null) return;
            if (evt.IsPlayer) return;
            manager.UnlockAchievement("creature_first_kill");
        }

        private void OnCellNewSpecies(CellNewSpeciesEvent evt)
        {
            ManagerOrNull?.UnlockAchievement("cell_first_speciation");
            if (evt.TotalSpeciesCount >= 10)
                ManagerOrNull?.UnlockAchievement("cell_10_species");
        }

        private void OnCellSpeciesExtinct(CellSpeciesExtinctEvent evt)
        {
            ManagerOrNull?.UnlockAchievement("cell_first_extinction");
        }

        private void OnLeviathanDefeated(CellLeviathanDefeatedEvent evt)
        {
            ManagerOrNull?.UnlockAchievement("cell_defeated_leviathan");
        }
    }
}
