using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// The shop's own clock, running much faster than real time (by default a whole day takes
    /// about 2.5 real minutes). There are no set rush hours: every so often the flow of customers
    /// drifts to a new random level, from quiet to busy. The time of day is saved with the shop.
    /// </summary>
    public class GameClock : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Game minutes that pass each real second (10 = a day takes 2.4 minutes).")]
        private float minutesPerSecond = 10f;

        [SerializeField, Range(0f, 24f), Tooltip("Time a brand-new game starts at, in hours.")]
        private float startHour = 7f;

        [SerializeField, Tooltip("Customers arrive between this many times as often (quiet) ...")]
        private float quietestRate = 0.6f;

        [SerializeField, Tooltip("... and this many times as often (busy).")]
        private float busiestRate = 1.8f;

        [SerializeField, Tooltip("Real seconds between changes in the flow of customers (picked at random in this range).")]
        private Vector2 secondsBetweenChanges = new(15f, 40f);

        /// <summary>How much faster customers arrive right now (1 when there's no clock).</summary>
        public static float RushMultiplier { get; private set; } = 1f;

        /// <summary>Extra customers allowed in line right now (one more while it's busy).</summary>
        public static int RushExtraCustomers { get; private set; }

        /// <summary>The clock in the current shop, if any.</summary>
        public static GameClock Instance { get; private set; }

        private float minutes; // minutes since midnight
        private int day = 1;
        private float targetRate = 1f;
        private float nextChange;

        /// <summary>Minutes since midnight. Saved with the game.</summary>
        public float MinuteOfDay => minutes;

        /// <summary>Day number, starting at 1. Saved with the game.</summary>
        public int Day => day;

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
            RushMultiplier = 1f;
            RushExtraCustomers = 0;
            PickNextRate();
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
        }

        private void Update()
        {
            minutes += Time.deltaTime * minutesPerSecond;
            if (minutes >= 24f * 60f)
            {
                minutes -= 24f * 60f;
                day++;
            }

            // Random customer traffic: drift smoothly toward a new level every so often.
            nextChange -= Time.deltaTime;
            if (nextChange <= 0f) PickNextRate();
            RushMultiplier = Mathf.MoveTowards(RushMultiplier, targetRate, 0.1f * Time.deltaTime);
            RushExtraCustomers = RushMultiplier > 1.4f ? 1 : 0;
        }

        private void PickNextRate()
        {
            targetRate = Random.Range(quietestRate, busiestRate);
            nextChange = Random.Range(secondsBetweenChanges.x, secondsBetweenChanges.y);
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
