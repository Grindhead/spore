using UnityEngine;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>
    /// Runtime archetype role assigned to a spawned organism.
    /// </summary>
    public enum OrganismArchetype
    {
        Generalist,
        FilterFeeder,
        BacteriaSwarm,
        Scavenger,
        Parasite,
        Photosynthetic,
        Colonial
    }

    public enum CellThreatLevel
    {
        Harmless,
        Low,
        Medium,
        High,
        Apex
    }

    public readonly struct OrganismArchetypeProfile
    {
        public readonly OrganismArchetype Archetype;
        public readonly CellStage.TrophicRole Diet;
        public readonly CellThreatLevel ThreatLevel;
        public readonly string Silhouette;
        public readonly string BehaviorTell;
        public readonly string RewardLabel;

        public OrganismArchetypeProfile(
            OrganismArchetype archetype,
            CellStage.TrophicRole diet,
            CellThreatLevel threatLevel,
            string silhouette,
            string behaviorTell,
            string rewardLabel)
        {
            Archetype = archetype;
            Diet = diet;
            ThreatLevel = threatLevel;
            Silhouette = silhouette;
            BehaviorTell = behaviorTell;
            RewardLabel = rewardLabel;
        }
    }

    public sealed class OrganismReadabilityMarker : MonoBehaviour
    {
        [SerializeField] private OrganismArchetype _archetype;
        [SerializeField] private CellStage.TrophicRole _diet;
        [SerializeField] private CellThreatLevel _threatLevel;
        [SerializeField] private string _silhouette;
        [SerializeField] private string _behaviorTell;
        [SerializeField] private string _rewardLabel;

        public OrganismArchetype Archetype => _archetype;
        public CellStage.TrophicRole Diet => _diet;
        public CellThreatLevel ThreatLevel => _threatLevel;
        public string Silhouette => _silhouette;
        public string BehaviorTell => _behaviorTell;
        public string RewardLabel => _rewardLabel;

        public void Apply(OrganismArchetypeProfile profile)
        {
            _archetype = profile.Archetype;
            _diet = profile.Diet;
            _threatLevel = profile.ThreatLevel;
            _silhouette = profile.Silhouette;
            _behaviorTell = profile.BehaviorTell;
            _rewardLabel = profile.RewardLabel;
        }
    }

    /// <summary>
    /// Ensures pooled organisms are reconfigured with exactly one behaviour archetype on spawn.
    /// This prevents stale AI/archetype components from previous lifetimes leaking into new spawns.
    /// </summary>
    public static class OrganismArchetypeConfigurator
    {
        private static readonly OrganismArchetypeProfile[] s_profiles =
        {
            new(OrganismArchetype.Generalist, CellStage.TrophicRole.Omnivore, CellThreatLevel.Low, "round drifter", "meanders until food or danger appears", "balanced mass"),
            new(OrganismArchetype.FilterFeeder, CellStage.TrophicRole.Herbivore, CellThreatLevel.Harmless, "wide fan body", "faces food clouds and cruises slowly", "plant-rich mass"),
            new(OrganismArchetype.BacteriaSwarm, CellStage.TrophicRole.Omnivore, CellThreatLevel.Medium, "small schooling dots", "clusters and darts as a group", "many tiny bites"),
            new(OrganismArchetype.Scavenger, CellStage.TrophicRole.Herbivore, CellThreatLevel.Low, "hooked carrion seeker", "rushes toward loose meat then flees", "quick DNA crumbs"),
            new(OrganismArchetype.Parasite, CellStage.TrophicRole.Carnivore, CellThreatLevel.High, "needle body", "shadows larger cells before lunging", "high-risk meat"),
            new(OrganismArchetype.Photosynthetic, CellStage.TrophicRole.Herbivore, CellThreatLevel.Harmless, "green leaf cell", "loiters in bright bloom lanes", "safe grazing"),
            new(OrganismArchetype.Colonial, CellStage.TrophicRole.Omnivore, CellThreatLevel.Medium, "linked bead chain", "splits pressure across nearby kin", "larger prey reward")
        };

        public static void Configure(Organism organism, OrganismArchetype archetype)
        {
            if (organism == null)
                return;

            var go = organism.gameObject;
            EnsureReadabilityMarker(go, GetProfile(archetype));
            RemoveAll<CellOrganismAI>(go);
            RemoveAll<FilterFeederCell>(go);
            RemoveAll<BacteriaSwarmCell>(go);
            RemoveAll<ScavengerCell>(go);
            RemoveAll<ParasiteCell>(go);
            RemoveAll<PhotosyntheticCell>(go);
            RemoveAll<ColonialCell>(go);

            switch (archetype)
            {
                case OrganismArchetype.FilterFeeder:
                    go.AddComponent<FilterFeederCell>();
                    return;
                case OrganismArchetype.BacteriaSwarm:
                    go.AddComponent<BacteriaSwarmCell>();
                    return;
                case OrganismArchetype.Scavenger:
                    go.AddComponent<ScavengerCell>();
                    return;
                case OrganismArchetype.Parasite:
                    go.AddComponent<ParasiteCell>();
                    return;
                case OrganismArchetype.Photosynthetic:
                    go.AddComponent<PhotosyntheticCell>();
                    return;
                case OrganismArchetype.Colonial:
                    go.AddComponent<ColonialCell>();
                    return;
                default:
                    go.AddComponent<CellOrganismAI>();
                    return;
            }
        }

        public static OrganismArchetypeProfile GetProfile(OrganismArchetype archetype)
        {
            for (int i = 0; i < s_profiles.Length; i++)
            {
                if (s_profiles[i].Archetype == archetype)
                    return s_profiles[i];
            }

            return s_profiles[0];
        }

        public static System.Collections.Generic.IReadOnlyList<OrganismArchetypeProfile> GetAllProfiles()
        {
            return s_profiles;
        }

        private static void EnsureReadabilityMarker(GameObject go, OrganismArchetypeProfile profile)
        {
            if (go == null)
                return;

            var marker = go.GetComponent<OrganismReadabilityMarker>();
            if (marker == null)
                marker = go.AddComponent<OrganismReadabilityMarker>();
            marker.Apply(profile);
        }

        private static void RemoveAll<T>(GameObject go) where T : UnityEngine.Behaviour
        {
            if (go == null)
                return;

            var components = go.GetComponents<T>();
            for (int i = 0; i < components.Length; i++)
                DestroyBehaviour(components[i]);
        }

        private static void DestroyBehaviour(UnityEngine.Behaviour behaviour)
        {
            if (behaviour == null)
                return;

            behaviour.enabled = false;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(behaviour);
                return;
            }
#endif
            Object.Destroy(behaviour);
        }
    }
}
