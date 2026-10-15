using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A pallet by the kitchen's side wall where the delivery truck leaves crates of supplies
    /// (Matcha House). Stand on its spot to pick a crate up, then carry it to a bar that's
    /// running low. Each carrier takes at most two crates, so the tray still has room for drinks.
    /// </summary>
    public class SupplyDock : MonoBehaviour
    {
        private static readonly List<SupplyDock> active = new();
        public static IReadOnlyList<SupplyDock> All => active;

        [SerializeField, Tooltip("The crate this dock holds.")]
        private ItemDefinition crate;

        [SerializeField, Tooltip("Crates waiting to be picked up.")]
        private ItemStack crates = new();

        [SerializeField, Tooltip("Where you stand to pick a crate up.")]
        private HandoffZone zone = new();

        [SerializeField, Min(1), Tooltip("Most crates one carrier takes.")]
        private int maxPerCarrier = 2;

        public ItemDefinition Crate => crate;
        public Vector3 ZoneCenter => zone.Center(transform);
        public int Count => crates.Count;
        public bool IsFull => crates.IsFull;

        /// <summary>The first dock in the shop, or null.</summary>
        public static SupplyDock First => active.Count > 0 ? active[0] : null;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        /// <summary>The truck drops a crate off, flying in from <paramref name="from"/>. False if the dock is full.</summary>
        public bool Deliver(Vector3 from)
        {
            if (crate == null || crates.IsFull) return false;
            CarriedItem item = crate.Spawn(from);
            if (crates.Push(item)) return true;
            Destroy(item.gameObject);
            return false;
        }

        private void Update() => zone.Tick(transform, Handoff);

        private bool Handoff(ItemCarrier carrier)
        {
            if (carrier.Stack.IsFull || carrier.Stack.CountOf(i => i == crate) >= maxPerCarrier) return false;
            if (!crates.TryTake(carrier.WillTake, out CarriedItem item)) return false;
            carrier.Stack.Push(item);
            return true;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
