#if UNITY_EDITOR
using NUnit.Framework;
using RoobsWorld.Editor;
using UnityEditor;

namespace RoobsWorld.Tests
{
    public class BuildSettingsSetupTests
    {
        [Test]
        public void BuildOrderedScenesForTests_RequiredScenesLeadAndAreEnabled()
        {
            var current = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Bonus.unity", false),
                new EditorBuildSettingsScene("Assets/Scenes/CellStage.unity", false),
                new EditorBuildSettingsScene("Assets/Scenes/CreatureStage.unity", false)
            };

            var ordered = RoobsWorldBuildSettingsSetup.BuildOrderedScenesForTests(
                current,
                new[] { "Assets/Scenes/CellStage.unity", "Assets/Scenes/CreatureStage.unity" });

            Assert.AreEqual(3, ordered.Length);
            Assert.AreEqual("Assets/Scenes/CellStage.unity", ordered[0].path);
            Assert.AreEqual("Assets/Scenes/CreatureStage.unity", ordered[1].path);
            Assert.IsTrue(ordered[0].enabled);
            Assert.IsTrue(ordered[1].enabled);
            Assert.AreEqual("Assets/Scenes/Bonus.unity", ordered[2].path);
            Assert.IsFalse(ordered[2].enabled);
        }
    }
}
#endif
