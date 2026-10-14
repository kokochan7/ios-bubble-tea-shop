using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// Sits on the player. When the player stands near a <see cref="CashPile"/>, bills
    /// fly from the pile one after another and are paid into the wallet.
    /// </summary>
    public class CashCollector : MonoBehaviour
    {
        [SerializeField, Tooltip("The wallet that receives collected money.")]
        private Wallet wallet;

        [SerializeField, Tooltip("How close (in meters) the player must be to a pile's center.")]
        private float pickupRadius = 1.8f;

        [SerializeField, Tooltip("Delay between bills, in seconds. Smaller is faster.")]
        private float secondsPerBill = 0.04f;

        private float cooldown;

        private void Update()
        {
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            Vector3 here = transform.position;
            float radiusSqr = pickupRadius * pickupRadius;
            var piles = CashPile.All;

            for (int i = 0; i < piles.Count; i++)
            {
                CashPile pile = piles[i];
                if (pile.Count == 0) continue;

                Vector3 offset = pile.transform.position - here;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSqr) continue;

                if (pile.TrySendBill(transform, wallet))
                {
                    cooldown = secondsPerBill;
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
