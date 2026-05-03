using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class StatCalculatorTests
    {
        private Dictionary<string, CreaturePartDef> BuildCatalog(params (string id, float speed, float health, float attack)[] parts)
        {
            var catalog = new Dictionary<string, CreaturePartDef>();
            foreach (var (id, speed, health, attack) in parts)
            {
                var def = ScriptableObject.CreateInstance<CreaturePartDef>();
                def.PartId = id;
                def.SpeedModifier = speed;
                def.HealthModifier = health;
                def.AttackModifier = attack;
                catalog[id] = def;
            }
            return catalog;
        }

        [Test]
        public void Base_Stats_Are_Applied_With_No_Parts()
        {
            var dna = new CreatureDNA { Parts = new List<PartPlacement>() };
            var catalog = new Dictionary<string, CreaturePartDef>();

            var stats = StatCalculator.Calculate(dna, catalog);

            Assert.AreEqual(CreatureStats.BaseStats.Speed, stats.Speed);
            Assert.AreEqual(CreatureStats.BaseStats.Health, stats.Health);
            Assert.AreEqual(CreatureStats.BaseStats.Attack, stats.Attack);
        }

        [Test]
        public void Parts_Modify_Stats()
        {
            var catalog = BuildCatalog(("leg_fast", 3f, 0f, 0f));
            var dna = new CreatureDNA
            {
                Parts = new List<PartPlacement>
                {
                    new PartPlacement { PartId = "leg_fast" },
                    new PartPlacement { PartId = "leg_fast" }
                }
            };

            var stats = StatCalculator.Calculate(dna, catalog);

            Assert.AreEqual(CreatureStats.BaseStats.Speed + 6f, stats.Speed);
        }

        [Test]
        public void Stats_Cannot_Go_Below_Minimums()
        {
            var catalog = BuildCatalog(("bad_part", -100f, -500f, -100f));
            var dna = new CreatureDNA
            {
                Parts = new List<PartPlacement>
                {
                    new PartPlacement { PartId = "bad_part" }
                }
            };

            var stats = StatCalculator.Calculate(dna, catalog);

            Assert.GreaterOrEqual(stats.Speed, 1f);
            Assert.GreaterOrEqual(stats.Health, 10f);
            Assert.GreaterOrEqual(stats.Attack, 0f);
        }

        [Test]
        public void Unknown_Parts_Are_Ignored()
        {
            var catalog = new Dictionary<string, CreaturePartDef>();
            var dna = new CreatureDNA
            {
                Parts = new List<PartPlacement>
                {
                    new PartPlacement { PartId = "nonexistent_part" }
                }
            };

            var stats = StatCalculator.Calculate(dna, catalog);

            Assert.AreEqual(CreatureStats.BaseStats.Speed, stats.Speed);
        }

        [Test]
        public void CreatureUpgrades_Modify_Health_And_Speed()
        {
            var catalog = new Dictionary<string, CreaturePartDef>();
            var dna = new CreatureDNA
            {
                Parts = new List<PartPlacement>(),
                Upgrades = new List<CreatureUpgradeRecord>
                {
                    new CreatureUpgradeRecord { UpgradeId = "test", HealthDelta = 45f, SpeedDelta = -1.4f }
                }
            };

            var stats = StatCalculator.Calculate(dna, catalog);

            Assert.AreEqual(CreatureStats.BaseStats.Health + 45f, stats.Health);
            Assert.AreEqual(CreatureStats.BaseStats.Speed - 1.4f, stats.Speed);
        }

        [Test]
        public void ApplyUpgrade_Appends_Permanent_Record()
        {
            var dna = CreatureDNA.CreateDefault();
            var option = new CreatureUpgradeOption
            {
                Id = "runner_frame",
                HealthDelta = -25f,
                SpeedDelta = 2.2f
            };

            CreatureUpgradeCatalog.ApplyUpgrade(dna, option);

            Assert.AreEqual(1, dna.Upgrades.Count);
            Assert.AreEqual("runner_frame", dna.Upgrades[0].UpgradeId);
            Assert.AreEqual(-25f, dna.Upgrades[0].HealthDelta);
            Assert.AreEqual(2.2f, dna.Upgrades[0].SpeedDelta);
        }
    }
}
