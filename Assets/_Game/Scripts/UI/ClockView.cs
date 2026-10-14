using BubbleTea.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The shop clock in the corner ("Day 2 · 11:45 AM") and a banner while a rush is on.
    /// </summary>
    public class ClockView : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private Text timeLabel;
        [SerializeField] private Text dayLabel;

        [SerializeField, Tooltip("Shows the rush (or quiet time) going on now.")]
        private Text periodLabel;

        private void OnEnable()
        {
            if (clock != null) clock.PeriodChanged += ShowPeriod;
        }

        private void OnDisable()
        {
            if (clock != null) clock.PeriodChanged -= ShowPeriod;
        }

        private void Start()
        {
            if (clock != null) ShowPeriod(clock.CurrentPeriod, clock.IsRush);
        }

        private void Update()
        {
            if (clock == null) return;
            if (timeLabel != null) timeLabel.text = clock.TimeLabel;
            if (dayLabel != null) dayLabel.text = "Day " + clock.Day;
            if (periodLabel != null && clock.IsRush)
            {
                // A gentle pulse so a rush catches the eye.
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 5f);
                periodLabel.color = new Color(1f, 0.75f * pulse + 0.2f, 0.3f);
            }
        }

        private void ShowPeriod(string name, bool rush)
        {
            if (periodLabel == null) return;
            periodLabel.gameObject.SetActive(!string.IsNullOrEmpty(name));
            periodLabel.text = rush ? name + "!" : name;
            periodLabel.color = rush ? new Color(1f, 0.85f, 0.3f) : new Color(0.75f, 0.8f, 0.95f);
        }
    }
}
