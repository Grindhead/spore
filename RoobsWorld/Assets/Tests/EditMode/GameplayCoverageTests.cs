using System.Linq;
using System.Reflection;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class GameplayTelemetryTrackerTests
    {
        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        [Test]
        public void Snapshot_AccumulatesPublishedGameplayEvents()
        {
            var tracker = GameplayTelemetryTracker.Attach("TelemetryCoverage");
            InvokeLifecycle(tracker, "OnEnable");

            try
            {
                EventBus.Publish(new CreatureDiedEvent { IsPlayer = true });
                EventBus.Publish(new CreatureDiedEvent { IsPlayer = false });
                EventBus.Publish(new CellGoalCompletedEvent { RepeatedActionCategory = true });
                EventBus.Publish(new CellGoalCompletedEvent { RepeatedActionCategory = false });
                EventBus.Publish(new CellMutationChosenEvent { ChoiceLabel = "spikes" });
                EventBus.Publish(new CellFoodConsumedEvent { FoodType = FoodType.Plant });
                EventBus.Publish(new CellFoodConsumedEvent { FoodType = FoodType.Meat });
                EventBus.Publish(new CellFoodConsumedEvent { FoodType = FoodType.Toxin });
                EventBus.Publish(new CreatureRivalDefeatedEvent { IsAlpha = false });
                EventBus.Publish(new CreatureRivalDefeatedEvent { IsAlpha = true });
                EventBus.Publish(new CreatureStaminaExhaustedEvent());
                EventBus.Publish(new CreatureStaminaRestoredEvent());
                EventBus.Publish(new CreatureAbilityUsedEvent { AbilityId = "dash" });
                EventBus.Publish(new CreatureStreakBonusEvent { StreakCount = 3, DnaBonus = 2 });
                EventBus.Publish(new CreatureObjectiveCompletedEvent { CompletionSeconds = 18f });
                EventBus.Publish(new CreatureObjectiveCompletedEvent { CompletionSeconds = 7.5f });

                var snapshot = tracker.Snapshot;

                Assert.AreEqual(1, snapshot.PlayerDeaths);
                Assert.AreEqual(2, snapshot.GoalsCompleted);
                Assert.AreEqual(1, snapshot.CellGoalCategoryRepeats);
                Assert.AreEqual(1, snapshot.MutationsChosen);
                Assert.AreEqual(1, snapshot.PlantFoodEaten);
                Assert.AreEqual(1, snapshot.MeatFoodEaten);
                Assert.AreEqual(1, snapshot.ToxinFoodEaten);
                Assert.AreEqual(2, snapshot.RivalKills);
                Assert.AreEqual(1, snapshot.AlphaRivalKills);
                Assert.AreEqual(1, snapshot.CreatureStaminaExhaustions);
                Assert.AreEqual(1, snapshot.CreatureStaminaRestores);
                Assert.AreEqual(1, snapshot.CreatureAbilityUses);
                Assert.AreEqual(1, snapshot.CreatureStreakBonuses);
                Assert.AreEqual(2, snapshot.CreatureObjectivesCompleted);
                Assert.AreEqual(7.5f, snapshot.FastestCreatureObjectiveSeconds);
            }
            finally
            {
                InvokeLifecycle(tracker, "OnDisable");
                Object.DestroyImmediate(tracker.gameObject);
            }
        }

        private static void InvokeLifecycle(GameplayTelemetryTracker tracker, string methodName)
        {
            typeof(GameplayTelemetryTracker)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(tracker, null);
        }
    }

    public class CellEvolutionStoryBuilderTests
    {
        [Test]
        public void Build_NullInputs_ReturnsGeneralistStory()
        {
            var story = CellEvolutionStoryBuilder.Build(null, null);

            Assert.AreEqual("Generalist survivor", story.DominantBehavior);
            Assert.AreEqual("no signature mutations", story.KeyMutations);
            Assert.AreEqual("Generalist survivor - no signature mutations", story.Summary);
        }

        [Test]
        public void ResolveDominantBehavior_ChoosesStrongestSpecialization()
        {
            Assert.AreEqual("Predatory hunter", CellEvolutionStoryBuilder.ResolveDominantBehavior(new Genome { Attack = 6f, Digestion = 2f }));
            Assert.AreEqual("Evasive sprinter", CellEvolutionStoryBuilder.ResolveDominantBehavior(new Genome { Speed = 7f, Defence = 3f }));
            Assert.AreEqual("Armoured drifter", CellEvolutionStoryBuilder.ResolveDominantBehavior(new Genome { Defence = 5f, Speed = 1f }));
            Assert.AreEqual("Efficient grazer", CellEvolutionStoryBuilder.ResolveDominantBehavior(new Genome { Digestion = 6f, Attack = 2f }));
        }

        [Test]
        public void ResolveKeyMutations_LimitsSummaryToFirstThreeSignatures()
        {
            var genome = new Genome { Traits = GeneticTraits.Photosynthetic | GeneticTraits.Toxic | GeneticTraits.Electric };
            var loadout = new CellPartLoadout();
            loadout.TryUnlock(CellPartType.Flagella);

            Assert.AreEqual("photosynthesis, toxins, electric pulses", CellEvolutionStoryBuilder.ResolveKeyMutations(genome, loadout));
        }
    }

    public class CellToCreatureDnaBridgeTests
    {
        [Test]
        public void CreateFromCell_CopiesLegacyTraitsAndAddsVisibleParts()
        {
            var genome = new Genome
            {
                Speed = 7f,
                Flagella = 3f,
                Size = 5f,
                Defence = 5f,
                Attack = 7f,
                Spikes = 2f,
                Color = Color.cyan,
                Traits = GeneticTraits.Toxic | GeneticTraits.Armoured
            };
            var loadout = new CellPartLoadout();
            loadout.TryUnlock(CellPartType.Flagella);
            loadout.TryUnlock(CellPartType.Cilia);
            loadout.TryUnlock(CellPartType.Spike);
            loadout.TryUnlock(CellPartType.Jet);

            var dna = CellToCreatureDnaBridge.CreateFromCell(genome, loadout);

            Assert.AreEqual((int)genome.Traits, dna.LegacyTraitMask);
            Assert.AreEqual("toxin_burst", dna.LegacyAbilityId);
            Assert.AreEqual(genome.Color, dna.Colors.PrimaryColor);
            Assert.AreEqual(new Color(0.72f, 0.2f, 0.88f), dna.Colors.AccentColor);
            Assert.IsTrue(dna.Body.BodyScale.x > 0f);
            Assert.IsTrue(dna.Parts.Any(part => part.PartId == "tail_basic"));
            Assert.IsTrue(dna.Parts.Any(part => part.PartId == "wing_basic"));
            Assert.IsTrue(dna.Parts.Any(part => part.PartId == "spike_basic"));
            Assert.IsTrue(dna.Parts.Any(part => part.PartId == "shell_basic"));
            Assert.IsTrue(dna.Parts.Any(part => part.PartId == "fin_basic"));
            StringAssert.Contains("Cell legacy:", dna.LegacySummary);
        }

        [Test]
        public void ApplyCellLegacy_DoesNotDuplicateExistingLegacyPart()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Parts.Add(new PartPlacement { PartId = "tail_basic" });
            var genome = new Genome { Speed = 10f, Flagella = 10f };

            CellToCreatureDnaBridge.ApplyCellLegacy(dna, genome, null);

            Assert.AreEqual(1, dna.Parts.Count(part => part.PartId == "tail_basic"));
            Assert.AreEqual("dash", dna.LegacyAbilityId);
        }

        [TestCase(GeneticTraits.Electric, "bioelectric_burst")]
        [TestCase(GeneticTraits.Toxic, "toxin_burst")]
        [TestCase(GeneticTraits.Regenerative, "regenerative_surge")]
        [TestCase(GeneticTraits.Armoured, "guard_surge")]
        public void ResolveInheritedAbilityId_MapsTraitsToAbility(GeneticTraits trait, string expectedAbility)
        {
            Assert.AreEqual(expectedAbility, CellToCreatureDnaBridge.ResolveInheritedAbilityId(new Genome { Traits = trait }, null));
        }
    }

    public class CreatureStageObjectivePlannerTests
    {
        [Test]
        public void CreatureStageObjective_ClampsTargetsAndIdentifiesHoldTimer()
        {
            var objective = new CreatureStageObjective(CreatureStageObjectiveType.HoldFeedingGround, -2, -4f, "key");

            Assert.AreEqual(1, objective.RivalDefeatTarget);
            Assert.AreEqual(1f, objective.HoldSecondsTarget);
            Assert.IsTrue(objective.UsesHoldTimer);
        }

        [Test]
        public void CreateObjectiveForSlot_ReturnsAllObjectiveTypes()
        {
            Assert.AreEqual(CreatureStageObjectiveType.RivalCull, CreatureStageObjectivePlanner.CreateObjectiveForSlot(0).Type);
            Assert.AreEqual(CreatureStageObjectiveType.AlphaHunt, CreatureStageObjectivePlanner.CreateObjectiveForSlot(1).Type);
            Assert.AreEqual(CreatureStageObjectiveType.HoldFeedingGround, CreatureStageObjectivePlanner.CreateObjectiveForSlot(2).Type);
            Assert.AreEqual(CreatureStageObjectiveType.PacifyRivals, CreatureStageObjectivePlanner.CreateObjectiveForSlot(3).Type);
            Assert.AreEqual(CreatureStageObjectiveType.AlphaHunt, CreatureStageObjectivePlanner.CreateObjectiveForSlot(-1).Type);
        }

        [Test]
        public void IsInsideFeedingGround_IgnoresHeightAndUsesConfiguredRadius()
        {
            Assert.IsTrue(CreatureStageObjectivePlanner.IsInsideFeedingGround(new Vector3(CreatureStageObjectivePlanner.FeedingGroundRadius, 99f, 0f)));
            Assert.IsFalse(CreatureStageObjectivePlanner.IsInsideFeedingGround(new Vector3(CreatureStageObjectivePlanner.FeedingGroundRadius + 0.2f, -99f, 0f)));
        }

        [Test]
        public void ComputeObjectiveSlotForCreature_LeavesFirstObjectiveUnbiased()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Parts.Add(new PartPlacement { PartId = "horn_basic", AttachBone = 0, Offset = Vector3.forward * 0.25f });
            dna.Parts.Add(new PartPlacement { PartId = "spike_basic", AttachBone = 1, Offset = Vector3.forward * 0.1f });

            int baseSlot = CreatureStageObjectivePlanner.ComputeObjectiveSlot(
                CreatureUpgradeCatalog.GetEvolutionLevel(dna),
                dna.Parts.Count,
                completedObjectives: 0);
            int routedSlot = CreatureStageObjectivePlanner.ComputeObjectiveSlotForCreature(dna, completedObjectives: 0);

            Assert.AreEqual(baseSlot, routedSlot);
        }

        [Test]
        public void ComputeBuildBiasOffset_ReturnsExpectedRouteBiases()
        {
            var aggressive = CreatureDNA.CreateDefault();
            aggressive.Parts.Add(new PartPlacement { PartId = "horn_basic", AttachBone = 0, Offset = Vector3.forward * 0.25f });
            aggressive.Parts.Add(new PartPlacement { PartId = "spike_basic", AttachBone = 1, Offset = Vector3.forward * 0.1f });
            aggressive.Parts.Add(new PartPlacement { PartId = "spike_basic", AttachBone = 1, Offset = Vector3.back * 0.1f });
            int aggressiveBias = CreatureStageObjectivePlanner.ComputeBuildBiasOffset(
                aggressive,
                CreatureUpgradeCatalog.GetEvolutionLevel(aggressive),
                aggressive.Parts.Count);
            Assert.AreEqual(1, aggressiveBias);

            var socialDefensive = CreatureDNA.CreateDefault();
            socialDefensive.Parts.Add(new PartPlacement { PartId = "eye_basic", AttachBone = 0, Offset = new Vector3(0f, 0.28f, 0.3f) });
            socialDefensive.Parts.Add(new PartPlacement { PartId = "arm_basic", AttachBone = 1, Offset = new Vector3(0.45f, 0f, 0.2f) });
            socialDefensive.Parts.Add(new PartPlacement { PartId = "shell_basic", AttachBone = 1, Offset = new Vector3(0f, 0.18f, -0.1f) });
            int socialBias = CreatureStageObjectivePlanner.ComputeBuildBiasOffset(
                socialDefensive,
                CreatureUpgradeCatalog.GetEvolutionLevel(socialDefensive),
                socialDefensive.Parts.Count);
            Assert.AreEqual(3, socialBias);
        }
    }

    public class CreatureSupportUtilityTests
    {
        [Test]
        public void CreatureHealingZone_ComputePulseAlpha_ClampsNegativeTimeAndFrequency()
        {
            Assert.AreEqual(0.5f, CreatureHealingZone.ComputePulseAlpha(-10f, -3f), 0.0001f);
            Assert.AreEqual(1f, CreatureHealingZone.ComputePulseAlpha(Mathf.PI * 0.5f, 1f), 0.0001f);
        }

        [Test]
        public void CreatureTargetAssist_IsWithinAssistCone_FlattensAndHandlesZeroVectors()
        {
            Assert.IsTrue(CreatureTargetAssist.IsWithinAssistCone(Vector3.zero, Vector3.right));
            Assert.IsTrue(CreatureTargetAssist.IsWithinAssistCone(Vector3.forward + Vector3.up * 10f, Vector3.forward));
            Assert.IsFalse(CreatureTargetAssist.IsWithinAssistCone(Vector3.forward, Vector3.back, 0.1f));
        }

        [Test]
        public void CreatureTargetAssist_ComputeTargetScore_PrefersForwardZoneTargets()
        {
            float assisted = CreatureTargetAssist.ComputeTargetScore(distance: 3f, forwardDot: 1f, overlapsAttackZone: true);
            float distantBehind = CreatureTargetAssist.ComputeTargetScore(distance: 12f, forwardDot: -1f, overlapsAttackZone: false);

            Assert.Greater(assisted, distantBehind);
        }
    }

    public class CreatureRelationshipUtilityTests
    {
        [Test]
        public void EvaluateSpeciesDisposition_MapsScoreBands()
        {
            Assert.AreEqual(CreatureSpeciesDisposition.Hostile, CreatureStage.EvaluateSpeciesDisposition(-3f));
            Assert.AreEqual(CreatureSpeciesDisposition.Annoyed, CreatureStage.EvaluateSpeciesDisposition(-0.5f));
            Assert.AreEqual(CreatureSpeciesDisposition.Curious, CreatureStage.EvaluateSpeciesDisposition(0.6f));
            Assert.AreEqual(CreatureSpeciesDisposition.Friendly, CreatureStage.EvaluateSpeciesDisposition(2f));
            Assert.AreEqual(CreatureSpeciesDisposition.Allied, CreatureStage.EvaluateSpeciesDisposition(3.5f));
        }

        [Test]
        public void ComputeSpeciesRelationshipDelta_ReflectsCombatIntent()
        {
            Assert.Less(CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Kill), 0f);
            Assert.Less(CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Hit), 0f);
            Assert.Greater(CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Guard), 0f);
            Assert.Greater(CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Counter), 0f);
            Assert.AreEqual(0f, CreatureStage.ComputeSpeciesRelationshipDelta(CreatureCombatFeedbackType.Telegraph));
        }
    }
}
