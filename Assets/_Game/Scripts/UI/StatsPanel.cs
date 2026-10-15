using System.Text;
using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Goals;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The STATS button (under GOALS) and its panel: money made, drinks sold of each kind,
    /// customers served, tips, walkouts and more, for this shop since it opened.
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("The text the stats are written into.")] private Text body;

        [SerializeField, Tooltip("Every drink on the menu, so each one's sales can be listed by name.")]
        private DrinkRecipe[] menu;

        private float refreshTimer;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => panel.SetActive(false));
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf) return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f) return;
            refreshTimer = 0.5f;
            Refresh();
        }

        private void Toggle()
        {
            if (panel == null) return;
            panel.SetActive(!panel.activeSelf);
            refreshTimer = 0f;
        }

        private void Refresh()
        {
            ShopRecords records = ShopRecords.Instance;
            if (body == null || records == null) return;

            var text = new StringBuilder();
            text.AppendLine($"Money made from sales:  ${records.MoneyEarned:N0}");
            text.AppendLine($"Tips:  {records.Tips:N0}  (${records.TipMoney:N0})");
            text.AppendLine($"Customers served:  {records.CustomersServed:N0}");
            text.AppendLine($"Drinks sold:  {records.DrinksSold:N0}");
            if (menu != null)
            {
                foreach (DrinkRecipe recipe in menu)
                {
                    if (recipe == null || recipe.Drink == null) continue;
                    int sold = records.SoldOf(recipe.Drink.name);
                    if (sold == 0 && !Station.AnyCanMake(recipe.Drink)) continue; // not unlocked yet
                    text.AppendLine($"     {recipe.Drink.DisplayName}:  {sold:N0}");
                }
            }
            text.AppendLine($"Customers who gave up waiting:  {records.Walkouts:N0}");
            text.AppendLine($"Customers who found it too pricey:  {records.TooPricey:N0}");
            text.AppendLine($"Upgrades bought:  {records.UpgradesBought:N0}");
            text.AppendLine($"Tables cleaned:  {records.TablesCleaned:N0}");
            int minutes = Mathf.FloorToInt(records.SecondsPlayed / 60f);
            string played = minutes >= 60 ? $"{minutes / 60}h {minutes % 60:00}m" : $"{minutes}m";
            text.Append($"Time played:  {played}");
            if (GameClock.Instance != null) text.Append($"   (Day {GameClock.Instance.Day})");
            body.text = text.ToString();
        }
    }
}
