using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class FoodManagerAddFoodAtTests
    {
        private GameObject _go;
        private FoodManager _fm;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("FoodManagerTest");
            _fm = _go.AddComponent<FoodManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void AddFoodAt_IncrementsFoodCount()
        {
            Assert.AreEqual(0, _fm.FoodCount);
            _fm.AddFoodAt(new Vector3(1, 0, 0), FoodType.Plant, 5f);
            Assert.AreEqual(1, _fm.FoodCount);
            _fm.AddFoodAt(new Vector3(2, 0, 0), FoodType.Meat, 10f);
            Assert.AreEqual(2, _fm.FoodCount);
        }

        [Test]
        public void AddFoodAt_GetNearestFoodReturnsAddedFood()
        {
            _fm.AddFoodAt(new Vector3(3, 0, 4), FoodType.Plant, 7f);
            var nearest = _fm.GetNearestFood(new Vector3(0, 0, 0), 10f);
            Assert.IsTrue(nearest.HasValue);
            Assert.AreEqual(7f, nearest.Value.Nutrition);
            Assert.AreEqual(FoodType.Plant, nearest.Value.Type);
        }

        [Test]
        public void AddFoodAt_ConsumeFoodRemovesFood()
        {
            int id = _fm.AddFoodAt(Vector3.zero, FoodType.Meat, 8f);
            Assert.AreEqual(1, _fm.FoodCount);
            bool consumed = _fm.ConsumeFood(id);
            Assert.IsTrue(consumed);
            Assert.AreEqual(0, _fm.FoodCount);
        }

        [Test]
        public void AddOrUpdateNetworkFood_PreservesHostIdAndUpdatesExistingFood()
        {
            int id = _fm.AddOrUpdateNetworkFood(42, new Vector3(1f, 0f, 0f), FoodType.Meat, 8f);
            Assert.AreEqual(42, id);
            Assert.IsTrue(_fm.HasFood(42));
            Assert.AreEqual(1, _fm.FoodCount);

            _fm.AddOrUpdateNetworkFood(42, new Vector3(4f, 0f, 0f), FoodType.Toxin, 3f);
            Assert.AreEqual(1, _fm.FoodCount);

            var nearest = _fm.GetNearestFood(new Vector3(4f, 0f, 0f), 1f);
            Assert.IsTrue(nearest.HasValue);
            Assert.AreEqual(42, nearest.Value.Id);
            Assert.AreEqual(FoodType.Toxin, nearest.Value.Type);
            Assert.AreEqual(4f, nearest.Value.Position.x, 0.001f);
        }

        [Test]
        public void CopyFoodIds_ReturnsManagedIds()
        {
            int first = _fm.AddFoodAt(Vector3.zero, FoodType.Plant, 5f);
            _fm.AddOrUpdateNetworkFood(88, Vector3.right, FoodType.Meat, 8f);
            var ids = new System.Collections.Generic.List<int>();

            int count = _fm.CopyFoodIds(ids);

            Assert.AreEqual(2, count);
            CollectionAssert.Contains(ids, first);
            CollectionAssert.Contains(ids, 88);
        }
    }

    public class FoodTrackerTests
    {
        [Test]
        public void Add_IncrementsCount()
        {
            var t = new FoodTracker();
            Assert.AreEqual(0, t.Count);
            t.Add(Vector3.zero, FoodType.Plant, 5f);
            Assert.AreEqual(1, t.Count);
            t.Add(new Vector3(1, 0, 0), FoodType.Meat, 10f);
            Assert.AreEqual(2, t.Count);
        }

        [Test]
        public void GetNearest_Empty_ReturnsNull()
        {
            var t = new FoodTracker();
            var r = t.GetNearest(Vector3.zero, 10f);
            Assert.IsFalse(r.HasValue);
        }

        [Test]
        public void GetNearest_ReturnsOnlyWithinMaxDistance()
        {
            var t = new FoodTracker();
            t.Add(new Vector3(100, 0, 0), FoodType.Plant, 5f);
            var r = t.GetNearest(Vector3.zero, 10f);
            Assert.IsFalse(r.HasValue);
        }

        [Test]
        public void GetNearest_ReturnsNearestWithinRange()
        {
            var t = new FoodTracker();
            t.Add(new Vector3(5, 0, 0), FoodType.Plant, 5f);
            t.Add(new Vector3(2, 0, 0), FoodType.Meat, 10f);
            t.Add(new Vector3(8, 0, 0), FoodType.Plant, 3f);
            var r = t.GetNearest(Vector3.zero, 10f);
            Assert.IsTrue(r.HasValue);
            Assert.AreEqual(2f, r.Value.Position.x, 0.001f);
            Assert.AreEqual(10f, r.Value.Nutrition);
            Assert.AreEqual(FoodType.Meat, r.Value.Type);
        }

        [Test]
        public void Remove_RemovesEntry()
        {
            var t = new FoodTracker();
            int id = t.Add(new Vector3(1, 0, 0), FoodType.Plant, 5f);
            Assert.AreEqual(1, t.Count);
            bool removed = t.Remove(id);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, t.Count);
            Assert.IsFalse(t.GetNearest(Vector3.zero, 10f).HasValue);
        }

        [Test]
        public void Remove_UnknownId_ReturnsFalse()
        {
            var t = new FoodTracker();
            t.Add(Vector3.zero, FoodType.Plant, 5f);
            bool removed = t.Remove(999);
            Assert.IsFalse(removed);
            Assert.AreEqual(1, t.Count);
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var t = new FoodTracker();
            t.Add(Vector3.zero, FoodType.Plant, 5f);
            t.Add(new Vector3(1, 0, 0), FoodType.Meat, 10f);
            t.Clear();
            Assert.AreEqual(0, t.Count);
            Assert.IsFalse(t.GetNearest(Vector3.zero, 10f).HasValue);
        }

        [Test]
        public void GetNearest_IgnoresY()
        {
            var t = new FoodTracker();
            t.Add(new Vector3(0, 100, 0), FoodType.Plant, 5f);
            var r = t.GetNearest(new Vector3(0, 0, 0), 10f);
            Assert.IsTrue(r.HasValue);
            Assert.AreEqual(5f, r.Value.Nutrition);
        }

        [Test]
        public void Get_ReturnsEntry_WhenIdExists()
        {
            var t = new FoodTracker();
            int id = t.Add(new Vector3(3, 0, 4), FoodType.Meat, 12f);
            var e = t.Get(id);
            Assert.IsTrue(e.HasValue);
            Assert.AreEqual(3f, e.Value.Position.x, 0.001f);
            Assert.AreEqual(4f, e.Value.Position.z, 0.001f);
            Assert.AreEqual(FoodType.Meat, e.Value.Type);
            Assert.AreEqual(12f, e.Value.Nutrition);
        }

        [Test]
        public void Get_ReturnsNull_WhenIdMissing()
        {
            var t = new FoodTracker();
            t.Add(Vector3.zero, FoodType.Plant, 5f);
            Assert.IsFalse(t.Get(999).HasValue);
        }

        [Test]
        public void AddWithId_UsesProvidedIdAndAdvancesGeneratedIds()
        {
            var t = new FoodTracker();
            int hostId = t.AddWithId(77, new Vector3(2f, 0f, 0f), FoodType.Meat, 9f);
            int generatedId = t.Add(Vector3.zero, FoodType.Plant, 5f);

            Assert.AreEqual(77, hostId);
            Assert.AreEqual(78, generatedId);
            Assert.IsTrue(t.Contains(77));
            Assert.IsTrue(t.Contains(78));
        }

        [Test]
        public void AddWithId_UpdatesExistingEntry()
        {
            var t = new FoodTracker();
            t.AddWithId(7, Vector3.zero, FoodType.Plant, 5f);
            t.AddWithId(7, new Vector3(3f, 0f, 0f), FoodType.Toxin, 2f);

            Assert.AreEqual(1, t.Count);
            var entry = t.Get(7);
            Assert.IsTrue(entry.HasValue);
            Assert.AreEqual(3f, entry.Value.Position.x, 0.001f);
            Assert.AreEqual(FoodType.Toxin, entry.Value.Type);
            Assert.AreEqual(2f, entry.Value.Nutrition, 0.001f);
        }
    }
}
