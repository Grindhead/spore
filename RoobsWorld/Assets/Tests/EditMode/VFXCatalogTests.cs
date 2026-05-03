using NUnit.Framework;
using RoobsWorld.Utils;

namespace RoobsWorld.Tests
{
    public class VFXCatalogTests
    {
        [Test]
        public void VFXType_Has_Expected_Count()
        {
            int count = System.Enum.GetValues(typeof(VFXType)).Length;
            Assert.GreaterOrEqual(count, 12);
        }

        [Test]
        public void GetConfig_Returns_NonNull_For_All_Types()
        {
            foreach (VFXType vfxType in System.Enum.GetValues(typeof(VFXType)))
            {
                var config = VFXCatalog.GetConfig(vfxType);
                Assert.IsNotNull(config, $"Missing config for {vfxType}");
            }
        }

        [Test]
        public void Explosion_Has_Positive_Lifetime()
        {
            var config = VFXCatalog.GetConfig(VFXType.Explosion);
            Assert.Greater(config.Lifetime, 0f);
        }

        [Test]
        public void Explosion_Has_Particles()
        {
            var config = VFXCatalog.GetConfig(VFXType.Explosion);
            Assert.Greater(config.ParticleCount, 0);
        }

        [Test]
        public void HealGlow_Is_Looping()
        {
            var config = VFXCatalog.GetConfig(VFXType.HealGlow);
            Assert.IsTrue(config.Loop);
        }

        [Test]
        public void SpeedTrail_Is_Not_Looping()
        {
            var config = VFXCatalog.GetConfig(VFXType.SpeedTrail);
            Assert.IsFalse(config.Loop);
        }

        [Test]
        public void All_Configs_Have_Positive_ParticleCount()
        {
            foreach (VFXType vfxType in System.Enum.GetValues(typeof(VFXType)))
            {
                var config = VFXCatalog.GetConfig(vfxType);
                Assert.Greater(config.ParticleCount, 0, $"{vfxType} has zero particles");
            }
        }

        [Test]
        public void All_Configs_Have_Positive_StartSpeed()
        {
            foreach (VFXType vfxType in System.Enum.GetValues(typeof(VFXType)))
            {
                var config = VFXCatalog.GetConfig(vfxType);
                Assert.GreaterOrEqual(config.StartSpeed, 0f, $"{vfxType} has negative start speed");
            }
        }

        [Test]
        public void DamageHit_StartSize_Is_Small()
        {
            var config = VFXCatalog.GetConfig(VFXType.DamageHit);
            Assert.LessOrEqual(config.StartSize, 1f);
        }

        [Test]
        public void TerraformPulse_Has_Large_Radius()
        {
            var config = VFXCatalog.GetConfig(VFXType.TerraformPulse);
            Assert.Greater(config.StartSize, 1f);
        }

        [Test]
        public void SpawnBurst_Has_Short_Lifetime()
        {
            var config = VFXCatalog.GetConfig(VFXType.SpawnBurst);
            Assert.LessOrEqual(config.Lifetime, 2f);
        }

        [Test]
        public void DeathPoof_Has_Short_Lifetime()
        {
            var config = VFXCatalog.GetConfig(VFXType.DeathPoof);
            Assert.LessOrEqual(config.Lifetime, 2f);
        }
    }
}
