using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Player;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A generic workstation. It knows a list of <see cref="RecipeStep"/>s: steps with an
    /// input wait for the player to drop that item off, steps without one run on their
    /// own. Finished items pile up on the output stack for the player to pick up.
    /// A tea brewer and a topping bar are both this component with different steps.
    /// In the Matcha House some bars use supplies: each drink uses one scoop of matcha, and an
    /// empty bar stops until someone brings a crate from the supply dock. The Whisk Bar is
    /// whisked by hand: it only works while the player stands at it, until Whisk Master is bought.
    /// Now and then a working bar jams: a red "!" pops up over it and it stops until the player
    /// walks over and taps it.
    /// </summary>
    public class Station : MonoBehaviour
    {
        // Stations currently in the shop, so customers only order drinks that can be made.
        private static readonly List<Station> active = new();

        [SerializeField, Tooltip("What this station can make.")]
        private RecipeStep[] steps;

        [SerializeField, Tooltip("Items waiting to be worked on.")]
        private ItemStack input = new();

        [SerializeField, Tooltip("Finished items, ready to pick up.")]
        private ItemStack output = new();

        [SerializeField, Tooltip("Where the player stands to drop off and pick up.")]
        private HandoffZone zone = new();

        [SerializeField, Min(0f), Tooltip("Extra speed per barista working here (1 = a barista doubles the speed).")]
        private float baristaSpeedBonus = 1f;

        [SerializeField, Tooltip("Whisked by hand: only works while the player stands in its zone, until the Whisk Master upgrade.")]
        private bool needsPlayer;

        [SerializeField, Tooltip("The crate that restocks this bar. Empty = it never runs out.")]
        private ItemDefinition supplyItem;

        [SerializeField, Tooltip("What the supply is called on the bar's label.")]
        private string supplyName = "MATCHA";

        [SerializeField, Min(1), Tooltip("Drinks one crate makes.")]
        private int suppliesPerCrate = 10;

        [SerializeField, Min(1), Tooltip("Most drinks' worth of supplies the bar holds (a full bar to start).")]
        private int maxSupplies = 20;

        [SerializeField, Tooltip("Seconds standing at the bar before the first drink goes onto the tray.")]
        private float pickupDelay = 0f;

        [SerializeField, Tooltip("Seconds between drinks going onto (or off) the tray here.")]
        private float secondsPerItem = 0.7f;

        [SerializeField, Tooltip("Seconds between jams anywhere in the shop, picked at random in this range (0 = never jams).")]
        private Vector2 secondsBetweenJams = new(60f, 120f);

        [SerializeField, Tooltip("How many times faster a hand-whisked bar works while the player whisks it.")]
        private float handWhiskSpeed = 3f;

        [SerializeField, Tooltip("How close the player must be (meters) to fix a jam by tapping.")]
        private float fixDistance = 3.5f;

        private static readonly Color LabelWhite = new(1f, 0.97f, 0.88f);
        private static readonly Color LabelOrange = new(1f, 0.65f, 0.25f);
        private static readonly Color LabelRed = new(1f, 0.38f, 0.32f);
        private static readonly Color LabelGreen = new(0.6f, 1f, 0.5f);

        private int supplies;
        private FloatingText supplyLabel;
        private FloatingText handLabel;
        private FloatingText jamMark;
        private FloatingText jamHint;
        // One jam at a time for the whole shop, on a real-time clock (not per-bar work time).
        private static float nextJamTime = float.PositiveInfinity;
        private static UnityEngine.SceneManagement.Scene jamScene;
        private float hintTimer;

        private int baristas;

        private RecipeStep current;
        private float workTimer;

        /// <summary>True while the bar is jammed and waiting for the player to tap it.</summary>
        public bool Jammed { get; private set; }

        /// <summary>True while a hand-whisked bar is whisking a drink (for the whisk animation).</summary>
        public bool Whisking => needsPlayer && current != null && !Jammed && (!NeedsHands || PlayerHere);

        /// <summary>0 to 1 progress on the current item (for a future progress bar).</summary>
        public float Progress => current != null ? Mathf.Clamp01(workTimer / current.Seconds) : 0f;

        /// <summary>True if this bar uses up supplies (and takes crates).</summary>
        public bool UsesSupplies => supplyItem != null;

        /// <summary>The crate this bar takes, or null.</summary>
        public ItemDefinition SupplyItem => supplyItem;

        /// <summary>Drinks' worth of supplies left. Saved with the game.</summary>
        public int Supplies => supplies;

        /// <summary>True if a crate fits right now (the bar has room for a whole crate).</summary>
        public bool WantsCrate => UsesSupplies && supplies + suppliesPerCrate <= maxSupplies;

        /// <summary>True if any bar in the shop uses supplies.</summary>
        public static bool AnyUsesSupplies
        {
            get
            {
                foreach (Station station in active)
                {
                    if (station.UsesSupplies) return true;
                }
                return false;
            }
        }

        /// <summary>Puts back the saved supplies (called when a game loads).</summary>
        public void RestoreSupplies(int amount) => supplies = Mathf.Clamp(amount, 0, maxSupplies);

        // Hand-whisked and Whisk Master not bought yet.
        private bool NeedsHands => needsPlayer && Stats.Get(StatId.AutoWhisk, 0f) < 0.5f;

        private bool PlayerHere => PlayerMovement.Current != null && zone.Contains(transform, PlayerMovement.Current.position);

        private void Awake()
        {
            supplies = maxSupplies;
            zone.SetPace(pickupDelay, secondsPerItem); // drinks go onto a tray one at a time, not all at once
        }

        private void Start()
        {
            // Labels: supplies just above the front of the bar, the whisking hint over the spot to stand on.
            Vector3 front = ZoneCenter - transform.position;
            front.y = 0f;
            if (UsesSupplies) supplyLabel = FloatingText.Create(transform, Vector3.up * 1.7f + front.normalized * 0.35f);
            if (needsPlayer) handLabel = FloatingText.Create(transform, front + Vector3.up * 1.1f, 0.05f);
            jamMark = FloatingText.Create(transform, Vector3.up * 2.4f, 0.16f);
            jamHint = FloatingText.Create(transform, Vector3.up * 1.95f, 0.04f);
            UnityEngine.SceneManagement.Scene scene = gameObject.scene;
            if (jamScene != scene)
            {
                // New shop loaded: the first jam comes 45 to 90 seconds in.
                jamScene = scene;
                nextJamTime = secondsBetweenJams.y > 0f ? Time.time + Random.Range(45f, 90f) : float.PositiveInfinity;
            }
        }

        private void PlanNextJam() =>
            nextJamTime = secondsBetweenJams.y > 0f ? Time.time + Random.Range(secondsBetweenJams.x, secondsBetweenJams.y) : float.PositiveInfinity;

        // True when it's time for a jam and no other bar is jammed. Only busy bars jam.
        private static bool JamDue()
        {
            if (Time.time < nextJamTime) return false;
            foreach (Station station in active)
            {
                if (station.Jammed) return false;
            }
            return true;
        }

        private void Jam()
        {
            Jammed = true;
            nextJamTime = float.PositiveInfinity; // the next one is planned once this is fixed
            GameEvents.RaiseStationJammed(transform.position, true);
        }

        // A tap near the bar (or its "!") fixes a jam, once the player has walked over to it.
        private void OnTapped(Vector2 screenPosition)
        {
            if (!Jammed || BubbleTea.Building.MoveMode.Busy) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            if (!TappedOnMe(camera, screenPosition)) return;
            if (PlayerMovement.Current == null || Vector3.Distance(PlayerMovement.Current.position, transform.position) > fixDistance)
            {
                hintTimer = 2f; // shows "WALK CLOSER!" for a moment
                return;
            }
            Jammed = false;
            PlanNextJam();
            GameEvents.RaiseStationJammed(transform.position, false);
        }

        private bool TappedOnMe(Camera camera, Vector2 screenPosition)
        {
            Ray ray = camera.ScreenPointToRay(screenPosition);
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 200f))
            {
                if (hit.collider.GetComponentInParent<Station>() == this) return true;
            }
            // Or close to the "!" on screen.
            Vector3 mark = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.4f);
            return mark.z > 0f && Vector2.Distance(screenPosition, mark) < Screen.height * 0.07f;
        }

        private void OnEnable()
        {
            active.Add(this);
            VirtualJoystick.Tapped += OnTapped;
            Stats.Changed += OnStatChanged;
            ApplyCapacityBonus();
        }

        private void OnDisable()
        {
            active.Remove(this);
            if (Jammed && gameObject.scene == jamScene) PlanNextJam(); // don't stall jams if a jammed bar goes away
            VirtualJoystick.Tapped -= OnTapped;
            Stats.Changed -= OnStatChanged;
        }

        private void OnStatChanged(StatId stat)
        {
            if (stat == StatId.StationCapacity) ApplyCapacityBonus();
        }

        // The Drink Shelf Space upgrade adds room for one more finished drink per level.
        private void ApplyCapacityBonus() =>
            output.ExtraCapacity = Mathf.RoundToInt(Stats.Get(StatId.StationCapacity, 0f));

        public static IReadOnlyList<Station> All => active;

        /// <summary>Baristas working here right now.</summary>
        public int Baristas => baristas;

        /// <summary>A barista arrived at this station.</summary>
        public void AddBarista() => baristas++;

        /// <summary>A barista left this station.</summary>
        public void RemoveBarista() => baristas = Mathf.Max(0, baristas - 1);

        /// <summary>True if this station turns something into <paramref name="item"/> (or makes it from scratch).</summary>
        public bool Makes(ItemDefinition item)
        {
            if (steps == null || item == null) return false;
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Output == item) return true;
            }
            return false;
        }

        /// <summary>Where a worker stands to drop off and pick up.</summary>
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>True if a finished <paramref name="item"/> is waiting to be picked up here.</summary>
        public bool HasReady(ItemDefinition item) => item != null && output.Contains(i => i == item);

        /// <summary>True if this station uses <paramref name="item"/> and has room for it.</summary>
        public bool Accepts(ItemDefinition item) => !input.IsFull && IsInput(item);

        /// <summary>The input this station turns into <paramref name="product"/>, or null if it can't.</summary>
        public ItemDefinition InputFor(ItemDefinition product)
        {
            if (steps == null || product == null) return null;
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Input != null && step.Output == product) return step.Input;
            }
            return null;
        }

        /// <summary>
        /// How many <paramref name="product"/> this station is already on its way to making: ingredients
        /// dropped off for it plus the one being worked on. Baristas count these so they don't bring a
        /// second ingredient for a drink that's already being made.
        /// </summary>
        public int PendingFor(ItemDefinition product)
        {
            if (steps == null || product == null) return 0;
            int count = current != null && current.Input != null && current.Output == product ? 1 : 0;
            foreach (RecipeStep step in steps)
            {
                if (step == null || step.Input == null || step.Output != product) continue;
                ItemDefinition ingredient = step.Input;
                count += input.CountOf(i => i == ingredient);
            }
            return count;
        }

        /// <summary>How many <paramref name="product"/> all stations together are already making from dropped-off ingredients.</summary>
        public static int TotalPending(ItemDefinition product)
        {
            int count = 0;
            foreach (Station station in active) count += station.PendingFor(product);
            return count;
        }

        /// <summary>A station with a finished <paramref name="item"/> ready, or null.</summary>
        public static Station FindReady(ItemDefinition item)
        {
            foreach (Station station in active)
            {
                if (station.HasReady(item)) return station;
            }
            return null;
        }

        /// <summary>
        /// True if the stations built so far can make <paramref name="item"/> from start to
        /// finish: some station produces it, and whatever that step needs can be made too
        /// (boba milk tea needs both the topping bar and the tea brewer).
        /// </summary>
        public static bool AnyCanMake(ItemDefinition item) => CanMake(item, 0);

        private static bool CanMake(ItemDefinition item, int depth)
        {
            if (item == null || depth > 8) return false;
            foreach (Station station in active)
            {
                if (station.steps == null) continue;
                foreach (RecipeStep step in station.steps)
                {
                    if (step == null || step.Output != item) continue;
                    if (step.Input == null || CanMake(step.Input, depth + 1)) return true;
                }
            }
            return false;
        }

        private void Update()
        {
            Work();
            zone.Tick(transform, Handoff);
            UpdateLabels();
        }

        private void Work()
        {
            if (current == null && !output.IsFull && (!UsesSupplies || supplies > 0))
            {
                current = StartNextStep();
                if (current != null && UsesSupplies) supplies--; // one scoop per drink
            }
            if (current == null || Jammed) return;
            if (NeedsHands && !PlayerHere) return; // waits for the player to whisk

            if (JamDue() && Random.value < 0.5f) // a random busy bar gets it
            {
                Jam();
                return;
            }

            float speed = Stats.Get(StatId.BrewSpeed, 1f) * (1f + baristaSpeedBonus * baristas);
            if (NeedsHands) speed *= handWhiskSpeed; // the player whisks fast
            workTimer += Time.deltaTime * speed;
            if (workTimer < current.Seconds) return;

            if (current.Output != null)
            {
                CarriedItem made = current.Output.Spawn(transform.position + Vector3.up);
                if (!output.Push(made)) Destroy(made.gameObject);
            }
            current = null;
            workTimer = 0f;
        }

        // Steps that use up an input come first, so dropped-off items get processed
        // before the station makes anything from scratch.
        private RecipeStep StartNextStep()
        {
            if (steps == null) return null;

            foreach (RecipeStep step in steps)
            {
                if (step == null || step.Input == null) continue;
                if (input.TryTake(item => item == step.Input, out CarriedItem used))
                {
                    Destroy(used.gameObject);
                    return step;
                }
            }
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Input == null) return step;
            }
            return null;
        }

        private void UpdateLabels()
        {
            if (jamMark != null)
            {
                if (!Jammed)
                {
                    jamMark.Hide();
                    jamHint.Hide();
                }
                else
                {
                    // A blinking red "!" until it's fixed.
                    if (Mathf.Repeat(Time.time * 2f, 1f) < 0.75f) jamMark.Show("!", LabelRed);
                    else jamMark.Hide();
                    hintTimer -= Time.deltaTime;
                    jamHint.Show(hintTimer > 0f ? "WALK CLOSER!" : "JAMMED! TAP TO FIX", hintTimer > 0f ? LabelOrange : LabelRed);
                }
            }
            if (supplyLabel != null)
            {
                if (supplies <= 0) supplyLabel.Show($"OUT OF {supplyName}!\nBring a crate", LabelRed);
                else supplyLabel.Show($"{supplyName} {supplies}/{maxSupplies}", supplies <= suppliesPerCrate / 2 ? LabelOrange : LabelWhite);
            }
            if (handLabel != null)
            {
                if (!NeedsHands || current == null) handLabel.Hide();
                else if (PlayerHere) handLabel.Show($"WHISKING {Mathf.RoundToInt(Progress * 100f)}%", LabelGreen);
                else handLabel.Show("STAND HERE\nTO WHISK!", LabelOrange);
            }
        }

        // A crate restocks the bar first; then drop off anything this station uses; otherwise pick up a finished item.
        private bool Handoff(ItemCarrier carrier)
        {
            if (WantsCrate && carrier.WillGiveTo(this) && carrier.Stack.TryTake(i => i == supplyItem, out CarriedItem crate))
            {
                supplies += suppliesPerCrate;
                crate.MoveTo(transform, Vector3.up * 1.2f, 0.5f);
                Destroy(crate.gameObject, 0.25f); // after its hop onto the bar
                return true;
            }
            if (!input.IsFull && carrier.WillGiveTo(this) && carrier.Stack.TryTake(IsInput, out CarriedItem dropped))
            {
                input.Push(dropped);
                return true;
            }
            if (!carrier.Stack.IsFull && output.TryTake(carrier.WillTake, out CarriedItem finished))
            {
                carrier.Stack.Push(finished);
                return true;
            }
            return false;
        }

        private bool IsInput(ItemDefinition item)
        {
            if (steps == null || item == null) return false;
            foreach (RecipeStep step in steps)
            {
                if (step != null && step.Input == item) return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
