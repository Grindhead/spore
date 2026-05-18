using System.Collections.Generic;
using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Provides a runtime-generated catalog of balanced default creature parts for use in DNA and the editor.
    /// </summary>
    public static class CreaturePartCatalog
    {
        private static Dictionary<string, CreaturePartDef> _runtimeCatalog;

        public static Dictionary<string, CreaturePartDef> GetDefaultCatalog()
        {
            if (_runtimeCatalog != null) return _runtimeCatalog;
            _runtimeCatalog = new Dictionary<string, CreaturePartDef>();
            RegisterGeneratedModularParts();
            RegisterLegacyFallbackParts();
            return _runtimeCatalog;
        }

        private static void Register(CreaturePartDef def)
        {
            _runtimeCatalog[def.PartId] = def;
        }

        private static CreaturePartDef CreatePartDef(string partId, string displayName, PartCategory category,
            float attack, float health, float speed, float defense, float social, float stealth,
            int cost, int maxCount)
        {
            var def = ScriptableObject.CreateInstance<CreaturePartDef>();
            def.PartId = partId;
            def.DisplayName = displayName;
            def.Category = category;
            def.AttackModifier = attack;
            def.HealthModifier = health;
            def.SpeedModifier = speed;
            def.DefenseModifier = defense;
            def.SocialModifier = social;
            def.StealthModifier = stealth;
            def.DNACost = cost;
            def.MaxCount = maxCount;
            def.RequiresSymmetry = category != PartCategory.Mouth && category != PartCategory.Tail && category != PartCategory.Shell;
            def.DefaultScale = GetDefaultScale(category);
            def.AllowedBones = GetDefaultAllowedBones(category);
            def.ModelPrefab = BlenderAssetLibrary.LoadCreaturePartPrefab(partId);
            def.name = partId;
            return def;
        }

        private static Vector3 GetDefaultScale(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Eye: return Vector3.one * 0.82f;
                case PartCategory.Mouth: return Vector3.one * 0.9f;
                case PartCategory.Wing: return Vector3.one * 1.05f;
                case PartCategory.Shell: return Vector3.one * 1.12f;
                case PartCategory.Spike: return Vector3.one * 0.92f;
                default: return Vector3.one;
            }
        }

        private static int[] GetDefaultAllowedBones(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Mouth:
                case PartCategory.Eye:
                case PartCategory.Horn:
                    return new[] { 0 };
                case PartCategory.Tail:
                    return new[] { 2, 3, 4, 5, 6, 7 };
                case PartCategory.Shell:
                    return new[] { 1, 2, 3 };
                default:
                    return new[] { 1, 2, 3, 4, 5, 6, 7 };
            }
        }

        public static CreaturePartDef GetPart(string partId)
        {
            var catalog = GetDefaultCatalog();
            return catalog.TryGetValue(partId, out var def) ? def : null;
        }

        private static void RegisterGeneratedModularParts()
        {
            var generatedPrefabs = Resources.LoadAll<GameObject>("Models/CreatureParts");
            for (int i = 0; i < generatedPrefabs.Length; i++)
            {
                var prefab = generatedPrefabs[i];
                if (prefab == null)
                    continue;

                string partId = prefab.name;
                if (!partId.StartsWith("animal_"))
                    continue;
                if (_runtimeCatalog.ContainsKey(partId))
                    continue;

                if (!TryMapGeneratedPartCategory(partId, out var category))
                    continue;

                string displayName = ToDisplayName(partId);
                var def = CreateGeneratedPartDef(partId, displayName, category, prefab);
                Register(def);
            }
        }

        private static void RegisterLegacyFallbackParts()
        {
            RegisterLegacyFallbackPart("mouth_basic", "Basic Mouth", PartCategory.Mouth, 3f, 0f, 0f, 0f, 1f, 0f, 18, 1);
            RegisterLegacyFallbackPart("eye_basic", "Basic Eye", PartCategory.Eye, 0f, 0f, 1f, 0f, 2f, 1f, 12, 6);
            RegisterLegacyFallbackPart("leg_basic", "Basic Leg", PartCategory.Leg, 0f, 1f, 3f, 0f, 0f, 0f, 16, 8);
            RegisterLegacyFallbackPart("arm_basic", "Basic Arm", PartCategory.Arm, 2f, 0f, 0f, 1f, 1f, 0f, 18, 6);
            RegisterLegacyFallbackPart("wing_basic", "Basic Wing", PartCategory.Wing, 0f, 0f, 4f, 0f, 0f, 1f, 20, 4);
            RegisterLegacyFallbackPart("tail_basic", "Basic Tail", PartCategory.Tail, 1f, 0f, 2f, 0f, 0f, 0f, 16, 2);
            RegisterLegacyFallbackPart("horn_basic", "Basic Horn", PartCategory.Horn, 3f, 0f, 0f, 2f, -1f, 0f, 20, 6);
            RegisterLegacyFallbackPart("fin_basic", "Basic Fin", PartCategory.Fin, 0f, 0f, 4f, 0f, 0f, 1f, 20, 4);
            RegisterLegacyFallbackPart("shell_basic", "Basic Shell", PartCategory.Shell, 0f, 2f, -1f, 3f, 0f, 0f, 22, 1);
            RegisterLegacyFallbackPart("spike_basic", "Basic Spike", PartCategory.Spike, 3f, 0f, 0f, 2f, -1f, 0f, 20, 6);
        }

        private static void RegisterLegacyFallbackPart(
            string partId,
            string displayName,
            PartCategory category,
            float attack,
            float health,
            float speed,
            float defense,
            float social,
            float stealth,
            int cost,
            int maxCount)
        {
            if (_runtimeCatalog.ContainsKey(partId))
                return;

            Register(CreatePartDef(
                partId,
                displayName,
                category,
                attack,
                health,
                speed,
                defense,
                social,
                stealth,
                cost,
                maxCount));
        }

        private static CreaturePartDef CreateGeneratedPartDef(string partId, string displayName, PartCategory category, GameObject prefab)
        {
            float attack = 0f;
            float health = 0f;
            float speed = 0f;
            float defense = 0f;
            float social = 0f;
            float stealth = 0f;
            int cost = 15;
            int maxCount = 4;

            switch (category)
            {
                case PartCategory.Mouth:
                    attack = 3f;
                    social = 1f;
                    cost = 18;
                    maxCount = 1;
                    break;
                case PartCategory.Shell:
                    health = 2f;
                    defense = 3f;
                    speed = -1f;
                    cost = 22;
                    maxCount = 1;
                    break;
                case PartCategory.Leg:
                    health = 1f;
                    speed = 3f;
                    cost = 16;
                    maxCount = 8;
                    break;
                case PartCategory.Arm:
                    attack = 2f;
                    defense = 1f;
                    social = 1f;
                    cost = 18;
                    maxCount = 6;
                    break;
                case PartCategory.Wing:
                case PartCategory.Fin:
                    speed = 4f;
                    stealth = 1f;
                    cost = 20;
                    maxCount = 4;
                    break;
                case PartCategory.Horn:
                case PartCategory.Spike:
                    attack = 3f;
                    defense = 2f;
                    social = -1f;
                    cost = 20;
                    maxCount = 6;
                    break;
                case PartCategory.Tail:
                    speed = 2f;
                    attack = 1f;
                    cost = 16;
                    maxCount = 2;
                    break;
                case PartCategory.Eye:
                    speed = 1f;
                    social = 2f;
                    stealth = 1f;
                    cost = 12;
                    maxCount = 6;
                    break;
            }

            var def = CreatePartDef(partId, displayName, category, attack, health, speed, defense, social, stealth, cost, maxCount);
            def.DefaultScale = GetGeneratedDefaultScale(category);
            def.ModelPrefab = prefab;
            return def;
        }

        private static Vector3 GetGeneratedDefaultScale(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Eye: return Vector3.one * 0.72f;
                case PartCategory.Mouth: return Vector3.one * 0.86f;
                case PartCategory.Wing: return Vector3.one * 0.98f;
                case PartCategory.Horn:
                case PartCategory.Spike: return Vector3.one * 0.84f;
                default: return Vector3.one * 0.92f;
            }
        }

        private static bool TryMapGeneratedPartCategory(string partId, out PartCategory category)
        {
            string id = partId.ToLowerInvariant();
            if (id.StartsWith("animal_head_") || id.StartsWith("animal_jaw_"))
            {
                category = PartCategory.Mouth;
                return true;
            }
            if (id.StartsWith("animal_torso_"))
            {
                category = PartCategory.Shell;
                return true;
            }
            if (id.StartsWith("animal_leg_"))
            {
                category = PartCategory.Leg;
                return true;
            }
            if (id.StartsWith("animal_arm_"))
            {
                category = PartCategory.Arm;
                return true;
            }
            if (id.StartsWith("animal_tail_"))
            {
                category = PartCategory.Tail;
                return true;
            }
            if (id.StartsWith("animal_ear_"))
            {
                category = PartCategory.Fin;
                return true;
            }
            if (id.StartsWith("animal_horn_"))
            {
                category = PartCategory.Horn;
                return true;
            }
            if (id.StartsWith("animal_wing_"))
            {
                category = PartCategory.Wing;
                return true;
            }

            category = PartCategory.Spike;
            return false;
        }

        private static string ToDisplayName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "Part";
            return id.Replace("_", " ");
        }
    }
}
