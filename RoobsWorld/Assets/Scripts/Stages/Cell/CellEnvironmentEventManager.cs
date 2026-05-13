using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Triggers periodic randomised environment events in the cell stage
    /// (NutrientBloom, VirusOutbreak, TemperatureChange, RadiationSurge, OxygenSurge).
    /// Publishes <see cref="CellEnvironmentEventTriggered"/> on start and end so the HUD and gameplay systems can react.
    /// RadiationSurge has a short display window then ends; CellStage reacts to it by offering the player a bonus mutation.
    /// Attach to the CellStage GameObject; call <see cref="Activate"/> / <see cref="Deactivate"/> from CellStage.OnEnter/OnExit.
    /// </summary>
    public class CellEnvironmentEventManager : MonoBehaviour
    {
        /// <summary>Minimum seconds between environment events (after the previous event ends).</summary>
        public float MinEventIntervalSeconds = 35f;

        /// <summary>Maximum seconds between environment events.</summary>
        public float MaxEventIntervalSeconds = 75f;

        /// <summary>Current active event; None when quiet.</summary>
        public CellEnvironmentEventType ActiveEvent { get; private set; } = CellEnvironmentEventType.None;

        /// <summary>Global metabolism multiplier applied during a VirusOutbreak. Defaults to 1 (no penalty).</summary>
        public float GlobalMetabolismMultiplier { get; private set; } = 1f;

        /// <summary>Global speed multiplier applied during a TemperatureChange. Defaults to 1 (no effect).</summary>
        public float GlobalSpeedMultiplier { get; private set; } = 1f;

        /// <summary>
        /// Global multiplier for photosynthetic passive gain (Organism/PlayerCell trait passives).
        /// OxygenSurge temporarily increases this above 1.
        /// </summary>
        public static float GlobalPhotosynthesisTraitMultiplier { get; private set; } = 1f;
        /// <summary>Seconds remaining for the currently active event (0 when none).</summary>
        public float ActiveEventRemainingSeconds => ActiveEvent == CellEnvironmentEventType.None
            ? 0f
            : Mathf.Max(0f, _activeEventEndsAt - Time.time);
        /// <summary>Number of active nutrient bloom hotspots available for AI steering.</summary>
        public int ActiveNutrientBloomHotspotCount => _activeBloomHotspots.Count;

        private FoodManager _foodManager;
        private PlayerCell  _player;
        private HostAuthorityController _authority;
        private Vector3 _arenaCenter;
        private float _arenaRadius;
        private float _nextEventTime;
        private float _activeRuntimeSeconds;
        private float _activeEventEndsAt;
        private bool _active;
        private readonly List<Vector3> _activeBloomHotspots = new List<Vector3>(8);

        /// <summary>Converts elapsed stage runtime to a clamped 0..1 nutrient-bloom progression value.</summary>
        public static float ComputeNutrientBloomProgression01(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f) return 0f;
            float max = Mathf.Max(1f, BalanceConstants.CellNutrientBloomProgressionSecondsToMax);
            return Mathf.Clamp01(elapsedSeconds / max);
        }

        /// <summary>Computes nutrient bloom spawn count from baseline food and progression.</summary>
        public static int ComputeNutrientBloomSpawnCount(
            int initialFoodCount,
            float progression01,
            float foodMultiplier = 1f)
        {
            int baseline = Mathf.Max(5, Mathf.RoundToInt(initialFoodCount * 0.3f));
            float clampedProgression = Mathf.Clamp01(progression01);
            float scale = Mathf.Lerp(1f, BalanceConstants.CellNutrientBloomMaxFoodScale, clampedProgression);
            float foodScale = Mathf.Max(0.5f, foodMultiplier);
            return Mathf.Max(5, Mathf.RoundToInt(baseline * scale * foodScale));
        }

        /// <summary>
        /// Copies active nutrient bloom hotspot positions into <paramref name="buffer"/>.
        /// Returns the number of copied hotspots.
        /// </summary>
        public int CopyNutrientBloomHotspots(List<Vector3> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            for (int i = 0; i < _activeBloomHotspots.Count; i++)
                buffer.Add(_activeBloomHotspots[i]);
            return buffer.Count;
        }

        /// <summary>
        /// Returns the nearest nutrient bloom hotspot when a bloom is active.
        /// Hotspot positions are in world space.
        /// </summary>
        public bool TryGetNearestNutrientBloomHotspot(Vector3 worldPosition, out Vector3 hotspotPosition)
        {
            hotspotPosition = Vector3.zero;
            if (ActiveEvent != CellEnvironmentEventType.NutrientBloom || _activeBloomHotspots.Count == 0)
                return false;

            Vector3 flatWorld = worldPosition;
            flatWorld.y = 0f;
            float bestDistSqr = float.MaxValue;
            int bestIndex = -1;
            for (int i = 0; i < _activeBloomHotspots.Count; i++)
            {
                Vector3 candidate = _activeBloomHotspots[i];
                candidate.y = 0f;
                float d = (candidate - flatWorld).sqrMagnitude;
                if (d < bestDistSqr)
                {
                    bestDistSqr = d;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0) return false;
            hotspotPosition = _activeBloomHotspots[bestIndex];
            return true;
        }

        /// <summary>Starts periodic event scheduling. Call from CellStage.OnEnter.</summary>
        public void Activate(FoodManager foodManager, float arenaRadius, PlayerCell player = null,
                             HostAuthorityController authority = null, Vector3 arenaCenter = default)
        {
            _foodManager = foodManager;
            _arenaRadius = arenaRadius;
            _arenaCenter = arenaCenter;
            _player      = player;
            _authority   = authority;
            _active = true;
            _activeRuntimeSeconds = 0f;
            _activeEventEndsAt = 0f;
            _activeBloomHotspots.Clear();
            GlobalPhotosynthesisTraitMultiplier = 1f;
            ScheduleNextEvent();
        }

        /// <summary>Stops events and resets state. Call from CellStage.OnExit.</summary>
        public void Deactivate()
        {
            _active = false;
            _authority = null;
            ActiveEvent = CellEnvironmentEventType.None;
            GlobalMetabolismMultiplier = 1f;
            GlobalSpeedMultiplier = 1f;
            GlobalPhotosynthesisTraitMultiplier = 1f;
            _activeRuntimeSeconds = 0f;
            _activeEventEndsAt = 0f;
            _activeBloomHotspots.Clear();
        }

        private void Update()
        {
            if (!_active) return;

            _activeRuntimeSeconds += Time.deltaTime;
            if (ActiveEvent != CellEnvironmentEventType.None) return;

            if (Time.time >= _nextEventTime)
                TriggerRandomEvent();
        }

        private void ScheduleNextEvent()
        {
            _nextEventTime = Time.time + Random.Range(MinEventIntervalSeconds, MaxEventIntervalSeconds);
        }

        private void TriggerRandomEvent()
        {
            if (_authority != null && !_authority.CanFireEnvironmentEvent()) return;

            // Randomly pick one of the active event types.
            var eventType = (CellEnvironmentEventType)Random.Range(1, 6); // 1-5
            float duration = eventType == CellEnvironmentEventType.RadiationSurge
                ? 5f
                : eventType == CellEnvironmentEventType.OxygenSurge
                    ? Random.Range(12f, 20f)
                    : Random.Range(15f, 30f);

            TryStartEvent(eventType, duration);
        }

        /// <summary>
        /// Starts a specific environment event immediately when no event is active.
        /// Returns false if the manager is inactive, another event is running, or authority blocks event spawning.
        /// </summary>
        public bool TryStartEvent(CellEnvironmentEventType eventType, float durationSeconds)
        {
            if (!_active || eventType == CellEnvironmentEventType.None)
                return false;
            if (ActiveEvent != CellEnvironmentEventType.None)
                return false;
            if (_authority != null && !_authority.CanFireEnvironmentEvent())
                return false;

            float duration = Mathf.Max(0.1f, durationSeconds);
            ActiveEvent = eventType;
            _activeEventEndsAt = Time.time + duration;
            EventBus.Publish(new CellEnvironmentEventTriggered
            {
                EventType = eventType,
                Started = true,
                DurationSeconds = duration
            });

            ApplyEventEffect(eventType);
            if (Application.isPlaying)
                StartCoroutine(EndEventAfter(eventType, duration));
            return true;
        }

        private void ApplyEventEffect(CellEnvironmentEventType eventType)
        {
            switch (eventType)
            {
                case CellEnvironmentEventType.NutrientBloom:
                    if (ActiveEvent == CellEnvironmentEventType.None)
                        ActiveEvent = CellEnvironmentEventType.NutrientBloom;

                    // Spawn clustered plant-food patches so players can "chase a bloom front"
                    // rather than getting a flat uniform sprinkle.
                    if (_foodManager != null)
                    {
                        float progression = ComputeNutrientBloomProgression01(_activeRuntimeSeconds);
                        float foodMultiplier = GameManager.HasInstance
                            ? GameManager.Instance.Modifiers.FoodSpawnMultiplier
                            : 1f;
                        int bloomCount = ComputeNutrientBloomSpawnCount(
                            _foodManager.InitialFoodCount,
                            progression,
                            foodMultiplier);

                        _activeBloomHotspots.Clear();
                        int clusterBonus = Mathf.RoundToInt(Mathf.Lerp(0f, 2f, progression));
                        int clusterCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Sqrt(bloomCount)) + clusterBonus, 2, 8);
                        int remaining = bloomCount;
                        for (int c = 0; c < clusterCount; c++)
                        {
                            int clustersLeft = clusterCount - c;
                            int thisClusterCount = Mathf.Max(1, Mathf.RoundToInt(remaining / (float)clustersLeft));
                            remaining -= thisClusterCount;

                            Vector3 clusterCenter = ClampToArena(GetRandomPointInArenaRing(0.35f, 0.92f));
                            _activeBloomHotspots.Add(clusterCenter);
                            float minRadiusFrac = BalanceConstants.CellNutrientBloomClusterRadiusFractionMin;
                            float maxRadiusFrac = Mathf.Lerp(
                                minRadiusFrac,
                                BalanceConstants.CellNutrientBloomClusterRadiusFractionMax,
                                progression);
                            float clusterRadius = Mathf.Max(1.25f, _arenaRadius * Random.Range(minRadiusFrac, maxRadiusFrac));
                            for (int i = 0; i < thisClusterCount; i++)
                            {
                                Vector2 offset = Random.insideUnitCircle * clusterRadius;
                                Vector3 pos = ClampToArena(clusterCenter + new Vector3(offset.x, 0f, offset.y));
                                float nutrition = Random.Range(5.5f, 7.5f);
                                _foodManager.AddFoodAt(pos, FoodType.Plant, nutrition);
                            }
                        }
                    }
                    break;

                case CellEnvironmentEventType.VirusOutbreak:
                    // Increase global metabolism cost AND spawn visible viral particles
                    GlobalMetabolismMultiplier = 1.4f;
                    SpawnViralParticles();
                    break;

                case CellEnvironmentEventType.TemperatureChange:
                    // Apply speed penalty (cold snap) or boost (thermal surge) randomly
                    GlobalSpeedMultiplier = Random.value > 0.5f ? 0.65f : 1.35f;
                    break;

                case CellEnvironmentEventType.RadiationSurge:
                    // No global multiplier — CellStage reacts to CellEnvironmentEventTriggered and
                    // calls PlayerCell.OfferRadiationMutation() directly.
                    break;

                case CellEnvironmentEventType.OxygenSurge:
                    GlobalPhotosynthesisTraitMultiplier = 2.4f;
                    break;
            }
        }

        private void SpawnViralParticles()
        {
            int count = BalanceConstants.ViralParticleSpawnCount;
            // Spawn initial target: the player if available, otherwise random position
            Transform initialTarget = _player != null ? _player.transform : null;

            for (int i = 0; i < count; i++)
            {
                // Scatter spawn points around the arena edge so they drift inward
                float angle  = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
                float radius = _arenaRadius * Random.Range(0.6f, 0.9f);
                var spawnPos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                var viralGenome = new Genome
                {
                    Speed = 6.5f,
                    Size = 0.75f,
                    Attack = 4.5f,
                    Defence = 0.8f,
                    Digestion = 0.5f,
                    MetabolismCost = 0.6f,
                    Spikes = 4.2f,
                    Flagella = 7.5f,
                    Color = new Color(0.9f, 0.1f, 0.85f),
                    Traits = GeneticTraits.Toxic
                };
                var go = ProceduralCellGenerator.CreateCellFromGenome(spawnPos, viralGenome);
                if (go == null)
                {
                    Debug.LogWarning("Virus outbreak spawn skipped: ProceduralCellGenerator.CreateCellFromGenome returned null.");
                    continue;
                }

                go.name = "ViralParticle";

                var rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = go.AddComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogWarning("Virus outbreak spawn skipped: could not attach Rigidbody to ViralParticle.");
                    Destroy(go);
                    continue;
                }
                rb.useGravity  = false;
                rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

                var col = go.GetComponent<SphereCollider>();
                if (col == null)
                    col = go.AddComponent<SphereCollider>();
                if (col == null)
                {
                    Debug.LogWarning("Virus outbreak spawn skipped: could not attach SphereCollider to ViralParticle.");
                    Destroy(go);
                    continue;
                }
                col.isTrigger = true;

                var vp = go.AddComponent<ViralParticle>();
                vp.Initialize(_player, initialTarget);
            }
        }

        private System.Collections.IEnumerator EndEventAfter(CellEnvironmentEventType eventType, float duration)
        {
            yield return new WaitForSeconds(duration);

            // Restore defaults
            GlobalMetabolismMultiplier = 1f;
            GlobalSpeedMultiplier = 1f;
            GlobalPhotosynthesisTraitMultiplier = 1f;
            _activeEventEndsAt = 0f;
            if (eventType == CellEnvironmentEventType.NutrientBloom)
                _activeBloomHotspots.Clear();
            ActiveEvent = CellEnvironmentEventType.None;

            EventBus.Publish(new CellEnvironmentEventTriggered
            {
                EventType = eventType,
                Started = false,
                DurationSeconds = 0f
            });

            ScheduleNextEvent();
        }

        private Vector3 GetRandomPointInArenaRing(float minRadiusFraction, float maxRadiusFraction)
        {
            float minFraction = Mathf.Clamp01(minRadiusFraction);
            float maxFraction = Mathf.Clamp(maxRadiusFraction, minFraction, 1f);
            float minRadius = _arenaRadius * minFraction;
            float maxRadius = _arenaRadius * maxFraction;
            float radius = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return _arenaCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private Vector3 ClampToArena(Vector3 worldPosition)
        {
            Vector3 flat = worldPosition - _arenaCenter;
            flat.y = 0f;
            float maxRadius = Mathf.Max(0.01f, _arenaRadius);
            if (flat.sqrMagnitude > maxRadius * maxRadius)
                flat = flat.normalized * maxRadius;
            return _arenaCenter + flat;
        }
    }
}

