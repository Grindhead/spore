using System.IO;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoobsWorld.Tests
{
    public class SaveDataTests
    {
        [Test]
        public void SaveData_Serializes_And_Deserializes()
        {
            var data = new SaveData
            {
                CurrentStage = GameStageType.Cell,
                WorldSeed = 12345,
                DNAPoints = 42,
                CreatureDNAJson = CreatureDNA.CreateDefault().ToJson()
            };

            string json = JsonUtility.ToJson(data);
            var restored = JsonUtility.FromJson<SaveData>(json);

            Assert.AreEqual(data.CurrentStage, restored.CurrentStage);
            Assert.AreEqual(data.WorldSeed, restored.WorldSeed);
            Assert.AreEqual(data.DNAPoints, restored.DNAPoints);
            Assert.AreEqual(data.CreatureDNAJson, restored.CreatureDNAJson);
        }

        [Test]
        public void SaveData_Default_Stage_Is_Cell()
        {
            var data = new SaveData();
            Assert.AreEqual(GameStageType.Cell, data.CurrentStage);
        }

        [Test]
        public void SaveData_Default_Version_Is_Set()
        {
            var data = new SaveData();
            Assert.IsNotNull(data.Version);
            Assert.IsNotEmpty(data.Version);
        }

        [Test]
        public void SaveData_Default_Entries_Are_Complete()
        {
            var data = new SaveData();

            Assert.IsFalse(string.IsNullOrWhiteSpace(data.CreatureDNAJson));
            Assert.IsFalse(string.IsNullOrWhiteSpace(data.AchievementJson));
            Assert.IsFalse(string.IsNullOrWhiteSpace(data.RunRecordJson));

            var dna = CreatureDNA.FromJson(data.CreatureDNAJson);
            Assert.IsTrue(GameManager.TryValidateCreatureDNA(dna, out string dnaError));
            Assert.IsNull(dnaError);

            Assert.IsTrue(RunRecord.TryFromJson(data.RunRecordJson, out var runRecord, out string runRecordError));
            Assert.IsNotNull(runRecord);
            Assert.IsNull(runRecordError);
        }
    }

    public class SaveManagerTests
    {
        [Test]
        public void Load_ReturnsNull_WhenFileDoesNotExist()
        {
            var result = SaveManager.Instance.Load("nonexistent_slot_xyz_12345");
            Assert.IsNull(result);
        }

        [Test]
        public void TryValidateLoadedData_ReturnsFalse_WhenVersionIsMissing()
        {
            var data = new SaveData { Version = "" };

            bool valid = SaveManager.TryValidateLoadedData(data, out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("missing version.", error);
        }

        [Test]
        public void GetAllSaveSlots_ReturnsArray_EmptyOrWithSlots()
        {
            string[] slots = SaveManager.Instance.GetAllSaveSlots();
            Assert.IsNotNull(slots);
        }

        [Test]
        public void Save_ThenLoad_ReturnsSameData()
        {
            var data = SaveManager.Instance.CreateNewSave(999);
            data.DNAPoints = 50;
            const string slot = "test_roundtrip_slot";
            try
            {
                SaveManager.Instance.Save(data, slot);
                var loaded = SaveManager.Instance.Load(slot);
                Assert.IsNotNull(loaded);
                Assert.AreEqual(data.WorldSeed, loaded.WorldSeed);
                Assert.AreEqual(data.DNAPoints, loaded.DNAPoints);
                Assert.AreEqual(data.CreatureDNAJson, loaded.CreatureDNAJson);
            }
            finally
            {
                SaveManager.Instance.DeleteSave(slot);
            }
        }
    }
}
