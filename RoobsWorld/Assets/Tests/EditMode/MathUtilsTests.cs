using NUnit.Framework;
using RoobsWorld.Utils;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class MathUtilsTests
    {
        [Test]
        public void Remap_Maps_Correctly()
        {
            float result = MathUtils.Remap(5f, 0f, 10f, 0f, 100f);
            Assert.AreEqual(50f, result, 0.01f);
        }

        [Test]
        public void Remap_Handles_Boundary_Values()
        {
            Assert.AreEqual(0f, MathUtils.Remap(0f, 0f, 10f, 0f, 100f), 0.01f);
            Assert.AreEqual(100f, MathUtils.Remap(10f, 0f, 10f, 0f, 100f), 0.01f);
        }

        [Test]
        public void RandomPointOnCircle_Returns_Correct_Radius()
        {
            for (int i = 0; i < 100; i++)
            {
                var point = MathUtils.RandomPointOnCircle(5f);
                float distance = new Vector2(point.x, point.z).magnitude;
                Assert.AreEqual(5f, distance, 0.01f);
                Assert.AreEqual(0f, point.y, 0.001f);
            }
        }

        [Test]
        public void RandomPointInCircle_Returns_Within_Radius()
        {
            for (int i = 0; i < 100; i++)
            {
                var point = MathUtils.RandomPointInCircle(5f);
                float distance = new Vector2(point.x, point.z).magnitude;
                Assert.LessOrEqual(distance, 5f + 0.01f);
                Assert.AreEqual(0f, point.y, 0.001f);
            }
        }

        [Test]
        public void StableHash_Is_Deterministic()
        {
            int hash1 = MathUtils.StableHash("test_string");
            int hash2 = MathUtils.StableHash("test_string");
            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void StableHash_Different_Inputs_Produce_Different_Hashes()
        {
            int hashA = MathUtils.StableHash("alpha");
            int hashB = MathUtils.StableHash("beta");
            Assert.AreNotEqual(hashA, hashB);
        }
    }
}
