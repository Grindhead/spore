using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// High-level game stages that the player can occupy.
    /// </summary>
    public enum GameStageType
    {
        None = 0,
        Cell = 1,
        Creature = 2
    }
    /// <summary>Fired when the game transitions from one stage to another.</summary>
    public struct StageTransitionEvent : IGameEvent
    {
        public GameStageType FromStage;
        public GameStageType ToStage;
    }

    /// <summary>Fired when a creature completes an evolution step (e.g. in nest or cell evolution).</summary>
    public struct CreatureEvolvedEvent : IGameEvent
    {
        public string CreatureId;
        public int EvolutionLevel;
    }

    /// <summary>Fired when the player's character gains a non-blocking upgrade.</summary>
    public struct PlayerCharacterImprovedEvent : IGameEvent
    {
        public string Title;
        public string Description;
        public string StatSummary;
    }

    /// <summary>Fired when the player's DNA point balance changes (earned or spent).</summary>
    public struct DNAPointsChangedEvent : IGameEvent
    {
        public int OldAmount;
        public int NewAmount;
    }

    /// <summary>Fired when a creature is spawned into the world.</summary>
    public struct CreatureSpawnedEvent : IGameEvent
    {
        public GameObject CreatureObject;
        public string CreatureId;
        public bool IsPlayer;
    }

    /// <summary>Fired when a creature dies (player or NPC). Used for achievements and cleanup.</summary>
    public struct CreatureDiedEvent : IGameEvent
    {
        public GameObject CreatureObject;
        public string CreatureId;
        public bool IsPlayer;
        public string Cause;
        public string RecommendationKey;
    }

    /// <summary>High-level combat feedback categories used by Creature stage HUD/VFX/audio hooks.</summary>
    public enum CreatureCombatFeedbackType
    {
        Hit,
        Miss,
        Kill,
        PlayerDamaged,
        Guard,
        Blocked,
        Counter,
        Telegraph,
        LowHealth
    }

    /// <summary>Lightweight per-species stance used in Creature stage to mirror Spore-style relationship drift.</summary>
    public enum CreatureSpeciesDisposition
    {
        Hostile,
        Annoyed,
        Curious,
        Friendly,
        Allied
    }

    /// <summary>Fired for readable Creature-stage combat beats such as misses, hits, blocks, and telegraphs.</summary>
    public struct CreatureCombatFeedbackEvent : IGameEvent
    {
        public CreatureCombatFeedbackType Type;
        public string SourceCreatureId;
        public string TargetCreatureId;
        public Vector3 Position;
        public float Amount;
        public bool IsPlayerSource;
        public bool IsPlayerTarget;
    }

    /// <summary>Fired when a tracked Creature-stage rival is defeated, including its tactical archetype label.</summary>
    public struct CreatureRivalDefeatedEvent : IGameEvent
    {
        public string CreatureId;
        public string ArchetypeName;
        public bool IsAlpha;
    }

    /// <summary>Fired when a rival species relationship stance changes because of player actions.</summary>
    public struct CreatureSpeciesRelationshipChangedEvent : IGameEvent
    {
        public string SpeciesLabel;
        public CreatureSpeciesDisposition PreviousDisposition;
        public CreatureSpeciesDisposition NewDisposition;
        public float Score;
        public float Delta;
    }

    /// <summary>Fired when a Creature-stage kill streak grants a bonus DNA payout.</summary>
    public struct CreatureStreakBonusEvent : IGameEvent
    {
        public int StreakCount;
        public int DnaBonus;
    }

    /// <summary>Fired when the Creature-stage objective loop pays out a completed objective reward.</summary>
    public struct CreatureObjectiveCompletedEvent : IGameEvent
    {
        public RoobsWorld.Stages.Creature.CreatureStageObjectiveType ObjectiveType;
        public string ObjectiveLocalizationKey;
        public int DnaReward;
        public int PaceBonusDna;
        public int CompletedObjectiveCount;
        public float CompletionSeconds;
        public int KillStreak;
    }

    /// <summary>Fired when the player uses a Creature-stage active ability inherited from Cell-stage traits.</summary>
    public struct CreatureAbilityUsedEvent : IGameEvent
    {
        public string AbilityId;
        public string AbilityLabel;
        public float StaminaSpent;
        public Vector3 Position;
    }

    /// <summary>Fired when the player attempts a Creature-stage action without enough stamina.</summary>
    public struct CreatureStaminaExhaustedEvent : IGameEvent
    {
        public string ActionId;
        public float CurrentStamina;
        public float RequiredStamina;
    }

    /// <summary>Fired when Creature-stage combat momentum restores player stamina.</summary>
    public struct CreatureStaminaRestoredEvent : IGameEvent
    {
        public string ReasonId;
        public float Amount;
        public float CurrentStamina;
        public float MaxStamina;
    }

    /// <summary>Fired when a creature starts or stops receiving healing-zone support.</summary>
    public struct CreatureHealingZoneEvent : IGameEvent
    {
        public bool Started;
        public Vector3 Position;
        public bool IsPlayer;
    }

    /// <summary>Fired when a game save completes successfully.</summary>
    public struct GameSavedEvent : IGameEvent
    {
        public string SavePath;
    }

    /// <summary>Fired when a game is loaded from disk.</summary>
    public struct GameLoadedEvent : IGameEvent
    {
        public string SavePath;
    }

    /// <summary>Fired when the game is paused or unpaused.</summary>
    public struct PauseEvent : IGameEvent
    {
        public bool IsPaused;
        public bool SuppressPauseMenu;
    }

    /// <summary>Fired when the player cell reaches an evolution mass threshold (before part-picker UI).</summary>
    public struct CellEvolutionTriggeredEvent : IGameEvent
    {
        public int EvolutionLevel;
        public float CurrentMass;
    }

    /// <summary>Fired when the player completes a cell evolution step (part selection done).</summary>
    public struct CellEvolutionCompleteEvent : IGameEvent
    {
        public int EvolutionLevel;
    }

    /// <summary>
    /// Fired when a cell-stage organism dies. Used by SpeciationSystem to decrement species population
    /// and by CellStage to spawn carcass food at the death location.
    /// </summary>
    public struct CellOrganismDiedEvent : IGameEvent
    {
        public int SpeciesId;
        public Vector3 Position;
        public float VisualScale;
    }

    /// <summary>Cell-stage environmental event types (Step 6). Nutrient bloom, virus, temperature affect zones/modifiers.</summary>
    public enum CellEnvironmentEventType
    {
        None,
        NutrientBloom,
        VirusOutbreak,
        TemperatureChange,
        /// <summary>Rare event: radiation bath accelerates mutation — player is offered one bonus mutation choice.</summary>
        RadiationSurge,
        OxygenSurge
    }

    /// <summary>Fired when a cell-stage environment event starts or ends (e.g. nutrient bloom in AlgaeForest).</summary>
    public struct CellEnvironmentEventTriggered : IGameEvent
    {
        public CellEnvironmentEventType EventType;
        public bool Started;
        public float DurationSeconds;
    }

    /// <summary>Fired when the player consumes food in the cell stage. Used for micro goals.</summary>
    public struct CellFoodConsumedEvent : IGameEvent
    {
        public RoobsWorld.Stages.Cell.FoodType FoodType;
    }

    /// <summary>Fired when the player earns the cell-stage varied-diet reward for switching food types.</summary>
    public struct CellDietVarietyBonusEvent : IGameEvent
    {
        public RoobsWorld.Stages.Cell.FoodType FoodType;
        public int ChainLength;
        public int DnaBonus;
        public float MassBonus;
    }

    /// <summary>Fired when a cell-stage micro goal is completed (e.g. eat N algae).</summary>
    public struct CellGoalCompletedEvent : IGameEvent
    {
        public RoobsWorld.Stages.Cell.CellGoal.GoalType GoalType;
        public RoobsWorld.Stages.Cell.CellGoal.GoalActionCategory ActionCategory;
        public int DnaReward;
        public int RewardMultiplier;
        public bool IsRepeatable;
        public bool RepeatedActionCategory;
        public int VarietyChain;
        public int VarietyBonusDna;
    }

    /// <summary>Fired when the player leaves predator threat range (used for "Escape predator" micro goal).</summary>
    public struct CellPlayerEscapedPredatorEvent : IGameEvent
    {
        public float BoostSeconds;
    }

    /// <summary>Fired when the player completes a cell upgrade milestone. Used for upgrade micro goals.</summary>
    public struct CellPlayerReproducedEvent : IGameEvent { }

    /// <summary>Fired when the player consumes a CellAI organism. PreyWasLarger is true when the prey's visual scale exceeded the player's.</summary>
    public struct CellAteLargerPreyEvent : IGameEvent { public bool PreyWasLarger; }

    /// <summary>Fired when the player collects a cell part pickup.</summary>
    public struct CellPartCollectedEvent : IGameEvent { public RoobsWorld.Stages.Cell.CellPartType PartType; }

    /// <summary>Fired when the player chooses a cell-stage mutation option.</summary>
    public struct CellMutationChosenEvent : IGameEvent
    {
        public string ChoiceLabel;
        public int TraitMask;
    }

    /// <summary>Fired when a new cell-stage species diverges from its parent (genetic distance threshold crossed).</summary>
    public struct CellNewSpeciesEvent : IGameEvent
    {
        public int NewSpeciesId;
        public int ParentSpeciesId;
        /// <summary>Total registered species count (including newly created) at the moment of the event.</summary>
        public int TotalSpeciesCount;
    }

    /// <summary>Fired when a cell-stage species' population reaches zero (extinction).</summary>
    public struct CellSpeciesExtinctEvent : IGameEvent
    {
        public int ExtinctSpeciesId;
        /// <summary>Total registered species count after this extinction.</summary>
        public int TotalSpeciesCount;
    }

    /// <summary>
    /// Fired when a <see cref="RoobsWorld.Stages.Cell.ViralParticle"/> makes contact with the player.
    /// Used by the HUD to flash a brief "Infected!" warning.
    /// </summary>
    public struct CellPlayerInfectedEvent : IGameEvent
    {
        /// <summary>Mass drained from the player on this contact hit.</summary>
        public float Drain;
    }

    /// <summary>Fired when a leviathan spawns in the cell stage. HUD shows a warning.</summary>
    public struct CellLeviathanSpawnedEvent : IGameEvent
    {
        public UnityEngine.Vector3 Position;
    }

    /// <summary>Fired when the player defeats a leviathan (health reaches zero).</summary>
    public struct CellLeviathanDefeatedEvent : IGameEvent
    {
        public UnityEngine.Vector3 Position;
    }

    // ---- Multiplayer ----

    /// <summary>Fired when a multiplayer session is created (host) or joined (client).</summary>
    public struct MultiplayerSessionStartedEvent : IGameEvent
    {
        public string SessionCode;
        public int    PlayerCount;
    }

    /// <summary>Fired when a remote player joins the current session.</summary>
    public struct MultiplayerPlayerJoinedEvent : IGameEvent
    {
        public int PlayerId;
    }

    /// <summary>Fired when a player leaves or disconnects from the session.</summary>
    public struct MultiplayerPlayerLeftEvent : IGameEvent
    {
        public int  PlayerId;
        public bool WasHost;
    }

    /// <summary>Fired when host authority migrates to a new player (e.g. original host disconnected).</summary>
    public struct MultiplayerHostMigratedEvent : IGameEvent
    {
        public int NewHostPlayerId;
    }

    /// <summary>Fired when a player creates a clan in the current multiplayer session.</summary>
    public struct MultiplayerClanCreatedEvent : IGameEvent
    {
        public int    ClanId;
        public string Name;
        public string Tag;
        public int    LeaderPlayerId;
    }

    /// <summary>Fired when a player changes ready state in the multiplayer lobby.</summary>
    public struct MultiplayerPlayerReadyChangedEvent : IGameEvent
    {
        public int  PlayerId;
        public bool IsReady;
    }

    /// <summary>Fired when a player moves between relay bandwidth lobbies.</summary>
    public struct MultiplayerPlayerLobbyChangedEvent : IGameEvent
    {
        public int    PlayerId;
        public string LobbyId;
    }

    /// <summary>Fired when the session host starts the shared multiplayer game.</summary>
    public struct MultiplayerGameStartedEvent : IGameEvent
    {
        public string SessionCode;
    }
}
