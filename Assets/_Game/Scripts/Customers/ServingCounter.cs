using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Where drinks are sold. Customers line up on the other side, each wanting one to five
    /// drinks from the menu (only drinks the shop can already make). The player drops off any drink on the menu (ones someone in line is
    /// waiting for go first); extras wait on the counter for a later customer who orders
    /// them, and are thrown out one by one if nobody does within a few seconds. When the
    /// counter is full, a drink someone in line wants goes straight to that customer.
    /// Each customer takes theirs and pays into the counter's cash pile.
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

        [SerializeField, Min(0f), Tooltip("Seconds before a drink nobody in line wants is thrown out (one at a time). 0 = never.")]
        private float expireSeconds = 2f;

        [SerializeField, Range(1, 5), Tooltip("The most drinks one customer orders (3 in the Boba Shop, 4 in the Matcha House).")]
        private int maxDrinksPerCustomer = 3;

        private float expireTimer;

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

        private void Update()
        {
            zone.Tick(transform, Handoff);
            ExpireExtras();
        }

        // Every few seconds, throws out the oldest drink that nobody in line is waiting for.
        private void ExpireExtras()
        {
            if (expireSeconds <= 0f || !drinks.Contains(IsExtra))
            {
                expireTimer = 0f;
                return;
            }
            expireTimer += Time.deltaTime;
            if (expireTimer < expireSeconds) return;
            expireTimer = 0f;
            if (drinks.TryTakeOldest(IsExtra, out CarriedItem old)) Destroy(old.gameObject);
        }

        // More of this drink on the counter than the line wants.
        private bool IsExtra(ItemDefinition item) => drinks.CountOf(onCounter => onCounter == item) > Wanted(item);

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
            foreach (ItemDefinition drink in QueuedOrders())
            {
                if (drink == null) continue;
                // Drinks already on the counter, on their way (reserved by a barista) or being made
                // from a dropped-off ingredient are covered, so nobody does the same work twice.
                if (Wanted(drink) - drinks.CountOf(i => i == drink) - Reserved(drink) - Station.TotalPending(drink) > 0) return drink;
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

        /// <summary>How many of this drink are waiting on the counter.</summary>
        public int Stored(ItemDefinition drink) => drinks.CountOf(item => item == drink);

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

        /// <summary>True if this drink is on the menu and the counter has room to keep it for a later customer.</summary>
        public bool CanStore(ItemDefinition drink) => !drinks.IsFull && FindRecipe(drink) != null;

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

        /// <summary>How many of <paramref name="drink"/> the customers in line still want.</summary>
        public int Wanted(ItemDefinition drink)
        {
            int wanted = 0;
            foreach (Customer customer in queue)
            {
                if (customer != null) wanted += CountIn(customer.Orders, drink);
            }
            return wanted;
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
        /// can make right now (see <see cref="Station.AnyCanMake"/>). The more kinds it can make,
        /// the bigger orders can get, up to the counter's limit; small orders stay the most common. A drink left on
        /// the counter by someone who walked out is ordered first, so it isn't wasted.
        /// </summary>
        public void PickOrders(List<DrinkRecipe> into)
        {
            into.Clear();
            orderChoices.Clear();
            foreach (DrinkRecipe recipe in Menu)
            {
                if (recipe != null && Station.AnyCanMake(recipe.Drink)) orderChoices.Add(recipe);
            }
            if (orderChoices.Count == 0) return;

            int most = Mathf.Clamp(orderChoices.Count, 1, maxDrinksPerCustomer);
            int count = Mathf.Min(most, 1 + Mathf.FloorToInt(Mathf.Pow(Random.value, 1.5f) * most));

            foreach (DrinkRecipe recipe in orderChoices)
            {
                int spare = drinks.CountOf(item => item == recipe.Drink) - Wanted(recipe.Drink);
                for (int i = 0; i < spare && into.Count < count; i++) into.Add(recipe);
            }
            while (into.Count < count) into.Add(orderChoices[Random.Range(0, orderChoices.Count)]);
        }


        /// <summary>
        /// Hands over one of the <paramref name="orders"/> if it's on the counter, and banks its
        /// payment plus a tip of <paramref name="tipShare"/> times the price.
        /// </summary>
        public bool TrySellAny(IReadOnlyList<DrinkRecipe> orders, out CarriedItem drink, out DrinkRecipe sold, float tipShare = 0f)
        {
            drink = null;
            sold = null;
            foreach (DrinkRecipe order in orders)
            {
                if (order == null || !drinks.TryTake(item => item == order.Drink, out drink)) continue;
                sold = order;
                Bank(order, tipShare);
                return true;
            }
            return false;
        }

        /// <summary>What this drink sells for here: its normal price (with upgrades) times the price you set.</summary>
        public int PriceOf(DrinkRecipe order) =>
            order == null ? 0 : Mathf.Max(1, Mathf.RoundToInt(FairPriceOf(order) * PriceBook.Factor(order.Drink)));

        /// <summary>The normal price, which customers expect to pay.</summary>
        public float FairPriceOf(DrinkRecipe order) =>
            order == null ? 0f : order.Price * priceMultiplier * Stats.Get(StatId.DrinkPrice, 1f);

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

        // Takes drinks someone in line is waiting for first, then any other drink on the menu,
        // which waits on the counter. New customers order a waiting drink first (see PickOrder),
        // so nothing sits there for long. When the counter is full, a wanted drink is handed
        // straight to the customer who ordered it.
        private bool Handoff(ItemCarrier carrier)
        {
            if (!carrier.WillGiveTo(this)) return false;
            if (drinks.IsFull) return ServeDirectly(carrier);
            if (!carrier.Stack.TryTake(IsStillWanted, out CarriedItem drink) &&
                !carrier.Stack.TryTake(item => FindRecipe(item) != null, out drink))
            {
                return false;
            }
            drinks.Push(drink);
            return true;
        }

        private bool ServeDirectly(ItemCarrier carrier)
        {
            if (!carrier.Stack.TryTake(IsStillWanted, out CarriedItem drink)) return false;

            // The first customers who ordered this drink get the copies already on the counter,
            // so hand this one to the next customer in line who ordered it.
            int covered = drinks.CountOf(onCounter => onCounter == drink.Definition);
            foreach (Customer customer in queue)
            {
                if (customer == null) continue;
                int wants = CountIn(customer.Orders, drink.Definition);
                if (wants == 0) continue;
                if (covered >= wants)
                {
                    covered -= wants;
                    continue;
                }
                if (customer.ServeNow(drink)) return true;
                break;
            }
            carrier.Stack.Push(drink); // nobody to give it to after all
            return false;
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
