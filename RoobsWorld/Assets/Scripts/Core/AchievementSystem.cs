using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Stage or category for grouping achievements in UI.
    /// </summary>
    public enum AchievementCategory
    {
        Cell,
        Creature
    }

    /// <summary>
    /// Static definition of all achievements, including ids, titles, descriptions, categories, and point values.
    /// </summary>
    [Serializable]
    public class AchievementDef
    {
        public string Id;
        public string Title;
        public string Description;
        public int Points;
        public bool Hidden;
        public AchievementCategory Category;
    }

    /// <summary>
    /// Event published when an achievement is unlocked so UI can react.
    /// </summary>
    public struct AchievementUnlockedEvent : IGameEvent
    {
        public string AchievementId;
        public string Title;
        public int Points;
    }

    /// <summary>
    /// Lookup utility for achievement definitions by id.
    /// </summary>
    public static class AchievementCatalog
    {
        private static readonly AchievementDef[] AllAchievements =
        {
            // Cell Stage
            new AchievementDef { Id = "cell_first_eat", Title = "First Bite", Description = "Eat your first food particle.", Points = 10, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_first_goal", Title = "On the Hunt", Description = "Complete your first micro goal.", Points = 15, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_grow_large", Title = "Big Fish", Description = "Reach maximum cell size.", Points = 25, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_survive_predator", Title = "Lucky Escape", Description = "Survive a predator cell encounter.", Points = 15, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_all_parts", Title = "Fully Equipped", Description = "Collect all cell part pickups.", Points = 30, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_evolve", Title = "Emergence", Description = "Complete the Cell Stage and evolve.", Points = 50, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_first_speciation", Title = "Genesis", Description = "Create a new species through evolution.", Points = 20, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_first_extinction", Title = "The End of a Line", Description = "Experience your first species extinction.", Points = 15, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_10_species", Title = "Evolutionary Arms Race", Description = "Have 10 species in the ecosystem simultaneously.", Points = 40, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_survived_leviathan", Title = "Small and Mighty", Description = "Escape a Leviathan by swimming far enough away.", Points = 30, Category = AchievementCategory.Cell },
            new AchievementDef { Id = "cell_defeated_leviathan", Title = "Leviathan Slayer", Description = "Destroy a Leviathan.", Points = 75, Hidden = true, Category = AchievementCategory.Cell },
            // Creature Stage
            new AchievementDef { Id = "creature_first_kill", Title = "First Hunt", Description = "Defeat your first rival creature.", Points = 20, Category = AchievementCategory.Creature },
            new AchievementDef { Id = "creature_pack_breaker", Title = "Pack Breaker", Description = "Complete the Creature-stage rival encounter objective.", Points = 45, Category = AchievementCategory.Creature },
            new AchievementDef { Id = "creature_apex_streak", Title = "Apex Streak", Description = "Reach a 5-kill streak in Creature stage.", Points = 35, Category = AchievementCategory.Creature },
        };

        private static Dictionary<string, AchievementDef> _lookup;

        public static AchievementDef[] GetAll() => AllAchievements;

        /// <summary>
        /// Returns achievements for a given category (hidden achievements excluded when includeHidden is false).
        /// </summary>
        public static AchievementDef[] GetByCategory(AchievementCategory category, bool includeHidden = false)
        {
            var list = new List<AchievementDef>();
            foreach (var a in AllAchievements)
            {
                if (a.Category != category) continue;
                if (a.Hidden && !includeHidden) continue;
                list.Add(a);
            }
            return list.ToArray();
        }

        public static AchievementDef Get(string id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, AchievementDef>();
                foreach (var a in AllAchievements)
                    _lookup[a.Id] = a;
            }
            return _lookup.TryGetValue(id, out var def) ? def : null;
        }
    }

    [Serializable]
    /// <summary>
    /// Serializable container for unlocked achievement ids in save files.
    /// </summary>
    public class AchievementSaveData
    {
        public List<string> UnlockedIds = new List<string>();
    }

    /// <summary>
    /// Tracks unlocked achievements in memory and publishes <see cref="AchievementUnlockedEvent"/> when new ones are earned.
    /// </summary>
    public class AchievementTracker
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>();

        public int UnlockedCount => _unlocked.Count;

        public int TotalPoints
        {
            get
            {
                int sum = 0;
                foreach (string id in _unlocked)
                {
                    var def = AchievementCatalog.Get(id);
                    if (def != null) sum += def.Points;
                }
                return sum;
            }
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        public bool TryUnlock(string id)
        {
            if (_unlocked.Contains(id)) return false;
            _unlocked.Add(id);

            var def = AchievementCatalog.Get(id);
            if (def != null)
            {
                EventBus.Publish(new AchievementUnlockedEvent
                {
                    AchievementId = id,
                    Title = def.Title,
                    Points = def.Points
                });
            }

            return true;
        }

        public void Reset()
        {
            _unlocked.Clear();
        }

        public string ToJson()
        {
            var data = new AchievementSaveData();
            data.UnlockedIds.AddRange(_unlocked);
            return JsonUtility.ToJson(data);
        }

        public static AchievementTracker FromJson(string json)
        {
            var tracker = new AchievementTracker();
            if (string.IsNullOrEmpty(json)) return tracker;

            var data = JsonUtility.FromJson<AchievementSaveData>(json);
            if (data?.UnlockedIds != null)
            {
                foreach (string id in data.UnlockedIds)
                    tracker._unlocked.Add(id);
            }
            return tracker;
        }
    }

    /// <summary>
    /// Abstraction for reporting achievement unlocks to platform systems (Steam, Xbox, PlayStation, Nintendo).
    /// When unavailable (editor, unsupported build), use null or <see cref="LocalAchievementProvider"/> for no-op.
    /// </summary>
    public interface IPlatformAchievementProvider
    {
        /// <summary>
        /// Reports that an achievement was unlocked so the platform can update its overlay/store.
        /// Called only when the achievement was newly unlocked (not already in tracker).
        /// </summary>
        /// <param name="achievementId">In-game achievement id (e.g. cell_first_eat). Map to platform-specific id in implementation.</param>
        void ReportUnlock(string achievementId);
    }

    /// <summary>
    /// No-op provider for editor and when platform API is unavailable. Safe local fallback; no runtime error.
    /// </summary>
    public sealed class LocalAchievementProvider : IPlatformAchievementProvider
    {
        public void ReportUnlock(string achievementId) { }
    }
}
