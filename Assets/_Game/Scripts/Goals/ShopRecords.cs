using System;
using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Goals
{
    /// <summary>How many of one drink were sold. Part of <see cref="ShopRecordsSave"/>.</summary>
    [Serializable]
    public class DrinkCount
    {
        public string item;
        public int count;
    }

    /// <summary>The shop's lifetime records as written to the save file.</summary>
    [Serializable]
    public class ShopRecordsSave
    {
        public int moneyEarned;
        public int drinksSold;
        public int customersServed;
        public int tips;
        public int tipMoney;
        public int walkouts;
        public int tooPricey;
        public int upgradesBought;
        public int tablesCleaned;
        public float secondsPlayed;
        public List<DrinkCount> perDrink = new();
    }

    /// <summary>
    /// Keeps lifetime totals for this shop (money made, drinks sold of each kind, customers,
    /// tips...). The Stats panel shows them and the goals are milestones on them. Saved with the shop.
    /// </summary>
    public class ShopRecords : MonoBehaviour
    {
        /// <summary>The records of the shop that's open, if any.</summary>
        public static ShopRecords Instance { get; private set; }

        /// <summary>Raised whenever a total changes (not for play time).</summary>
        public static event Action Changed;

        private ShopRecordsSave data = new();

        public int MoneyEarned => data.moneyEarned;
        public int DrinksSold => data.drinksSold;
        public int CustomersServed => data.customersServed;
        public int Tips => data.tips;
        public int TipMoney => data.tipMoney;
        public int Walkouts => data.walkouts;
        public int TooPricey => data.tooPricey;
        public int UpgradesBought => data.upgradesBought;
        public int TablesCleaned => data.tablesCleaned;
        public float SecondsPlayed => data.secondsPlayed;

        /// <summary>How many of this drink (by its item asset name) have been sold.</summary>
        public int SoldOf(string item)
        {
            DrinkCount entry = data.perDrink.Find(d => d.item == item);
            return entry != null ? entry.count : 0;
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.DrinkSold += OnDrinkSold;
            GameEvents.DrinkServed += OnDrinkServed;
            GameEvents.CustomerServed += OnCustomerServed;
            GameEvents.Tipped += OnTipped;
            GameEvents.CustomerLeftUpset += OnWalkout;
            GameEvents.CustomerRefusedPrice += OnTooPricey;
            GameEvents.UpgradeBought += OnUpgradeBought;
            GameEvents.TableCleaned += OnTableCleaned;
        }

        private void OnDisable()
        {
            GameEvents.DrinkSold -= OnDrinkSold;
            GameEvents.DrinkServed -= OnDrinkServed;
            GameEvents.CustomerServed -= OnCustomerServed;
            GameEvents.Tipped -= OnTipped;
            GameEvents.CustomerLeftUpset -= OnWalkout;
            GameEvents.CustomerRefusedPrice -= OnTooPricey;
            GameEvents.UpgradeBought -= OnUpgradeBought;
            GameEvents.TableCleaned -= OnTableCleaned;
        }

        private void Update() => data.secondsPlayed += Time.unscaledDeltaTime;

        /// <summary>Puts back the saved records.</summary>
        public void Restore(ShopRecordsSave saved)
        {
            data = saved ?? new ShopRecordsSave();
            data.perDrink ??= new List<DrinkCount>();
            Changed?.Invoke();
        }

        /// <summary>A copy for the save file.</summary>
        public ShopRecordsSave Snapshot() => JsonUtility.FromJson<ShopRecordsSave>(JsonUtility.ToJson(data));

        private void OnDrinkSold(Vector3 _, int amount) => Bump(() => data.moneyEarned += amount);

        private void OnDrinkServed(string item) => Bump(() =>
        {
            data.drinksSold++;
            DrinkCount entry = data.perDrink.Find(d => d.item == item);
            if (entry == null) data.perDrink.Add(entry = new DrinkCount { item = item });
            entry.count++;
        });

        private void OnCustomerServed(Vector3 _, float mood) => Bump(() => data.customersServed++);

        private void OnTipped(Vector3 _, int amount) => Bump(() =>
        {
            data.tips++;
            data.tipMoney += amount;
        });

        private void OnWalkout(Vector3 _) => Bump(() => data.walkouts++);
        private void OnTooPricey(Vector3 _) => Bump(() => data.tooPricey++);
        private void OnUpgradeBought() => Bump(() => data.upgradesBought++);
        private void OnTableCleaned(Vector3 _) => Bump(() => data.tablesCleaned++);

        private static void Bump(Action change)
        {
            change();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Instance = null;
            Changed = null;
        }
    }
}
