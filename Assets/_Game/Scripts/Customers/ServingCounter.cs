using System.Collections.Generic;
using BubbleTea.Cooking;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Where drinks are sold. The player drops finished drinks here, customers line up
    /// on the other side, take one each, and pay into the counter's cash pile.
    /// </summary>
    public class ServingCounter : MonoBehaviour
    {
        // Counters currently in the shop, so customer spawners can find one that was
        // just unlocked (it's spawned from a prefab, so nothing can link to it ahead of time).
        private static readonly List<ServingCounter> active = new();
        public static IReadOnlyList<ServingCounter> All => active;

        [SerializeField, Tooltip("Drinks this counter sells, with their prices.")]
        private DrinkRecipe[] menu;

        [SerializeField, Tooltip("Drinks waiting on the counter.")]
        private ItemStack drinks = new();

        [SerializeField, Tooltip("Where the player stands to drop drinks off.")]
        private HandoffZone zone = new();

        [SerializeField, Tooltip("Customer payments land here for the player to collect.")]
        private CashPile cashPile;

        [SerializeField, Tooltip("Front of the customer line. The line extends along this transform's forward direction.")]
        private Transform queueStart;

        [SerializeField, Tooltip("Gap between customers in line, in meters.")]
        private float queueSpacing = 1.1f;

        private readonly List<Customer> queue = new();

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Update() => zone.Tick(transform, Handoff);

        public void JoinQueue(Customer customer)
        {
            if (!queue.Contains(customer)) queue.Add(customer);
        }

        public void LeaveQueue(Customer customer) => queue.Remove(customer);

        public int QueueIndexOf(Customer customer) => queue.IndexOf(customer);

        public Vector3 QueuePosition(int index)
        {
            Transform start = queueStart != null ? queueStart : transform;
            return start.position + start.forward * (index * queueSpacing);
        }

        /// <summary>
        /// Hands the next drink to the customer and banks their payment.
        /// Returns false if there's nothing to sell.
        /// </summary>
        public bool TrySell(out CarriedItem drink)
        {
            DrinkRecipe sold = null;
            if (!drinks.TryTake(item => (sold = FindRecipe(item)) != null, out drink)) return false;
            if (cashPile != null) cashPile.AddValue(sold.Price);
            return true;
        }

        private bool Handoff(ItemCarrier carrier)
        {
            if (drinks.IsFull) return false;
            if (!carrier.Stack.TryTake(item => FindRecipe(item) != null, out CarriedItem drink)) return false;
            drinks.Push(drink);
            return true;
        }

        private DrinkRecipe FindRecipe(ItemDefinition item)
        {
            if (menu == null || item == null) return null;
            foreach (DrinkRecipe recipe in menu)
            {
                if (recipe != null && recipe.Drink == item) return recipe;
            }
            return null;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
