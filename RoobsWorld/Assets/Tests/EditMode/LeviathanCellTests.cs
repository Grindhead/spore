using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for the leviathan feature.
    /// Validates balance constants, localization keys, and achievement catalog entries.
    /// (Runtime behaviour — FSM states, physics, contact damage — requires PlayMode tests.)
    /// </summary>
    public class LeviathanCellTests
    {
        // ---- BalanceConstants sanity ----

        [Test]
        public void LeviathanMinSpawnSeconds_IsPositive()
        {
            Assert.Greater(BalanceConstants.LeviathanMinSpawnSeconds, 0f);
        }

        [Test]
        public void LeviathanMinSpeciesCount_AtLeastTwo()
        {
            Assert.GreaterOrEqual(BalanceConstants.LeviathanMinSpeciesCount, 2);
        }

        [Test]
        public void LeviathanRespawnInterval_GreaterThanMinSpawn()
        {
            // Respawn time should be at least as long as the initial spawn delay
            Assert.GreaterOrEqual(
                BalanceConstants.LeviathanRespawnIntervalSeconds,
                BalanceConstants.LeviathanMinSpawnSeconds);
        }

        [Test]
        public void LeviathanSize_LargerThanTypicalCell()
        {
            // Normal cell scale is ~1; leviathan should be significantly bigger
            Assert.Greater(BalanceConstants.LeviathanSize, 4f);
        }

        [Test]
        public void LeviathanMaxHealth_IsSubstantial()
        {
            Assert.Greater(BalanceConstants.LeviathanMaxHealth, 50f);
        }

        [Test]
        public void LeviathanDamagePerHit_Positive()
        {
            Assert.Greater(BalanceConstants.LeviathanDamagePerHit, 0f);
        }

        [Test]
        public void LeviathanChargeSpeed_FasterThanHuntSpeed()
        {
            Assert.Greater(BalanceConstants.LeviathanChargeSpeed, BalanceConstants.LeviathanHuntSpeed);
        }

        [Test]
        public void LeviathanHuntSpeed_FasterThanPatrolSpeed()
        {
            Assert.Greater(BalanceConstants.LeviathanHuntSpeed, BalanceConstants.LeviathanPatrolSpeed);
        }

        [Test]
        public void LeviathanDetectionRadius_LargerThanChargeWindupRadius()
        {
            Assert.Greater(BalanceConstants.LeviathanDetectionRadius, BalanceConstants.LeviathanChargeWindupRadius);
        }

        [Test]
        public void LeviathanSurviveRadius_LargerThanDetectionRadius()
        {
            // The "survived" threshold must be outside detection so the player has to actually escape
            Assert.Greater(BalanceConstants.LeviathanSurviveRadius, BalanceConstants.LeviathanDetectionRadius);
        }

        [Test]
        public void LeviathanFoodDropCount_Positive()
        {
            Assert.Greater(BalanceConstants.LeviathanFoodDropCount, 0);
        }

        [Test]
        public void LeviathanFoodDropNutrition_Positive()
        {
            Assert.Greater(BalanceConstants.LeviathanFoodDropNutrition, 0f);
        }

        // ---- Localization keys ----

        [Test]
        public void LocalizationKey_Warning_Exists()
        {
            string val = Localization.Get("cell.leviathan.warning");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.leviathan.warning", val, "Key should resolve to a real string");
        }

        [Test]
        public void LocalizationKey_Defeated_Exists()
        {
            string val = Localization.Get("cell.leviathan.defeated");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.leviathan.defeated", val);
        }

        [Test]
        public void LocalizationKey_Tutorial_Exists()
        {
            string val = Localization.Get("cell.leviathan.tutorial");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.leviathan.tutorial", val);
        }

        // ---- Achievement catalog ----

        [Test]
        public void Achievement_SurvivedLeviathan_InCatalog()
        {
            var def = AchievementCatalog.Get("cell_survived_leviathan");
            Assert.IsNotNull(def, "cell_survived_leviathan must be in AchievementCatalog");
            Assert.AreEqual(AchievementCategory.Cell, def.Category);
            Assert.Greater(def.Points, 0);
        }

        [Test]
        public void Achievement_DefeatedLeviathan_InCatalog()
        {
            var def = AchievementCatalog.Get("cell_defeated_leviathan");
            Assert.IsNotNull(def, "cell_defeated_leviathan must be in AchievementCatalog");
            Assert.AreEqual(AchievementCategory.Cell, def.Category);
            Assert.IsTrue(def.Hidden, "Defeating a leviathan should be a hidden (surprise) achievement");
        }

        [Test]
        public void Achievement_DefeatedLeviathan_HigherPointsThanSurvived()
        {
            var survived = AchievementCatalog.Get("cell_survived_leviathan");
            var defeated = AchievementCatalog.Get("cell_defeated_leviathan");
            Assert.IsNotNull(survived);
            Assert.IsNotNull(defeated);
            Assert.Greater(defeated.Points, survived.Points,
                "Defeating a leviathan should be worth more than merely surviving it");
        }

        [Test]
        public void CopyActiveInstances_IncludesActiveLeviathan()
        {
            var go = new GameObject("Leviathan_Active");
            var leviathan = go.AddComponent<LeviathanCell>();
            try
            {
                var buffer = new List<LeviathanCell>();

                int count = LeviathanCell.CopyActiveInstances(buffer);

                Assert.AreEqual(buffer.Count, count);
                Assert.Contains(leviathan, buffer);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CopyActiveInstances_ExcludesInactiveLeviathan()
        {
            var go = new GameObject("Leviathan_Inactive");
            var leviathan = go.AddComponent<LeviathanCell>();
            try
            {
                go.SetActive(false);
                var buffer = new List<LeviathanCell>();

                int count = LeviathanCell.CopyActiveInstances(buffer);

                Assert.AreEqual(buffer.Count, count);
                Assert.IsFalse(buffer.Contains(leviathan));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CopyActiveInstances_NullBuffer_ReturnsZero()
        {
            int count = LeviathanCell.CopyActiveInstances(null);
            Assert.AreEqual(0, count);
        }
    }
}
