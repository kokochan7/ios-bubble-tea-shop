using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A bin for unwanted items. Anyone standing in its zone (the player or a barista heading
    /// here) drops what they carry into it, one item at a time.
    /// </summary>
    public class TrashCan : MonoBehaviour
    {
        private static readonly List<TrashCan> active = new();
        public static IReadOnlyList<TrashCan> All => active;

        [SerializeField, Tooltip("Where the player stands to throw things away.")]
        private HandoffZone zone = new();

        [SerializeField, Tooltip("Where items fly to before they disappear (the bin's opening).")]
        private Transform opening;

        /// <summary>Where a worker stands to throw things away.</summary>
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>The trash can closest to <paramref name="position"/>, or null if there isn't one.</summary>
        public static TrashCan Nearest(Vector3 position)
        {
            TrashCan best = null;
            float bestDistance = float.MaxValue;
            foreach (TrashCan can in active)
            {
                if (can == null) continue;
                float distance = (can.transform.position - position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = can;
            }
            return best;
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Update() => zone.Tick(transform, TakeOne);

        private bool TakeOne(ItemCarrier carrier)
        {
            if (!carrier.WillGiveTo(this) || !carrier.Stack.TryTake(_ => true, out CarriedItem item)) return false;
            item.MoveTo(opening != null ? opening : transform, Vector3.zero);
            Destroy(item.gameObject, 0.25f); // after its hop into the bin
            return true;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
