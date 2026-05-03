using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Cell-stage movement: applies speed from genome and adds small wobble/pulsation for fluid feel.
    /// Step 2 of cell-stage evolution roadmap. Used by AI or player to get move direction and speed.
    /// </summary>
    public static class Movement
    {
        /// <summary>Scale factor applied to genome Speed for world units per second (matches Organism.Move).</summary>
        public const float SpeedScale = 5f;

        /// <summary>Base wobble amplitude; scaled by genome Flagella for more flagella = more wobble.</summary>
        public const float WobbleAmplitudeBase = 0.2f;

        /// <summary>Effective movement speed from genome (world units per second).</summary>
        public static float GetSpeed(Genome genome)
        {
            if (genome == null) return SpeedScale;
            return genome.Speed * SpeedScale;
        }

        /// <summary>
        /// Applies a small wobble to the movement direction for organic feel.
        /// Wobble is in the XZ plane; magnitude scales with genome Flagella and varies with time.
        /// </summary>
        /// <param name="direction">Desired move direction (XZ); will be normalized.</param>
        /// <param name="time">Time value for deterministic wobble (e.g. Time.time).</param>
        /// <param name="genome">Genome for Flagella-based wobble amount; null uses zero wobble.</param>
        /// <returns>Normalized direction with wobble applied.</returns>
        public static Vector3 ApplyWobble(Vector3 direction, float time, Genome genome)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Vector3.forward;

            direction.Normalize();
            float wobble = WobbleAmplitudeBase * (genome != null ? Mathf.Clamp01(genome.Flagella / 10f) : 0f);
            if (wobble <= 0.0001f)
                return direction;

            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            float s = Mathf.Sin(time * 3.4f) * wobble;
            float c = Mathf.Cos(time * 2.6f) * wobble * 0.55f;
            Vector3 withWobble = direction + right * s + direction * c;
            withWobble.y = 0f;
            if (withWobble.sqrMagnitude < 0.0001f)
                return direction;
            return withWobble.normalized;
        }
    }
}
