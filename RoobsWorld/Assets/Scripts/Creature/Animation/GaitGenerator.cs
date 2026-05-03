namespace RoobsWorld.Creature.Animation
{
    /// <summary>
    /// Logical side of a limb used for phasing gaits.
    /// </summary>
    public enum LimbSide { Left, Right }

    /// <summary>
    /// Configuration for a single limb participating in procedural locomotion.
    /// </summary>
    public struct LimbConfig
    {
        public int LimbIndex;
        public LimbSide Side;
        public int BodySegment;
    }

    /// <summary>
    /// Generated gait parameters for a set of limbs (per-limb phase, step height, stride timing).
    /// </summary>
    public struct GaitData
    {
        public float[] LimbPhases;
        public float StepHeight;
        public float StrideDuration;
        public float StrideLength;
    }

    /// <summary>
    /// Produces simple alternating gaits for bipeds/quadru­peds/etc. based on limb side and body segment index.
    /// </summary>
    public static class GaitGenerator
    {
        private const float DefaultStepHeight = 0.3f;
        private const float DefaultStrideDuration = 0.5f;
        private const float DefaultStrideLength = 1.2f;

        public static GaitData Generate(LimbConfig[] limbs)
        {
            if (limbs == null || limbs.Length == 0)
            {
                return new GaitData
                {
                    LimbPhases = new float[0],
                    StepHeight = DefaultStepHeight,
                    StrideDuration = DefaultStrideDuration,
                    StrideLength = DefaultStrideLength
                };
            }

            var phases = new float[limbs.Length];

            for (int i = 0; i < limbs.Length; i++)
            {
                bool isOddSegment = limbs[i].BodySegment % 2 != 0;
                bool isLeft = limbs[i].Side == LimbSide.Left;

                // Alternating diagonal pattern:
                // Left on even segment = group A (phase 0)
                // Right on even segment = group B (phase 0.5)
                // Left on odd segment = group B (phase 0.5)
                // Right on odd segment = group A (phase 0)
                bool groupA = isLeft ^ isOddSegment;
                phases[i] = groupA ? 0f : 0.5f;
            }

            return new GaitData
            {
                LimbPhases = phases,
                StepHeight = DefaultStepHeight,
                StrideDuration = DefaultStrideDuration,
                StrideLength = DefaultStrideLength
            };
        }
    }
}
