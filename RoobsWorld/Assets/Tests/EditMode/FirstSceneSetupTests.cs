#if UNITY_EDITOR
using NUnit.Framework;
using RoobsWorld.Editor;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class FirstSceneSetupTests
    {
        [Test]
        public void FindInputActionAsset_ReturnsInputSystemActionsAsset_WhenPresent()
        {
            Object asset = RoobsWorldFirstSceneSetup.FindInputActionAssetForTests();
            Assert.NotNull(asset);
            Assert.AreEqual("InputSystem_Actions", asset.name);
        }
    }
}
#endif

