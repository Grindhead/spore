using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Tests for the territorial-predator extension of PredatorCell.
    /// Validates SetTerritory, IsTerritorial, accessor values, and the
    /// AlgaeForest spawn band used in CellStage.SpawnPredators().
    /// Implements gameplayLoopAndEvolutionDesign §8 "Territorial" behaviour.
    /// </summary>
    public class TerritorialPredatorTests
    {
        // Helper: create a minimal PredatorCell (needs Rigidbody from RequireComponent).
        private static (GameObject go, PredatorCell pred) MakePredator()
        {
            var go = new GameObject("Predator_Test");
            go.AddComponent<Rigidbody>();
            var pred = go.AddComponent<PredatorCell>();
            return (go, pred);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any GameObjects created by tests
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
                Object.DestroyImmediate(go);
        }

        // ---- Default state ----

        [Test]
        public void IsTerritorial_DefaultsFalse()
        {
            var (go, pred) = MakePredator();
            Assert.IsFalse(pred.IsTerritorial);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TerritoryOrigin_DefaultsToZero()
        {
            var (go, pred) = MakePredator();
            Assert.AreEqual(Vector3.zero, pred.TerritoryOrigin);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TerritoryRadius_DefaultsToZero()
        {
            var (go, pred) = MakePredator();
            Assert.AreEqual(0f, pred.TerritoryRadius);
            Object.DestroyImmediate(go);
        }

        // ---- SetTerritory ----

        [Test]
        public void SetTerritory_SetsIsTerritorialTrue()
        {
            var (go, pred) = MakePredator();
            pred.SetTerritory(new Vector3(30f, 0f, 10f), 12f);
            Assert.IsTrue(pred.IsTerritorial);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetTerritory_StoresTerritoryOriginXZ()
        {
            var (go, pred) = MakePredator();
            var origin = new Vector3(25f, 5f, -15f);  // Y should be zeroed out
            pred.SetTerritory(origin, 10f);
            Assert.AreEqual(0f, pred.TerritoryOrigin.y, "Territory origin Y must always be 0");
            Assert.AreEqual(25f, pred.TerritoryOrigin.x, 0.001f);
            Assert.AreEqual(-15f, pred.TerritoryOrigin.z, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetTerritory_StoresTerritoryRadius()
        {
            var (go, pred) = MakePredator();
            pred.SetTerritory(Vector3.zero, 12f);
            Assert.AreEqual(12f, pred.TerritoryRadius, 0.001f);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetTerritory_ClampsNegativeRadiusToOne()
        {
            var (go, pred) = MakePredator();
            pred.SetTerritory(Vector3.zero, -5f);
            Assert.GreaterOrEqual(pred.TerritoryRadius, 1f, "Radius must be at least 1 even when a negative value is passed");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetTerritory_ZeroRadiusClampsToOne()
        {
            var (go, pred) = MakePredator();
            pred.SetTerritory(Vector3.zero, 0f);
            Assert.GreaterOrEqual(pred.TerritoryRadius, 1f);
            Object.DestroyImmediate(go);
        }

        // ---- Spawn-band geometry ----

        [Test]
        public void AlgaeForestBand_ContainsSpawnFractions()
        {
            // CellStage spawns territorial predators between 0.62 and 0.88 of arenaRadius.
            // Those fractions must fall within the AlgaeForest band (0.60–0.90 of arenaRadius).
            const float innerFrac = 0.62f;
            const float outerFrac = 0.88f;
            const float algaeMin  = RoobsWorld.Core.BalanceConstants.CellBiomeOpenWaterMaxRadiusFraction;  // 0.60
            const float algaeMax  = RoobsWorld.Core.BalanceConstants.CellBiomeAlgaeForestMaxRadiusFraction; // 0.90

            Assert.GreaterOrEqual(innerFrac, algaeMin, "Inner spawn fraction must be inside AlgaeForest band");
            Assert.LessOrEqual(outerFrac, algaeMax, "Outer spawn fraction must be inside AlgaeForest band");
        }

        [Test]
        public void TerritorialPredatorRadius_IsLargerThanPerceptionRadius_NotRequired_ButReasonable()
        {
            // The territory radius (12f) should be larger than a typical patrol waypoint arrival distance
            // but not so large it covers the whole arena (arena radius = 50f).
            const float territoryRadius = CellStage.TerritorialPredatorRadius;
            Assert.Greater(territoryRadius, 5f, "Territory radius should be large enough to form a meaningful patrol zone");
            Assert.Less(territoryRadius, 30f, "Territory radius must not cover the whole arena");
        }

        [Test]
        public void ShouldRefreshPlayerReference_ReturnsTrue_WhenNoPlayerOrInactiveOrDead()
        {
            bool noPlayer = PredatorCell.ShouldRefreshPlayerReference(
                refreshTimerRemaining: 0.2f,
                hasPlayer: false,
                playerIsActive: false,
                playerIsDead: false);
            bool inactive = PredatorCell.ShouldRefreshPlayerReference(
                refreshTimerRemaining: 0.2f,
                hasPlayer: true,
                playerIsActive: false,
                playerIsDead: false);
            bool dead = PredatorCell.ShouldRefreshPlayerReference(
                refreshTimerRemaining: 0.2f,
                hasPlayer: true,
                playerIsActive: true,
                playerIsDead: true);

            Assert.IsTrue(noPlayer);
            Assert.IsTrue(inactive);
            Assert.IsTrue(dead);
        }

        [Test]
        public void ShouldRefreshPlayerReference_UsesTimerForValidPlayer()
        {
            bool notYet = PredatorCell.ShouldRefreshPlayerReference(
                refreshTimerRemaining: 0.1f,
                hasPlayer: true,
                playerIsActive: true,
                playerIsDead: false);
            bool timerElapsed = PredatorCell.ShouldRefreshPlayerReference(
                refreshTimerRemaining: 0f,
                hasPlayer: true,
                playerIsActive: true,
                playerIsDead: false);

            Assert.IsFalse(notYet);
            Assert.IsTrue(timerElapsed);
        }

        [Test]
        public void IsAnyPredatorWithinRadiusSqr_ReturnsTrue_WhenPredatorNearby()
        {
            var (goA, predatorA) = MakePredator();
            var (goB, predatorB) = MakePredator();
            goA.transform.position = Vector3.zero;
            goB.transform.position = new Vector3(10f, 0f, 0f);

            var predators = new List<PredatorCell> { predatorA, predatorB };
            bool anyNearby = CellStage.IsAnyPredatorWithinRadiusSqr(
                predators,
                playerPosition: new Vector3(1f, 0f, 0f),
                radiusSqr: 4f);

            Assert.IsTrue(anyNearby);
            Object.DestroyImmediate(goA);
            Object.DestroyImmediate(goB);
        }

        [Test]
        public void IsAnyPredatorWithinRadiusSqr_ReturnsFalse_WhenNoneNearbyOrRadiusInvalid()
        {
            var (goA, predatorA) = MakePredator();
            goA.transform.position = new Vector3(30f, 0f, 0f);

            var predators = new List<PredatorCell> { predatorA };
            bool farAway = CellStage.IsAnyPredatorWithinRadiusSqr(
                predators,
                playerPosition: Vector3.zero,
                radiusSqr: 16f);
            bool invalidRadius = CellStage.IsAnyPredatorWithinRadiusSqr(
                predators,
                playerPosition: Vector3.zero,
                radiusSqr: 0f);

            Assert.IsFalse(farAway);
            Assert.IsFalse(invalidRadius);
            Object.DestroyImmediate(goA);
        }
    }
}
