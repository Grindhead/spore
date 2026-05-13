using System.Collections.Generic;
using RoobsWorld.Core;
using RoobsWorld.Creature.Animation;
using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Creature
{
    /// <summary>
    /// Runtime factory that builds live creature GameObjects from <see cref="CreatureDNA"/> using procedural meshes and stats.
    /// </summary>
    public class CreatureFactory : Singleton<CreatureFactory>
    {
        [SerializeField] private CreaturePartDef[] _partCatalogArray;

        private readonly Dictionary<string, CreaturePartDef> _partCatalog = new Dictionary<string, CreaturePartDef>();

        /// <summary>
        /// Seeds the internal part catalog from the runtime defaults and any assigned ScriptableObject overrides.
        /// </summary>
        protected override void OnInitialize()
        {
            var defaults = CreaturePartCatalog.GetDefaultCatalog();
            foreach (var kvp in defaults)
                _partCatalog[kvp.Key] = kvp.Value;

            if (_partCatalogArray != null)
            {
                foreach (var part in _partCatalogArray)
                {
                    if (part != null)
                        _partCatalog[part.PartId] = part;
                }
            }
        }

        /// <summary>
        /// Registers or overrides a creature part definition in the runtime catalog.
        /// </summary>
        public void RegisterPart(CreaturePartDef partDef)
        {
            _partCatalog[partDef.PartId] = partDef;
        }

        /// <summary>
        /// Returns the current runtime part catalog keyed by <see cref="CreaturePartDef.PartId"/>.
        /// </summary>
        public Dictionary<string, CreaturePartDef> GetPartCatalog() => _partCatalog;

        /// <summary>
        /// Constructs and spawns a creature instance at the given position, wiring stats, animation, and spawn event.
        /// </summary>
        public GameObject SpawnCreature(CreatureDNA dna, Vector3 position, bool isPlayer = false)
        {
            var root = new GameObject($"Creature_{dna.CreatureName}");
            root.transform.position = position;
            root.transform.localScale = GetBodyScale(dna);

            CreatureModelBuilder.Build(root, dna, _partCatalog);

            var stats = StatCalculator.Calculate(dna, _partCatalog);

            var creatureComp = root.AddComponent<CreatureInstance>();
            creatureComp.Initialize(dna, stats, isPlayer);

            if (!isPlayer)
            {
                var rb = root.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                var col = root.AddComponent<CapsuleCollider>();
                col.height = dna.Body.SpineSegments * 0.5f;
                col.radius = 0.3f;
                col.center = Vector3.up * col.height * 0.5f;
            }

            AttachProceduralAnimation(root, dna);
            AttachSwimAnimation(root, dna);

            EventBus.Publish(new CreatureSpawnedEvent
            {
                CreatureObject = root,
                CreatureId = dna.CreatureId,
                IsPlayer = isPlayer
            });

            return root;
        }

        private static Vector3 GetBodyScale(CreatureDNA dna)
        {
            if (dna?.Body == null || dna.Body.BodyScale == Vector3.zero)
                return Vector3.one;
            return dna.Body.BodyScale;
        }

        private void AttachProceduralAnimation(GameObject root, CreatureDNA dna)
        {
            var legPlacements = new List<PartPlacement>();
            foreach (var p in dna.Parts)
            {
                if (p.PartId.StartsWith("leg"))
                    legPlacements.Add(p);
            }

            if (legPlacements.Count == 0) return;

            var limbConfigs = new LimbConfig[legPlacements.Count];
            var jointChains = new Transform[legPlacements.Count][];

            var legMounts = new List<CreaturePartMount>();
            var allMounts = root.GetComponentsInChildren<CreaturePartMount>(true);
            for (int i = 0; i < allMounts.Length; i++)
            {
                var mount = allMounts[i];
                if (mount != null && mount.PartId.StartsWith("leg"))
                    legMounts.Add(mount);
            }

            legMounts.Sort((a, b) => a.PartIndex.CompareTo(b.PartIndex));
            if (legMounts.Count == 0) return;

            int legIndex = 0;
            for (int i = 0; i < legMounts.Count && legIndex < legPlacements.Count; i++)
            {
                var child = legMounts[i].transform;

                var placement = legPlacements[legIndex];
                bool isLeft = placement.Offset.x < 0;

                var hip = new GameObject($"Hip_{legIndex}");
                hip.transform.SetParent(root.transform, false);
                hip.transform.position = child.position + Vector3.up * 0.5f;

                var knee = new GameObject($"Knee_{legIndex}");
                knee.transform.SetParent(hip.transform, false);
                knee.transform.localPosition = Vector3.down * 0.3f;

                var foot = child;
                foot.SetParent(knee.transform, true);

                jointChains[legIndex] = new Transform[] { hip.transform, knee.transform, foot };

                limbConfigs[legIndex] = new LimbConfig
                {
                    LimbIndex = legIndex,
                    Side = isLeft ? LimbSide.Left : LimbSide.Right,
                    BodySegment = placement.AttachBone
                };

                legIndex++;
            }

            if (legIndex == 0) return;

            if (legIndex < limbConfigs.Length)
            {
                System.Array.Resize(ref limbConfigs, legIndex);
                System.Array.Resize(ref jointChains, legIndex);
            }

            var animator = root.AddComponent<ProceduralAnimator>();
            animator.Initialize(limbConfigs, jointChains);
        }

        private static void AttachSwimAnimation(GameObject root, CreatureDNA dna)
        {
            var animator = root.AddComponent<CreatureSwimAnimator>();
            animator.Initialize(dna);
        }

    }
}
