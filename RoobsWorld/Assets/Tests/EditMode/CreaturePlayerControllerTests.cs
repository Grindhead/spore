using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Creature;
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

        [Test]
        public void CanPrimeGuardCounter_RequiresActiveGuardBlockedPlayerHit()
        {
            Assert.IsTrue(CreaturePlayerController.CanPrimeGuardCounter(
                guardRemaining: 0.2f,
                feedbackType: CreatureCombatFeedbackType.Blocked,
                isPlayerTarget: true));
            Assert.IsFalse(CreaturePlayerController.CanPrimeGuardCounter(
                guardRemaining: 0f,
                feedbackType: CreatureCombatFeedbackType.Blocked,
                isPlayerTarget: true));
            Assert.IsFalse(CreaturePlayerController.CanPrimeGuardCounter(
                guardRemaining: 0.2f,
                feedbackType: CreatureCombatFeedbackType.Hit,
                isPlayerTarget: true));
            Assert.IsFalse(CreaturePlayerController.CanPrimeGuardCounter(
                guardRemaining: 0.2f,
                feedbackType: CreatureCombatFeedbackType.Blocked,
                isPlayerTarget: false));
        }

        [Test]
        public void ApplyCounterAttackMultiplier_SpendsOnlyActiveCounterWindow()
        {
            Assert.That(
                CreaturePlayerController.ApplyCounterAttackMultiplier(10f, counterWindowRemaining: 0f, counterMultiplier: 1.45f),
                Is.EqualTo(10f).Within(0.0001f));
            Assert.That(
                CreaturePlayerController.ApplyCounterAttackMultiplier(10f, counterWindowRemaining: 0.5f, counterMultiplier: 1.45f),
                Is.EqualTo(14.5f).Within(0.0001f));
        }

        [Test]
        public void StaminaSpendAndRecovery_ClampWithinBudget()
        {
            Assert.IsTrue(CreaturePlayerController.CanSpendStamina(20f, 18f));
            Assert.IsFalse(CreaturePlayerController.CanSpendStamina(12f, 18f));
            Assert.That(CreaturePlayerController.SpendStamina(20f, 18f), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(CreaturePlayerController.SpendStamina(8f, 18f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(CreaturePlayerController.RecoverStamina(50f, 100f, 24f, 0.5f), Is.EqualTo(62f).Within(0.0001f));
            Assert.That(CreaturePlayerController.RecoverStamina(98f, 100f, 24f, 0.5f), Is.EqualTo(100f).Within(0.0001f));
            Assert.That(CreaturePlayerController.RestoreStamina(60f, 100f, 26f), Is.EqualTo(86f).Within(0.0001f));
            Assert.That(CreaturePlayerController.RestoreStamina(90f, 100f, 26f), Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void UpgradeStaminaModifiers_ChangeBudgetRecoveryAndCooldown()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Upgrades.Add(new CreatureUpgradeRecord
            {
                MaxStaminaDelta = 20f,
                StaminaRecoveryDelta = 8f,
                AbilityCooldownDelta = -1.5f
            });

            Assert.That(CreaturePlayerController.ComputeMaxStamina(dna, 100f), Is.EqualTo(120f).Within(0.0001f));
            Assert.That(CreaturePlayerController.ComputeStaminaRecovery(dna, 24f), Is.EqualTo(32f).Within(0.0001f));
            Assert.That(CreaturePlayerController.ComputeAbilityCooldown(dna, 6f), Is.EqualTo(4.5f).Within(0.0001f));
        }

        [Test]
        public void ResolveInheritedAbility_UsesLegacyAbilityId()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.LegacyAbilityId = "toxin_burst";

            Assert.AreEqual(
                CreatureInheritedAbilityType.ToxinBurst,
                CreaturePlayerController.ResolveInheritedAbility(dna));
        }

        [Test]
        public void ResolveInheritedAbility_FallsBackToCreatureParts()
        {
            var dna = CreatureDNA.CreateDefault();
            dna.Parts.Add(new PartPlacement { PartId = "tail_basic", AttachBone = 2, Offset = Vector3.back });

            Assert.AreEqual(
                CreatureInheritedAbilityType.Dash,
                CreaturePlayerController.ResolveInheritedAbility(dna));
        }
    }
}
