using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class LocalizationTests
    {
        [Test]
        public void Get_ReturnsExpectedValue_ForMenuTitle()
        {
            Assert.AreEqual("ROOB'S WORLD", Localization.Get("menu.title"));
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForMenuVersionSuffix()
        {
            Assert.AreEqual("Pre-Alpha", Localization.Get("menu.version_suffix"));
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForAchievementUnlocked()
        {
            Assert.AreEqual("Achievement Unlocked!", Localization.Get("achievement.unlocked"));
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForSettingsKeys()
        {
            Assert.AreEqual("SETTINGS", Localization.Get("settings.title"));
            Assert.AreEqual("Graphics", Localization.Get("settings.graphics"));
            Assert.AreEqual("Audio", Localization.Get("settings.audio"));
            Assert.AreEqual("Controls", Localization.Get("settings.controls"));
            Assert.AreEqual("Back", Localization.Get("settings.back"));
            Assert.AreEqual("Apply", Localization.Get("settings.apply"));
            Assert.AreEqual("Reset to Defaults", Localization.Get("settings.reset"));
        }

        [Test]
        public void Get_ReturnsKey_WhenKeyMissing()
        {
            string result = Localization.Get("nonexistent.key.xyz");
            Assert.AreEqual("nonexistent.key.xyz", result);
        }

        [Test]
        public void Get_ReturnsKey_WhenKeyNullOrEmpty()
        {
            Assert.AreEqual(null, Localization.Get(null));
            Assert.AreEqual("", Localization.Get(""));
        }

        [Test]
        public void Set_AndGet_RoundTrip()
        {
            Localization.Set("EN", "test.key.temp", "Temp Value");
            Assert.AreEqual("Temp Value", Localization.Get("test.key.temp"));
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForQualityKeys()
        {
            Assert.AreEqual("Low", Localization.Get("settings.quality_low"));
            Assert.AreEqual("Medium", Localization.Get("settings.quality_medium"));
            Assert.AreEqual("High", Localization.Get("settings.quality_high"));
            Assert.AreEqual("Ultra", Localization.Get("settings.quality_ultra"));
        }

        [Test]
        public void Get_ReturnsNonEmptyTranslatedText_ForAllTutorialKeys()
        {
            const string stage = "cell";
            const int stepCount = 7;
            for (int i = 0; i < stepCount; i++)
            {
                string key = $"tutorial.{stage}.{i}";
                string value = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(value), $"Key {key} should have non-empty value");
                Assert.AreNotEqual(key, value, $"Key {key} should be translated, not returned as-is");
            }
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForCellGoalKeys()
        {
            Assert.AreEqual("Eat {0} {1}", Localization.Get("cell.goal.task"));
            Assert.AreEqual(" ({0}/{1})", Localization.Get("cell.goal.progress"));
            Assert.AreEqual("algae", Localization.Get("cell.goal.food_plant"));
            Assert.AreEqual("dead cells", Localization.Get("cell.goal.food_meat"));
            Assert.AreEqual("toxin", Localization.Get("cell.goal.food_toxin"));
            Assert.AreEqual("Escape {0} predator(s)", Localization.Get("cell.goal.escape_predator_task"));
            Assert.AreEqual("Goal complete! +{0} DNA", Localization.Get("cell.goal.complete_message"));
            Assert.AreEqual("Upgrade {0} time(s)", Localization.Get("cell.goal.reproduce_task"));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.goal.survive_toxic_task")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.goal.survive_toxic_progress")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.goal.bloom_eat_task")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.goal.eat_larger_task")));
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForCellCurrentIndicator()
        {
            Assert.AreEqual("Current →", Localization.Get("cell.current.indicator"));
        }

        [Test]
        public void Get_ReturnsExpectedValue_ForNewGameKeys()
        {
            Assert.AreEqual("Start Game", Localization.Get("newgame.start"));
            Assert.AreEqual("Cancel", Localization.Get("newgame.cancel"));
        }

        [Test]
        public void Get_ReturnsNonEmptyValue_ForAccessibilityKeys()
        {
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("settings.colorblind_mode")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("settings.high_contrast")));
        }

        [Test]
        public void Get_ReturnsNonEmptyValue_ForEnvironmentEventKeys()
        {
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.env.nutrient_bloom")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.env.virus_outbreak")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.env.temperature_change")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.env.radiation_surge")));
            Assert.IsFalse(string.IsNullOrEmpty(Localization.Get("cell.env.radiation_surge_subtitle")));
        }

        [Test]
        public void Get_ReturnsNonEmptyValue_ForTutorialContextualTipKeys()
        {
            var keys = new[]
            {
                "tutorial.cell.tip.first_eat",
                "tutorial.cell.tip.first_evolution",
                "tutorial.cell.tip.first_reproduction",
                "tutorial.cell.tip.predator_warning",
                "tutorial.cell.tip.first_speciation",
                "tutorial.cell.tip.first_extinction",
                "tutorial.cell.tip.ten_species",
                "tutorial.reset",
                "tutorial.cell.goal_intro",
            };
            foreach (string key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Key '{key}' should have a non-empty value");
                Assert.AreNotEqual(key, val, $"Key '{key}' should be translated, not returned as-is");
            }
        }

        [Test]
        public void Get_ReturnsNonEmptyValue_ForRunSummaryKeys()
        {
            var keys = new[]
            {
                "cell.summary.title", "cell.summary.time", "cell.summary.peak_mass",
                "cell.summary.dna", "cell.summary.species", "cell.summary.traits",
                "cell.summary.restart", "cell.summary.menu",
                "cell.summary.new_best", "cell.summary.run_count"
            };
            foreach (string key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Key '{key}' should have a non-empty value");
                Assert.AreNotEqual(key, val, $"Key '{key}' should be translated, not returned as-is");
            }
        }

        [Test]
        public void Get_ReturnsNonEmptyValue_ForBiomeAndVirusKeys()
        {
            var keys = new[]
            {
                "cell.biome.openwater", "cell.biome.algaeforest",
                "cell.biome.deepzone",  "cell.biome.toxiczone",
                "cell.virus.infected",  "cell.virus.armoured_resist",
            };
            foreach (string key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Key '{key}' should have a non-empty value");
                Assert.AreNotEqual(key, val, $"Key '{key}' should be translated, not returned as-is");
            }
        }

        [Test]
        public void Get_ReturnsNonEmptyValue_ForLeviathanKeys()
        {
            var keys = new[]
            {
                "cell.leviathan.warning",
                "cell.leviathan.defeated",
                "cell.leviathan.tutorial",
            };
            foreach (string key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Key '{key}' should have a non-empty value");
                Assert.AreNotEqual(key, val, $"Key '{key}' should be translated, not returned as-is");
            }
        }

        [Test]
        public void Profile_Modal_Keys_Have_Translations()
        {
            var keys = new[]
            {
                "profile.title",
                "profile.stats_header",
                "profile.achievements_header",
                "profile.runs_completed",
                "profile.best_time",
                "profile.best_mass",
                "profile.best_dna",
                "profile.best_species",
                "profile.best_traits",
                "profile.leviathans_survived",
                "profile.leviathans_defeated",
            };
            foreach (string key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Profile key '{key}' missing");
                Assert.AreNotEqual(key, val, $"Profile key '{key}' not translated");
            }
        }
    }
}
