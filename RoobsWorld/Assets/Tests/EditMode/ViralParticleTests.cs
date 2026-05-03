using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Stages.Cell;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for the Viral Particle feature and VirusOutbreak improvements.
    /// Runtime physics / contact behaviour requires PlayMode tests.
    /// </summary>
    public class ViralParticleTests
    {
        // ---- BalanceConstants sanity ----

        [Test]
        public void ViralParticleSpawnCount_IsPositive()
        {
            Assert.Greater(BalanceConstants.ViralParticleSpawnCount, 0);
        }

        [Test]
        public void ViralParticleScale_SmallerThanTypicalCell()
        {
            // Normal cell is scale 1; viral particles must be noticeably smaller
            Assert.Less(BalanceConstants.ViralParticleScale, 0.5f);
            Assert.Greater(BalanceConstants.ViralParticleScale, 0f);
        }

        [Test]
        public void ViralParticleSpeed_Positive()
        {
            Assert.Greater(BalanceConstants.ViralParticleSpeed, 0f);
        }

        [Test]
        public void ViralParticleLifetime_IsSubstantial()
        {
            // Long enough to cross the arena but not infinite
            Assert.Greater(BalanceConstants.ViralParticleLifetimeSeconds, 5f);
        }

        [Test]
        public void ViralParticleDrainOnPlayer_IsSmallButNoticeable()
        {
            // Should be a meaningful hit but not instantly lethal
            Assert.Greater(BalanceConstants.ViralParticleDrainOnPlayer, 0f);
            Assert.Less(BalanceConstants.ViralParticleDrainOnPlayer, 5f);
        }

        [Test]
        public void ViralParticleArmouredReduction_ReducesDamage()
        {
            // Reduction factor < 1 means Armoured trait helps
            Assert.Less(BalanceConstants.ViralParticleArmouredDrainReduction, 1f);
            Assert.Greater(BalanceConstants.ViralParticleArmouredDrainReduction, 0f);
        }

        [Test]
        public void ViralParticleDrainOnOrganism_PositiveAndHigherThanPlayerDrain()
        {
            // NPCs should take more damage (they can't dodge like the player)
            Assert.Greater(BalanceConstants.ViralParticleDrainOnOrganism, 0f);
            Assert.Greater(BalanceConstants.ViralParticleDrainOnOrganism,
                           BalanceConstants.ViralParticleDrainOnPlayer);
        }

        [Test]
        public void ArmouredDrain_IsLessThanBaseDrain()
        {
            float reduced = BalanceConstants.ViralParticleDrainOnPlayer
                          * BalanceConstants.ViralParticleArmouredDrainReduction;
            Assert.Less(reduced, BalanceConstants.ViralParticleDrainOnPlayer);
        }

        // ---- Localization keys ----

        [Test]
        public void LocalizationKey_Infected_Exists()
        {
            string val = Localization.Get("cell.virus.infected");
            Assert.IsFalse(string.IsNullOrEmpty(val));
            Assert.AreNotEqual("cell.virus.infected", val);
        }

        [Test]
        public void LocalizationKey_ArmouredResist_Exists()
        {
            string val = Localization.Get("cell.virus.armoured_resist");
            Assert.IsFalse(string.IsNullOrEmpty(val));
        }

        // ---- Biome localization keys ----

        [Test]
        public void LocalizationKeys_AllBiomes_Exist()
        {
            var keys = new[]
            {
                "cell.biome.openwater",
                "cell.biome.algaeforest",
                "cell.biome.deepzone",
                "cell.biome.toxiczone",
            };
            foreach (string key in keys)
            {
                string val = Localization.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Biome key '{key}' missing");
                Assert.AreNotEqual(key, val, $"Biome key '{key}' returns key instead of translation");
            }
        }

        [Test]
        public void BiomeLocalizationKey_MatchesBiomeTypeToLower()
        {
            // Ensures UpdateBiomeLabel()'s string format "cell.biome." + biome.ToString().ToLower()
            // matches what's in the localization table.
            foreach (Biome.BiomeType bt in System.Enum.GetValues(typeof(Biome.BiomeType)))
            {
                string key = "cell.biome." + bt.ToString().ToLower();
                string val = Localization.Get(key);
                Assert.AreNotEqual(key, val,
                    $"BiomeType.{bt} has no localization at key '{key}'");
            }
        }

        // ---- GameEvents struct ----

        [Test]
        public void CellPlayerInfectedEvent_CanBeCreatedAndRead()
        {
            var evt = new CellPlayerInfectedEvent { Drain = 0.6f };
            Assert.AreEqual(0.6f, evt.Drain, 0.001f);
        }
    }
}
