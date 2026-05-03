using NUnit.Framework;
using UnityEngine;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class GenomeTests
    {
        [Test]
        public void Default_HasExpectedTraits()
        {
            var g = Genome.Default();
            Assert.AreEqual(1f, g.Speed);
            Assert.AreEqual(1f, g.Size);
            Assert.AreEqual(1f, g.Attack);
            Assert.AreEqual(1f, g.Defence);
            Assert.AreEqual(1f, g.Digestion);
            Assert.AreEqual(0.5f, g.MetabolismCost);
        }

        [Test]
        public void Clone_MatchesOriginal()
        {
            var g = new Genome { Speed = 2f, Size = 1.5f, Attack = 3f, Color = Color.red };
            var c = g.Clone();
            Assert.AreEqual(g.Speed, c.Speed);
            Assert.AreEqual(g.Size, c.Size);
            Assert.AreEqual(g.Attack, c.Attack);
            Assert.AreEqual(g.Color, c.Color);
        }

        [Test]
        public void MaxHealth_ReflectsSizeAndDefence()
        {
            var g = new Genome { Size = 2f, Defence = 3f };
            Assert.GreaterOrEqual(g.MaxHealth, 0.1f);
            Assert.AreEqual(6f, g.MaxHealth);
        }

        [Test]
        public void FoodEfficiency_EqualsDigestion()
        {
            var g = new Genome { Digestion = 2.5f };
            Assert.AreEqual(2.5f, g.FoodEfficiency);
        }
    }

    public class OrganismTests
    {
        [Test]
        public void Eat_IncreasesEnergy()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default());
            float before = org.Energy;
            float gained = org.Eat(10f);
            Assert.Greater(org.Energy, before);
            Assert.Greater(gained, 0f);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Move_ChangesPosition()
        {
            var org = Organism.Create(Vector3.zero, new Genome { Speed = 2f });
            Vector3 before = org.transform.position;
            org.Move(Vector3.forward, 0.2f);
            Assert.Greater(org.transform.position.z, before.z);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Die_SetsIsDead()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default());
            Assert.IsFalse(org.IsDead);
            org.Die();
            Assert.IsTrue(org.IsDead);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Die_PublishesCellOrganismDiedEvent_WithPositionAndScale()
        {
            var org = Organism.Create(new Vector3(3f, 0f, -2f), new Genome { Size = 2f });
            CellOrganismDiedEvent last = default;
            bool fired = false;
            System.Action<CellOrganismDiedEvent> handler = evt =>
            {
                fired = true;
                last = evt;
            };
            EventBus.Subscribe(handler);

            org.Die();

            EventBus.Unsubscribe(handler);

            Assert.IsTrue(fired);
            Assert.AreEqual(org.SpeciesId, last.SpeciesId);
            Assert.AreEqual(org.transform.position.x, last.Position.x, 0.001f);
            Assert.AreEqual(org.transform.position.z, last.Position.z, 0.001f);
            Assert.Greater(last.VisualScale, 0f);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Create_WithNullGenome_UsesDefault()
        {
            var org = Organism.Create(Vector3.zero, null);
            Assert.NotNull(org.Genome);
            Assert.AreEqual(1f, org.Genome.Speed);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Eat_WhenDead_ReturnsZero()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default());
            org.Die();
            float gained = org.Eat(10f);
            Assert.AreEqual(0f, gained);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void ApplyMetabolism_DrainsEnergy()
        {
            var genome = new Genome { MetabolismCost = 1f };
            var org = Organism.Create(Vector3.zero, genome);
            float before = org.Energy;
            org.ApplyMetabolism(5f);
            Assert.Less(org.Energy, before);
            Assert.AreEqual(before - 5f, org.Energy, 0.001f);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void ApplyMetabolism_WhenDead_DoesNothing()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default());
            float before = org.Energy;
            org.Die();
            org.ApplyMetabolism(10f);
            Assert.AreEqual(before, org.Energy);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Tick_WhenStarved_ReturnsTrueAndSetsDead()
        {
            var genome = new Genome { MetabolismCost = 1f };
            var org = Organism.Create(Vector3.zero, genome);
            float initial = org.Energy;
            bool died = false;
            for (int i = 0; i < 200 && !org.IsDead; i++)
                died = org.Tick(0.5f);
            Assert.IsTrue(org.IsDead);
            Assert.IsTrue(died);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void Eat_ThenApplyMetabolism_NetEnergyCorrect()
        {
            var genome = new Genome { Digestion = 1f, MetabolismCost = 0.5f };
            var org = Organism.Create(Vector3.zero, genome);
            float before = org.Energy;
            org.Eat(20f);
            org.ApplyMetabolism(10f);
            Assert.AreEqual(before + 20f - 5f, org.Energy, 0.001f);
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void AdaptiveMutationBias_RecordsOwnSurvivalHistory()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default());

            for (int i = 0; i < 10; i++)
                org.RecordFoodExperience(FoodType.Plant);
            for (int i = 0; i < 5; i++)
                org.RecordPredationExperience();
            for (int i = 0; i < 6; i++)
                org.RecordDamageExperience();
            for (int i = 0; i < 3; i++)
                org.RecordThreatEscapeExperience();

            var bias = org.GetAdaptiveMutationBias();

            Assert.Greater(bias.Digestion, 0f, "Plant feeding should bias digestion.");
            Assert.Greater(bias.Attack, 0f, "Predation should bias attack.");
            Assert.Greater(bias.Defence, 0f, "Damage should bias defence.");
            Assert.Greater(bias.Speed, 0f, "Threat escapes should bias speed.");
            Object.DestroyImmediate(org.gameObject);
        }

        [Test]
        public void ResetAdaptiveMutationBias_ClearsSurvivalHistory()
        {
            var org = Organism.Create(Vector3.zero, Genome.Default());
            for (int i = 0; i < 8; i++)
                org.RecordPredationExperience();

            Assert.Greater(org.GetAdaptiveMutationBias().Attack, 0f);

            org.ResetAdaptiveMutationBias();

            Assert.AreEqual(0f, org.GetAdaptiveMutationBias().Attack);
            Object.DestroyImmediate(org.gameObject);
        }
    }

    public class CellStageCarcassNutritionTests
    {
        [Test]
        public void ComputeCarcassNutrition_ClampsBelowMinimumScale()
        {
            float nutrition = CellStage.ComputeCarcassNutrition(-5f);
            Assert.AreEqual(BalanceConstants.CellCarcassNutritionMin, nutrition, 0.001f);
        }

        [Test]
        public void ComputeCarcassNutrition_ClampsAboveMaximumScale()
        {
            float nutrition = CellStage.ComputeCarcassNutrition(999f);
            Assert.AreEqual(BalanceConstants.CellCarcassNutritionMax, nutrition, 0.001f);
        }

        [Test]
        public void ComputeCarcassNutrition_LargerScaleYieldsMoreNutrition()
        {
            float small = CellStage.ComputeCarcassNutrition(0.3f);
            float large = CellStage.ComputeCarcassNutrition(1.2f);
            Assert.Greater(large, small);
        }
    }
}
