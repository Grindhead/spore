using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.UI;
using UnityEngine;

namespace RoobsWorld.Tests.EditMode
{
    public class RuntimePolishRulesTests
    {
        [Test]
        public void RuntimePerformanceBudget_ScalesForMultiplayerAndStress()
        {
            var solo = RuntimePerformanceBudget.Calculate(playerCount: 1, arenaRadius: 48f);
            var squad = RuntimePerformanceBudget.Calculate(playerCount: 4, arenaRadius: 72f);
            var stress = RuntimePerformanceBudget.Calculate(playerCount: 8, arenaRadius: 72f, stressSession: true);

            Assert.Greater(squad.Food, solo.Food);
            Assert.Greater(squad.RemoteGhosts, solo.RemoteGhosts);
            Assert.Greater(stress.CreatureRivals, squad.CreatureRivals);
            Assert.IsTrue(RuntimePerformanceBudget.IsStressBudget(stress));
        }

        [Test]
        public void RuntimePerformanceBudget_ClampsNetworkTickRateForLargeSessions()
        {
            Assert.AreEqual(20, RuntimePerformanceBudget.ClampNetworkTickRate(30, playerCount: 2));
            Assert.AreEqual(16, RuntimePerformanceBudget.ClampNetworkTickRate(30, playerCount: 4));
            Assert.AreEqual(12, RuntimePerformanceBudget.ClampNetworkTickRate(30, playerCount: 10));
            Assert.AreEqual(8, RuntimePerformanceBudget.ClampNetworkTickRate(1, playerCount: 10));
        }

        [Test]
        public void UIStyleGuide_AccessibilityRulesClampTextMotionAndHitTargets()
        {
            Assert.AreEqual(0f, UIStyleGuide.Accessibility.ApplyMotionPreference(0.35f, reducedMotion: true));
            Assert.AreEqual(0.35f, UIStyleGuide.Accessibility.ApplyMotionPreference(0.35f, reducedMotion: false), 0.0001f);
            Assert.AreEqual(UIStyleGuide.Accessibility.MinimumReadableTextScale, UIStyleGuide.Accessibility.ClampTextScale(0.1f));
            Assert.AreEqual(UIStyleGuide.Accessibility.MaximumReadableTextScale, UIStyleGuide.Accessibility.ClampTextScale(9f));

            var go = new GameObject("HitTarget");
            try
            {
                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(12f, 20f);
                UIStyleGuide.EnsureMinimumHitTarget(rect);
                Assert.GreaterOrEqual(rect.sizeDelta.x, UIStyleGuide.Layout.MinimumTouchTarget);
                Assert.GreaterOrEqual(rect.sizeDelta.y, UIStyleGuide.Layout.MinimumTouchTarget);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
