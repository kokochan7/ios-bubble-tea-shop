using BubbleTea.Core;
using BubbleTea.Customers;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired cleaner who looks after the seating area: walks to the nearest table with an
    /// empty cup on it and clears it, and when the tables are clean, collects the money and
    /// tips seated customers left by the tables straight into the player's wallet. The Walk
    /// Speed upgrade makes them faster.
    /// </summary>
    public class Cleaner : MonoBehaviour
    {
        [SerializeField, Tooltip("Walking speed in meters per second, before upgrades.")]
        private float speed = 3.5f;

        [SerializeField, Tooltip("Seconds between bills when collecting.")]
        private float secondsPerBill = 0.03f;

        [SerializeField, Tooltip("How close to a cash pile they must be to collect it, in meters.")]
        private float collectRadius = 1.2f;

        [SerializeField, Tooltip("Where collected bills fly to. Leave empty for the cleaner's position.")]
        private Transform hands;

        private Wallet wallet;
        private float cooldown;

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            wallet = FindFirstObjectByType<Wallet>();
#else
            wallet = FindObjectOfType<Wallet>();
#endif
        }

        private void Update()
        {
            SeatingTable messy = NearestMessyTable();
            if (messy != null)
            {
                // Stand in the aisle beside the table, not inside it.
                if (WalkTo(messy.ZoneCenter + new Vector3(0f, 0f, -1f))) messy.TryCleanOne();
                return;
            }

            CashPile pile = FullestPile();
            if (pile == null) return;
            Vector3 spot = pile.transform.position;
            Vector3 offset = spot - transform.position;
            offset.y = 0f;
            if (offset.magnitude > collectRadius)
            {
                WalkTo(spot);
                return;
            }

            cooldown -= Time.deltaTime * Stats.Get(StatId.MoveSpeed, 1f) * PayStatus.SpeedOf(this); // slower while their pay is late
            if (cooldown > 0f || wallet == null) return;
            if (pile.TrySendBill(hands != null ? hands : transform, wallet)) cooldown = secondsPerBill;
        }

        private SeatingTable NearestMessyTable()
        {
            SeatingTable best = null;
            float bestDistance = float.MaxValue;
            foreach (SeatingTable table in SeatingTable.All)
            {
                if (table == null || !table.NeedsCleaning) continue;
                float distance = (table.ZoneCenter - transform.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = table;
                }
            }
            return best;
        }

        // The seating area's money pile with the most bills in it (tables share their area's pile).
        private static CashPile FullestPile()
        {
            CashPile best = null;
            foreach (SeatingTable table in SeatingTable.All)
            {
                CashPile pile = table != null ? table.CashPile : null;
                if (pile == null || pile.Count == 0) continue;
                if (best == null || pile.Count > best.Count) best = pile;
            }
            return best;
        }

        // Moves toward the target on the floor. Returns true once there.
        private bool WalkTo(Vector3 target)
        {
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.09f) return true;

            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f) * PayStatus.SpeedOf(this);
            StaffSteering.Step(transform, target, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
            return false;
        }
    }
}
