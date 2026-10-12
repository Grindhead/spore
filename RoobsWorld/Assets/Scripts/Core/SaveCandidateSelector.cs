using System;
using System.Globalization;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Picks which save snapshot to load when a local file (plus its backup) and a cloud copy
    /// may both exist. Pure logic so it can be unit tested without IO.
    ///
    /// Policy:
    ///   1. Invalid or unreadable candidates are discarded; the next candidate is tried.
    ///      Local order: primary file, then the backup kept by the atomic writer.
    ///   2. A local tombstone (slot deleted on this device) hides any candidate saved at or
    ///      before the deletion time, so a stale cloud copy cannot resurrect it.
    ///   3. A cloud tombstone hides any candidate saved at or before the cloud deletion
    ///      (slot deleted on another device).
    ///   4. Of the remaining valid candidates the newest <see cref="SaveData.Timestamp"/> wins;
    ///      a tie or an unparsable timestamp prefers local, which is always written first.
    /// </summary>
    public static class SaveCandidateSelector
    {
        public enum Source
        {
            None,
            LocalPrimary,
            LocalBackup,
            Cloud
        }

        public struct Selection
        {
            public SaveData Data;
            public Source Source;
            /// <summary>True when the local copy is stale/missing and should be rewritten from <see cref="Data"/>.</summary>
            public bool ShouldRewriteLocal;
            /// <summary>True when a deletion should be (re)sent to the cloud.</summary>
            public bool ShouldResendCloudDelete;
            /// <summary>True when a cloud deletion newer than every local snapshot means local files should be removed.</summary>
            public bool ShouldDeleteLocal;
        }

        public static Selection Select(
            SaveData localPrimary,
            SaveData localBackup,
            SaveData cloud,
            DateTime? localDeletedAtUtc,
            DateTime? cloudDeletedAtUtc,
            Func<SaveData, bool> isValid)
        {
            if (isValid == null)
                throw new ArgumentNullException(nameof(isValid));

            SaveData local = null;
            Source localSource = Source.None;
            if (localPrimary != null && isValid(localPrimary))
            {
                local = localPrimary;
                localSource = Source.LocalPrimary;
            }
            else if (localBackup != null && isValid(localBackup))
            {
                local = localBackup;
                localSource = Source.LocalBackup;
            }

            if (cloud != null && !isValid(cloud))
                cloud = null;

            var selection = new Selection();

            if (localDeletedAtUtc.HasValue)
            {
                if (IsAtOrBefore(local, localDeletedAtUtc.Value)) { local = null; localSource = Source.None; }
                if (IsAtOrBefore(cloud, localDeletedAtUtc.Value)) { cloud = null; selection.ShouldResendCloudDelete = true; }
            }

            if (cloudDeletedAtUtc.HasValue)
            {
                if (IsAtOrBefore(cloud, cloudDeletedAtUtc.Value)) cloud = null;
                if (IsAtOrBefore(local, cloudDeletedAtUtc.Value))
                {
                    local = null;
                    localSource = Source.None;
                    selection.ShouldDeleteLocal = true;
                }
            }

            if (local == null && cloud == null)
            {
                selection.Source = Source.None;
                return selection;
            }

            if (cloud != null && (local == null || IsNewer(cloud, local)))
            {
                selection.Data = cloud;
                selection.Source = Source.Cloud;
                selection.ShouldRewriteLocal = true;
                return selection;
            }

            selection.Data = local;
            selection.Source = localSource;
            selection.ShouldRewriteLocal = localSource == Source.LocalBackup;
            return selection;
        }

        public static DateTime? ParseTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return DateTime.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : (DateTime?)null;
        }

        private static bool IsNewer(SaveData candidate, SaveData baseline)
        {
            var a = ParseTimestamp(candidate?.Timestamp);
            var b = ParseTimestamp(baseline?.Timestamp);
            if (!a.HasValue) return false;
            if (!b.HasValue) return true;
            return a.Value > b.Value;
        }

        private static bool IsAtOrBefore(SaveData data, DateTime cutoffUtc)
        {
            if (data == null) return false;
            var stamp = ParseTimestamp(data.Timestamp);
            // An undated snapshot cannot prove it post-dates the deletion.
            return !stamp.HasValue || stamp.Value <= cutoffUtc;
        }
    }
}
