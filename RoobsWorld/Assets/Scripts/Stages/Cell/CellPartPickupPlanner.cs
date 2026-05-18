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
        public readonly struct SpawnBand
        {
            public readonly float MinRadiusFraction;
            public readonly float MaxRadiusFraction;
            public readonly bool AllowToxicZone;

            public SpawnBand(float minRadiusFraction, float maxRadiusFraction, bool allowToxicZone)
            {
                MinRadiusFraction = Clamp01(minRadiusFraction);
                MaxRadiusFraction = Clamp01(Math.Max(minRadiusFraction, maxRadiusFraction));
                AllowToxicZone = allowToxicZone;
            }
        }

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

        public static SpawnBand GetStrategicSpawnBand(CellPartType partType)
        {
            switch (partType)
            {
                case CellPartType.HerbivoreMouth:
                case CellPartType.OmnivoreMouth:
                case CellPartType.SuctionMouth:
                case CellPartType.Cilia:
                    return new SpawnBand(0.2f, 0.58f, allowToxicZone: false);
                case CellPartType.CarnivoreMouth:
                case CellPartType.Spike:
                    return new SpawnBand(0.52f, 0.88f, allowToxicZone: false);
                case CellPartType.PoisonVacuole:
                case CellPartType.ElectricNodule:
                case CellPartType.Jet:
                    return new SpawnBand(0.62f, 0.92f, allowToxicZone: true);
                case CellPartType.ArmorPlate:
                case CellPartType.SensorEye:
                    return new SpawnBand(0.35f, 0.76f, allowToxicZone: false);
                default:
                    return new SpawnBand(0.18f, 0.72f, allowToxicZone: false);
            }
        }

        public static string GetPreviewText(CellPartType partType)
        {
            var definition = CellPartLoadout.GetDefinition(partType);
            return $"{definition.DisplayName}: {definition.Tooltip}";
        }

        public static CellPartType SelectGoalPartType(CellPartLoadout loadout)
        {
            if (loadout == null)
                return CellPartType.Cilia;

            if (!loadout.HasPart(CellPartType.Cilia))
                return CellPartType.Cilia;
            if (!loadout.HasPart(CellPartType.HerbivoreMouth))
                return CellPartType.HerbivoreMouth;
            if (!loadout.HasPart(CellPartType.CarnivoreMouth))
                return CellPartType.CarnivoreMouth;
            if (!loadout.HasPart(CellPartType.OmnivoreMouth))
                return CellPartType.OmnivoreMouth;
            if (!loadout.HasPart(CellPartType.Spike))
                return CellPartType.Spike;
            if (!loadout.HasPart(CellPartType.Jet))
                return CellPartType.Jet;
            if (!loadout.HasPart(CellPartType.SensorEye))
                return CellPartType.SensorEye;
            if (!loadout.HasPart(CellPartType.Flagella))
                return CellPartType.Flagella;

            return CellPartType.Cilia;
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f) return 0f;
            if (value >= 1f) return 1f;
            return value;
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
