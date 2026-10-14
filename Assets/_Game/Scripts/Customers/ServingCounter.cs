using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Where drinks are sold. Customers line up on the other side, each wanting one drink
    /// from the menu. The player drops off drinks someone in line is waiting for, and each
    /// customer takes theirs and pays into the counter's cash pile.
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
        private readonly List<DrinkRecipe> orderChoices = new();

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
        /// Picks a drink for a new customer: anything on the menu that a station in the shop
        /// can make. Before any station exists, it falls back to the first menu item.
        /// </summary>
        public DrinkRecipe PickOrder()
        {
            orderChoices.Clear();
            DrinkRecipe fallback = null;
            if (menu != null)
            {
                foreach (DrinkRecipe recipe in menu)
                {
                    if (recipe == null) continue;
                    if (fallback == null) fallback = recipe;
                    if (Station.AnyCanMake(recipe.Drink)) orderChoices.Add(recipe);
                }
            }
            return orderChoices.Count > 0 ? orderChoices[Random.Range(0, orderChoices.Count)] : fallback;
        }

        /// <summary>
        /// Hands the customer the drink they ordered and banks their payment.
        /// Returns false if that drink isn't on the counter yet.
        /// </summary>
        public bool TrySell(DrinkRecipe order, out CarriedItem drink)
        {
            drink = null;
            if (order == null || !drinks.TryTake(item => item == order.Drink, out drink)) return false;
            if (cashPile != null) cashPile.AddValue(Mathf.RoundToInt(order.Price * Stats.Get(StatId.DrinkPrice, 1f)));
            return true;
        }

        // Only takes drinks someone in line still needs, so the counter never fills up
        // with drinks nobody will buy.
        private bool Handoff(ItemCarrier carrier)
        {
            if (drinks.IsFull) return false;
            if (!carrier.Stack.TryTake(IsStillWanted, out CarriedItem drink)) return false;
            drinks.Push(drink);
            return true;
        }

        private bool IsStillWanted(ItemDefinition item)
        {
            if (FindRecipe(item) == null) return false;
            int wanted = 0;
            foreach (Customer customer in queue)
            {
                if (customer != null && customer.Order != null && customer.Order.Drink == item) wanted++;
            }
            return drinks.CountOf(onCounter => onCounter == item) < wanted;
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
