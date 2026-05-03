using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class CameraManagerTests
    {
        [Test]
        public void ComputeShakeIntensity_FallsLinearlyAcrossWindow()
        {
            float start = CameraManager.ComputeShakeIntensity(0f, 0f, 1f, 0.4f);
            float mid = CameraManager.ComputeShakeIntensity(0.5f, 0f, 1f, 0.4f);
            float end = CameraManager.ComputeShakeIntensity(1f, 0f, 1f, 0.4f);

            Assert.AreEqual(0.4f, start, 0.0001f);
            Assert.AreEqual(0.2f, mid, 0.0001f);
            Assert.AreEqual(0f, end, 0.0001f);
        }

        [Test]
        public void ComputeShakeIntensity_ClampsOutsideWindow()
        {
            float before = CameraManager.ComputeShakeIntensity(-2f, 0f, 1f, 0.5f);
            float after = CameraManager.ComputeShakeIntensity(3f, 0f, 1f, 0.5f);

            Assert.AreEqual(0.5f, before, 0.0001f);
            Assert.AreEqual(0f, after, 0.0001f);
        }

        [Test]
        public void ComputeThreatZoomOut_ScalesWithIntensity()
        {
            float low = CameraManager.ComputeThreatZoomOut(0.25f, 0.8f);
            float high = CameraManager.ComputeThreatZoomOut(1f, 0.8f);

            Assert.AreEqual(0.2f, low, 0.0001f);
            Assert.AreEqual(0.8f, high, 0.0001f);
        }

        [Test]
        public void ShouldTriggerThreatPulse_RequiresThresholdAndCooldown()
        {
            bool belowThreshold = CameraManager.ShouldTriggerThreatPulse(0.6f, threshold: 0.75f, elapsedSinceLastPulse: 2f, cooldown: 1f);
            bool beforeCooldown = CameraManager.ShouldTriggerThreatPulse(0.9f, threshold: 0.75f, elapsedSinceLastPulse: 0.2f, cooldown: 1f);
            bool valid = CameraManager.ShouldTriggerThreatPulse(0.9f, threshold: 0.75f, elapsedSinceLastPulse: 1.2f, cooldown: 1f);

            Assert.IsFalse(belowThreshold);
            Assert.IsFalse(beforeCooldown);
            Assert.IsTrue(valid);
        }
    }
}
