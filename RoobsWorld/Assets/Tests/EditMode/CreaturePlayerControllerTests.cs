using NUnit.Framework;
using RoobsWorld.Stages.Creature;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class CreaturePlayerControllerTests
    {
        [Test]
        public void ComputeMoveWorld_UsesCameraForward_ForForwardInput()
        {
            Vector3 move = CreaturePlayerController.ComputeMoveWorld(
                new Vector2(0f, 1f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 0f));

            Assert.That(move.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ComputeMoveWorld_UsesCameraRight_ForHorizontalInput()
        {
            Vector3 move = CreaturePlayerController.ComputeMoveWorld(
                new Vector2(1f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(0f, 0f, -1f));

            Assert.That(move.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.z, Is.EqualTo(-1f).Within(0.001f));
        }

        [Test]
        public void ComputeMoveWorld_NormalizesDiagonalInput()
        {
            Vector3 move = CreaturePlayerController.ComputeMoveWorld(
                new Vector2(1f, 1f),
                new Vector3(0f, 0f, 1f),
                new Vector3(1f, 0f, 0f));

            Assert.That(move.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ComputeMoveWorld_FallsBack_WhenCameraVectorsAreFlatZero()
        {
            Vector3 move = CreaturePlayerController.ComputeMoveWorld(
                new Vector2(0f, 1f),
                Vector3.zero,
                Vector3.zero);

            Assert.That(move.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(move.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void ComputePassiveRegenAmount_ReturnsZero_WhenRecentlyDamaged()
        {
            float regen = CreaturePlayerController.ComputePassiveRegenAmount(
                currentHealth: 40f,
                maxHealth: 100f,
                tookDamageRecently: true,
                regenPerSecond: 3f,
                deltaTime: 0.5f);

            Assert.That(regen, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ComputePassiveRegenAmount_ReturnsExpectedValue_WhenEligible()
        {
            float regen = CreaturePlayerController.ComputePassiveRegenAmount(
                currentHealth: 40f,
                maxHealth: 100f,
                tookDamageRecently: false,
                regenPerSecond: 4f,
                deltaTime: 0.5f);

            Assert.That(regen, Is.EqualTo(2f).Within(0.0001f));
        }

        [Test]
        public void ComputePassiveRegenAmount_ClampsToMissingHealth()
        {
            float regen = CreaturePlayerController.ComputePassiveRegenAmount(
                currentHealth: 99f,
                maxHealth: 100f,
                tookDamageRecently: false,
                regenPerSecond: 10f,
                deltaTime: 1f);

            Assert.That(regen, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
