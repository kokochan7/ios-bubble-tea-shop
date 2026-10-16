using System;
using System.Collections.Generic;
using BubbleTea.Building;
using BubbleTea.Cooking;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Walks in, waits in line at a <see cref="ServingCounter"/>, buys their drinks (one to
    /// five, more as the shop can make more kinds) when it's their turn, and walks out with them. Gray-box movement: straight lines, no pathfinding.
    /// Their order (and patience) starts when they walk in through the door: then a light by their
    /// order turns from green to yellow to red. Some customers tip (1% to 30%), more often and more when
    /// served quickly; a customer who runs out of patience leaves without buying.
    /// Some visitors are special (the Matcha House has them): tourists off a tour bus wear a sun
    /// hat, order less and wait longer; a tea critic wears a gold crown, always orders the most,
    /// always tips well, and moves the rating by a whole star either way.
    /// </summary>
    public class Customer : MonoBehaviour, IHoldsItems
    {
        /// <summary>What kind of customer this is.</summary>
        public enum Visit { Regular, Tourist, Critic, Vip }

        [SerializeField, Tooltip("Walking speed in meters per second.")]
        private float speed = 3f;

        [SerializeField, Tooltip("Where the drink sits while the customer carries it.")]
        private Vector3 drinkHoldPosition = new(0f, 0.62f, 0.48f);

        [SerializeField, Tooltip("The speech bubble above the head. A model of the ordered drink appears on it.")]
        private Transform orderBubble;

        [SerializeField, Tooltip("Size of the drink model shown in the bubble.")]
        private float bubbleIconScale = 2f;

        [SerializeField, Min(0f), Tooltip("Seconds a customer will wait before leaving without buying. 0 = they wait forever.")]
        private float patienceSeconds = 15f;

        [SerializeField, Range(0f, 2f), Tooltip("Extra patience for each drink after the first, as a share of the patience above.")]
        private float patiencePerExtraDrink = 0.4f;

        [SerializeField, Range(0f, 1f), Tooltip("How fast patience drains for customers behind the front of the line (0.5 = half speed).")]
        private float backOfLineDrain = 0.5f;

        [SerializeField, Tooltip("Chance this customer tips: x when served at the last moment, y when served right away.")]
        private Vector2 tipChance = new(0.1f, 0.6f);

        [SerializeField, Tooltip("A tip is between these shares of the price (0.01 = 1%, 0.3 = 30%). Quicker service tips toward the top.")]
        private Vector2 tipRange = new(0.01f, 0.3f);

        [SerializeField, Tooltip("Most a customer will pay, as a share of a drink's normal price. Each customer picks a limit between these two.")]
        private Vector2 priceTolerance = new(1.4f, 2.0f);

        [SerializeField, Tooltip("Whether they turn to face the counter at the front of the line (cars keep facing the lane).")]
        private bool faceCounter = true;

        [SerializeField, Tooltip("Whether this customer can sit at a café table after buying (cars can't).")]
        private bool canSit = true;

        [SerializeField, Range(0f, 1f), Tooltip("Chance a customer sits down if a clean seat is free.")]
        private float sitChance = 0.6f;

        [SerializeField, Tooltip("Seconds a customer sits, picked between these two.")]
        private Vector2 sitSeconds = new(5f, 9f);

        [SerializeField, Tooltip("How far the customer sinks while sitting, so they look seated.")]
        private float sitDrop = 0.35f;

        [SerializeField, Tooltip("A light next to the order that shows the customer's mood. Optional.")]
        private Renderer moodLight;

        [SerializeField, Min(0.1f), Tooltip("Tourists wait this many times longer than other customers.")]
        private float touristPatience = 2.5f;

        [SerializeField, Min(1), Tooltip("The most drinks a tourist orders.")]
        private int touristMaxDrinks = 2;

        [SerializeField, Tooltip("A shop's first customers each order just one drink (an easy start).")]
        private int oneDrinkStarters = 10;

        [SerializeField, Min(0.1f), Tooltip("A tea critic waits this many times longer than other customers (they order a lot).")]
        private float criticPatience = 1.5f;

        [SerializeField, Tooltip("Stars a tea critic adds when served, and takes away (on top of the usual half star) when they walk out.")]
        private float criticStars = 1f;

        private static readonly Color Happy = new(0.35f, 0.85f, 0.4f);
        private static readonly Color Unsure = new(1f, 0.82f, 0.25f);
        private static readonly Color Upset = new(0.95f, 0.3f, 0.3f);
        private static readonly Color CrownGold = new(1f, 0.8f, 0.2f);
        private static readonly Color SunHat = new(0.45f, 0.8f, 0.95f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private enum State { Queueing, ToSeat, Sitting, Leaving }

        private ServingCounter counter;
        private Vector3 exit;
        private Action<Customer> onGone;
        private State state;
        private float waited;
        private bool atCounter;
        private MaterialPropertyBlock moodBlock;
        private SeatingTable table;
        private int seat = -1;
        private float sitTimer;
        private float tolerance;
        private bool checkedPrice;
        private float tipShare = -1f; // decided once, on the first drink handed over
        private bool hasDoorLine;     // the order starts on stepping past doorZ (otherwise at the counter)
        private float doorZ;

        private readonly List<DrinkRecipe> orders = new();
        private readonly List<CarriedItem> held = new();
        private readonly List<GameObject> icons = new();
        private float patience;
        private Visit visit;

        /// <summary>How happy they were (0 to 1) when they got their last drink.</summary>
        public float ServedMood { get; private set; }

        /// <summary>The VIP's name, or null for everyone else.</summary>
        public string VipName { get; private set; }

        /// <summary>Regular customer, tourist, tea critic or VIP regular.</summary>
        public Visit Kind => visit;

        /// <summary>True while they carry drinks they bought (their arms hold them).</summary>
        public bool IsHolding => held.Count > 0;

        /// <summary>True once they've been handed every drink they ordered.</summary>
        public bool WasServed { get; private set; }

        /// <summary>True while this customer is first in line and standing at the counter, ready to be served.</summary>
        public bool IsAtCounter => state == State.Queueing && atCounter;

        /// <summary>The drinks this customer still wants.</summary>
        public IReadOnlyList<DrinkRecipe> Orders => OrderStarted ? orders : NoOrders;
        private static readonly List<DrinkRecipe> NoOrders = new();

        /// <summary>
        /// True once their order has started: when they step in through the shop's door (or, with no
        /// door set, like cars at the drive-thru, when they reach the counter). Until then their
        /// bubble is hidden, their patience doesn't run down and nobody starts on their drinks.
        /// </summary>
        public bool OrderStarted { get; private set; }

        /// <summary>The next drink this customer wants, or null once they have them all.</summary>
        public DrinkRecipe Order => orders.Count > 0 ? orders[0] : null;

        /// <summary>1 = just arrived and happy, 0 = out of patience.</summary>
        public float Mood => patience <= 0f ? 1f : Mathf.Clamp01(1f - waited / patience);

        public void Init(ServingCounter servingCounter, Vector3 exitPoint, Action<Customer> gone, Visit kind = Visit.Regular)
        {
            counter = servingCounter;
            exit = exitPoint;
            onGone = gone;
            visit = kind;
            WasServed = false;
            state = State.Queueing;
            waited = 0f;
            tolerance = UnityEngine.Random.Range(priceTolerance.x, priceTolerance.y);
            checkedPrice = false;
            tipShare = -1f;
            OrderStarted = false;
            if (IsStarter()) counter.PickOrders(orders, maxCount: 1);
            else if (kind == Visit.Tourist) counter.PickOrders(orders, maxCount: touristMaxDrinks);
            else if (kind == Visit.Critic) counter.PickOrders(orders, exactCount: counter.MaxDrinks);
            else counter.PickOrders(orders);
            patience = patienceSeconds * (1f + patiencePerExtraDrink * Mathf.Max(0, orders.Count - 1));
            if (kind == Visit.Tourist) patience *= touristPatience;
            else if (kind == Visit.Critic) patience *= criticPatience;
            patience *= 1f + ShopDecor.Patience; // a nicely decorated shop is worth the wait
            if (kind == Visit.Tourist || kind == Visit.Critic) AddHat(kind == Visit.Critic);
            ShowOrder();
            // The order stays hidden until they reach the counter.
            if (orderBubble != null) orderBubble.gameObject.SetActive(false);
            if (moodLight != null) moodLight.gameObject.SetActive(false);
            counter.JoinQueue(this);
            if (orders.Count == 0) StopWaiting(); // nothing on the menu can be made yet
        }

        // Puts copies of the ordered drinks' models on the bubble so you can see what they want.
        // Several drinks are shown side by side, a little smaller.
        private void ShowOrder()
        {
            if (orderBubble == null) return;
            float scale = bubbleIconScale; // the same size however many drinks they want
            DrinkIcons.Show(orderBubble, orders, scale, scale * 0.23f, icons);
            if (moodLight != null && moodLight.transform.parent == orderBubble)
            {
                // Keep the mood light just past the last drink.
                Vector3 spot = moodLight.transform.localPosition;
                spot.x = Mathf.Max(0.7f, (orders.Count - 1) * 0.5f * scale * 0.23f + 0.55f);
                moodLight.transform.localPosition = spot;
            }
        }

        private void Update()
        {
            switch (state)
            {
                case State.Queueing:
                    Queue();
                    break;
                case State.ToSeat:
                    GoToSeat();
                    break;
                case State.Sitting:
                    Sit();
                    break;
                default:
                    if (WalkTo(exit)) Leave();
                    break;
            }
        }

        private void GoToSeat()
        {
            if (table == null)
            {
                state = State.Leaving;
                return;
            }
            Transform spot = table.Seat(seat);
            if (!WalkTo(spot.position)) return;

            transform.SetPositionAndRotation(spot.position + Vector3.down * sitDrop, spot.rotation);
            if (TryGetComponent(out WalkBob bob)) bob.Sitting = true;
            sitTimer = UnityEngine.Random.Range(sitSeconds.x, sitSeconds.y);
            state = State.Sitting;
        }

        private void Sit()
        {
            sitTimer -= Time.deltaTime;
            if (sitTimer > 0f) return;

            transform.position += Vector3.up * sitDrop;
            if (TryGetComponent(out WalkBob bob)) bob.Sitting = false;
            StandUp(true);
            state = State.Leaving;
        }

        private void StandUp(bool sat)
        {
            if (table != null) table.Leave(seat, sat);
            table = null;
            seat = -1;
        }

        private void Queue()
        {
            if (counter == null)
            {
                state = State.Leaving;
                return;
            }

            // Patience only starts once the order has (then drains at full speed at the front of the
            // line and slower further back).
            int index = counter.QueueIndexOf(this);
            if (!OrderStarted && hasDoorLine && transform.position.z >= doorZ) StartOrder();
            if (OrderStarted)
            {
                waited += Time.deltaTime * (index <= 0 ? 1f : backOfLineDrain);
                ShowMood();
                if (patience > 0f && waited >= patience)
                {
                    GiveUp();
                    return;
                }
            }

            bool inPlace = WalkTo(counter.QueuePosition(Mathf.Max(index, 0)));
            atCounter = inPlace && index == 0;
            if (!atCounter) return;
            if (!OrderStarted) StartOrder();

            // At the front of the line they read the prices; too expensive and they leave.
            if (!checkedPrice)
            {
                checkedPrice = true;
                foreach (DrinkRecipe order in orders)
                {
                    if (!TooExpensive(order)) continue;
                    RefusePrice(order);
                    return;
                }
            }

            // Face the counter and wait: drinks are handed over fresh (see ServingCounter).
            Vector3 toCounter = counter.transform.position - transform.position;
            toCounter.y = 0f;
            if (faceCounter && toCounter.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toCounter);
        }

        /// <summary>The order starts once the customer walks north past this z (the shop's front wall).</summary>
        public void StartOrderPast(float z)
        {
            hasDoorLine = true;
            doorZ = z;
        }

        // Inside the shop (or at the counter): the order bubble and mood light pop up and the clock starts.
        private void StartOrder()
        {
            OrderStarted = true;
            waited = 0f;
            if (orderBubble != null) orderBubble.gameObject.SetActive(true);
            if (moodLight != null) moodLight.gameObject.SetActive(true);
            ShowOrder();
            ShowMood();
        }

        /// <summary>
        /// Handed one of their drinks, fresh, at the counter. They pay at the counter as usual.
        /// </summary>
        public bool ServeNow(CarriedItem drink)
        {
            if (state != State.Queueing || !OrderStarted || counter == null || drink == null) return false;
            DrinkRecipe order = orders.Find(o => o != null && o.Drink == drink.Definition);
            if (order == null) return false;
            if (TooExpensive(order))
            {
                RefusePrice(order);
                return false;
            }
            counter.Bank(order, TipShare());
            TakeDrink(drink, order);
            return true;
        }

        // Takes one ordered drink; with the last one they're done and head off.
        private void TakeDrink(CarriedItem drink, DrinkRecipe order)
        {
            orders.Remove(order);
            held.Add(drink);
            for (int i = 0; i < held.Count; i++)
            {
                // Several drinks are carried side by side.
                Vector3 spot = drinkHoldPosition + Vector3.right * ((i - (held.Count - 1) * 0.5f) * 0.46f);
                if (held[i] != null) held[i].MoveTo(transform, spot, bubbleIconScale);
            }
            ShowOrder();
            if (orders.Count > 0) return;

            GameEvents.RaiseCustomerServed(transform.position, Mood);
            ServedMood = Mood;
            WasServed = true;
            if (visit == Visit.Critic)
            {
                GameEvents.RaiseStarsAwarded(criticStars);
                GameEvents.RaiseAnnouncement($"The tea critic loved it! +{criticStars:0.#} star", CrownGold);
            }
            StopWaiting();
            if (canSit && UnityEngine.Random.value < sitChance && SeatingTable.TryReserve(this, out table, out seat)) state = State.ToSeat;
        }

        // Not everyone tips. Whether this customer does, and how much (1% to 30% of the price), is
        // decided on the first drink they're handed: the happier they are, the likelier and bigger.
        // A tea critic always tips, at the generous end.
        private float TipShare()
        {
            if (tipShare >= 0f) return tipShare;
            float mood = Mood;
            if (visit == Visit.Critic || visit == Visit.Vip)
                tipShare = UnityEngine.Random.Range(Mathf.Lerp(tipRange.x, tipRange.y, 0.5f), tipRange.y);
            else if (UnityEngine.Random.value < Mathf.Lerp(tipChance.x, tipChance.y, mood) + ShopDecor.Happiness)
                tipShare = UnityEngine.Random.Range(tipRange.x, Mathf.Lerp(tipRange.x, tipRange.y, Mathf.Max(mood, 0.2f)))
                    * (1f + ShopDecor.Happiness); // decor makes customers happier, so they tip a bit more
            else
                tipShare = 0f;
            tipShare *= 1f + VipRegulars.TipBonus; // VIPs won over make every tip a little bigger
            return tipShare;
        }

        // One of the shop's first customers: everyone who has come and gone, plus those already in line.
        private bool IsStarter()
        {
            Goals.ShopRecords records = Goals.ShopRecords.Instance;
            if (records == null) return false;
            int seen = records.CustomersServed + records.Walkouts + records.TooPricey + counter.QueueCount;
            return seen < oneDrinkStarters;
        }

        private bool TooExpensive(DrinkRecipe order) => counter.PriceOf(order) > counter.FairPriceOf(order) * tolerance + 0.01f;

        // The price is more than this customer will pay: they leave without buying (costs half a star).
        private void RefusePrice(DrinkRecipe order)
        {
            if (order != null && order.Drink != null) GameEvents.RaiseDrinkTooPricey(order.Drink.DisplayName);
            GameEvents.RaiseCustomerRefusedPrice(transform.position);
            CriticWalkedOut();
            StopWaiting();
        }

        // Out of patience: leave the line without buying.
        private void GiveUp()
        {
            GameEvents.RaiseCustomerLeftUpset(transform.position);
            CriticWalkedOut();
            StopWaiting();
        }

        /// <summary>
        /// Turns this customer into a named VIP regular who orders just their favorite drink and
        /// wears a pink bow. Called right after they're spawned.
        /// </summary>
        public void MakeVip(string vipName, DrinkRecipe favorite)
        {
            VipName = vipName;
            orders.Clear();
            if (favorite != null) orders.Add(favorite);
            patience = patienceSeconds * (1f + ShopDecor.Patience);
            ShowOrder();
            if (!OrderStarted && orderBubble != null) orderBubble.gameObject.SetActive(false);
            AddBow();
        }

        private void AddBow()
        {
            var bowColor = new Color(1f, 0.45f, 0.68f);
            foreach (float side in new[] { -1f, 1f })
            {
                GameObject loop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                loop.name = "VIP Bow";
                Destroy(loop.GetComponent<Collider>());
                loop.transform.SetParent(transform, false);
                loop.transform.localPosition = new Vector3(side * 0.1f, 1.72f, -0.05f);
                loop.transform.localScale = new Vector3(0.16f, 0.11f, 0.06f);
                var bowRenderer = loop.GetComponent<Renderer>();
                if (moodLight != null) bowRenderer.sharedMaterial = moodLight.sharedMaterial;
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, bowColor);
                block.SetColor(ColorId, bowColor);
                bowRenderer.SetPropertyBlock(block);
            }
        }

        // A critic who walks out costs a whole star more than other customers.
        private void CriticWalkedOut()
        {
            if (visit != Visit.Critic) return;
            GameEvents.RaiseStarsAwarded(-criticStars);
            GameEvents.RaiseAnnouncement($"The tea critic walked out! -{criticStars + 0.5f:0.#} stars", Upset);
        }

        // A gold crown for a tea critic, a wide sun hat for a tourist.
        private void AddHat(bool crown)
        {
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = crown ? "Crown" : "Sun Hat";
            Destroy(hat.GetComponent<Collider>());
            hat.transform.SetParent(transform, false);
            hat.transform.localPosition = new Vector3(0f, crown ? 1.72f : 1.64f, 0f);
            hat.transform.localScale = crown ? new Vector3(0.42f, 0.13f, 0.42f) : new Vector3(0.85f, 0.025f, 0.85f);
            var hatRenderer = hat.GetComponent<Renderer>();
            if (moodLight != null) hatRenderer.sharedMaterial = moodLight.sharedMaterial;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, crown ? CrownGold : SunHat);
            block.SetColor(ColorId, crown ? CrownGold : SunHat);
            hatRenderer.SetPropertyBlock(block);
        }

        private void StopWaiting()
        {
            if (orderBubble != null) orderBubble.gameObject.SetActive(false);
            if (moodLight != null) moodLight.gameObject.SetActive(false);
            counter.LeaveQueue(this);
            state = State.Leaving;
        }

        // Green while happy, yellow when unsure, red (and blinking) when nearly out of patience.
        private void ShowMood()
        {
            if (moodLight == null) return;
            float mood = Mood;
            Color color = mood > 0.5f ? Color.Lerp(Unsure, Happy, (mood - 0.5f) * 2f) : Color.Lerp(Upset, Unsure, mood * 2f);
            if (mood < 0.25f && Mathf.Repeat(Time.time * 3f, 1f) < 0.35f) color = Color.Lerp(color, Color.white, 0.5f);

            moodBlock ??= new MaterialPropertyBlock();
            moodLight.GetPropertyBlock(moodBlock);
            moodBlock.SetColor(BaseColorId, color);
            moodBlock.SetColor(ColorId, color);
            moodLight.SetPropertyBlock(moodBlock);
        }

        // Moves toward the target on the floor. Returns true once there.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.0025f) return true;

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }

        private void Leave()
        {
            onGone?.Invoke(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            StandUp(false);
            if (counter != null) counter.LeaveQueue(this);
        }
    }
}
