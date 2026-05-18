using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class TutorialDataTests
    {
        [TearDown]
        public void ClearTutorialPrefs()
        {
            PlayerPrefs.DeleteKey("TutorialProgress_Completed");
            PlayerPrefs.DeleteKey("TutorialProgress_Steps");
            PlayerPrefs.Save();
        }
        [Test]
        public void Cell_Stage_Has_Tutorial_Steps()
        {
            var steps = TutorialCatalog.GetSteps(GameStageType.Cell);
            Assert.IsNotNull(steps);
            Assert.Greater(steps.Length, 0);
        }

        [Test]
        public void Creature_Stage_Has_Tutorial_Steps()
        {
            var steps = TutorialCatalog.GetSteps(GameStageType.Creature);
            Assert.IsNotNull(steps);
            Assert.Greater(steps.Length, 0);
        }

        [Test]
        public void None_Stage_Has_No_Tutorial_Steps()
        {
            var steps = TutorialCatalog.GetSteps(GameStageType.None);
            Assert.IsNull(steps);
        }

        [Test]
        public void Each_Step_Has_NonEmpty_Message()
        {
            var stages = new[] { GameStageType.Cell, GameStageType.Creature };

            foreach (var stage in stages)
            {
                var steps = TutorialCatalog.GetSteps(stage);
                for (int i = 0; i < steps.Length; i++)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(steps[i].Message),
                        $"Stage {stage} step {i} has empty message");
                }
            }
        }

        [Test]
        public void Each_Step_Has_Positive_Duration()
        {
            var stages = new[] { GameStageType.Cell, GameStageType.Creature };

            foreach (var stage in stages)
            {
                var steps = TutorialCatalog.GetSteps(stage);
                for (int i = 0; i < steps.Length; i++)
                {
                    Assert.Greater(steps[i].DisplayDuration, 0f,
                        $"Stage {stage} step {i} has non-positive duration");
                }
            }
        }

        [Test]
        public void TutorialProgress_Tracks_Completion()
        {
            var progress = new TutorialProgress();
            Assert.IsFalse(progress.IsStageCompleted(GameStageType.Cell));

            progress.MarkStageCompleted(GameStageType.Cell);
            Assert.IsTrue(progress.IsStageCompleted(GameStageType.Cell));
            Assert.IsFalse(progress.IsStageCompleted(GameStageType.None));
        }

        [Test]
        public void TutorialProgress_Reset_Clears_All()
        {
            var progress = new TutorialProgress();
            progress.MarkStageCompleted(GameStageType.Cell);
            progress.MarkStageCompleted(GameStageType.None);

            progress.Reset();

            Assert.IsFalse(progress.IsStageCompleted(GameStageType.Cell));
            Assert.IsFalse(progress.IsStageCompleted(GameStageType.None));
        }

        [Test]
        public void TutorialProgress_Reset_ClearsStepIndexes()
        {
            var progress = new TutorialProgress();
            progress.AdvanceStep(GameStageType.Cell);
            progress.AdvanceStep(GameStageType.Cell);
            progress.SetStepIndex(GameStageType.None, 3);

            progress.Reset();

            Assert.AreEqual(0, progress.GetStepIndex(GameStageType.Cell));
            Assert.AreEqual(0, progress.GetStepIndex(GameStageType.None));
        }

        [Test]
        public void TutorialProgress_StepIndex_Starts_At_Zero()
        {
            var progress = new TutorialProgress();
            Assert.AreEqual(0, progress.GetStepIndex(GameStageType.Cell));
        }

        [Test]
        public void TutorialProgress_AdvanceStep_Increments()
        {
            var progress = new TutorialProgress();
            progress.AdvanceStep(GameStageType.Cell);
            Assert.AreEqual(1, progress.GetStepIndex(GameStageType.Cell));
            progress.AdvanceStep(GameStageType.Cell);
            Assert.AreEqual(2, progress.GetStepIndex(GameStageType.Cell));
        }

        [Test]
        public void TutorialProgress_SaveToPlayerPrefs_And_LoadFromPlayerPrefs_RoundTrip()
        {
            var progress = new TutorialProgress();
            progress.MarkStageCompleted(GameStageType.Cell);
            progress.MarkStageCompleted(GameStageType.Creature);
            progress.SetStepIndex(GameStageType.None, 2);
            progress.SaveToPlayerPrefs();

            var loaded = TutorialProgress.LoadFromPlayerPrefs();
            Assert.IsTrue(loaded.IsStageCompleted(GameStageType.Cell));
            Assert.IsTrue(loaded.IsStageCompleted(GameStageType.Creature));
            Assert.AreEqual(2, loaded.GetStepIndex(GameStageType.None));
            Assert.IsFalse(loaded.IsStageCompleted(GameStageType.None));
        }

        [Test]
        public void Creature_Tutorial_Localization_Keys_Are_Defined()
        {
            var steps = TutorialCatalog.GetSteps(GameStageType.Creature);
            Assert.IsNotNull(steps);

            for (int i = 0; i < steps.Length; i++)
            {
                string key = steps[i].Message;
                string localized = Localization.Get(key);
                Assert.AreNotEqual(key, localized, $"Missing localization for key: {key}");
            }

            string controlsHint = Localization.Get("creature.controls.hint");
            Assert.IsFalse(string.IsNullOrWhiteSpace(controlsHint));
            Assert.AreNotEqual("creature.controls.hint", controlsHint);

            string encounterProgress = Localization.Get("creature.encounter.progress");
            Assert.IsFalse(string.IsNullOrWhiteSpace(encounterProgress));
            Assert.AreNotEqual("creature.encounter.progress", encounterProgress);

            string alphaProgress = Localization.Get("creature.objective.alpha_progress");
            Assert.IsFalse(string.IsNullOrWhiteSpace(alphaProgress));
            Assert.AreNotEqual("creature.objective.alpha_progress", alphaProgress);

            string holdSecure = Localization.Get("creature.objective.hold_state.secure");
            Assert.IsFalse(string.IsNullOrWhiteSpace(holdSecure));
            Assert.AreNotEqual("creature.objective.hold_state.secure", holdSecure);

            string holdReturn = Localization.Get("creature.objective.hold_state.return");
            Assert.IsFalse(string.IsNullOrWhiteSpace(holdReturn));
            Assert.AreNotEqual("creature.objective.hold_state.return", holdReturn);

            string pacifyObjective = Localization.Get("creature.objective.pacify");
            Assert.IsFalse(string.IsNullOrWhiteSpace(pacifyObjective));
            Assert.AreNotEqual("creature.objective.pacify", pacifyObjective);

            string pacifyGuidance = Localization.Get("creature.objective.guidance.pacify");
            Assert.IsFalse(string.IsNullOrWhiteSpace(pacifyGuidance));
            Assert.AreNotEqual("creature.objective.guidance.pacify", pacifyGuidance);

            string relationshipShift = Localization.Get("creature.relationship.shift");
            Assert.IsFalse(string.IsNullOrWhiteSpace(relationshipShift));
            Assert.AreNotEqual("creature.relationship.shift", relationshipShift);

            string relationshipHostile = Localization.Get("creature.relationship.hostile");
            Assert.IsFalse(string.IsNullOrWhiteSpace(relationshipHostile));
            Assert.AreNotEqual("creature.relationship.hostile", relationshipHostile);

            string encounterComplete = Localization.Get("creature.encounter.complete");
            Assert.IsFalse(string.IsNullOrWhiteSpace(encounterComplete));
            Assert.AreNotEqual("creature.encounter.complete", encounterComplete);

            string downedTip = Localization.Get("creature.player.down");
            Assert.IsFalse(string.IsNullOrWhiteSpace(downedTip));
            Assert.AreNotEqual("creature.player.down", downedTip);

            string shieldedTip = Localization.Get("creature.player.shielded");
            Assert.IsFalse(string.IsNullOrWhiteSpace(shieldedTip));
            Assert.AreNotEqual("creature.player.shielded", shieldedTip);

            string statusHunt = Localization.Get("creature.status.hunt");
            Assert.IsFalse(string.IsNullOrWhiteSpace(statusHunt));
            Assert.AreNotEqual("creature.status.hunt", statusHunt);

            string statusShielded = Localization.Get("creature.status.shielded");
            Assert.IsFalse(string.IsNullOrWhiteSpace(statusShielded));
            Assert.AreNotEqual("creature.status.shielded", statusShielded);

            string statusLowHealth = Localization.Get("creature.status.low_health");
            Assert.IsFalse(string.IsNullOrWhiteSpace(statusLowHealth));
            Assert.AreNotEqual("creature.status.low_health", statusLowHealth);

            string streakTip = Localization.Get("creature.streak.bonus.tip");
            Assert.IsFalse(string.IsNullOrWhiteSpace(streakTip));
            Assert.AreNotEqual("creature.streak.bonus.tip", streakTip);

            string safeRespawnTip = Localization.Get("creature.player.safe_respawn");
            Assert.IsFalse(string.IsNullOrWhiteSpace(safeRespawnTip));
            Assert.AreNotEqual("creature.player.safe_respawn", safeRespawnTip);

            string lowHealthTip = Localization.Get("creature.player.low_health");
            Assert.IsFalse(string.IsNullOrWhiteSpace(lowHealthTip));
            Assert.AreNotEqual("creature.player.low_health", lowHealthTip);
        }
    }
}
