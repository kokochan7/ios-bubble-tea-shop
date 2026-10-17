using BubbleTea.Core;
using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired stocker (Matcha House, Night Market) who keeps the bars supplied: when a bar has room
    /// for a whole crate, they fetch one of the kind it uses from the supply dock and carry it there,
    /// emptiest bar first. With nothing to do they stand still beside the dock.
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

        private void OnEnable() => BubbleTea.Extras.TeamSkills.CountWorker(BubbleTea.Extras.TeamSkills.Role.Stocker, 1);
        private void OnDisable() => BubbleTea.Extras.TeamSkills.CountWorker(BubbleTea.Extras.TeamSkills.Role.Stocker, -1);

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
            SupplyDock dock = SupplyDock.First;
            timer += Time.deltaTime;
            if (timer < secondsBetweenPlans)
            {
                WaitByDock(dock);
                return;
            }
            timer = 0f;

            ItemDefinition held = HeldCrate(dock);
            if (held != null)
            {
                // Take it to the emptiest bar that uses this kind (or hold on to it until one does).
                Station needs = Neediest(s => s.SupplyItem == held);
                if (needs != null)
                {
                    target = needs;
                    carrier.GiveOnlyTo = target;
                    job = Job.Deliver;
                    return;
                }
            }
            else if (dock != null && dock.Count > 0)
            {
                Station needs = Neediest(s => dock.CountOf(s.SupplyItem) > 0);
                if (needs != null)
                {
                    ItemDefinition crate = needs.SupplyItem;
                    carrier.TakeFilter = i => i == crate && carrier.Stack.Count == 0;
                    job = Job.Fetch;
                    return;
                }
            }
            WaitByDock(dock);
        }

        // Nothing to do: walk to the spot beside the dock and stand still there, facing the kitchen.
        private void WaitByDock(SupplyDock dock)
        {
            if (dock == null || !WalkTo(dock.WaitSpot)) return;
            Vector3 toKitchen = dock.ZoneCenter - dock.transform.position;
            toKitchen.y = 0f;
            if (toKitchen.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toKitchen), 360f * Time.deltaTime);
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
            ItemDefinition supply = target != null ? target.SupplyItem : null;
            bool holding = supply != null && carrier.Stack.Contains(i => i == supply);
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

        // The first crate being carried, or null.
        private ItemDefinition HeldCrate(SupplyDock dock)
        {
            if (dock == null) return null;
            for (int i = 0; i < carrier.Stack.Count; i++)
            {
                CarriedItem item = carrier.Stack.At(i);
                if (item != null && dock.IsCrate(item.Definition)) return item.Definition;
            }
            return null;
        }

        // The bar with room for a crate (that also passes the check) that has the least left.
        private static Station Neediest(System.Func<Station, bool> also)
        {
            Station best = null;
            foreach (Station station in Station.All)
            {
                if (station == null || !station.WantsCrate || !also(station)) continue;
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
