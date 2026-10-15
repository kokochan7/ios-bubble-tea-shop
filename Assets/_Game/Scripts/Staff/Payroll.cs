using System;
using System.Collections.Generic;
using BubbleTea.Economy;
using BubbleTea.Goals;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// Pays the hired workers. At the end of every shop hour each barista, cashier and cleaner
    /// takes their hourly wage out of the player's money. A worker who can't be paid (not enough
    /// money) stops and stands still with a red light over their head (see <see cref="PayStatus"/>)
    /// until there's money to pay them, then gets back to work. The Stats panel shows what each
    /// kind of worker charges and how much has been paid in total.
    /// </summary>
    public class Payroll : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("Dollars each barista is paid per shop hour.")]
        private int baristaWage = 9;

        [SerializeField, Min(0), Tooltip("Dollars each cashier is paid per shop hour.")]
        private int cashierWage = 6;

        [SerializeField, Min(0), Tooltip("Dollars each cleaner is paid per shop hour.")]
        private int cleanerWage = 6;

        [SerializeField, Min(1f), Tooltip("Real seconds between paydays when the shop has no clock.")]
        private float secondsPerHourWithoutClock = 6f;

        /// <summary>Raised when workers stop because they weren't paid, with a description like "A barista".</summary>
        public static event Action<string> WorkersUnpaid;

        /// <summary>The payroll of the shop that's open, if any.</summary>
        public static Payroll Instance { get; private set; }

        public int BaristaWage => baristaWage;
        public int CashierWage => cashierWage;
        public int CleanerWage => cleanerWage;

        public int Baristas { get; private set; }
        public int Cashiers { get; private set; }
        public int Cleaners { get; private set; }

        /// <summary>Wages for everyone hired right now, per shop hour.</summary>
        public int WagesPerHour => Baristas * baristaWage + Cashiers * cashierWage + Cleaners * cleanerWage;

        private Wallet wallet;
        private int lastHour = -1;
        private float timer;
        private float countTimer;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            wallet = FindFirstObjectByType<Wallet>();
#else
            wallet = FindObjectOfType<Wallet>();
#endif
            CountStaff();
        }

        private void Update()
        {
            countTimer -= Time.deltaTime;
            if (countTimer <= 0f)
            {
                countTimer = 1f;
                CountStaff();
                PayWhatsOwed();
            }

            if (Core.GameClock.Instance != null)
            {
                int hour = Mathf.FloorToInt(Core.GameClock.Instance.MinuteOfDay / 60f);
                if (lastHour < 0) lastHour = hour;   // don't charge for the hour the game opened in
                if (hour == lastHour) return;
                lastHour = hour;
            }
            else
            {
                timer += Time.deltaTime;
                if (timer < secondsPerHourWithoutClock) return;
                timer = 0f;
            }
            PayWages();
        }

        // Pays everyone in turn (baristas, then cashiers, then cleaners). Anyone who can't be paid stops.
        private void PayWages()
        {
            if (wallet == null) return;
            var unpaid = new List<MonoBehaviour>();
            int paidOut = 0;
            Pay(FindAll<Barista>(), baristaWage, unpaid, ref paidOut);
            Pay(FindAll<Cashier>(), cashierWage, unpaid, ref paidOut);
            Pay(FindAll<Cleaner>(), cleanerWage, unpaid, ref paidOut);
            Record(paidOut);
            if (unpaid.Count > 0) WorkersUnpaid?.Invoke(Describe(unpaid));
        }

        private void Pay<T>(T[] workers, int wage, List<MonoBehaviour> unpaid, ref int paidOut) where T : MonoBehaviour
        {
            foreach (T worker in workers)
            {
                PayStatus status = worker.GetComponent<PayStatus>();
                if (status != null && status.Unpaid) continue; // still owed from before; paid by PayWhatsOwed
                if (wallet.TrySpend(wage)) paidOut += wage;
                else
                {
                    unpaid.Add(worker);
                    if (status != null) status.SetUnpaid(true);
                }
            }
        }

        // Every second: workers who are owed their wage get paid as soon as there's enough money.
        private void PayWhatsOwed()
        {
            if (wallet == null) return;
            int paidOut = 0;
            foreach (PayStatus status in FindAll<PayStatus>())
            {
                if (!status.Unpaid) continue;
                int wage = WageOf(status);
                if (!wallet.TrySpend(wage)) continue;
                paidOut += wage;
                status.SetUnpaid(false);
            }
            Record(paidOut);
        }

        private int WageOf(PayStatus status)
        {
            if (status.GetComponent<Barista>() != null) return baristaWage;
            if (status.GetComponent<Cashier>() != null) return cashierWage;
            return cleanerWage;
        }

        private static void Record(int paidOut)
        {
            if (paidOut > 0 && ShopRecords.Instance != null) ShopRecords.Instance.AddWagesPaid(paidOut);
        }

        private static string Describe(List<MonoBehaviour> unpaid)
        {
            if (unpaid.Count > 1) return $"{unpaid.Count} workers";
            return unpaid[0] switch
            {
                Barista => "A barista",
                Cashier => "A cashier",
                _ => "A cleaner",
            };
        }

        private static T[] FindAll<T>() where T : MonoBehaviour
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<T>();
#endif
        }

        private void CountStaff()
        {
#if UNITY_2023_1_OR_NEWER
            Baristas = FindObjectsByType<Barista>(FindObjectsSortMode.None).Length;
            Cashiers = FindObjectsByType<Cashier>(FindObjectsSortMode.None).Length;
            Cleaners = FindObjectsByType<Cleaner>(FindObjectsSortMode.None).Length;
#else
            Baristas = FindObjectsOfType<Barista>().Length;
            Cashiers = FindObjectsOfType<Cashier>().Length;
            Cleaners = FindObjectsOfType<Cleaner>().Length;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Instance = null;
            WorkersUnpaid = null;
        }
    }
}
