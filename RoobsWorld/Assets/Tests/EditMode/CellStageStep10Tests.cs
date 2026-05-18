using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using RoobsWorld.Stages.Cell;
using RoobsWorld.UI;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for Step 10 (Player Features & Advanced Mechanics) constants and behaviour.
    /// </summary>
    public class CellStageStep10Tests
    {
        [Test]
        public void CellPlayerReproductionMassThreshold_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellPlayerReproductionMassThreshold, 0f,
                "Player reproduction should trigger at a positive mass.");
        }

        [Test]
        public void CellPlayerReproductionMassCost_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellPlayerReproductionMassCost, 0f,
                "Reproduction cost should be positive.");
        }

        [Test]
        public void CellFoodConsumedPerUpgrade_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellFoodConsumedPerUpgrade, 0,
                "Upgrade gating by food requires a positive food count.");
        }

        [Test]
        public void HasConsumedEnoughFoodForUpgrade_UsesConfiguredThreshold()
        {
            int required = BalanceConstants.CellFoodConsumedPerUpgrade;

            Assert.IsFalse(PlayerCell.HasConsumedEnoughFoodForUpgrade(required - 1, required));
            Assert.IsTrue(PlayerCell.HasConsumedEnoughFoodForUpgrade(required, required));
        }

        [Test]
        public void ShouldOfferUpgradePrompt_RequiresMassFoodAndDelay()
        {
            int required = BalanceConstants.CellFoodConsumedPerUpgrade;

            Assert.IsFalse(PlayerCell.ShouldOfferUpgradePrompt(
                BalanceConstants.CellPlayerReproductionMassThreshold - 0.1f,
                required,
                required,
                BalanceConstants.CellPlayerReproductionMassThreshold,
                currentTime: 10f,
                unlockTime: 2f,
                isDead: false,
                isEvolving: false,
                promptActive: false));

            Assert.IsFalse(PlayerCell.ShouldOfferUpgradePrompt(
                BalanceConstants.CellPlayerReproductionMassThreshold,
                required - 1,
                required,
                BalanceConstants.CellPlayerReproductionMassThreshold,
                currentTime: 10f,
                unlockTime: 2f,
                isDead: false,
                isEvolving: false,
                promptActive: false));

            Assert.IsFalse(PlayerCell.ShouldOfferUpgradePrompt(
                BalanceConstants.CellPlayerReproductionMassThreshold,
                required,
                required,
                BalanceConstants.CellPlayerReproductionMassThreshold,
                currentTime: 1f,
                unlockTime: 2f,
                isDead: false,
                isEvolving: false,
                promptActive: false));

            Assert.IsTrue(PlayerCell.ShouldOfferUpgradePrompt(
                BalanceConstants.CellPlayerReproductionMassThreshold,
                required,
                required,
                BalanceConstants.CellPlayerReproductionMassThreshold,
                currentTime: 10f,
                unlockTime: 2f,
                isDead: false,
                isEvolving: false,
                promptActive: false));
        }

        [Test]
        public void ShouldOfferUpgradePrompt_BlocksWhenPlayerCannotChoose()
        {
            int required = BalanceConstants.CellFoodConsumedPerUpgrade;
            float mass = BalanceConstants.CellPlayerReproductionMassThreshold;

            Assert.IsFalse(PlayerCell.ShouldOfferUpgradePrompt(
                mass,
                required,
                required,
                mass,
                currentTime: 10f,
                unlockTime: 2f,
                isDead: true,
                isEvolving: false,
                promptActive: false));

            Assert.IsFalse(PlayerCell.ShouldOfferUpgradePrompt(
                mass,
                required,
                required,
                mass,
                currentTime: 10f,
                unlockTime: 2f,
                isDead: false,
                isEvolving: true,
                promptActive: false));

            Assert.IsFalse(PlayerCell.ShouldOfferUpgradePrompt(
                mass,
                required,
                required,
                mass,
                currentTime: 10f,
                unlockTime: 2f,
                isDead: false,
                isEvolving: false,
                promptActive: true));
        }

        [Test]
        public void DietVarietyChain_ResetsOnRepeatAndGrowsOnSwitch()
        {
            int first = PlayerCell.ComputeDietVarietyChain(null, FoodType.Plant, 0);
            int switched = PlayerCell.ComputeDietVarietyChain(FoodType.Plant, FoodType.Meat, first);
            int repeated = PlayerCell.ComputeDietVarietyChain(FoodType.Meat, FoodType.Meat, switched);

            Assert.AreEqual(1, first);
            Assert.AreEqual(2, switched);
            Assert.AreEqual(1, repeated);
        }

        [Test]
        public void DietVarietyBonus_StartsWhenThresholdIsReached()
        {
            Assert.IsFalse(PlayerCell.ShouldAwardDietVarietyBonus(1, 1, 2));
            Assert.IsTrue(PlayerCell.ShouldAwardDietVarietyBonus(1, 2, 2));
            Assert.IsTrue(PlayerCell.ShouldAwardDietVarietyBonus(2, 3, 2));
        }

        [Test]
        public void ComputeDietVarietyMassBonus_AddsOnlyForActiveChain()
        {
            Assert.That(
                PlayerCell.ComputeDietVarietyMassBonus(0.12f, currentChain: 1, threshold: 2, bonusMultiplier: 0.35f),
                Is.EqualTo(0f).Within(0.0001f));
            Assert.That(
                PlayerCell.ComputeDietVarietyMassBonus(0.12f, currentChain: 2, threshold: 2, bonusMultiplier: 0.35f),
                Is.EqualTo(0.042f).Within(0.0001f));
        }

        [Test]
        public void DietVarietyBonusText_ShowsChainAndDnaReward()
        {
            Assert.AreEqual("Varied diet x2! +1 DNA", CellStageHUD.BuildDietVarietyBonusText(2, 1));
            Assert.AreEqual("Varied diet x1! +0 DNA", CellStageHUD.BuildDietVarietyBonusText(-2, -1));
        }

        [Test]
        public void EvolutionStatsUI_FormatTraits_UsedForMutationChoiceLabels()
        {
            var genome = Genome.Default();
            string traits = EvolutionStatsUI.FormatTraits(genome);
            Assert.IsNotNull(traits);
            Assert.IsTrue(traits.Contains("Speed") || traits.Length > 0,
                "FormatTraits should produce a non-empty label for mutation choice UI.");
        }

        [Test]
        public void MutationChoiceUI_PauseEvent_SuppressesPauseMenu()
        {
            EventBus.Clear();
            if (GameManager.HasInstance)
                Object.DestroyImmediate(GameManager.Instance.gameObject);

            PauseEvent pauseEvent = default;
            bool receivedPauseEvent = false;
            EventBus.Subscribe<PauseEvent>(evt =>
            {
                pauseEvent = evt;
                receivedPauseEvent = true;
            });

            try
            {
                _ = GameManager.Instance;
                MutationChoiceUI.Show(Genome.Default(), _ => { });

                Assert.IsTrue(receivedPauseEvent);
                Assert.IsTrue(pauseEvent.IsPaused);
                Assert.IsTrue(pauseEvent.SuppressPauseMenu);
            }
            finally
            {
                var ui = Object.FindAnyObjectByType<MutationChoiceUI>();
                if (ui != null)
                    Object.DestroyImmediate(ui.gameObject);

                if (AudioManager.HasInstance)
                    Object.DestroyImmediate(AudioManager.Instance.gameObject);
                if (GameManager.HasInstance)
                    Object.DestroyImmediate(GameManager.Instance.gameObject);
                EventBus.Clear();
                Time.timeScale = 1f;
            }
        }

        [Test]
        public void CellEvolutionUI_ShowScreen_PauseEvent_SuppressesPauseMenu()
        {
            EventBus.Clear();
            if (GameManager.HasInstance)
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            if (UIManager.HasInstance)
                Object.DestroyImmediate(UIManager.Instance.gameObject);

            PauseEvent pauseEvent = default;
            bool receivedPauseEvent = false;
            EventBus.Subscribe<PauseEvent>(evt =>
            {
                pauseEvent = evt;
                receivedPauseEvent = true;
            });

            try
            {
                _ = GameManager.Instance;
                CellEvolutionUI.CreateRuntime(null);
                UIManager.Instance.ShowScreen(CellEvolutionUI.ID);

                Assert.IsTrue(receivedPauseEvent);
                Assert.IsTrue(pauseEvent.IsPaused);
                Assert.IsTrue(pauseEvent.SuppressPauseMenu);
            }
            finally
            {
                if (UIManager.HasInstance)
                {
                    UIManager.Instance.HideAll();
                    Object.DestroyImmediate(UIManager.Instance.gameObject);
                }
                var ui = Object.FindAnyObjectByType<CellEvolutionUI>();
                if (ui != null)
                    Object.DestroyImmediate(ui.gameObject);

                if (GameManager.HasInstance)
                    Object.DestroyImmediate(GameManager.Instance.gameObject);
                EventBus.Clear();
                Time.timeScale = 1f;
            }
        }

        [Test]
        public void CellDocumentaryTimeScaleOptions_HasNormalAndSpeedOptions()
        {
            var options = BalanceConstants.CellDocumentaryTimeScaleOptions;
            Assert.IsNotNull(options);
            Assert.GreaterOrEqual(options.Length, 2, "Documentary mode should offer at least 1× and one faster option.");
            Assert.AreEqual(1f, options[0], 0.001f, "First option should be normal speed (1×).");
            for (int i = 0; i < options.Length; i++)
                Assert.Greater(options[i], 0f, $"Time scale option {i} must be positive.");
        }
        [Test]
        public void ComputeSizeSpeedPenalty_KeepsLargeCellsWeightyButPlayable()
        {
            float small = PlayerCell.ComputeSizeSpeedPenalty(0.5f);
            float medium = PlayerCell.ComputeSizeSpeedPenalty(5f);
            float large = PlayerCell.ComputeSizeSpeedPenalty(10f);

            Assert.That(small, Is.EqualTo(1f).Within(0.001f));
            Assert.Greater(medium, large);
            Assert.GreaterOrEqual(large, 0.62f);
        }

        [Test]
        public void MultiplayerScaling_LeavesSoloCellStageCountsUnchanged()
        {
            Assert.AreEqual(60, CellStage.ComputeMultiplayerScaledCount(60, 1, 14, 42));
            Assert.AreEqual(1, CellStage.ComputeMultiplayerPredatorCount(1, 1, 2, 2));
            Assert.That(
                CellStage.ComputeMultiplayerArenaRadius(50f, 1, 0.08f, 0.24f),
                Is.EqualTo(50f).Within(0.001f));
        }

        [Test]
        public void MultiplayerScaling_AddsCappedResourcesForExtraPlayers()
        {
            Assert.AreEqual(102, CellStage.ComputeMultiplayerScaledCount(60, 6, 14, 42));
            Assert.AreEqual(39, CellStage.ComputeMultiplayerScaledCount(24, 4, 5, 15));
            Assert.AreEqual(7, CellStage.ComputeMultiplayerScaledCount(4, 4, 1, 3));
        }

        [Test]
        public void MultiplayerScaling_ExpandsArenaRadiusAndCapsBonus()
        {
            Assert.That(
                CellStage.ComputeMultiplayerArenaRadius(50f, 3, 0.08f, 0.24f),
                Is.EqualTo(58f).Within(0.001f));
            Assert.That(
                CellStage.ComputeMultiplayerArenaRadius(50f, 99, 0.08f, 0.24f),
                Is.EqualTo(62f).Within(0.001f));
        }

        [Test]
        public void MultiplayerScaling_AddsPredatorsOnlyForLargerLobbies()
        {
            Assert.AreEqual(1, CellStage.ComputeMultiplayerPredatorCount(1, 2, 2, 2));
            Assert.AreEqual(2, CellStage.ComputeMultiplayerPredatorCount(1, 3, 2, 2));
            Assert.AreEqual(3, CellStage.ComputeMultiplayerPredatorCount(1, 99, 2, 2));
        }

        [Test]
        public void ActiveCellPlayerCount_UsesSessionCountWithSoloFallback()
        {
            Assert.AreEqual(1, CellStage.GetActiveCellPlayerCount(null));

            var session = new MultiplayerSessionManager();
            Assert.AreEqual(1, CellStage.GetActiveCellPlayerCount(session));

            session.CreateSession();
            session.AddRemotePlayer(1, "P1");
            Assert.AreEqual(2, CellStage.GetActiveCellPlayerCount(session));
        }

        [Test]
        public void GoalVarietyChain_GrowsOnNewCategoriesAndResetsOnRepeat()
        {
            int chain = CellStage.ComputeGoalVarietyChain(
                hasPreviousCategory: false,
                CellGoal.GoalActionCategory.Feeding,
                CellGoal.GoalActionCategory.Feeding,
                currentChain: 0);
            Assert.AreEqual(1, chain);

            chain = CellStage.ComputeGoalVarietyChain(
                hasPreviousCategory: true,
                CellGoal.GoalActionCategory.Feeding,
                CellGoal.GoalActionCategory.Escaping,
                chain);
            Assert.AreEqual(2, chain);

            chain = CellStage.ComputeGoalVarietyChain(
                hasPreviousCategory: true,
                CellGoal.GoalActionCategory.Escaping,
                CellGoal.GoalActionCategory.Escaping,
                chain);
            Assert.AreEqual(1, chain);
        }

        [Test]
        public void GoalVarietyDnaBonus_StartsAtThresholdAndCaps()
        {
            Assert.AreEqual(0, CellStage.ComputeGoalVarietyDnaBonus(1, 2, 1, 3));
            Assert.AreEqual(1, CellStage.ComputeGoalVarietyDnaBonus(2, 2, 1, 3));
            Assert.AreEqual(2, CellStage.ComputeGoalVarietyDnaBonus(3, 2, 1, 3));
            Assert.AreEqual(3, CellStage.ComputeGoalVarietyDnaBonus(8, 2, 1, 3));
            Assert.AreEqual(0, CellStage.ComputeGoalVarietyDnaBonus(8, 2, 0, 3));
        }
    }
}
