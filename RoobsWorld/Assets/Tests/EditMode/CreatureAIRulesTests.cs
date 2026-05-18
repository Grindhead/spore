using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Creature;
using RoobsWorld.Creature.AI;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureAIRulesTests
    {
        [Test]
        public void SmallerCreature_ShouldFleeFromLargerPlayer()
        {
            var selfObject = new GameObject("SmallCreature");
            var playerObject = new GameObject("LargePlayerCreature");
            try
            {
                var self = selfObject.AddComponent<CreatureInstance>();
                self.Initialize(CreateDna("small", 0.7f), CreatureStats.BaseStats, isPlayer: false);

                var player = playerObject.AddComponent<CreatureInstance>();
                player.Initialize(CreateDna("player", 1.4f), CreatureStats.BaseStats, isPlayer: true);

                Assert.IsTrue(CreatureSizeRules.ShouldFleeFromPlayer(self, player));
            }
            finally
            {
                Object.DestroyImmediate(selfObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void RivalDesiredDirection_FleesPlayer_WhenFlaggedAndInsideAggro()
        {
            Vector3 direction = CreatureRivalAI.ComputeDesiredDirection(
                selfPosition: Vector3.zero,
                playerPosition: new Vector3(2f, 0f, 0f),
                wanderTarget: new Vector3(0f, 0f, 9f),
                aggroRange: 5f,
                fleeFromPlayer: true);

            Assert.That(direction.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(direction.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void FindNearestThreat_TreatsLargerPlayerAsThreatForSmallerCreature()
        {
            var selfObject = new GameObject("SmallCreatureThreatTest");
            var playerObject = new GameObject("LargePlayerThreatTest");
            try
            {
                selfObject.transform.position = Vector3.zero;
                playerObject.transform.position = new Vector3(2f, 0f, 0f);
                selfObject.AddComponent<SphereCollider>();
                playerObject.AddComponent<SphereCollider>();

                var self = selfObject.AddComponent<CreatureInstance>();
                self.Initialize(CreateDna("small-threat", 0.7f), CreatureStats.BaseStats, isPlayer: false);

                var player = playerObject.AddComponent<CreatureInstance>();
                player.Initialize(CreateDna("player-threat", 1.4f), CreatureStats.BaseStats, isPlayer: true);

                Physics.SyncTransforms();
                var ctx = new BTContext { Self = selfObject, Creature = self };
                var status = new FindNearestThreat(10f).Tick(ctx);

                Assert.AreEqual(BTStatus.Success, status);
                Assert.AreEqual(playerObject.transform, ctx.Target);
            }
            finally
            {
                Object.DestroyImmediate(selfObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void FindNearestPrey_DoesNotSelectLargerPlayerForSmallerCreature()
        {
            var selfObject = new GameObject("SmallCreaturePreyTest");
            var playerObject = new GameObject("LargePlayerPreyTest");
            try
            {
                selfObject.transform.position = Vector3.zero;
                playerObject.transform.position = new Vector3(2f, 0f, 0f);
                selfObject.AddComponent<SphereCollider>();
                playerObject.AddComponent<SphereCollider>();

                var self = selfObject.AddComponent<CreatureInstance>();
                var selfStats = CreatureStats.BaseStats;
                selfStats.Attack = 50f;
                self.Initialize(CreateDna("small-prey", 0.7f), selfStats, isPlayer: false);

                var player = playerObject.AddComponent<CreatureInstance>();
                var playerStats = CreatureStats.BaseStats;
                playerStats.Health = 20f;
                player.Initialize(CreateDna("player-prey", 1.4f), playerStats, isPlayer: true);

                Physics.SyncTransforms();
                var ctx = new BTContext { Self = selfObject, Creature = self };
                var status = new FindNearestPrey(10f).Tick(ctx);

                Assert.AreEqual(BTStatus.Failure, status);
                Assert.IsNull(ctx.Target);
            }
            finally
            {
                Object.DestroyImmediate(selfObject);
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void FindNearestThreat_DetectsCreatureInstanceOnParentWhenColliderIsChild()
        {
            var selfObject = new GameObject("ThreatChildColliderSelf");
            var playerRoot = new GameObject("ThreatChildColliderPlayerRoot");
            var playerColliderChild = new GameObject("ThreatChildColliderPlayerBody");
            try
            {
                selfObject.transform.position = Vector3.zero;
                playerRoot.transform.position = new Vector3(2f, 0f, 0f);
                playerColliderChild.transform.SetParent(playerRoot.transform, false);

                selfObject.AddComponent<SphereCollider>();
                playerColliderChild.AddComponent<SphereCollider>();

                var self = selfObject.AddComponent<CreatureInstance>();
                self.Initialize(CreateDna("small-child-self", 0.7f), CreatureStats.BaseStats, isPlayer: false);

                var player = playerRoot.AddComponent<CreatureInstance>();
                player.Initialize(CreateDna("player-child-root", 1.4f), CreatureStats.BaseStats, isPlayer: true);

                Physics.SyncTransforms();
                var ctx = new BTContext { Self = selfObject, Creature = self };
                var status = new FindNearestThreat(10f).Tick(ctx);

                Assert.AreEqual(BTStatus.Success, status);
                Assert.AreEqual(playerRoot.transform, ctx.Target);
            }
            finally
            {
                Object.DestroyImmediate(selfObject);
                Object.DestroyImmediate(playerRoot);
            }
        }

        [Test]
        public void FindNearestPrey_DetectsCreatureInstanceOnParentWhenColliderIsChild()
        {
            var selfObject = new GameObject("PreyChildColliderSelf");
            var preyRoot = new GameObject("PreyChildColliderRoot");
            var preyColliderChild = new GameObject("PreyChildColliderBody");
            try
            {
                selfObject.transform.position = Vector3.zero;
                preyRoot.transform.position = new Vector3(2f, 0f, 0f);
                preyColliderChild.transform.SetParent(preyRoot.transform, false);

                selfObject.AddComponent<SphereCollider>();
                preyColliderChild.AddComponent<SphereCollider>();

                var self = selfObject.AddComponent<CreatureInstance>();
                var selfStats = CreatureStats.BaseStats;
                selfStats.Attack = 35f;
                self.Initialize(CreateDna("prey-child-self", 1.2f), selfStats, isPlayer: false);

                var prey = preyRoot.AddComponent<CreatureInstance>();
                var preyStats = CreatureStats.BaseStats;
                preyStats.Health = 15f;
                prey.Initialize(CreateDna("prey-child-root", 0.7f), preyStats, isPlayer: false);

                Physics.SyncTransforms();
                var ctx = new BTContext { Self = selfObject, Creature = self };
                var status = new FindNearestPrey(10f).Tick(ctx);

                Assert.AreEqual(BTStatus.Success, status);
                Assert.AreEqual(preyRoot.transform, ctx.Target);
            }
            finally
            {
                Object.DestroyImmediate(selfObject);
                Object.DestroyImmediate(preyRoot);
            }
        }

        [Test]
        public void MovementMemory_SelectsCandidateAwayFromRecentlyVisitedCells()
        {
            var recent = new Queue<Vector2Int>();
            CreatureMovementMemory.RecordRecentLocation(recent, Vector3.zero, 2f, 8);
            CreatureMovementMemory.RecordRecentLocation(recent, new Vector3(2f, 0f, 0f), 2f, 8);
            CreatureMovementMemory.RecordRecentLocation(recent, new Vector3(4f, 0f, 0f), 2f, 8);

            Vector3 best = CreatureMovementMemory.SelectLeastVisitedCandidate(
                currentPosition: Vector3.zero,
                candidates: new[]
                {
                    new Vector3(2f, 0f, 0f),
                    new Vector3(4f, 0f, 0f),
                    new Vector3(0f, 0f, 8f)
                },
                recentCells: recent,
                cellSize: 2f,
                minTravelDistance: 1f);

            Assert.That(best.z, Is.EqualTo(8f).Within(0.001f));
        }

        [Test]
        public void MovementMemory_CapsRecentCells()
        {
            var recent = new Queue<Vector2Int>();
            for (int i = 0; i < 12; i++)
                CreatureMovementMemory.RecordRecentLocation(recent, new Vector3(i, 0f, 0f), 1f, 5);

            Assert.That(recent.Count, Is.LessThanOrEqualTo(5));
            Assert.IsFalse(recent.Contains(new Vector2Int(0, 0)));
        }

        [Test]
        public void AmbientActivity_UsesArchetypeFlavorAndInjuryCaution()
        {
            Assert.AreEqual(
                CreatureRivalAmbientActivity.Forage,
                CreatureRivalAI.ChooseAmbientActivity(
                    random01: 0.1f,
                    healthRatio: 1f,
                    recentlyDamaged: false,
                    CreatureRivalArchetype.Tank));

            Assert.AreEqual(
                CreatureRivalAmbientActivity.Investigate,
                CreatureRivalAI.ChooseAmbientActivity(
                    random01: 0.5f,
                    healthRatio: 1f,
                    recentlyDamaged: false,
                    CreatureRivalArchetype.Stalker));

            Assert.AreEqual(
                CreatureRivalAmbientActivity.Roam,
                CreatureRivalAI.ChooseAmbientActivity(
                    random01: 0.1f,
                    healthRatio: 0.2f,
                    recentlyDamaged: false,
                    CreatureRivalArchetype.Tank));
        }

        [Test]
        public void SocialSpacingDirection_MovesAwayFromNearbyCreatures()
        {
            Vector3 direction = CreatureRivalAI.ComputeSocialSpacingDirection(
                selfPosition: Vector3.zero,
                neighborPositions: new[]
                {
                    new Vector3(1f, 0f, 0f),
                    new Vector3(8f, 0f, 0f)
                },
                personalSpaceRadius: 3f);

            Assert.That(direction.x, Is.LessThan(0f));
            Assert.That(direction.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void BlendSocialSteering_AllowsIdleCreatureToStepAwayFromCrowd()
        {
            Vector3 direction = CreatureRivalAI.BlendSocialSteering(
                desiredDirection: Vector3.zero,
                socialSpacingDirection: Vector3.left,
                socialSpacingWeight: 0.5f);

            Assert.That(direction.x, Is.EqualTo(-1f).Within(0.001f));
        }

        [Test]
        public void ApplyPlanarMeander_AddsSubtleSidewaysDrift()
        {
            Vector3 direction = CreatureRivalAI.ApplyPlanarMeander(
                desiredDirection: Vector3.forward,
                phase: Mathf.PI * 0.5f,
                strength: 0.5f);

            Assert.That(direction.x, Is.LessThan(0f));
            Assert.That(direction.z, Is.GreaterThan(0f));
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        private static CreatureDNA CreateDna(string id, float bodyScale)
        {
            var dna = CreatureDNA.CreateDefault();
            dna.CreatureId = id;
            dna.Body.BodyScale = new Vector3(bodyScale, bodyScale, bodyScale);
            return dna;
        }
    }
}
