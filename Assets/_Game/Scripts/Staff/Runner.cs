using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired helper who keeps the counter stocked. Each time they're free they pick one job:
    /// <list type="bullet">
    /// <item>bring a drink someone in line is waiting for from the station where it's ready, or</item>
    /// <item>if it isn't ready anywhere, carry the ingredient it needs to the station that makes it
    /// (for example plain milk tea to the topping bar, to become boba), or</item>
    /// <item>when nobody is waiting, stock the counter with whatever drink is ready, the one
    /// it has fewest of first, so later customers are served straight away.</item>
    /// </list>
    /// They swap items through the same floor zones the player uses. The Walk Speed and
    /// Carry More upgrades apply to runners too: they walk faster and carry several of the
    /// same item per trip.
    /// </summary>
    [RequireComponent(typeof(ItemCarrier))]
    public class Runner : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second.")]
        private float speed = 3.5f;

        [SerializeField, Tooltip("Seconds between looking for a new job while idle.")]
        private float secondsBetweenPlans = 0.25f;

        [SerializeField, Tooltip("Seconds to wait at a zone for the hand-off before giving up on the job.")]
        private float giveUpSeconds = 4f;

        [SerializeField, Tooltip("Seconds to hold an item nobody needs before throwing it away.")]
        private float discardSeconds = 8f;

        private enum Job { None, Fetch, Deliver }

        private ItemCarrier carrier;
        private Job job;
        private ItemDefinition item;
        private int want = 1; // how many of the item to pick up this trip
        private bool reserved; // whether this job holds a reservation on deliverToCounter
        private Station pickupFrom;
        private Station deliverToStation;
        private ServingCounter deliverToCounter;
        private float timer;
        private float holdingUselessTimer;

        private void Awake()
        {
            carrier = GetComponent<ItemCarrier>();
            TakeNothing();
        }

        private void Update()
        {
            switch (job)
            {
                case Job.None: Plan(); break;
                case Job.Fetch: Fetch(); break;
                case Job.Deliver: Deliver(); break;
            }
        }

        // ---------- Choosing a job ----------

        private void Plan()
        {
            timer += Time.deltaTime;
            if (timer < secondsBetweenPlans) return;
            timer = 0f;

            if (carrier.Stack.Count > 0)
            {
                PlanForHeldItem();
                return;
            }
            holdingUselessTimer = 0f;

            // Serve whichever counter has a customer waiting on a drink (main shop first).
            ServingCounter counter = null;
            ItemDefinition needed = null;
            foreach (ServingCounter candidate in ServingCounter.All)
            {
                needed = candidate.NextNeeded();
                if (needed == null) continue;
                counter = candidate;
                break;
            }
            if (counter == null)
            {
                PlanStocking();
                return;
            }

            // Is the drink ready somewhere? Fetch it for the counter.
            Station ready = Station.FindReady(needed);
            if (ready != null)
            {
                counter.Reserve(needed);
                StartFetch(needed, ready, null, counter, CarryLimit());
                reserved = true;
                return;
            }

            // Otherwise feed a station that turns an ingredient into it.
            foreach (Station maker in Station.All)
            {
                ItemDefinition ingredient = maker.InputFor(needed);
                if (ingredient == null || !maker.Accepts(ingredient)) continue;
                Station source = Station.FindReady(ingredient);
                if (source == null || source == maker) continue;
                StartFetch(ingredient, source, maker, null, CarryLimit());
                return;
            }

            // Nothing to fetch for that order yet: keep busy stocking the counter meanwhile.
            PlanStocking();
        }

        // Nobody is waiting on anything we can help with, so bring a ready drink to a counter
        // that has room, picking the drink it has fewest of.
        private void PlanStocking()
        {
            foreach (ServingCounter counter in ServingCounter.All)
            {
                ItemDefinition best = null;
                Station bestSource = null;
                int fewest = int.MaxValue;
                foreach (DrinkRecipe recipe in counter.Menu)
                {
                    if (recipe == null || recipe.Drink == null || !counter.CanStore(recipe.Drink)) continue;
                    Station source = Station.FindReady(recipe.Drink);
                    if (source == null) continue;
                    int stored = counter.Stored(recipe.Drink);
                    if (stored >= fewest) continue;
                    fewest = stored;
                    best = recipe.Drink;
                    bestSource = source;
                }
                if (best == null) continue;
                StartFetch(best, bestSource, null, counter, CarryLimit());
                return;
            }
        }

        // Already holding something (a job was cut short): find somewhere it's useful.
        private void PlanForHeldItem()
        {
            ItemDefinition held = null;
            carrier.Stack.Contains(i => { held = i; return true; });

            foreach (ServingCounter counter in ServingCounter.All)
            {
                if (!counter.Needs(held)) continue;
                counter.Reserve(held);
                StartDeliver(held, null, counter);
                reserved = true;
                return;
            }
            // Nobody is waiting for it, but the counter can keep it for a later customer.
            foreach (ServingCounter counter in ServingCounter.All)
            {
                if (!counter.CanStore(held)) continue;
                StartDeliver(held, null, counter);
                return;
            }
            foreach (Station station in Station.All)
            {
                if (!station.Accepts(held)) continue;
                StartDeliver(held, station, null);
                return;
            }

            holdingUselessTimer += secondsBetweenPlans;
            if (holdingUselessTimer < discardSeconds) return;
            holdingUselessTimer = 0f;
            if (carrier.Stack.TryTake(_ => true, out CarriedItem junk)) Destroy(junk.gameObject);
        }

        // ---------- Doing the job ----------

        private void StartFetch(ItemDefinition what, Station from, Station toStation, ServingCounter toCounter, int count)
        {
            item = what;
            want = Mathf.Max(1, count);
            pickupFrom = from;
            deliverToStation = toStation;
            deliverToCounter = toCounter;
            carrier.TakeFilter = i => i == item && carrier.Stack.Count < want;
            timer = 0f;
            job = Job.Fetch;
        }

        private void Fetch()
        {
            if (pickupFrom == null)
            {
                Cancel();
                return;
            }

            // Leave with a full load, or with what there is once the station has run out.
            int held = carrier.Stack.CountOf(i => i == item);
            if (held >= want || (held > 0 && !pickupFrom.HasReady(item)))
            {
                TakeNothing();
                StartDeliver(item, deliverToStation, deliverToCounter);
                return;
            }

            if (!WalkTo(pickupFrom.ZoneCenter)) return;
            timer += Time.deltaTime;
            if (timer >= giveUpSeconds) Cancel();
        }

        private void StartDeliver(ItemDefinition what, Station toStation, ServingCounter toCounter)
        {
            item = what;
            deliverToStation = toStation;
            deliverToCounter = toCounter;
            carrier.GiveOnlyTo = toStation != null ? toStation : toCounter;
            timer = 0f;
            job = Job.Deliver;
        }

        private void Deliver()
        {
            Component target = deliverToStation != null ? deliverToStation : deliverToCounter;
            if (target == null || !carrier.Stack.Contains(i => i == item))
            {
                // Handed over (or the destination is gone).
                Finish();
                return;
            }

            Vector3 spot = deliverToStation != null ? deliverToStation.ZoneCenter : deliverToCounter.ZoneCenter;
            if (!WalkTo(spot)) return;
            timer += Time.deltaTime;
            if (timer >= giveUpSeconds) Finish();
        }

        private void Cancel() => Finish();

        private void Finish()
        {
            ReleaseReservation();
            Clear();
        }

        private void ReleaseReservation()
        {
            if (reserved && deliverToCounter != null) deliverToCounter.Release(item);
            reserved = false;
        }

        private void Clear()
        {
            TakeNothing();
            item = null;
            pickupFrom = null;
            deliverToStation = null;
            deliverToCounter = null;
            timer = 0f;
            job = Job.None;
        }

        // Between jobs the runner keeps what it holds and picks nothing up.
        private void TakeNothing()
        {
            carrier.TakeFilter = _ => false;
            carrier.GiveOnlyTo = this;
        }

        private void OnDestroy() => ReleaseReservation();

        // One item, plus whatever the Carry More upgrade adds.
        private static int CarryLimit() => 1 + Mathf.RoundToInt(Stats.Get(StatId.CarryBonus, 0f));

        // Moves toward the target on the floor. Returns true once close enough to stand in a zone.
        // The Walk Speed upgrade speeds runners up as well.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.09f) return true;

            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f);
            transform.position = Vector3.MoveTowards(transform.position, target, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }
    }
}
