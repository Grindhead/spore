using NUnit.Framework;
using RoobsWorld.Core;
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
    }
}
