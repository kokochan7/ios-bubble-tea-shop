using BubbleTea.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The shop clock in the corner ("Day 2 · 11:45 AM").
    /// </summary>
    public class ClockView : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private Text timeLabel;
        [SerializeField] private Text dayLabel;

        private void Update()
        {
            if (clock == null) return;
            if (timeLabel != null) timeLabel.text = clock.TimeLabel;
            if (dayLabel != null) dayLabel.text = "Day " + clock.Day;
        }
    }
}
