using System;
using BubbleTea.Cooking;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Walks in, waits in line at a <see cref="ServingCounter"/>, buys a drink when it's
    /// their turn, and walks out with it. Gray-box movement: straight lines, no pathfinding.
    /// Customers lose patience while they wait: a light by their order turns from green to
    /// yellow to red. Quick service earns a tip; a customer who runs out of patience leaves
    /// without buying.
    /// </summary>
    public class Customer : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second.")]
        private float speed = 3f;

        [SerializeField, Tooltip("Seconds a customer waits at the front before taking a drink.")]
        private float pauseBeforeBuying = 0.3f;

        [SerializeField, Tooltip("Where the drink sits while the customer carries it.")]
        private Vector3 drinkHoldPosition = new(0f, 0.3f, 0.6f);

        [SerializeField, Tooltip("The speech bubble above the head. A model of the ordered drink appears on it.")]
        private Transform orderBubble;

        [SerializeField, Tooltip("Size of the drink model shown in the bubble.")]
        private float bubbleIconScale = 1.3f;

        [SerializeField, Min(0f), Tooltip("Seconds a customer will wait before leaving without buying. 0 = they wait forever.")]
        private float patienceSeconds = 30f;

        [SerializeField, Range(0f, 1f), Tooltip("How fast patience drains for customers behind the front of the line (0.5 = half speed).")]
        private float backOfLineDrain = 0.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Served before this share of their patience is used up: big tip.")]
        private float fastServiceShare = 0.35f;

        [SerializeField, Range(0f, 1f), Tooltip("Served before this share of their patience is used up: small tip.")]
        private float okServiceShare = 0.7f;

        [SerializeField, Range(0f, 2f), Tooltip("Big tip, as a share of the drink's price (0.5 = half the price).")]
        private float bigTip = 0.5f;

        [SerializeField, Range(0f, 2f), Tooltip("Small tip, as a share of the drink's price.")]
        private float smallTip = 0.2f;

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

        private static readonly Color Happy = new(0.35f, 0.85f, 0.4f);
        private static readonly Color Unsure = new(1f, 0.82f, 0.25f);
        private static readonly Color Upset = new(0.95f, 0.3f, 0.3f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private enum State { Queueing, ToSeat, Sitting, Leaving }

        private ServingCounter counter;
        private Vector3 exit;
        private Action<Customer> onGone;
        private State state;
        private float waitTimer;
        private float waited;
        private MaterialPropertyBlock moodBlock;
        private SeatingTable table;
        private int seat = -1;
        private float sitTimer;

        /// <summary>The drink this customer wants.</summary>
        public DrinkRecipe Order { get; private set; }

        /// <summary>1 = just arrived and happy, 0 = out of patience.</summary>
        public float Mood => patienceSeconds <= 0f ? 1f : Mathf.Clamp01(1f - waited / patienceSeconds);

        public void Init(ServingCounter servingCounter, Vector3 exitPoint, Action<Customer> gone)
        {
            counter = servingCounter;
            exit = exitPoint;
            onGone = gone;
            state = State.Queueing;
            waited = 0f;
            Order = counter.PickOrder();
            ShowOrder();
            counter.JoinQueue(this);
        }

        // Puts a copy of the ordered drink's model on the bubble so you can see what they want.
        private void ShowOrder()
        {
            if (orderBubble == null) return;
            if (Order == null || Order.Drink == null || Order.Drink.Prefab == null)
            {
                orderBubble.gameObject.SetActive(false);
                return;
            }

            GameObject icon = Instantiate(Order.Drink.Prefab, orderBubble);
            icon.transform.localPosition = Vector3.zero;
            icon.transform.localRotation = Quaternion.identity;
            icon.transform.localScale = Vector3.one * bubbleIconScale;
            var item = icon.GetComponent<CarriedItem>();
            if (item != null) Destroy(item); // just a picture, not a real drink
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
            sitTimer = UnityEngine.Random.Range(sitSeconds.x, sitSeconds.y);
            state = State.Sitting;
        }

        private void Sit()
        {
            sitTimer -= Time.deltaTime;
            if (sitTimer > 0f) return;

            transform.position += Vector3.up * sitDrop;
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

            // Patience drains at full speed at the front of the line and slower further back.
            int index = counter.QueueIndexOf(this);
            waited += Time.deltaTime * (index <= 0 ? 1f : backOfLineDrain);
            ShowMood();
            if (patienceSeconds > 0f && waited >= patienceSeconds)
            {
                GiveUp();
                return;
            }

            if (!WalkTo(counter.QueuePosition(Mathf.Max(index, 0))) || index != 0)
            {
                waitTimer = 0f;
                return;
            }

            // At the front of the line: face the counter and buy once a drink is there.
            Vector3 toCounter = counter.transform.position - transform.position;
            toCounter.y = 0f;
            if (toCounter.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toCounter);

            waitTimer += Time.deltaTime;
            if (waitTimer < pauseBeforeBuying || !counter.TrySell(Order, out CarriedItem drink, TipShare())) return;
            TakeDrink(drink);
        }

        /// <summary>
        /// Handed their drink directly (the counter was full), wherever they are in line.
        /// They pay at the counter as usual.
        /// </summary>
        public bool ServeNow(CarriedItem drink)
        {
            if (state != State.Queueing || counter == null || drink == null) return false;
            counter.Bank(Order, TipShare());
            TakeDrink(drink);
            return true;
        }

        private void TakeDrink(CarriedItem drink)
        {
            drink.MoveTo(transform, drinkHoldPosition);
            GameEvents.RaiseCustomerServed(transform.position, Mood);
            StopWaiting();
            if (canSit && UnityEngine.Random.value < sitChance && SeatingTable.TryReserve(this, out table, out seat)) state = State.ToSeat;
        }

        // How generous this customer is right now, as a share of the price.
        private float TipShare()
        {
            float used = 1f - Mood;
            if (used <= fastServiceShare) return bigTip;
            if (used <= okServiceShare) return smallTip;
            return 0f;
        }

        // Out of patience: leave the line without buying.
        private void GiveUp()
        {
            GameEvents.RaiseCustomerLeftUpset(transform.position);
            StopWaiting();
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
