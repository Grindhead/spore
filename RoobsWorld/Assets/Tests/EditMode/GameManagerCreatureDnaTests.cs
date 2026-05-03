using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace RoobsWorld.Tests.EditMode
{
    public class GameManagerCreatureDnaTests
    {
        [SetUp]
        public void SetUp()
        {
            if (GameManager.HasInstance)
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            if (SaveManager.HasInstance)
                Object.DestroyImmediate(SaveManager.Instance.gameObject);

            var gameManagerObject = new GameObject("GameManager");
            gameManagerObject.AddComponent<GameManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (GameManager.HasInstance)
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            if (SaveManager.HasInstance)
                Object.DestroyImmediate(SaveManager.Instance.gameObject);
        }

        [Test]
        public void SetPlayerCreatureDNA_StoresJson_AndRoundTrips()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.CreatureName = "RoundTrip";

            GameManager.Instance.SetPlayerCreatureDNA(dna);
            var loaded = GameManager.Instance.GetPlayerCreatureDNA();

            Assert.IsNotNull(GameManager.Instance.CurrentSave);
            Assert.IsNotNull(loaded);
            Assert.AreEqual("RoundTrip", loaded.CreatureName);
            Assert.AreEqual(dna.CreatureId, loaded.CreatureId);
        }

        [Test]
        public void GetPlayerCreatureDNAOrDefault_CreatesAndCachesStarterDna()
        {
            var dna = GameManager.Instance.GetPlayerCreatureDNAOrDefault();

            Assert.IsNotNull(dna);
            Assert.IsNotNull(GameManager.Instance.CurrentSave);
            Assert.IsFalse(string.IsNullOrWhiteSpace(GameManager.Instance.CurrentSave.CreatureDNAJson));
        }

        [Test]
        public void TryValidateCreatureDNA_ReturnsFalse_WhenCreatureIdIsMissing()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.CreatureId = null;

            bool valid = GameManager.TryValidateCreatureDNA(dna, out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("missing creature id.", error);
        }

        [Test]
        public void TryValidateCreatureDNA_ReturnsFalse_WhenBodyShapeIsInvalid()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Body.SpineSegments = 0;

            bool valid = GameManager.TryValidateCreatureDNA(dna, out string error);

            Assert.IsFalse(valid);
            Assert.AreEqual("invalid spine segment count: 0.", error);
        }

        [Test]
        public void GetPlayerCreatureDNAOrDefault_NoCurrentSave_ReturnsDefaultAndRecaches()
        {
            var loaded = GameManager.Instance.GetPlayerCreatureDNAOrDefault();

            Assert.IsNotNull(loaded);
            Assert.IsFalse(string.IsNullOrWhiteSpace(GameManager.Instance.CurrentSave.CreatureDNAJson));
        }

        [Test]
        public void GetPlayerCreatureDNA_RejectsIncompatibleShapeWithoutRecaching()
        {
            GameManager.Instance.SetPlayerCreatureDNA(CreatureDNA.CreateDefault());
            var incompatible = CreatureDNA.CreateDefault();
            incompatible.Parts[0].AttachBone = 99;
            GameManager.Instance.CurrentSave.CreatureDNAJson = incompatible.ToJson();

            LogAssert.Expect(LogType.Error, new Regex(@"\[GameManager\] Invalid CreatureDNAJson: parts\[0\] attach bone 99 is outside body range 0\.\.2\."));

            var loaded = GameManager.Instance.GetPlayerCreatureDNA();

            Assert.IsNull(loaded);
            Assert.IsTrue(GameManager.Instance.CurrentSave.CreatureDNAJson.Contains("\"AttachBone\": 99"));
        }
    }
}
