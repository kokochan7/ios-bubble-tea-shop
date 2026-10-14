using System;
using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// A square on the floor. Stand on it and your money drains into it, bill by bill.
    /// Once the full price is paid, the station from its <see cref="BuildableDefinition"/>
    /// pops into place and the next pads in the chain appear. Partial payments are kept,
    /// so you can walk off and come back later.
    /// </summary>
    public class UnlockPad : MonoBehaviour
    {
        [SerializeField, Tooltip("What this pad builds and how much it costs.")]
        private BuildableDefinition definition;

        [SerializeField, Tooltip("The player's wallet. The pad drains it while the player stands here.")]
        private Wallet wallet;

        [SerializeField, Tooltip("Bill prefab used for the flying-money effect.")]
        private CashBill billPrefab;

        [SerializeField, Tooltip("Pads that appear once this one is unlocked.")]
        private UnlockPad[] unlocksNext;

        [SerializeField, Tooltip("Half the pad's width in meters. The player counts as 'on' the pad inside this square.")]
        private float halfSize = 1.2f;

        [SerializeField, Tooltip("Seconds the player must stand still on the pad before money starts flowing.")]
        private float startDelay = 0.3f;

        [SerializeField, Tooltip("Seconds it takes to pay the full price when you have enough money.")]
        private float secondsToFill = 1.5f;

        [SerializeField, Tooltip("Payments per second while draining. Each one sends a bill flying.")]
        private float paymentsPerSecond = 20f;

        private readonly Stack<CashBill> spareBills = new();
        private int paid;
        private int billsInFlight;
        private float standTimer;
        private float paymentTimer;
        private bool completed;

        /// <summary>Raised with (remaining, cost) whenever a payment lands.</summary>
        public event Action<int, int> ProgressChanged;

        /// <summary>Raised once, when the station appears.</summary>
        public event Action<UnlockPad> Unlocked;

        public BuildableDefinition Definition => definition;
        public int Cost => definition != null ? definition.Cost : 0;
        public int Remaining => Mathf.Max(0, Cost - paid);

        private void Update()
        {
            if (completed || definition == null || wallet == null) return;

            if (!IsPlayerOnPad())
            {
                standTimer = 0f;
                return;
            }

            standTimer += Time.deltaTime;
            if (standTimer < startDelay) return;

            paymentTimer -= Time.deltaTime;
            while (paymentTimer <= 0f && Remaining > 0 && wallet.Money > 0)
            {
                paymentTimer += 1f / Mathf.Max(paymentsPerSecond, 1f);
                Pay();
            }
            if (paymentTimer < 0f) paymentTimer = 0f;
        }

        private void Pay()
        {
            int perPayment = Mathf.CeilToInt(Cost / Mathf.Max(secondsToFill * paymentsPerSecond, 1f));
            int amount = Mathf.Min(perPayment, Remaining, wallet.Money);
            if (amount <= 0 || !wallet.TrySpend(amount)) return;

            paid += amount;
            ProgressChanged?.Invoke(Remaining, Cost);
            SendBill();
            TryComplete();
        }

        private void SendBill()
        {
            if (billPrefab == null) return;

            CashBill bill = spareBills.Count > 0 ? spareBills.Pop() : Instantiate(billPrefab);
            bill.PlaceIn(null, wallet.transform.position + Vector3.up);
            bill.gameObject.SetActive(true);
            billsInFlight++;
            bill.FlyTo(transform, landed =>
            {
                landed.gameObject.SetActive(false);
                landed.transform.SetParent(transform, false);
                spareBills.Push(landed);
                billsInFlight--;
                TryComplete();
            });
        }

        // Waits for the last bill to land so the station appears as the money arrives.
        private void TryComplete()
        {
            if (completed || Remaining > 0 || billsInFlight > 0) return;
            completed = true;

            if (definition.Prefab != null)
            {
                GameObject station = Instantiate(definition.Prefab, transform.position, transform.rotation);
                station.name = definition.DisplayName;
                station.AddComponent<PopIn>();
            }

            foreach (UnlockPad next in unlocksNext)
            {
                if (next != null) next.gameObject.SetActive(true);
            }

            Unlocked?.Invoke(this);
            gameObject.SetActive(false);
        }

        private bool IsPlayerOnPad()
        {
            Vector3 offset = wallet.transform.position - transform.position;
            return Mathf.Abs(offset.x) <= halfSize && Mathf.Abs(offset.z) <= halfSize;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            Gizmos.DrawWireCube(transform.position, new Vector3(halfSize * 2f, 0.05f, halfSize * 2f));
        }
    }
}
