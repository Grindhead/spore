using System.Collections.Generic;

namespace RoobsWorld.Stages.Cell
{
    public readonly struct CellEvolutionStory
    {
        public readonly string DominantBehavior;
        public readonly string KeyMutations;
        public readonly string Summary;

        public CellEvolutionStory(string dominantBehavior, string keyMutations)
        {
            DominantBehavior = dominantBehavior;
            KeyMutations = keyMutations;
            Summary = string.IsNullOrWhiteSpace(keyMutations)
                ? dominantBehavior
                : $"{dominantBehavior} - {keyMutations}";
        }
    }

    /// <summary>
    /// Builds a compact narrative from the player's cell genome and collected parts.
    /// </summary>
    public static class CellEvolutionStoryBuilder
    {
        public static CellEvolutionStory Build(Genome genome, CellPartLoadout loadout)
        {
            string behavior = ResolveDominantBehavior(genome);
            string mutations = ResolveKeyMutations(genome, loadout);
            return new CellEvolutionStory(behavior, mutations);
        }

        public static string ResolveDominantBehavior(Genome genome)
        {
            if (genome == null)
                return "Generalist survivor";

            if (genome.Attack >= genome.Digestion + 2f || genome.Spikes >= 4f)
                return "Predatory hunter";
            if (genome.Speed >= genome.Defence + 2f || genome.Flagella >= 5f)
                return "Evasive sprinter";
            if (genome.Defence >= genome.Speed + 2f || genome.HasTrait(GeneticTraits.Armoured))
                return "Armoured drifter";
            if (genome.Digestion >= genome.Attack + 2f || genome.HasTrait(GeneticTraits.Photosynthetic))
                return "Efficient grazer";

            return "Generalist survivor";
        }

        public static string ResolveKeyMutations(Genome genome, CellPartLoadout loadout)
        {
            var parts = new List<string>(6);
            if (genome != null)
            {
                if (genome.HasTrait(GeneticTraits.Photosynthetic)) parts.Add("photosynthesis");
                if (genome.HasTrait(GeneticTraits.Toxic)) parts.Add("toxins");
                if (genome.HasTrait(GeneticTraits.Armoured)) parts.Add("armour");
                if (genome.HasTrait(GeneticTraits.Regenerative)) parts.Add("regeneration");
                if (genome.HasTrait(GeneticTraits.Electric)) parts.Add("electric pulses");
            }

            if (loadout != null)
            {
                if (loadout.HasPart(CellPartType.Flagella)) parts.Add("flagella");
                if (loadout.HasPart(CellPartType.Cilia)) parts.Add("cilia");
                if (loadout.HasPart(CellPartType.Spike)) parts.Add("spikes");
                if (loadout.HasPart(CellPartType.Jet)) parts.Add("jet");
            }

            if (parts.Count == 0)
                return "no signature mutations";

            int count = System.Math.Min(3, parts.Count);
            return string.Join(", ", parts.GetRange(0, count));
        }
    }
}
