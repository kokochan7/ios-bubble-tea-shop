using System;
using BubbleTea.Building;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Rating
{
    /// <summary>
    /// The shop's star rating, from 1 to 5. Half comes from service (happy customers raise it,
    /// customers who walk out lower it) and half from how built-up and decorated the shop is.
    /// More stars bring customers more often and let more of them line up.
    /// </summary>
    public class ShopRating : MonoBehaviour
    {
        /// <summary>An unlock pad and how many rating points it's worth once bought.</summary>
        [Serializable]
        public struct RatedPad
        {
            public UnlockPad pad;
            public int points;
        }

        [SerializeField, Tooltip("Pads that make the shop nicer. Decorations are worth more than stations.")]
        private RatedPad[] pads = Array.Empty<RatedPad>();

        [SerializeField, Range(0.01f, 0.5f), Tooltip("How much one happy customer moves the service score.")]
        private float serveWeight = 0.05f;

        [SerializeField, Range(0.01f, 0.5f), Tooltip("How much one customer walking out lowers the service score.")]
        private float walkoutWeight = 0.12f;

        [SerializeField, Tooltip("Customers arrive this many times faster at 5 stars than at 1 star.")]
        private float maxRateBoost = 2f;

        [SerializeField, Tooltip("Extra customers allowed in line at 5 stars.")]
        private int maxExtraCustomers = 2;

        private float service = 0.5f;
        private float stars = -1f;

        /// <summary>Stars from 1 to 5, to one decimal place.</summary>
        public float Stars => Mathf.Max(1f, stars);

        /// <summary>0 to 1: how happy recent customers were. Saved with the game.</summary>
        public float Service => service;

        /// <summary>Raised with the new star count when it changes.</summary>
        public event Action<float> Changed;

        private void OnEnable()
        {
            GameEvents.CustomerServed += OnServed;
            GameEvents.CustomerLeftUpset += OnWalkout;
            foreach (RatedPad entry in pads)
            {
                if (entry.pad != null) entry.pad.Unlocked += OnUnlocked;
            }
        }

        private void OnDisable()
        {
            GameEvents.CustomerServed -= OnServed;
            GameEvents.CustomerLeftUpset -= OnWalkout;
            foreach (RatedPad entry in pads)
            {
                if (entry.pad != null) entry.pad.Unlocked -= OnUnlocked;
            }
        }

        private void Start() => Recalculate();

        /// <summary>Puts back the saved service score.</summary>
        public void Restore(float savedService)
        {
            service = Mathf.Clamp01(savedService);
            Recalculate();
        }

        private void OnServed(Vector3 _, float mood)
        {
            service = Mathf.Lerp(service, Mathf.Clamp01(mood), serveWeight);
            Recalculate();
        }

        private void OnWalkout(Vector3 _)
        {
            service = Mathf.Lerp(service, 0f, walkoutWeight);
            Recalculate();
        }

        private void OnUnlocked(UnlockPad _) => Recalculate();

        /// <summary>0 to 1: share of the shop's rating points that have been bought.</summary>
        public float Decor
        {
            get
            {
                int earned = 0, total = 0;
                foreach (RatedPad entry in pads)
                {
                    if (entry.pad == null) continue;
                    total += entry.points;
                    if (entry.pad.IsUnlocked) earned += entry.points;
                }
                return total > 0 ? (float)earned / total : 0f;
            }
        }

        private void Recalculate()
        {
            float value = Mathf.Round((1f + 2f * service + 2f * Decor) * 10f) / 10f;
            if (Mathf.Approximately(value, stars)) return;
            stars = value;

            float t = (stars - 1f) / 4f;
            Stats.Set(StatId.CustomerRate, Mathf.Lerp(1f, maxRateBoost, t));
            Stats.Set(StatId.ExtraCustomers, Mathf.Floor(t * maxExtraCustomers + 0.001f));
            Changed?.Invoke(stars);
        }
    }
}
