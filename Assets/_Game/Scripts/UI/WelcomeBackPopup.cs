using System;
using BubbleTea.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// "Welcome back!" panel that shows what the shop earned while the player was away,
    /// with a Collect button that adds it to their money.
    /// </summary>
    public class WelcomeBackPopup : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel to show and hide.")]
        private GameObject panel;

        [SerializeField] private Text amountLabel;

        [SerializeField] private Text awayLabel;

        [SerializeField] private Button collectButton;

        [SerializeField, Tooltip("Where the money comes from.")]
        private OfflineEarnings earnings;

        private void Awake()
        {
            if (collectButton != null) collectButton.onClick.AddListener(Collect);
        }

        private void OnEnable()
        {
            if (panel != null) panel.SetActive(false);
            if (earnings != null) earnings.PendingChanged += Show;
        }

        private void OnDisable()
        {
            if (earnings != null) earnings.PendingChanged -= Show;
        }

        private void Show(int amount, TimeSpan away)
        {
            if (amountLabel != null) amountLabel.text = "$" + amount;
            if (awayLabel != null)
            {
                string time = FormatTime(away);
                bool capped = earnings != null && away.TotalHours > earnings.MaxHours;
                awayLabel.text = capped
                    ? $"You were away {time}.\nYour shop works up to {FormatTime(TimeSpan.FromHours(earnings.MaxHours))} without you."
                    : $"You were away {time}.";
            }
            if (panel != null) panel.SetActive(true);
        }

        private void Collect()
        {
            if (earnings != null) earnings.Collect();
            if (panel != null) panel.SetActive(false);
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays} d {time.Hours} h";
            if (time.TotalHours >= 1) return time.Minutes > 0 ? $"{(int)time.TotalHours} h {time.Minutes} min" : $"{(int)time.TotalHours} h";
            return $"{Math.Max(1, (int)time.TotalMinutes)} min";
        }
    }
}
