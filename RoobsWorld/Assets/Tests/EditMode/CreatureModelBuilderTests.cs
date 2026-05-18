using NUnit.Framework;
using RoobsWorld.Creature;
using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureModelBuilderTests
    {
        [Test]
        public void Build_CreatesSpineAnchorsForEveryBodySegment()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Body.SpineSegments = 4;
            var root = new GameObject("BuilderAnchors_Test");

            try
            {
                var result = CreatureModelBuilder.Build(root, dna, CreaturePartCatalog.GetDefaultCatalog());

                Assert.AreEqual(4, result.SpineAnchors.Length);
                for (int i = 0; i < result.SpineAnchors.Length; i++)
                {
                    Assert.IsNotNull(result.SpineAnchors[i]);
                    Assert.AreEqual(
                        CreatureModelBuilder.GetSpineAnchorLocalPosition(i, 4),
                        result.SpineAnchors[i].localPosition);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Build_AttachesPartRelativeToRequestedSpineBone()
        {
            var dna = new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Colors = new CreatureColors(),
                Parts = new System.Collections.Generic.List<PartPlacement>
                {
                    new PartPlacement
                    {
                        PartId = "tail_basic",
                        AttachBone = 2,
                        Offset = new Vector3(0.1f, 0.2f, -0.3f),
                        Scale = 1f
                    }
                }
            };
            var root = new GameObject("BuilderPlacement_Test");

            try
            {
                CreatureModelBuilder.Build(root, dna, CreaturePartCatalog.GetDefaultCatalog());
                var mount = root.GetComponentInChildren<CreaturePartMount>();

                Assert.IsNotNull(mount);
                Assert.AreEqual("tail_basic", mount.PartId);
                Assert.AreEqual(2, mount.AttachBone);
                Assert.AreEqual($"{CreatureModelBuilder.SpineAnchorPrefix}2", mount.transform.parent.name);
                Assert.AreEqual(dna.Parts[0].Offset, mount.transform.localPosition);
                Assert.AreEqual(
                    CreatureModelBuilder.GetSpineAnchorLocalPosition(2, 3) + dna.Parts[0].Offset,
                    mount.transform.position);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ModelFactory_CreatesDnaDrivenModularCreature()
        {
            var dna = new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Colors = new CreatureColors { PrimaryColor = Color.green, SecondaryColor = Color.blue, AccentColor = Color.white },
                Parts = new System.Collections.Generic.List<PartPlacement>
                {
                    new PartPlacement { PartId = "horn_basic", AttachBone = 0, Offset = new Vector3(0f, 0.3f, 0.35f), Scale = 1f },
                    new PartPlacement { PartId = "wing_basic", AttachBone = 1, Offset = new Vector3(0.42f, 0.05f, 0f), Scale = 0.8f }
                }
            };

            var creature = ModelFactory.CreateCreatureModel(Vector3.zero, dna);
            try
            {
                var mounts = creature.GetComponentsInChildren<CreaturePartMount>();

                Assert.AreEqual(2, mounts.Length);
                Assert.AreEqual("horn_basic", mounts[0].PartId);
                Assert.AreEqual("wing_basic", mounts[1].PartId);
            }
            finally
            {
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void Build_CreatesVisibleSkeletonAndLimbBonesForArmsAndLegs()
        {
            var dna = new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Colors = new CreatureColors(),
                Parts = new System.Collections.Generic.List<PartPlacement>
                {
                    new PartPlacement { PartId = "arm_basic", AttachBone = 1, Offset = new Vector3(0.45f, 0.2f, 0.05f), Scale = 1f },
                    new PartPlacement { PartId = "leg_basic", AttachBone = 1, Offset = new Vector3(-0.4f, -0.3f, 0.05f), Scale = 1f }
                }
            };

            var root = new GameObject("BuilderSkeleton_Test");

            try
            {
                CreatureModelBuilder.Build(root, dna, CreaturePartCatalog.GetDefaultCatalog());
                var skeleton = root.transform.Find($"{CreatureModelBuilder.BodyRootName}/{CreatureModelBuilder.SkeletonRootName}");

                Assert.IsNotNull(skeleton);
                Assert.IsTrue(skeleton.Find("Vertebra_0") != null);
                Assert.IsTrue(skeleton.Find("LimbUpper_00") != null);
                Assert.IsTrue(skeleton.Find("LimbLower_00") != null);
                Assert.IsTrue(skeleton.Find("LimbUpper_01") != null);
                Assert.IsTrue(skeleton.Find("LimbLower_01") != null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Build_CreatesLimbSkeletonForAnimalPrefixedArmAndLegIds()
        {
            var dna = new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Colors = new CreatureColors(),
                Parts = new System.Collections.Generic.List<PartPlacement>
                {
                    new PartPlacement { PartId = "animal_arm_001", AttachBone = 1, Offset = new Vector3(0.45f, 0.2f, 0.05f), Scale = 1f },
                    new PartPlacement { PartId = "animal_leg_001", AttachBone = 1, Offset = new Vector3(-0.4f, -0.3f, 0.05f), Scale = 1f }
                }
            };

            var root = new GameObject("BuilderSkeletonAnimalIds_Test");

            try
            {
                CreatureModelBuilder.Build(root, dna, CreaturePartCatalog.GetDefaultCatalog());
                var skeleton = root.transform.Find($"{CreatureModelBuilder.BodyRootName}/{CreatureModelBuilder.SkeletonRootName}");

                Assert.IsNotNull(skeleton);
                Assert.IsTrue(skeleton.Find("LimbUpper_00") != null);
                Assert.IsTrue(skeleton.Find("LimbLower_00") != null);
                Assert.IsTrue(skeleton.Find("LimbUpper_01") != null);
                Assert.IsTrue(skeleton.Find("LimbLower_01") != null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
