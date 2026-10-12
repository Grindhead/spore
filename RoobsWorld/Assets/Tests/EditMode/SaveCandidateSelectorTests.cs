using System;
using System.IO;
using NUnit.Framework;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class SaveCandidateSelectorTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        private static SaveData At(DateTime utc, int dna, bool valid = true)
        {
            return new SaveData
            {
                Timestamp = utc.ToString("o"),
                DNAPoints = dna,
                Version = valid ? SaveData.CurrentVersion : ""
            };
        }

        private static bool Valid(SaveData d) => SaveManager.TryValidateLoadedData(d, out _);

        [Test]
        public void NewerLocal_Wins_OverStaleCloud()
        {
            var local = At(T0.AddMinutes(5), 2);
            var cloud = At(T0, 1);
            var result = SaveCandidateSelector.Select(local, null, cloud, null, null, Valid);
            Assert.AreSame(local, result.Data);
            Assert.AreEqual(SaveCandidateSelector.Source.LocalPrimary, result.Source);
            Assert.IsFalse(result.ShouldRewriteLocal);
        }

        [Test]
        public void NewerCloud_Wins_AndRewritesLocal()
        {
            var local = At(T0, 1);
            var cloud = At(T0.AddMinutes(5), 2);
            var result = SaveCandidateSelector.Select(local, null, cloud, null, null, Valid);
            Assert.AreSame(cloud, result.Data);
            Assert.IsTrue(result.ShouldRewriteLocal);
        }

        [Test]
        public void InvalidCloud_FallsBackToValidLocal()
        {
            var local = At(T0, 1);
            var cloud = At(T0.AddMinutes(5), 2, valid: false);
            var result = SaveCandidateSelector.Select(local, null, cloud, null, null, Valid);
            Assert.AreSame(local, result.Data);
        }

        [Test]
        public void CorruptPrimary_FallsBackToBackup()
        {
            var primary = At(T0.AddMinutes(5), 2, valid: false);
            var backup = At(T0, 1);
            var result = SaveCandidateSelector.Select(primary, backup, null, null, null, Valid);
            Assert.AreSame(backup, result.Data);
            Assert.AreEqual(SaveCandidateSelector.Source.LocalBackup, result.Source);
            Assert.IsTrue(result.ShouldRewriteLocal);
        }

        [Test]
        public void TiedTimestamps_PreferLocal()
        {
            var local = At(T0, 1);
            var cloud = At(T0, 2);
            Assert.AreSame(local, SaveCandidateSelector.Select(local, null, cloud, null, null, Valid).Data);
        }

        [Test]
        public void LocalTombstone_HidesOlderCloudCopy_AndResendsDelete()
        {
            var cloud = At(T0, 1);
            var result = SaveCandidateSelector.Select(null, null, cloud, T0.AddMinutes(1), null, Valid);
            Assert.IsNull(result.Data);
            Assert.IsTrue(result.ShouldResendCloudDelete);
        }

        [Test]
        public void CloudTombstone_HidesOlderLocalCopy()
        {
            var local = At(T0, 1);
            var result = SaveCandidateSelector.Select(local, null, null, null, T0.AddMinutes(1), Valid);
            Assert.IsNull(result.Data);
            Assert.IsTrue(result.ShouldDeleteLocal);
        }

        [Test]
        public void SaveAfterDeletion_IsKept()
        {
            var local = At(T0.AddMinutes(2), 1);
            var result = SaveCandidateSelector.Select(local, null, null, null, T0.AddMinutes(1), Valid);
            Assert.AreSame(local, result.Data);
        }

        [Test]
        public void NoCandidates_ReturnsNone()
        {
            var result = SaveCandidateSelector.Select(null, null, null, null, null, Valid);
            Assert.IsNull(result.Data);
            Assert.AreEqual(SaveCandidateSelector.Source.None, result.Source);
        }
    }

    public class SaveManagerLocalDurabilityTests
    {
        private const string Slot = "test_durability_slot";

        private static string PrimaryPath =>
            Path.Combine(Application.persistentDataPath, "saves", Slot + ".json");

        [TearDown]
        public void TearDown()
        {
            SaveManager.Instance.DeleteSave(Slot);
            string tombstone = PrimaryPath + ".deleted";
            if (File.Exists(tombstone))
                File.Delete(tombstone);
        }

        [Test]
        public void CorruptedPrimary_RecoversPreviousSnapshotFromBackup()
        {
            var first = SaveManager.Instance.CreateNewSave(1);
            first.DNAPoints = 10;
            SaveManager.Instance.Save(first, Slot);

            var second = SaveManager.Instance.CreateNewSave(1);
            second.DNAPoints = 20;
            SaveManager.Instance.Save(second, Slot);

            Assert.IsTrue(File.Exists(PrimaryPath + ".bak"));

            // Simulate an interrupted/corrupted write of the primary file.
            File.WriteAllText(PrimaryPath, "{ truncated");

            var loaded = SaveManager.Instance.Load(Slot);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(10, loaded.DNAPoints);
        }

        [Test]
        public void DeletedSlot_DoesNotReappear_AndSavingAgainRevivesIt()
        {
            var data = SaveManager.Instance.CreateNewSave(2);
            SaveManager.Instance.Save(data, Slot);
            SaveManager.Instance.DeleteSave(Slot);

            Assert.IsFalse(SaveManager.Instance.SaveExists(Slot));
            Assert.IsNull(SaveManager.Instance.Load(Slot));
            Assert.IsTrue(File.Exists(PrimaryPath + ".deleted"));

            SaveManager.Instance.Save(SaveManager.Instance.CreateNewSave(3), Slot);
            Assert.IsFalse(File.Exists(PrimaryPath + ".deleted"));
            Assert.IsNotNull(SaveManager.Instance.Load(Slot));
        }
    }
}
