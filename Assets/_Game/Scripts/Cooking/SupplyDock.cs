using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A pallet where crates of supplies are left: by the Matcha House's side wall (the delivery truck
    /// brings matcha powder) or on the Night Market's river jetty (a dinghy brings four kinds of fruit).
    /// Stand on its spot to pick a crate up, then carry it to a bar that's running low; with several
    /// kinds, you're handed the one the neediest bar wants. Each carrier takes at most two crates, so
    /// the tray still has room for drinks.
    /// </summary>
    public class SupplyDock : MonoBehaviour
    {
        private static readonly List<SupplyDock> active = new();
        public static IReadOnlyList<SupplyDock> All => active;

        [SerializeField, Tooltip("The crate this dock holds.")]
        private ItemDefinition crate;

        [SerializeField, Tooltip("Other kinds of crate it also holds (the Night Market's fruits).")]
        private ItemDefinition[] moreCrates = System.Array.Empty<ItemDefinition>();

        [SerializeField, Tooltip("Crates waiting to be picked up.")]
        private ItemStack crates = new();

        [SerializeField, Tooltip("Where you stand to pick a crate up.")]
        private HandoffZone zone = new();

        [SerializeField, Min(1), Tooltip("Most crates one carrier takes.")]
        private int maxPerCarrier = 2;

        [SerializeField, Tooltip("Where an idle stocker stands, relative to the dock (beside the pallet).")]
        private Vector3 waitOffset = new(1.4f, 0f, 0f);

        public ItemDefinition Crate => crate;

        /// <summary>True for any kind of crate this dock holds.</summary>
        public bool IsCrate(ItemDefinition item) => item != null && (item == crate || System.Array.IndexOf(moreCrates, item) >= 0);

        /// <summary>Crates of this kind waiting on the dock.</summary>
        public int CountOf(ItemDefinition kind) => crates.CountOf(i => i == kind);
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>Where an idle stocker waits, out of the way beside the pallet.</summary>
        public Vector3 WaitSpot => transform.TransformPoint(waitOffset);
        public int Count => crates.Count;
        public bool IsFull => crates.IsFull;

        /// <summary>The first dock in the shop, or null.</summary>
        public static SupplyDock First => active.Count > 0 ? active[0] : null;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        /// <summary>The truck drops a crate off, flying in from <paramref name="from"/>. False if the dock is full.</summary>
        public bool Deliver(Vector3 from) => Deliver(from, crate);

        /// <summary>Drops off a crate of one kind (a dinghy's fruit). False if the dock is full.</summary>
        public bool Deliver(Vector3 from, ItemDefinition kind)
        {
            if (kind == null || crates.IsFull) return false;
            CarriedItem item = kind.Spawn(from);
            if (crates.Push(item)) return true;
            Destroy(item.gameObject);
            return false;
        }

        private void Update() => zone.Tick(transform, Handoff);

        private bool Handoff(ItemCarrier carrier)
        {
            if (carrier.Stack.IsFull || carrier.Stack.CountOf(IsCrate) >= maxPerCarrier) return false;
            // The kind the emptiest bar is waiting for first, then anything.
            ItemDefinition wanted = Wanted();
            if (!(wanted != null && crates.TryTake(i => i == wanted && carrier.WillTake(i), out CarriedItem item))
                && !crates.TryTake(carrier.WillTake, out item)) return false;
            carrier.Stack.Push(item);
            return true;
        }

        // The supply the emptiest bar that has room for a crate uses, if this dock has one.
        private ItemDefinition Wanted()
        {
            Station best = null;
            foreach (Station station in Station.All)
            {
                if (station == null || !station.WantsCrate || CountOf(station.SupplyItem) == 0) continue;
                if (best == null || station.Supplies < best.Supplies) best = station;
            }
            return best != null ? best.SupplyItem : null;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
