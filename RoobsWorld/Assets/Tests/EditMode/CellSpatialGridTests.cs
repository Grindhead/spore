using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class CellSpatialGridTests
    {
        [SetUp]
        public void SetUp()
        {
            // Grid centered at origin, halfSize 20, cellSize 5
        }

        [Test]
        public void Register_And_GetInRadius_ReturnsEntry()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(2, 0, 0), CellSpatialGrid.Category.Food);
            var list = grid.GetInRadius(Vector3.zero, 10f, CellSpatialGrid.Category.Food);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0].Id);
            Assert.AreEqual(2f, list[0].Position.x, 0.001f);
        }

        [Test]
        public void GetInRadius_OutsideRadius_ReturnsEmpty()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(50, 0, 0), CellSpatialGrid.Category.Food);
            var list = grid.GetInRadius(Vector3.zero, 10f);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void GetInRadius_FilterByCategory_ExcludesOther()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(1, 0, 0), CellSpatialGrid.Category.Food);
            grid.Register(2, new Vector3(2, 0, 0), CellSpatialGrid.Category.Organism);
            var list = grid.GetInRadius(Vector3.zero, 10f, CellSpatialGrid.Category.Food);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list[0].Id);
        }

        [Test]
        public void Unregister_RemovesEntry()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(1, 0, 0), CellSpatialGrid.Category.Food);
            Assert.AreEqual(1, grid.Count);
            bool removed = grid.Unregister(1);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, grid.Count);
            Assert.AreEqual(0, grid.GetInRadius(Vector3.zero, 10f).Count);
        }

        [Test]
        public void Unregister_UnknownId_ReturnsFalse()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, Vector3.zero, CellSpatialGrid.Category.Food);
            Assert.IsFalse(grid.Unregister(999));
            Assert.AreEqual(1, grid.Count);
        }

        [Test]
        public void UpdatePosition_MovesEntry()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(1, 0, 0), CellSpatialGrid.Category.Food);
            grid.UpdatePosition(1, new Vector3(15, 0, 0));
            var list = grid.GetInRadius(new Vector3(15, 0, 0), 2f);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(15f, list[0].Position.x, 0.001f);
            var listNearOrigin = grid.GetInRadius(Vector3.zero, 10f);
            Assert.AreEqual(0, listNearOrigin.Count);
        }

        [Test]
        public void GetNearestInRadius_ReturnsNearest()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(5, 0, 0), CellSpatialGrid.Category.Food);
            grid.Register(2, new Vector3(2, 0, 0), CellSpatialGrid.Category.Food);
            grid.Register(3, new Vector3(8, 0, 0), CellSpatialGrid.Category.Food);
            var nearest = grid.GetNearestInRadius(Vector3.zero, 10f, CellSpatialGrid.Category.Food);
            Assert.IsTrue(nearest.HasValue);
            Assert.AreEqual(2, nearest.Value.Id);
            Assert.AreEqual(2f, nearest.Value.Position.x, 0.001f);
        }

        [Test]
        public void GetNearestInRadius_NoneInRadius_ReturnsNull()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(100, 0, 0), CellSpatialGrid.Category.Food);
            var nearest = grid.GetNearestInRadius(Vector3.zero, 10f);
            Assert.IsFalse(nearest.HasValue);
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, Vector3.zero, CellSpatialGrid.Category.Food);
            grid.Register(2, Vector3.one, CellSpatialGrid.Category.Organism);
            grid.Clear();
            Assert.AreEqual(0, grid.Count);
            Assert.AreEqual(0, grid.GetInRadius(Vector3.zero, 100f).Count);
        }

        [Test]
        public void Register_SameIdTwice_OverwritesPosition()
        {
            var grid = new CellSpatialGrid(Vector3.zero, 20f, 5f);
            grid.Register(1, new Vector3(1, 0, 0), CellSpatialGrid.Category.Food);
            grid.Register(1, new Vector3(12, 0, 0), CellSpatialGrid.Category.Food);
            Assert.AreEqual(1, grid.Count);
            var list = grid.GetInRadius(new Vector3(12, 0, 0), 2f);
            Assert.AreEqual(1, list.Count);
        }
    }
}
