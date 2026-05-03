using System;
using System.Collections.Generic;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.UI
{
    /// <summary>
    /// Single tutorial message configuration. Message holds a localization key (e.g. tutorial.cell.0);
    /// display uses Localization.Get(Message) for the visible text.
    /// </summary>
    public class TutorialStep
    {
        public string Message;
        public float DisplayDuration;
        public string HighlightElement;
    }

    /// <summary>
    /// Tracks which tutorial stages and steps the player has already seen. Persists to PlayerPrefs so progress survives restarts.
    /// </summary>
    public class TutorialProgress
    {
        private const string PREFS_COMPLETED = "TutorialProgress_Completed";
        private const string PREFS_STEPS = "TutorialProgress_Steps";

        private readonly HashSet<GameStageType> _completedStages = new HashSet<GameStageType>();
        private readonly Dictionary<GameStageType, int> _stepIndices = new Dictionary<GameStageType, int>();

        public bool IsStageCompleted(GameStageType stage) => _completedStages.Contains(stage);

        public void MarkStageCompleted(GameStageType stage) => _completedStages.Add(stage);

        public int GetStepIndex(GameStageType stage)
        {
            return _stepIndices.TryGetValue(stage, out int idx) ? idx : 0;
        }

        public void AdvanceStep(GameStageType stage)
        {
            if (!_stepIndices.ContainsKey(stage))
                _stepIndices[stage] = 0;
            _stepIndices[stage]++;
        }

        public void Reset()
        {
            _completedStages.Clear();
            _stepIndices.Clear();
        }

        /// <summary>Used when loading from PlayerPrefs to restore completed stages.</summary>
        public void AddCompletedStage(GameStageType stage) => _completedStages.Add(stage);

        /// <summary>Used when loading from PlayerPrefs to restore step index.</summary>
        public void SetStepIndex(GameStageType stage, int index) => _stepIndices[stage] = index;

        /// <summary>
        /// Saves completed stages and step indices to PlayerPrefs. Call after MarkStageCompleted or AdvanceStep.
        /// </summary>
        public void SaveToPlayerPrefs()
        {
            var completedList = new List<string>();
            foreach (var stage in _completedStages)
                completedList.Add(stage.ToString());
            PlayerPrefs.SetString(PREFS_COMPLETED, string.Join(",", completedList));

            var stepParts = new List<string>();
            foreach (var kvp in _stepIndices)
                stepParts.Add($"{kvp.Key}:{kvp.Value}");
            PlayerPrefs.SetString(PREFS_STEPS, string.Join(",", stepParts));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads progress from PlayerPrefs, or returns an empty progress if none saved.
        /// </summary>
        public static TutorialProgress LoadFromPlayerPrefs()
        {
            var progress = new TutorialProgress();
            string completed = PlayerPrefs.GetString(PREFS_COMPLETED, "");
            if (!string.IsNullOrEmpty(completed))
            {
                foreach (string part in completed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Enum.TryParse<GameStageType>(part.Trim(), out var stage))
                        progress.AddCompletedStage(stage);
                }
            }

            string steps = PlayerPrefs.GetString(PREFS_STEPS, "");
            if (!string.IsNullOrEmpty(steps))
            {
                foreach (string part in steps.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int colon = part.IndexOf(':');
                    if (colon > 0 && colon < part.Length - 1 &&
                        Enum.TryParse<GameStageType>(part.Substring(0, colon).Trim(), out var stage) &&
                        int.TryParse(part.Substring(colon + 1).Trim(), out int idx))
                        progress.SetStepIndex(stage, idx);
                }
            }

            return progress;
        }
    }

    /// <summary>
    /// Predefined tutorial step sequences for each <see cref="GameStageType"/>.
    /// </summary>
    public static class TutorialCatalog
    {
        private static readonly Dictionary<GameStageType, TutorialStep[]> Steps =
            new Dictionary<GameStageType, TutorialStep[]>
        {
            { GameStageType.Cell, new[]
            {
                new TutorialStep { Message = "tutorial.cell.0", DisplayDuration = 6f },
                new TutorialStep { Message = "tutorial.cell.1", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.cell.2", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.cell.3", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.cell.4", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.cell.5", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.cell.6", DisplayDuration = 5f },
            }},
            { GameStageType.Creature, new[]
            {
                new TutorialStep { Message = "tutorial.creature.0", DisplayDuration = 6f },
                new TutorialStep { Message = "tutorial.creature.1", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.creature.2", DisplayDuration = 5f },
                new TutorialStep { Message = "tutorial.creature.3", DisplayDuration = 5f },
            }},
        };

        public static TutorialStep[] GetSteps(GameStageType stage)
        {
            return Steps.TryGetValue(stage, out var steps) ? steps : null;
        }
    }
}
