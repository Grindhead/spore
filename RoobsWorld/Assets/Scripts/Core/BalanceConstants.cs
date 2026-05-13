namespace RoobsWorld.Core
{
    /// <summary>
    /// Central balance tuning constants for the Cell stage.
    /// </summary>
    /// <remarks>
    /// All values are compile-time constants; for runtime tuning consider a ScriptableObject or JSON config loaded at startup.
    /// </remarks>
    public static class BalanceConstants
    {
        // ---- Cell Stage ----
        // Playtest levers: CellEvolutionMassThresholdBase, CellPlayerReproductionMassThreshold/Cost, CellDNAPointsPerFood/PerCellConsume/PerGoalComplete, CellBehaviourEnergyLowThreshold, CellAITickIntervalSeconds, CellDocumentaryTimeScaleOptions.
        /// <summary>Mass required (base) to trigger first evolution tier.</summary>
        public const float CellEvolutionMassThresholdBase = 10f;
        /// <summary>Multiplier applied to food nutrition when consumed by player cell.</summary>
        public const float CellFoodNutritionMultiplier = 1f;
        /// <summary>Scale factor for player cell visual size per mass unit.</summary>
        public const float CellMassToScaleFactor = 0.05f;
        /// <summary>Global visual size multiplier applied to cell-stage entities. Increase to make all entities appear larger.</summary>
        public const float CellEntityVisualScaleMultiplier = 2.5f;
        /// <summary>DNA points awarded when player cell consumes a food item.</summary>
        public const int CellDNAPointsPerFood = 1;
        /// <summary>DNA points awarded when player cell consumes another (smaller) cell.</summary>
        public const int CellDNAPointsPerCellConsume = 5;
        /// <summary>DNA points awarded when player selects a part at evolution (Cell Evolution UI).</summary>
        public const int CellDNAPointsPerEvolutionChoice = 5;
        /// <summary>DNA points awarded when player completes a micro goal (e.g. Eat N algae, Eat N dead cells).</summary>
        public const int CellDNAPointsPerGoalComplete = 3;
        /// <summary>Minimum multiplier used by repeatable risk-route Cell goals.</summary>
        public const int CellRepeatableRiskGoalRewardMultiplier = 2;
        /// <summary>Completed Cell goals before repeatable risk-route rewards begin ramping.</summary>
        public const int CellRepeatableRewardRampStartGoalCount = 9;
        /// <summary>Completed repeatable Cell goals between each additional risk-route reward multiplier.</summary>
        public const int CellRepeatableRewardRampInterval = 4;
        /// <summary>Maximum additional multiplier from late-run repeatable Cell goal reward ramping.</summary>
        public const int CellRepeatableRewardRampMaxBonus = 2;
        /// <summary>Consecutive varied Cell goal categories needed before adaptive-chain bonus DNA starts.</summary>
        public const int CellGoalVarietyBonusStartChain = 2;
        public const int CellGoalVarietyBonusDnaPerStep = 1;
        public const int CellGoalVarietyBonusMaxDna = 3;
        /// <summary>Boost-duration multiplier granted after the player fully escapes Cell-stage predator threat range.</summary>
        public const float CellPredatorEscapeBoostDurationMultiplier = 1.35f;
        /// <summary>Arena-radius increase per extra multiplayer cell-stage player.</summary>
        public const float CellMultiplayerArenaRadiusPerExtraPlayerFraction = 0.08f;
        /// <summary>Maximum arena-radius increase for multiplayer cell-stage sessions.</summary>
        public const float CellMultiplayerArenaRadiusMaxBonusFraction = 0.24f;
        /// <summary>Extra starting food spawned for each additional multiplayer player.</summary>
        public const int CellMultiplayerFoodPerExtraPlayer = 14;
        public const int CellMultiplayerFoodMaxBonus = 42;
        /// <summary>Extra decorative NPC cells spawned for each additional multiplayer player.</summary>
        public const int CellMultiplayerDecorativeNpcPerExtraPlayer = 4;
        public const int CellMultiplayerDecorativeNpcMaxBonus = 12;
        /// <summary>Extra simulated organisms spawned for each additional multiplayer player.</summary>
        public const int CellMultiplayerOrganismPerExtraPlayer = 5;
        public const int CellMultiplayerOrganismMaxBonus = 15;
        /// <summary>Extra starting part pickups spawned for each additional multiplayer player.</summary>
        public const int CellMultiplayerPartPickupPerExtraPlayer = 1;
        public const int CellMultiplayerPartPickupMaxBonus = 3;
        /// <summary>Number of extra multiplayer players needed before adding one extra starting predator.</summary>
        public const int CellMultiplayerPredatorExtraPlayersPerBonus = 2;
        public const int CellMultiplayerPredatorMaxBonus = 2;
        /// <summary>DNA points awarded for defeating a rival creature in Creature stage.</summary>
        public const int CreatureDNAPointsPerRivalDefeat = 4;
        /// <summary>DNA points awarded when completing the Creature-stage encounter objective.</summary>
        public const int CreatureDNAPointsObjectiveComplete = 20;
        /// <summary>Creature-stage stamina budget for attack, guard, and inherited abilities.</summary>
        public const float CreaturePlayerMaxStamina = 100f;
        public const float CreaturePlayerStaminaRecoveryPerSecond = 24f;
        public const float CreaturePlayerAttackStaminaCost = 18f;
        public const float CreaturePlayerGuardStaminaCost = 24f;
        public const float CreaturePlayerAbilityStaminaCost = 38f;
        public const float CreaturePlayerAbilityCooldownSeconds = 6f;
        public const float CreaturePlayerStaminaRestoredOnKill = 26f;
        public const float CreaturePlayerStaminaRestoredOnCounter = 14f;
        public const float CreatureObjectivePaceBonusSecondsPerDefeat = 18f;
        public const float CreatureObjectivePaceBonusHoldGraceFraction = 0.2f;
        public const int CreatureObjectivePaceBonusMaxDna = 6;
        public const float CreatureObjectiveMomentumDelayReductionPerClear = 0.45f;
        public const float CreatureObjectiveMomentumMinimumDelaySeconds = 3.5f;
        public const float CreatureObjectiveStaminaRestoreBase = 18f;
        public const float CreatureObjectiveStaminaRestorePaceBonusMultiplier = 1.5f;
        public const float CreatureObjectiveStaminaRestoreMax = 30f;
        /// <summary>Energy below this threshold triggers SeekFood when food is available (cell AI behaviour).</summary>
        public const float CellBehaviourEnergyLowThreshold = 30f;
        /// <summary>Energy required to reproduce (spawn offspring with mutated genome).</summary>
        public const float CellReproductionEnergyThreshold = 60f;
        /// <summary>Energy deducted from parent when reproducing (fixed cost).</summary>
        public const float CellReproductionEnergyCost = 40f;
        /// <summary>Initial energy given to newborn offspring.</summary>
        public const float CellOffspringInitialEnergy = 30f;
        /// <summary>Player cell mass threshold to trigger reproduction (mutation choice). Lower than evolution threshold so reproduction happens before the first evolution moment.</summary>
        public const float CellPlayerReproductionMassThreshold = 6f;
        /// <summary>Mass deducted from player after reproduction.</summary>
        public const float CellPlayerReproductionMassCost = 3f;
        /// <summary>Food items the player must consume before another upgrade opportunity can appear.</summary>
        public const int CellFoodConsumedPerUpgrade = 6;
        /// <summary>Genetic distance (sum of trait differences) above which offspring becomes a new species. Higher = less frequent but more meaningful speciation events.</summary>
        public const float CellSpeciationGeneticDistanceThreshold = 8f;
        /// <summary>Seconds between AI decisions for cell organisms (Step 9). Update less often than rendering for scale.</summary>
        public const float CellAITickIntervalSeconds = 0.2f;
        /// <summary>Fallback unboosted player cruise speed used when no live player can be sampled.</summary>
        public const float CellPlayerFallbackCruiseSpeed = 5.6f;
        /// <summary>Maximum normal organism speed as a fraction of current player cruise speed.</summary>
        public const float CellOrganismMaxSpeedToPlayerRatio = 1.05f;
        /// <summary>Maximum legacy NPC cell speed as a fraction of current player cruise speed.</summary>
        public const float CellNpcMaxSpeedToPlayerRatio = 0.92f;
        /// <summary>Maximum predator chase speed as a fraction of current player cruise speed.</summary>
        public const float CellPredatorMaxSpeedToPlayerRatio = 1.12f;
        /// <summary>Maximum short predator burst speed as a fraction of current player cruise speed.</summary>
        public const float CellPredatorBurstMaxSpeedToPlayerRatio = 1.38f;
        /// <summary>Cell size for spatial grid partitioning (Step 9). Larger = fewer cells, faster queries with less precision.</summary>
        public const float CellSpatialGridCellSize = 5f;
        /// <summary>Evolution Documentary Mode time-scale options (1Ã—, 2Ã—, 4Ã—). RightBracket cycles through these.</summary>
        public static readonly float[] CellDocumentaryTimeScaleOptions = { 1f, 2f, 4f };
        /// <summary>Distance within which player is considered "threatened" by a predator (Escape predator goal).</summary>
        public const float CellGoalPredatorThreatRadius = 20f;
        /// <summary>Distance beyond which player counts as having "escaped" (must be &gt; CellGoalPredatorThreatRadius).</summary>
        public const float CellGoalPredatorSafeRadius = 30f;
        /// <summary>Chance (0-1) that a dying NPC organism leaves a meat carcass behind.</summary>
        public const float CellCarcassDropChance = 0.75f;
        /// <summary>Minimum nutrition value for an organism carcass drop (small organisms).</summary>
        public const float CellCarcassNutritionMin = 5f;
        /// <summary>Maximum nutrition value for an organism carcass drop (large organisms).</summary>
        public const float CellCarcassNutritionMax = 12f;
        /// <summary>Visual scale treated as "small" for carcass nutrition interpolation.</summary>
        public const float CellCarcassScaleMin = 0.2f;
        /// <summary>Visual scale treated as "large" for carcass nutrition interpolation.</summary>
        public const float CellCarcassScaleMax = 1.5f;
        /// <summary>Random offset radius applied to carcass drop position so deaths do not stack perfectly.</summary>
        public const float CellCarcassDropScatterRadius = 0.9f;

        /// <summary>Seconds between dynamic food reinforcement checks.</summary>
        public const float CellDynamicFoodCheckIntervalSeconds = 10f;
        /// <summary>Minimum total food count before additional plant food is spawned.</summary>
        public const int   CellDynamicMinimumFoodCount = 60;
        /// <summary>Number of plant food items spawned when dynamic reinforcement triggers.</summary>
        public const int   CellDynamicFoodSpawnCount = 10;
        /// <summary>Seconds between predator reinforcement checks as the player grows stronger.</summary>
        public const float CellDynamicPredatorReinforcementIntervalSeconds = 60f;
        /// <summary>Player stage progress fraction required before predator reinforcements can spawn.</summary>
        public const float CellDynamicPredatorMassThresholdFraction = 0.9f;
        /// <summary>Maximum number of predators allowed when dynamic reinforcement is active.</summary>
        public const int   CellDynamicPredatorMaxCount = 1;
        /// <summary>Minimum player distance for predator reinforcement spawn points.</summary>
        public const float CellDynamicPredatorMinSpawnDistanceFromPlayer = 18f;
        /// <summary>Attempts used to find a fair reinforcement spawn before falling back to opposite-side spawn.</summary>
        public const int   CellDynamicPredatorSpawnAttempts = 6;
        /// <summary>Seconds of Cell-stage runtime needed for nutrient bloom scaling to reach its maximum intensity.</summary>
        public const float CellNutrientBloomProgressionSecondsToMax = 240f;
        /// <summary>Maximum multiplier applied to nutrient bloom food count at late-stage progression.</summary>
        public const float CellNutrientBloomMaxFoodScale = 1.85f;
        /// <summary>Minimum cluster radius as a fraction of arena radius when spawning nutrient bloom hotspots.</summary>
        public const float CellNutrientBloomClusterRadiusFractionMin = 0.08f;
        /// <summary>Maximum cluster radius as a fraction of arena radius when spawning nutrient bloom hotspots.</summary>
        public const float CellNutrientBloomClusterRadiusFractionMax = 0.18f;
        /// <summary>Pulse frequency (Hz-like scalar) used by the Nutrient Bloom HUD banner animation.</summary>
        public const float CellNutrientBloomHudPulseFrequency = 5f;
        /// <summary>Blend strength for herbivore/phototrophic NPC drift toward active nutrient bloom hotspots.</summary>
        public const float CellNutrientBloomHerbivoreDriftStrength = 0.55f;

        // ---- Cell Stage: Genetic Traits ----
        /// <summary>Energy per second gained by Photosynthetic organisms in light biomes (AlgaeForest/OpenWater).</summary>
        public const float GeneticTraitPhotosyntheticEnergyRate = 3f;
        /// <summary>Energy drained from target per second by Toxic contact.</summary>
        public const float GeneticTraitToxicContactDrain = 4f;
        /// <summary>Fraction of incoming damage absorbed by Armoured trait (0 = no reduction, 1 = full immunity).</summary>
        public const float GeneticTraitArmouredDamageReduction = 0.35f;
        /// <summary>Energy regenerated per second by Regenerative trait (active only when energy &gt; 10).</summary>
        public const float GeneticTraitRegenerativeRate = 1.5f;
        /// <summary>Radius of the Electric stun pulse.</summary>
        public const float GeneticTraitElectricPulseRadius = 4f;
        /// <summary>Seconds between Electric pulse firings.</summary>
        public const float GeneticTraitElectricPulseInterval = 3f;
        /// <summary>Seconds that shocked organisms are slowed after an Electric pulse.</summary>
        public const float GeneticTraitElectricSlowDuration = 1.2f;

        // ---- Cell Stage: Biomes (Step 6) ----
        /// <summary>Radius fraction of arena (0â€“1): inner band is Deep zone (0 to this).</summary>
        public const float CellBiomeDeepZoneMaxRadiusFraction = 0.2f;
        /// <summary>Radius fraction: OpenWater band (DeepZoneMax to this).</summary>
        public const float CellBiomeOpenWaterMaxRadiusFraction = 0.6f;
        /// <summary>Radius fraction: AlgaeForest band (OpenWaterMax to this). Outer ring beyond is OpenWater.</summary>
        public const float CellBiomeAlgaeForestMaxRadiusFraction = 0.9f;
        /// <summary>Toxic zone center offset from arena center, as fraction of arena radius (X and Z).</summary>
        public const float CellBiomeToxicZoneCenterOffsetFraction = 0.4f;
        /// <summary>Toxic zone radius as fraction of arena radius.</summary>
        public const float CellBiomeToxicZoneRadiusFraction = 0.2f;
        /// <summary>Metabolism multiplier in Deep zone (cold, slower burn).</summary>
        public const float CellBiomeMetabolismMultiplierDeep = 0.7f;
        /// <summary>Metabolism multiplier in OpenWater.</summary>
        public const float CellBiomeMetabolismMultiplierOpenWater = 1f;
        /// <summary>Metabolism multiplier in AlgaeForest.</summary>
        public const float CellBiomeMetabolismMultiplierAlgaeForest = 1f;
        /// <summary>Metabolism multiplier in Toxic zone (stress). Higher = riskier to linger.</summary>
        public const float CellBiomeMetabolismMultiplierToxic = 1.5f;
        /// <summary>Speed multiplier in Deep zone.</summary>
        public const float CellBiomeSpeedMultiplierDeep = 0.8f;
        /// <summary>Speed multiplier in OpenWater.</summary>
        public const float CellBiomeSpeedMultiplierOpenWater = 1f;
        /// <summary>Speed multiplier in AlgaeForest.</summary>
        public const float CellBiomeSpeedMultiplierAlgaeForest = 1f;
        /// <summary>Speed multiplier in Toxic zone.</summary>
        public const float CellBiomeSpeedMultiplierToxic = 0.85f;
        /// <summary>Food efficiency multiplier in Toxic zone (sickness).</summary>
        public const float CellBiomeFoodEfficiencyMultiplierToxic = 0.7f;
        /// <summary>Food efficiency multiplier in other zones (no penalty).</summary>
        public const float CellBiomeFoodEfficiencyMultiplierDefault = 1f;

        // ---- Cell Stage: Viral Particles (VirusOutbreak event) ----
        /// <summary>Number of viral particles spawned when a VirusOutbreak event starts.</summary>
        public const int ViralParticleSpawnCount = 18;
        /// <summary>Visual scale of each viral particle (world units). Much smaller than a normal cell.</summary>
        public const float ViralParticleScale = 0.32f;
        /// <summary>Movement speed of a viral particle seeking its target (units/s).</summary>
        public const float ViralParticleSpeed = 9f;
        /// <summary>Seconds a viral particle exists before self-destructing if it hasn't hit anything.</summary>
        public const float ViralParticleLifetimeSeconds = 30f;
        /// <summary>Mass drained from the player when a viral particle lands a hit.</summary>
        public const float ViralParticleDrainOnPlayer = 0.6f;
        /// <summary>Multiplier applied to player drain when the player carries the Armoured trait.</summary>
        public const float ViralParticleArmouredDrainReduction = 0.5f;
        /// <summary>Energy/mass drained from an NPC organism when a viral particle hits it.</summary>
        public const float ViralParticleDrainOnOrganism = 8f;

        // ---- Cell Stage: Leviathan ----
        /// <summary>Seconds into the run before a leviathan can first spawn.</summary>
        public const float LeviathanMinSpawnSeconds = 90f;
        /// <summary>Minimum number of species required before a leviathan spawns.</summary>
        public const int LeviathanMinSpeciesCount = 3;
        /// <summary>Seconds between leviathan despawn/defeat and the next spawn check.</summary>
        public const float LeviathanRespawnIntervalSeconds = 120f;
        /// <summary>Visual scale factor (world units) of the leviathan GameObject.</summary>
        public const float LeviathanSize = 8f;
        /// <summary>Maximum health of the leviathan.</summary>
        public const float LeviathanMaxHealth = 200f;
        /// <summary>Damage the leviathan deals per contact tick.</summary>
        public const float LeviathanDamagePerHit = 5f;
        /// <summary>Seconds between leviathan contact damage ticks.</summary>
        public const float LeviathanDamageCooldown = 0.8f;
        /// <summary>Patrol movement speed (slow, majestic).</summary>
        public const float LeviathanPatrolSpeed = 3f;
        /// <summary>Hunt movement speed (closing in on player).</summary>
        public const float LeviathanHuntSpeed = 6f;
        /// <summary>Charge dash speed after wind-up.</summary>
        public const float LeviathanChargeSpeed = 18f;
        /// <summary>Radius within which the leviathan detects the player and transitions to Hunt.</summary>
        public const float LeviathanDetectionRadius = 35f;
        /// <summary>Distance from player that triggers the charge wind-up.</summary>
        public const float LeviathanChargeWindupRadius = 12f;
        /// <summary>Seconds of telegraph slow-creep before the charge dash fires.</summary>
        public const float LeviathanChargeWindupSeconds = 1.5f;
        /// <summary>Seconds the charge dash lasts before transitioning to Rest.</summary>
        public const float LeviathanChargeDurationSeconds = 1.2f;
        /// <summary>Seconds the leviathan rests after a charge.</summary>
        public const float LeviathanRestDurationSeconds = 2.5f;
        /// <summary>Seconds the leviathan patrols before picking a new waypoint.</summary>
        public const float LeviathanPatrolDuration = 5f;
        /// <summary>Maximum distance from arena center used when picking patrol targets.</summary>
        public const float LeviathanPatrolRadius = 30f;
        /// <summary>Distance the player must reach from the leviathan to count as "survived".</summary>
        public const float LeviathanSurviveRadius = 40f;
        /// <summary>Number of food items dropped when a leviathan is defeated.</summary>
        public const int LeviathanFoodDropCount = 12;
        /// <summary>Nutrition value of each food item dropped on leviathan death.</summary>
        public const float LeviathanFoodDropNutrition = 10f;

    }

    /// <summary>
    /// Runtime-selectable feel presets for the Cell Stage.
    /// </summary>
    public enum CellFeelProfile
    {
        Classic,
        Arcade,
        Documentary,
        Survival
    }

    /// <summary>
    /// Centralized multipliers for quickly iterating on cell-stage moment-to-moment feel.
    /// </summary>
    public static class CellFeelDirector
    {
        private static CellFeelProfile _currentProfile = CellFeelProfile.Classic;

        public static CellFeelProfile CurrentProfile => _currentProfile;

        public static CellFeelProfile CycleNext()
        {
            _currentProfile = _currentProfile switch
            {
                CellFeelProfile.Classic => CellFeelProfile.Arcade,
                CellFeelProfile.Arcade => CellFeelProfile.Documentary,
                CellFeelProfile.Documentary => CellFeelProfile.Survival,
                _ => CellFeelProfile.Classic
            };
            return _currentProfile;
        }

        public static void SetProfile(CellFeelProfile profile)
        {
            _currentProfile = profile;
        }

        public static string GetProfileLocalizationKey(CellFeelProfile profile)
        {
            return profile switch
            {
                CellFeelProfile.Classic => "cell.feel.classic",
                CellFeelProfile.Arcade => "cell.feel.arcade",
                CellFeelProfile.Documentary => "cell.feel.documentary",
                CellFeelProfile.Survival => "cell.feel.survival",
                _ => "cell.feel.classic"
            };
        }

        public static float PlayerSpeedMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.12f,
            CellFeelProfile.Documentary => 0.9f,
            CellFeelProfile.Survival => 0.95f,
            _ => 1f
        };

        public static float PlayerAccelerationMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.18f,
            CellFeelProfile.Documentary => 0.82f,
            CellFeelProfile.Survival => 1f,
            _ => 1f
        };

        public static float PlayerTurnMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.2f,
            CellFeelProfile.Documentary => 0.86f,
            CellFeelProfile.Survival => 0.96f,
            _ => 1f
        };

        public static float PlayerBoostDurationMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.1f,
            CellFeelProfile.Documentary => 1f,
            CellFeelProfile.Survival => 0.92f,
            _ => 1f
        };

        public static float PredatorPerceptionMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 0.95f,
            CellFeelProfile.Documentary => 0.85f,
            CellFeelProfile.Survival => 1.18f,
            _ => 1f
        };

        public static float PredatorSpeedMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 0.95f,
            CellFeelProfile.Documentary => 0.82f,
            CellFeelProfile.Survival => 1.1f,
            _ => 1f
        };

        public static float PredatorDamageMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 0.88f,
            CellFeelProfile.Documentary => 0.78f,
            CellFeelProfile.Survival => 1.22f,
            _ => 1f
        };

        public static float PredatorAttackCadenceMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 0.92f,
            CellFeelProfile.Documentary => 0.82f,
            CellFeelProfile.Survival => 1.18f,
            _ => 1f
        };

        public static float NpcCellSpeedMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.05f,
            CellFeelProfile.Documentary => 0.86f,
            CellFeelProfile.Survival => 1.06f,
            _ => 1f
        };

        public static float CameraFollowMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.12f,
            CellFeelProfile.Documentary => 0.82f,
            CellFeelProfile.Survival => 1f,
            _ => 1f
        };

        public static float CameraZoomMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 0.94f,
            CellFeelProfile.Documentary => 1.18f,
            CellFeelProfile.Survival => 0.9f,
            _ => 1f
        };

        public static float CameraLeadMultiplier => _currentProfile switch
        {
            CellFeelProfile.Classic => 1f,
            CellFeelProfile.Arcade => 1.1f,
            CellFeelProfile.Documentary => 0.78f,
            CellFeelProfile.Survival => 1f,
            _ => 1f
        };
    }
}
