using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Wooden dinghies (Night Market) that keep drifting down the river behind the stall, a few on
    /// the water at once. When the <see cref="SupplyDock"/> has room and a bar uses fruit, the next
    /// boat stops at the jetty, unloads a few crates (the fruits the bars are lowest on) and floats
    /// on downstream; every other boat just passes by on the far side of the river. Only one boat
    /// uses the jetty at a time. Crates ride in the boats (one model per crate) and the boats bob.
    /// </summary>
    public class RiverDinghy : MonoBehaviour
    {
        [System.Serializable]
        public class Boat
        {
            [Tooltip("The boat model. Hidden while it's away.")]
            public Transform model;

            [Tooltip("Crate models in the boat, shown one per crate it carries.")]
            public Transform[] cargo = System.Array.Empty<Transform>();
        }

        private enum State { Away, Arriving, Unloading, Leaving }

        // One boat's trip down the river.
        private sealed class Trip
        {
            public State state;
            public bool delivering;
            public float timer, lane;
            public readonly List<ItemDefinition> load = new();
        }

        [SerializeField, Tooltip("The dock it unloads at.")]
        private SupplyDock dock;

        [SerializeField, Tooltip("The boats, each sent down the river in turn.")]
        private Boat[] boats = System.Array.Empty<Boat>();

        [SerializeField, Tooltip("Where boats come from (upstream, off screen, middle of the river).")]
        private Vector3 upstream;

        [SerializeField, Tooltip("Where a delivering boat stops by the jetty.")]
        private Vector3 jetty;

        [SerializeField, Tooltip("Where boats float off to (downstream, off screen, middle of the river).")]
        private Vector3 downstream;

        [SerializeField, Tooltip("Passing boats keep to the far side: this many meters past the middle, at most.")]
        private float farLane = 1.4f;

        [SerializeField, Tooltip("Speed on the river in meters per second.")]
        private float speed = 4f;

        [SerializeField, Tooltip("Real seconds between boats setting off, picked at random in this range.")]
        private Vector2 secondsBetweenBoats = new(5f, 10f);

        [SerializeField, Tooltip("Crates each delivering boat brings, picked at random in this range.")]
        private Vector2Int cratesPerBoat = new(3, 6);

        [SerializeField, Tooltip("Seconds between crates while unloading.")]
        private float secondsPerCrate = 0.45f;

        private Trip[] trips = System.Array.Empty<Trip>();
        private float launchTimer;
        private bool jettyBusy;

        private void Start()
        {
            trips = new Trip[boats.Length];
            for (int i = 0; i < boats.Length; i++)
            {
                trips[i] = new Trip();
                if (boats[i].model != null) boats[i].model.gameObject.SetActive(false);
            }
            launchTimer = Random.Range(2f, 5f); // the first boat comes along soon
        }

        private void Update()
        {
            launchTimer -= Time.deltaTime;
            if (launchTimer <= 0f && Launch()) launchTimer = Random.Range(secondsBetweenBoats.x, secondsBetweenBoats.y);
            for (int i = 0; i < boats.Length; i++)
            {
                if (boats[i].model != null) Step(boats[i], trips[i]);
            }
        }

        // Sends a waiting boat down the river. False when every boat is already out.
        private bool Launch()
        {
            for (int i = 0; i < boats.Length; i++)
            {
                Trip trip = trips[i];
                Boat boat = boats[i];
                if (trip.state != State.Away || boat.model == null) continue;
                trip.load.Clear();
                trip.delivering = !jettyBusy && dock != null && Station.AnyUsesSupplies && !dock.IsFull;
                if (trip.delivering) PackLoad(trip, boat);
                trip.delivering = trip.load.Count > 0;
                if (trip.delivering)
                {
                    jettyBusy = true;
                    trip.lane = jetty.z; // keeps to the near side, by the jetty
                    ShowCargo(boat, trip.load.Count);
                }
                else
                {
                    trip.lane = upstream.z + Random.Range(0.3f, farLane);
                    ShowCargo(boat, Random.Range(0, boat.cargo.Length + 1)); // some carry fruit to market further on
                }
                boat.model.position = new Vector3(upstream.x, upstream.y, trip.lane);
                boat.model.rotation = Quaternion.LookRotation(downstream - upstream);
                boat.model.gameObject.SetActive(true);
                trip.state = State.Arriving;
                return true;
            }
            return false;
        }

        private void Step(Boat boat, Trip trip)
        {
            switch (trip.state)
            {
                case State.Away:
                    return;

                case State.Arriving:
                    if (!trip.delivering)
                    {
                        trip.state = State.Leaving;
                        return;
                    }
                    if (!Float(boat.model, jetty)) return;
                    trip.timer = secondsPerCrate;
                    trip.state = State.Unloading;
                    break;

                case State.Unloading:
                    Bob(boat.model);
                    trip.timer -= Time.deltaTime;
                    if (trip.timer > 0f) return;
                    trip.timer = secondsPerCrate;
                    if (trip.load.Count > 0 && dock.Deliver(boat.model.position + Vector3.up * 0.8f, trip.load[trip.load.Count - 1]))
                    {
                        trip.load.RemoveAt(trip.load.Count - 1);
                        ShowCargo(boat, trip.load.Count);
                        return;
                    }
                    jettyBusy = false; // all unloaded, or the dock is full
                    trip.state = State.Leaving;
                    break;

                default:
                    if (!Float(boat.model, new Vector3(downstream.x, downstream.y, trip.lane))) return;
                    boat.model.gameObject.SetActive(false);
                    trip.load.Clear();
                    trip.state = State.Away;
                    break;
            }
        }

        // Fills the boat with the fruits the bars need most (the emptiest bar's fruit first).
        private void PackLoad(Trip trip, Boat boat)
        {
            var needs = new List<Station>();
            foreach (Station station in Station.All)
            {
                if (station != null && station.UsesSupplies && dock.IsCrate(station.SupplyItem)) needs.Add(station);
            }
            if (needs.Count == 0) return;
            needs.Sort((a, b) => a.Supplies.CompareTo(b.Supplies));
            int count = Mathf.Min(Random.Range(cratesPerBoat.x, cratesPerBoat.y + 1), Mathf.Max(1, boat.cargo.Length));
            for (int i = 0; i < count; i++)
            {
                // Mostly what the emptiest bars use, with a random one now and then.
                Station pick = Random.value < 0.7f ? needs[i % Mathf.Min(2, needs.Count)] : needs[Random.Range(0, needs.Count)];
                trip.load.Add(pick.SupplyItem);
            }
        }

        private static void ShowCargo(Boat boat, int count)
        {
            for (int i = 0; i < boat.cargo.Length; i++)
            {
                if (boat.cargo[i] != null) boat.cargo[i].gameObject.SetActive(i < count);
            }
        }

        // Floats toward a point, turning to face where it's going. True once there.
        private bool Float(Transform model, Vector3 target)
        {
            Vector3 flat = new(model.position.x, target.y, model.position.z);
            Vector3 offset = target - flat;
            if (offset.sqrMagnitude > 0.01f)
                model.rotation = Quaternion.RotateTowards(model.rotation, Quaternion.LookRotation(offset), 180f * Time.deltaTime);
            flat = Vector3.MoveTowards(flat, target, speed * Time.deltaTime);
            model.position = flat;
            Bob(model);
            return (target - flat).sqrMagnitude < 0.01f;
        }

        // A gentle bob and roll on the water (each boat a little out of step with the others).
        private void Bob(Transform model)
        {
            float t = Time.time + model.GetSiblingIndex() * 1.3f;
            Vector3 spot = model.position;
            spot.y = jetty.y + Mathf.Sin(t * 2.1f) * 0.05f;
            model.position = spot;
            Vector3 euler = model.rotation.eulerAngles;
            model.rotation = Quaternion.Euler(Mathf.Sin(t * 1.7f) * 2f, euler.y, Mathf.Sin(t * 1.3f) * 3f);
        }
    }
}
