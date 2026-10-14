using BubbleTea.Building;
using BubbleTea.Economy;
using BubbleTea.Saving;
using BubbleTea.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Game master tools for testing faster, opened from the MENU panel: free money, building
    /// every pad at once, instant pads and maxed upgrades. Only shown in the Editor and in
    /// Development builds, so players of the App Store version never see it.
    /// </summary>
    public class GmMenu : MonoBehaviour
    {
        [SerializeField, Tooltip("Opens the GM panel (hidden outside the Editor and Development builds).")]
        private Button openButton;

        [SerializeField, Tooltip("The GM panel.")]
        private GameObject panel;

        [SerializeField] private Button moneyButton;
        [SerializeField] private Button buildAllButton;
        [SerializeField] private Button instantButton;
        [SerializeField] private Text instantLabel;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private Button closeButton;

        [SerializeField, Tooltip("Money added by the money button.")]
        private int moneyAmount = 10000;

        [SerializeField] private Wallet wallet;
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private UpgradeManager upgradeManager;

        private void Awake()
        {
            if (openButton != null)
            {
                openButton.gameObject.SetActive(Debug.isDebugBuild);
                openButton.onClick.AddListener(() => SetOpen(true));
            }
            if (closeButton != null) closeButton.onClick.AddListener(() => SetOpen(false));
            if (moneyButton != null) moneyButton.onClick.AddListener(GiveMoney);
            if (buildAllButton != null) buildAllButton.onClick.AddListener(BuildEverything);
            if (instantButton != null) instantButton.onClick.AddListener(ToggleInstant);
            if (upgradesButton != null) upgradesButton.onClick.AddListener(MaxUpgrades);
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            if (panel != null) panel.SetActive(open);
            Refresh();
        }

        private void GiveMoney()
        {
            if (wallet != null) wallet.Add(moneyAmount);
        }

        // Builds every pad in the shop, hidden ones included, as if each had been paid for.
        private void BuildEverything()
        {
            if (saveManager == null) return;
            foreach (UnlockPad pad in saveManager.Pads)
            {
                if (pad != null && !pad.IsUnlocked) pad.BuildNow();
            }
            saveManager.Save();
        }

        private void ToggleInstant()
        {
            UnlockPad.InstantBuild = !UnlockPad.InstantBuild;
            Refresh();
        }

        private void MaxUpgrades()
        {
            if (upgradeManager == null) return;
            foreach (UpgradeDefinition upgrade in upgradeManager.All)
            {
                if (upgrade != null) upgradeManager.SetLevel(upgrade, upgrade.MaxLevel);
            }
        }

        private void Refresh()
        {
            if (instantLabel != null) instantLabel.text = UnlockPad.InstantBuild ? "Instant Build: ON" : "Instant Build: OFF";
        }
    }
}
