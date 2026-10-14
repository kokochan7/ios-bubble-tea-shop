using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// The floor money that gets a new shop going: tops up its pile every few seconds until it
    /// has handed out a set amount in total (for example $500), then stops for good. The
    /// amount already handed out is saved, so it doesn't start over next time.
    /// </summary>
    public class CashPileRefiller : MonoBehaviour
    {
        [SerializeField, Tooltip("Piles to keep topped up.")]
        private CashPile[] piles;

        [SerializeField, Tooltip("Seconds between refills.")]
        private float interval = 4f;

        [SerializeField, Tooltip("Bills added to each pile per refill.")]
        private int billsPerRefill = 6;

        [SerializeField, Min(0), Tooltip("Total money the floor hands out before it stops. 0 = never stops.")]
        private int totalLimit = 500;

        private float timer;
        private int placed; // money put on the floor so far, collected or not

        /// <summary>
        /// Money the player has picked up from the floor so far (saved with the game). Bills still
        /// lying there don't count, so quitting before collecting them loses nothing.
        /// </summary>
        public int Given
        {
            get
            {
                int lying = 0;
                foreach (CashPile pile in piles) if (pile != null) lying += pile.Count * pile.ValuePerBill;
                return Mathf.Max(0, placed - lying);
            }
        }

        /// <summary>Called by the save manager when a game loads.</summary>
        public void Restore(int given) => placed = Mathf.Max(0, given);

        // Starts at the full interval, so the first bills appear right away (after the save loads in Start).
        private void Awake() => timer = interval;

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;

            foreach (CashPile pile in piles)
            {
                if (pile == null) continue;
                int bills = billsPerRefill;
                if (totalLimit > 0)
                {
                    int left = totalLimit - placed;
                    bills = Mathf.Min(bills, Mathf.CeilToInt((float)left / Mathf.Max(pile.ValuePerBill, 1)));
                }
                if (bills <= 0) continue;
                int before = pile.Count;
                pile.AddBills(bills);
                placed += (pile.Count - before) * pile.ValuePerBill;
            }
        }
    }
}
