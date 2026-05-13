using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests to lock down BalanceConstants sanity for Phase 8.3 tuning.
    /// Prevents invalid constants (e.g. negative thresholds, zero tick interval) from breaking gameplay.
    /// </summary>
    public class BalanceConstantsTests
    {
        [Test]
        public void CellEvolutionMassThresholdBase_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellEvolutionMassThresholdBase, 0f);
        }

        [Test]
        public void CellAITickIntervalSeconds_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellAITickIntervalSeconds, 0f);
        }

        [Test]
        public void CellAISpeedCaps_ArePlayerRelativeAndSane()
        {
            Assert.Greater(BalanceConstants.CellPlayerFallbackCruiseSpeed, 0f);
            Assert.Greater(BalanceConstants.CellOrganismMaxSpeedToPlayerRatio, 0f);
            Assert.Greater(BalanceConstants.CellNpcMaxSpeedToPlayerRatio, 0f);
            Assert.Greater(BalanceConstants.CellPredatorMaxSpeedToPlayerRatio, 0f);
            Assert.Greater(BalanceConstants.CellPredatorBurstMaxSpeedToPlayerRatio, BalanceConstants.CellPredatorMaxSpeedToPlayerRatio);
            Assert.LessOrEqual(BalanceConstants.CellNpcMaxSpeedToPlayerRatio, BalanceConstants.CellOrganismMaxSpeedToPlayerRatio);
        }

        [Test]
        public void CellSpatialGridCellSize_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellSpatialGridCellSize, 0f);
        }

        [Test]
        public void CellReproductionConstants_ArePositiveAndConsistent()
        {
            Assert.Greater(BalanceConstants.CellReproductionEnergyThreshold, 0f);
            Assert.Greater(BalanceConstants.CellReproductionEnergyCost, 0f);
            Assert.Greater(BalanceConstants.CellOffspringInitialEnergy, 0f);
            Assert.LessOrEqual(BalanceConstants.CellReproductionEnergyCost, BalanceConstants.CellReproductionEnergyThreshold,
                "Reproduction cost should not exceed threshold.");
        }

        [Test]
        public void CellBiomeRadiusFractions_AreInValidRange()
        {
            Assert.Greater(BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction, 0f);
            Assert.LessOrEqual(BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction, 1f);
            Assert.Greater(BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction, BalanceConstants.CellBiomeDeepZoneMaxRadiusFraction);
            Assert.Greater(BalanceConstants.CellBiomeAlgaeForestMaxRadiusFraction, BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction);
            Assert.Greater(BalanceConstants.CellBiomeToxicZoneRadiusFraction, 0f);
            Assert.LessOrEqual(BalanceConstants.CellBiomeToxicZoneRadiusFraction, 1f);
        }

        [Test]
        public void CellDNAPoints_AreNonNegative()
        {
            Assert.GreaterOrEqual(BalanceConstants.CellDNAPointsPerFood, 0);
            Assert.GreaterOrEqual(BalanceConstants.CellDNAPointsPerCellConsume, 0);
            Assert.GreaterOrEqual(BalanceConstants.CellDNAPointsPerEvolutionChoice, 0);
            Assert.GreaterOrEqual(BalanceConstants.CellDNAPointsPerGoalComplete, 0);
        }

        [Test]
        public void CellDNAPointsPerGoalComplete_IsPositive_SoGoalsAreRewarding()
        {
            Assert.Greater(BalanceConstants.CellDNAPointsPerGoalComplete, 0,
                "Micro goal completion should grant a positive DNA reward.");
        }

        [Test]
        public void DNARewards_CreateReadableProgressionBudget()
        {
            Assert.Less(BalanceConstants.CellDNAPointsPerFood, BalanceConstants.CellDNAPointsPerGoalComplete);
            Assert.LessOrEqual(BalanceConstants.CellDNAPointsPerGoalComplete, BalanceConstants.CellDNAPointsPerCellConsume);
            Assert.Greater(BalanceConstants.CreatureDNAPointsObjectiveComplete, BalanceConstants.CreatureDNAPointsPerRivalDefeat);
            Assert.LessOrEqual(BalanceConstants.CreatureDNAPointsObjectiveComplete, 25);
        }

        [Test]
        public void PassFiveGameplayTuningConstants_AreSane()
        {
            Assert.GreaterOrEqual(BalanceConstants.CellRepeatableRiskGoalRewardMultiplier, 2);
            Assert.Greater(BalanceConstants.CreaturePlayerMaxStamina, 0f);
            Assert.Greater(BalanceConstants.CreaturePlayerStaminaRecoveryPerSecond, 0f);
            Assert.Greater(BalanceConstants.CreaturePlayerAttackStaminaCost, 0f);
            Assert.Greater(BalanceConstants.CreaturePlayerGuardStaminaCost, 0f);
            Assert.Greater(BalanceConstants.CreaturePlayerAbilityStaminaCost, BalanceConstants.CreaturePlayerAttackStaminaCost);
            Assert.Greater(BalanceConstants.CreaturePlayerAbilityCooldownSeconds, 0f);
            Assert.Greater(BalanceConstants.CreaturePlayerStaminaRestoredOnKill, 0f);
            Assert.Greater(BalanceConstants.CreaturePlayerStaminaRestoredOnCounter, 0f);
            Assert.Less(BalanceConstants.CreaturePlayerStaminaRestoredOnCounter, BalanceConstants.CreaturePlayerStaminaRestoredOnKill);
        }

        [Test]
        public void CellPlayerReproductionMassCost_LessThanThreshold()
        {
            Assert.Less(BalanceConstants.CellPlayerReproductionMassCost, BalanceConstants.CellPlayerReproductionMassThreshold,
                "Reproduction cost should be less than threshold so player can reproduce without immediately falling below threshold.");
        }

        [Test]
        public void CellBehaviourEnergyLowThreshold_IsPositive()
        {
            Assert.Greater(BalanceConstants.CellBehaviourEnergyLowThreshold, 0f,
                "Energy low threshold for AI SeekFood behaviour must be positive.");
        }

        [Test]
        public void CellGoalPredatorSafeRadius_GreaterThanThreatRadius()
        {
            Assert.Greater(BalanceConstants.CellGoalPredatorSafeRadius, BalanceConstants.CellGoalPredatorThreatRadius,
                "Safe radius must exceed threat radius so escape is detected with hysteresis.");
            Assert.Greater(BalanceConstants.CellGoalPredatorThreatRadius, 0f);
        }

        [Test]
        public void CellCarcassDropConstants_AreSane()
        {
            Assert.GreaterOrEqual(BalanceConstants.CellCarcassDropChance, 0f);
            Assert.LessOrEqual(BalanceConstants.CellCarcassDropChance, 1f);
            Assert.Greater(BalanceConstants.CellCarcassNutritionMin, 0f);
            Assert.Greater(BalanceConstants.CellCarcassNutritionMax, BalanceConstants.CellCarcassNutritionMin);
            Assert.Greater(BalanceConstants.CellCarcassScaleMax, BalanceConstants.CellCarcassScaleMin);
            Assert.GreaterOrEqual(BalanceConstants.CellCarcassDropScatterRadius, 0f);
        }
    }
}
