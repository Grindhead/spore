using System.Collections.Generic;
using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Stages.Creature
{
    public enum CreatureWorldPointType
    {
        HomeNest,
        SpeciesNest,
        FoodSource,
        WaterSource,
        TerritoryCenter,
        MigrationGate
    }

    public enum CreatureRelationshipState
    {
        Unknown,
        Neutral,
        Wary,
        Allied,
        Hostile
    }

    public enum CreatureProgressionEventType
    {
        Eat,
        Drink,
        HealAtNest,
        AllySpecies,
        DefeatSpecies,
        DiscoverLandmark,
        RecruitPackMate,
        CompleteMigration
    }

    public readonly struct CreatureWorldPoint
    {
        public readonly CreatureWorldPointType Type;
        public readonly string Id;
        public readonly Vector3 Position;
        public readonly float Radius;
        public readonly int SpeciesIndex;

        public CreatureWorldPoint(CreatureWorldPointType type, string id, Vector3 position, float radius, int speciesIndex = -1)
        {
            Type = type;
            Id = id;
            Position = position;
            Radius = Mathf.Max(0.1f, radius);
            SpeciesIndex = speciesIndex;
        }
    }

    public readonly struct CreatureSpeciesProfile
    {
        public readonly int Index;
        public readonly string SpeciesId;
        public readonly CreatureRelationshipState StartingRelationship;
        public readonly Vector3 TerritoryCenter;
        public readonly float TerritoryRadius;
        public readonly bool SocialRouteAvailable;
        public readonly bool CombatRouteAvailable;

        public CreatureSpeciesProfile(
            int index,
            string speciesId,
            CreatureRelationshipState startingRelationship,
            Vector3 territoryCenter,
            float territoryRadius,
            bool socialRouteAvailable,
            bool combatRouteAvailable)
        {
            Index = index;
            SpeciesId = speciesId;
            StartingRelationship = startingRelationship;
            TerritoryCenter = territoryCenter;
            TerritoryRadius = Mathf.Max(1f, territoryRadius);
            SocialRouteAvailable = socialRouteAvailable;
            CombatRouteAvailable = combatRouteAvailable;
        }
    }

    public readonly struct CreatureVerbRating
    {
        public readonly CreatureVerb Verb;
        public readonly float Rating;
        public readonly bool IsAvailable;

        public CreatureVerbRating(CreatureVerb verb, float rating)
        {
            Verb = verb;
            Rating = Mathf.Max(0f, rating);
            IsAvailable = Rating > 0.01f;
        }
    }

    public readonly struct CreatureWorldPlan
    {
        public readonly CreatureWorldPoint[] Points;
        public readonly CreatureSpeciesProfile[] Species;
        public readonly CreatureVerbRating[] VerbRatings;
        public readonly int RecommendedActiveRivals;
        public readonly int RecommendedFoodSources;
        public readonly float ArenaRadius;

        public CreatureWorldPlan(
            CreatureWorldPoint[] points,
            CreatureSpeciesProfile[] species,
            CreatureVerbRating[] verbRatings,
            int recommendedActiveRivals,
            int recommendedFoodSources,
            float arenaRadius)
        {
            Points = points ?? System.Array.Empty<CreatureWorldPoint>();
            Species = species ?? System.Array.Empty<CreatureSpeciesProfile>();
            VerbRatings = verbRatings ?? System.Array.Empty<CreatureVerbRating>();
            RecommendedActiveRivals = Mathf.Max(1, recommendedActiveRivals);
            RecommendedFoodSources = Mathf.Max(1, recommendedFoodSources);
            ArenaRadius = Mathf.Max(10f, arenaRadius);
        }

        public CreatureWorldPoint? FirstPointOfType(CreatureWorldPointType type)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                if (Points[i].Type == type)
                    return Points[i];
            }
            return null;
        }
    }

    public struct CreatureProgressionState
    {
        public int DNA;
        public int Meals;
        public int Allies;
        public int Defeats;
        public int Discoveries;
        public int MigrationsCompleted;
        public bool IsAtNest;
        public bool HasPackMate;
    }

    public static class CreatureWorldPlanner
    {
        public const int CreatorReopenDnaThreshold = 40;
        public const int MigrationDnaThreshold = 65;

        public static CreatureWorldPlan CreatePlan(CreatureDNA dna, float arenaRadius, int playerCount = 1, int seed = 0)
        {
            float radius = Mathf.Max(18f, arenaRadius);
            int safePlayers = Mathf.Max(1, playerCount);
            var points = new List<CreatureWorldPoint>();
            var species = new List<CreatureSpeciesProfile>();

            points.Add(new CreatureWorldPoint(CreatureWorldPointType.HomeNest, "home_nest", new Vector3(-radius * 0.36f, 0f, -radius * 0.28f), 4.5f));
            points.Add(new CreatureWorldPoint(CreatureWorldPointType.FoodSource, "fruit_grove", new Vector3(radius * 0.18f, 0f, -radius * 0.12f), 6f));
            points.Add(new CreatureWorldPoint(CreatureWorldPointType.FoodSource, "bone_field", new Vector3(-radius * 0.04f, 0f, radius * 0.26f), 5.5f));
            points.Add(new CreatureWorldPoint(CreatureWorldPointType.WaterSource, "shallow_pool", new Vector3(radius * 0.28f, 0f, radius * 0.20f), 5f));
            points.Add(new CreatureWorldPoint(CreatureWorldPointType.MigrationGate, "ridge_pass", new Vector3(radius * 0.78f, 0f, radius * 0.04f), 4.25f));

            int speciesCount = Mathf.Clamp(3 + safePlayers / 3, 3, 6);
            for (int i = 0; i < speciesCount; i++)
            {
                float angle = (seed * 0.37f) + i * Mathf.PI * 2f / speciesCount;
                Vector3 center = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * 0.46f);
                var relationship = i == 0 ? CreatureRelationshipState.Neutral : i == 1 ? CreatureRelationshipState.Wary : CreatureRelationshipState.Unknown;
                species.Add(new CreatureSpeciesProfile(
                    i,
                    $"species_{i + 1}",
                    relationship,
                    center,
                    radius * 0.18f,
                    socialRouteAvailable: true,
                    combatRouteAvailable: true));
                points.Add(new CreatureWorldPoint(CreatureWorldPointType.TerritoryCenter, $"territory_{i + 1}", center, radius * 0.18f, i));
                points.Add(new CreatureWorldPoint(CreatureWorldPointType.SpeciesNest, $"species_nest_{i + 1}", center + center.normalized * 2.5f, 3.25f, i));
            }

            int foodSources = Mathf.Clamp(2 + safePlayers, 3, 10);
            for (int i = 2; i < foodSources; i++)
            {
                float angle = (seed * 0.19f) + i * 1.93f;
                Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (radius * Mathf.Lerp(0.18f, 0.62f, (i % 4) / 3f));
                points.Add(new CreatureWorldPoint(CreatureWorldPointType.FoodSource, $"forage_{i}", pos, 3.4f));
            }

            return new CreatureWorldPlan(
                points.ToArray(),
                species.ToArray(),
                BuildVerbRatings(dna, CreaturePartCatalog.GetDefaultCatalog()),
                recommendedActiveRivals: Mathf.Clamp(3 + safePlayers, 4, 14),
                recommendedFoodSources: foodSources,
                arenaRadius: radius);
        }

        public static CreatureVerbRating[] BuildVerbRatings(CreatureDNA dna, Dictionary<string, CreaturePartDef> catalog)
        {
            var ratings = new Dictionary<CreatureVerb, float>();
            foreach (CreatureVerb verb in System.Enum.GetValues(typeof(CreatureVerb)))
                ratings[verb] = 0f;

            if (dna?.Parts != null && catalog != null)
            {
                for (int i = 0; i < dna.Parts.Count; i++)
                {
                    var placement = dna.Parts[i];
                    if (placement == null || !catalog.TryGetValue(placement.PartId, out var def) || def?.Verbs == null)
                        continue;

                    float scale = Mathf.Clamp(placement.Scale <= 0f ? 1f : placement.Scale, 0.2f, 3f);
                    for (int v = 0; v < def.Verbs.Length; v++)
                    {
                        CreatureVerb verb = def.Verbs[v];
                        float statBonus = GetVerbStatBonus(def, verb);
                        ratings[verb] += Mathf.Max(0.5f, scale + statBonus * 0.2f);
                    }
                }
            }

            var result = new List<CreatureVerbRating>();
            foreach (var kvp in ratings)
                result.Add(new CreatureVerbRating(kvp.Key, kvp.Value));
            return result.ToArray();
        }

        public static CreatureRelationshipState ClassifyRelationship(int disposition)
        {
            if (disposition >= 60) return CreatureRelationshipState.Allied;
            if (disposition >= 15) return CreatureRelationshipState.Neutral;
            if (disposition <= -55) return CreatureRelationshipState.Hostile;
            if (disposition <= -10) return CreatureRelationshipState.Wary;
            return CreatureRelationshipState.Unknown;
        }

        public static int ApplySocialAction(int currentDisposition, CreatureVerb verb, float rating)
        {
            int bonus = verb == CreatureVerb.Sing || verb == CreatureVerb.Charm || verb == CreatureVerb.Pose || verb == CreatureVerb.CallPack
                ? 12
                : 4;
            return Mathf.Clamp(currentDisposition + bonus + Mathf.RoundToInt(rating * 3f), -100, 100);
        }

        public static int ApplyCombatAction(int currentDisposition, CreatureVerb verb, float rating)
        {
            int penalty = verb == CreatureVerb.Bite || verb == CreatureVerb.Charge || verb == CreatureVerb.Strike || verb == CreatureVerb.Spit
                ? 18
                : 8;
            return Mathf.Clamp(currentDisposition - penalty - Mathf.RoundToInt(rating * 2f), -100, 100);
        }

        public static int ComputeDnaReward(CreatureProgressionEventType eventType, CreatureRelationshipState relationship = CreatureRelationshipState.Unknown)
        {
            int baseReward = eventType switch
            {
                CreatureProgressionEventType.Eat => 4,
                CreatureProgressionEventType.Drink => 1,
                CreatureProgressionEventType.HealAtNest => 2,
                CreatureProgressionEventType.AllySpecies => 18,
                CreatureProgressionEventType.DefeatSpecies => 14,
                CreatureProgressionEventType.DiscoverLandmark => 8,
                CreatureProgressionEventType.RecruitPackMate => 12,
                CreatureProgressionEventType.CompleteMigration => 28,
                _ => 0
            };

            if (relationship == CreatureRelationshipState.Allied && eventType == CreatureProgressionEventType.AllySpecies)
                baseReward += 6;
            if (relationship == CreatureRelationshipState.Hostile && eventType == CreatureProgressionEventType.DefeatSpecies)
                baseReward += 4;
            return baseReward;
        }

        public static CreatureProgressionState ApplyProgressionEvent(CreatureProgressionState state, CreatureProgressionEventType eventType)
        {
            state.DNA += ComputeDnaReward(eventType);
            switch (eventType)
            {
                case CreatureProgressionEventType.Eat:
                    state.Meals++;
                    break;
                case CreatureProgressionEventType.AllySpecies:
                    state.Allies++;
                    break;
                case CreatureProgressionEventType.DefeatSpecies:
                    state.Defeats++;
                    break;
                case CreatureProgressionEventType.DiscoverLandmark:
                    state.Discoveries++;
                    break;
                case CreatureProgressionEventType.RecruitPackMate:
                    state.HasPackMate = true;
                    break;
                case CreatureProgressionEventType.CompleteMigration:
                    state.MigrationsCompleted++;
                    break;
            }
            return state;
        }

        public static bool IsInsideWorldPoint(CreatureWorldPoint point, Vector3 position, float extraRadius = 0f)
        {
            return ComputeDistanceOutsideWorldPoint(point, position, extraRadius) <= 0.001f;
        }

        public static float ComputeDistanceOutsideWorldPoint(CreatureWorldPoint point, Vector3 position, float extraRadius = 0f)
        {
            Vector2 pos = new Vector2(position.x, position.z);
            Vector2 target = new Vector2(point.Position.x, point.Position.z);
            return Mathf.Max(0f, Vector2.Distance(pos, target) - (point.Radius + Mathf.Max(0f, extraRadius)));
        }

        public static bool TryGetWorldPointProgressionEvent(
            CreatureWorldPoint point,
            bool firstVisit,
            bool canMigrate,
            out CreatureProgressionEventType eventType)
        {
            eventType = default;
            switch (point.Type)
            {
                case CreatureWorldPointType.HomeNest:
                    if (!firstVisit)
                        return false;
                    eventType = CreatureProgressionEventType.HealAtNest;
                    return true;
                case CreatureWorldPointType.FoodSource:
                    if (!firstVisit)
                        return false;
                    eventType = CreatureProgressionEventType.Eat;
                    return true;
                case CreatureWorldPointType.WaterSource:
                    if (!firstVisit)
                        return false;
                    eventType = CreatureProgressionEventType.Drink;
                    return true;
                case CreatureWorldPointType.TerritoryCenter:
                case CreatureWorldPointType.SpeciesNest:
                    if (!firstVisit)
                        return false;
                    eventType = CreatureProgressionEventType.DiscoverLandmark;
                    return true;
                case CreatureWorldPointType.MigrationGate:
                    eventType = canMigrate
                        ? CreatureProgressionEventType.CompleteMigration
                        : CreatureProgressionEventType.DiscoverLandmark;
                    return canMigrate || firstVisit;
                default:
                    return false;
            }
        }

        public static bool ShouldOpenCreatorAtNest(CreatureProgressionState state)
        {
            return state.IsAtNest && state.DNA >= CreatorReopenDnaThreshold;
        }

        public static bool CanMigrate(CreatureProgressionState state)
        {
            return state.DNA >= MigrationDnaThreshold
                && (state.Allies > 0 || state.Defeats > 0)
                && state.Discoveries > 0;
        }

        public static bool ShouldRecruitPackMate(
            CreatureProgressionState state,
            CreatureVerbRating[] ratings,
            float minimumRating = 0.5f)
        {
            if (state.HasPackMate || state.Allies <= 0)
                return false;

            if (HasUsableVerb(ratings, CreatureVerb.CallPack, minimumRating))
                return true;

            int socialVerbCount = 0;
            if (HasUsableVerb(ratings, CreatureVerb.Sing, minimumRating)) socialVerbCount++;
            if (HasUsableVerb(ratings, CreatureVerb.Charm, minimumRating)) socialVerbCount++;
            if (HasUsableVerb(ratings, CreatureVerb.Pose, minimumRating)) socialVerbCount++;
            return socialVerbCount >= 2;
        }

        public static bool HasUsableVerb(CreatureVerbRating[] ratings, CreatureVerb verb, float minimumRating = 0.5f)
        {
            if (ratings == null)
                return false;

            for (int i = 0; i < ratings.Length; i++)
            {
                if (ratings[i].Verb == verb && ratings[i].Rating >= minimumRating)
                    return true;
            }
            return false;
        }

        private static float GetVerbStatBonus(CreaturePartDef def, CreatureVerb verb)
        {
            return verb switch
            {
                CreatureVerb.Bite or CreatureVerb.Charge or CreatureVerb.Strike or CreatureVerb.Spit => def.AttackModifier,
                CreatureVerb.Jump or CreatureVerb.Glide or CreatureVerb.Sprint => def.SpeedModifier,
                CreatureVerb.Sing or CreatureVerb.Charm or CreatureVerb.Pose or CreatureVerb.CallPack => def.SocialModifier,
                CreatureVerb.Sneak => def.StealthModifier + def.DefenseModifier * 0.25f,
                _ => 0f
            };
        }
    }
}
