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
    }
}
