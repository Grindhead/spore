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
        }
    }
}
