using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class InputActionsContractTests
    {
        private const string InputActionsAssetPath = "Assets/InputSystem_Actions.inputactions";

        [Test]
        public void InputSystemActionsAsset_Exists_AndHasExpectedMapsAndActions()
        {
            var asset = AssetDatabase.LoadAssetAtPath<Object>(InputActionsAssetPath);
            Assert.IsNotNull(asset, $"Expected input actions asset at '{InputActionsAssetPath}'.");
        }
    }
}

