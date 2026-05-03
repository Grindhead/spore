using System.Linq;
using NUnit.Framework;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class CreatureStageArenaTests
    {
        [Test]
        public void EnsureBuilt_CreatesFloorAndWalls()
        {
            var root = new GameObject("CreatureArenaTestsRoot");
            var arena = root.AddComponent<CreatureStageArena>();

            arena.EnsureBuilt();

            Assert.NotNull(root.transform.Find("Floor"));
            Assert.NotNull(root.transform.Find("ArenaWalls"));
            Assert.AreEqual(4, root.transform.Find("ArenaWalls").childCount);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void EnsureBuilt_IsIdempotent()
        {
            var root = new GameObject("CreatureArenaTestsRoot_Idempotent");
            var arena = root.AddComponent<CreatureStageArena>();

            arena.EnsureBuilt();
            arena.EnsureBuilt();

            Assert.AreEqual(1, root.transform.Cast<Transform>().Count(t => t.name == "Floor"));
            Assert.AreEqual(1, root.transform.Cast<Transform>().Count(t => t.name == "ArenaWalls"));

            Object.DestroyImmediate(root);
        }
    }
}
