using NUnit.Framework;
using RoobsWorld.Creature.Animation;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class GaitGeneratorTests
    {
        [Test]
        public void Biped_Has_Two_Limb_Groups_With_Alternating_Phase()
        {
            var limbs = new LimbConfig[]
            {
                new LimbConfig { LimbIndex = 0, Side = LimbSide.Left, BodySegment = 0 },
                new LimbConfig { LimbIndex = 1, Side = LimbSide.Right, BodySegment = 0 },
            };

            var gait = GaitGenerator.Generate(limbs);

            Assert.AreEqual(2, gait.LimbPhases.Length);
            Assert.AreNotEqual(gait.LimbPhases[0], gait.LimbPhases[1],
                "Opposite limbs should have different phase offsets");
        }

        [Test]
        public void Quadruped_Has_Four_Phases_Alternating_Diagonals()
        {
            var limbs = new LimbConfig[]
            {
                new LimbConfig { LimbIndex = 0, Side = LimbSide.Left, BodySegment = 0 },
                new LimbConfig { LimbIndex = 1, Side = LimbSide.Right, BodySegment = 0 },
                new LimbConfig { LimbIndex = 2, Side = LimbSide.Left, BodySegment = 1 },
                new LimbConfig { LimbIndex = 3, Side = LimbSide.Right, BodySegment = 1 },
            };

            var gait = GaitGenerator.Generate(limbs);

            Assert.AreEqual(4, gait.LimbPhases.Length);
            Assert.AreEqual(gait.LimbPhases[0], gait.LimbPhases[3],
                "Front-left and rear-right (diagonals) should share phase");
            Assert.AreEqual(gait.LimbPhases[1], gait.LimbPhases[2],
                "Front-right and rear-left (diagonals) should share phase");
        }

        [Test]
        public void Hexapod_Produces_Tripod_Gait()
        {
            var limbs = new LimbConfig[]
            {
                new LimbConfig { LimbIndex = 0, Side = LimbSide.Left, BodySegment = 0 },
                new LimbConfig { LimbIndex = 1, Side = LimbSide.Right, BodySegment = 0 },
                new LimbConfig { LimbIndex = 2, Side = LimbSide.Left, BodySegment = 1 },
                new LimbConfig { LimbIndex = 3, Side = LimbSide.Right, BodySegment = 1 },
                new LimbConfig { LimbIndex = 4, Side = LimbSide.Left, BodySegment = 2 },
                new LimbConfig { LimbIndex = 5, Side = LimbSide.Right, BodySegment = 2 },
            };

            var gait = GaitGenerator.Generate(limbs);

            Assert.AreEqual(6, gait.LimbPhases.Length);
            Assert.AreEqual(gait.LimbPhases[0], gait.LimbPhases[3],
                "Tripod group A: L0, R1, L2");
            Assert.AreEqual(gait.LimbPhases[0], gait.LimbPhases[4],
                "Tripod group A: L0, R1, L2");
            Assert.AreEqual(gait.LimbPhases[1], gait.LimbPhases[2],
                "Tripod group B: R0, L1, R2");
            Assert.AreEqual(gait.LimbPhases[1], gait.LimbPhases[5],
                "Tripod group B: R0, L1, R2");
        }

        [Test]
        public void StepHeight_Defaults_To_Positive_Value()
        {
            var limbs = new LimbConfig[]
            {
                new LimbConfig { LimbIndex = 0, Side = LimbSide.Left, BodySegment = 0 },
                new LimbConfig { LimbIndex = 1, Side = LimbSide.Right, BodySegment = 0 },
            };

            var gait = GaitGenerator.Generate(limbs);

            Assert.Greater(gait.StepHeight, 0f);
        }

        [Test]
        public void StrideDuration_Defaults_To_Positive_Value()
        {
            var limbs = new LimbConfig[]
            {
                new LimbConfig { LimbIndex = 0, Side = LimbSide.Left, BodySegment = 0 },
                new LimbConfig { LimbIndex = 1, Side = LimbSide.Right, BodySegment = 0 },
            };

            var gait = GaitGenerator.Generate(limbs);

            Assert.Greater(gait.StrideDuration, 0f);
        }

        [Test]
        public void Zero_Limbs_Returns_Empty_Gait()
        {
            var gait = GaitGenerator.Generate(new LimbConfig[0]);

            Assert.AreEqual(0, gait.LimbPhases.Length);
        }

        [Test]
        public void Single_Limb_Gets_Phase_Zero()
        {
            var limbs = new LimbConfig[]
            {
                new LimbConfig { LimbIndex = 0, Side = LimbSide.Left, BodySegment = 0 },
            };

            var gait = GaitGenerator.Generate(limbs);

            Assert.AreEqual(1, gait.LimbPhases.Length);
            Assert.AreEqual(0f, gait.LimbPhases[0], 0.001f);
        }
    }
}
