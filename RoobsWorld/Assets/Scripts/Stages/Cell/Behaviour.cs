using UnityEngine;
using RoobsWorld.Core;

namespace RoobsWorld.Stages.Cell
{
    /// <summary>Behaviour decision for cell-stage AI: SeekFood, Flee, Attack, Reproduce, Wander.</summary>
    public enum BehaviourState
    {
        SeekFood,
        Flee,
        Attack,
        Reproduce,
        Wander
    }

    /// <summary>Result of a behaviour decision: state and optional target position for movement.</summary>
    public struct BehaviourResult
    {
        public BehaviourState State;
        /// <summary>Target position for movement (e.g. food for SeekFood, away-from-threat for Flee, prey for Attack). Null or zero for Wander.</summary>
        public Vector3? TargetPosition;
    }

    /// <summary>Context passed into behaviour decision (energy, nearby food/threat/prey).</summary>
    public struct BehaviourContext
    {
        public float Energy;
        public Vector3 MyPosition;
        public bool HasNearestFood;
        public Vector3 NearestFoodPosition;
        public float NearestFoodDistance;
        public bool HasThreat;
        public Vector3 ThreatPosition;
        public float ThreatDistance;
        public bool HasPrey;
        public Vector3 PreyPosition;
        public float PreyDistance;
    }

    /// <summary>
    /// Cell-stage AI decision-making: SeekFood, Flee, Attack, Wander based on energy and environment.
    /// Step 2 of cell-stage evolution roadmap. Pure logic for testability.
    /// </summary>
    public static class Behaviour
    {
        private const float ThreatRiskNearDistance = 1.6f;
        private const float ThreatRiskFarDistance = 12f;
        private const float ImmediateThreatDistance = ThreatRiskNearDistance;
        private const float ResourceOpportunityCloseDistance = 0.25f;
        private const float ResourceOpportunityFarDistance = 18f;

        /// <summary>Continuous decision scores used for state selection.</summary>
        public struct BehaviourDecisionScores
        {
            public float HungerPressure;
            public float ThreatRisk;
            public float FoodOpportunity;
            public float PreyOpportunity;
            public float FleeUtility;
            public float SeekFoodUtility;
            public float AttackUtility;
            public float ReproduceUtility;
            public float WanderUtility;
        }

        /// <summary>Energy below this triggers SeekFood when food is available.</summary>
        public static float GetLowEnergyThreshold()
        {
            return BalanceConstants.CellBehaviourEnergyLowThreshold;
        }

        /// <summary>
        /// Maps threat distance to risk in [0, 1].
        /// 1 = immediate danger, 0 = negligible predation pressure.
        /// </summary>
        public static float EvaluateThreatRisk(float threatDistance)
        {
            if (threatDistance <= ThreatRiskNearDistance) return 1f;
            if (threatDistance >= ThreatRiskFarDistance) return 0f;

            float span = Mathf.Max(0.01f, ThreatRiskFarDistance - ThreatRiskNearDistance);
            float t = (threatDistance - ThreatRiskNearDistance) / span;
            return 1f - Mathf.Clamp01(t);
        }

        /// <summary>
        /// Maps resource distance (food/prey) to opportunity quality in [0, 1].
        /// 1 = immediate gain, 0 = too far to be worth current attention.
        /// </summary>
        public static float EvaluateResourceOpportunity(float resourceDistance)
        {
            if (resourceDistance <= ResourceOpportunityCloseDistance) return 1f;
            if (resourceDistance >= ResourceOpportunityFarDistance) return 0f;

            float span = Mathf.Max(0.01f, ResourceOpportunityFarDistance - ResourceOpportunityCloseDistance);
            float t = (resourceDistance - ResourceOpportunityCloseDistance) / span;
            return 1f - Mathf.Clamp01(t);
        }

        private static float ResolveDistance(float providedDistance, Vector3 from, Vector3 to)
        {
            if (providedDistance > 0f)
                return providedDistance;

            to.y = from.y;
            return Vector3.Distance(from, to);
        }

        /// <summary>
        /// Scores all behaviour utilities from a state-dependent survival model:
        /// predation pressure, starvation pressure, and local opportunity quality.
        /// </summary>
        public static BehaviourDecisionScores ScoreDecisions(BehaviourContext context)
        {
            float lowEnergy = GetLowEnergyThreshold();
            float hungerPressure = Mathf.Clamp01((lowEnergy - context.Energy) / Mathf.Max(1f, lowEnergy));
            float satiety = 1f - hungerPressure;

            float threatDistance = context.HasThreat
                ? ResolveDistance(context.ThreatDistance, context.MyPosition, context.ThreatPosition)
                : ThreatRiskFarDistance;
            float foodDistance = context.HasNearestFood
                ? ResolveDistance(context.NearestFoodDistance, context.MyPosition, context.NearestFoodPosition)
                : ResourceOpportunityFarDistance;
            float preyDistance = context.HasPrey
                ? ResolveDistance(context.PreyDistance, context.MyPosition, context.PreyPosition)
                : ResourceOpportunityFarDistance;

            float threatRisk = context.HasThreat ? EvaluateThreatRisk(threatDistance) : 0f;
            float foodOpportunity = context.HasNearestFood ? EvaluateResourceOpportunity(foodDistance) : 0f;
            float preyOpportunity = context.HasPrey ? EvaluateResourceOpportunity(preyDistance) : 0f;

            // Hungrier cells accept more predation risk while foraging.
            float predationPressure = threatRisk * Mathf.Lerp(1f, 0.68f, hungerPressure);

            var scores = new BehaviourDecisionScores
            {
                HungerPressure = hungerPressure,
                ThreatRisk = threatRisk,
                FoodOpportunity = foodOpportunity,
                PreyOpportunity = preyOpportunity,
                FleeUtility = float.NegativeInfinity,
                SeekFoodUtility = float.NegativeInfinity,
                AttackUtility = float.NegativeInfinity,
                ReproduceUtility = float.NegativeInfinity,
                WanderUtility = 0.08f + satiety * 0.12f - predationPressure * 0.45f
            };

            if (context.HasThreat)
            {
                scores.FleeUtility =
                    predationPressure * (0.82f + satiety * 0.24f)
                    - hungerPressure * 0.30f;
            }

            if (context.HasNearestFood)
            {
                scores.SeekFoodUtility =
                    foodOpportunity * (0.32f + hungerPressure * 1.45f)
                    - predationPressure * 0.72f;
            }

            if (context.HasPrey)
            {
                scores.AttackUtility =
                    preyOpportunity * (0.42f + satiety * 0.66f + hungerPressure * 0.22f)
                    - predationPressure * 0.90f;
            }

            if (context.HasThreat && threatDistance <= ImmediateThreatDistance)
            {
                // Contact-range threat should dominate unless starvation pressure is extreme.
                scores.FleeUtility += 0.55f;
                scores.SeekFoodUtility -= 0.40f;
                scores.AttackUtility -= 0.45f;
            }

            if (context.HasNearestFood && context.HasPrey)
            {
                // Optimal foraging style tradeoff: hungry cells prefer the safer/closer resource.
                float profitabilityDelta = preyOpportunity - foodOpportunity;
                if (hungerPressure > 0.45f && profitabilityDelta < 0f)
                    scores.AttackUtility -= 0.25f;
                if (satiety > 0.55f && profitabilityDelta > 0f)
                    scores.AttackUtility += 0.12f;
            }

            float reproductionThreshold = ReproductionSystem.GetReproductionThreshold();
            if (context.Energy >= reproductionThreshold)
            {
                float surplus = Mathf.Clamp01(
                    (context.Energy - reproductionThreshold) / Mathf.Max(1f, reproductionThreshold));
                scores.ReproduceUtility =
                    0.84f + surplus * 0.52f
                    - predationPressure * 0.90f
                    - hungerPressure * 0.55f;
            }

            return scores;
        }

        /// <summary>
        /// Returns true when fleeing has higher utility than all available alternatives.
        /// </summary>
        public static bool ShouldFleeUnderPredationRisk(BehaviourContext context)
        {
            if (!context.HasThreat)
                return false;

            BehaviourDecisionScores scores = ScoreDecisions(context);
            float bestAlternative = Mathf.Max(
                Mathf.Max(scores.SeekFoodUtility, scores.AttackUtility),
                Mathf.Max(scores.ReproduceUtility, scores.WanderUtility));
            return scores.FleeUtility >= bestAlternative;
        }

        /// <summary>
        /// Decides current behaviour from context.
        /// Uses utility scores from ScoreDecisions and selects the highest utility state.
        /// </summary>
        public static BehaviourResult Decide(BehaviourContext context)
        {
            BehaviourDecisionScores scores = ScoreDecisions(context);

            BehaviourState chosenState = BehaviourState.Wander;
            float best = scores.WanderUtility;

            if (scores.SeekFoodUtility > best)
            {
                best = scores.SeekFoodUtility;
                chosenState = BehaviourState.SeekFood;
            }
            if (scores.AttackUtility > best)
            {
                best = scores.AttackUtility;
                chosenState = BehaviourState.Attack;
            }
            if (scores.ReproduceUtility > best)
            {
                best = scores.ReproduceUtility;
                chosenState = BehaviourState.Reproduce;
            }
            if (scores.FleeUtility > best)
            {
                chosenState = BehaviourState.Flee;
            }

            switch (chosenState)
            {
                case BehaviourState.Flee:
                {
                    Vector3 away = context.MyPosition - context.ThreatPosition;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f)
                        away = Vector3.forward;
                    else
                        away.Normalize();
                    return new BehaviourResult
                    {
                        State = BehaviourState.Flee,
                        TargetPosition = context.MyPosition + away * 10f
                    };
                }
                case BehaviourState.SeekFood:
                    return new BehaviourResult
                    {
                        State = BehaviourState.SeekFood,
                        TargetPosition = context.NearestFoodPosition
                    };
                case BehaviourState.Attack:
                    return new BehaviourResult
                    {
                        State = BehaviourState.Attack,
                        TargetPosition = context.PreyPosition
                    };
                case BehaviourState.Reproduce:
                    return new BehaviourResult
                    {
                        State = BehaviourState.Reproduce,
                        TargetPosition = null
                    };
                default:
                    return new BehaviourResult
                    {
                        State = BehaviourState.Wander,
                        TargetPosition = null
                    };
            }
        }

        /// <summary>
        /// Decides current behaviour while applying short-term behavioural persistence.
        /// This avoids lifeless twitching between equally plausible actions on adjacent AI ticks.
        /// </summary>
        public static BehaviourResult DecideWithMemory(BehaviourContext context, BehaviourState previousState)
        {
            BehaviourResult fresh = Decide(context);
            if (fresh.State == previousState)
                return fresh;

            BehaviourDecisionScores scores = ScoreDecisions(context);
            BehaviourState remembered = ResolveRememberedState(context, scores, previousState, fresh.State);
            return remembered == fresh.State ? fresh : BuildResultForState(context, remembered);
        }

        public static BehaviourState ResolveRememberedState(
            BehaviourContext context,
            BehaviourDecisionScores scores,
            BehaviourState previousState,
            BehaviourState freshState)
        {
            if (freshState == BehaviourState.Flee || freshState == BehaviourState.Reproduce)
                return freshState;

            switch (previousState)
            {
                case BehaviourState.Flee:
                    if (context.HasThreat && scores.ThreatRisk > 0.24f)
                        return BehaviourState.Flee;
                    break;
                case BehaviourState.SeekFood:
                    if (context.HasNearestFood
                        && scores.FoodOpportunity > 0.30f
                        && scores.FleeUtility < scores.SeekFoodUtility + 0.18f)
                        return BehaviourState.SeekFood;
                    break;
                case BehaviourState.Attack:
                    if (context.HasPrey
                        && scores.PreyOpportunity > 0.28f
                        && scores.ThreatRisk < 0.55f
                        && freshState != BehaviourState.SeekFood)
                        return BehaviourState.Attack;
                    break;
            }

            return freshState;
        }

        private static BehaviourResult BuildResultForState(BehaviourContext context, BehaviourState state)
        {
            switch (state)
            {
                case BehaviourState.Flee:
                {
                    Vector3 away = context.MyPosition - context.ThreatPosition;
                    away.y = 0f;
                    if (away.sqrMagnitude < 0.01f)
                        away = Vector3.forward;
                    else
                        away.Normalize();
                    return new BehaviourResult
                    {
                        State = BehaviourState.Flee,
                        TargetPosition = context.MyPosition + away * 10f
                    };
                }
                case BehaviourState.SeekFood:
                    return new BehaviourResult
                    {
                        State = BehaviourState.SeekFood,
                        TargetPosition = context.NearestFoodPosition
                    };
                case BehaviourState.Attack:
                    return new BehaviourResult
                    {
                        State = BehaviourState.Attack,
                        TargetPosition = context.PreyPosition
                    };
                case BehaviourState.Reproduce:
                    return new BehaviourResult
                    {
                        State = BehaviourState.Reproduce,
                        TargetPosition = null
                    };
                default:
                    return new BehaviourResult
                    {
                        State = BehaviourState.Wander,
                        TargetPosition = null
                    };
            }
        }

        /// <summary>Converts a behaviour result and current position into a move direction (XZ, normalized). Wander uses optional wanderDirection (e.g. random).</summary>
        public static Vector3 GetMoveDirection(BehaviourResult result, Vector3 myPosition, Vector3 wanderDirection = default)
        {
            if (result.State == BehaviourState.Reproduce)
                return Vector3.zero;
            if (result.State == BehaviourState.Wander)
            {
                wanderDirection.y = 0f;
                if (wanderDirection.sqrMagnitude < 0.01f)
                    return Vector3.zero;
                return wanderDirection.normalized;
            }
            if (result.TargetPosition == null)
                return Vector3.zero;
            Vector3 to = result.TargetPosition.Value - myPosition;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f)
                return Vector3.zero;
            return to.normalized;
        }
    }
}
