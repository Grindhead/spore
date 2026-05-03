using NUnit.Framework;
using RoobsWorld.Creature;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class CreatureDNATests
    {
        [Test]
        public void CreateDefault_Returns_Valid_DNA()
        {
            var dna = CreatureDNA.CreateDefault();

            Assert.IsNotNull(dna);
            Assert.IsNotNull(dna.CreatureId);
            Assert.IsNotEmpty(dna.CreatureId);
            Assert.AreEqual("Roob", dna.CreatureName);
            Assert.AreEqual(3, dna.Body.SpineSegments);
            Assert.AreEqual(SymmetryType.Bilateral, dna.Body.Symmetry);
            Assert.IsTrue(dna.Parts.Count > 0);
        }

        [Test]
        public void Serialization_Roundtrip_Preserves_Data()
        {
            var original = CreatureDNA.CreateDefault();
            original.CreatureName = "TestCreature";
            original.Body.SpineSegments = 5;

            string json = original.ToJson();
            var restored = CreatureDNA.FromJson(json);

            Assert.AreEqual(original.CreatureName, restored.CreatureName);
            Assert.AreEqual(original.Body.SpineSegments, restored.Body.SpineSegments);
            Assert.AreEqual(original.Parts.Count, restored.Parts.Count);
            Assert.AreEqual(original.CreatureId, restored.CreatureId);
        }

        [Test]
        public void Clone_Creates_Independent_Copy()
        {
            var original = CreatureDNA.CreateDefault();
            var clone = original.Clone();

            clone.CreatureName = "Modified";
            clone.Body.SpineSegments = 10;

            Assert.AreNotEqual(original.CreatureName, clone.CreatureName);
            Assert.AreNotEqual(original.Body.SpineSegments, clone.Body.SpineSegments);
        }

        [Test]
        public void Parts_Have_Valid_PartIds()
        {
            var dna = CreatureDNA.CreateDefault();

            foreach (var part in dna.Parts)
            {
                Assert.IsNotNull(part.PartId);
                Assert.IsNotEmpty(part.PartId);
            }
        }

        [Test]
        public void Colors_Have_Sensible_Defaults()
        {
            var dna = CreatureDNA.CreateDefault();

            Assert.AreNotEqual(Color.black, dna.Colors.PrimaryColor);
            Assert.AreNotEqual(Color.black, dna.Colors.SecondaryColor);
        }

        [Test]
        public void ToJson_Produces_Valid_JSON()
        {
            var dna = CreatureDNA.CreateDefault();
            string json = dna.ToJson();

            Assert.IsNotNull(json);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains("CreatureId"));
            Assert.IsTrue(json.Contains("Parts"));
            Assert.IsTrue(json.Contains("Body"));
        }
    }
}
