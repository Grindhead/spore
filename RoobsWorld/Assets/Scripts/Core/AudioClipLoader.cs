using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Loads music and ambient clips from <c>Resources/Music</c> and SFX from <c>Resources/SFX</c> by catalog ID, registering with <see cref="AdaptiveMusicController"/> and <see cref="AudioManager"/>.
    /// Place music in <c>Assets/Resources/Music/</c> and SFX in <c>Assets/Resources/SFX/</c> with names matching <see cref="MusicTrackCatalog"/> IDs (e.g. cell_exploration, ui_click).
    /// Missing clips receive quiet procedural fallback cues so gameplay feedback never becomes a silent no-op.
    /// </summary>
    /// <remarks>
    /// Music/ambient: file names match track IDs (cell_exploration, ambient_underwater, etc.).
    /// SFX: file names match GetSFXId (ui_click, ui_hover, attack_hit, creature_death, food_eaten, stage_transition, etc.).
    /// Add this component to the same GameObject as <see cref="AdaptiveMusicController"/> so it runs after the controller's Awake.
    /// </remarks>
    public class AudioClipLoader : MonoBehaviour
    {
        private const string MusicResourcesPath = "Music/";
        private const string SFXResourcesPath = "SFX/";
        [SerializeField] private bool _generateFallbackClips = true;

        private void Start()
        {
            var catalog = new MusicTrackCatalog();
            int musicRegistered = LoadMusicAndAmbient(catalog);
            int sfxRegistered = LoadSFX(catalog);
            Debug.Log($"[AudioClipLoader] Music/ambient: {musicRegistered}; SFX: {sfxRegistered}");
        }

        private int LoadMusicAndAmbient(MusicTrackCatalog catalog)
        {
            var controller = GetComponent<AdaptiveMusicController>();
            if (controller == null)
            {
                Debug.LogWarning("[AudioClipLoader] AdaptiveMusicController not found on same GameObject; skipping music load.");
                return 0;
            }

            int registered = 0;
            foreach (string trackId in catalog.GetAllMusicTrackIds())
            {
                var clip = Resources.Load<AudioClip>(MusicResourcesPath + trackId);
                if (clip == null && _generateFallbackClips)
                    clip = ProceduralAudioFactory.CreateMusicFallback(trackId);
                if (clip != null)
                {
                    controller.RegisterClip(trackId, clip);
                    registered++;
                }
            }
            foreach (string ambientId in catalog.GetAllAmbientIds())
            {
                var clip = Resources.Load<AudioClip>(MusicResourcesPath + ambientId);
                if (clip == null && _generateFallbackClips)
                    clip = ProceduralAudioFactory.CreateMusicFallback(ambientId, ambient: true);
                if (clip != null)
                {
                    controller.RegisterClip(ambientId, clip);
                    registered++;
                }
            }
            return registered;
        }

        private int LoadSFX(MusicTrackCatalog catalog)
        {
            if (!AudioManager.HasInstance) return 0;
            int registered = 0;
            foreach (SFXType sfx in System.Enum.GetValues(typeof(SFXType)))
            {
                string id = catalog.GetSFXId(sfx);
                if (string.IsNullOrEmpty(id)) continue;
                var clip = Resources.Load<AudioClip>(SFXResourcesPath + id);
                if (clip == null && _generateFallbackClips)
                    clip = ProceduralAudioFactory.CreateSfxFallback(sfx);
                if (clip != null)
                {
                    AudioManager.Instance.RegisterSFXClip(sfx, clip);
                    registered++;
                }
            }
            return registered;
        }
    }
}
