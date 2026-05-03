using System;
using System.Collections.Generic;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Builds a pickup plan that guarantees part coverage before adding random duplicates.
    /// This keeps "collect all parts" progression attainable in normal runs.
    /// </summary>
    public static class CellPartPickupPlanner
    {
        public static List<CellPartType> BuildPlan(int pickupCount, Random rng = null)
        {
            if (pickupCount <= 0)
                return new List<CellPartType>();

            rng ??= new Random();

            var allTypes = new List<CellPartType>();
            foreach (CellPartType partType in Enum.GetValues(typeof(CellPartType)))
                allTypes.Add(partType);

            Shuffle(allTypes, rng);

            int guaranteedCount = Math.Min(pickupCount, allTypes.Count);
            var result = new List<CellPartType>(pickupCount);
            for (int i = 0; i < guaranteedCount; i++)
                result.Add(allTypes[i]);

            for (int i = guaranteedCount; i < pickupCount; i++)
                result.Add(allTypes[rng.Next(allTypes.Count)]);

            return result;
        }

        private static void Shuffle(List<CellPartType> values, Random rng)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
