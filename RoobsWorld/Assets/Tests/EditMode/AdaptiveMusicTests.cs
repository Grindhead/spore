using NUnit.Framework;
using RoobsWorld.Core;

namespace RoobsWorld.Tests
{
    public class MusicTrackCatalogTests
    {
        [Test]
        public void GetStageTrack_Returns_Correct_Track_Id_For_Cell()
        {
            var catalog = new MusicTrackCatalog();
            string trackId = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Exploration);
            Assert.AreEqual("cell_exploration", trackId);
        }

        [Test]
        public void GetStageTrack_Unsupported_Stage_ReturnsNull()
        {
            var catalog = new MusicTrackCatalog();
            string trackId = catalog.GetStageTrack((GameStageType)999, MusicMood.Exploration);
            Assert.IsNull(trackId);
        }

        [Test]
        public void GetStageTrack_Returns_Null_For_None_Stage()
        {
            var catalog = new MusicTrackCatalog();
            string trackId = catalog.GetStageTrack(GameStageType.None, MusicMood.Exploration);
            Assert.IsNull(trackId);
        }

        [Test]
        public void Cell_Stage_Has_Exploration_Track()
        {
            var catalog = new MusicTrackCatalog();
            string trackId = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Exploration);
            Assert.IsNotNull(trackId);
        }

        [Test]
        public void Cell_Stage_Has_Combat_Track()
        {
            var catalog = new MusicTrackCatalog();
            string trackId = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Combat);
            Assert.IsNotNull(trackId);
        }

        [Test]
        public void Cell_Stage_Has_Ambient_Id()
        {
            var catalog = new MusicTrackCatalog();
            string ambientId = catalog.GetAmbientId(GameStageType.Cell);
            Assert.IsNotNull(ambientId);
        }

        [Test]
        public void Unsupported_Stage_ReturnsNullAmbient()
        {
            var catalog = new MusicTrackCatalog();
            string ambientId = catalog.GetAmbientId((GameStageType)999);
            Assert.IsNull(ambientId);
        }

        [Test]
        public void GetSFXId_Returns_Correct_Id()
        {
            var catalog = new MusicTrackCatalog();
            Assert.AreEqual("ui_click", catalog.GetSFXId(SFXType.UIClick));
            Assert.AreEqual("ui_hover", catalog.GetSFXId(SFXType.UIHover));
            Assert.AreEqual("attack_hit", catalog.GetSFXId(SFXType.AttackHit));
            Assert.AreEqual("creature_death", catalog.GetSFXId(SFXType.CreatureDeath));
        }

        [Test]
        public void GetSFXId_CellEat_ReturnsDistinctIdsPerFoodType()
        {
            var catalog = new MusicTrackCatalog();
            string plant  = catalog.GetSFXId(SFXType.FoodEatenPlant);
            string meat   = catalog.GetSFXId(SFXType.FoodEatenMeat);
            string toxin  = catalog.GetSFXId(SFXType.FoodEatenToxin);

            Assert.AreEqual("food_eaten_plant", plant);
            Assert.AreEqual("food_eaten_meat",  meat);
            Assert.AreEqual("food_eaten_toxin", toxin);

            // All three must be distinct from each other and from the generic fallback
            Assert.AreNotEqual(plant, meat);
            Assert.AreNotEqual(plant, toxin);
            Assert.AreNotEqual(meat,  toxin);
            Assert.AreNotEqual("food_eaten", plant);
        }

        [Test]
        public void GetSFXId_EvolutionComplete_ReturnsCorrectId()
        {
            var catalog = new MusicTrackCatalog();
            Assert.AreEqual("evolution_complete", catalog.GetSFXId(SFXType.EvolutionComplete));
        }

        [Test]
        public void GetSFXId_MutationChoiceShown_ReturnsCorrectId()
        {
            var catalog = new MusicTrackCatalog();
            Assert.AreEqual("mutation_choice", catalog.GetSFXId(SFXType.MutationChoiceShown));
        }

        [Test]
        public void MusicMood_Has_Expected_Values()
        {
            Assert.AreEqual(0, (int)MusicMood.Exploration);
            Assert.AreEqual(1, (int)MusicMood.Combat);
            Assert.AreEqual(2, (int)MusicMood.Social);
            Assert.AreEqual(3, (int)MusicMood.Tense);
            Assert.AreEqual(4, (int)MusicMood.Victory);
        }

        [Test]
        public void SFXType_Has_Expected_Count()
        {
            int count = System.Enum.GetValues(typeof(SFXType)).Length;
            Assert.GreaterOrEqual(count, 10);
        }

        [Test]
        public void Cell_Has_Tense_Track_For_Predator_Proximity()
        {
            var catalog = new MusicTrackCatalog();
            string tenseId = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Tense);
            Assert.AreEqual("cell_tense", tenseId,
                "Predator-nearby path uses MusicMood.Tense; cell_tense must be catalogued");
        }

        [Test]
        public void Cell_Has_Victory_Track_For_Goal_Completion()
        {
            var catalog = new MusicTrackCatalog();
            string victoryId = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Victory);
            Assert.AreEqual("cell_victory", victoryId,
                "Goal completion calls NotifyVictory(); cell_victory must be catalogued");
        }

        [Test]
        public void Cell_Combat_And_Tense_Tracks_Are_Distinct()
        {
            var catalog = new MusicTrackCatalog();
            string combat = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Combat);
            string tense  = catalog.GetStageTrack(GameStageType.Cell, MusicMood.Tense);
            Assert.AreNotEqual(combat, tense,
                "Chase (combat) and proximity-warning (tense) should use different tracks");
        }

        [Test]
        public void MoodPriority_Ranks_Combat_Higher_Than_Exploration()
        {
            Assert.Greater(
                AdaptiveMusicController.GetMoodPriority(MusicMood.Combat),
                AdaptiveMusicController.GetMoodPriority(MusicMood.Exploration));
        }

        [Test]
        public void ShouldBlockMoodDowngrade_BlocksLowerPriority_WhenTemporaryActive()
        {
            bool blocked = AdaptiveMusicController.ShouldBlockMoodDowngrade(
                currentMood: MusicMood.Victory,
                temporaryMoodTimer: 1f,
                requestedMood: MusicMood.Exploration);
            Assert.IsTrue(blocked);
        }

        [Test]
        public void ShouldBlockMoodDowngrade_AllowsHigherPriority_WhenTemporaryActive()
        {
            bool blocked = AdaptiveMusicController.ShouldBlockMoodDowngrade(
                currentMood: MusicMood.Victory,
                temporaryMoodTimer: 1f,
                requestedMood: MusicMood.Combat);
            Assert.IsFalse(blocked);
        }

        [Test]
        public void ProceduralAudioFactory_CreatesFallbackClipForEverySfx()
        {
            foreach (SFXType type in System.Enum.GetValues(typeof(SFXType)))
            {
                var clip = ProceduralAudioFactory.CreateSfxFallback(type);

                Assert.IsNotNull(clip, $"{type} should have a fallback clip.");
                Assert.Greater(clip.samples, 64);
                Assert.AreEqual(ProceduralAudioFactory.SampleRate, clip.frequency);
            }
        }

        [Test]
        public void ProceduralAudioFactory_CreatesMusicAndAmbientFallbacks()
        {
            var music = ProceduralAudioFactory.CreateMusicFallback("cell_combat");
            var ambient = ProceduralAudioFactory.CreateMusicFallback("ambient_underwater", ambient: true);

            Assert.IsNotNull(music);
            Assert.IsNotNull(ambient);
            Assert.Greater(ambient.length, music.length);
            Assert.AreEqual(1, music.channels);
        }
    }
}
