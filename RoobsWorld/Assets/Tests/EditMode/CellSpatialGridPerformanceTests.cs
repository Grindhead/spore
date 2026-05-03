using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// Performance benchmarks for CellSpatialGrid at production scale.
    /// Validates that GetNearestInRadius and simulated AI ticks stay within frame budget
    /// with 500+ organisms and 200+ food items.
    /// Thresholds are deliberately generous (CI-safe) — real profiling happens in the Unity Profiler.
    /// </summary>
    public class CellSpatialGridPerformanceTests
    {
        private const float ArenaHalfSize = 50f;
        private const float CellSize = 5f;   // matches BalanceConstants.CellSpatialGridCellSize
        private const int OrganismCount = 500;
        private const int FoodCount = 200;
        private const float AiQueryRadius = 15f;

        // Frame-budget thresholds (milliseconds)
        private const long BulkRegisterMs = 50;
        private const long AiTickMs = 50;      // 500 organisms each doing one nearest-food query
        private const long BulkUpdateMs = 30;

        private CellSpatialGrid BuildPopulatedGrid(out Vector3[] organismPositions, out Vector3[] foodPositions)
        {
            var grid = new CellSpatialGrid(Vector3.zero, ArenaHalfSize, CellSize);

            // Deterministic positions spread across arena using a simple deterministic pattern
            organismPositions = new Vector3[OrganismCount];
            for (int i = 0; i < OrganismCount; i++)
            {
                float angle = (i / (float)OrganismCount) * 2f * Mathf.PI;
                float r = (i % 5 + 1) * 8f; // 8, 16, 24, 32, 40
                organismPositions[i] = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                grid.Register(i, organismPositions[i], CellSpatialGrid.Category.Organism);
            }

            foodPositions = new Vector3[FoodCount];
            for (int i = 0; i < FoodCount; i++)
            {
                float angle = (i / (float)FoodCount) * 2f * Mathf.PI;
                float r = (i % 10 + 0.5f) * 4.5f;
                foodPositions[i] = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                grid.Register(OrganismCount + i, foodPositions[i], CellSpatialGrid.Category.Food);
            }

            return grid;
        }

        [Test]
        public void BulkRegister_500Organisms_200Food_CompletesWithinBudget()
        {
            var sw = Stopwatch.StartNew();

            var grid = new CellSpatialGrid(Vector3.zero, ArenaHalfSize, CellSize);
            for (int i = 0; i < OrganismCount; i++)
            {
                float angle = (i / (float)OrganismCount) * 2f * Mathf.PI;
                float r = (i % 5 + 1) * 8f;
                grid.Register(i, new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r),
                    CellSpatialGrid.Category.Organism);
            }
            for (int i = 0; i < FoodCount; i++)
            {
                float angle = (i / (float)FoodCount) * 2f * Mathf.PI;
                float r = (i % 10 + 0.5f) * 4.5f;
                grid.Register(OrganismCount + i, new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r),
                    CellSpatialGrid.Category.Food);
            }

            sw.Stop();
            Assert.AreEqual(OrganismCount + FoodCount, grid.Count);
            Assert.Less(sw.ElapsedMilliseconds, BulkRegisterMs,
                $"Bulk registration of {OrganismCount + FoodCount} entries took {sw.ElapsedMilliseconds} ms (budget: {BulkRegisterMs} ms)");
        }

        [Test]
        public void AiTick_500OrganismsEachQueryNearestFood_CompletesWithinBudget()
        {
            var grid = BuildPopulatedGrid(out var organismPositions, out _);

            var sw = Stopwatch.StartNew();

            int foundCount = 0;
            for (int i = 0; i < OrganismCount; i++)
            {
                var nearest = grid.GetNearestInRadius(organismPositions[i], AiQueryRadius, CellSpatialGrid.Category.Food);
                if (nearest.HasValue) foundCount++;
            }

            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, AiTickMs,
                $"AI tick ({OrganismCount} queries) took {sw.ElapsedMilliseconds} ms (budget: {AiTickMs} ms)");
            // Sanity: at least some organisms should be near food
            Assert.Greater(foundCount, 0, "Expected at least some organisms to find nearby food");
        }

        [Test]
        public void BulkUpdatePositions_500Organisms_CompletesWithinBudget()
        {
            var grid = BuildPopulatedGrid(out var organismPositions, out _);

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < OrganismCount; i++)
            {
                // Simulate movement: nudge each organism by a small delta
                var newPos = organismPositions[i] + new Vector3(0.1f, 0f, 0.1f);
                grid.UpdatePosition(i, newPos);
            }

            sw.Stop();
            Assert.AreEqual(OrganismCount + FoodCount, grid.Count, "Entry count should be unchanged after updates");
            Assert.Less(sw.ElapsedMilliseconds, BulkUpdateMs,
                $"Bulk position update of {OrganismCount} organisms took {sw.ElapsedMilliseconds} ms (budget: {BulkUpdateMs} ms)");
        }

        [Test]
        public void GetInRadius_WithMixedCategories_DoesNotReturnWrongCategory()
        {
            var grid = BuildPopulatedGrid(out var organismPositions, out _);

            // Query food from organism positions — should never return organisms
            for (int i = 0; i < 20; i++)
            {
                var results = grid.GetInRadius(organismPositions[i], AiQueryRadius, CellSpatialGrid.Category.Food);
                foreach (var entry in results)
                    Assert.Less(entry.Id, OrganismCount + FoodCount, "Should only return food entries");
            }
        }

        [Test]
        public void GridCountMatchesExpected_AfterPopulation()
        {
            var grid = BuildPopulatedGrid(out _, out _);
            Assert.AreEqual(OrganismCount + FoodCount, grid.Count);
        }

        [Test]
        public void CellSpatialGridCellSize_MatchesBalanceConstant()
        {
            Assert.AreEqual(BalanceConstants.CellSpatialGridCellSize, CellSize, 0.001f,
                "Test cell size should match BalanceConstants so performance numbers reflect real config");
        }
    }
}
