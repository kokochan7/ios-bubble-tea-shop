using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A little truck that brings crates of supplies to the <see cref="SupplyDock"/> every so often
    /// (Matcha House): it drives along its route to the kitchen's side wall, drops a few crates
    /// onto the dock and drives back the way it came. It only comes once a bar that uses supplies
    /// is built, and not while the dock is full.
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

        [SerializeField, Tooltip("Seconds between deliveries.")]
        private float secondsBetweenDeliveries = 30f;

        [SerializeField, Min(1), Tooltip("Crates dropped off each time.")]
        private int cratesPerDelivery = 3;

        [SerializeField, Tooltip("Seconds between crates while unloading.")]
        private float secondsPerCrate = 0.4f;

        private enum State { Waiting, Arriving, Unloading, Leaving }

        private State state;
        private float timer;
        private int waypoint;
        private int toDrop;

        private void Start()
        {
            timer = secondsBetweenDeliveries;
            if (truck != null) truck.gameObject.SetActive(false);
        }

        private void Update()
        {
            switch (state)
            {
                case State.Waiting:
                    if (dock == null || route.Length == 0 || !Station.AnyUsesSupplies || dock.IsFull) return;
                    timer -= Time.deltaTime;
                    if (timer > 0f) return;
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
                    toDrop = cratesPerDelivery;
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
                    timer = secondsBetweenDeliveries;
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
