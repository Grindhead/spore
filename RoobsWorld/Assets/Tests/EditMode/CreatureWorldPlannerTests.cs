using System.Linq;
using NUnit.Framework;
using RoobsWorld.Creature;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreatureWorldPlannerTests
    {
        [Test]
        public void CreatePlan_IncludesNestsFoodWaterTerritoriesAndMigrationGate()
        {
            var plan = CreatureWorldPlanner.CreatePlan(CreatureDNA.CreateDefault(), 48f, playerCount: 4, seed: 42);

            Assert.IsTrue(plan.Points.Any(p => p.Type == CreatureWorldPointType.HomeNest));
            Assert.IsTrue(plan.Points.Any(p => p.Type == CreatureWorldPointType.SpeciesNest));
            Assert.IsTrue(plan.Points.Any(p => p.Type == CreatureWorldPointType.FoodSource));
            Assert.IsTrue(plan.Points.Any(p => p.Type == CreatureWorldPointType.WaterSource));
            Assert.IsTrue(plan.Points.Any(p => p.Type == CreatureWorldPointType.TerritoryCenter));
            Assert.IsTrue(plan.Points.Any(p => p.Type == CreatureWorldPointType.MigrationGate));
            Assert.GreaterOrEqual(plan.Species.Length, 4);
            Assert.GreaterOrEqual(plan.RecommendedActiveRivals, 4);
            Assert.GreaterOrEqual(plan.RecommendedFoodSources, 4);
        }

        [Test]
        public void CreatePlan_IsDeterministicForSameSeed()
        {
            var a = CreatureWorldPlanner.CreatePlan(CreatureDNA.CreateDefault(), 52f, playerCount: 2, seed: 77);
            var b = CreatureWorldPlanner.CreatePlan(CreatureDNA.CreateDefault(), 52f, playerCount: 2, seed: 77);

            Assert.AreEqual(a.Points.Length, b.Points.Length);
            Assert.AreEqual(a.Species[1].TerritoryCenter, b.Species[1].TerritoryCenter);
            Assert.AreEqual(a.Points.First(p => p.Type == CreatureWorldPointType.MigrationGate).Position,
                b.Points.First(p => p.Type == CreatureWorldPointType.MigrationGate).Position);
        }

        [Test]
        public void BuildVerbRatings_MapsCreaturePartsToWorldVerbs()
        {
            var dna = new CreatureDNA
            {
                Body = new BodyPlan { SpineSegments = 3, BodyScale = Vector3.one },
                Parts = new System.Collections.Generic.List<PartPlacement>
                {
                    new PartPlacement { PartId = "mouth_basic", Scale = 1f },
                    new PartPlacement { PartId = "leg_basic", Scale = 1f },
                    new PartPlacement { PartId = "arm_basic", Scale = 1f },
                    new PartPlacement { PartId = "wing_basic", Scale = 1f },
                    new PartPlacement { PartId = "tail_basic", Scale = 1f }
                }
            };

            var ratings = CreatureWorldPlanner.BuildVerbRatings(dna, CreaturePartCatalog.GetDefaultCatalog());

            Assert.IsTrue(CreatureWorldPlanner.HasUsableVerb(ratings, CreatureVerb.Bite));
            Assert.IsTrue(CreatureWorldPlanner.HasUsableVerb(ratings, CreatureVerb.Sprint));
            Assert.IsTrue(CreatureWorldPlanner.HasUsableVerb(ratings, CreatureVerb.Strike));
            Assert.IsTrue(CreatureWorldPlanner.HasUsableVerb(ratings, CreatureVerb.Glide));
            Assert.IsTrue(CreatureWorldPlanner.HasUsableVerb(ratings, CreatureVerb.CallPack));
        }

        [Test]
        public void RelationshipActions_SupportSocialAndCombatRoutes()
        {
            int socialDisposition = CreatureWorldPlanner.ApplySocialAction(0, CreatureVerb.Charm, rating: 3f);
            int combatDisposition = CreatureWorldPlanner.ApplyCombatAction(0, CreatureVerb.Bite, rating: 3f);

            Assert.Greater(socialDisposition, 0);
            Assert.Less(combatDisposition, 0);
            Assert.AreEqual(CreatureRelationshipState.Allied, CreatureWorldPlanner.ClassifyRelationship(65));
            Assert.AreEqual(CreatureRelationshipState.Hostile, CreatureWorldPlanner.ClassifyRelationship(-70));
        }

        [Test]
        public void ProgressionRewards_OpenCreatorAtNestAndGateMigration()
        {
            var state = new CreatureProgressionState { IsAtNest = true };
            for (int i = 0; i < 4; i++)
                state = CreatureWorldPlanner.ApplyProgressionEvent(state, CreatureProgressionEventType.Eat);
            state = CreatureWorldPlanner.ApplyProgressionEvent(state, CreatureProgressionEventType.AllySpecies);
            state = CreatureWorldPlanner.ApplyProgressionEvent(state, CreatureProgressionEventType.DiscoverLandmark);

            Assert.IsTrue(CreatureWorldPlanner.ShouldOpenCreatorAtNest(state));
            Assert.IsFalse(CreatureWorldPlanner.CanMigrate(state));

            state = CreatureWorldPlanner.ApplyProgressionEvent(state, CreatureProgressionEventType.CompleteMigration);
            Assert.IsTrue(CreatureWorldPlanner.CanMigrate(state));
        }

        [Test]
        public void PackMateRecruitment_RequiresAllianceAndSocialTools()
        {
            var alliedState = new CreatureProgressionState { Allies = 1 };
            var alreadyPacked = new CreatureProgressionState { Allies = 1, HasPackMate = true };
            var socialRatings = new[]
            {
                new CreatureVerbRating(CreatureVerb.Sing, 0.8f),
                new CreatureVerbRating(CreatureVerb.Charm, 0.8f)
            };
            var callPackRatings = new[]
            {
                new CreatureVerbRating(CreatureVerb.CallPack, 0.8f)
            };
            var weakSocialRatings = new[]
            {
                new CreatureVerbRating(CreatureVerb.Pose, 0.2f)
            };

            Assert.IsTrue(CreatureWorldPlanner.ShouldRecruitPackMate(alliedState, socialRatings));
            Assert.IsTrue(CreatureWorldPlanner.ShouldRecruitPackMate(alliedState, callPackRatings));
            Assert.IsTrue(CreatureWorldPlanner.HasSocialPacifyAssist(socialRatings));
            Assert.AreEqual(2, CreatureWorldPlanner.CountUsableSocialVerbs(socialRatings));
            Assert.IsFalse(CreatureWorldPlanner.ShouldRecruitPackMate(new CreatureProgressionState(), socialRatings));
            Assert.IsFalse(CreatureWorldPlanner.ShouldRecruitPackMate(alreadyPacked, callPackRatings));
            Assert.IsFalse(CreatureWorldPlanner.ShouldRecruitPackMate(alliedState, weakSocialRatings));
        }

        [Test]
        public void TryGetBestSocialVerb_ReturnsHighestRatedSocialTool()
        {
            var ratings = new[]
            {
                new CreatureVerbRating(CreatureVerb.Sing, 1.2f),
                new CreatureVerbRating(CreatureVerb.Charm, 2.4f),
                new CreatureVerbRating(CreatureVerb.Bite, 8f)
            };

            Assert.IsTrue(CreatureWorldPlanner.TryGetBestSocialVerb(ratings, out var verb, out float rating));
            Assert.AreEqual(CreatureVerb.Charm, verb);
            Assert.AreEqual(2.4f, rating, 0.001f);
            Assert.IsFalse(CreatureWorldPlanner.TryGetBestSocialVerb(null, out _, out _));
        }

        [Test]
        public void WorldPointHelpers_MapVisitsToProgressionEvents()
        {
            var food = new CreatureWorldPoint(CreatureWorldPointType.FoodSource, "fruit", Vector3.zero, 3f);
            var gate = new CreatureWorldPoint(CreatureWorldPointType.MigrationGate, "gate", Vector3.right * 10f, 2f);

            Assert.IsTrue(CreatureWorldPlanner.IsInsideWorldPoint(food, new Vector3(2.9f, 0f, 0f)));
            Assert.AreEqual(0f, CreatureWorldPlanner.ComputeDistanceOutsideWorldPoint(food, new Vector3(3.5f, 0f, 0f), extraRadius: 0.5f));

            Assert.IsTrue(CreatureWorldPlanner.TryGetWorldPointProgressionEvent(food, firstVisit: true, canMigrate: false, out var foodEvent));
            Assert.AreEqual(CreatureProgressionEventType.Eat, foodEvent);
            Assert.IsFalse(CreatureWorldPlanner.TryGetWorldPointProgressionEvent(food, firstVisit: false, canMigrate: false, out _));

            Assert.IsTrue(CreatureWorldPlanner.TryGetWorldPointProgressionEvent(gate, firstVisit: true, canMigrate: false, out var discoverEvent));
            Assert.AreEqual(CreatureProgressionEventType.DiscoverLandmark, discoverEvent);
            Assert.IsTrue(CreatureWorldPlanner.TryGetWorldPointProgressionEvent(gate, firstVisit: false, canMigrate: true, out var migrateEvent));
            Assert.AreEqual(CreatureProgressionEventType.CompleteMigration, migrateEvent);
        }

        [Test]
        public void WorldStatusText_CommunicatesNestAndMigrationReadiness()
        {
            var state = new CreatureProgressionState
            {
                DNA = 70,
                Meals = 3,
                Allies = 1,
                Discoveries = 2,
                MigrationsCompleted = 0,
                IsAtNest = true
            };

            string nestStatus = CreatureStageHUD.BuildWorldStatusText(
                state,
                canEditAtNest: true,
                canMigrate: false,
                nearestNestDistance: 0f,
                nearestMigrationDistance: 11f);
            string gateStatus = CreatureStageHUD.BuildWorldStatusText(
                state,
                canEditAtNest: true,
                canMigrate: true,
                nearestNestDistance: 0f,
                nearestMigrationDistance: 0f);

            Assert.That(nestStatus, Does.Contain("Nest editor ready"));
            Assert.That(nestStatus, Does.Contain("Gate 11m"));
            Assert.That(nestStatus, Does.Contain("Pack 0"));
            Assert.That(gateStatus, Does.Contain("Migration gate ready"));
            Assert.That(CreatureStageHUD.BuildWorldProgressionLabel(CreatureProgressionEventType.Eat, 4), Does.Contain("+4 DNA"));
        }

        [Test]
        public void WorldStatusText_ShowsPackMateProgress()
        {
            var state = new CreatureProgressionState
            {
                Meals = 2,
                Allies = 1,
                HasPackMate = true
            };

            string status = CreatureStageHUD.BuildWorldStatusText(
                state,
                canEditAtNest: false,
                canMigrate: false);

            Assert.That(status, Does.Contain("Pack 1"));
            Assert.That(CreatureStageHUD.BuildWorldProgressionLabel(CreatureProgressionEventType.RecruitPackMate, 12), Does.Contain("Packmate recruited"));
        }

        [Test]
        public void ArenaStoresWorldPlanAndBuildsMarkers()
        {
            var root = new GameObject("CreatureArenaTest");
            try
            {
                var arena = root.AddComponent<CreatureStageArena>();
                var plan = CreatureWorldPlanner.CreatePlan(CreatureDNA.CreateDefault(), 32f, playerCount: 1, seed: 5);

                arena.ApplyWorldPlan(plan);
                arena.EnsureBuilt();

                Assert.AreEqual(plan.Points.Length, arena.WorldPlan.Points.Length);
                Assert.IsNotNull(root.transform.Find("CreatureWorldMarkers"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
