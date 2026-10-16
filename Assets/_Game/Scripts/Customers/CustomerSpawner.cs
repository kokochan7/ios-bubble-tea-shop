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
        private float criticTimer;

        private void Update()
        {
            ServingCounter counter = ServingCounter.FindLane(lane);
            if (customerPrefab == null || counter == null || !counter.CanMakeAnything) return;
            criticTimer += Time.deltaTime;

            // A better rating and rush hours (see GameClock) both bring customers faster.
            timer += Time.deltaTime * Stats.Get(StatId.CustomerRate, 1f) * GameClock.RushMultiplier
                * (1f + BubbleTea.Building.ShopDecor.Flow); // decor draws more customers in
            int max = maxCustomers + Mathf.RoundToInt(Stats.Get(StatId.ExtraCustomers, 0f)) + GameClock.RushExtraCustomers;
            if (timer < secondsBetweenCustomers || inside.Count >= max) return;
            timer = 0f;

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
