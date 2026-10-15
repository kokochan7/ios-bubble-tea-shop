using BubbleTea.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shows the upgrade menu when the player steps onto the upgrade desk and pauses the game
    /// while it's open. The close button hides it and unpauses; it opens again the next time
    /// the player walks onto the desk. Put this on an object that stays active, and point it at the panel.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel to show and hide (a child of this object).")]
        private GameObject panel;

        [SerializeField, Tooltip("Closes the menu and unpauses the game.")]
        private Button closeButton;

        private bool dismissed;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            if (panel == null) return;
            bool inside = UpgradeZone.AnyPlayerInside();
            if (!inside) dismissed = false;
            bool show = inside && !dismissed;
            if (panel.activeSelf == show) return;
            panel.SetActive(show);
            Time.timeScale = show ? 0f : 1f;
        }

        /// <summary>Hides the menu until the player steps off the desk and back on.</summary>
        public void Close() => dismissed = true;

        private void OnDisable()
        {
            if (panel != null && panel.activeSelf) Time.timeScale = 1f;
        }
    }
}
