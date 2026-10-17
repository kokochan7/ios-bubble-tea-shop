using System;
using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Sends customers to the serving counter for its lane once that counter is built and a
    /// station can make something on its menu (so nobody walks out before the shop can serve), keeping up to a set number inside at once. A better shop rating brings customers
    /// more often and lets more of them line up. Now and then (in the Matcha House) the customer is a
    /// tea critic. A tour bus can drop off extra customers through <see cref="SpawnVisitor"/>; they
    /// count toward the limit, so regulars wait until the group has gone.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField, Tooltip("Lane of the counter these customers go to (Main, DriveThru...).")]
        private string lane = "Main";

        [SerializeField, Tooltip("Customer prefab (needs a Customer component).")]
        private Customer customerPrefab;

        [SerializeField, Tooltip("Where customers leave to. They appear at this spawner's position.")]
        private Transform exitPoint;

        [SerializeField, Tooltip("Seconds between new customers.")]
        private float secondsBetweenCustomers = 3f;

        [SerializeField, Tooltip("Most customers in the shop at once.")]
        private int maxCustomers = 4;

        [SerializeField, Tooltip("Customers' orders start as they step in past this z (the shop's front wall). Off: orders start at the counter (drive-thru cars).")]
        private bool startOrdersAtDoor;

        [SerializeField, Tooltip("The z of the shop's front wall, for the setting above.")]
        private float doorZ;

        [SerializeField, Range(0f, 1f), Tooltip("Chance a new customer is a tea critic (0 = never).")]
        private float criticChance;

        [SerializeField, Min(0f), Tooltip("Seconds of play before the first tea critic, and between critics.")]
        private float secondsBetweenCritics = 90f;

        private readonly List<Customer> inside = new();
        private float timer;
        private float gap = -1f;      // seconds until the next customer, a bit random each time

        // The shop's traffic comes and goes at random, the same for every spawner: a quiet spell,
        // normal trade, a busy rush, or now and then nobody at all for a little while.
        private static float trafficRate = 1f;
        private static int trafficExtra;
        private static float nextTrafficChange;

        private static void UpdateTraffic()
        {
            if (Time.time < nextTrafficChange) return;
            float roll = UnityEngine.Random.value;
            float lasts;
            if (roll < 0.12f) { trafficRate = 0f; trafficExtra = 0; lasts = UnityEngine.Random.Range(10f, 20f); }           // empty
            else if (roll < 0.4f) { trafficRate = 0.4f; trafficExtra = -1; lasts = UnityEngine.Random.Range(20f, 40f); }    // quiet
            else if (roll < 0.75f) { trafficRate = 1f; trafficExtra = 0; lasts = UnityEngine.Random.Range(25f, 50f); }      // normal
            else { trafficRate = 1.9f; trafficExtra = 2; lasts = UnityEngine.Random.Range(20f, 35f); }                     // busy
            nextTrafficChange = Time.time + lasts;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            trafficRate = 1f;
            trafficExtra = 0;
            nextTrafficChange = 0f;
        }
        private float criticTimer;

        private void Update()
        {
            ServingCounter counter = ServingCounter.FindLane(lane);
            if (customerPrefab == null || counter == null || !counter.CanMakeAnything) return;
            criticTimer += Time.deltaTime;

            // A better rating and rush hours (see GameClock) both bring customers faster, and the
            // traffic swings at random between empty, quiet, normal and busy spells.
            UpdateTraffic();
            timer += Time.deltaTime * Stats.Get(StatId.CustomerRate, 1f) * GameClock.RushMultiplier
                * (1f + BubbleTea.Building.ShopDecor.Flow) // decor draws more customers in
                * BubbleTea.Extras.TeamSkills.CustomerFlow // the cleaners' Welcome Mat perk
                * trafficRate;
            int max = Mathf.Max(1, maxCustomers + Mathf.RoundToInt(Stats.Get(StatId.ExtraCustomers, 0f)) + GameClock.RushExtraCustomers + trafficExtra);
            if (gap < 0f) gap = secondsBetweenCustomers * UnityEngine.Random.Range(0.5f, 1.6f);
            if (timer < gap || inside.Count >= max) return;
            timer = 0f;
            gap = secondsBetweenCustomers * UnityEngine.Random.Range(0.5f, 1.6f); // customers don't arrive like clockwork

            Customer.Visit kind = Customer.Visit.Regular;
            if (criticChance > 0f && criticTimer >= secondsBetweenCritics && UnityEngine.Random.value < criticChance)
            {
                kind = Customer.Visit.Critic;
                criticTimer = 0f;
                GameEvents.RaiseAnnouncement("A tea critic is here! Serve them fast for +1 star.\nIf they walk out you lose 1.5 stars.",
                    new Color(0.85f, 0.62f, 0.1f));
            }
            Spawn(counter, transform.position, kind, null);
        }

        /// <summary>
        /// Sends in one extra customer at <paramref name="position"/> right now, past the usual limit
        /// (a tourist off the bus). <paramref name="gone"/> is called when they leave. Null if the
        /// counter isn't built yet.
        /// </summary>
        public Customer SpawnVisitor(Vector3 position, Customer.Visit kind, Action<Customer> gone)
        {
            ServingCounter counter = ServingCounter.FindLane(lane);
            if (customerPrefab == null || counter == null) return null;
            return Spawn(counter, position, kind, gone);
        }

        private Customer Spawn(ServingCounter counter, Vector3 position, Customer.Visit kind, Action<Customer> gone)
        {
            Customer customer = Instantiate(customerPrefab, position, transform.rotation);
            inside.Add(customer);
            Vector3 exit = exitPoint != null ? exitPoint.position : transform.position;
            customer.Init(counter, exit, left =>
            {
                inside.Remove(left);
                gone?.Invoke(left);
            }, kind);
            if (startOrdersAtDoor) customer.StartOrderPast(doorZ);
            return customer;
        }
    }
}
