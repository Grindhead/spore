using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// 2D spatial partitioning for the cell arena. Reduces O(n) scans to nearby cells for AI (nearest food, threat, prey).
    /// Step 9 of cell-stage evolution. Aligns with memory-bank/cellStageImplementationSteps.md and evolutionSimulationDesign.md.
    /// </summary>
    public class CellSpatialGrid
    {
        /// <summary>Category for grid entries (used to query only food or only organisms).</summary>
        public enum Category
        {
            Food = 0,
            Organism = 1
        }

        private readonly Vector3 _center;
        private readonly float _halfSize;
        private readonly float _cellSize;
        private readonly Dictionary<long, (Vector3 Position, Category Cat)> _idToEntry = new Dictionary<long, (Vector3, Category)>();
        private readonly Dictionary<Vector2Int, List<long>> _cellToIds = new Dictionary<Vector2Int, List<long>>();

        /// <summary>Creates a grid covering [center - halfSize, center + halfSize] on X and Z. Y is ignored for bucketing.</summary>
        public CellSpatialGrid(Vector3 center, float halfSize, float cellSize)
        {
            _center = center;
            _halfSize = Mathf.Max(0.1f, halfSize);
            _cellSize = Mathf.Max(0.5f, cellSize);
        }

        private Vector2Int CellKey(Vector3 worldPos)
        {
            float x = (worldPos.x - (_center.x - _halfSize)) / _cellSize;
            float z = (worldPos.z - (_center.z - _halfSize)) / _cellSize;
            int cx = Mathf.FloorToInt(x);
            int cz = Mathf.FloorToInt(z);
            return new Vector2Int(cx, cz);
        }

        private void AddToCell(Vector2Int key, long id)
        {
            if (!_cellToIds.TryGetValue(key, out var list))
            {
                list = new List<long>(4);
                _cellToIds[key] = list;
            }
            if (!list.Contains(id))
                list.Add(id);
        }

        private void RemoveFromCell(Vector2Int key, long id)
        {
            if (!_cellToIds.TryGetValue(key, out var list)) return;
            list.Remove(id);
            if (list.Count == 0)
                _cellToIds.Remove(key);
        }

        /// <summary>Register or update position for an id. Use same id for UpdatePosition later.</summary>
        public void Register(long id, Vector3 position, Category category)
        {
            if (_idToEntry.TryGetValue(id, out var existing))
            {
                RemoveFromCell(CellKey(existing.Position), id);
            }

            _idToEntry[id] = (position, category);
            AddToCell(CellKey(position), id);
        }

        /// <summary>Update position for an already registered id.</summary>
        public void UpdatePosition(long id, Vector3 position)
        {
            if (!_idToEntry.TryGetValue(id, out var existing))
                return;

            var oldKey = CellKey(existing.Position);
            var newKey = CellKey(position);
            if (oldKey != newKey)
            {
                RemoveFromCell(oldKey, id);
                AddToCell(newKey, id);
            }
            _idToEntry[id] = (position, existing.Cat);
        }

        /// <summary>Remove an id from the grid.</summary>
        public bool Unregister(long id)
        {
            if (!_idToEntry.TryGetValue(id, out var existing))
                return false;

            RemoveFromCell(CellKey(existing.Position), id);
            _idToEntry.Remove(id);
            return true;
        }

        /// <summary>Get all entries within radius of position, optionally filtered by category. Y is ignored for distance.</summary>
        public IReadOnlyList<(long Id, Vector3 Position)> GetInRadius(Vector3 position, float radius, Category? category = null)
        {
            position.y = 0f;
            float radiusSq = radius * radius;
            int cellRadius = Mathf.Max(1, Mathf.CeilToInt(radius / _cellSize));
            var centerKey = CellKey(position);
            var results = new List<(long, Vector3)>(8);

            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    var key = new Vector2Int(centerKey.x + dx, centerKey.y + dz);
                    if (!_cellToIds.TryGetValue(key, out var ids))
                        continue;

                    for (int i = 0; i < ids.Count; i++)
                    {
                        long id = ids[i];
                        if (!_idToEntry.TryGetValue(id, out var entry))
                            continue;
                        if (category.HasValue && entry.Cat != category.Value)
                            continue;

                        Vector3 p = entry.Position;
                        float deltaX = position.x - p.x;
                        float deltaZ = position.z - p.z;
                        float sq = deltaX * deltaX + deltaZ * deltaZ;
                        if (sq <= radiusSq)
                            results.Add((id, entry.Position));
                    }
                }
            }

            return results;
        }

        /// <summary>Nearest entry within radius, optionally by category. Returns null if none.</summary>
        public (long Id, Vector3 Position)? GetNearestInRadius(Vector3 position, float radius, Category? category = null)
        {
            position.y = 0f;
            float radiusSq = radius * radius;
            float bestSq = radiusSq;
            long bestId = 0;
            Vector3 bestPosition = default;
            bool found = false;
            int cellRadius = Mathf.Max(1, Mathf.CeilToInt(radius / _cellSize));
            var centerKey = CellKey(position);

            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    var key = new Vector2Int(centerKey.x + dx, centerKey.y + dz);
                    if (!_cellToIds.TryGetValue(key, out var ids))
                        continue;

                    for (int i = 0; i < ids.Count; i++)
                    {
                        long id = ids[i];
                        if (!_idToEntry.TryGetValue(id, out var entry))
                            continue;
                        if (category.HasValue && entry.Cat != category.Value)
                            continue;

                        Vector3 p = entry.Position;
                        float deltaX = position.x - p.x;
                        float deltaZ = position.z - p.z;
                        float sq = deltaX * deltaX + deltaZ * deltaZ;
                        if (sq <= bestSq)
                        {
                            bestSq = sq;
                            bestId = id;
                            bestPosition = entry.Position;
                            found = true;
                        }
                    }
                }
            }

            return found ? (bestId, bestPosition) : null;
        }

        /// <summary>Number of registered entries (for tests).</summary>
        public int Count => _idToEntry.Count;

        /// <summary>Clear all entries.</summary>
        public void Clear()
        {
            _idToEntry.Clear();
            _cellToIds.Clear();
        }
    }
}
