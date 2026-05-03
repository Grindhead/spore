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

            BuildBodyFromPrimitives(root, dna);
            AttachPartsFromPrimitives(root, dna);

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

        private void BuildBodyFromPrimitives(GameObject root, CreatureDNA dna)
        {
            var bodyParent = new GameObject("Body");
            bodyParent.transform.SetParent(root.transform, false);

            float segmentSpacing = 0.4f;
            Color primaryColor = ProceduralTextureFactory.Opaque(dna.Colors.PrimaryColor);
            Color secondaryColor = ProceduralTextureFactory.Opaque(dna.Colors.SecondaryColor);
            var skinTex = ProceduralTextureFactory.CreateCreatureSkinTexture(
                primaryColor, secondaryColor);

            for (int i = 0; i < dna.Body.SpineSegments; i++)
            {
                float t = (float)i / Mathf.Max(1, dna.Body.SpineSegments - 1);
                float scale = Mathf.Lerp(0.6f, 0.35f, Mathf.Abs(t - 0.4f) * 2f);

                var segment = new GameObject($"Spine_{i}");
                segment.transform.SetParent(bodyParent.transform, false);
                segment.transform.localScale = Vector3.one * scale;
                segment.transform.localPosition = new Vector3(0, 0.5f, (i - dna.Body.SpineSegments / 2f) * segmentSpacing);

                var mf = segment.AddComponent<MeshFilter>();
                mf.mesh = CreaturePartMeshFactory.CreateSpineSegment(0.5f);
                var mr = segment.AddComponent<MeshRenderer>();
                Color segColor = ProceduralTextureFactory.Opaque(Color.Lerp(primaryColor, secondaryColor, t));
                mr.material = ProceduralTextureFactory.CreateMaterialWithTexture(skinTex, segColor);
            }
        }

        private void AttachPartsFromPrimitives(GameObject root, CreatureDNA dna)
        {
            var partsParent = new GameObject("Parts");
            partsParent.transform.SetParent(root.transform, false);

            foreach (var placement in dna.Parts)
            {
                if (_partCatalog.TryGetValue(placement.PartId, out var partDef) && partDef.ModelPrefab != null)
                {
                    var partObj = Instantiate(partDef.ModelPrefab, partsParent.transform);
                    partObj.transform.localPosition = placement.Offset;
                    partObj.transform.localEulerAngles = placement.Rotation;
                    partObj.transform.localScale = partDef.DefaultScale * placement.Scale;
                    BlenderAssetLibrary.DisableColliders(partObj);
                    continue;
                }

                var tempPart = CreateTempPartPrimitive(placement.PartId);
                tempPart.transform.SetParent(partsParent.transform, false);
                tempPart.transform.localPosition = placement.Offset + Vector3.up * 0.5f;
                tempPart.transform.localEulerAngles = placement.Rotation;
                tempPart.transform.localScale *= placement.Scale;
            }
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

            var partsParent = root.transform.Find("Parts");
            if (partsParent == null) return;

            int legIndex = 0;
            for (int i = 0; i < partsParent.childCount && legIndex < legPlacements.Count; i++)
            {
                var child = partsParent.GetChild(i);
                if (!child.name.Contains("leg")) continue;

                var placement = legPlacements[legIndex];
                bool isLeft = placement.Offset.x < 0;

                var hip = new GameObject($"Hip_{legIndex}");
                hip.transform.SetParent(root.transform, false);
                hip.transform.localPosition = child.localPosition + Vector3.up * 0.5f;

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

        private GameObject CreateTempPartPrimitive(string partId)
        {
            var mesh = CreaturePartMeshFactory.GetPartMesh(partId);
            Color color = CreaturePartMeshFactory.GetPartColor(partId);

            var obj = new GameObject($"Part_{partId}");
            var mf = obj.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            var mr = obj.AddComponent<MeshRenderer>();
            mr.material = TempAssetBuilder.CreateTempMaterial(color, $"{partId}_Mat");

            if (partId.StartsWith("eye"))
            {
                var pupil = new GameObject("Pupil");
                pupil.transform.SetParent(obj.transform, false);
                pupil.transform.localPosition = Vector3.forward * 0.03f;
                var pmf = pupil.AddComponent<MeshFilter>();
                pmf.mesh = CreaturePartMeshFactory.CreatePupil();
                var pmr = pupil.AddComponent<MeshRenderer>();
                pmr.material = TempAssetBuilder.CreateTempMaterial(Color.black, "PupilMat");
            }

            return obj;
        }
    }
}
