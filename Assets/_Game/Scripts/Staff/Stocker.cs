using BubbleTea.Core;
using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired stocker (Matcha House) who keeps the bars supplied: when a bar has room for a whole
    /// crate, they fetch one from the supply dock and carry it there, emptiest bar first. With
    /// nothing to do they wait by the dock. The Walk Speed upgrade makes them faster.
    /// </summary>
    [RequireComponent(typeof(ItemCarrier))]
    public class Stocker : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second, before upgrades.")]
        private float speed = 3.5f;

        [SerializeField, Tooltip("Seconds between looking for a new job while idle.")]
        private float secondsBetweenPlans = 0.3f;

        [SerializeField, Tooltip("Seconds to wait at a spot for the hand-off before giving up.")]
        private float giveUpSeconds = 4f;

        private enum Job { None, Fetch, Deliver }

        private ItemCarrier carrier;
        private Job job;
        private Station target;
        private float timer;

        private void Awake()
        {
            carrier = GetComponent<ItemCarrier>();
            HoldOn();
        }

        private void Update()
        {
            switch (job)
            {
                case Job.Fetch: Fetch(); break;
                case Job.Deliver: Deliver(); break;
                default: Plan(); break;
            }
        }

        private void Plan()
        {
            timer += Time.deltaTime;
            if (timer < secondsBetweenPlans) return;
            timer = 0f;

            SupplyDock dock = SupplyDock.First;
            Station neediest = Neediest();
            bool holding = dock != null && carrier.Stack.Contains(i => i == dock.Crate);
            if (holding && neediest != null)
            {
                target = neediest;
                carrier.GiveOnlyTo = target;
                job = Job.Deliver;
                return;
            }
            if (!holding && neediest != null && dock != null && dock.Count > 0)
            {
                ItemDefinition crate = dock.Crate;
                carrier.TakeFilter = i => i == crate && carrier.Stack.Count == 0;
                job = Job.Fetch;
                return;
            }
            // Nothing to do: wait by the dock.
            if (dock != null) WalkTo(dock.ZoneCenter + new Vector3(0f, 0f, -1.2f));
        }

        private void Fetch()
        {
            SupplyDock dock = SupplyDock.First;
            if (dock == null || carrier.Stack.Count > 0 || dock.Count == 0)
            {
                Done();
                return;
            }
            if (!WalkTo(dock.ZoneCenter)) return;
            timer += Time.deltaTime;
            if (timer >= giveUpSeconds) Done();
        }

        private void Deliver()
        {
            SupplyDock dock = SupplyDock.First;
            bool holding = dock != null && carrier.Stack.Contains(i => i == dock.Crate);
            // Handed over, or someone else filled the bar first: think again.
            if (target == null || !holding || !target.WantsCrate)
            {
                Done();
                return;
            }
            if (!WalkTo(target.ZoneCenter)) return;
            timer += Time.deltaTime;
            if (timer >= giveUpSeconds) Done();
        }

        private void Done()
        {
            HoldOn();
            target = null;
            timer = 0f;
            job = Job.None;
        }

        // Between jobs: keep what's held, pick nothing up.
        private void HoldOn()
        {
            carrier.TakeFilter = _ => false;
            carrier.GiveOnlyTo = this;
        }

        // The bar with room for a crate that has the least left.
        private static Station Neediest()
        {
            Station best = null;
            foreach (Station station in Station.All)
            {
                if (station == null || !station.WantsCrate) continue;
                if (best == null || station.Supplies < best.Supplies) best = station;
            }
            return best;
        }

        // Moves toward the target on the floor. Returns true once close enough to stand in a zone.
        private bool WalkTo(Vector3 destination)
        {
            destination.y = transform.position.y;
            Vector3 offset = destination - transform.position;
            if (offset.sqrMagnitude < 0.09f) return true;

            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f) * PayStatus.SpeedOf(this); // slower while their pay is late
            StaffSteering.Step(transform, destination, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }
    }
}
