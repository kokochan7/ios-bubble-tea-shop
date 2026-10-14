using BubbleTea.Rating;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shown when the shop's rating drops to 0 stars: the game is lost. The save is deleted
    /// right away and the game pauses; Start Over begins a brand-new game in the first shop.
    /// </summary>
    public class GameOverPopup : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel to show.")]
        private GameObject panel;

        [SerializeField] private Button startOverButton;

        [SerializeField] private ShopRating rating;

        [SerializeField] private SaveManager saveManager;

        private void Awake()
        {
            Time.timeScale = 1f; // in case the last game ended paused
            if (panel != null) panel.SetActive(false);
            if (startOverButton != null) startOverButton.onClick.AddListener(StartOver);
        }

        private void OnEnable()
        {
            if (rating != null) rating.ShopClosed += Show;
        }

        private void OnDisable()
        {
            if (rating != null) rating.ShopClosed -= Show;
        }

        private void Show()
        {
            if (saveManager != null) saveManager.EraseSave();
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void StartOver()
        {
            Time.timeScale = 1f;
            if (saveManager != null) saveManager.ResetProgress();
        }
    }
}
