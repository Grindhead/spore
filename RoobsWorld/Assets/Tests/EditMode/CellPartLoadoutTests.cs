using NUnit.Framework;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    public class CellPartLoadoutTests
    {
        [Test]
        public void Defaults_Have_No_Parts_And_Base_Modifiers()
        {
            var loadout = new CellPartLoadout();

            Assert.AreEqual(1f, loadout.SpeedMultiplier, 0.001f);
            Assert.IsFalse(loadout.HasContactDamage);
            Assert.IsFalse(loadout.HasPart(CellPartType.Flagella));
            Assert.AreEqual(0f, loadout.ConsumeProbeRadiusBonus, 0.001f);
            Assert.AreEqual(1f, loadout.FoodGainMultiplier, 0.001f);
            Assert.AreEqual(1f, loadout.BoostDurationMultiplier, 0.001f);
        }

        [Test]
        public void Unlocking_Flagella_And_Cilia_Increases_Speed()
        {
            var loadout = new CellPartLoadout();

            loadout.TryUnlock(CellPartType.Flagella);
            loadout.TryUnlock(CellPartType.Cilia);

            Assert.AreEqual(1.3f, loadout.SpeedMultiplier, 0.001f);
        }

        [Test]
        public void Duplicate_Unlocks_Do_Not_Stack()
        {
            var loadout = new CellPartLoadout();

            bool firstUnlock = loadout.TryUnlock(CellPartType.Jet);
            bool secondUnlock = loadout.TryUnlock(CellPartType.Jet);

            Assert.IsTrue(firstUnlock);
            Assert.IsFalse(secondUnlock);
            Assert.AreEqual(1.35f, loadout.SpeedMultiplier, 0.001f);
        }

        [Test]
        public void Spike_Enables_Contact_Damage()
        {
            var loadout = new CellPartLoadout();

            loadout.TryUnlock(CellPartType.Spike);

            Assert.IsTrue(loadout.HasContactDamage);
            Assert.Greater(loadout.ContactDamageMultiplier, 1f);
        }

        [Test]
        public void Parts_Unlock_Build_Defining_Bonuses()
        {
            var loadout = new CellPartLoadout();

            loadout.TryUnlock(CellPartType.Cilia);
            Assert.Greater(loadout.ConsumeProbeRadiusBonus, 0f);
            Assert.Greater(loadout.FoodGainMultiplier, 1f);

            loadout.TryUnlock(CellPartType.Jet);
            Assert.Greater(loadout.BoostDurationMultiplier, 1f);
            Assert.Greater(loadout.BoostSpeedMultiplier, 1f);
        }

        [Test]
        public void Catalog_Includes_Mouth_Defense_And_Utility_Parts()
        {
            var definitions = CellPartLoadout.GetAllDefinitions();

            Assert.GreaterOrEqual(definitions.Count, 12);
            Assert.AreEqual(CellPartCategory.Mouth, CellPartLoadout.GetDefinition(CellPartType.HerbivoreMouth).Category);
            Assert.AreEqual(CellDietRole.Carnivore, CellPartLoadout.GetDefinition(CellPartType.CarnivoreMouth).DietRole);
            Assert.AreEqual(CellPartCategory.Defense, CellPartLoadout.GetDefinition(CellPartType.ArmorPlate).Category);
            Assert.AreEqual(CellPartCategory.Utility, CellPartLoadout.GetDefinition(CellPartType.SensorEye).Category);
        }

        [Test]
        public void Mouths_Specialize_Food_Gain_By_Diet()
        {
            var herbivore = new CellPartLoadout();
            herbivore.TryUnlock(CellPartType.HerbivoreMouth);

            var carnivore = new CellPartLoadout();
            carnivore.TryUnlock(CellPartType.CarnivoreMouth);

            var omnivore = new CellPartLoadout();
            omnivore.TryUnlock(CellPartType.OmnivoreMouth);

            Assert.Greater(herbivore.GetFoodGainMultiplier(FoodType.Plant), herbivore.GetFoodGainMultiplier(FoodType.Meat));
            Assert.Greater(carnivore.GetFoodGainMultiplier(FoodType.Meat), carnivore.GetFoodGainMultiplier(FoodType.Plant));
            Assert.Greater(omnivore.GetFoodGainMultiplier(FoodType.Plant), 1f);
            Assert.Greater(omnivore.GetFoodGainMultiplier(FoodType.Meat), 1f);
        }

        [Test]
        public void Defensive_And_Sensor_Parts_Add_Readable_Gameplay_Effects()
        {
            var loadout = new CellPartLoadout();

            loadout.TryUnlock(CellPartType.PoisonVacuole);
            loadout.TryUnlock(CellPartType.ElectricNodule);
            loadout.TryUnlock(CellPartType.ArmorPlate);
            loadout.TryUnlock(CellPartType.SensorEye);

            Assert.IsTrue(loadout.HasPoisonAdaptation);
            Assert.IsTrue(loadout.HasArmorPlate);
            Assert.IsTrue(loadout.HasThreatSensor);
            Assert.Greater(loadout.ContactDamageMultiplier, 1f);
            Assert.Greater(loadout.ConsumeProbeRadiusBonus, 0f);
            Assert.Greater(loadout.GetFoodGainMultiplier(FoodType.Toxin), 1f);
        }
    }
}
