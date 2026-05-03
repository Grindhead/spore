using System.Collections.Generic;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Spawns and tracks rival creatures for the Creature-stage encounter loop.
    /// </summary>
    public class CreatureStageEncounterManager : MonoBehaviour
    {
        [SerializeField] private int _startingRivals = 5;
        [SerializeField] private int _maxSpawnedRivals = 10;
        [SerializeField] private int _defeatsPerRivalTier = 2;
        [SerializeField] private float _respawnDelaySeconds = 4f;
        [SerializeField] private float _killStreakWindowSeconds = 9f;
        [SerializeField] private int _streakBonusThreshold = 3;
        [SerializeField] private int _streakBonusDnaBase = 3;
        [SerializeField] private int _streakBonusStepInterval = 2;
        [SerializeField] private float _respawnSafeDistanceFromRivals = 10f;
        [SerializeField] private int _respawnSampleCount = 14;
        [SerializeField] private float _spawnRadiusMin = 8f;
        [SerializeField] private float _spawnRadiusMax = 20f;
        [SerializeField] private float _arenaClampRadius = 48f;

        private const float RivalTierScaleStep = 0.03f;
        private const float RivalTierScaleMaxBonus = 0.12f;

        private readonly Dictionary<string, CreatureRivalAI> _trackedRivals = new Dictionary<string, CreatureRivalAI>();
        private Transform _player;
        private int _defeatedCount;
        private int _killStreakCount;
        private int _totalSpawnedRivals;
        private int _spawnSequence;
        private float _respawnCooldownTimer;
        private float _killStreakTimerRemaining;
        private bool _initialized;

        public int ActiveRivalCount => CountLivingTrackedRivals();
        public int DefeatedCount => _defeatedCount;
        public int KillStreakCount => _killStreakCount;
        public int TotalSpawnedRivals => _totalSpawnedRivals;

        public void Initialize(Transform player)
        {
            if (_initialized)
                EventBus.Unsubscribe<CreatureDiedEvent>(OnCreatureDied);

            _player = player;
            _defeatedCount = 0;
            _killStreakCount = 0;
            _trackedRivals.Clear();
            _totalSpawnedRivals = 0;
            _spawnSequence = 0;
            _respawnCooldownTimer = 0f;
            _killStreakTimerRemaining = 0f;
            EventBus.Subscribe<CreatureDiedEvent>(OnCreatureDied);
            SpawnInitialRivals();
            _initialized = true;
        }

        public void SetPlayerTarget(Transform player)
        {
            _player = player;
            var rivals = FindObjectsByType<CreatureRivalAI>(FindObjectsInactive.Exclude);
            for (int i = 0; i < rivals.Length; i++)
                rivals[i].SetPlayerTarget(player);
        }

        /// <summary>
        /// Attempts to find a respawn point that maximizes distance from living rivals.
        /// Returns true when the selected point satisfies the configured safe-distance threshold.
        /// </summary>
        public bool TryGetSafeRespawnPosition(Vector3 fallbackPosition, out Vector3 safePosition)
        {
            Vector3 fallback = ClampToArena(fallbackPosition);
            var rivals = GetAliveRivalPositions();
            if (rivals.Count == 0)
            {
                safePosition = fallback;
                return true;
            }

            int sampleCount = Mathf.Max(4, _respawnSampleCount);
            Vector3[] candidates = new Vector3[sampleCount];
            candidates[0] = fallback;

            for (int i = 1; i < sampleCount; i++)
                candidates[i] = ClampToArena(GetRandomSpawnPosition(fallback, _spawnRadiusMin, _spawnRadiusMax, _arenaClampRadius));

            Vector3[] rivalPositions = rivals.ToArray();
            safePosition = SelectBestRespawnCandidate(candidates, rivalPositions, fallback);

            float nearest = ComputeNearestRivalDistanceFlat(safePosition, rivalPositions);
            return nearest >= Mathf.Max(0.1f, _respawnSafeDistanceFromRivals);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CreatureDiedEvent>(OnCreatureDied);
            _initialized = false;
        }

        private void SpawnInitialRivals()
        {
            if (_player == null)
                return;

            int count = Mathf.Max(1, _startingRivals);
            for (int i = 0; i < count; i++)
                SpawnRival();
        }

        private void SpawnRival()
        {
            if (_player == null)
                return;

            Vector3 spawnPos = GetRandomSpawnPosition(_player.position, _spawnRadiusMin, _spawnRadiusMax, _arenaClampRadius);
            int rivalTier = ComputeRivalTier(_defeatedCount, _defeatsPerRivalTier);
            var playerCreature = _player.GetComponent<RoobsWorld.Creature.CreatureInstance>();
            var dna = BuildRivalDNA(_spawnSequence, rivalTier, playerCreature?.DNA);
            _spawnSequence++;
            var rivalObject = RoobsWorld.Creature.CreatureFactory.Instance.SpawnCreature(dna, spawnPos, false);
            if (rivalObject == null)
                return;

            var ai = rivalObject.GetComponent<CreatureRivalAI>();
            if (ai == null)
                ai = rivalObject.AddComponent<CreatureRivalAI>();
            ai.SetPlayerTarget(_player);

            _trackedRivals[dna.CreatureId] = ai;
            _totalSpawnedRivals++;
        }

        private void OnCreatureDied(CreatureDiedEvent evt)
        {
            if (evt.IsPlayer || string.IsNullOrWhiteSpace(evt.CreatureId))
                return;
            if (!_trackedRivals.Remove(evt.CreatureId))
                return;

            _defeatedCount++;
            _killStreakCount = ComputeNextStreak(_killStreakCount, _killStreakTimerRemaining);
            _killStreakTimerRemaining = Mathf.Max(0f, _killStreakWindowSeconds);

            int streakBonusDna = ComputeStreakBonusDNA(
                _killStreakCount,
                _streakBonusThreshold,
                _streakBonusDnaBase,
                _streakBonusStepInterval);

            if (GameManager.HasInstance)
            {
                GameManager.Instance.AddDNAPoints(BalanceConstants.CreatureDNAPointsPerRivalDefeat);
                if (streakBonusDna > 0)
                    GameManager.Instance.AddDNAPoints(streakBonusDna);
                if (_killStreakCount >= 5)
                    GameManager.Instance.UnlockAchievement("creature_apex_streak");
            }

            if (streakBonusDna > 0)
                RoobsWorld.UI.TutorialOverlay.Instance?.ShowContextualTip("creature.streak.bonus.tip", 1.8f);

            _respawnCooldownTimer = Mathf.Max(_respawnCooldownTimer, _respawnDelaySeconds);
        }

        public void UpdatePopulation(float deltaTime, int desiredActiveRivals, int objectiveDefeats)
        {
            if (!_initialized)
                return;
            if (_player == null)
                return;

            PruneInvalidTrackedRivals();
            _respawnCooldownTimer = Mathf.Max(0f, _respawnCooldownTimer - Mathf.Max(0f, deltaTime));
            _killStreakTimerRemaining = Mathf.Max(0f, _killStreakTimerRemaining - Mathf.Max(0f, deltaTime));
            if (_killStreakTimerRemaining <= 0f)
                _killStreakCount = 0;

            bool shouldSpawn = ShouldSpawnReplacement(
                activeRivals: ActiveRivalCount,
                desiredActiveRivals: desiredActiveRivals,
                defeatedRivals: _defeatedCount,
                objectiveDefeats: objectiveDefeats,
                totalSpawnedRivals: _totalSpawnedRivals,
                maxSpawnedRivals: Mathf.Max(_startingRivals, _maxSpawnedRivals),
                respawnCooldownTimer: _respawnCooldownTimer);

            if (!shouldSpawn)
                return;

            SpawnRival();
            _respawnCooldownTimer = _respawnDelaySeconds;
        }

        private static RoobsWorld.Creature.CreatureDNA BuildRivalDNA(
            int index,
            int rivalTier,
            RoobsWorld.Creature.CreatureDNA playerDna)
        {
            var dna = RoobsWorld.Creature.CreatureDNA.CreateDefault();
            int safeTier = Mathf.Max(0, rivalTier);
            dna.CreatureName = $"Rival {index + 1}";
            dna.Body.BodyScale = ComputeRivalBodyScale(playerDna, safeTier);
            float tierLerp = Mathf.Clamp01(safeTier * 0.2f + Random.value * 0.25f);
            dna.Colors.PrimaryColor = Color.Lerp(new Color(0.78f, 0.22f, 0.2f), new Color(0.58f, 0.16f, 0.1f), tierLerp);
            dna.Colors.SecondaryColor = Color.Lerp(new Color(0.32f, 0.14f, 0.1f), new Color(0.16f, 0.08f, 0.07f), tierLerp);

            // Add extra offensive parts so encounters become meaningful.
            dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
            {
                PartId = "spike_basic",
                AttachBone = 1,
                Offset = new Vector3(0f, 0.2f, 0.55f),
                Scale = 1f
            });
            dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
            {
                PartId = "horn_basic",
                AttachBone = 0,
                Offset = new Vector3(0f, 0.28f, 0.28f),
                Scale = 0.95f
            });

            if (safeTier >= 1)
            {
                dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
                {
                    PartId = "arm_basic",
                    AttachBone = 1,
                    Offset = new Vector3(0.55f, 0.18f, 0.08f),
                    Scale = 0.95f
                });
                dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
                {
                    PartId = "arm_basic",
                    AttachBone = 1,
                    Offset = new Vector3(-0.55f, 0.18f, 0.08f),
                    Scale = 0.95f
                });
            }

            if (safeTier >= 2)
            {
                dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
                {
                    PartId = "shell_basic",
                    AttachBone = 1,
                    Offset = new Vector3(0f, 0.24f, -0.08f),
                    Scale = 1f
                });
                dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
                {
                    PartId = "spike_basic",
                    AttachBone = 2,
                    Offset = new Vector3(0f, 0.26f, -0.34f),
                    Scale = 0.9f
                });
            }

            if (safeTier >= 3)
            {
                dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
                {
                    PartId = "tail_basic",
                    AttachBone = 2,
                    Offset = new Vector3(0f, 0.1f, -0.7f),
                    Scale = 1f
                });
                dna.Parts.Add(new RoobsWorld.Creature.PartPlacement
                {
                    PartId = "horn_basic",
                    AttachBone = 0,
                    Offset = new Vector3(0f, 0.35f, 0.4f),
                    Scale = 1f
                });
            }
            return dna;
        }

        public static Vector3 ComputeRivalBodyScale(RoobsWorld.Creature.CreatureDNA playerDna, int rivalTier)
        {
            Vector3 playerScale = playerDna?.Body != null ? playerDna.Body.BodyScale : Vector3.one;
            if (playerScale == Vector3.zero)
                playerScale = Vector3.one;

            float tierBonus = Mathf.Min(Mathf.Max(0, rivalTier) * RivalTierScaleStep, RivalTierScaleMaxBonus);
            return playerScale * (1f + tierBonus);
        }

        private int CountLivingTrackedRivals()
        {
            int alive = 0;
            foreach (var pair in _trackedRivals)
            {
                if (pair.Value == null)
                    continue;

                var creature = pair.Value.GetComponent<RoobsWorld.Creature.CreatureInstance>();
                if (creature != null && creature.IsAlive)
                    alive++;
            }
            return alive;
        }

        private List<Vector3> GetAliveRivalPositions()
        {
            var positions = new List<Vector3>();
            foreach (var pair in _trackedRivals)
            {
                if (pair.Value == null)
                    continue;

                var creature = pair.Value.GetComponent<RoobsWorld.Creature.CreatureInstance>();
                if (creature == null || !creature.IsAlive)
                    continue;

                positions.Add(creature.transform.position);
            }
            return positions;
        }

        private Vector3 ClampToArena(Vector3 position)
        {
            float maxClamp = Mathf.Max(1f, _arenaClampRadius);
            Vector3 flat = new Vector3(position.x, 0f, position.z);
            if (flat.magnitude > maxClamp)
                flat = flat.normalized * maxClamp;

            float y = _player != null ? _player.position.y : position.y;
            return new Vector3(flat.x, y, flat.z);
        }

        private void PruneInvalidTrackedRivals()
        {
            if (_trackedRivals.Count == 0)
                return;

            var staleIds = new List<string>();
            foreach (var pair in _trackedRivals)
            {
                if (pair.Value == null)
                {
                    staleIds.Add(pair.Key);
                    continue;
                }

                var creature = pair.Value.GetComponent<RoobsWorld.Creature.CreatureInstance>();
                if (creature == null || !creature.IsAlive)
                    staleIds.Add(pair.Key);
            }

            for (int i = 0; i < staleIds.Count; i++)
                _trackedRivals.Remove(staleIds[i]);
        }

        /// <summary>
        /// Returns a random spawn position in a ring around center, clamped to arena radius.
        /// </summary>
        public static Vector3 GetRandomSpawnPosition(
            Vector3 center,
            float minRadius,
            float maxRadius,
            float clampRadius)
        {
            float minR = Mathf.Max(0.1f, minRadius);
            float maxR = Mathf.Max(minR + 0.1f, maxRadius);

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist = Random.Range(minR, maxR);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
            Vector3 candidate = center + offset;

            float maxClamp = Mathf.Max(1f, clampRadius);
            Vector3 flat = new Vector3(candidate.x, 0f, candidate.z);
            if (flat.magnitude > maxClamp)
                flat = flat.normalized * maxClamp;

            return new Vector3(flat.x, center.y, flat.z);
        }

        /// <summary>
        /// Returns true when encounter rules allow spawning a replacement rival.
        /// </summary>
        public static bool ShouldSpawnReplacement(
            int activeRivals,
            int desiredActiveRivals,
            int defeatedRivals,
            int objectiveDefeats,
            int totalSpawnedRivals,
            int maxSpawnedRivals,
            float respawnCooldownTimer)
        {
            if (respawnCooldownTimer > 0f)
                return false;
            if (defeatedRivals >= objectiveDefeats)
                return false;
            if (activeRivals >= desiredActiveRivals)
                return false;
            int effectiveMaxSpawned = Mathf.Max(maxSpawnedRivals, objectiveDefeats + desiredActiveRivals);
            if (totalSpawnedRivals >= effectiveMaxSpawned)
                return false;
            return true;
        }

        /// <summary>
        /// Computes rival power tier from defeated count and tier step interval.
        /// </summary>
        public static int ComputeRivalTier(int defeatedRivals, int defeatsPerTier)
        {
            int safeDefeats = Mathf.Max(0, defeatedRivals);
            int safeStep = Mathf.Max(1, defeatsPerTier);
            return safeDefeats / safeStep;
        }

        /// <summary>
        /// Continues a streak when time remains; otherwise starts a fresh streak at 1.
        /// </summary>
        public static int ComputeNextStreak(int currentStreak, float streakTimerRemaining)
        {
            if (streakTimerRemaining > 0f)
                return Mathf.Max(0, currentStreak) + 1;
            return 1;
        }

        /// <summary>
        /// Calculates bonus DNA for streak milestones. Returns 0 when no milestone is reached.
        /// </summary>
        public static int ComputeStreakBonusDNA(
            int streakCount,
            int threshold,
            int baseBonusDna,
            int stepInterval)
        {
            int safeThreshold = Mathf.Max(1, threshold);
            int safeBase = Mathf.Max(0, baseBonusDna);
            int safeStep = Mathf.Max(1, stepInterval);
            int safeStreak = Mathf.Max(0, streakCount);

            if (safeStreak < safeThreshold)
                return 0;

            int offset = safeStreak - safeThreshold;
            if (offset % safeStep != 0)
                return 0;

            int tier = offset / safeStep;
            return safeBase + tier;
        }

        /// <summary>
        /// Returns the candidate with the largest nearest-rival distance (flattened XZ distance).
        /// </summary>
        public static Vector3 SelectBestRespawnCandidate(Vector3[] candidates, Vector3[] rivalPositions, Vector3 fallback)
        {
            if (candidates == null || candidates.Length == 0)
                return fallback;
            if (rivalPositions == null || rivalPositions.Length == 0)
                return candidates[0];

            Vector3 best = candidates[0];
            float bestScore = ComputeNearestRivalDistanceFlat(best, rivalPositions);

            for (int i = 1; i < candidates.Length; i++)
            {
                float score = ComputeNearestRivalDistanceFlat(candidates[i], rivalPositions);
                if (score > bestScore)
                {
                    best = candidates[i];
                    bestScore = score;
                }
            }

            return best;
        }

        private static float ComputeNearestRivalDistanceFlat(Vector3 position, Vector3[] rivalPositions)
        {
            if (rivalPositions == null || rivalPositions.Length == 0)
                return float.MaxValue;

            float nearest = float.MaxValue;
            Vector2 pos = new Vector2(position.x, position.z);

            for (int i = 0; i < rivalPositions.Length; i++)
            {
                Vector2 rival = new Vector2(rivalPositions[i].x, rivalPositions[i].z);
                float dist = Vector2.Distance(pos, rival);
                if (dist < nearest)
                    nearest = dist;
            }

            return nearest;
        }
    }
}
