using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// The staff sofa against the kitchen's back wall. Baristas on a break sit on one of its seats;
    /// each seat holds one barista at a time.
    /// </summary>
    public class StaffSofa : MonoBehaviour
    {
        private static readonly List<StaffSofa> active = new();

        [SerializeField, Tooltip("Where a barista sits (facing out from the sofa).")]
        private Transform[] seats = System.Array.Empty<Transform>();

        private readonly Dictionary<Transform, Object> takenBy = new();

        private void Awake() => BubbleTea.Building.Movable.Make(gameObject, "StaffSofa", tapToMove: true);

        /// <summary>True while a barista has a seat here (sitting or on their way).</summary>
        public bool InUse
        {
            get
            {
                foreach (KeyValuePair<Transform, Object> taken in takenBy) if (taken.Value != null) return true;
                return false;
            }
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        /// <summary>Claims a free seat on any sofa in the shop. Null if there's none.</summary>
        public static Transform TakeSeat(Object who)
        {
            foreach (StaffSofa sofa in active)
            {
                foreach (Transform seat in sofa.seats)
                {
                    if (seat == null || sofa.takenBy.TryGetValue(seat, out Object owner) && owner != null) continue;
                    sofa.takenBy[seat] = who;
                    return seat;
                }
            }
            return null;
        }

        /// <summary>Frees a seat taken with <see cref="TakeSeat"/>.</summary>
        public static void FreeSeat(Transform seat)
        {
            if (seat == null) return;
            foreach (StaffSofa sofa in active) sofa.takenBy.Remove(seat);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => active.Clear();
    }
}
