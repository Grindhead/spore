using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for RunRecord personal-best tracking.
    /// </summary>
    public class RunRecordTests
    {
        // ---- RunRecord.Empty ----

        [Test]
        public void Empty_AllBestsAreZero()
        {
            var r = RunRecord.Empty();
            Assert.AreEqual(0f, r.BestTimeSurvived);
            Assert.AreEqual(0f, r.BestPeakMass);
            Assert.AreEqual(0,  r.BestDNAEarned);
            Assert.AreEqual(0,  r.BestSpeciesCount);
            Assert.AreEqual(0,  r.BestTraitCount);
            Assert.AreEqual(0,  r.TotalRunsCompleted);
        }

        // ---- RunRecord.Update — first run ----

        [Test]
        public void Update_FirstRun_AllFlagsTrue()
        {
            var r     = RunRecord.Empty();
            var flags = r.Update(60f, 5f, 20, 3, 2);

            Assert.IsTrue(flags.Time,     "First run should set Time best");
            Assert.IsTrue(flags.PeakMass, "First run should set PeakMass best");
            Assert.IsTrue(flags.DNA,      "First run should set DNA best");
            Assert.IsTrue(flags.Species,  "First run should set Species best");
            Assert.IsTrue(flags.Traits,   "First run should set Traits best");
            Assert.IsTrue(flags.Any);
        }

        [Test]
        public void Update_FirstRun_BestsStored()
        {
            var r = RunRecord.Empty();
            r.Update(90f, 7.5f, 35, 5, 3);

            Assert.AreEqual(90f,  r.BestTimeSurvived, 0.001f);
            Assert.AreEqual(7.5f, r.BestPeakMass,     0.001f);
            Assert.AreEqual(35,   r.BestDNAEarned);
            Assert.AreEqual(5,    r.BestSpeciesCount);
            Assert.AreEqual(3,    r.BestTraitCount);
            Assert.AreEqual(1,    r.TotalRunsCompleted);
        }

        // ---- RunRecord.Update — subsequent run that beats nothing ----

        [Test]
        public void Update_WorseThanBest_NoFlagsSet()
        {
            var r = RunRecord.Empty();
            r.Update(90f, 7f, 30, 5, 3);   // first run sets bests
            var flags = r.Update(30f, 2f, 10, 2, 1); // worse run

            Assert.IsFalse(flags.Time);
            Assert.IsFalse(flags.PeakMass);
            Assert.IsFalse(flags.DNA);
            Assert.IsFalse(flags.Species);
            Assert.IsFalse(flags.Traits);
            Assert.IsFalse(flags.Any);
        }

        [Test]
        public void Update_WorseThanBest_BestsUnchanged()
        {
            var r = RunRecord.Empty();
            r.Update(90f, 7f, 30, 5, 3);
            r.Update(30f, 2f, 10, 2, 1);

            Assert.AreEqual(90f, r.BestTimeSurvived, 0.001f);
            Assert.AreEqual(7f,  r.BestPeakMass,     0.001f);
            Assert.AreEqual(30,  r.BestDNAEarned);
            Assert.AreEqual(5,   r.BestSpeciesCount);
            Assert.AreEqual(3,   r.BestTraitCount);
        }

        // ---- RunRecord.Update — partial improvement ----

        [Test]
        public void Update_OnlyTimeImproves_OnlyTimeFlagSet()
        {
            var r = RunRecord.Empty();
            r.Update(60f, 7f, 30, 5, 3);
            var flags = r.Update(120f, 5f, 20, 4, 2);

            Assert.IsTrue(flags.Time,      "Time improved");
            Assert.IsFalse(flags.PeakMass, "PeakMass did not improve");
            Assert.IsFalse(flags.DNA,      "DNA did not improve");
            Assert.IsFalse(flags.Species,  "Species did not improve");
            Assert.IsFalse(flags.Traits,   "Traits did not improve");
        }

        // ---- TotalRunsCompleted ----

        [Test]
        public void TotalRunsCompleted_IncrementsEachUpdate()
        {
            var r = RunRecord.Empty();
            r.Update(10f, 1f, 5, 1, 0);
            r.Update(20f, 2f, 6, 1, 0);
            r.Update(30f, 3f, 7, 2, 1);
            Assert.AreEqual(3, r.TotalRunsCompleted);
        }

        // ---- JSON round-trip ----

        [Test]
        public void ToJson_FromJson_RoundTrip()
        {
            var r = RunRecord.Empty();
            r.Update(75f, 9f, 42, 7, 4);
            r.TotalLeviathansSurvived = 2;
            r.TotalLeviathanDefeated  = 1;

            string json = r.ToJson();
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var restored = RunRecord.FromJson(json);
            Assert.AreEqual(r.BestTimeSurvived,         restored.BestTimeSurvived,         0.001f);
            Assert.AreEqual(r.BestPeakMass,             restored.BestPeakMass,             0.001f);
            Assert.AreEqual(r.BestDNAEarned,            restored.BestDNAEarned);
            Assert.AreEqual(r.BestSpeciesCount,         restored.BestSpeciesCount);
            Assert.AreEqual(r.BestTraitCount,           restored.BestTraitCount);
            Assert.AreEqual(r.TotalRunsCompleted,       restored.TotalRunsCompleted);
            Assert.AreEqual(r.TotalLeviathansSurvived,  restored.TotalLeviathansSurvived);
            Assert.AreEqual(r.TotalLeviathanDefeated,   restored.TotalLeviathanDefeated);
        }

        [Test]
        public void FromJson_NullInput_ReturnsEmpty()
        {
            var r = RunRecord.FromJson(null);
            Assert.IsNotNull(r);
            Assert.AreEqual(0, r.TotalRunsCompleted);
        }

        [Test]
        public void FromJson_EmptyString_ReturnsEmpty()
        {
            var r = RunRecord.FromJson("");
            Assert.IsNotNull(r);
            Assert.AreEqual(0f, r.BestTimeSurvived);
        }

        [Test]
        public void TryFromJson_InvalidJson_ReturnsErrorAndEmptyRecord()
        {
            bool parsed = RunRecord.TryFromJson("{not json at all!!!}", out var r, out string error);

            Assert.IsFalse(parsed);
            Assert.IsNotNull(r);
            Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        }

        // ---- SaveData has RunRecordJson field ----

        [Test]
        public void SaveData_RunRecordJson_DefaultsToCompleteRecord()
        {
            var save = new SaveData();

            Assert.IsTrue(RunRecord.TryFromJson(save.RunRecordJson, out var record, out string error));
            Assert.IsNotNull(record);
            Assert.IsNull(error);
            Assert.AreEqual(0, record.TotalRunsCompleted);
        }

        // ---- Localization keys ----

        [Test]
        public void LocalizationKey_NewBest_Exists()
        {
            string val = Localization.Get("cell.summary.new_best");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.summary.new_best", val);
        }

        [Test]
        public void LocalizationKey_RunCount_Exists()
        {
            string val = Localization.Get("cell.summary.run_count");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.summary.run_count", val);
        }
    }
}
