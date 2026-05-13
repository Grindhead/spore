using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Utils;
using System.Reflection;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class CellStageCollisionSetupTests
    {
        [Test]
        public void AddFoodAt_ConfiguresFoodCollidersAsTriggersImmediately()
        {
            var managerGo = new GameObject("FoodManager_Test");
            var manager = managerGo.AddComponent<FoodManager>();

            var before = Object.FindObjectsByType<FoodItem>(FindObjectsInactive.Include);
            manager.AddFoodAt(Vector3.zero, FoodType.Plant, 5f);
            var after = Object.FindObjectsByType<FoodItem>(FindObjectsInactive.Include);

            FoodItem spawnedFood = null;
            for (int i = 0; i < after.Length; i++)
            {
                bool existedBefore = false;
                for (int j = 0; j < before.Length; j++)
                {
                    if (before[j] == after[i])
                    {
                        existedBefore = true;
                        break;
                    }
                }

                if (!existedBefore)
                {
                    spawnedFood = after[i];
                    break;
                }
            }

            Assert.NotNull(spawnedFood, "Expected FoodManager.AddFoodAt to create a FoodItem.");
            var colliders = spawnedFood.GetComponentsInChildren<Collider>(true);
            Assert.Greater(colliders.Length, 0, "Spawned food should have at least one collider.");
            for (int i = 0; i < colliders.Length; i++)
            {
                Assert.IsTrue(colliders[i].enabled, "Food collider should be enabled.");
                Assert.IsTrue(colliders[i].isTrigger, "Food collider should be a trigger.");
            }

            Object.DestroyImmediate(spawnedFood.gameObject);
            Object.DestroyImmediate(managerGo);
        }

        [Test]
        public void CellPickup_EnsureTriggerColliders_MarksAllCollidersAsTriggers()
        {
            var pickupGo = ModelFactory.CreateCellPickup(Vector3.zero, Color.cyan);
            var pickup = pickupGo.AddComponent<CellPartPickup>();

            pickup.EnsureTriggerColliders();

            var colliders = pickupGo.GetComponentsInChildren<Collider>(true);
            Assert.Greater(colliders.Length, 0, "Cell pickup should have at least one collider.");
            for (int i = 0; i < colliders.Length; i++)
            {
                Assert.IsTrue(colliders[i].enabled, "Pickup collider should be enabled.");
                Assert.IsTrue(colliders[i].isTrigger, "Pickup collider should be a trigger.");
            }

            Object.DestroyImmediate(pickupGo);
        }

        [Test]
        public void SpawnForegroundAlgae_CreatesCollectableFoodTriggers()
        {
            var stageGo = new GameObject("CellStage_Test");
            try
            {
                var stage = stageGo.AddComponent<CellStage>();
                var spawnForegroundAlgae = typeof(CellStage).GetMethod(
                    "SpawnForegroundAlgae",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(spawnForegroundAlgae, "Expected CellStage.SpawnForegroundAlgae to exist.");
                spawnForegroundAlgae.Invoke(stage, null);

                var foregroundAlgae = GameObject.Find("ForegroundAlgae_0");
                Assert.NotNull(foregroundAlgae, "Expected foreground algae to be spawned.");

                var foodItem = foregroundAlgae.GetComponent<FoodItem>();
                Assert.NotNull(foodItem, "Foreground algae should be collectable food.");
                Assert.AreEqual(FoodType.Plant, foodItem.Type);

                var colliders = foregroundAlgae.GetComponentsInChildren<Collider>(true);
                Assert.Greater(colliders.Length, 0, "Foreground algae should have at least one collider.");
                for (int i = 0; i < colliders.Length; i++)
                {
                    Assert.IsTrue(colliders[i].enabled, "Foreground algae collider should be enabled.");
                    Assert.IsTrue(colliders[i].isTrigger, "Foreground algae collider should be a trigger.");
                }
            }
            finally
            {
                var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                for (int i = 0; i < allObjects.Length; i++)
                {
                    if (allObjects[i] != null && allObjects[i].name.StartsWith("ForegroundAlgae_"))
                        Object.DestroyImmediate(allObjects[i]);
                }

                Object.DestroyImmediate(stageGo);
            }
        }

        [Test]
        public void SetCurrentGoal_ToxinFood_SeedsToxinFoodOpportunity()
        {
            var stageGo = new GameObject("CellStage_GoalSupport_Toxin_Test");
            try
            {
                var stage = stageGo.AddComponent<CellStage>();
                var foodManager = stageGo.AddComponent<FoodManager>();
                typeof(CellStage)
                    .GetField("_foodManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(stage, foodManager);

                var setCurrentGoal = typeof(CellStage).GetMethod(
                    "SetCurrentGoal",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(setCurrentGoal, "Expected CellStage.SetCurrentGoal to exist.");
                setCurrentGoal.Invoke(stage, new object[] { CellGoal.CreateEatFoodGoal(FoodType.Toxin, 2) });

                Assert.GreaterOrEqual(
                    foodManager.FoodCount,
                    5,
                    "Starting a toxin-eating goal should seed enough toxin food to make the goal immediately playable.");
            }
            finally
            {
                Object.DestroyImmediate(stageGo);
            }
        }

        [Test]
        public void SetCurrentGoal_BloomEating_StartsNutrientBloomOpportunity()
        {
            var stageGo = new GameObject("CellStage_GoalSupport_Bloom_Test");
            var previousRandomState = Random.state;
            try
            {
                Random.InitState(104);
                var stage = stageGo.AddComponent<CellStage>();
                var foodManager = stageGo.AddComponent<FoodManager>();
                var eventManager = stageGo.AddComponent<CellEnvironmentEventManager>();
                eventManager.Activate(foodManager, arenaRadius: 45f);

                typeof(CellStage)
                    .GetField("_foodManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(stage, foodManager);
                typeof(CellStage)
                    .GetField("_envEventManager", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(stage, eventManager);

                var setCurrentGoal = typeof(CellStage).GetMethod(
                    "SetCurrentGoal",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.NotNull(setCurrentGoal, "Expected CellStage.SetCurrentGoal to exist.");
                setCurrentGoal.Invoke(stage, new object[] { CellGoal.CreateEatDuringBloomGoal(3) });

                Assert.AreEqual(RoobsWorld.Core.CellEnvironmentEventType.NutrientBloom, eventManager.ActiveEvent);
                Assert.Greater(foodManager.FoodCount, 0);
            }
            finally
            {
                Random.state = previousRandomState;
                var eventManager = stageGo.GetComponent<CellEnvironmentEventManager>();
                if (eventManager != null)
                    eventManager.Deactivate();
                Object.DestroyImmediate(stageGo);
            }
        }

        [Test]
        public void CellAI_Attach_EnsuresEnabledTriggerColliders()
        {
            var npcGo = ModelFactory.CreateCell(Vector3.zero, Color.white, 0.3f);
            npcGo.AddComponent<CellAI>();

            var colliders = npcGo.GetComponentsInChildren<Collider>(true);
            Assert.Greater(colliders.Length, 0, "NPC cell should have at least one collider.");
            for (int i = 0; i < colliders.Length; i++)
            {
                Assert.IsTrue(colliders[i].enabled, "NPC collider should be enabled.");
                Assert.IsTrue(colliders[i].isTrigger, "NPC collider should be a trigger.");
            }

            Object.DestroyImmediate(npcGo);
        }

        [Test]
        public void Organism_Attach_EnsuresEnabledTriggerColliders()
        {
            var organismGo = ProceduralCellGenerator.CreateCellFromGenome(Vector3.zero, Genome.Default());
            organismGo.AddComponent<Organism>();

            var colliders = organismGo.GetComponentsInChildren<Collider>(true);
            Assert.Greater(colliders.Length, 0, "Organism should have at least one collider.");
            for (int i = 0; i < colliders.Length; i++)
            {
                Assert.IsTrue(colliders[i].enabled, "Organism collider should be enabled.");
                Assert.IsTrue(colliders[i].isTrigger, "Organism collider should be a trigger.");
            }

            Object.DestroyImmediate(organismGo);
        }
    }
}
