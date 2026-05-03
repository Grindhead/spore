using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Creature.AI
{
    /// <summary>
    /// Lightweight planar movement memory used by creature AI to avoid pacing over the same cells.
    /// </summary>
    public static class CreatureMovementMemory
    {
        public static Vector2Int QuantizePlanar(Vector3 position, float cellSize)
        {
            float safeCellSize = Mathf.Max(0.1f, cellSize);
            return new Vector2Int(
                Mathf.RoundToInt(position.x / safeCellSize),
                Mathf.RoundToInt(position.z / safeCellSize));
        }

        public static void RecordRecentLocation(
            Queue<Vector2Int> recentCells,
            Vector3 position,
            float cellSize,
            int capacity)
        {
            if (recentCells == null)
                return;

            int safeCapacity = Mathf.Max(1, capacity);
            Vector2Int cell = QuantizePlanar(position, cellSize);
            if (recentCells.Count == 0 || GetLastCell(recentCells) != cell)
                recentCells.Enqueue(cell);

            while (recentCells.Count > safeCapacity)
                recentCells.Dequeue();
        }

        public static Vector3 SelectLeastVisitedCandidate(
            Vector3 currentPosition,
            IReadOnlyList<Vector3> candidates,
            IEnumerable<Vector2Int> recentCells,
            float cellSize,
            float minTravelDistance)
        {
            if (candidates == null || candidates.Count == 0)
                return currentPosition;

            Vector3 best = candidates[0];
            float bestScore = float.NegativeInfinity;
            float safeMinTravel = Mathf.Max(0f, minTravelDistance);

            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 candidate = candidates[i];
                float travelDistance = HorizontalDistance(currentPosition, candidate);
                float score = ComputeNoveltyScore(candidate, recentCells, cellSize) + travelDistance * 0.15f;
                if (travelDistance < safeMinTravel)
                    score -= (safeMinTravel - travelDistance) * 4f;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        public static float ComputeNoveltyScore(
            Vector3 candidate,
            IEnumerable<Vector2Int> recentCells,
            float cellSize)
        {
            if (recentCells == null)
                return 100f;

            Vector2Int candidateCell = QuantizePlanar(candidate, cellSize);
            float nearestCellDistance = float.MaxValue;
            var orderedCells = new List<Vector2Int>();

            foreach (Vector2Int recentCell in recentCells)
                orderedCells.Add(recentCell);

            for (int i = 0; i < orderedCells.Count; i++)
            {
                Vector2Int recentCell = orderedCells[i];
                int age = orderedCells.Count - 1 - i;
                float cellDistance = Vector2Int.Distance(candidateCell, recentCell);
                float recencyPenalty = Mathf.Max(0f, 8f - age) * 0.25f;
                nearestCellDistance = Mathf.Min(nearestCellDistance, cellDistance - recencyPenalty);
            }

            return nearestCellDistance == float.MaxValue ? 100f : nearestCellDistance;
        }

        private static Vector2Int GetLastCell(Queue<Vector2Int> cells)
        {
            Vector2Int last = default;
            foreach (Vector2Int cell in cells)
                last = cell;
            return last;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
