using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Utils;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Spawns algae, bacteria, and dead cells; keeps track of positions for AI (GetNearestFood). Step 3 cell-stage evolution.
    /// </summary>
    public class FoodManager : MonoBehaviour
    {
        private const float SpawnMinSeparation = 0.48f;
        private const int SpawnPositionAttempts = 10;

        [Header("Spawn counts (used by SpawnInArena)")]
        [SerializeField] private int _algaeCount = 72;
        [SerializeField] private int _bacteriaCount = 28;
        [SerializeField] private int _deadCellCount = 10;

        [Header("Default nutrition per type")]
        [SerializeField] private float _algaeNutrition = 5.5f;
        [SerializeField] private float _bacteriaNutrition = 8.5f;
        [SerializeField] private float _deadCellNutrition = 10f;

        private readonly FoodTracker _tracker = new FoodTracker();
        private readonly Dictionary<int, GameObject> _idToGameObject = new Dictionary<int, GameObject>();
        private CellSpatialGrid _spatialGrid;
        private Vector3 _arenaCenter = Vector3.zero;

        /// <summary>Number of food entries currently tracked.</summary>
        public int FoodCount => _tracker.Count;

        /// <summary>Base food count (algae + bacteria + dead cells) before balance scaling.</summary>
        public int InitialFoodCount => _algaeCount + _bacteriaCount + _deadCellCount;

        /// <summary>Optional spatial grid for faster GetNearestFood (Step 9). Set by CellStage.</summary>
        public void SetSpatialGrid(CellSpatialGrid grid)
        {
            _spatialGrid = grid;
        }

        public void SpawnInArena(float arenaRadius, int totalFoodOverride = 0)
        {
            SpawnInArena(Vector3.zero, arenaRadius, totalFoodOverride);
        }

        /// <summary>
        /// Spawn algae (Plant), bacteria (Plant), and dead cells (Meat) in the arena around <paramref name="arenaCenter"/>.
        /// Clears existing tracked food. Optional <paramref name="totalFoodOverride"/> scales counts proportionally.
        /// </summary>
        public void SpawnInArena(Vector3 arenaCenter, float arenaRadius, int totalFoodOverride = 0)
        {
            _arenaCenter = arenaCenter;
            Clear();
            int algae    = _algaeCount;
            int bacteria = _bacteriaCount;
            int dead     = _deadCellCount;

            if (totalFoodOverride > 0 && InitialFoodCount > 0)
            {
                float scale = totalFoodOverride / (float)InitialFoodCount;
                algae    = Mathf.Max(1, Mathf.RoundToInt(_algaeCount    * scale));
                bacteria = Mathf.Max(1, Mathf.RoundToInt(_bacteriaCount * scale));
                dead     = Mathf.Max(0, Mathf.RoundToInt(_deadCellCount * scale));
            }

            float deepMax     = arenaRadius * BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction;
            float openWaterMax = arenaRadius * BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction;
            float algaeMax    = arenaRadius * BalanceConstants.CellBiomeAlgaeForestMaxRadiusFraction;

            // Plant food is concentrated by biome type to encourage movement and zone control.
            SpawnGroup(algae,    FoodType.Plant, _algaeNutrition,    arenaRadius, AccessibilityPalette.GetFoodColor(FoodType.Plant), "Algae",  openWaterMax, algaeMax, "cell_food_algae");
            SpawnGroup(bacteria, FoodType.Plant, _bacteriaNutrition, arenaRadius, AccessibilityPalette.GetBacteriaColor(AccessibilityPalette.GetCurrentMode()), "Bacteria", deepMax, openWaterMax, "cell_food_bacteria");
            SpawnGroup(dead,     FoodType.Meat,  _deadCellNutrition, arenaRadius, AccessibilityPalette.GetFoodColor(FoodType.Meat), "DeadCell", openWaterMax * 0.75f, arenaRadius, "cell_food_meat");
        }

        /// <summary>Add one food at a position (e.g. for environmental manipulation). Returns the food's manager id.</summary>
        public int AddFoodAt(Vector3 position, FoodType type = FoodType.Plant, float nutrition = 5f)
        {
            GameObject go = CreateFoodObject(position, type, nutrition, $"{GetFoodNamePrefix(type)}_manual_{_tracker.Count}");
            int id = _tracker.Add(position, type, nutrition);
            go.GetComponent<FoodItem>().SetFoodManagerId(id);
            _idToGameObject[id] = go;
            if (_spatialGrid != null)
                _spatialGrid.Register(id, position, CellSpatialGrid.Category.Food);
            return id;
        }

        public bool HasFood(int id)
        {
            return id > 0 && _idToGameObject.TryGetValue(id, out var go) && go != null && _tracker.Contains(id);
        }

        public int CopyFoodIds(List<int> destination)
        {
            if (destination == null)
                return 0;

            destination.Clear();
            foreach (var kv in _idToGameObject)
            {
                if (kv.Key > 0 && kv.Value != null)
                    destination.Add(kv.Key);
            }

            return destination.Count;
        }

        public int AddOrUpdateNetworkFood(int id, Vector3 position, FoodType type, float nutrition = 0f)
        {
            if (id <= 0)
                return AddFoodAt(position, type, nutrition > 0f ? nutrition : GetDefaultNutrition(type));

            float resolvedNutrition = nutrition > 0f ? nutrition : GetDefaultNutrition(type);
            if (_spatialGrid != null)
                _spatialGrid.Unregister(id);

            if (_idToGameObject.TryGetValue(id, out GameObject existing) && existing != null)
            {
                existing.transform.position = position;
                var item = existing.GetComponent<FoodItem>() ?? existing.AddComponent<FoodItem>();
                item.Initialize(type, resolvedNutrition);
                item.SetFoodManagerId(id);
                item.EnsureTriggerColliders();
            }
            else
            {
                if (existing == null)
                    _idToGameObject.Remove(id);

                var go = CreateFoodObject(position, type, resolvedNutrition, $"{GetFoodNamePrefix(type)}_net_{id}");
                go.GetComponent<FoodItem>().SetFoodManagerId(id);
                _idToGameObject[id] = go;
            }

            _tracker.AddWithId(id, position, type, resolvedNutrition);
            if (_spatialGrid != null)
                _spatialGrid.Register(id, position, CellSpatialGrid.Category.Food);
            return id;
        }

        private void SpawnGroup(
            int count,
            FoodType type,
            float nutrition,
            float arenaRadius,
            Color color,
            string namePrefix,
            float minRadius = 0f,
            float maxRadius = float.NaN,
            string prefabName = "cell_food_algae")
        {
            if (float.IsNaN(maxRadius)) maxRadius = arenaRadius;
            minRadius = Mathf.Max(0f, minRadius);
            maxRadius = Mathf.Clamp(maxRadius, minRadius, arenaRadius);

            for (int i = 0; i < count; i++)
            {
                var pos = GetSpawnPointInRing(minRadius, maxRadius, SpawnMinSeparation);
                GameObject go = CreateFoodObject(pos, type, nutrition, $"{namePrefix}_{i}", color, prefabName);
                var foodItem = go.GetComponent<FoodItem>();
                int id = _tracker.Add(pos, type, nutrition);
                foodItem.SetFoodManagerId(id);
                _idToGameObject[id] = go;
                if (_spatialGrid != null)
                    _spatialGrid.Register(id, pos, CellSpatialGrid.Category.Food);
            }
        }

        private GameObject CreateFoodObject(
            Vector3 position,
            FoodType type,
            float nutrition,
            string objectName,
            Color? overrideColor = null,
            string overridePrefabName = null)
        {
            Color color = overrideColor ?? AccessibilityPalette.GetFoodColor(type);
            string prefabName = string.IsNullOrWhiteSpace(overridePrefabName)
                ? GetPrefabName(type)
                : overridePrefabName;
            GameObject go = ModelFactory.CreateCellFood(position, color, 0.15f, prefabName);
            go.name = objectName;
            var foodItem = go.GetComponent<FoodItem>() ?? go.AddComponent<FoodItem>();
            foodItem.Initialize(type, nutrition);
            foodItem.EnsureTriggerColliders();
            return go;
        }

        private string GetFoodNamePrefix(FoodType type)
        {
            return type == FoodType.Meat ? "DeadCell" : type == FoodType.Plant ? "Algae" : "Toxin";
        }

        private string GetPrefabName(FoodType type)
        {
            return type == FoodType.Meat
                ? "cell_food_meat"
                : type == FoodType.Toxin ? "cell_food_bacteria" : "cell_food_algae";
        }

        private float GetDefaultNutrition(FoodType type)
        {
            return type == FoodType.Meat
                ? _deadCellNutrition
                : type == FoodType.Toxin ? _bacteriaNutrition : _algaeNutrition;
        }

        private Vector3 GetSpawnPointInRing(float minRadius, float maxRadius, float minSeparation)
        {
            Vector3 fallback = GetRandomPointInRing(minRadius, maxRadius);
            if (_tracker.Count == 0 || minSeparation <= 0f)
                return fallback;

            float bestNearestSq = -1f;
            for (int attempt = 0; attempt < SpawnPositionAttempts; attempt++)
            {
                Vector3 candidate = GetRandomPointInRing(minRadius, maxRadius);
                var nearest = _tracker.GetNearest(candidate, minSeparation);
                if (!nearest.HasValue)
                    return candidate;

                float sq = (candidate - nearest.Value.Position).sqrMagnitude;
                if (sq > bestNearestSq)
                {
                    bestNearestSq = sq;
                    fallback = candidate;
                }
            }

            return fallback;
        }

        private Vector3 GetRandomPointInRing(float minRadius, float maxRadius)
        {
            float r = Mathf.Sqrt(Random.Range(minRadius * minRadius, maxRadius * maxRadius));
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return _arenaCenter + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        }

        /// <summary>Nearest food within maxDistance for AI. Null if none. Uses spatial grid when set (Step 9).</summary>
        public FoodNearestResult? GetNearestFood(Vector3 position, float maxDistance)
        {
            if (_spatialGrid != null)
            {
                var nearest = _spatialGrid.GetNearestInRadius(position, maxDistance, CellSpatialGrid.Category.Food);
                if (nearest.HasValue)
                {
                    var entry = _tracker.Get((int)nearest.Value.Id);
                    if (entry.HasValue)
                        return new FoodNearestResult { Position = entry.Value.Position, Id = (int)nearest.Value.Id, Type = entry.Value.Type, Nutrition = entry.Value.Nutrition };
                }
                return null;
            }
            return _tracker.GetNearest(position, maxDistance);
        }

        /// <summary>
        /// Nearest food of a specific type within maxDistance for selective feeders (e.g. ScavengerCell
        /// targeting Meat only). Does not use the spatial grid (type-unaware); acceptable for sparse food types.
        /// </summary>
        public FoodNearestResult? GetNearestFoodOfType(Vector3 position, float maxDistance, FoodType type)
        {
            return _tracker.GetNearestOfType(position, maxDistance, type);
        }

        /// <summary>Remove and destroy food by id (e.g. when consumed). Returns true if found.</summary>
        public bool ConsumeFood(int id)
        {
            if (!_tracker.Remove(id)) return false;
            if (_spatialGrid != null)
                _spatialGrid.Unregister(id);
            if (_idToGameObject.TryGetValue(id, out GameObject go))
            {
                _idToGameObject.Remove(id);
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }
            return true;
        }

        /// <summary>Remove all tracked food and destroy GameObjects.</summary>
        public void Clear()
        {
            if (_spatialGrid != null)
            {
                foreach (int id in new List<int>(_idToGameObject.Keys))
                    _spatialGrid.Unregister(id);
            }
            foreach (var go in _idToGameObject.Values)
            {
                if (go != null)
                {
                    if (Application.isPlaying)
                        Destroy(go);
                    else
                        DestroyImmediate(go);
                }
            }
            _idToGameObject.Clear();
            _tracker.Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
