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
    /// Each job is reserved at the counter, and drinks already being made count too, so two
    /// baristas never work on the same drink. Anything they hold that no customer wants goes
    /// straight in the nearest trash can.
    /// With nothing to carry, they stand behind the station customers need most and make it work
    /// faster (one barista per station). They swap items through the same floor zones the player
    /// uses. The Walk Speed and Carry More upgrades apply to baristas too: they walk faster and
    /// carry several of the same item per trip.
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

        private enum Job { None, Fetch, Deliver, Trash }

        private ItemCarrier carrier;
        private Job job;
        private ItemDefinition item;
        private int want = 1; // how many of the item to pick up this trip
        private ServingCounter reservedAt; // the counter this job holds a reservation on, if any
        private ItemDefinition reservedDrink;
        private Station pickupFrom;
        private Station deliverToStation;
        private ServingCounter deliverToCounter;
        private TrashCan trash;
        private float timer;
        private float holdingUselessTimer;
        private Station helping;      // the station they stand behind while there's nothing to carry
        private bool helpingThere;    // true once they've arrived and are speeding it up
        private float rethinkTimer;

        private void Awake()
        {
            carrier = GetComponent<ItemCarrier>();
            TakeNothing();
        }

        private void Update()
        {
            switch (job)
            {
                case Job.None:
                    Plan();
                    if (job == Job.None) HelpAtStation();
                    else StopHelping();
                    break;
                case Job.Fetch: Fetch(); break;
                case Job.Deliver: Deliver(); break;
                case Job.Trash: EmptyHands(); break;
            }
        }

        private void OnDisable() => StopHelping();

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

            // Is the drink ready somewhere? Fetch it for the counter.
            Station ready = Station.FindReady(needed);
            if (ready != null)
            {
                StartFetch(needed, ready, null, counter, CarryLimit());
                Reserve(counter, needed);
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

        private void Reserve(ServingCounter counter, ItemDefinition drink)
        {
            ReleaseReservation();
            counter.Reserve(drink);
            reservedAt = counter;
            reservedDrink = drink;
        }

        // Already holding something (a job was cut short): find somewhere it's useful.
        private void PlanForHeldItem()
        {
            ItemDefinition held = null;
            carrier.Stack.Contains(i => { held = i; return true; });

            foreach (ServingCounter counter in ServingCounter.All)
            {
                if (!counter.Needs(held)) continue;
                StartDeliver(held, null, counter);
                Reserve(counter, held);
                return;
            }
            // An ingredient for a drink someone is waiting on goes to the station that makes it.
            foreach (Station station in Station.All)
            {
                if (!station.Accepts(held) || !IsWanted(OutputOf(station, held))) continue;
                StartDeliver(held, station, null);
                return;
            }
            // Nobody wants it: straight to the trash.

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
            if (reservedAt != null) reservedAt.Release(reservedDrink);
            reservedAt = null;
            reservedDrink = null;
        }

        // True if a customer in line is still waiting for this drink.
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
        // The Walk Speed upgrade speeds runners up as well.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.09f) return true;

            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f);
            StaffSteering.Step(transform, target, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }
    }
}
