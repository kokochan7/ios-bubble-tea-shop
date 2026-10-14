using BubbleTea.Core;
using BubbleTea.Customers;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A hired cashier who stands by the counter and collects what customers pay. Bills fly
    /// from the counter's cash pile to the cashier and go straight into the player's wallet,
    /// so the player no longer has to walk over to pick them up. The Walk Speed upgrade
    /// makes them collect faster.
    /// </summary>
    public class Cashier : MonoBehaviour
    {
        [SerializeField, Tooltip("Which counter to work at (its lane, such as Main or DriveThru).")]
        private string lane = "Main";

        [SerializeField, Tooltip("Seconds between bills before upgrades. Smaller is faster.")]
        private float secondsPerBill = 0.1f;

        [SerializeField, Tooltip("Where the bills fly to. Leave empty for the cashier's position.")]
        private Transform hands;

        private ServingCounter counter;
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
            if (counter == null)
            {
                counter = ServingCounter.FindLane(lane);
                if (counter == null) return;
                FaceCashPile();
            }
            if (wallet == null || counter.CashPile == null) return;

            cooldown -= Time.deltaTime * Stats.Get(StatId.MoveSpeed, 1f);
            if (cooldown > 0f) return;
            if (counter.CashPile.TrySendBill(hands != null ? hands : transform, wallet)) cooldown = secondsPerBill;
        }

        private void FaceCashPile()
        {
            if (counter.CashPile == null) return;
            Vector3 toPile = counter.CashPile.transform.position - transform.position;
            toPile.y = 0f;
            if (toPile.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toPile);
        }
    }
}
