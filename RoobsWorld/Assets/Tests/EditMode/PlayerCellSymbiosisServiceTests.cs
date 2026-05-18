using NUnit.Framework;
using RoobsWorld.Stages.Cell;
using RoobsWorld.Stages.Cell.Services;

namespace RoobsWorld.Tests.EditMode
{
    public class PlayerCellSymbiosisServiceTests
    {
        [Test]
        public void SymbiosisService_MatchesPlayerCellFacade()
        {
            Assert.AreEqual(
                PlayerCellSymbiosisRules.CanUpgradeSymbiosisLevel(2, 3),
                PlayerCell.CanUpgradeSymbiosisLevel(2));

            Assert.AreEqual(
                PlayerCellSymbiosisRules.UpgradeSymbiosisLevel(2, 3),
                PlayerCell.UpgradeSymbiosisLevel(2));

            Assert.AreEqual(
                PlayerCellSymbiosisRules.GetTraitPassiveMultiplier(2, 0.2f),
                PlayerCell.GetTraitPassiveMultiplier(2),
                0.0001f);
        }
    }
}
