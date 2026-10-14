using BubbleTea.Core;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The settings panel opened by the button in the corner: sound and vibration on/off,
    /// and a reset button that asks for a second tap before wiping your progress.
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

        [SerializeField, Tooltip("Turns vibration on and off.")]
        private Button vibrationButton;

        [SerializeField] private Text vibrationLabel;

        [SerializeField, Tooltip("Wipes the save after a second tap.")]
        private Button resetButton;

        [SerializeField] private Text resetLabel;

        [SerializeField, Tooltip("The save manager, used by the reset button.")]
        private SaveManager saveManager;

        private bool confirmingReset;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (soundButton != null) soundButton.onClick.AddListener(() => GameSettings.SoundOn = !GameSettings.SoundOn);
            if (vibrationButton != null) vibrationButton.onClick.AddListener(() => GameSettings.VibrationOn = !GameSettings.VibrationOn);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetPressed);
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
            if (vibrationLabel != null) vibrationLabel.text = GameSettings.VibrationOn ? "Vibration: ON" : "Vibration: OFF";
            if (resetLabel != null) resetLabel.text = confirmingReset ? "Tap again to erase!" : "Reset Progress";
        }
    }
}
