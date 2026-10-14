using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Sends customers to the serving counter for its lane once that counter is built and a
    /// station can make something on its menu (so nobody walks out before the shop can serve), keeping up to a set number inside at once. A better shop rating brings customers
    /// more often and lets more of them line up.
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

        private readonly List<Customer> inside = new();
        private float timer;

        private void Update()
        {
            ServingCounter counter = ServingCounter.FindLane(lane);
            if (customerPrefab == null || counter == null || !counter.CanMakeAnything) return;

            timer += Time.deltaTime * Stats.Get(StatId.CustomerRate, 1f);
            int max = maxCustomers + Mathf.RoundToInt(Stats.Get(StatId.ExtraCustomers, 0f));
            if (timer < secondsBetweenCustomers || inside.Count >= max) return;
            timer = 0f;

            Customer customer = Instantiate(customerPrefab, transform.position, transform.rotation);
            inside.Add(customer);
            Vector3 exit = exitPoint != null ? exitPoint.position : transform.position;
            customer.Init(counter, exit, gone => inside.Remove(gone));
        }
    }
}
