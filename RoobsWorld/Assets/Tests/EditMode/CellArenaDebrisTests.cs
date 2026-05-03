using System.Reflection;
using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class CellArenaDebrisTests
    {
        private const BindingFlags NonPublicInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [TearDown]
        public void TearDown()
        {
            var debrisRoot = GameObject.Find("CellArenaDebris");
            if (debrisRoot != null)
                Object.DestroyImmediate(debrisRoot);

            var testRoot = GameObject.Find("CellArenaDebris_Test");
            if (testRoot != null)
                Object.DestroyImmediate(testRoot);
        }

        [Test]
        public void Update_WhenNotInitialised_DoesNotThrow()
        {
            var go = new GameObject("CellArenaDebris_Test");
            var debris = go.AddComponent<CellArenaDebris>();
            MethodInfo update = typeof(CellArenaDebris).GetMethod("Update", NonPublicInstance);

            Assert.IsNotNull(update, "Expected private Update() method to exist.");
            Assert.DoesNotThrow(() => update.Invoke(debris, null));
        }
    }
}
