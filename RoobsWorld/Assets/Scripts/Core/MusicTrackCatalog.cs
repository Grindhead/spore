using System.Collections.Generic;
using System.Linq;

namespace RoobsWorld.Core
{
    /// <summary>
    /// High-level music mood tags used to select appropriate background tracks.
    /// </summary>
    public enum MusicMood
    {
        Exploration = 0,
        Combat = 1,
        Social = 2,
        Tense = 3,
        Victory = 4
    }

    /// <summary>
    /// Identifiers for sound effect categories mapped to audio asset ids.
    /// </summary>
    public enum SFXType
    {
        UIClick,
        UIHover,
        UIBack,
        AttackHit,
        AttackMiss,
        CreatureDeath,
        CreatureSpawn,
        FoodEaten,
        FoodEatenPlant,
        FoodEatenMeat,
        FoodEatenToxin,
        EvolutionComplete,
        MutationChoiceShown,
        DNAGained,
        LevelUp,
        StageTransition,
        BuildingPlaced,
        DiplomacySuccess,
        DiplomacyFail,
        ShieldHit,
        ExplosionSmall,
        ExplosionLarge,
        TerraformPulse,
        ColonyEstablished,
        VictoryFanfare
    }

    /// <summary>
    /// Maps stages and moods to music track ids, plus ambient and SFX ids, for consumption by <see cref="AdaptiveMusicController"/>.
    /// </summary>
    /// <remarks>
    /// Expected music track IDs (place AudioClips in Resources/Music/ with these names for auto-load by <see cref="AudioClipLoader"/>):
    /// cell_exploration, cell_combat, cell_tense, cell_victory.
    /// Ambient: ambient_underwater.
    /// SFX IDs: ui_click, ui_hover, attack_hit, creature_death, food_eaten, stage_transition, etc. — see GetSFXId(SFXType).
    /// </remarks>
    public class MusicTrackCatalog
    {
        private readonly Dictionary<(GameStageType stage, MusicMood mood), string> _tracks;
        private readonly Dictionary<GameStageType, string> _ambients;
        private readonly Dictionary<SFXType, string> _sfxIds;

        public MusicTrackCatalog()
        {
            _tracks = new Dictionary<(GameStageType, MusicMood), string>
            {
                { (GameStageType.Cell, MusicMood.Exploration), "cell_exploration" },
                { (GameStageType.Cell, MusicMood.Combat), "cell_combat" },
                { (GameStageType.Cell, MusicMood.Tense), "cell_tense" },
                { (GameStageType.Cell, MusicMood.Victory), "cell_victory" },

            };

            _ambients = new Dictionary<GameStageType, string>
            {
                { GameStageType.Cell, "ambient_underwater" },
            };

            _sfxIds = new Dictionary<SFXType, string>
            {
                { SFXType.UIClick, "ui_click" },
                { SFXType.UIHover, "ui_hover" },
                { SFXType.UIBack, "ui_back" },
                { SFXType.AttackHit, "attack_hit" },
                { SFXType.AttackMiss, "attack_miss" },
                { SFXType.CreatureDeath, "creature_death" },
                { SFXType.CreatureSpawn, "creature_spawn" },
                { SFXType.FoodEaten, "food_eaten" },
                { SFXType.FoodEatenPlant, "food_eaten_plant" },
                { SFXType.FoodEatenMeat, "food_eaten_meat" },
                { SFXType.FoodEatenToxin, "food_eaten_toxin" },
                { SFXType.EvolutionComplete, "evolution_complete" },
                { SFXType.MutationChoiceShown, "mutation_choice" },
                { SFXType.DNAGained, "dna_gained" },
                { SFXType.LevelUp, "level_up" },
                { SFXType.StageTransition, "stage_transition" },
                { SFXType.BuildingPlaced, "building_placed" },
                { SFXType.DiplomacySuccess, "diplomacy_success" },
                { SFXType.DiplomacyFail, "diplomacy_fail" },
                { SFXType.ShieldHit, "shield_hit" },
                { SFXType.ExplosionSmall, "explosion_small" },
                { SFXType.ExplosionLarge, "explosion_large" },
                { SFXType.TerraformPulse, "terraform_pulse" },
                { SFXType.ColonyEstablished, "colony_established" },
                { SFXType.VictoryFanfare, "victory_fanfare" },
            };
        }

        public string GetStageTrack(GameStageType stage, MusicMood mood)
        {
            if (IsMenuOrNoneStage(stage))
                return null;

            if (!StageManager.IsStageSupported(stage))
                return null;

            stage = StageManager.NormalizeRequestedStage(stage);

            return _tracks.TryGetValue((stage, mood), out var track)
                ? track
                : _tracks.TryGetValue((GameStageType.Cell, MusicMood.Exploration), out var fallback) ? fallback : null;
        }

        public string GetAmbientId(GameStageType stage)
        {
            if (IsMenuOrNoneStage(stage))
                return null;

            if (!StageManager.IsStageSupported(stage))
                return null;

            stage = StageManager.NormalizeRequestedStage(stage);
            return _ambients.TryGetValue(stage, out var id)
                ? id
                : _ambients.TryGetValue(GameStageType.Cell, out var fallback) ? fallback : null;
        }

        public string GetSFXId(SFXType sfx)
        {
            return _sfxIds.TryGetValue(sfx, out var id) ? id : null;
        }

        /// <summary>
        /// Returns all music track IDs used for (stage, mood) combinations. Use to load clips from Resources for <see cref="AdaptiveMusicController"/>.
        /// </summary>
        /// <returns>Unique set of track IDs (e.g. cell_exploration, cell_combat).</returns>
        public IReadOnlyCollection<string> GetAllMusicTrackIds()
        {
            return _tracks.Values.Distinct().ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns all ambient track IDs per stage. Use to load ambient clips from Resources.
        /// </summary>
        /// <returns>Unique set of ambient IDs (e.g. ambient_underwater).</returns>
        public IReadOnlyCollection<string> GetAllAmbientIds()
        {
            return _ambients.Values.Distinct().ToList().AsReadOnly();
        }

        /// <summary>
        /// Returns true for non-gameplay stages that should not play stage music/ambient.
        /// </summary>
        private static bool IsMenuOrNoneStage(GameStageType stage)
        {
            return stage == GameStageType.None;
        }
    }
}
