using System.Collections.Generic;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Tracks which cell parts the player has unlocked and exposes derived movement and damage properties.
    /// </summary>
    public enum CellPartType
    {
        Flagella,
        Cilia,
        Spike,
        Jet,
        HerbivoreMouth,
        CarnivoreMouth,
        OmnivoreMouth,
        SuctionMouth,
        PoisonVacuole,
        ElectricNodule,
        ArmorPlate,
        SensorEye
    }

    public enum CellPartCategory
    {
        Movement,
        Mouth,
        Defense,
        Utility
    }

    public enum CellDietRole
    {
        Neutral,
        Herbivore,
        Carnivore,
        Omnivore
    }

    public readonly struct CellPartDefinition
    {
        public readonly CellPartType Type;
        public readonly CellPartCategory Category;
        public readonly CellDietRole DietRole;
        public readonly string DisplayName;
        public readonly string Tooltip;
        public readonly string UnlockSource;
        public readonly int DnaCost;
        public readonly float SpeedBonus;
        public readonly float PlantGainBonus;
        public readonly float MeatGainBonus;
        public readonly float ToxinGainBonus;
        public readonly float ContactDamageMultiplier;
        public readonly float ProbeRadiusBonus;

        public CellPartDefinition(
            CellPartType type,
            CellPartCategory category,
            CellDietRole dietRole,
            string displayName,
            string tooltip,
            string unlockSource,
            int dnaCost,
            float speedBonus = 0f,
            float plantGainBonus = 0f,
            float meatGainBonus = 0f,
            float toxinGainBonus = 0f,
            float contactDamageMultiplier = 1f,
            float probeRadiusBonus = 0f)
        {
            Type = type;
            Category = category;
            DietRole = dietRole;
            DisplayName = displayName;
            Tooltip = tooltip;
            UnlockSource = unlockSource;
            DnaCost = dnaCost;
            SpeedBonus = speedBonus;
            PlantGainBonus = plantGainBonus;
            MeatGainBonus = meatGainBonus;
            ToxinGainBonus = toxinGainBonus;
            ContactDamageMultiplier = contactDamageMultiplier;
            ProbeRadiusBonus = probeRadiusBonus;
        }
    }

    /// <summary>
    /// Runtime loadout of unlocked cell parts that modifies speed and contact damage.
    /// </summary>
    public sealed class CellPartLoadout
    {
        private static readonly CellPartDefinition[] s_definitions =
        {
            new(CellPartType.Flagella, CellPartCategory.Movement, CellDietRole.Neutral, "Flagella", "Fast cruising propulsion for open-water chases.", "starter mutation pod", 6, speedBonus: 0.2f),
            new(CellPartType.Cilia, CellPartCategory.Movement, CellDietRole.Neutral, "Cilia", "Fine control, wider feeding reach, and steadier grazing.", "starter mutation pod", 5, speedBonus: 0.1f, plantGainBonus: 0.08f, meatGainBonus: 0.08f, probeRadiusBonus: 0.35f),
            new(CellPartType.Jet, CellPartCategory.Movement, CellDietRole.Neutral, "Jet", "Burst propulsion for escapes and ambushes.", "deep current pod", 9, speedBonus: 0.35f),
            new(CellPartType.HerbivoreMouth, CellPartCategory.Mouth, CellDietRole.Herbivore, "Filter Mouth", "Specialized plant grazing mouth with high algae yield.", "green food bloom", 4, plantGainBonus: 0.35f),
            new(CellPartType.CarnivoreMouth, CellPartCategory.Mouth, CellDietRole.Carnivore, "Bite Mouth", "Predatory mouth that extracts more mass from meat.", "meat shard cache", 5, meatGainBonus: 0.4f, contactDamageMultiplier: 1.1f),
            new(CellPartType.OmnivoreMouth, CellPartCategory.Mouth, CellDietRole.Omnivore, "Omnivore Mouth", "Flexible mouth that gains reliable value from plants and meat.", "mixed food eddy", 6, plantGainBonus: 0.18f, meatGainBonus: 0.18f),
            new(CellPartType.SuctionMouth, CellPartCategory.Mouth, CellDietRole.Omnivore, "Suction Mouth", "Pulls small food into range and helps opportunistic feeding.", "feeding ground pod", 7, plantGainBonus: 0.1f, meatGainBonus: 0.1f, probeRadiusBonus: 0.3f),
            new(CellPartType.Spike, CellPartCategory.Defense, CellDietRole.Neutral, "Spike", "contact damage for predators that close too greedily.", "bone-like shard", 7, contactDamageMultiplier: 1.45f),
            new(CellPartType.PoisonVacuole, CellPartCategory.Defense, CellDietRole.Neutral, "Poison Vacuole", "Toxin-adapted organelle that turns poison into a risky resource.", "toxic vent pod", 8, toxinGainBonus: 0.45f, contactDamageMultiplier: 1.12f),
            new(CellPartType.ElectricNodule, CellPartCategory.Defense, CellDietRole.Neutral, "Electric Nodule", "Bioelectric surface node that improves deterrence and stuns.", "charged mineral pod", 9, contactDamageMultiplier: 1.22f),
            new(CellPartType.ArmorPlate, CellPartCategory.Defense, CellDietRole.Neutral, "Armor Plate", "Dense outer plate for safer predator contact at a mobility cost.", "shell fragment pod", 8, speedBonus: -0.06f),
            new(CellPartType.SensorEye, CellPartCategory.Utility, CellDietRole.Neutral, "Sensor Eye", "Early warning organelle that expands pickup and threat readability.", "clear-water pod", 5, probeRadiusBonus: 0.2f)
        };

        private readonly HashSet<CellPartType> _unlockedParts = new();

        public float SpeedMultiplier
        {
            get
            {
                float multiplier = 1f;
                for (int i = 0; i < s_definitions.Length; i++)
                {
                    var def = s_definitions[i];
                    if (_unlockedParts.Contains(def.Type))
                        multiplier += def.SpeedBonus;
                }

                return multiplier < 0.4f ? 0.4f : multiplier;
            }
        }

        public bool HasContactDamage => ContactDamageMultiplier > 1f;
        public float ContactDamageMultiplier
        {
            get
            {
                float multiplier = 1f;
                for (int i = 0; i < s_definitions.Length; i++)
                {
                    var def = s_definitions[i];
                    if (_unlockedParts.Contains(def.Type))
                        multiplier *= def.ContactDamageMultiplier;
                }

                return multiplier;
            }
        }

        public float ConsumeProbeRadiusBonus
        {
            get
            {
                float bonus = 0f;
                for (int i = 0; i < s_definitions.Length; i++)
                {
                    var def = s_definitions[i];
                    if (_unlockedParts.Contains(def.Type))
                        bonus += def.ProbeRadiusBonus;
                }

                return bonus;
            }
        }

        public float FoodGainMultiplier => GetFoodGainMultiplier(FoodType.Plant);
        public float BoostDurationMultiplier => _unlockedParts.Contains(CellPartType.Jet) ? 1.35f : 1f;
        public float BoostSpeedMultiplier => _unlockedParts.Contains(CellPartType.Jet) ? 1.15f : 1f;
        public bool HasArmorPlate => _unlockedParts.Contains(CellPartType.ArmorPlate);
        public bool HasPoisonAdaptation => _unlockedParts.Contains(CellPartType.PoisonVacuole);
        public bool HasThreatSensor => _unlockedParts.Contains(CellPartType.SensorEye);

        public float GetFoodGainMultiplier(FoodType foodType)
        {
            float multiplier = 1f;
            for (int i = 0; i < s_definitions.Length; i++)
            {
                var def = s_definitions[i];
                if (!_unlockedParts.Contains(def.Type))
                    continue;

                multiplier += foodType switch
                {
                    FoodType.Meat => def.MeatGainBonus,
                    FoodType.Toxin => def.ToxinGainBonus,
                    _ => def.PlantGainBonus
                };
            }

            return multiplier;
        }

        public bool TryUnlock(CellPartType partType)
        {
            return _unlockedParts.Add(partType);
        }

        public bool HasPart(CellPartType partType)
        {
            return _unlockedParts.Contains(partType);
        }

        public static IReadOnlyList<CellPartDefinition> GetAllDefinitions()
        {
            return s_definitions;
        }

        public static CellPartDefinition GetDefinition(CellPartType partType)
        {
            for (int i = 0; i < s_definitions.Length; i++)
            {
                if (s_definitions[i].Type == partType)
                    return s_definitions[i];
            }

            return s_definitions[0];
        }
    }
}
