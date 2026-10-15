using BubbleTea.Economy;
using BubbleTea.Goals;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// Pays the hired workers. At the end of every shop hour each barista, cashier and cleaner
    /// takes their hourly wage out of the player's money (never below $0). The Stats panel shows
    /// what each kind of worker charges and how much has been paid in total.
    /// </summary>
    public class Payroll : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("Dollars each barista is paid per shop hour.")]
        private int baristaWage = 3;

        [SerializeField, Min(0), Tooltip("Dollars each cashier is paid per shop hour.")]
        private int cashierWage = 2;

        [SerializeField, Min(0), Tooltip("Dollars each cleaner is paid per shop hour.")]
        private int cleanerWage = 2;

        [SerializeField, Min(1f), Tooltip("Real seconds between paydays when the shop has no clock.")]
        private float secondsPerHourWithoutClock = 6f;

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

        private void PayWages()
        {
            CountStaff();
            if (wallet == null) return;
            int owed = Mathf.Min(WagesPerHour, wallet.Money);
            if (owed <= 0 || !wallet.TrySpend(owed)) return;
            if (ShopRecords.Instance != null) ShopRecords.Instance.AddWagesPaid(owed);
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
        private static void ResetOnPlay() => Instance = null;
    }
}
