using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// Loops this shop's background music, fading in when the scene starts. Each shop scene
    /// has its own track, so the music changes when you take the bus to another shop.
    /// The Music button in the MENU turns it off and on.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f), Tooltip("Music volume.")]
        private float volume = 0.35f;

        [SerializeField, Tooltip("Seconds to fade in and out.")]
        private float fadeSeconds = 1.5f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        private void OnEnable() => GameSettings.Changed += Refresh;
        private void OnDisable() => GameSettings.Changed -= Refresh;

        private void Start() => Refresh();

        private void Refresh()
        {
            if (source == null || source.clip == null) return;
            if (GameSettings.MusicOn && !source.isPlaying) source.Play();
        }

        private void Update()
        {
            if (source == null || !source.isPlaying) return;
            float target = GameSettings.MusicOn ? volume : 0f;
            float step = volume / Mathf.Max(fadeSeconds, 0.01f) * Time.unscaledDeltaTime;
            source.volume = Mathf.MoveTowards(source.volume, target, step);
            if (!GameSettings.MusicOn && source.volume <= 0f) source.Pause();
        }
    }
}
