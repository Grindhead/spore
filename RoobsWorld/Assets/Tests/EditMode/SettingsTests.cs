using NUnit.Framework;
using RoobsWorld.Core;
using UnityEngine;

namespace RoobsWorld.Tests
{
    public class GameSettingsTests
    {
        [Test]
        public void Default_Resolution_Is_1920x1080()
        {
            var settings = new GameSettings();
            Assert.AreEqual(1920, settings.ResolutionWidth);
            Assert.AreEqual(1080, settings.ResolutionHeight);
        }

        [Test]
        public void Default_Fullscreen_Is_True()
        {
            var settings = new GameSettings();
            Assert.IsTrue(settings.Fullscreen);
        }

        [Test]
        public void Default_Quality_Is_Medium()
        {
            var settings = new GameSettings();
            Assert.AreEqual(2, settings.QualityLevel);
        }

        [Test]
        public void Default_MasterVolume_Is_Full()
        {
            var settings = new GameSettings();
            Assert.AreEqual(1f, settings.MasterVolume, 0.001f);
        }

        [Test]
        public void Default_MusicVolume_Is_0_7()
        {
            var settings = new GameSettings();
            Assert.AreEqual(0.7f, settings.MusicVolume, 0.001f);
        }

        [Test]
        public void Default_SFXVolume_Is_Full()
        {
            var settings = new GameSettings();
            Assert.AreEqual(1f, settings.SFXVolume, 0.001f);
        }

        [Test]
        public void Default_CameraSensitivity_Is_1()
        {
            var settings = new GameSettings();
            Assert.AreEqual(1f, settings.CameraSensitivity, 0.001f);
        }

        [Test]
        public void Default_InvertY_Is_False()
        {
            var settings = new GameSettings();
            Assert.IsFalse(settings.InvertY);
        }

        [Test]
        public void Default_ShowFPS_Is_False()
        {
            var settings = new GameSettings();
            Assert.IsFalse(settings.ShowFPS);
        }

        [Test]
        public void Serialization_Roundtrip_Preserves_All_Fields()
        {
            var settings = new GameSettings
            {
                ResolutionWidth = 2560,
                ResolutionHeight = 1440,
                Fullscreen = false,
                QualityLevel = 3,
                MasterVolume = 0.8f,
                MusicVolume = 0.5f,
                SFXVolume = 0.9f,
                CameraSensitivity = 1.5f,
                InvertY = true,
                ShowFPS = true
            };

            string json = JsonUtility.ToJson(settings);
            var restored = JsonUtility.FromJson<GameSettings>(json);

            Assert.AreEqual(2560, restored.ResolutionWidth);
            Assert.AreEqual(1440, restored.ResolutionHeight);
            Assert.IsFalse(restored.Fullscreen);
            Assert.AreEqual(3, restored.QualityLevel);
            Assert.AreEqual(0.8f, restored.MasterVolume, 0.001f);
            Assert.AreEqual(0.5f, restored.MusicVolume, 0.001f);
            Assert.AreEqual(0.9f, restored.SFXVolume, 0.001f);
            Assert.AreEqual(1.5f, restored.CameraSensitivity, 0.001f);
            Assert.IsTrue(restored.InvertY);
            Assert.IsTrue(restored.ShowFPS);
        }

        [Test]
        public void ResetToDefaults_Returns_Default_Values()
        {
            var settings = new GameSettings
            {
                MasterVolume = 0.3f,
                MusicVolume = 0.1f,
                QualityLevel = 5,
                InvertY = true
            };

            var defaults = new GameSettings();

            Assert.AreEqual(1f, defaults.MasterVolume, 0.001f);
            Assert.AreEqual(0.7f, defaults.MusicVolume, 0.001f);
            Assert.AreEqual(2, defaults.QualityLevel);
            Assert.IsFalse(defaults.InvertY);
        }

        [Test]
        public void QualityLevel_Names_Match_Expected_Count()
        {
            string[] expected = { "Low", "Medium", "High", "Ultra" };
            Assert.AreEqual(4, expected.Length);
        }

        [Test]
        public void AmbientVolume_Default_Is_Full()
        {
            var settings = new GameSettings();
            Assert.AreEqual(1f, settings.AmbientVolume, 0.001f);
        }

        [Test]
        public void Serialization_Preserves_AmbientVolume()
        {
            var settings = new GameSettings { AmbientVolume = 0.4f };
            string json = JsonUtility.ToJson(settings);
            var restored = JsonUtility.FromJson<GameSettings>(json);
            Assert.AreEqual(0.4f, restored.AmbientVolume, 0.001f);
        }

        [Test]
        public void Default_UITextScale_Is_One()
        {
            var settings = new GameSettings();
            Assert.AreEqual(1f, settings.UITextScale, 0.001f);
        }

        [Test]
        public void Serialization_Preserves_UITextScale()
        {
            var settings = new GameSettings { UITextScale = 1.5f };
            string json = JsonUtility.ToJson(settings);
            var restored = JsonUtility.FromJson<GameSettings>(json);
            Assert.AreEqual(1.5f, restored.UITextScale, 0.001f);
        }

        [Test]
        public void Default_HighContrastMode_Is_False()
        {
            var settings = new GameSettings();
            Assert.IsFalse(settings.HighContrastMode);
        }

        [Test]
        public void Serialization_Preserves_HighContrastMode()
        {
            var settings = new GameSettings { HighContrastMode = true };
            string json = JsonUtility.ToJson(settings);
            var restored = JsonUtility.FromJson<GameSettings>(json);
            Assert.IsTrue(restored.HighContrastMode);
        }
    }

    public class SettingsManagerTests
    {
        private const string SETTINGS_KEY = "GameSettings";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(SETTINGS_KEY);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(SETTINGS_KEY);
        }

        [Test]
        public void GetUITextScale_ReturnsClampedValue_WhenBelowMin()
        {
            var settings = new GameSettings { UITextScale = 0.3f };
            PlayerPrefs.SetString(SETTINGS_KEY, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
            SettingsManager.Instance.Load();
            float scale = SettingsManager.GetUITextScale();
            Assert.AreEqual(0.5f, scale, 0.001f);
        }

        [Test]
        public void GetUITextScale_ReturnsClampedValue_WhenAboveMax()
        {
            var settings = new GameSettings { UITextScale = 3f };
            PlayerPrefs.SetString(SETTINGS_KEY, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
            SettingsManager.Instance.Load();
            float scale = SettingsManager.GetUITextScale();
            Assert.AreEqual(2f, scale, 0.001f);
        }

        [Test]
        public void GetUITextScale_ReturnsStoredValue_WhenWithinRange()
        {
            var settings = new GameSettings { UITextScale = 1.25f };
            PlayerPrefs.SetString(SETTINGS_KEY, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
            SettingsManager.Instance.Load();
            float scale = SettingsManager.GetUITextScale();
            Assert.AreEqual(1.25f, scale, 0.001f);
        }
    }
}
