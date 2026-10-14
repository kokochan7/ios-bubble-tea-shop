using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// A stack of bills on the floor. Bills are created once and reused, so collecting
    /// and refilling never allocates new objects during play.
    /// </summary>
    public class CashPile : MonoBehaviour
    {
        // Every pile that is currently enabled, so the player can find nearby piles
        // without physics layers or searching the scene.
        private static readonly List<CashPile> active = new();
        public static IReadOnlyList<CashPile> All => active;

        [SerializeField, Tooltip("The bill prefab (needs a CashBill component).")]
        private CashBill billPrefab;

        [SerializeField, Tooltip("Money each bill is worth.")]
        private int valuePerBill = 5;

        [SerializeField, Tooltip("Bills in the pile when the game starts.")]
        private int startingBills = 12;

        [SerializeField, Tooltip("Most bills this pile can hold.")]
        private int maxBills = 40;

        [SerializeField, Tooltip("Bills per layer across (x) and deep (z).")]
        private Vector2Int layout = new(2, 3);

        [SerializeField, Tooltip("Gap between bills across (x), up (y) and deep (z).")]
        private Vector3 spacing = new(0.48f, 0.06f, 0.28f);

        private readonly List<CashBill> stacked = new();
        private readonly Stack<CashBill> spare = new();
        private int created;

        public int Count => stacked.Count;
        public int ValuePerBill => valuePerBill;

        private void Awake()
        {
            AddBills(startingBills);
        }

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        /// <summary>Adds up to <paramref name="amount"/> bills on top of the pile.</summary>
        public void AddBills(int amount)
        {
            for (int i = 0; i < amount && stacked.Count < maxBills; i++)
            {
                CashBill bill = GetFreeBill();
                if (bill == null) return;
                bill.PlaceIn(transform, SlotPosition(stacked.Count));
                bill.gameObject.SetActive(true);
                stacked.Add(bill);
            }
        }

        /// <summary>Adds enough bills to be worth at least <paramref name="amount"/>.</summary>
        public void AddValue(int amount)
        {
            if (amount <= 0) return;
            AddBills(Mathf.CeilToInt((float)amount / Mathf.Max(valuePerBill, 1)));
        }

        /// <summary>
        /// Sends the top bill flying to <paramref name="flyTarget"/> and pays it into
        /// <paramref name="wallet"/> when it lands. Returns false if the pile is empty.
        /// </summary>
        public bool TrySendBill(Transform flyTarget, Wallet wallet)
        {
            if (stacked.Count == 0) return false;

            CashBill bill = stacked[stacked.Count - 1];
            stacked.RemoveAt(stacked.Count - 1);
            bill.FlyTo(flyTarget, landed =>
            {
                if (wallet != null) wallet.Add(valuePerBill);
                GameEvents.RaiseCashCollected(flyTarget != null ? flyTarget.position : transform.position);
                landed.gameObject.SetActive(false);
                landed.transform.SetParent(transform, false);
                spare.Push(landed);
            });
            return true;
        }

        private CashBill GetFreeBill()
        {
            if (spare.Count > 0) return spare.Pop();
            if (billPrefab == null || created >= maxBills) return null;
            created++;
            return Instantiate(billPrefab, transform);
        }

        private Vector3 SlotPosition(int index)
        {
            int perLayer = Mathf.Max(1, layout.x * layout.y);
            int layer = index / perLayer;
            int slot = index % perLayer;
            int column = slot % Mathf.Max(1, layout.x);
            int row = slot / Mathf.Max(1, layout.x);

            float x = (column - (layout.x - 1) * 0.5f) * spacing.x;
            float z = (row - (layout.y - 1) * 0.5f) * spacing.z;
            float y = (layer + 0.5f) * spacing.y;
            return new Vector3(x, y, z);
        }
    }
}
