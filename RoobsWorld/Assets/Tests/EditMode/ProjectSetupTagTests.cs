#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using RoobsWorld.Editor;
using UnityEditorInternal;

namespace RoobsWorld.Tests
{
    public class ProjectSetupTagTests
    {
        [Test]
        public void EnsureRequiredTags_AddsFoodAndAiTags()
        {
            RoobsWorldProjectSetup.EnsureRequiredTags();

            var tags = InternalEditorUtility.tags;
            Assert.IsTrue(tags.Contains("Food"));
            Assert.IsTrue(tags.Contains("AI_Generated"));
            Assert.IsTrue(tags.Contains("AI_Enemy"));
            Assert.IsTrue(tags.Contains("AI_Loot"));
        }
    }
}
#endif

