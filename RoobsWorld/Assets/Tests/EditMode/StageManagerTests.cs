using NUnit.Framework;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class StageManagerTests
    {
        private static readonly GameStageType AlternateTestStage = (GameStageType)777;

        private sealed class MockStage : IGameStage
        {
            public GameStageType StageType { get; }
            public bool OnEnterCalled { get; private set; }
            public bool OnExitCalled { get; private set; }
            public int UpdateCount { get; private set; }

            public MockStage(GameStageType stageType)
            {
                StageType = stageType;
            }

            public void OnEnter() => OnEnterCalled = true;
            public void OnExit() => OnExitCalled = true;
            public void OnUpdate() => UpdateCount++;
        }

        [Test]
        public void CurrentStageType_IsNone_WhenNoStageSet()
        {
            Assert.AreEqual(GameStageType.None, StageManager.Instance.CurrentStageType);
        }

        [Test]
        public void IsTransitioning_IsFalse_Initially()
        {
            Assert.IsFalse(StageManager.Instance.IsTransitioning);
        }

        [Test]
        public void ShouldRunStageUpdate_ReturnsFalse_WhenPaused()
        {
            Assert.IsFalse(StageManager.ShouldRunStageUpdate(
                isTransitioning: false,
                hasGameManager: true,
                isPaused: true));
        }

        [Test]
        public void ShouldRunStageUpdate_AllowsUpdate_WhenNoPauseManagerExists()
        {
            Assert.IsTrue(StageManager.ShouldRunStageUpdate(
                isTransitioning: false,
                hasGameManager: false,
                isPaused: false));
        }

        [Test]
        public void RegisterStage_AndSetStageImmediate_SetsCurrentStage()
        {
            var stage = new MockStage(GameStageType.Cell);
            StageManager.Instance.RegisterStage(stage);
            StageManager.Instance.SetStageImmediate(stage);

            Assert.AreEqual(GameStageType.Cell, StageManager.Instance.CurrentStageType);
            Assert.IsTrue(stage.OnEnterCalled);
        }

        [Test]
        public void SetStageImmediate_CallsOnExit_OnPreviousStage()
        {
            var first = new MockStage(GameStageType.Cell);
            var second = new MockStage(AlternateTestStage);
            StageManager.Instance.RegisterStage(first);
            StageManager.Instance.RegisterStage(second);
            StageManager.Instance.SetStageImmediate(first);
            Assert.IsTrue(first.OnEnterCalled);
            StageManager.Instance.SetStageImmediate(second);
            Assert.IsTrue(first.OnExitCalled);
            Assert.IsTrue(second.OnEnterCalled);
        }

        [Test]
        public void UnregisterStage_RemovesStage()
        {
            var alternate = new MockStage(AlternateTestStage);
            var cell = new MockStage(GameStageType.Cell);
            StageManager.Instance.RegisterStage(alternate);
            StageManager.Instance.RegisterStage(cell);
            StageManager.Instance.SetStageImmediate(alternate);
            Assert.AreEqual(AlternateTestStage, StageManager.Instance.CurrentStageType);

            StageManager.Instance.UnregisterStage(AlternateTestStage);
            StageManager.Instance.SetStageImmediate(cell);
            Assert.AreEqual(GameStageType.Cell, StageManager.Instance.CurrentStageType);
        }
    }
}
