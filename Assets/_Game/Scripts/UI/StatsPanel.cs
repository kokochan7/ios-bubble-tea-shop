using System.Text;
using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Goals;
using BubbleTea.Staff;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The STATS button (under GOALS) and its panel: money made, drinks sold of each kind,
    /// customers, what the workers are paid, and more, for this shop since it opened.
    /// Names go down the left and numbers down the right; the panel grows to fit the list.
    /// </summary>
    public class StatsPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField, Tooltip("The left column: what each line is.")] private Text body;
        [SerializeField, Tooltip("The right column: the numbers.")] private Text values;

        [SerializeField, Tooltip("Every drink on the menu, so each one's sales can be listed by name.")]
        private DrinkRecipe[] menu;

        [SerializeField, Tooltip("Space above the list (for the title), in UI units.")]
        private float topSpace = 150f;

        [SerializeField, Tooltip("Space below the list (for the Close button), in UI units.")]
        private float bottomSpace = 190f;

        private const string HeadingColor = "#FFD36B";

        private readonly StringBuilder left = new();
        private readonly StringBuilder right = new();
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
            left.Clear();
            right.Clear();

            Heading("SALES");
            Line("Money made (with tips)", $"${records.MoneyEarned:N0}");
            Line("Tips", $"{records.Tips:N0}  (${records.TipMoney:N0})");
            Line("Paid in wages", $"-${records.WagesPaid:N0}");
            Line("Profit", $"${records.MoneyEarned - records.WagesPaid:N0}");

            Heading("DRINKS SOLD");
            Line("All drinks", $"{records.DrinksSold:N0}");
            if (menu != null)
            {
                foreach (DrinkRecipe recipe in menu)
                {
                    if (recipe == null || recipe.Drink == null) continue;
                    int sold = records.SoldOf(recipe.Drink.name);
                    if (sold == 0 && !Station.AnyCanMake(recipe.Drink)) continue; // not unlocked yet
                    Line("   " + recipe.Drink.DisplayName, sold.ToString("N0"));
                }
            }

            Heading("CUSTOMERS");
            Line("Served", $"{records.CustomersServed:N0}");
            Line("Gave up waiting", $"{records.Walkouts:N0}");
            Line("Found it too pricey", $"{records.TooPricey:N0}");

            Payroll payroll = Payroll.Instance;
            if (payroll != null)
            {
                Heading("STAFF WAGES (per shop hour)");
                StaffLine("Baristas", payroll.Baristas, payroll.BaristaWage);
                StaffLine("Cashiers", payroll.Cashiers, payroll.CashierWage);
                StaffLine("Cleaners", payroll.Cleaners, payroll.CleanerWage);
                Line("Total", $"${payroll.WagesPerHour:N0}/hr");
            }

            Heading("SHOP");
            Line("Upgrades bought", $"{records.UpgradesBought:N0}");
            Line("Tables cleaned", $"{records.TablesCleaned:N0}");
            int minutes = Mathf.FloorToInt(records.SecondsPlayed / 60f);
            Line("Time played", minutes >= 60 ? $"{minutes / 60}h {minutes % 60:00}m" : $"{minutes}m");
            if (GameClock.Instance != null) Line("Day", GameClock.Instance.Day.ToString("N0"));

            body.text = left.ToString().TrimEnd('\n');
            if (values != null) values.text = right.ToString().TrimEnd('\n');
            Resize();
        }

        private void Heading(string title)
        {
            bool first = left.Length == 0;
            left.Append(first ? "" : "\n").Append($"<color={HeadingColor}>{title}</color>\n");
            right.Append(first ? "" : "\n").Append('\n');
        }

        private void Line(string label, string value)
        {
            left.Append(label).Append('\n');
            right.Append(value).Append('\n');
        }

        private void StaffLine(string label, int count, int wage)
        {
            if (count > 0) Line($"{label} ({count})", $"${wage}/hr each");
            else Line(label, $"none hired (${wage}/hr)");
        }

        // Grows the panel to fit the list, keeping it centered; FitToScreen shrinks it if the screen is short.
        private void Resize()
        {
            if (!(panel.transform is RectTransform panelRect)) return;
            int lines = 1;
            foreach (char c in body.text) if (c == '\n') lines++;
            float listHeight = lines * body.fontSize * 1.18f; // the built-in font's line height
            Vector2 size = panelRect.sizeDelta;
            size.y = topSpace + listHeight + bottomSpace;
            panelRect.sizeDelta = size;
            SetHeight(body.rectTransform, listHeight);
            if (values != null) SetHeight(values.rectTransform, listHeight);
        }

        private static void SetHeight(RectTransform rect, float height)
        {
            Vector2 size = rect.sizeDelta;
            size.y = height;
            rect.sizeDelta = size;
        }
    }
}
