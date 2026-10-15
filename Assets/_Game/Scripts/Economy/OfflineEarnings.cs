using System;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// Pays the player for time spent away from the game. While you play it measures how much
    /// the shop earns per minute from drink sales; when you come back (a fresh launch, or the app
    /// returning from the background) it offers part of that for the minutes you were gone.
    /// The money waits in <see cref="Pending"/> until the player taps Collect.
    /// </summary>
    public class OfflineEarnings : MonoBehaviour
    {
        [SerializeField, Tooltip("The wallet the collected money goes into.")]
        private Wallet wallet;

        [SerializeField, Range(0f, 1f), Tooltip("Share of the normal earning speed paid while away. 0.5 = half.")]
        private float efficiency = 0.12f;

        [SerializeField, Tooltip("The shop stops earning after this many hours away, so players come back.")]
        private float maxHours = 1.5f;

        [SerializeField, Tooltip("Ignore absences shorter than this many minutes (no pop-up for a quick app switch).")]
        private float minMinutes = 1f;

        [SerializeField, Tooltip("How many seconds of play make up one measurement of the earning speed.")]
        private float sampleSeconds = 60f;

        [SerializeField, Range(0.05f, 1f), Tooltip("How quickly the earning speed follows new measurements. 1 = only the latest minute counts.")]
        private float smoothing = 0.3f;

        private float ratePerMinute;
        private float windowEarned;
        private float windowSeconds;
        private DateTime? pausedAtUtc;

        /// <summary>Money earned while away, not yet collected.</summary>
        public int Pending { get; private set; }

        /// <summary>The measured earning speed in dollars per minute of play. Saved with the game.</summary>
        public float RatePerMinute => ratePerMinute;

        /// <summary>Most hours that count toward a reward.</summary>
        public float MaxHours => maxHours;

        /// <summary>Raised when money was earned while away: the total waiting, and how long the player was gone.</summary>
        public event Action<int, TimeSpan> PendingChanged;

        private void OnEnable() => GameEvents.DrinkSold += OnSale;
        private void OnDisable() => GameEvents.DrinkSold -= OnSale;

        private void OnSale(Vector3 _, int amount) => windowEarned += amount;

        private void Update()
        {
            windowSeconds += Time.deltaTime;
            if (windowSeconds < sampleSeconds) return;

            float sample = windowEarned * 60f / windowSeconds;
            ratePerMinute = ratePerMinute <= 0f ? sample : Mathf.Lerp(ratePerMinute, sample, smoothing);
            windowEarned = 0f;
            windowSeconds = 0f;
        }

        // On iPhone the app usually goes to the background instead of quitting, so a return
        // from the background counts as coming back too.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                pausedAtUtc = DateTime.UtcNow;
            }
            else if (pausedAtUtc.HasValue)
            {
                Grant(DateTime.UtcNow - pausedAtUtc.Value);
                pausedAtUtc = null;
            }
        }

        /// <summary>Restores the earning speed saved last time.</summary>
        public void Restore(float savedRatePerMinute) => ratePerMinute = Mathf.Max(0f, savedRatePerMinute);

        /// <summary>Adds the reward for being away this long (if any) to <see cref="Pending"/>.</summary>
        public void Grant(TimeSpan away)
        {
            // A negative time means the device clock was moved backwards: pay nothing.
            if (away.TotalMinutes < minMinutes) return;

            double minutes = Math.Min(away.TotalMinutes, maxHours * 60.0);
            int amount = (int)Math.Floor(ratePerMinute * efficiency * minutes);
            if (amount <= 0) return;

            Pending += amount;
            PendingChanged?.Invoke(Pending, away);
        }

        /// <summary>Moves the waiting money into the wallet.</summary>
        public void Collect()
        {
            if (Pending <= 0) return;
            if (wallet != null) wallet.Add(Pending);
            Pending = 0;
            GameEvents.RaiseBonusCollected();
        }
    }
}
