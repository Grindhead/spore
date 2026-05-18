using System.Collections.Generic;
using System.Linq;
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
            def.Tooltip = BuildTooltip(displayName, category);
            def.Category = category;
            def.Rarity = GetDefaultRarity(partId, category);
            def.IconGlyph = GetCategoryGlyph(category);
            def.AttackModifier = attack;
            def.HealthModifier = health;
            def.SpeedModifier = speed;
            def.DefenseModifier = defense;
            def.SocialModifier = social;
            def.StealthModifier = stealth;
            def.Verbs = GetDefaultVerbs(partId, category);
            def.AnimationHint = BuildAnimationHint(def.Verbs);
            def.DNACost = cost;
            def.MaxCount = maxCount;
            def.RequiresSymmetry = category != PartCategory.Mouth && category != PartCategory.Tail && category != PartCategory.Shell;
            def.DefaultScale = GetDefaultScale(category);
            def.AllowedBones = GetDefaultAllowedBones(category);
            def.ProgressionFamily = GetProgressionFamily(partId, category);
            def.ProgressionTier = GetProgressionTier(partId);
            def.HiddenInCatalog = ShouldHideDuplicateGeneratedPart(partId, def.ProgressionTier);
            def.ModelPrefab = BlenderAssetLibrary.LoadCreaturePartPrefab(partId);
            def.name = partId;
            return def;
        }

        public static Dictionary<string, CreaturePartDef> GetVisibleCatalog()
        {
            var catalog = GetDefaultCatalog();
            var visible = new Dictionary<string, CreaturePartDef>();
            foreach (var kvp in catalog)
            {
                if (kvp.Value != null && !kvp.Value.HiddenInCatalog)
                    visible[kvp.Key] = kvp.Value;
            }
            return visible;
        }

        public static bool HasVerb(CreaturePartDef def, CreatureVerb verb)
        {
            if (def?.Verbs == null)
                return false;

            for (int i = 0; i < def.Verbs.Length; i++)
            {
                if (def.Verbs[i] == verb)
                    return true;
            }
            return false;
        }

        public static List<string> ValidateCatalog(Dictionary<string, CreaturePartDef> catalog = null)
        {
            catalog ??= GetDefaultCatalog();
            var errors = new List<string>();
            var visibleVerbSet = new HashSet<CreatureVerb>();

            foreach (var kvp in catalog)
            {
                var def = kvp.Value;
                if (def == null)
                {
                    errors.Add($"{kvp.Key} has no definition.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(def.PartId))
                    errors.Add($"{kvp.Key} is missing PartId.");
                if (string.IsNullOrWhiteSpace(def.DisplayName))
                    errors.Add($"{kvp.Key} is missing display name.");
                if (string.IsNullOrWhiteSpace(def.Tooltip))
                    errors.Add($"{kvp.Key} is missing tooltip.");
                if (def.DNACost <= 0)
                    errors.Add($"{kvp.Key} has a non-positive DNA cost.");
                if (def.AllowedBones == null || def.AllowedBones.Length == 0)
                    errors.Add($"{kvp.Key} has no allowed anchors.");
                if (def.Verbs == null || def.Verbs.Length == 0)
                    errors.Add($"{kvp.Key} has no creature verbs.");

                if (!def.HiddenInCatalog && def.Verbs != null)
                {
                    for (int i = 0; i < def.Verbs.Length; i++)
                        visibleVerbSet.Add(def.Verbs[i]);
                }
            }

            foreach (CreatureVerb verb in System.Enum.GetValues(typeof(CreatureVerb)))
            {
                if (!visibleVerbSet.Contains(verb))
                    errors.Add($"Visible catalog is missing verb {verb}.");
            }

            return errors;
        }

        public static bool TryGetPlacementRuleError(CreaturePartDef def, int attachBone, int spineSegments, out string reason)
        {
            reason = null;
            if (def == null)
            {
                reason = "Choose a known part first.";
                return true;
            }

            int safeSpineSegments = Mathf.Clamp(spineSegments, 1, 8);
            if (attachBone < 0 || attachBone >= safeSpineSegments)
            {
                reason = $"{def.DisplayName} needs a body anchor between 0 and {safeSpineSegments - 1}.";
                return true;
            }

            if (def.AllowedBones != null && def.AllowedBones.Length > 0)
            {
                bool allowed = false;
                for (int i = 0; i < def.AllowedBones.Length; i++)
                {
                    if (Mathf.Clamp(def.AllowedBones[i], 0, safeSpineSegments - 1) == attachBone)
                    {
                        allowed = true;
                        break;
                    }
                }

                if (!allowed)
                {
                    reason = $"{def.DisplayName} belongs on {BuildAnchorLabel(def.Category)} anchors.";
                    return true;
                }
            }

            return false;
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

        private static CreaturePartRarity GetDefaultRarity(string partId, PartCategory category)
        {
            int tier = GetProgressionTier(partId);
            if (category == PartCategory.Shell || category == PartCategory.Wing || category == PartCategory.Horn)
                tier++;

            if (tier >= 5) return CreaturePartRarity.Apex;
            if (tier >= 3) return CreaturePartRarity.Rare;
            if (tier >= 2) return CreaturePartRarity.Uncommon;
            return CreaturePartRarity.Common;
        }

        private static CreatureVerb[] GetDefaultVerbs(string partId, PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Mouth:
                    return new[] { CreatureVerb.Bite, CreatureVerb.Spit, CreatureVerb.Sing };
                case PartCategory.Eye:
                    return new[] { CreatureVerb.Charm, CreatureVerb.Pose };
                case PartCategory.Leg:
                    return new[] { CreatureVerb.Sprint, CreatureVerb.Jump, CreatureVerb.Charge };
                case PartCategory.Arm:
                    return new[] { CreatureVerb.Strike, CreatureVerb.Pose, CreatureVerb.CallPack };
                case PartCategory.Wing:
                    return new[] { CreatureVerb.Glide, CreatureVerb.Jump, CreatureVerb.Sprint };
                case PartCategory.Tail:
                    return new[] { CreatureVerb.Charge, CreatureVerb.Sprint, CreatureVerb.CallPack };
                case PartCategory.Horn:
                    return new[] { CreatureVerb.Charge, CreatureVerb.Strike };
                case PartCategory.Fin:
                    return new[] { CreatureVerb.Glide, CreatureVerb.Sprint, CreatureVerb.Sneak };
                case PartCategory.Shell:
                    return new[] { CreatureVerb.Sneak, CreatureVerb.Pose };
                case PartCategory.Spike:
                    return new[] { CreatureVerb.Strike, CreatureVerb.Charge };
                default:
                    return new[] { CreatureVerb.Pose };
            }
        }

        private static string BuildTooltip(string displayName, PartCategory category)
        {
            string anchors = BuildAnchorLabel(category);
            return $"{displayName}: {category.ToString().ToLowerInvariant()} part for {anchors} anchors.";
        }

        private static string BuildAnimationHint(CreatureVerb[] verbs)
        {
            if (verbs == null || verbs.Length == 0)
                return "idle";

            if (verbs.Contains(CreatureVerb.Glide)) return "glide";
            if (verbs.Contains(CreatureVerb.Charge)) return "charge";
            if (verbs.Contains(CreatureVerb.Strike)) return "strike";
            if (verbs.Contains(CreatureVerb.Bite)) return "bite";
            if (verbs.Contains(CreatureVerb.Sing)) return "social";
            return "idle";
        }

        private static string GetCategoryGlyph(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Mouth: return "M";
                case PartCategory.Eye: return "E";
                case PartCategory.Leg: return "L";
                case PartCategory.Arm: return "A";
                case PartCategory.Wing: return "W";
                case PartCategory.Tail: return "T";
                case PartCategory.Horn: return "H";
                case PartCategory.Fin: return "F";
                case PartCategory.Shell: return "S";
                case PartCategory.Spike: return "P";
                default: return "?";
            }
        }

        private static string BuildAnchorLabel(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Mouth:
                case PartCategory.Eye:
                case PartCategory.Horn:
                    return "head/front";
                case PartCategory.Tail:
                    return "rear";
                case PartCategory.Shell:
                    return "back";
                default:
                    return "side/body";
            }
        }

        private static string GetProgressionFamily(string partId, PartCategory category)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return category.ToString();

            string id = partId.ToLowerInvariant();
            if (id.StartsWith("animal_"))
            {
                string[] parts = id.Split('_');
                if (parts.Length >= 2)
                    return $"animal_{parts[1]}";
            }

            int underscore = id.IndexOf('_');
            return underscore > 0 ? id.Substring(0, underscore) : category.ToString().ToLowerInvariant();
        }

        private static int GetProgressionTier(string partId)
        {
            if (string.IsNullOrWhiteSpace(partId))
                return 1;

            string digits = new string(partId.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(digits) || !int.TryParse(digits, out int parsed))
                return 1;

            return Mathf.Clamp(parsed, 1, 6);
        }

        private static bool ShouldHideDuplicateGeneratedPart(string partId, int tier)
        {
            if (string.IsNullOrWhiteSpace(partId) || !partId.StartsWith("animal_"))
                return false;

            return tier > 4 && tier % 2 == 0;
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

            string lower = id.ToLowerInvariant();
            if (lower.StartsWith("animal_"))
            {
                string[] pieces = lower.Split('_');
                string category = pieces.Length >= 2 ? pieces[1] : "part";
                int tier = GetProgressionTier(id);
                string roman = tier switch
                {
                    1 => "I",
                    2 => "II",
                    3 => "III",
                    4 => "IV",
                    5 => "V",
                    _ => "VI"
                };
                string prefix = category switch
                {
                    "head" => "Bite Head",
                    "jaw" => "Grasper Jaw",
                    "torso" => "Back Shell",
                    "leg" => "Runner Leg",
                    "arm" => "Striker Arm",
                    "tail" => "Balance Tail",
                    "ear" => "Signal Fin",
                    "horn" => "Charge Horn",
                    "wing" => "Glider Wing",
                    _ => "Creature Part"
                };
                return $"{prefix} {roman}";
            }

            return id.Replace("_", " ");
        }
    }
}
