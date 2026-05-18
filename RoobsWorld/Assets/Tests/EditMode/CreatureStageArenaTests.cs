using System.Linq;
using NUnit.Framework;
using RoobsWorld.Multiplayer;
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

        [Test]
        public void ApplyMapDefinition_UsesServerSeedAndLandmarks()
        {
            var root = new GameObject("CreatureArenaTestsRoot_Map");
            var arena = root.AddComponent<CreatureStageArena>();
            var map = new SessionMapDefinition(
                SessionMapDefinition.CreatureMode,
                SessionMapDefinition.CurrentVersion,
                424242,
                36f,
                32,
                6f,
                0.04f,
                new[] { new SessionMapLandmark("HighGround", 3f, -5f, 7f, 1f, 6f) });

            arena.ApplyMapDefinition(map);
            arena.EnsureBuilt();

            Assert.AreEqual(424242, arena.IslandSeed);
            Assert.AreEqual(36f, arena.ArenaRadius, 0.001f);
            Assert.AreEqual(1, arena.ConfiguredLandmarkCount);
            Transform landmarks = root.transform.Find("ArenaLandmarks");
            Assert.NotNull(landmarks);
            Assert.AreEqual(1, landmarks.childCount);
            Assert.AreEqual("HighGround_0", landmarks.GetChild(0).name);

            Object.DestroyImmediate(root);
        }
    }
}
