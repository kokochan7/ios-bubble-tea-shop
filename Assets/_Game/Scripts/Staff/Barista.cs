using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired barista. They pick the station customers need most, stand behind it, and make it
    /// work faster while they're there. Every few seconds they look again and move if another
    /// station is clearly busier. Only one barista works at each station. The Walk Speed
    /// upgrade makes them walk faster too (Brew Speed already speeds up every station).
    /// </summary>
    public class Barista : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second.")]
        private float speed = 3.5f;

        [SerializeField, Tooltip("Seconds between looking for a busier station.")]
        private float rethinkSeconds = 8f;

        [SerializeField, Tooltip("How far behind the station they stand, in meters.")]
        private float standDistance = 1.1f;

        private Station station;
        private bool working;
        private float rethinkTimer;

        private void Update()
        {
            rethinkTimer -= Time.deltaTime;
            if (station == null || !station.isActiveAndEnabled || rethinkTimer <= 0f)
            {
                rethinkTimer = rethinkSeconds;
                Station best = PickStation();
                if (best != station) SwitchTo(best);
            }
            if (station == null) return;

            if (WalkTo(StandSpot(station)))
            {
                Vector3 toStation = station.transform.position - transform.position;
                toStation.y = 0f;
                if (toStation.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toStation), 720f * Time.deltaTime);
                }
                if (!working)
                {
                    station.AddBarista();
                    working = true;
                }
            }
        }

        private void SwitchTo(Station next)
        {
            if (working && station != null) station.RemoveBarista();
            working = false;
            station = next;
        }

        private void OnDisable() => SwitchTo(null);

        // The station with the most demand that no other barista has taken. A small bonus for
        // the current station stops baristas from running back and forth between two.
        private Station PickStation()
        {
            Station best = null;
            float bestScore = float.MinValue;
            foreach (Station candidate in Station.All)
            {
                if (candidate == null) continue;
                bool mine = candidate == station;
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

        // Whether another barista is already walking to this station.
        private bool TakenByAnother(Station candidate)
        {
            foreach (Barista other in FindBaristas())
            {
                if (other != this && other.station == candidate) return true;
            }
            return false;
        }

        private static Barista[] FindBaristas()
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<Barista>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<Barista>();
#endif
        }

        // How much the people in line need this station: 2 per customer whose drink it makes,
        // 1 per customer whose drink is made from something it makes.
        private static float Demand(Station candidate)
        {
            float score = 0f;
            foreach (ServingCounter counter in ServingCounter.All)
            {
                foreach (Station other in Station.All)
                {
                    // Drinks finished at "other" whose ingredient comes from the candidate.
                    if (other == null) continue;
                    foreach (ItemDefinition drink in DrinksOn(counter))
                    {
                        if (other == candidate && candidate.Makes(drink)) score += 2f;
                        else if (other != candidate && other.Makes(drink) && candidate.Makes(other.InputFor(drink))) score += 1f;
                    }
                }
            }
            return score;
        }

        private static System.Collections.Generic.IEnumerable<ItemDefinition> DrinksOn(ServingCounter counter)
        {
            foreach (ItemDefinition drink in counter.QueuedOrders())
            {
                if (drink != null) yield return drink;
            }
        }

        // Behind the station, on the opposite side from where the player stands.
        private Vector3 StandSpot(Station target)
        {
            Vector3 away = target.transform.position - target.ZoneCenter;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -target.transform.forward;
            return target.transform.position + away.normalized * standDistance;
        }

        // Moves toward the target on the floor. Returns true once there.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.04f) return true;

            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f);
            transform.position = Vector3.MoveTowards(transform.position, target, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }
    }
}
