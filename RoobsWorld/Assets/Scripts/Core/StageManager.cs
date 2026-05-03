using System;
using System.Collections;
using System.Collections.Generic;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Stages.Creature;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Contract for a top-level game stage that can be driven by <see cref="StageManager"/>.
    /// </summary>
    public interface IGameStage
    {
        /// <summary>Identifies the logical game stage represented by this instance.</summary>
        GameStageType StageType { get; }
        /// <summary>Called when the stage becomes active after a transition.</summary>
        void OnEnter();
        /// <summary>Called when the stage is deactivated during a transition.</summary>
        void OnExit();
        /// <summary>Per-frame update invoked by <see cref="StageManager"/> while the stage is active.</summary>
        void OnUpdate();
    }

    /// <summary>
    /// Finite state machine that manages scene loading and lifecycle for the current <see cref="IGameStage"/>.
    /// </summary>
    public class StageManager : Singleton<StageManager>
    {
        private readonly Dictionary<GameStageType, IGameStage> _stages = new Dictionary<GameStageType, IGameStage>();
        private IGameStage _currentStage;
        private bool _isTransitioning;

        public GameStageType CurrentStageType => _currentStage?.StageType ?? GameStageType.None;
        public bool IsTransitioning => _isTransitioning;

        private static readonly Dictionary<GameStageType, string> StageSceneMap = new Dictionary<GameStageType, string>
        {
            { GameStageType.Cell, "CellStage" },
            { GameStageType.Creature, "CreatureStage" }
        };

        private static readonly Dictionary<GameStageType, Type> StageComponentMap = new Dictionary<GameStageType, Type>
        {
            { GameStageType.Cell, typeof(CellStage) },
            { GameStageType.Creature, typeof(CreatureStage) },
        };

        /// <summary>
        /// Returns true when the stage is allowed in the cell+creature build.
        /// </summary>
        public static bool IsStageSupported(GameStageType stageType)
        {
            return stageType == GameStageType.Cell
                || stageType == GameStageType.Creature;
        }

        /// <summary>
        /// Returns a current-build stage request, or None when the request is unsupported.
        /// </summary>
        public static GameStageType NormalizeRequestedStage(GameStageType requested)
        {
            if (IsStageSupported(requested))
                return requested;

            Debug.LogError($"[StageManager] Unsupported stage '{requested}' requested.");
            return GameStageType.None;
        }

        /// <summary>
        /// Registers a stage implementation so it can be activated by <see cref="TransitionTo"/>.
        /// Typically called from the stage MonoBehaviour's <c>Start</c> method.
        /// </summary>
        public void RegisterStage(IGameStage stage)
        {
            _stages[stage.StageType] = stage;
        }

        /// <summary>
        /// Removes a previously registered stage, e.g. when its scene is unloaded.
        /// </summary>
        public void UnregisterStage(GameStageType stageType)
        {
            _stages.Remove(stageType);
        }

        /// <summary>
        /// Asynchronously transitions from the current stage to <paramref name="targetStage"/>, loading the mapped scene,
        /// publishing a <see cref="StageTransitionEvent"/>, and invoking <see cref="IGameStage.OnExit"/> / <see cref="IGameStage.OnEnter"/>.
        /// </summary>
        public void TransitionTo(GameStageType targetStage)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[StageManager] Already transitioning, ignoring request.");
                return;
            }

            StartCoroutine(TransitionCoroutine(targetStage));
        }

        /// <summary>
        /// Immediately sets the active stage without loading a new scene. Useful for tests and editor tooling.
        /// </summary>
        public void SetStageImmediate(IGameStage stage)
        {
            _currentStage?.OnExit();
            _stages[stage.StageType] = stage;
            _currentStage = stage;
            _currentStage.OnEnter();
        }

        private IEnumerator TransitionCoroutine(GameStageType targetStage)
        {
            _isTransitioning = true;
            targetStage = NormalizeRequestedStage(targetStage);
            if (targetStage == GameStageType.None)
            {
                _isTransitioning = false;
                yield break;
            }

            var fromStage = CurrentStageType;

            _currentStage?.OnExit();

            EventBus.Publish(new StageTransitionEvent
            {
                FromStage = fromStage,
                ToStage = targetStage
            });

            var vfxPos = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 2f : Vector3.zero;
            RoobsWorld.Utils.VFXFactory.Create(RoobsWorld.Utils.VFXType.StageTransition, vfxPos);

            if (StageSceneMap.TryGetValue(targetStage, out string sceneName))
            {
                if (Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    var op = SceneManager.LoadSceneAsync(sceneName);
                    if (op != null)
                        yield return op;
                }
                else
                {
                    Debug.Log($"[StageManager] Scene '{sceneName}' is not in build settings; using runtime stage component.");
                }
            }

            // Let loaded-scene stage objects run Start() and self-register before we fallback-spawn.
            if (!_stages.ContainsKey(targetStage))
                yield return null;

            // If the scene was empty, spawn the stage MonoBehaviour so it can self-register.
            if (!_stages.ContainsKey(targetStage) && StageComponentMap.TryGetValue(targetStage, out var stageType))
            {
                var go = new GameObject(stageType.Name);
                go.AddComponent(stageType);
                // Wait one frame for Start() to run and call RegisterStage.
                yield return null;
            }

            if (_stages.TryGetValue(targetStage, out var stage))
            {
                _currentStage = stage;
                _currentStage.OnEnter();
            }
            else
            {
                Debug.LogWarning($"[StageManager] No stage registered for {targetStage}");
                _currentStage = null;
            }

            _isTransitioning = false;
        }

        private void Update()
        {
            if (ShouldRunStageUpdate(_isTransitioning, GameManager.HasInstance, GameManager.HasInstance && GameManager.Instance.IsPaused))
                _currentStage?.OnUpdate();
        }

        public static bool ShouldRunStageUpdate(bool isTransitioning, bool hasGameManager, bool isPaused)
        {
            return !isTransitioning && (!hasGameManager || !isPaused);
        }
    }
}
