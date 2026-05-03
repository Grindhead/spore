using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Result of GetNearest: position, nutrition, type, and id for removal. Step 3 cell-stage evolution.
    /// </summary>
    public struct FoodNearestResult
    {
        public Vector3 Position;
        public float Nutrition;
        public FoodType Type;
        public int Id;
    }

    /// <summary>
    /// Pure C# tracker of food positions and nutrition for AI (GetNearest). No GameObjects. Step 3 cell-stage evolution.
    /// </summary>
    public class FoodTracker
    {
        private readonly List<(int Id, Vector3 Position, FoodType Type, float Nutrition)> _entries = new List<(int, Vector3, FoodType, float)>();
        private int _nextId = 1;

        public int Count => _entries.Count;

        /// <summary>Add a food entry. Returns id for later Remove.</summary>
        public int Add(Vector3 position, FoodType type, float nutrition)
        {
            int id = _nextId++;
            _entries.Add((id, position, type, nutrition));
            return id;
        }

        /// <summary>Remove food by id. Returns true if found and removed.</summary>
        public bool Remove(int id)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Id == id)
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Get entry by id (for spatial grid lookup). Returns null if not found.</summary>
        public (Vector3 Position, FoodType Type, float Nutrition)? Get(int id)
        {
            foreach (var e in _entries)
            {
                if (e.Id == id)
                    return (e.Position, e.Type, e.Nutrition);
            }
            return null;
        }

        /// <summary>Nearest food within maxDistance, or null if none. Ties broken by first found.</summary>
        public FoodNearestResult? GetNearest(Vector3 position, float maxDistance)
        {
            position.y = 0f;
            float maxSq = maxDistance * maxDistance;
            int best = -1;
            float bestSq = maxSq;

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                Vector3 p = e.Position;
                p.y = 0f;
                float sq = (position - p).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }

            if (best < 0) return null;

            var entry = _entries[best];
            return new FoodNearestResult
            {
                Position = entry.Position,
                Nutrition = entry.Nutrition,
                Type = entry.Type,
                Id = entry.Id
            };
        }

        /// <summary>
        /// Nearest food of a specific type within maxDistance, or null if none.
        /// Used by ScavengerCell to target dead-cell (Meat) food specifically.
        /// </summary>
        public FoodNearestResult? GetNearestOfType(Vector3 position, float maxDistance, FoodType type)
        {
            position.y = 0f;
            float maxSq  = maxDistance * maxDistance;
            int   best   = -1;
            float bestSq = maxSq;

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.Type != type) continue;
                Vector3 p = e.Position;
                p.y = 0f;
                float sq = (position - p).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best   = i;
                }
            }

            if (best < 0) return null;
            var entry = _entries[best];
            return new FoodNearestResult
            {
                Position  = entry.Position,
                Nutrition = entry.Nutrition,
                Type      = entry.Type,
                Id        = entry.Id
            };
        }

        /// <summary>Clear all entries. Id counter is unchanged.</summary>
        public void Clear()
        {
            _entries.Clear();
        }
    }
}
