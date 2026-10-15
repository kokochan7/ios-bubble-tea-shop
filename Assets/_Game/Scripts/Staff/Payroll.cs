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
    /// money) keeps working, slower, with a yellow light over their head; after 6 unpaid hours
    /// they stop with a red light (see <see cref="PayStatus"/>). Once their back pay is paid
    /// they're back to full speed. The Stats panel shows what each
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

        /// <summary>Raised with a message for the player when workers go unpaid or stop.</summary>
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

        // Pays everyone in turn (baristas, then cashiers, then cleaners). Anyone who can't be paid
        // (or is already owed) owes one more hour: yellow and slower, red and stopped after the grace hours.
        private void PayWages()
        {
            if (wallet == null) return;
            var late = new List<MonoBehaviour>();
            var stopped = new List<MonoBehaviour>();
            int paidOut = 0;
            Pay(FindAll<Barista>(), baristaWage, late, stopped, ref paidOut);
            Pay(FindAll<Cashier>(), cashierWage, late, stopped, ref paidOut);
            Pay(FindAll<Cleaner>(), cleanerWage, late, stopped, ref paidOut);
            Record(paidOut);
            if (stopped.Count > 0)
                WorkersUnpaid?.Invoke($"{Describe(stopped)} stopped working: unpaid for {PayStatus.GraceHours} hours!\nThey'll start again once you can pay them.");
            else if (late.Count > 0)
                WorkersUnpaid?.Invoke($"{Describe(late)} didn't get paid: working slower!\nAfter {PayStatus.GraceHours} unpaid hours they stop.");
        }

        private void Pay<T>(T[] workers, int wage, List<MonoBehaviour> late, List<MonoBehaviour> stopped, ref int paidOut)
            where T : MonoBehaviour
        {
            foreach (T worker in workers)
            {
                PayStatus status = worker.GetComponent<PayStatus>();
                bool behind = status != null && status.Unpaid; // back pay is settled by PayWhatsOwed
                if (!behind && wallet.TrySpend(wage))
                {
                    paidOut += wage;
                    continue;
                }
                if (status == null) continue;
                bool wasWorking = !status.Stopped;
                status.AddHourOwed();
                if (status.Stopped && wasWorking) stopped.Add(worker);
                else if (status.HoursOwed == 1) late.Add(worker);
            }
        }

        // Every second: workers who are owed get all their back pay as soon as there's enough money.
        private void PayWhatsOwed()
        {
            if (wallet == null) return;
            int paidOut = 0;
            foreach (PayStatus status in FindAll<PayStatus>())
            {
                if (!status.Unpaid) continue;
                int owed = WageOf(status) * status.HoursOwed;
                if (!wallet.TrySpend(owed)) continue;
                paidOut += owed;
                status.PaidUp();
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
