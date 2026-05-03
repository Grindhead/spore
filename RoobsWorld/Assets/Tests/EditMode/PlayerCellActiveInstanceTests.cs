using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class PlayerCellActiveInstanceTests
    {
        [SetUp]
        [TearDown]
        public void CleanupPlayers()
        {
            var players = Object.FindObjectsByType<PlayerCell>(FindObjectsInactive.Include);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                    Object.DestroyImmediate(players[i].gameObject);
            }
        }

        [Test]
        public void TryGetActiveInstance_ReturnsFalse_WhenNoPlayerExists()
        {
            bool found = PlayerCell.TryGetActiveInstance(out var player);

            Assert.IsFalse(found);
            Assert.IsNull(player);
        }

        [Test]
        public void TryGetActiveInstance_ReturnsTrue_WhenPlayerEnabled()
        {
            var go = new GameObject("PlayerCell_Test");
            var player = go.AddComponent<PlayerCell>();

            bool found = PlayerCell.TryGetActiveInstance(out var active);

            Assert.IsTrue(found);
            Assert.AreEqual(player, active);
        }

        [Test]
        public void TryGetActiveInstance_ReturnsFalse_WhenPlayerDisabled()
        {
            var go = new GameObject("PlayerCell_Test");
            go.AddComponent<PlayerCell>();
            go.SetActive(false);

            bool found = PlayerCell.TryGetActiveInstance(out var active);

            Assert.IsFalse(found);
            Assert.IsNull(active);
        }
    }
}
