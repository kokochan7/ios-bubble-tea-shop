using System.Collections.Generic;
using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// A café table with seats. After buying, some customers sit here for a while, leave a
    /// little extra money and an empty cup behind. A seat with a mess on it can't be used until
    /// someone walks up to the table to clean it, and messy tables lower the shop rating.
    /// </summary>
    public class SeatingTable : MonoBehaviour
    {
        private static readonly List<SeatingTable> active = new();

        [SerializeField, Tooltip("Where each customer sits. Their forward direction should face the table.")]
        private Transform[] seats;

        [SerializeField, Tooltip("The empty cup left on the table, one per seat (same order as the seats).")]
        private GameObject[] messes;

        [SerializeField, Tooltip("Stand here to clean the table.")]
        private HandoffZone zone = new();

        [SerializeField, Tooltip("Where the extra money from seated customers piles up.")]
        private CashPile cashPile;

        [SerializeField, Min(0), Tooltip("Extra money a seated customer leaves.")]
        private int bonusPerVisit = 6;

        private Customer[] occupants;
        private float cleanTimer;

        /// <summary>Tables in the shop right now.</summary>
        public static IReadOnlyList<SeatingTable> All => active;

        /// <summary>Where the money from seated customers piles up.</summary>
        public CashPile CashPile => cashPile;

        /// <summary>Where someone stands to clean the table.</summary>
        public Vector3 ZoneCenter => zone.Center(transform);

        /// <summary>True if an empty seat has a cup left on it.</summary>
        public bool NeedsCleaning
        {
            get
            {
                for (int i = 0; i < seats.Length; i++) if (IsMessy(i) && occupants[i] == null) return true;
                return false;
            }
        }

        /// <summary>Messy seats in the whole shop.</summary>
        public static int MessCount
        {
            get
            {
                int count = 0;
                foreach (SeatingTable table in active) count += table.Messes;
                return count;
            }
        }

        private int Messes
        {
            get
            {
                int count = 0;
                foreach (GameObject mess in messes) if (mess != null && mess.activeSelf) count++;
                return count;
            }
        }

        private void Awake() => occupants = new Customer[seats.Length];

        private void OnEnable()
        {
            active.Add(this);
            foreach (GameObject mess in messes) if (mess != null) mess.SetActive(false);
            GameEvents.RaiseMessChanged(MessCount);
        }

        private void OnDisable()
        {
            active.Remove(this);
            GameEvents.RaiseMessChanged(MessCount);
        }

        private void Update() => zone.Tick(transform, Clean);

        private static readonly List<(SeatingTable, int)> free = new();

        /// <summary>Finds a clean, empty seat anywhere in the shop and holds it for the customer.</summary>
        public static bool TryReserve(Customer customer, out SeatingTable table, out int seat)
        {
            // Any free, clean seat, picked at random so the tables fill up in no set order.
            free.Clear();
            foreach (SeatingTable candidate in active)
            {
                for (int i = 0; i < candidate.seats.Length; i++)
                {
                    if (candidate.occupants[i] == null && !candidate.IsMessy(i)) free.Add((candidate, i));
                }
            }
            if (free.Count == 0)
            {
                table = null;
                seat = -1;
                return false;
            }
            (table, seat) = free[UnityEngine.Random.Range(0, free.Count)];
            table.occupants[seat] = customer;
            return true;
        }

        public Transform Seat(int seat) => seats[seat];

        /// <summary>True while a customer sits here or is on the way to a seat.</summary>
        public bool InUse
        {
            get
            {
                if (occupants == null) return false;
                foreach (Customer occupant in occupants) if (occupant != null) return true;
                return false;
            }
        }

        /// <summary>True while nobody has this seat (a shop pet may nap on it until a customer comes).</summary>
        public bool SeatFree(int seat) => seat >= 0 && seat < seats.Length && occupants[seat] == null;

        /// <summary>The nearest empty seat within range, for a napping pet. False if there's none.</summary>
        public static bool TryFindNapSeat(Vector3 near, float range, out SeatingTable table, out int seat)
        {
            table = null;
            seat = -1;
            float best = range * range;
            foreach (SeatingTable candidate in active)
            {
                for (int i = 0; i < candidate.seats.Length; i++)
                {
                    if (candidate.occupants[i] != null || candidate.seats[i] == null) continue;
                    float distance = (candidate.seats[i].position - near).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    table = candidate;
                    seat = i;
                }
            }
            return table != null;
        }

        /// <summary>The customer got up. Pays the bonus and leaves a mess if they actually sat down.</summary>
        public void Leave(int seat, bool sat)
        {
            if (seat < 0 || seat >= seats.Length) return;
            occupants[seat] = null;
            if (!sat) return;

            if (cashPile != null) cashPile.AddValue(Mathf.RoundToInt(bonusPerVisit * (1f + BubbleTea.Extras.TeamSkills.TableCashBonus)));
            if (seat < messes.Length && messes[seat] != null) messes[seat].SetActive(true);
            GameEvents.RaiseMessChanged(MessCount);
        }

        private bool IsMessy(int seat) => seat < messes.Length && messes[seat] != null && messes[seat].activeSelf;

        /// <summary>Clears one empty seat's mess, at most a few times a second. Used by the cleaner.</summary>
        public bool TryCleanOne()
        {
            if (Time.time < cleanTimer) return false;
            cleanTimer = Time.time + 0.4f / BubbleTea.Extras.TeamSkills.CleanSpeed;
            return Clean(null);
        }

        // Anyone standing at the table clears one empty seat's mess at a time.
        private bool Clean(ItemCarrier carrier)
        {
            for (int i = 0; i < seats.Length; i++)
            {
                if (!IsMessy(i) || occupants[i] != null) continue;
                messes[i].SetActive(false);
                GameEvents.RaiseTableCleaned(messes[i].transform.position);
                GameEvents.RaiseMessChanged(MessCount);
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected() => zone.DrawGizmo(transform);
    }
}
