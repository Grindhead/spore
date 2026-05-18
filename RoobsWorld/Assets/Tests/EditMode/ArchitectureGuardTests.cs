using System.IO;
using NUnit.Framework;

namespace RoobsWorld.Tests.EditMode
{
    public class ArchitectureGuardTests
    {
        private const string ScriptsRoot = "Assets/Scripts";

        [Test]
        public void CellStage_DoesNotUseSceneWideFindInUpdateLoop()
        {
            string path = Path.Combine(ScriptsRoot, "Stages/Cell/CellStage.cs");
            string text = File.ReadAllText(path);
            StringAssert.DoesNotContain("FindAnyObjectByType", text);
        }

        [Test]
        public void PlayerCell_UsesSymbiosisService()
        {
            string path = Path.Combine(ScriptsRoot, "Stages/Cell/PlayerCell.cs");
            string text = File.ReadAllText(path);
            StringAssert.Contains("PlayerCellSymbiosisRules", text);
        }
    }
}
