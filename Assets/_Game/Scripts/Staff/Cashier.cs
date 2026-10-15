using BubbleTea.Core;
using BubbleTea.Customers;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired cashier who walks to their spot behind the counter, faces the customers and
    /// collects what they pay. Bills fly from the counter's cash pile to the cashier and go
    /// straight into the player's wallet, so the player no longer has to walk over to pick
    /// them up. The Walk Speed upgrade makes them walk and collect faster.
    /// </summary>
    public class Cashier : MonoBehaviour
    {
        [SerializeField, Tooltip("Which counter to work at (its lane, such as Main or DriveThru).")]
        private string lane = "Main";

        [SerializeField, Tooltip("Seconds between bills before upgrades. Smaller is faster.")]
        private float secondsPerBill = 0.02f;

        [SerializeField, Tooltip("Where the bills fly to. Leave empty for the cashier's position.")]
        private Transform hands;

        [SerializeField, Tooltip("Where to stand, relative to the counter (+z is the player's side, behind the counter).")]
        private Vector3 spot = new(0.6f, 0f, 0.85f);

        [SerializeField, Tooltip("Walking speed in meters per second, before upgrades.")]
        private float speed = 3.5f;

        private ServingCounter counter;
        private Wallet wallet;
        private float cooldown;
        private bool atSpot;

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
            if (counter == null)
            {
                counter = ServingCounter.FindLane(lane);
                atSpot = false;
                if (counter == null) return;
            }
            if (!atSpot)
            {
                WalkToSpot();
                return;
            }
            if (wallet == null || counter.CashPile == null) return;

            cooldown -= Time.deltaTime * Stats.Get(StatId.MoveSpeed, 1f);
            if (cooldown > 0f) return;
            if (counter.CashPile.TrySendBill(hands != null ? hands : transform, wallet)) cooldown = secondsPerBill;
        }

        // Walks behind the counter, then turns to face the customers across it.
        private void WalkToSpot()
        {
            Vector3 target = counter.transform.TransformPoint(spot);
            target.y = transform.position.y;
            Vector3 offset = target - transform.position;
            if (offset.sqrMagnitude < 0.04f) // within 20 cm: settle in and stay put
            {
                transform.position = target;
                transform.rotation = Quaternion.LookRotation(-counter.transform.forward);
                atSpot = true;
                return;
            }
            float pace = speed * Stats.Get(StatId.MoveSpeed, 1f);
            StaffSteering.Step(transform, target, pace * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(offset), 720f * Time.deltaTime);
        }
    }
}
