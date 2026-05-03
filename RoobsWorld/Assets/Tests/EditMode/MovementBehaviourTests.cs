using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using CellBehaviour = RoobsWorld.Stages.Cell.Behaviour;

namespace RoobsWorld.Tests
{
    public class MovementTests
    {
        [Test]
        public void GetSpeed_WithNullGenome_ReturnsBaseScale()
        {
            float speed = Movement.GetSpeed(null);
            Assert.AreEqual(Movement.SpeedScale, speed);
        }

        [Test]
        public void GetSpeed_WithGenome_ReturnsSpeedTimesScale()
        {
            var g = new Genome { Speed = 2f };
            float speed = Movement.GetSpeed(g);
            Assert.AreEqual(2f * Movement.SpeedScale, speed);
        }

        [Test]
        public void ApplyWobble_WithZeroDirection_ReturnsForward()
        {
            var g = Genome.Default();
            Vector3 r = Movement.ApplyWobble(Vector3.zero, 0f, g);
            Assert.AreEqual(Vector3.forward, r);
        }

        [Test]
        public void ApplyWobble_ReturnsNormalizedVector()
        {
            var g = new Genome { Flagella = 5f };
            Vector3 dir = new Vector3(1f, 0f, 1f);
            Vector3 r = Movement.ApplyWobble(dir, 1f, g);
            Assert.AreEqual(1f, r.magnitude, 0.001f);
            Assert.AreEqual(0f, r.y, 0.001f);
        }

        [Test]
        public void ApplyWobble_WithZeroFlagella_ReturnsSameDirection()
        {
            var g = new Genome { Flagella = 0f };
            Vector3 dir = new Vector3(1f, 0f, 0f).normalized;
            Vector3 r = Movement.ApplyWobble(dir, 1f, g);
            Assert.AreEqual(dir.x, r.x, 0.001f);
            Assert.AreEqual(dir.z, r.z, 0.001f);
        }

        [Test]
        public void ApplyWobble_WithFlagella_VariesWithTime()
        {
            var g = new Genome { Flagella = 10f };
            Vector3 dir = Vector3.forward;
            Vector3 r0 = Movement.ApplyWobble(dir, 0f, g);
            Vector3 r1 = Movement.ApplyWobble(dir, 0.5f, g);
            Vector3 r2 = Movement.ApplyWobble(dir, 1f, g);
            Assert.IsTrue(r0 != r1 || r1 != r2, "Wobble should vary with time");
        }
    }

    public class BehaviourTests
    {
        [Test]
        public void Decide_WhenHungryAndHasFood_ReturnsSeekFood()
        {
            var ctx = new BehaviourContext
            {
                Energy = 20f,
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodPosition = new Vector3(5f, 0f, 0f)
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.SeekFood, result.State);
            Assert.NotNull(result.TargetPosition);
            Assert.AreEqual(new Vector3(5f, 0f, 0f), result.TargetPosition.Value);
        }

        [Test]
        public void Decide_WhenNotHungryButHasThreat_ReturnsFlee()
        {
            var ctx = new BehaviourContext
            {
                Energy = 80f,
                MyPosition = Vector3.zero,
                HasThreat = true,
                ThreatPosition = new Vector3(2f, 0f, 0f)
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Flee, result.State);
            Assert.NotNull(result.TargetPosition);
            Assert.Less(result.TargetPosition.Value.x, 0f, "Flee direction should be away from threat");
        }

        [Test]
        public void Decide_WhenNoThreatButHasPrey_ReturnsAttack()
        {
            var ctx = new BehaviourContext
            {
                Energy = 50f,
                MyPosition = Vector3.zero,
                HasPrey = true,
                PreyPosition = new Vector3(3f, 0f, 3f)
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Attack, result.State);
            Assert.NotNull(result.TargetPosition);
            Assert.AreEqual(new Vector3(3f, 0f, 3f), result.TargetPosition.Value);
        }

        [Test]
        public void Decide_WhenHighEnergyAndHasPrey_ReturnsReproduce()
        {
            var ctx = new BehaviourContext
            {
                Energy = ReproductionSystem.GetReproductionThreshold() + 5f,
                MyPosition = Vector3.zero,
                HasPrey = true,
                PreyPosition = new Vector3(3f, 0f, 3f)
            };

            var result = CellBehaviour.Decide(ctx);

            Assert.AreEqual(BehaviourState.Reproduce, result.State);
            Assert.IsNull(result.TargetPosition);
        }

        [Test]
        public void Decide_WhenNothingNearby_ReturnsWander()
        {
            var ctx = new BehaviourContext
            {
                Energy = 50f,
                MyPosition = Vector3.zero,
                HasNearestFood = false,
                HasThreat = false,
                HasPrey = false
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Wander, result.State);
            Assert.IsNull(result.TargetPosition);
        }

        [Test]
        public void Decide_WhenHighEnergyAndNothingNearby_ReturnsReproduce()
        {
            float threshold = ReproductionSystem.GetReproductionThreshold();
            var ctx = new BehaviourContext
            {
                Energy = threshold + 10f,
                MyPosition = Vector3.zero,
                HasNearestFood = false,
                HasThreat = false,
                HasPrey = false
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Reproduce, result.State);
            Assert.IsNull(result.TargetPosition);
        }

        [Test]
        public void Decide_ThreatTakesPriorityOverPrey()
        {
            var ctx = new BehaviourContext
            {
                Energy = 50f,
                MyPosition = Vector3.zero,
                HasThreat = true,
                ThreatPosition = new Vector3(1f, 0f, 0f),
                HasPrey = true,
                PreyPosition = new Vector3(10f, 0f, 0f)
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Flee, result.State);
        }

        [Test]
        public void Decide_HungryButNotCritical_WithThreatAndFood_ReturnsFlee()
        {
            float low = CellBehaviour.GetLowEnergyThreshold();
            var ctx = new BehaviourContext
            {
                Energy = low * 0.8f, // hungry, but not critically hungry
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodPosition = new Vector3(1f, 0f, 0f),
                NearestFoodDistance = 1f,
                HasThreat = true,
                ThreatPosition = new Vector3(3f, 0f, 0f),
                ThreatDistance = 3f
            };

            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Flee, result.State);
        }

        [Test]
        public void Decide_HungryWithFoodTakesPriorityOverThreat()
        {
            var ctx = new BehaviourContext
            {
                Energy = 10f,
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodPosition = new Vector3(1f, 0f, 0f),
                HasThreat = true,
                ThreatPosition = new Vector3(2f, 0f, 0f)
            };
            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.SeekFood, result.State);
        }

        [Test]
        public void Decide_CriticallyHungry_ImmediateThreat_ReturnsFlee()
        {
            float low = CellBehaviour.GetLowEnergyThreshold();
            var ctx = new BehaviourContext
            {
                Energy = low * 0.3f, // critically hungry
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodPosition = new Vector3(1f, 0f, 0f),
                NearestFoodDistance = 1f,
                HasThreat = true,
                ThreatPosition = new Vector3(1f, 0f, 0f),
                ThreatDistance = 1f // immediate threat should override starvation foraging
            };

            var result = CellBehaviour.Decide(ctx);
            Assert.AreEqual(BehaviourState.Flee, result.State);
        }

        [Test]
        public void ShouldFleeUnderPredationRisk_CriticallyHungryWithSaferFood_ReturnsFalse()
        {
            float low = CellBehaviour.GetLowEnergyThreshold();
            var ctx = new BehaviourContext
            {
                Energy = low * 0.3f,
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodDistance = 1f,
                HasThreat = true,
                ThreatDistance = 4f
            };

            Assert.IsFalse(CellBehaviour.ShouldFleeUnderPredationRisk(ctx));
        }

        [Test]
        public void ScoreDecisions_ImmediateThreatBoostsFleeOverSeekFood()
        {
            float low = CellBehaviour.GetLowEnergyThreshold();
            var ctx = new BehaviourContext
            {
                Energy = low * 0.3f,
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodPosition = new Vector3(1f, 0f, 0f),
                NearestFoodDistance = 1f,
                HasThreat = true,
                ThreatPosition = new Vector3(1f, 0f, 0f),
                ThreatDistance = 1f
            };

            var scores = CellBehaviour.ScoreDecisions(ctx);
            Assert.Greater(scores.FleeUtility, scores.SeekFoodUtility);
        }

        [Test]
        public void ScoreDecisions_DistantThreatAndCloseFoodBoostsSeekFood()
        {
            float low = CellBehaviour.GetLowEnergyThreshold();
            var ctx = new BehaviourContext
            {
                Energy = low * 0.3f,
                MyPosition = Vector3.zero,
                HasNearestFood = true,
                NearestFoodPosition = new Vector3(0.8f, 0f, 0f),
                NearestFoodDistance = 0.8f,
                HasThreat = true,
                ThreatPosition = new Vector3(5f, 0f, 0f),
                ThreatDistance = 5f
            };

            var scores = CellBehaviour.ScoreDecisions(ctx);
            Assert.Greater(scores.SeekFoodUtility, scores.FleeUtility);
        }

        [Test]
        public void EvaluateThreatRisk_DecreasesWithDistance()
        {
            float near = CellBehaviour.EvaluateThreatRisk(1f);
            float mid = CellBehaviour.EvaluateThreatRisk(4f);
            float far = CellBehaviour.EvaluateThreatRisk(20f);

            Assert.GreaterOrEqual(near, mid);
            Assert.GreaterOrEqual(mid, far);
        }

        [Test]
        public void GetLowEnergyThreshold_ReturnsPositiveValue()
        {
            float t = CellBehaviour.GetLowEnergyThreshold();
            Assert.Greater(t, 0f);
        }
    }

    /// <summary>Step 2 integration: multiple organisms moving and reacting in one tick.</summary>
    public class MovementBehaviourIntegrationTests
    {
        [Test]
        public void MultipleOrganisms_OneTick_ReactAndMove()
        {
            var genome = new Genome { Speed = 2f, Flagella = 1f };
            var a = Organism.Create(new Vector3(0f, 0f, 0f), genome);
            var b = Organism.Create(new Vector3(5f, 0f, 0f), genome);
            var c = Organism.Create(new Vector3(10f, 0f, 0f), genome);
            float dt = 0.2f;
            float time = 1f;

            // A: threat nearby (B) -> Flee
            var ctxA = new BehaviourContext
            {
                Energy = 50f,
                MyPosition = a.transform.position,
                HasThreat = true,
                ThreatPosition = b.transform.position
            };
            var resA = CellBehaviour.Decide(ctxA);
            Vector3 dirA = CellBehaviour.GetMoveDirection(resA, a.transform.position);
            dirA = Movement.ApplyWobble(dirA, time, a.Genome);
            a.Move(dirA, dt);

            // B: prey nearby (C) -> Attack
            var ctxB = new BehaviourContext
            {
                Energy = 50f,
                MyPosition = b.transform.position,
                HasPrey = true,
                PreyPosition = c.transform.position
            };
            var resB = CellBehaviour.Decide(ctxB);
            Vector3 dirB = CellBehaviour.GetMoveDirection(resB, b.transform.position);
            dirB = Movement.ApplyWobble(dirB, time + 0.1f, b.Genome);
            b.Move(dirB, dt);

            // C: nothing -> Wander (fixed direction so it moves)
            var ctxC = new BehaviourContext { Energy = 50f, MyPosition = c.transform.position };
            var resC = CellBehaviour.Decide(ctxC);
            Vector3 dirC = CellBehaviour.GetMoveDirection(resC, c.transform.position, Vector3.left);
            dirC = Movement.ApplyWobble(dirC, time + 0.2f, c.Genome);
            c.Move(dirC, dt);

            Assert.Less(a.transform.position.x, 0f, "A fleeing should move away from B (negative X)");
            Assert.Greater(b.transform.position.x, 5f, "B attacking should move toward C (positive X)");
            Assert.Less(c.transform.position.x, 10f, "C wandering left should decrease X");

            UnityEngine.Object.DestroyImmediate(a.gameObject);
            UnityEngine.Object.DestroyImmediate(b.gameObject);
            UnityEngine.Object.DestroyImmediate(c.gameObject);
        }
    }
}
