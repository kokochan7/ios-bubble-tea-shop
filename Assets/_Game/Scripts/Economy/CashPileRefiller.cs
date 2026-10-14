using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// Test helper for step 2: tops up cash piles every few seconds so there is always
    /// something to collect. Customers will drop cash instead once they exist.
    /// </summary>
    public class CashPileRefiller : MonoBehaviour
    {
        [SerializeField, Tooltip("Piles to keep topped up.")]
        private CashPile[] piles;

        [SerializeField, Tooltip("Seconds between refills.")]
        private float interval = 4f;

        [SerializeField, Tooltip("Bills added to each pile per refill.")]
        private int billsPerRefill = 6;

        private float timer;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;

            foreach (CashPile pile in piles)
            {
                if (pile != null) pile.AddBills(billsPerRefill);
            }
        }
    }
}
