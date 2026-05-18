using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Creates lightweight runtime fallback clips so placeholder/missing Resources audio still produces readable feedback.
    /// Authored clips should override these by using the ids in <see cref="MusicTrackCatalog"/>.
    /// </summary>
    public static class ProceduralAudioFactory
    {
        public const int SampleRate = 22050;

        public static AudioClip CreateSfxFallback(SFXType type)
        {
            float frequency = type switch
            {
                SFXType.UIClick => 880f,
                SFXType.UIHover => 660f,
                SFXType.UIBack => 330f,
                SFXType.AttackHit => 150f,
                SFXType.AttackMiss => 280f,
                SFXType.CreatureDeath => 90f,
                SFXType.FoodEatenPlant => 520f,
                SFXType.FoodEatenMeat => 210f,
                SFXType.FoodEatenToxin => 430f,
                SFXType.EvolutionComplete => 740f,
                SFXType.MutationChoiceShown => 620f,
                SFXType.DNAGained => 920f,
                SFXType.LevelUp => 1040f,
                SFXType.StageTransition => 470f,
                SFXType.DiplomacySuccess => 780f,
                SFXType.DiplomacyFail => 180f,
                SFXType.ShieldHit => 260f,
                SFXType.VictoryFanfare => 980f,
                _ => 440f
            };

            float duration = type == SFXType.CreatureDeath || type == SFXType.StageTransition ? 0.42f : 0.18f;
            return CreateTone($"fallback_{type}", frequency, duration, 0.28f, descending: IsLowCue(type));
        }

        public static AudioClip CreateMusicFallback(string id, bool ambient = false)
        {
            float frequency = ambient ? 110f : ResolveMusicFrequency(id);
            float duration = ambient ? 2.5f : 2f;
            return CreateTone($"fallback_{id}", frequency, duration, ambient ? 0.08f : 0.12f, descending: id != null && id.Contains("combat"));
        }

        public static AudioClip CreateTone(string name, float frequency, float durationSeconds, float amplitude, bool descending = false)
        {
            int sampleCount = Mathf.Max(64, Mathf.RoundToInt(SampleRate * Mathf.Max(0.03f, durationSeconds)));
            var data = new float[sampleCount];
            float safeFrequency = Mathf.Clamp(frequency, 40f, 4000f);
            float safeAmplitude = Mathf.Clamp01(amplitude);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float normalized = i / (float)(sampleCount - 1);
                float env = Mathf.Sin(Mathf.PI * normalized);
                float sweep = descending ? Mathf.Lerp(1.18f, 0.72f, normalized) : Mathf.Lerp(0.94f, 1.06f, normalized);
                data[i] = Mathf.Sin(t * safeFrequency * sweep * Mathf.PI * 2f) * safeAmplitude * env;
            }

            var clip = AudioClip.Create(string.IsNullOrWhiteSpace(name) ? "fallback_audio" : name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            clip.hideFlags = HideFlags.HideAndDontSave;
            return clip;
        }

        private static bool IsLowCue(SFXType type)
        {
            return type == SFXType.AttackHit
                || type == SFXType.CreatureDeath
                || type == SFXType.DiplomacyFail
                || type == SFXType.ExplosionLarge
                || type == SFXType.ShieldHit;
        }

        private static float ResolveMusicFrequency(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return 220f;
            if (id.Contains("combat")) return 146f;
            if (id.Contains("tense")) return 185f;
            if (id.Contains("victory")) return 294f;
            if (id.Contains("social")) return 247f;
            if (id.Contains("creature")) return 196f;
            return 220f;
        }
    }
}
