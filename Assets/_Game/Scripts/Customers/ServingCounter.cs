using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Where drinks are sold. Customers line up on the other side, each wanting one or more
    /// drinks from the menu (only drinks the shop can already make). Drinks are made fresh:
    /// the counter never keeps any. Only the customer at the front of the line is served: a drink
    /// they ordered goes straight to them, and any other drink stays with whoever carried it.
    /// Each customer pays into the counter's cash pile.
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

        [SerializeField, Tooltip("Where the player stands to drop drinks off.")]
        private HandoffZone zone = new();

        [SerializeField, Tooltip("Customer payments land here for the player to collect.")]
        private CashPile cashPile;

        [SerializeField, Tooltip("Front of the customer line. The line extends along this transform's forward direction.")]
        private Transform queueStart;

        [SerializeField, Tooltip("Gap between customers in line, in meters.")]
        private float queueSpacing = 1.1f;

        [SerializeField, Range(1, 5), Tooltip("The most drinks one customer orders (3 in the Boba Shop, 4 in the Matcha House).")]
        private int maxDrinksPerCustomer = 3;

        private readonly List<Customer> queue = new();
        private readonly List<DrinkRecipe> orderChoices = new();
        private readonly List<DrinkRecipe> weightedChoices = new();
        private readonly Dictionary<ItemDefinition, int> reserved = new();

        public string Lane => lane;

        /// <summary>The most drinks one customer orders here.</summary>
        public int MaxDrinks => Mathf.Max(1, maxDrinksPerCustomer);

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

        /// <summary>How many customers are waiting at this counter.</summary>
        public int QueueCount => queue.Count;

        public int QueueIndexOf(Customer customer) => queue.IndexOf(customer);

        /// <summary>Where a worker stands to drop drinks off.</summary>
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>
        /// The next drink the customer at the front of the line needs that isn't already being
        /// brought or made by a worker. Null if nothing is needed. (Only the front customer is
        /// served, so workers only work on their order.)
        /// </summary>
        public ItemDefinition NextNeeded()
        {
            Customer front = FrontCustomer;
            if (front == null) return null;
            foreach (DrinkRecipe order in front.Orders)
            {
                if (order != null && Uncovered(order.Drink) > 0) return order.Drink;
            }
            return null;
        }

        /// <summary>
        /// How many more of this drink the front customer needs than workers are already bringing
        /// (reserved) or making (dropped-off ingredients), so nobody does the same work twice.
        /// </summary>
        public int Uncovered(ItemDefinition drink) =>
            drink == null ? 0 : Wanted(drink) - Reserved(drink) - Station.TotalPending(drink);

        /// <summary>A worker calls this when they set off to bring <paramref name="count"/> of a drink, so others don't bring it too.</summary>
        public void Reserve(ItemDefinition drink, int count = 1)
        {
            if (drink != null && count > 0) reserved[drink] = Reserved(drink) + count;
        }

        /// <summary>Undoes <see cref="Reserve"/> once the drinks are delivered or the worker gives up.</summary>
        public void Release(ItemDefinition drink, int count = 1)
        {
            if (drink != null) reserved[drink] = Mathf.Max(0, Reserved(drink) - count);
        }

        /// <summary>True if the customer at the front of the line still needs this drink.</summary>
        public bool Needs(ItemDefinition drink) => IsStillWanted(drink);

        /// <summary>True once some station in the shop can make a drink on this counter's menu.</summary>
        public bool CanMakeAnything
        {
            get
            {
                foreach (DrinkRecipe recipe in Menu)
                {
                    if (recipe != null && Station.AnyCanMake(recipe.Drink)) return true;
                }
                return false;
            }
        }

        /// <summary>The drinks this counter sells.</summary>
        public IEnumerable<DrinkRecipe> Menu => menu ?? System.Array.Empty<DrinkRecipe>();

        private int Reserved(ItemDefinition drink) => reserved.TryGetValue(drink, out int count) ? count : 0;

        /// <summary>Every drink the customers in line still want, front of the line first.</summary>
        public IEnumerable<ItemDefinition> QueuedOrders()
        {
            foreach (Customer customer in queue)
            {
                if (customer == null) continue;
                foreach (DrinkRecipe order in customer.Orders)
                {
                    if (order != null) yield return order.Drink;
                }
            }
        }

        /// <summary>How many of <paramref name="drink"/> the customer at the front of the line still wants.</summary>
        public int Wanted(ItemDefinition drink)
        {
            Customer front = FrontCustomer;
            return front != null ? CountIn(front.Orders, drink) : 0;
        }

        private static int CountIn(IReadOnlyList<DrinkRecipe> orders, ItemDefinition drink)
        {
            int count = 0;
            foreach (DrinkRecipe order in orders)
            {
                if (order != null && order.Drink == drink) count++;
            }
            return count;
        }

        /// <summary>The customer at the front of the line, or null.</summary>
        public Customer FrontCustomer => queue.Count > 0 ? queue[0] : null;

        public Vector3 QueuePosition(int index)
        {
            Transform start = queueStart != null ? queueStart : transform;
            return start.position + start.forward * (index * queueSpacing);
        }

        /// <summary>
        /// Picks the drinks a new customer wants, into <paramref name="into"/>: only drinks the shop
        /// can make right now (see <see cref="Station.AnyCanMake"/>), up to the counter's limit
        /// (several of the same drink is fine). Smaller orders are a little more common, and today's
        /// special (see <see cref="DailySpecial"/>) is ordered more often.
        /// <paramref name="maxCount"/> caps the order size (tourists order less); <paramref name="exactCount"/>
        /// sets it (a tea critic always orders the most). 0 = the counter's usual limit.
        /// </summary>
        public void PickOrders(List<DrinkRecipe> into, int maxCount = 0, int exactCount = 0)
        {
            into.Clear();
            orderChoices.Clear();
            foreach (DrinkRecipe recipe in Menu)
            {
                if (recipe != null && Station.AnyCanMake(recipe.Drink)) orderChoices.Add(recipe);
            }
            if (orderChoices.Count == 0) return;

            weightedChoices.Clear();
            foreach (DrinkRecipe recipe in orderChoices)
            {
                for (int i = DailySpecial.WeightFor(recipe.Drink); i > 0; i--) weightedChoices.Add(recipe);
            }

            int most = Mathf.Max(1, maxCount > 0 ? Mathf.Min(maxCount, maxDrinksPerCustomer) : maxDrinksPerCustomer);
            int count = exactCount > 0
                ? exactCount
                : Mathf.Min(most, 1 + Mathf.FloorToInt(Mathf.Pow(Random.value, 1.3f) * most));
            while (into.Count < count) into.Add(weightedChoices[Random.Range(0, weightedChoices.Count)]);
            // Same drinks together, so the order reads like "2x Milk Tea, 1x Taro Boba".
            into.Sort((a, b) => orderChoices.IndexOf(a).CompareTo(orderChoices.IndexOf(b)));
        }


        /// <summary>What this drink sells for here: its normal price (with upgrades) times the price you set.</summary>
        public int PriceOf(DrinkRecipe order) =>
            order == null ? 0 : Mathf.Max(1, Mathf.RoundToInt(FairPriceOf(order) * PriceBook.Factor(order.Drink)));

        /// <summary>The normal price, which customers expect to pay (today's special is advertised at double).</summary>
        public float FairPriceOf(DrinkRecipe order) =>
            order == null ? 0f : order.Price * priceMultiplier * Stats.Get(StatId.DrinkPrice, 1f) * DailySpecial.PriceFactorFor(order.Drink);

        /// <summary>Banks the payment (and tip) for a drink handed straight to a customer.</summary>
        public void Bank(DrinkRecipe order, float tipShare)
        {
            if (order == null) return;
            int price = PriceOf(order);
            int tip = Mathf.RoundToInt(price * Mathf.Max(0f, tipShare));
            if (cashPile != null) cashPile.AddValue(price + tip);
            GameEvents.RaiseDrinkSold(transform.position, price + tip);
            GameEvents.RaiseDrinkServed(order.Drink.name);
            if (tip > 0) GameEvents.RaiseTipped(transform.position, tip);
        }

        // Hands a drink the carrier brought to the customer at the front of the line, if it's one
        // they ordered. Everyone behind them waits their turn; other drinks stay with the carrier.
        private bool Handoff(ItemCarrier carrier)
        {
            if (!carrier.WillGiveTo(this)) return false;
            Customer front = FrontCustomer;
            if (front == null || !front.IsAtCounter) return false;
            if (!carrier.Stack.TryTake(item => CountIn(front.Orders, item) > 0, out CarriedItem drink)) return false;
            if (front.ServeNow(drink)) return true;
            carrier.Stack.Push(drink); // they didn't take it after all (too pricey)
            return false;
        }

        private bool IsStillWanted(ItemDefinition item) => FindRecipe(item) != null && Wanted(item) > 0;

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
