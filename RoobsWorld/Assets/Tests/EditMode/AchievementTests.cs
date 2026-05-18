using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests.EditMode
{
    public class AchievementTrackerTests
    {
        [Test]
        public void TryUnlock_NewId_ReturnsTrueAndUnlocks()
        {
            var tracker = new AchievementTracker();
            Assert.IsTrue(tracker.TryUnlock("cell_first_eat"));
            Assert.IsTrue(tracker.IsUnlocked("cell_first_eat"));
            Assert.AreEqual(1, tracker.UnlockedCount);
        }

        [Test]
        public void TryUnlock_SameIdTwice_ReturnsFalseSecondTime()
        {
            var tracker = new AchievementTracker();
            Assert.IsTrue(tracker.TryUnlock("cell_first_eat"));
            Assert.IsFalse(tracker.TryUnlock("cell_first_eat"));
            Assert.AreEqual(1, tracker.UnlockedCount);
        }

        [Test]
        public void IsUnlocked_UnknownId_ReturnsFalse()
        {
            var tracker = new AchievementTracker();
            Assert.IsFalse(tracker.IsUnlocked("cell_first_eat"));
        }

        [Test]
        public void TotalPoints_SumsPointsFromUnlockedDefs()
        {
            var tracker = new AchievementTracker();
            tracker.TryUnlock("cell_first_eat"); // 10 pts
            tracker.TryUnlock("cell_evolve");   // 50 pts
            Assert.AreEqual(60, tracker.TotalPoints);
        }

        [Test]
        public void ToJson_And_FromJson_RoundTrip()
        {
            var tracker = new AchievementTracker();
            tracker.TryUnlock("cell_first_eat");
            tracker.TryUnlock("cell_first_goal");

            string json = tracker.ToJson();
            var restored = AchievementTracker.FromJson(json);

            Assert.AreEqual(2, restored.UnlockedCount);
            Assert.IsTrue(restored.IsUnlocked("cell_first_eat"));
            Assert.IsTrue(restored.IsUnlocked("cell_first_goal"));
        }

        [Test]
        public void FromJson_EmptyOrNull_ReturnsEmptyTracker()
        {
            var empty = AchievementTracker.FromJson(null);
            Assert.AreEqual(0, empty.UnlockedCount);

            var empty2 = AchievementTracker.FromJson("");
            Assert.AreEqual(0, empty2.UnlockedCount);
        }

        [Test]
        public void Reset_ClearsAllUnlocked()
        {
            var tracker = new AchievementTracker();
            tracker.TryUnlock("cell_first_eat");
            tracker.Reset();
            Assert.AreEqual(0, tracker.UnlockedCount);
            Assert.IsFalse(tracker.IsUnlocked("cell_first_eat"));
        }
    }

    public class AchievementCatalogTests
    {
        [Test]
        public void Get_KnownId_ReturnsDef()
        {
            var def = AchievementCatalog.Get("cell_first_eat");
            Assert.IsNotNull(def);
            Assert.AreEqual("cell_first_eat", def.Id);
            Assert.AreEqual("First Bite", def.Title);
            Assert.AreEqual(10, def.Points);
            Assert.AreEqual(AchievementCategory.Cell, def.Category);
        }

        [Test]
        public void Get_CellFirstGoal_ReturnsMicroGoalAchievement()
        {
            var def = AchievementCatalog.Get("cell_first_goal");
            Assert.IsNotNull(def);
            Assert.AreEqual("cell_first_goal", def.Id);
            Assert.AreEqual("On the Hunt", def.Title);
            Assert.AreEqual(15, def.Points);
            Assert.AreEqual(AchievementCategory.Cell, def.Category);
        }

        [Test]
        public void Get_UnknownId_ReturnsNull()
        {
            Assert.IsNull(AchievementCatalog.Get("unknown_achievement"));
        }

        [Test]
        public void GetSteamApiName_MapsKnownIdsToSteamStyleNames()
        {
            Assert.AreEqual("CELL_FIRST_EAT", AchievementCatalog.GetSteamApiName("cell_first_eat"));
            Assert.AreEqual("CREATURE_APEX_STREAK", AchievementCatalog.GetSteamApiName("creature_apex_streak"));
        }

        [Test]
        public void GetSteamApiName_UnknownIdFallsBackToUppercaseId()
        {
            Assert.AreEqual("CUSTOM_UNLOCK", AchievementCatalog.GetSteamApiName("custom_unlock"));
        }

        [Test]
        public void GetPlayStationTrophyId_MapsKnownIds()
        {
            Assert.AreEqual("cell_first_eat", AchievementCatalog.GetPlayStationTrophyId("cell_first_eat"));
            Assert.AreEqual("creature_apex_streak", AchievementCatalog.GetPlayStationTrophyId("creature_apex_streak"));
        }

        [Test]
        public void GetXboxAchievementId_MapsKnownIds()
        {
            Assert.AreEqual("cell_first_eat", AchievementCatalog.GetXboxAchievementId("cell_first_eat"));
            Assert.AreEqual("creature_apex_streak", AchievementCatalog.GetXboxAchievementId("creature_apex_streak"));
        }

        [Test]
        public void AllAchievements_HavePlatformMappings()
        {
            foreach (var def in AchievementCatalog.GetAll())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.Id), "Missing local id");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.SteamApiName), $"{def.Id} missing Steam API name");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.PlayStationTrophyId), $"{def.Id} missing PlayStation trophy id");
                Assert.IsFalse(string.IsNullOrWhiteSpace(def.XboxAchievementId), $"{def.Id} missing Xbox achievement id");
            }
        }

        [Test]
        public void GetAll_ReturnsAllDefs()
        {
            var all = AchievementCatalog.GetAll();
            Assert.GreaterOrEqual(all.Length, 10);
        }

        [Test]
        public void GetByCategory_ReturnsOnlyThatCategory()
        {
            var cell = AchievementCatalog.GetByCategory(AchievementCategory.Cell);
            Assert.GreaterOrEqual(cell.Length, 5);
            foreach (var def in cell)
                Assert.AreEqual(AchievementCategory.Cell, def.Category);
        }

        [Test]
        public void GetByCategory_ExcludesHiddenByDefault()
        {
            var cell = AchievementCatalog.GetByCategory(AchievementCategory.Cell, includeHidden: false);
            foreach (var def in cell)
                Assert.IsFalse(def.Hidden);
        }

        [Test]
        public void GetByCategory_IncludeHidden_ContainsHidden()
        {
            var cell = AchievementCatalog.GetByCategory(AchievementCategory.Cell, includeHidden: true);
            bool hasHidden = false;
            foreach (var def in cell)
                if (def.Hidden) hasHidden = true;
            Assert.IsTrue(hasHidden);
        }

        [Test]
        public void Get_CreatureApexStreak_ReturnsDef()
        {
            var def = AchievementCatalog.Get("creature_apex_streak");
            Assert.IsNotNull(def);
            Assert.AreEqual("creature_apex_streak", def.Id);
            Assert.AreEqual(AchievementCategory.Creature, def.Category);
            Assert.AreEqual(35, def.Points);
        }
    }

    /// <summary>
    /// Mock provider that records ReportUnlock calls for tests.
    /// </summary>
    internal sealed class MockPlatformAchievementProvider : IPlatformAchievementProvider
    {
        public System.Collections.Generic.List<string> ReportedIds { get; } = new System.Collections.Generic.List<string>();
        public void ReportUnlock(string achievementId) => ReportedIds.Add(achievementId);
    }

    public class PlatformAchievementProviderTests
    {
        [Test]
        public void PlatformServicesConfig_DefaultsUseDevelopmentPlaceholders()
        {
            Assert.IsTrue(PlatformServicesConfig.UsesDevelopmentSteamAppId);
            Assert.IsTrue(PlatformServicesConfig.UsesPlaceholderPlayStationServiceId);
            Assert.IsTrue(PlatformServicesConfig.UsesPlaceholderXboxTitleId);
        }

        [Test]
        public void LocalAchievementProvider_ReportUnlock_DoesNotThrow()
        {
            var provider = new LocalAchievementProvider();
            Assert.DoesNotThrow(() => provider.ReportUnlock("cell_first_eat"));
        }

        [Test]
        public void UnlockAchievementCore_WhenUnlockSucceeds_CallsProviderReportUnlock()
        {
            var tracker = new AchievementTracker();
            var mock = new MockPlatformAchievementProvider();
            GameManager.UnlockAchievementCore(tracker, mock, "cell_first_eat");
            Assert.AreEqual(1, mock.ReportedIds.Count);
            Assert.AreEqual("cell_first_eat", mock.ReportedIds[0]);
        }

        [Test]
        public void UnlockAchievementCore_WhenUnlockFails_DoesNotCallProvider()
        {
            var tracker = new AchievementTracker();
            tracker.TryUnlock("cell_first_eat");
            var mock = new MockPlatformAchievementProvider();
            GameManager.UnlockAchievementCore(tracker, mock, "cell_first_eat");
            Assert.AreEqual(0, mock.ReportedIds.Count);
        }

        [Test]
        public void UnlockAchievementCore_WhenProviderNull_DoesNotThrow()
        {
            var tracker = new AchievementTracker();
            Assert.DoesNotThrow(() => GameManager.UnlockAchievementCore(tracker, null, "cell_first_eat"));
            Assert.IsTrue(tracker.IsUnlocked("cell_first_eat"));
        }

        [Test]
        public void UnlockAchievementCore_WhenTrackerNull_DoesNotThrow()
        {
            var mock = new MockPlatformAchievementProvider();
            Assert.DoesNotThrow(() => GameManager.UnlockAchievementCore(null, mock, "cell_first_eat"));
            Assert.AreEqual(0, mock.ReportedIds.Count);
        }

        [Test]
        public void SteamRichPresenceStatus_MapsKnownStages()
        {
            Assert.AreEqual("Exploring the primordial ocean", SteamPlatformProvider.BuildRichPresenceStatus(GameStageType.Cell));
            Assert.AreEqual("Hunting in Creature Stage", SteamPlatformProvider.BuildRichPresenceStatus(GameStageType.Creature));
            Assert.AreEqual("In the main menu", SteamPlatformProvider.BuildRichPresenceStatus(GameStageType.None));

            Assert.AreEqual("cell", SteamPlatformProvider.BuildRichPresenceStageKey(GameStageType.Cell));
            Assert.AreEqual("creature", SteamPlatformProvider.BuildRichPresenceStageKey(GameStageType.Creature));
            Assert.AreEqual("menu", SteamPlatformProvider.BuildRichPresenceStageKey(GameStageType.None));
        }
    }
}
