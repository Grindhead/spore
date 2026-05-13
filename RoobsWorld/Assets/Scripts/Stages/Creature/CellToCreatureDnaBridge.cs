using RoobsWorld.Creature;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    /// <summary>
    /// Carries visible Cell-stage adaptations into the starter Creature-stage body.
    /// </summary>
    public static class CellToCreatureDnaBridge
    {
        public static CreatureDNA CreateFromCell(Genome cellGenome, CellPartLoadout loadout, CreatureDNA baseDna = null)
        {
            var dna = baseDna?.Clone() ?? CreatureDNA.CreateDefault();
            ApplyCellLegacy(dna, cellGenome, loadout);
            return dna;
        }

        public static void ApplyCellLegacy(CreatureDNA dna, Genome cellGenome, CellPartLoadout loadout)
        {
            if (dna == null || cellGenome == null)
                return;

            if (dna.Body == null)
                dna.Body = new BodyPlan();
            if (dna.Colors == null)
                dna.Colors = new CreatureColors();
            dna.Parts ??= new System.Collections.Generic.List<PartPlacement>();

            float speedBias = Mathf.Clamp01((cellGenome.Speed + cellGenome.Flagella) / 18f);
            float bulkBias = Mathf.Clamp01((cellGenome.Size + cellGenome.Defence) / 18f);
            float attackBias = Mathf.Clamp01((cellGenome.Attack + cellGenome.Spikes) / 18f);
            dna.LegacyTraitMask = (int)cellGenome.Traits;
            dna.LegacyAbilityId = ResolveInheritedAbilityId(cellGenome, loadout);

            Vector3 scale = dna.Body.BodyScale == Vector3.zero ? Vector3.one : dna.Body.BodyScale;
            scale.x = Mathf.Clamp(1f + bulkBias * 0.18f - speedBias * 0.08f, 0.82f, 1.28f);
            scale.y = Mathf.Clamp(1f + bulkBias * 0.12f, 0.86f, 1.22f);
            scale.z = Mathf.Clamp(1f + speedBias * 0.2f + attackBias * 0.08f, 0.92f, 1.34f);
            dna.Body.BodyScale = scale;

            dna.Colors.PrimaryColor = cellGenome.Color;
            if (cellGenome.HasTrait(GeneticTraits.Photosynthetic))
                dna.Colors.AccentColor = new Color(0.24f, 0.9f, 0.34f);
            else if (cellGenome.HasTrait(GeneticTraits.Toxic))
                dna.Colors.AccentColor = new Color(0.72f, 0.2f, 0.88f);
            else if (cellGenome.HasTrait(GeneticTraits.Electric))
                dna.Colors.AccentColor = new Color(0.25f, 0.72f, 1f);
            else
                dna.Colors.AccentColor = Color.Lerp(Color.white, cellGenome.Color, 0.35f);
            dna.Colors.SecondaryColor = Color.Lerp(cellGenome.Color, Color.black, 0.42f);

            if (speedBias >= 0.45f || (loadout != null && loadout.HasPart(CellPartType.Flagella)))
                AddPartIfMissing(dna, "tail_basic", attachBone: 2, new Vector3(0f, 0.08f, -0.72f), 1.05f);
            if (loadout != null && loadout.HasPart(CellPartType.Jet))
                AddPartIfMissing(dna, "wing_basic", attachBone: 1, new Vector3(0.46f, 0.12f, -0.05f), 0.88f);
            if (attackBias >= 0.45f || (loadout != null && loadout.HasPart(CellPartType.Spike)))
                AddPartIfMissing(dna, "spike_basic", attachBone: 1, new Vector3(0f, 0.22f, 0.22f), 0.95f);
            if (bulkBias >= 0.52f || cellGenome.HasTrait(GeneticTraits.Armoured))
                AddPartIfMissing(dna, "shell_basic", attachBone: 1, new Vector3(0f, 0.24f, -0.08f), 1f);
            if (loadout != null && loadout.HasPart(CellPartType.Cilia))
                AddPartIfMissing(dna, "fin_basic", attachBone: 1, new Vector3(-0.48f, 0.08f, -0.02f), 0.9f);

            dna.LegacySummary = BuildLegacySummary(cellGenome, loadout, dna.LegacyAbilityId, speedBias, bulkBias, attackBias);
        }

        public static string ResolveInheritedAbilityId(Genome cellGenome, CellPartLoadout loadout)
        {
            if (cellGenome == null)
                return "none";
            if (cellGenome.HasTrait(GeneticTraits.Electric))
                return "bioelectric_burst";
            if (cellGenome.HasTrait(GeneticTraits.Toxic))
                return "toxin_burst";
            if (cellGenome.HasTrait(GeneticTraits.Regenerative))
                return "regenerative_surge";
            if (cellGenome.HasTrait(GeneticTraits.Armoured))
                return "guard_surge";
            if (loadout != null && loadout.HasPart(CellPartType.Spike))
                return "bioelectric_burst";
            if (loadout != null && (loadout.HasPart(CellPartType.Jet) || loadout.HasPart(CellPartType.Flagella)))
                return "dash";
            if (cellGenome.Speed + cellGenome.Flagella >= 8f)
                return "dash";

            return "none";
        }

        public static string BuildLegacySummary(
            Genome cellGenome,
            CellPartLoadout loadout,
            string abilityId,
            float speedBias = 0f,
            float bulkBias = 0f,
            float attackBias = 0f)
        {
            if (cellGenome == null)
                return RoobsWorld.Core.Localization.Get("creature.legacy.summary");

            string traits = cellGenome.Traits == GeneticTraits.None
                ? "baseline traits"
                : cellGenome.Traits.ToString().Replace(", ", "/");
            string parts = BuildUnlockedPartSummary(loadout);
            string body = BuildBodyBiasSummary(speedBias, bulkBias, attackBias);
            string ability = GetAbilityDisplayName(abilityId);
            return $"Cell legacy: {traits}; starter parts: {parts}; body: {body}; ability: {ability}.";
        }

        public static string GetAbilityDisplayName(string abilityId)
        {
            CreatureInheritedAbilityType ability = CreatureInheritedAbilityType.None;
            switch ((abilityId ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "bioelectric_burst":
                    ability = CreatureInheritedAbilityType.BioelectricBurst;
                    break;
                case "toxin_burst":
                    ability = CreatureInheritedAbilityType.ToxinBurst;
                    break;
                case "regenerative_surge":
                    ability = CreatureInheritedAbilityType.RegenerativeSurge;
                    break;
                case "guard_surge":
                    ability = CreatureInheritedAbilityType.GuardSurge;
                    break;
                case "dash":
                case "legacy_dash":
                case "survival_dash":
                    ability = CreatureInheritedAbilityType.Dash;
                    break;
            }

            return CreaturePlayerController.GetAbilityDisplayName(ability);
        }

        private static string BuildUnlockedPartSummary(CellPartLoadout loadout)
        {
            if (loadout == null)
                return "default limbs";

            bool any = false;
            string summary = string.Empty;
            AppendPart(loadout, CellPartType.Flagella, "tail", ref summary, ref any);
            AppendPart(loadout, CellPartType.Cilia, "fins", ref summary, ref any);
            AppendPart(loadout, CellPartType.Spike, "spikes", ref summary, ref any);
            AppendPart(loadout, CellPartType.Jet, "wings", ref summary, ref any);
            return any ? summary : "default limbs";
        }

        private static void AppendPart(CellPartLoadout loadout, CellPartType partType, string label, ref string summary, ref bool any)
        {
            if (!loadout.HasPart(partType))
                return;

            summary += any ? $", {label}" : label;
            any = true;
        }

        private static string BuildBodyBiasSummary(float speedBias, float bulkBias, float attackBias)
        {
            if (bulkBias >= speedBias && bulkBias >= attackBias && bulkBias >= 0.32f)
                return "sturdier";
            if (attackBias >= speedBias && attackBias >= 0.32f)
                return "sharper";
            if (speedBias >= 0.32f)
                return "faster";

            return "balanced";
        }

        private static void AddPartIfMissing(CreatureDNA dna, string partId, int attachBone, Vector3 offset, float scale)
        {
            for (int i = 0; i < dna.Parts.Count; i++)
            {
                if (dna.Parts[i] != null && dna.Parts[i].PartId == partId)
                    return;
            }

            dna.Parts.Add(new PartPlacement
            {
                PartId = partId,
                AttachBone = attachBone,
                Offset = offset,
                Scale = scale
            });
        }
    }
}
