using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Sends customers into the shop once a serving counter exists, keeping up to a
    /// set number inside at once.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
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
            if (customerPrefab == null || ServingCounter.All.Count == 0) return;

            timer += Time.deltaTime;
            if (timer < secondsBetweenCustomers || inside.Count >= maxCustomers) return;
            timer = 0f;

            Customer customer = Instantiate(customerPrefab, transform.position, transform.rotation);
            inside.Add(customer);
            Vector3 exit = exitPoint != null ? exitPoint.position : transform.position;
            customer.Init(ServingCounter.All[0], exit, gone => inside.Remove(gone));
        }
    }
}
