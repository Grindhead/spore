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

            Register(CreatePartDef("mouth_basic", "Basic Mouth", PartCategory.Mouth,
                attack: 3f, health: 0, speed: 0, defense: 0, social: 1f, stealth: 0, cost: 15, maxCount: 1));

            Register(CreatePartDef("eye_basic", "Basic Eye", PartCategory.Eye,
                attack: 0, health: 0, speed: 1f, defense: 0, social: 2f, stealth: 1f, cost: 10, maxCount: 4));

            Register(CreatePartDef("leg_basic", "Basic Leg", PartCategory.Leg,
                attack: 0, health: 1f, speed: 3f, defense: 0, social: 0, stealth: 0, cost: 15, maxCount: 6));

            Register(CreatePartDef("arm_basic", "Basic Arm", PartCategory.Arm,
                attack: 2f, health: 0, speed: 0, defense: 1f, social: 2f, stealth: 0, cost: 20, maxCount: 4));

            Register(CreatePartDef("wing_basic", "Basic Wing", PartCategory.Wing,
                attack: 0, health: 0, speed: 5f, defense: 0, social: 1f, stealth: 0, cost: 25, maxCount: 2));

            Register(CreatePartDef("tail_basic", "Basic Tail", PartCategory.Tail,
                attack: 1f, health: 0, speed: 2f, defense: 0, social: 0, stealth: 1f, cost: 15, maxCount: 1));

            Register(CreatePartDef("horn_basic", "Basic Horn", PartCategory.Horn,
                attack: 4f, health: 0, speed: 0, defense: 2f, social: -1f, stealth: 0, cost: 20, maxCount: 4));

            Register(CreatePartDef("fin_basic", "Basic Fin", PartCategory.Fin,
                attack: 0, health: 0, speed: 4f, defense: 0, social: 0, stealth: 2f, cost: 15, maxCount: 4));

            Register(CreatePartDef("shell_basic", "Basic Shell", PartCategory.Shell,
                attack: 0, health: 3f, speed: -2f, defense: 5f, social: 0, stealth: -1f, cost: 25, maxCount: 1));

            Register(CreatePartDef("spike_basic", "Basic Spike", PartCategory.Spike,
                attack: 2f, health: 0, speed: 0, defense: 3f, social: -1f, stealth: 0, cost: 15, maxCount: 6));

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
            def.DefaultScale = Vector3.one;
            def.ModelPrefab = BlenderAssetLibrary.LoadCreaturePartPrefab(partId);
            def.name = partId;
            return def;
        }

        public static CreaturePartDef GetPart(string partId)
        {
            var catalog = GetDefaultCatalog();
            return catalog.TryGetValue(partId, out var def) ? def : null;
        }
    }
}
