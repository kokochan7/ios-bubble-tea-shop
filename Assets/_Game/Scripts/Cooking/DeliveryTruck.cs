using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A little truck that brings crates of supplies to the <see cref="SupplyDock"/> twice a day
    /// (Matcha House), at set hours on the shop's clock: it drives along its route to the kitchen's
    /// side wall, drops 6 to 12 crates onto the dock and drives back the way it came. It only comes
    /// once a bar that uses supplies is built; if the dock is full when it's due, it comes as soon
    /// as there's room.
    /// </summary>
    public class DeliveryTruck : MonoBehaviour
    {
        [SerializeField, Tooltip("The dock it delivers to.")]
        private SupplyDock dock;

        [SerializeField, Tooltip("The truck model. Hidden while it's away.")]
        private Transform truck;

        [SerializeField, Tooltip("Points it drives through, from off screen to where it parks by the dock.")]
        private Vector3[] route = System.Array.Empty<Vector3>();

        [SerializeField, Tooltip("Driving speed in meters per second.")]
        private float speed = 9f;

        [SerializeField, Tooltip("Hours of the day (on the shop's clock) when the truck comes.")]
        private float[] deliveryHours = { 8f, 16f };

        [SerializeField, Tooltip("Crates dropped off each time, picked at random in this range.")]
        private Vector2Int cratesPerDelivery = new(6, 12);

        [SerializeField, Tooltip("Without a shop clock: real seconds between deliveries.")]
        private float secondsBetweenDeliveries = 72f;

        [SerializeField, Tooltip("Seconds between crates while unloading.")]
        private float secondsPerCrate = 0.4f;

        private enum State { Waiting, Arriving, Unloading, Leaving }

        private State state;
        private float timer;
        private int waypoint;
        private int toDrop;
        private bool due;           // a delivery hour has passed and the truck hasn't come yet
        private float lastMinute = -1f;

        private void Start()
        {
            timer = secondsBetweenDeliveries;
            if (truck != null) truck.gameObject.SetActive(false);
        }

        // True when the clock has just passed one of the delivery hours (or, with no clock, the timer ran out).
        private bool DeliveryHourPassed()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null)
            {
                timer -= Time.deltaTime;
                if (timer > 0f) return false;
                timer = secondsBetweenDeliveries;
                return true;
            }
            float now = clock.MinuteOfDay;
            float before = lastMinute;
            lastMinute = now;
            if (before < 0f) return false;
            foreach (float hour in deliveryHours)
            {
                float at = hour * 60f;
                // Passed it this frame (also across midnight, when the clock wraps around).
                if (now >= before ? before < at && now >= at : before < at || now >= at) return true;
            }
            return false;
        }

        private void Update()
        {
            switch (state)
            {
                case State.Waiting:
                    if (DeliveryHourPassed()) due = Station.AnyUsesSupplies; // nothing to bring before a bar needs supplies
                    if (!due || dock == null || route.Length == 0 || dock.IsFull) return;
                    due = false;
                    waypoint = 0;
                    if (truck != null)
                    {
                        truck.position = route[0];
                        truck.gameObject.SetActive(true);
                    }
                    state = State.Arriving;
                    break;

                case State.Arriving:
                    if (!Drive(route[waypoint])) return;
                    if (waypoint < route.Length - 1)
                    {
                        waypoint++;
                        return;
                    }
                    toDrop = Random.Range(cratesPerDelivery.x, cratesPerDelivery.y + 1);
                    timer = secondsPerCrate;
                    state = State.Unloading;
                    break;

                case State.Unloading:
                    timer -= Time.deltaTime;
                    if (timer > 0f) return;
                    timer = secondsPerCrate;
                    Vector3 from = truck != null ? truck.position + Vector3.up * 1.5f : dock.transform.position;
                    if (toDrop > 0 && dock.Deliver(from))
                    {
                        toDrop--;
                        return;
                    }
                    state = State.Leaving; // all dropped off, or the dock is full
                    break;

                default:
                    if (!Drive(route[waypoint])) return;
                    if (waypoint > 0)
                    {
                        waypoint--;
                        return;
                    }
                    if (truck != null) truck.gameObject.SetActive(false);
                    state = State.Waiting;
                    break;
            }
        }

        // Drives toward a point, turning to face where it's going. True once there.
        private bool Drive(Vector3 target)
        {
            if (truck == null) return true;
            Vector3 offset = target - truck.position;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.01f) return true;
            truck.rotation = Quaternion.RotateTowards(truck.rotation, Quaternion.LookRotation(offset), 360f * Time.deltaTime);
            truck.position = Vector3.MoveTowards(truck.position, target, speed * Time.deltaTime);
            return false;
        }
    }
}
