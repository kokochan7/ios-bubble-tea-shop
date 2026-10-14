using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Where drinks are sold. Customers line up on the other side, each wanting one drink
    /// from the menu. The player drops off any drink on the menu (ones someone in line is
    /// waiting for go first); extras wait on the counter for a later customer who orders
    /// them. Each customer takes theirs and pays into the counter's cash pile.
    /// </summary>
    public class ServingCounter : MonoBehaviour
    {
        // Counters currently in the shop, so customer spawners can find one that was
        // just unlocked (it's spawned from a prefab, so nothing can link to it ahead of time).
        private static readonly List<ServingCounter> active = new();
        public static IReadOnlyList<ServingCounter> All => active;

        [SerializeField, Tooltip("Which customer line this counter serves. A spawner with the same lane sends customers here.")]
        private string lane = "Main";

        [SerializeField, Min(0.1f), Tooltip("Multiplies what customers pay here (a drive-through might pay 1.5x).")]
        private float priceMultiplier = 1f;

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
        private readonly Dictionary<ItemDefinition, int> reserved = new();

        public string Lane => lane;

        /// <summary>Where customers' payments pile up (a cashier collects from here).</summary>
        public CashPile CashPile => cashPile;

        /// <summary>The counter serving <paramref name="laneName"/>, or null if it isn't built yet.</summary>
        public static ServingCounter FindLane(string laneName)
        {
            foreach (ServingCounter counter in active)
            {
                if (counter.lane == laneName) return counter;
            }
            return null;
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Update() => zone.Tick(transform, Handoff);

        public void JoinQueue(Customer customer)
        {
            if (!queue.Contains(customer)) queue.Add(customer);
        }

        public void LeaveQueue(Customer customer) => queue.Remove(customer);

        public int QueueIndexOf(Customer customer) => queue.IndexOf(customer);

        /// <summary>Where a worker stands to drop drinks off.</summary>
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>
        /// The drink the line needs most: the first order (front of the line first) that isn't
        /// already on the counter or being brought by a worker. Null if nothing is needed.
        /// </summary>
        public ItemDefinition NextNeeded()
        {
            foreach (Customer customer in queue)
            {
                ItemDefinition drink = customer != null && customer.Order != null ? customer.Order.Drink : null;
                if (drink == null) continue;
                if (Wanted(drink) - drinks.CountOf(i => i == drink) - Reserved(drink) > 0) return drink;
            }
            return null;
        }

        /// <summary>A worker calls this when they set off to bring <paramref name="drink"/>, so others don't bring it too.</summary>
        public void Reserve(ItemDefinition drink)
        {
            if (drink != null) reserved[drink] = Reserved(drink) + 1;
        }

        /// <summary>Undoes <see cref="Reserve"/> once the drink is delivered or the worker gives up.</summary>
        public void Release(ItemDefinition drink)
        {
            if (drink != null && Reserved(drink) > 0) reserved[drink] = Reserved(drink) - 1;
        }

        /// <summary>True if someone in line still needs this drink.</summary>
        public bool Needs(ItemDefinition drink) => IsStillWanted(drink);

        /// <summary>True if this drink is on the menu and the counter has room to keep it for a later customer.</summary>
        public bool CanStore(ItemDefinition drink) => !drinks.IsFull && FindRecipe(drink) != null;

        private int Reserved(ItemDefinition drink) => reserved.TryGetValue(drink, out int count) ? count : 0;

        /// <summary>The drink each customer in line ordered, front of the line first.</summary>
        public IEnumerable<ItemDefinition> QueuedOrders()
        {
            foreach (Customer customer in queue)
            {
                if (customer != null && customer.Order != null) yield return customer.Order.Drink;
            }
        }

        /// <summary>How many customers in line ordered <paramref name="drink"/>.</summary>
        public int Wanted(ItemDefinition drink)
        {
            int wanted = 0;
            foreach (Customer customer in queue)
            {
                if (customer != null && customer.Order != null && customer.Order.Drink == drink) wanted++;
            }
            return wanted;
        }

        public Vector3 QueuePosition(int index)
        {
            Transform start = queueStart != null ? queueStart : transform;
            return start.position + start.forward * (index * queueSpacing);
        }

        /// <summary>
        /// Picks a drink for a new customer: anything on the menu that a station in the shop
        /// can make. Before any station exists, it falls back to the first menu item.
        /// A drink left on the counter by someone who walked out is ordered first, so it isn't wasted.
        /// </summary>
        public DrinkRecipe PickOrder()
        {
            if (menu != null)
            {
                foreach (DrinkRecipe recipe in menu)
                {
                    if (recipe != null && drinks.CountOf(item => item == recipe.Drink) > Wanted(recipe.Drink)) return recipe;
                }
            }

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
        /// Hands the customer the drink they ordered and banks their payment, plus a tip of
        /// <paramref name="tipShare"/> times the price. Returns false if that drink isn't on the counter yet.
        /// </summary>
        public bool TrySell(DrinkRecipe order, out CarriedItem drink, float tipShare = 0f)
        {
            drink = null;
            if (order == null || !drinks.TryTake(item => item == order.Drink, out drink)) return false;
            int price = Mathf.RoundToInt(order.Price * priceMultiplier * Stats.Get(StatId.DrinkPrice, 1f));
            int tip = Mathf.RoundToInt(price * Mathf.Max(0f, tipShare));
            if (cashPile != null) cashPile.AddValue(price + tip);
            GameEvents.RaiseDrinkSold(transform.position, price + tip);
            GameEvents.RaiseDrinkServed(order.Drink.name);
            if (tip > 0) GameEvents.RaiseTipped(transform.position, tip);
            return true;
        }

        // Takes drinks someone in line is waiting for first, then any other drink on the menu,
        // which waits on the counter. New customers order a waiting drink first (see PickOrder),
        // so nothing sits there for long.
        private bool Handoff(ItemCarrier carrier)
        {
            if (drinks.IsFull || !carrier.WillGiveTo(this)) return false;
            if (!carrier.Stack.TryTake(IsStillWanted, out CarriedItem drink) &&
                !carrier.Stack.TryTake(item => FindRecipe(item) != null, out drink))
            {
                return false;
            }
            drinks.Push(drink);
            return true;
        }

        private bool IsStillWanted(ItemDefinition item)
        {
            if (FindRecipe(item) == null) return false;
            return drinks.CountOf(onCounter => onCounter == item) < Wanted(item);
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
