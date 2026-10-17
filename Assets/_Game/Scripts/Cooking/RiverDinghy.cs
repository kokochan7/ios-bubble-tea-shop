using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A wooden dinghy (Night Market) that drifts down the river behind the stall every so often,
    /// stops at the jetty, unloads a few crates of fruit onto the <see cref="SupplyDock"/> and floats
    /// on downstream. It brings the fruits the bars are lowest on, and only comes once a bar that
    /// uses fruit is built. The crates ride in the boat (one model per crate it still has), and the
    /// boat bobs on the water.
    /// </summary>
    public class RiverDinghy : MonoBehaviour
    {
        [SerializeField, Tooltip("The dock it unloads at.")]
        private SupplyDock dock;

        [SerializeField, Tooltip("The boat model. Hidden while it's away.")]
        private Transform boat;

        [SerializeField, Tooltip("Crate models in the boat, shown one per crate it carries.")]
        private Transform[] cargo = System.Array.Empty<Transform>();

        [SerializeField, Tooltip("Where it comes from (upstream, off screen).")]
        private Vector3 upstream;

        [SerializeField, Tooltip("Where it stops by the jetty.")]
        private Vector3 jetty;

        [SerializeField, Tooltip("Where it floats off to (downstream, off screen).")]
        private Vector3 downstream;

        [SerializeField, Tooltip("Speed on the river in meters per second.")]
        private float speed = 4f;

        [SerializeField, Tooltip("Real seconds between boats, picked at random in this range.")]
        private Vector2 secondsBetweenBoats = new(30f, 60f);

        [SerializeField, Tooltip("Crates each boat brings, picked at random in this range.")]
        private Vector2Int cratesPerBoat = new(3, 6);

        [SerializeField, Tooltip("Seconds between crates while unloading.")]
        private float secondsPerCrate = 0.45f;

        private enum State { Away, Arriving, Unloading, Leaving }

        private State state;
        private float timer;
        private readonly List<ItemDefinition> load = new();

        private void Start()
        {
            timer = Random.Range(8f, 15f); // the first boat comes soon after a fruit bar is built
            if (boat != null) boat.gameObject.SetActive(false);
        }

        private void Update()
        {
            switch (state)
            {
                case State.Away:
                    if (dock == null || boat == null || !Station.AnyUsesSupplies) return;
                    timer -= Time.deltaTime;
                    if (timer > 0f || dock.IsFull) return;
                    PackLoad();
                    if (load.Count == 0)
                    {
                        timer = 5f;
                        return;
                    }
                    boat.position = upstream;
                    boat.gameObject.SetActive(true);
                    state = State.Arriving;
                    break;

                case State.Arriving:
                    if (!Float(jetty)) return;
                    timer = secondsPerCrate;
                    state = State.Unloading;
                    break;

                case State.Unloading:
                    Bob();
                    timer -= Time.deltaTime;
                    if (timer > 0f) return;
                    timer = secondsPerCrate;
                    if (load.Count > 0 && dock.Deliver(boat.position + Vector3.up * 0.8f, load[load.Count - 1]))
                    {
                        load.RemoveAt(load.Count - 1);
                        ShowCargo();
                        return;
                    }
                    state = State.Leaving; // all unloaded, or the dock is full
                    break;

                default:
                    if (!Float(downstream)) return;
                    boat.gameObject.SetActive(false);
                    load.Clear();
                    timer = Random.Range(secondsBetweenBoats.x, secondsBetweenBoats.y);
                    state = State.Away;
                    break;
            }
        }

        // Fills the boat with the fruits the bars need most (the emptiest bar's fruit first).
        private void PackLoad()
        {
            load.Clear();
            var needs = new List<Station>();
            foreach (Station station in Station.All)
            {
                if (station != null && station.UsesSupplies && dock.IsCrate(station.SupplyItem)) needs.Add(station);
            }
            if (needs.Count == 0) return;
            needs.Sort((a, b) => a.Supplies.CompareTo(b.Supplies));
            int count = Mathf.Min(Random.Range(cratesPerBoat.x, cratesPerBoat.y + 1), Mathf.Max(1, cargo.Length));
            for (int i = 0; i < count; i++)
            {
                // Mostly what the emptiest bars use, with a random one now and then.
                Station pick = Random.value < 0.7f ? needs[i % Mathf.Min(2, needs.Count)] : needs[Random.Range(0, needs.Count)];
                load.Add(pick.SupplyItem);
            }
            ShowCargo();
        }

        private void ShowCargo()
        {
            for (int i = 0; i < cargo.Length; i++)
            {
                if (cargo[i] != null) cargo[i].gameObject.SetActive(i < load.Count);
            }
        }

        // Floats toward a point, turning to face downstream. True once there.
        private bool Float(Vector3 target)
        {
            Vector3 flat = new(boat.position.x, target.y, boat.position.z);
            Vector3 offset = target - flat;
            if (offset.sqrMagnitude > 0.01f)
                boat.rotation = Quaternion.RotateTowards(boat.rotation, Quaternion.LookRotation(offset), 180f * Time.deltaTime);
            flat = Vector3.MoveTowards(flat, target, speed * Time.deltaTime);
            boat.position = flat;
            Bob();
            return (target - flat).sqrMagnitude < 0.01f;
        }

        // A gentle bob and roll on the water.
        private void Bob()
        {
            float t = Time.time;
            Vector3 spot = boat.position;
            spot.y = jetty.y + Mathf.Sin(t * 2.1f) * 0.05f;
            boat.position = spot;
            Vector3 euler = boat.rotation.eulerAngles;
            boat.rotation = Quaternion.Euler(Mathf.Sin(t * 1.7f) * 2f, euler.y, Mathf.Sin(t * 1.3f) * 3f);
        }
    }
}
