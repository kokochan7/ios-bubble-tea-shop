using BubbleTea.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// One line in the upgrade panel: the upgrade's name, its level, and a buy button
    /// showing the price. The button greys out when you can't afford it.
    /// </summary>
    public class UpgradeRow : MonoBehaviour
    {
        [SerializeField, Tooltip("Upgrade manager in the scene.")]
        private UpgradeManager manager;

        [SerializeField, Tooltip("The upgrade this row sells.")]
        private UpgradeDefinition upgrade;

        [SerializeField, Tooltip("Shows the upgrade's name.")]
        private Text nameLabel;

        [SerializeField, Tooltip("Shows the current level.")]
        private Text levelLabel;

        [SerializeField, Tooltip("Buys the next level.")]
        private Button buyButton;

        [SerializeField, Tooltip("Text on the buy button (the price, or MAX).")]
        private Text priceLabel;

        private void Awake()
        {
            if (buyButton != null) buyButton.onClick.AddListener(Buy);
        }

        private void OnEnable()
        {
            if (manager == null) return;
            manager.Changed += Refresh;
            if (manager.Wallet != null) manager.Wallet.MoneyChanged += OnMoneyChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (manager == null) return;
            manager.Changed -= Refresh;
            if (manager.Wallet != null) manager.Wallet.MoneyChanged -= OnMoneyChanged;
        }

        private void Buy()
        {
            if (manager != null) manager.TryBuy(upgrade);
        }

        private void OnMoneyChanged(int _) => Refresh();

        private void Refresh()
        {
            if (manager == null || upgrade == null) return;

            int level = manager.LevelOf(upgrade);
            int cost = manager.NextCost(upgrade);
            bool maxed = cost < 0;

            if (nameLabel != null) nameLabel.text = upgrade.DisplayName;
            if (levelLabel != null) levelLabel.text = $"Lv {level}/{upgrade.MaxLevel}";
            if (priceLabel != null) priceLabel.text = maxed ? "MAX" : "$" + cost.ToString("N0");
            if (buyButton != null)
            {
                buyButton.interactable = !maxed && manager.Wallet != null && manager.Wallet.Money >= cost;
            }
        }
    }
}
