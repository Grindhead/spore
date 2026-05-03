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

    /// <summary>
    /// Ensures pooled organisms are reconfigured with exactly one behaviour archetype on spawn.
    /// This prevents stale AI/archetype components from previous lifetimes leaking into new spawns.
    /// </summary>
    public static class OrganismArchetypeConfigurator
    {
        public static void Configure(Organism organism, OrganismArchetype archetype)
        {
            if (organism == null)
                return;

            var go = organism.gameObject;
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
