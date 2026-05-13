using System.Reflection;
using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class CellBackgroundFishTests
    {
        private const BindingFlags NonPublicInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            var fishRoot = GameObject.Find("CellBackgroundFish");
            if (fishRoot != null)
                Object.DestroyImmediate(fishRoot);

            var testRoot = GameObject.Find("CellBackgroundFish_Test");
            if (testRoot != null)
                Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Spawn_CreatesNonInteractiveScaleSilhouettes()
        {
            var fish = CellBackgroundFish.Spawn(Vector3.zero, CellBackgroundFish.DefaultArenaRadius);

            Assert.AreEqual(CellBackgroundFish.DefaultCount, fish.Count);
            Assert.AreEqual(CellBackgroundFish.DefaultArenaRadius, fish.ArenaRadius);
            Assert.IsNull(fish.GetComponentInChildren<Collider>(),
                "Background fish should be visual scale cues only, with no gameplay colliders.");
        }

        [Test]
        public void Update_WhenNotInitialised_DoesNotThrow()
        {
            var go = new GameObject("CellBackgroundFish_Test");
            var fish = go.AddComponent<CellBackgroundFish>();
            MethodInfo update = typeof(CellBackgroundFish).GetMethod("Update", NonPublicInstance);

            Assert.IsNotNull(update, "Expected private Update() method to exist.");
            Assert.DoesNotThrow(() => update.Invoke(fish, null));
        }
    }
}
