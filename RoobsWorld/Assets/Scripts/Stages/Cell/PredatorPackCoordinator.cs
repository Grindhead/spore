using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Static service that coordinates multiple <see cref="PredatorCell"/> instances during a chase.
    /// When two or more predators are simultaneously chasing the player, assigns flanking roles
    /// so they approach from different angles instead of all rushing straight in.
    ///
    /// Design: leader (first registered chaser) targets the player directly;
    /// secondary chasers are offset +/-90 degrees perpendicular to the leader-to-player axis.
    /// </summary>
    public static class PredatorPackCoordinator
    {
        /// <summary>Lateral offset (world units) for non-leader flankers.</summary>
        public const float FlankOffset = 5f;

        private static readonly List<PredatorCell> _registered = new List<PredatorCell>();
        private static readonly List<PredatorCell> _chasersBuffer = new List<PredatorCell>(16);

        // ------------------------------------------------------------------ Registration

        public static void Register(PredatorCell predator)
        {
            if (predator != null && !_registered.Contains(predator))
                _registered.Add(predator);
        }

        public static void Unregister(PredatorCell predator)
        {
            _registered.Remove(predator);
        }

        /// <summary>
        /// Copies currently active registered predators into <paramref name="buffer"/> and returns the count.
        /// Null or destroyed references are compacted out of the registry.
        /// </summary>
        public static int CopyRegisteredPredators(List<PredatorCell> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();
            CompactRegistry();
            var scenePredators = Object.FindObjectsByType<PredatorCell>(FindObjectsInactive.Exclude);
            for (int i = 0; i < scenePredators.Length; i++)
            {
                var predator = scenePredators[i];
                if (predator != null && !_registered.Contains(predator))
                    _registered.Add(predator);
            }

            for (int i = 0; i < _registered.Count; i++)
            {
                var predator = _registered[i];
                if (predator != null && predator.isActiveAndEnabled)
                    buffer.Add(predator);
            }

            return buffer.Count;
        }

        private static void CompactRegistry()
        {
            for (int i = _registered.Count - 1; i >= 0; i--)
            {
                if (_registered[i] == null)
                    _registered.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------------ Flank targeting

        /// <summary>
        /// Returns the ideal move target for <paramref name="self"/> when chasing <paramref name="playerPos"/>.
        /// Returns <paramref name="playerPos"/> directly when fewer than 2 predators are chasing.
        /// Otherwise the first registered chaser is the leader (goes straight) and subsequent chasers
        /// flank alternately left and right.
        /// </summary>
        public static Vector3 GetFlankTarget(PredatorCell self, Vector3 playerPos)
        {
            CompactRegistry();

            _chasersBuffer.Clear();
            foreach (var predator in _registered)
            {
                if (predator != null && predator.isActiveAndEnabled && predator.IsChasing)
                    _chasersBuffer.Add(predator);
            }

            if (_chasersBuffer.Count < 2)
                return playerPos;

            int myIndex = _chasersBuffer.IndexOf(self);
            if (myIndex <= 0)
                return playerPos; // leader or unregistered -> direct chase

            // Compute flank offset relative to the first chaser (leader)
            Vector3 offset = ComputeFlankOffset(_chasersBuffer[0].transform.position, playerPos, myIndex);
            return playerPos + offset;
        }

        /// <summary>
        /// Pure math helper: computes the flanking offset for a predator at <paramref name="index"/>
        /// (index 1, 2, 3 ...) given the leader's position and the player's position.
        /// Index 1 flanks right, index 2 flanks left, index 3 flanks right, etc.
        /// </summary>
        public static Vector3 ComputeFlankOffset(Vector3 leaderPos, Vector3 playerPos, int index)
        {
            var toPlayer = playerPos - leaderPos;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f)
                return Vector3.zero;

            toPlayer.Normalize();
            var perp = Vector3.Cross(toPlayer, Vector3.up).normalized;
            float sign = (index % 2 == 1) ? 1f : -1f; // odd -> right, even -> left
            return perp * sign * FlankOffset;
        }
    }
}
