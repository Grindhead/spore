using System;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Flags indicating which personal-best stats were beaten in the most recent run.
    /// Returned by <see cref="RunRecord.Update"/> so the run-summary UI can highlight them.
    /// </summary>
    public struct NewBestFlags
    {
        public bool Time;
        public bool PeakMass;
        public bool DNA;
        public bool Species;
        public bool Traits;

        /// <summary>True if any stat was a personal best.</summary>
        public bool Any => Time || PeakMass || DNA || Species || Traits;
    }

    /// <summary>
    /// Persistent record of a player's best cell-stage run stats, accumulated across all runs.
    /// Serialized as JSON and stored in <see cref="SaveData.RunRecordJson"/>.
    ///
    /// Use <see cref="Update"/> after each run to refresh bests and get <see cref="NewBestFlags"/>
    /// for the run-summary UI. Call <see cref="FromJson"/>/<see cref="ToJson"/> to persist.
    /// </summary>
    [Serializable]
    public class RunRecord
    {
        // ---- Personal bests ----
        /// <summary>Longest time survived in a single run (seconds).</summary>
        public float BestTimeSurvived;
        /// <summary>Highest peak mass reached in a single run.</summary>
        public float BestPeakMass;
        /// <summary>Most DNA points earned in a single run.</summary>
        public int   BestDNAEarned;
        /// <summary>Most species present simultaneously in a single run.</summary>
        public int   BestSpeciesCount;
        /// <summary>Most genetic traits active simultaneously in a single run.</summary>
        public int   BestTraitCount;

        // ---- Aggregate stats ----
        /// <summary>Total number of completed runs (player died or exited).</summary>
        public int TotalRunsCompleted;
        /// <summary>Total leviathans survived across all runs.</summary>
        public int TotalLeviathansSurvived;
        /// <summary>Total leviathans defeated across all runs.</summary>
        public int TotalLeviathanDefeated;

        // ---- Factory ----

        /// <summary>Returns a new empty record (first ever run).</summary>
        public static RunRecord Empty() => new RunRecord();

        /// <summary>Deserializes a record from JSON; returns <see cref="Empty"/> on null/empty input.</summary>
        public static RunRecord FromJson(string json)
        {
            if (TryFromJson(json, out var record, out string error))
                return record;

            Debug.LogError($"[RunRecord] Invalid run record JSON: {error}");
            return Empty();
        }

        /// <summary>
        /// Deserializes without logging so tests and callers can assert malformed data explicitly.
        /// Null or empty input is treated as an absent record, not invalid JSON.
        /// </summary>
        public static bool TryFromJson(string json, out RunRecord record, out string error)
        {
            record = Empty();
            error = null;

            if (string.IsNullOrEmpty(json))
                return true;

            try
            {
                record = JsonUtility.FromJson<RunRecord>(json);
                if (record == null)
                {
                    record = Empty();
                    error = "deserialization returned no data.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                record = Empty();
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Serializes this record to JSON.</summary>
        public string ToJson() => JsonUtility.ToJson(this, false);

        // ---- Update ----

        /// <summary>
        /// Records stats from a finished run, updates personal bests, and returns flags
        /// indicating which (if any) records were broken.
        /// </summary>
        public NewBestFlags Update(float timeSurvived, float peakMass, int dnaEarned,
                                   int speciesCount, int traitCount)
        {
            TotalRunsCompleted++;

            var flags = new NewBestFlags();

            if (timeSurvived  > BestTimeSurvived)  { BestTimeSurvived  = timeSurvived;  flags.Time     = true; }
            if (peakMass      > BestPeakMass)       { BestPeakMass      = peakMass;      flags.PeakMass = true; }
            if (dnaEarned     > BestDNAEarned)      { BestDNAEarned     = dnaEarned;     flags.DNA      = true; }
            if (speciesCount  > BestSpeciesCount)   { BestSpeciesCount  = speciesCount;  flags.Species  = true; }
            if (traitCount    > BestTraitCount)     { BestTraitCount    = traitCount;    flags.Traits   = true; }

            return flags;
        }
    }
}
