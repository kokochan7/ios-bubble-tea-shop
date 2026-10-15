using BubbleTea.Core;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The title screen the game opens on. Play opens the shop you were in last (or the first
    /// shop on a new game); Settings opens the sound, music and vibration switches (a
    /// <see cref="SettingsMenu"/> on the same screen). Built by Bubble Tea > Build Main Menu Scene.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        /// <summary>The menu scene's name (it must be first in File > Build Profiles).</summary>
        public const string SceneName = "MainMenu";

        [SerializeField, Tooltip("Opens the game.")]
        private Button playButton;

        [SerializeField, Tooltip("Says PLAY on a new game and CONTINUE when there's a save.")]
        private Text playLabel;

        [SerializeField, Tooltip("Bobs gently up and down (the big cup).")]
        private RectTransform bobbing;

        [SerializeField, Tooltip("Breathes gently in and out to draw the eye (the Play button).")]
        private RectTransform pulsing;

        [SerializeField, Tooltip("Background bubbles that drift slowly upward and wrap around.")]
        private RectTransform[] floaters;

        private Vector2 bobStart;
        private float[] floatSpeeds;

        /// <summary>True when the menu scene is in the build, so the game can go back to it.</summary>
        public static bool InBuild => Application.CanStreamedLevelBeLoaded(SceneName);

        /// <summary>Leaves the shop for the title screen. Save first.</summary>
        public static void Open()
        {
            Time.timeScale = 1f;
            Stats.Clear(); // the next shop applies its own upgrades and rating
            SceneManager.LoadScene(SceneName);
        }

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (bobbing != null) bobStart = bobbing.anchoredPosition;
            if (floaters != null)
            {
                floatSpeeds = new float[floaters.Length];
                for (int i = 0; i < floaters.Length; i++) floatSpeeds[i] = 0.012f + 0.02f * Mathf.Repeat(i * 0.618f, 1f);
            }
        }

        private void Start()
        {
            Time.timeScale = 1f; // in case the last game ended paused
            if (playLabel != null) playLabel.text = HasSave() ? "CONTINUE" : "PLAY";
        }

        private void Update()
        {
            if (bobbing != null) bobbing.anchoredPosition = bobStart + new Vector2(0f, Mathf.Sin(Time.time * 2f) * 18f);
            if (pulsing != null) pulsing.localScale = Vector3.one * (1f + Mathf.Sin(Time.time * 3f) * 0.03f);
            DriftBubbles();
        }

        // Each bubble rises slowly (as a fraction of the screen per second), sways a little and
        // starts again at the bottom once it floats off the top.
        private void DriftBubbles()
        {
            if (floaters == null || floatSpeeds == null) return;
            for (int i = 0; i < floaters.Length; i++)
            {
                RectTransform bubble = floaters[i];
                if (bubble == null) continue;
                Vector2 anchor = bubble.anchorMin;
                anchor.y += floatSpeeds[i] * Time.deltaTime;
                if (anchor.y > 1.08f) anchor.y = -0.08f;
                anchor.x += Mathf.Sin(Time.time * 0.8f + i) * 0.004f * Time.deltaTime;
                bubble.anchorMin = bubble.anchorMax = anchor;
            }
        }

        private static bool HasSave()
        {
            return PlayerPrefs.HasKey(SaveManager.LastShopKey) || SaveManager.AllSaveFiles().Length > 0;
        }

        private void Play()
        {
            string target = PlayerPrefs.GetString(SaveManager.LastShopKey, "");
            if (string.IsNullOrEmpty(target) || !Application.CanStreamedLevelBeLoaded(target)) target = SaveManager.FirstShopScene;
            Stats.Clear();
            if (Application.CanStreamedLevelBeLoaded(target))
            {
                SceneManager.LoadScene(target);
            }
            else if (SceneManager.sceneCountInBuildSettings > 1)
            {
                SceneManager.LoadScene(1); // the shop right after the menu
            }
            else
            {
                Debug.LogWarning("Bubble Tea: no shop scene in the build. Click Bubble Tea > Build Prototype Scene.");
            }
        }
    }
}
