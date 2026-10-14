using System;
using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// The shop's own clock, running much faster than real time (by default a whole day takes
    /// 12 real minutes). Customers come more often at breakfast, lunch and dinner, and only a
    /// few drop by late at night. The time of day is saved with the shop.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        /// <summary>A stretch of the day with its own customer flow.</summary>
        [Serializable]
        public struct Period
        {
            public string name;
            [Tooltip("Start time in hours, e.g. 11.5 = 11:30 AM.")] public float from;
            [Tooltip("End time in hours.")] public float to;
            [Tooltip("Customers arrive this many times as often.")] public float rate;
            [Tooltip("Extra customers allowed in line.")] public int extraCustomers;
            [Tooltip("Shown on screen while it lasts, e.g. Lunch rush!")] public bool isRush;
        }

        [SerializeField, Min(0.1f), Tooltip("Game minutes that pass each real second (2 = a day takes 12 minutes).")]
        private float minutesPerSecond = 2f;

        [SerializeField, Range(0f, 24f), Tooltip("Time a brand-new game starts at, in hours.")]
        private float startHour = 7f;

        [SerializeField, Tooltip("Busy and quiet times. Anything not listed is a normal flow of customers.")]
        private Period[] periods =
        {
            new() { name = "Breakfast rush", from = 7f, to = 9f, rate = 2f, extraCustomers = 1, isRush = true },
            new() { name = "Lunch rush", from = 11.5f, to = 13.5f, rate = 2.5f, extraCustomers = 2, isRush = true },
            new() { name = "Dinner rush", from = 17.5f, to = 19.5f, rate = 2.2f, extraCustomers = 2, isRush = true },
            new() { name = "Late night", from = 22f, to = 30f, rate = 0.4f, extraCustomers = 0, isRush = false }, // until 6 AM
        };

        /// <summary>How much faster customers arrive right now (1 when there's no clock).</summary>
        public static float RushMultiplier { get; private set; } = 1f;

        /// <summary>Extra customers allowed in line right now.</summary>
        public static int RushExtraCustomers { get; private set; }

        /// <summary>The clock in the current shop, if any.</summary>
        public static GameClock Instance { get; private set; }

        /// <summary>Raised when a new period starts (its name, empty for normal times) and whether it's a rush.</summary>
        public event Action<string, bool> PeriodChanged;

        private float minutes; // minutes since midnight
        private int day = 1;
        private int periodIndex = -2;

        /// <summary>Minutes since midnight. Saved with the game.</summary>
        public float MinuteOfDay => minutes;

        /// <summary>Day number, starting at 1. Saved with the game.</summary>
        public int Day => day;

        public string CurrentPeriod => periodIndex >= 0 ? periods[periodIndex].name : "";
        public bool IsRush => periodIndex >= 0 && periods[periodIndex].isRush;

        /// <summary>The time like "7:45 AM".</summary>
        public string TimeLabel
        {
            get
            {
                int total = Mathf.FloorToInt(minutes);
                int hour = total / 60 % 24, minute = total % 60 / 15 * 15; // shown in quarter hours
                int shown = hour % 12 == 0 ? 12 : hour % 12;
                return $"{shown}:{minute:00} {(hour < 12 ? "AM" : "PM")}";
            }
        }

        private void Awake()
        {
            Instance = this;
            minutes = startHour * 60f;
            UpdatePeriod();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            RushMultiplier = 1f;
            RushExtraCustomers = 0;
        }

        /// <summary>Puts back the saved time.</summary>
        public void Restore(float savedMinutes, int savedDay)
        {
            minutes = Mathf.Repeat(savedMinutes, 24f * 60f);
            day = Mathf.Max(1, savedDay);
            UpdatePeriod();
        }

        private void Update()
        {
            minutes += Time.deltaTime * minutesPerSecond;
            if (minutes >= 24f * 60f)
            {
                minutes -= 24f * 60f;
                day++;
            }
            UpdatePeriod();
        }

        private void UpdatePeriod()
        {
            float hour = minutes / 60f;
            int found = -1;
            for (int i = 0; i < periods.Length; i++)
            {
                // A period past midnight (to > 24) also covers the early hours.
                if ((hour >= periods[i].from && hour < periods[i].to) || hour + 24f < periods[i].to && hour + 24f >= periods[i].from)
                {
                    found = i;
                    break;
                }
            }
            if (found == periodIndex) return;
            periodIndex = found;
            RushMultiplier = found >= 0 ? Mathf.Max(0.05f, periods[found].rate) : 1f;
            RushExtraCustomers = found >= 0 ? periods[found].extraCustomers : 0;
            PeriodChanged?.Invoke(CurrentPeriod, IsRush);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Instance = null;
            RushMultiplier = 1f;
            RushExtraCustomers = 0;
        }
    }
}
