using BubbleTea.Rating;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shown when the shop's rating drops to 0 stars: the shop is lost. In the first shop the
    /// whole save is deleted and Start Over begins a brand-new game; in a later shop (the Matcha
    /// House) only that shop's save is deleted and Start Over begins that shop again.
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
            if (saveManager != null)
            {
                if (SaveManager.InFirstShop) saveManager.EraseSave();
                else saveManager.EraseThisShop();
            }
            if (panel != null) panel.SetActive(true);
            Time.timeScale = 0f;
        }

        private void StartOver()
        {
            Time.timeScale = 1f;
            if (saveManager == null) return;
            if (SaveManager.InFirstShop) saveManager.ResetProgress();
            else saveManager.RestartThisShop();
        }
    }
}
