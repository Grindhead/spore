using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class AchievementServiceTests
    {
        private GameObject _serviceObject;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            if (GameManager.HasInstance)
                Object.DestroyImmediate(GameManager.Instance.gameObject);

            var gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<GameManager>();

            _serviceObject = new GameObject("AchievementService");
            _serviceObject.AddComponent<AchievementService>();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            if (_serviceObject != null)
                Object.DestroyImmediate(_serviceObject);
            if (GameManager.HasInstance)
                Object.DestroyImmediate(GameManager.Instance.gameObject);
        }

        [Test]
        public void PlayerEscapedPredator_UnlocksLuckyEscape()
        {
            EventBus.Publish(new CellPlayerEscapedPredatorEvent());
            Assert.IsTrue(GameManager.Instance.Achievements.IsUnlocked("cell_survive_predator"));
        }

        [Test]
        public void CollectingAllUniqueParts_UnlocksFullyEquipped()
        {
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Flagella });
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Cilia });
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Flagella });
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Spike });

            Assert.IsFalse(GameManager.Instance.Achievements.IsUnlocked("cell_all_parts"));

            foreach (CellPartType partType in System.Enum.GetValues(typeof(CellPartType)))
                EventBus.Publish(new CellPartCollectedEvent { PartType = partType });

            Assert.IsTrue(GameManager.Instance.Achievements.IsUnlocked("cell_all_parts"));
        }

        [Test]
        public void EnteringCellStage_DoesNotUnlockCellEvolveAchievement()
        {
            EventBus.Publish(new StageTransitionEvent { FromStage = GameStageType.None, ToStage = GameStageType.Cell });
            Assert.IsFalse(GameManager.Instance.Achievements.IsUnlocked("cell_evolve"));
        }

        [Test]
        public void EnteringCellStage_ResetsPartCollectionProgress()
        {
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Flagella });
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Cilia });
            EventBus.Publish(new CellPartCollectedEvent { PartType = CellPartType.Spike });

            EventBus.Publish(new StageTransitionEvent { FromStage = GameStageType.None, ToStage = GameStageType.Cell });
            foreach (CellPartType partType in System.Enum.GetValues(typeof(CellPartType)))
            {
                if (partType != CellPartType.Flagella)
                    EventBus.Publish(new CellPartCollectedEvent { PartType = partType });
            }

            Assert.IsFalse(GameManager.Instance.Achievements.IsUnlocked("cell_all_parts"));
        }

        [Test]
        public void DefeatingRivalCreature_UnlocksFirstHunt()
        {
            EventBus.Publish(new CreatureDiedEvent
            {
                CreatureId = "rival-1",
                IsPlayer = false
            });

            Assert.IsTrue(GameManager.Instance.Achievements.IsUnlocked("creature_first_kill"));
        }
    }
}
