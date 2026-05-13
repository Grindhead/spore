using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class DataCompletenessTests
    {
        [Test]
        public void AchievementCatalog_AllEntriesHaveRequiredFieldsAndUniqueIds()
        {
            var ids = new HashSet<string>();

            foreach (var achievement in AchievementCatalog.GetAll())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(achievement.Id), "Achievement is missing Id.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(achievement.Title), $"Achievement {achievement.Id} is missing Title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(achievement.Description), $"Achievement {achievement.Id} is missing Description.");
                Assert.IsTrue(ids.Add(achievement.Id), $"Duplicate achievement Id: {achievement.Id}");
            }
        }

        [Test]
        public void CreatureUpgradeCatalog_AllEntriesHaveRequiredFieldsAndUniqueIds()
        {
            var ids = new HashSet<string>();

            foreach (var option in CreatureUpgradeCatalog.GetCoreOptions())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Id), $"Creature upgrade {option.Title} is missing Id.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Title), $"Creature upgrade {option.Id} is missing Title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Description), $"Creature upgrade {option.Id} is missing Description.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Tag), $"Creature upgrade {option.Id} is missing comparison tag.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(option.Tradeoff), $"Creature upgrade {option.Id} is missing tradeoff text.");
                Assert.IsTrue(ids.Add(option.Id), $"Duplicate creature upgrade Id: {option.Id}");

                var record = option.ToRecord();
                Assert.IsFalse(string.IsNullOrWhiteSpace(record.UpgradeId), $"Creature upgrade {option.Id} creates an incomplete record.");
            }
        }

        [Test]
        public void StarterCreatureDNA_IsCompleteAndCurrentBuildCompatible()
        {
            var dna = CreatureDNA.CreateDefault();

            Assert.IsTrue(GameManager.TryValidateCreatureDNA(dna, out string error));
            Assert.IsNull(error);
            Assert.IsFalse(string.IsNullOrWhiteSpace(dna.CreatureId));
            Assert.IsFalse(string.IsNullOrWhiteSpace(dna.CreatureName));
            Assert.IsNotNull(dna.Body);
            Assert.IsNotNull(dna.Parts);
            Assert.IsNotNull(dna.Upgrades);
            Assert.IsNotNull(dna.Colors);
            Assert.Greater(dna.Parts.Count, 0);

            for (int i = 0; i < dna.Parts.Count; i++)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(dna.Parts[i].PartId), $"Starter DNA part {i} is missing PartId.");
                Assert.Greater(dna.Parts[i].Scale, 0f, $"Starter DNA part {i} has invalid Scale.");
            }
        }

        [Test]
        public void SaveData_DefaultEntriesAreComplete()
        {
            var save = new SaveData();

            Assert.IsFalse(string.IsNullOrWhiteSpace(save.Version));
            Assert.IsFalse(string.IsNullOrWhiteSpace(save.Timestamp));
            Assert.IsFalse(string.IsNullOrWhiteSpace(save.CreatureDNAJson));
            Assert.IsFalse(string.IsNullOrWhiteSpace(save.AchievementJson));
            Assert.IsFalse(string.IsNullOrWhiteSpace(save.RunRecordJson));

            var dna = CreatureDNA.FromJson(save.CreatureDNAJson);
            Assert.IsTrue(GameManager.TryValidateCreatureDNA(dna, out string dnaError));
            Assert.IsNull(dnaError);

            Assert.IsTrue(RunRecord.TryFromJson(save.RunRecordJson, out var runRecord, out string runRecordError));
            Assert.IsNotNull(runRecord);
            Assert.IsNull(runRecordError);

            Assert.IsTrue(SaveManager.TryValidateLoadedData(save, out string saveError));
            Assert.IsNull(saveError);
        }
    }
}
