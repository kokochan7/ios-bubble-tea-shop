using BubbleTea.Core;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The settings panel opened by the button in the corner: sound, music and vibration on/off,
    /// a button back to the main menu, and a reset button that asks for a second tap before
    /// wiping your progress. The main menu uses it too, without the shop-only buttons.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel to show and hide.")]
        private GameObject panel;

        [SerializeField, Tooltip("Opens and closes the panel.")]
        private Button openButton;

        [SerializeField, Tooltip("Closes the panel.")]
        private Button closeButton;

        [SerializeField, Tooltip("Turns sound on and off.")]
        private Button soundButton;

        [SerializeField] private Text soundLabel;

        [SerializeField, Tooltip("Turns the background music on and off.")]
        private Button musicButton;

        [SerializeField] private Text musicLabel;

        [SerializeField, Tooltip("Turns vibration on and off.")]
        private Button vibrationButton;

        [SerializeField] private Text vibrationLabel;

        [SerializeField, Tooltip("Wipes the save after a second tap.")]
        private Button resetButton;

        [SerializeField] private Text resetLabel;

        [SerializeField, Tooltip("Saves and goes back to the main menu (hidden if the menu scene isn't built).")]
        private Button mainMenuButton;

        [SerializeField, Tooltip("The save manager, used by the reset and main menu buttons.")]
        private SaveManager saveManager;

        private bool confirmingReset;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (soundButton != null) soundButton.onClick.AddListener(() => GameSettings.SoundOn = !GameSettings.SoundOn);
            if (musicButton != null) musicButton.onClick.AddListener(() => GameSettings.MusicOn = !GameSettings.MusicOn);
            if (vibrationButton != null) vibrationButton.onClick.AddListener(() => GameSettings.VibrationOn = !GameSettings.VibrationOn);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetPressed);
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(GoToMainMenu);
                mainMenuButton.gameObject.SetActive(MainMenu.InBuild);
            }
        }

        private void GoToMainMenu()
        {
            if (saveManager != null) saveManager.Save();
            MainMenu.Open();
        }

        private void OnEnable()
        {
            GameSettings.Changed += Refresh;
            if (panel != null) panel.SetActive(false);
            Refresh();
        }

        private void OnDisable() => GameSettings.Changed -= Refresh;

        private void Toggle()
        {
            if (panel == null) return;
            panel.SetActive(!panel.activeSelf);
            confirmingReset = false;
            Refresh();
        }

        private void Close()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnResetPressed()
        {
            if (!confirmingReset)
            {
                confirmingReset = true;
                Refresh();
                return;
            }
            if (saveManager != null) saveManager.ResetProgress();
        }

        private void Refresh()
        {
            if (soundLabel != null) soundLabel.text = GameSettings.SoundOn ? "Sound: ON" : "Sound: OFF";
            if (musicLabel != null) musicLabel.text = GameSettings.MusicOn ? "Music: ON" : "Music: OFF";
            if (vibrationLabel != null) vibrationLabel.text = GameSettings.VibrationOn ? "Vibration: ON" : "Vibration: OFF";
            if (resetLabel != null) resetLabel.text = confirmingReset ? "Tap again to erase!" : "Reset Progress";
        }
    }
}
