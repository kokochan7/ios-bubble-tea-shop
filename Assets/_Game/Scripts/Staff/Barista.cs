using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Upgrades;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired barista (they used to be two roles, runner and barista). Each time they're free
    /// they pick one job:
    /// <list type="bullet">
    /// <item>bring a drink someone in line is waiting for from the station where it's ready, or</item>
    /// <item>if it isn't ready anywhere, carry the ingredient it needs to the station that makes it
    /// (for example plain milk tea to the topping bar, to become boba), or</item>
    /// </list>
    /// They only work on the order of the customer at the front of the line (the only one the
    /// counter serves), and pick up exactly as many drinks as that order still needs. Each job is
    /// reserved at the counter, and drinks already being made count too, so two baristas never
    /// work on the same drink. Anything they hold that the front customer doesn't need (or that
    /// couldn't be handed over) goes in the nearest trash can.
    /// With nothing to carry, they stand behind the station customers need most and make it work
    /// faster (one barista per station). They swap items through the same floor zones the player
    /// uses. The Carry More upgrade applies to baristas too: they carry several of the same item
    /// per trip.
    /// Every so often, between jobs, a barista takes a short break on the staff sofa; their station
    /// runs slower until they're back.
    /// </summary>
    [RequireComponent(typeof(ItemCarrier))]
    public class Barista : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second.")]
        private float speed = 3.5f;

        [SerializeField, Tooltip("Seconds between looking for a new job while idle.")]
        private float secondsBetweenPlans = 0.25f;

        [SerializeField, Tooltip("Seconds to wait at a zone for the hand-off before giving up on the job.")]
        private float giveUpSeconds = 4f;

        [SerializeField, Tooltip("Seconds to hold an item nobody needs before throwing it away.")]
        private float discardSeconds = 0.5f;

        [SerializeField, Tooltip("Seconds between looking for a busier station while helping at one.")]
        private float rethinkSeconds = 8f;

        [SerializeField, Tooltip("How far in front of a station's middle they stand to help, in meters.")]
        private float standDistance = 1.1f;

        [SerializeField, Tooltip("Seconds of work between breaks, picked at random in this range.")]
        private Vector2 secondsBetweenBreaks = new(60f, 120f);

        [SerializeField, Tooltip("How long a break lasts, in seconds, picked at random in this range.")]
        private Vector2 breakSeconds = new(10f, 16f);


        private enum Job { None, Fetch, Deliver, Trash }

        private ItemCarrier carrier;
        private Job job;
        private ItemDefinition item;
        private int want = 1; // how many of the item to pick up this trip
        private ServingCounter reservedAt; // the counter this job holds a reservation on, if any
        private ItemDefinition reservedDrink;
        private int reservedCount;
        private bool throwOut; // the last delivery failed: trash what's held instead of trying again
        private Station pickupFrom;
        private Station deliverToStation;
        private ServingCounter deliverToCounter;
        private TrashCan trash;
        private float timer;
        private float holdingUselessTimer;
        private Station helping;      // the station they stand behind while there's nothing to carry
        private bool helpingThere;    // true once they've arrived and are speeding it up
        private float rethinkTimer;
        private float untilBreak;
        private Transform breakSeat;   // the sofa seat while on a break
        private bool sittingDown;
        private float standY;
        private float breakTimer;
        private FloatingText breakLabel;

        private static readonly Color BreakBlue = new(0.6f, 0.85f, 1f);

        /// <summary>True while they're on a break (walking to the sofa or sitting on it).</summary>
        public bool OnBreak => breakSeat != null;

        private void Awake()
        {
            carrier = GetComponent<ItemCarrier>();
            TakeNothing();
            untilBreak = Random.Range(secondsBetweenBreaks.x, secondsBetweenBreaks.y) * BubbleTea.Extras.TeamSkills.BreakSpacing;
        }

        private void Update()
        {
            if (OnBreak)
            {
                TakeBreak();
                return;
            }
            untilBreak -= Time.deltaTime;

            switch (job)
            {
                case Job.None:
                    // Time for a break, and nothing in hand: head for the sofa.
                    if (untilBreak <= 0f && carrier.Stack.Count == 0 && StartBreak()) return;
                    Plan();
                    if (job == Job.None) HelpAtStation();
                    else StopHelping();
                    break;
                case Job.Fetch: Fetch(); break;
                case Job.Deliver: Deliver(); break;
                case Job.Trash: EmptyHands(); break;
            }
        }

        private void OnEnable() => BubbleTea.Extras.TeamSkills.CountWorker(BubbleTea.Extras.TeamSkills.Role.Barista, 1);

        private void OnDisable()
        {
            BubbleTea.Extras.TeamSkills.CountWorker(BubbleTea.Extras.TeamSkills.Role.Barista, -1);
            StopHelping();
            EndBreak();
        }

        // ---------- Breaks ----------

        private bool StartBreak()
        {
            breakSeat = StaffSofa.TakeSeat(this);
            if (breakSeat == null)
            {
                untilBreak = 10f; // the sofa is full: try again a little later
                return false;
            }
            StopHelping();
            sittingDown = false;
            return true;
        }

        private void TakeBreak()
        {
            if (!sittingDown)
            {
                if (!WalkTo(breakSeat.position)) return;
                standY = transform.position.y;
                transform.SetPositionAndRotation(breakSeat.position, breakSeat.rotation); // the seat sits at cushion height
                if (TryGetComponent(out WalkBob bob)) bob.Sitting = true;
                sittingDown = true;
                breakTimer = Random.Range(breakSeconds.x, breakSeconds.y);
                return;
            }
            breakLabel ??= FloatingText.Create(transform, Vector3.up * 1.9f, 0.04f);
            breakLabel.Show(Mathf.Repeat(Time.time, 1.5f) < 0.75f ? "ON BREAK z" : "ON BREAK zZ", BreakBlue);
            breakTimer -= Time.deltaTime;
            if (breakTimer <= 0f) EndBreak();
        }

        private void EndBreak()
        {
            if (breakSeat == null) return;
            if (sittingDown)
            {
                Vector3 standing = transform.position;
                standing.y = standY;
                transform.position = standing;
                if (TryGetComponent(out WalkBob bob)) bob.Sitting = false;
            }
            if (breakLabel != null) breakLabel.Hide();
            StaffSofa.FreeSeat(breakSeat);
            breakSeat = null;
            sittingDown = false;
            untilBreak = Random.Range(secondsBetweenBreaks.x, secondsBetweenBreaks.y) * BubbleTea.Extras.TeamSkills.BreakSpacing;
        }

        // ---------- Helping at a station ----------

        private void HelpAtStation()
        {
            rethinkTimer -= Time.deltaTime;
            if (helping == null || !helping.isActiveAndEnabled || rethinkTimer <= 0f)
            {
                rethinkTimer = rethinkSeconds;
                Station best = PickStation();
                if (best != helping)
                {
                    StopHelping();
                    helping = best;
                }
            }
            if (helping == null || !WalkTo(StandSpot(helping))) return;

            Vector3 toStation = helping.transform.position - transform.position;
            toStation.y = 0f;
            if (toStation.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toStation), 720f * Time.deltaTime);
            }
            if (!helpingThere)
            {
                helping.AddBarista();
                helpingThere = true;
            }
        }

        private void StopHelping()
        {
            if (helpingThere && helping != null) helping.RemoveBarista();
            helpingThere = false;
            helping = null;
        }

        // The station with the most demand that no other barista has taken. A small bonus for
        // the current station stops baristas from running back and forth between two.
        private Station PickStation()
        {
            Station best = null;
            float bestScore = float.MinValue;
            foreach (Station candidate in Station.All)
            {
                if (candidate == null) continue;
                bool mine = candidate == helping;
                if (!mine && (candidate.Baristas > 0 || TakenByAnother(candidate))) continue;

                float score = Demand(candidate) + (mine ? 1.5f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        private bool TakenByAnother(Station candidate)
        {
#if UNITY_2023_1_OR_NEWER
            Barista[] all = FindObjectsByType<Barista>(FindObjectsSortMode.None);
#else
            Barista[] all = FindObjectsOfType<Barista>();
#endif
            foreach (Barista other in all)
            {
                if (other != this && other.helping == candidate) return true;
            }
            return false;
        }

        // How much the people in line need this station: 2 per customer whose drink it makes,
        // 1 per customer whose drink is made from something it makes.
        private static float Demand(Station candidate)
        {
            float score = 0f;
            foreach (ServingCounter counter in ServingCounter.All)
            {
                foreach (ItemDefinition drink in counter.QueuedOrders())
                {
                    if (drink == null) continue;
                    if (candidate.Makes(drink))
                    {
                        score += 2f;
                        continue;
                    }
                    foreach (Station other in Station.All)
                    {
                        if (other != null && other != candidate && other.Makes(drink) && candidate.Makes(other.InputFor(drink)))
                        {
                            score += 1f;
                            break;
                        }
                    }
                }
            }
            return score;
        }

        // Where a barista stands while helping at a station.
        private Vector3 StandSpot(Station target)
        {
            // In front of the bar (the bars stand against the back wall), a little to one side
            // so it doesn't block the hand-off spot.
            Vector3 front = target.ZoneCenter - target.transform.position;
            front.y = 0f;
            if (front.sqrMagnitude < 0.0001f) front = target.transform.forward;
            front.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, front);
            return target.transform.position + front * standDistance + side * 0.7f;
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
            if (counter == null) return; // nobody waiting on anything: help at a station

            // Is the drink ready somewhere? Fetch exactly as many as the front customer still needs.
            Station ready = Station.FindReady(needed);
            if (ready != null)
            {
                int count = Mathf.Clamp(counter.Uncovered(needed), 1, CarryLimit());
                StartFetch(needed, ready, null, counter, count);
                Reserve(counter, needed, count);
                return;
            }

            // Otherwise feed a station that turns an ingredient into it (one ingredient: the drink
            // is reserved, so other baristas pick a different job).
            foreach (Station maker in Station.All)
            {
                ItemDefinition ingredient = maker.InputFor(needed);
                if (ingredient == null || !maker.Accepts(ingredient)) continue;
                Station source = Station.FindReady(ingredient);
                if (source == null || source == maker) continue;
                StartFetch(ingredient, source, maker, null, 1);
                Reserve(counter, needed);
                return;
            }
            // Nothing to do for that order yet: help at a station meanwhile. (Baristas no longer
            // stock the counter with drinks nobody ordered; those would only be thrown away.)
        }

        private void Reserve(ServingCounter counter, ItemDefinition drink, int count = 1)
        {
            ReleaseReservation();
            counter.Reserve(drink, count);
            reservedAt = counter;
            reservedDrink = drink;
            reservedCount = count;
        }

        // Already holding something (a job was cut short): find somewhere it's useful.
        private void PlanForHeldItem()
        {
            ItemDefinition held = null;
            carrier.Stack.Contains(i => { held = i; return true; });

            if (!throwOut)
            {
                foreach (ServingCounter counter in ServingCounter.All)
                {
                    if (!counter.Needs(held)) continue;
                    StartDeliver(held, null, counter);
                    Reserve(counter, held, Mathf.Min(carrier.Stack.CountOf(i => i == held), Mathf.Max(1, counter.Wanted(held))));
                    return;
                }
                // An ingredient for a drink the front customer is waiting on goes to the station that makes it.
                foreach (Station station in Station.All)
                {
                    if (!station.Accepts(held) || !IsWanted(OutputOf(station, held))) continue;
                    StartDeliver(held, station, null);
                    return;
                }
            }
            // Nobody wants it (or it couldn't be handed over): straight to the trash.

            holdingUselessTimer += secondsBetweenPlans;
            if (holdingUselessTimer < discardSeconds) return;
            holdingUselessTimer = 0f;

            trash = TrashCan.Nearest(transform.position);
            if (trash != null)
            {
                carrier.GiveOnlyTo = trash;
                timer = 0f;
                job = Job.Trash;
            }
            else if (carrier.Stack.TryTake(_ => true, out CarriedItem junk))
            {
                Destroy(junk.gameObject);
            }
        }

        // Walks to the trash can and stands there until their hands are empty.
        private void EmptyHands()
        {
            if (trash == null || carrier.Stack.Count == 0)
            {
                Finish();
                return;
            }
            if (!WalkTo(trash.ZoneCenter)) return;
            timer += Time.deltaTime;
            if (timer >= giveUpSeconds) Finish();
        }

        // Hands empty again: the next thing they hold gets a fresh chance to be delivered.
        private void LateUpdate()
        {
            if (carrier.Stack.Count == 0) throwOut = false;
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
            // The customer it was for left: think again (it goes in the trash if nobody wants it).
            if (deliverToCounter != null && !deliverToCounter.Needs(item))
            {
                Finish();
                return;
            }

            Vector3 spot = deliverToStation != null ? deliverToStation.ZoneCenter : deliverToCounter.ZoneCenter;
            if (!WalkTo(spot)) return;
            // Wait for the customer to step up to the counter before the clock starts.
            if (deliverToCounter != null && (deliverToCounter.FrontCustomer == null || !deliverToCounter.FrontCustomer.IsAtCounter)) return;
            timer += Time.deltaTime;
            if (timer < giveUpSeconds) return;
            // Nobody took it: don't stand here holding it, throw it out and get on with the next order.
            throwOut = true;
            Finish();
        }

        private void Cancel() => Finish();

        private void Finish()
        {
            ReleaseReservation();
            Clear();
        }

        private void ReleaseReservation()
        {
            if (reservedAt != null) reservedAt.Release(reservedDrink, reservedCount);
            reservedAt = null;
            reservedDrink = null;
            reservedCount = 0;
        }

        // True if the customer at the front of a line is still waiting for this drink.
        private static bool IsWanted(ItemDefinition drink)
        {
            if (drink == null) return false;
            foreach (ServingCounter counter in ServingCounter.All)
            {
                if (counter.Needs(drink)) return true;
            }
            return false;
        }

        // What this station makes from the given ingredient.
        private static ItemDefinition OutputOf(Station station, ItemDefinition ingredient)
        {
            foreach (ServingCounter counter in ServingCounter.All)
            {
                foreach (DrinkRecipe recipe in counter.Menu)
                {
                    if (recipe != null && station.InputFor(recipe.Drink) == ingredient) return recipe.Drink;
                }
            }
            return null;
        }

        private void Clear()
        {
            TakeNothing();
            item = null;
            pickupFrom = null;
            deliverToStation = null;
            deliverToCounter = null;
            trash = null;
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

        // One drink at a time, plus one for each level of the Carry More upgrade.
        private static int CarryLimit() => 1 + UpgradeManager.LevelOfStat(StatId.CarryBonus);

        // Moves toward the target on the floor. Returns true once close enough to stand in a zone.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.09f) return true;

            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f) * PayStatus.SpeedOf(this); // slower while their pay is late
            StaffSteering.Step(transform, target, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }
    }
}
