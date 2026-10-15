using System;
using BubbleTea.Building;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Rating
{
    /// <summary>
    /// The shop's star rating, from 0 to 5. Half comes from service (happy customers raise it)
    /// and half from how built-up the shop is. Messy café tables take stars away until they're
    /// cleaned. Every customer who walks out (too slow, or too expensive) costs half a star (too slow or too pricey), earned back a little with each
    /// customer served. At 0 stars the shop closes and the game starts over.
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

        [SerializeField, Tooltip("Stars lost each time a customer walks out.")]
        private float starsPerWalkout = 0.5f;

        [SerializeField, Tooltip("Stars lost each time a customer leaves because a price is too high.")]
        private float starsPerPriceyWalkout = 0.5f;

        [SerializeField, Tooltip("Lost stars earned back with each customer served.")]
        private float starsBackPerServe = 0.1f;

        [SerializeField, Tooltip("Stars lost for each messy café seat.")]
        private float starsPerMess = 0.25f;

        [SerializeField, Tooltip("Most stars messy tables can cost.")]
        private float maxMessPenalty = 1.5f;

        [SerializeField, Tooltip("Customers arrive this many times faster at 5 stars than at 1 star.")]
        private float maxRateBoost = 2f;

        [SerializeField, Tooltip("Extra customers allowed in line at 5 stars.")]
        private int maxExtraCustomers = 2;

        private float service = 1f;
        private float lost;
        private int messes;
        private float stars = -1f;
        private bool closed;

        /// <summary>Stars from 0 to 5, to one decimal place.</summary>
        public float Stars => Mathf.Max(0f, stars);

        /// <summary>0 to 1: how happy recent customers were. Saved with the game.</summary>
        public float Service => service;

        /// <summary>Stars lost to walkouts and not yet earned back. Saved with the game.</summary>
        public float Lost => lost;

        /// <summary>True once the rating has hit 0 stars.</summary>
        public bool IsClosed => closed;

        /// <summary>Raised with the new star count when it changes.</summary>
        public event Action<float> Changed;

        /// <summary>Raised once when the rating hits 0 stars: the game is lost.</summary>
        public event Action ShopClosed;

        private void OnEnable()
        {
            GameEvents.CustomerServed += OnServed;
            GameEvents.CustomerLeftUpset += OnWalkout;
            GameEvents.CustomerRefusedPrice += OnPriceyWalkout;
            GameEvents.MessChanged += OnMessChanged;
            foreach (RatedPad entry in pads)
            {
                if (entry.pad != null) entry.pad.Unlocked += OnUnlocked;
            }
        }

        private void OnDisable()
        {
            GameEvents.CustomerServed -= OnServed;
            GameEvents.CustomerLeftUpset -= OnWalkout;
            GameEvents.CustomerRefusedPrice -= OnPriceyWalkout;
            GameEvents.MessChanged -= OnMessChanged;
            foreach (RatedPad entry in pads)
            {
                if (entry.pad != null) entry.pad.Unlocked -= OnUnlocked;
            }
        }

        private void Start() => Recalculate();

        /// <summary>Puts back the saved service score and stars lost to walkouts.</summary>
        public void Restore(float savedService, float savedLost)
        {
            service = Mathf.Clamp01(savedService);
            lost = Mathf.Max(0f, savedLost);
            Recalculate();
        }

        private void OnServed(Vector3 _, float mood)
        {
            service = Mathf.Lerp(service, Mathf.Clamp01(mood), serveWeight);
            lost = Mathf.Max(0f, lost - starsBackPerServe);
            Recalculate();
        }

        private void OnWalkout(Vector3 _)
        {
            lost += starsPerWalkout;
            Recalculate();
        }

        private void OnPriceyWalkout(Vector3 _)
        {
            lost += starsPerPriceyWalkout;
            Recalculate();
        }

        private void OnUnlocked(UnlockPad _) => Recalculate();

        private void OnMessChanged(int count)
        {
            messes = count;
            Recalculate();
        }

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
            // Messy tables alone never take the shop below 1 star; only walkouts can close it.
            float penalty = Mathf.Min(maxMessPenalty, messes * starsPerMess);
            float earned = Mathf.Max(1f, 1f + 2f * service + 2f * Decor - penalty);
            float value = Mathf.Round(Mathf.Clamp(earned - lost, 0f, 5f) * 10f) / 10f;
            if (Mathf.Approximately(value, stars)) return;
            stars = value;

            float t = Mathf.Clamp01((stars - 1f) / 4f);
            Stats.Set(StatId.CustomerRate, Mathf.Lerp(1f, maxRateBoost, t));
            Stats.Set(StatId.ExtraCustomers, Mathf.Floor(t * maxExtraCustomers + 0.001f));
            Changed?.Invoke(stars);

            if (stars <= 0f && !closed)
            {
                closed = true;
                ShopClosed?.Invoke();
            }
        }
    }
}
