using System;
using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Moments worth celebrating with a sound, a particle burst or a vibration. Gameplay
    /// scripts raise them; the Feedback module listens. Gameplay never needs to know what
    /// the feedback looks or sounds like.
    /// </summary>
    public static class GameEvents
    {
        /// <summary>A bill reached the player (position of the player).</summary>
        public static event Action<Vector3> CashCollected;

        /// <summary>A bill left the player for an unlock pad.</summary>
        public static event Action<Vector3> PadPaid;

        /// <summary>A pad was paid off and its station appeared (not raised when loading a save).</summary>
        public static event Action<Vector3> Unlocked;

        /// <summary>A customer bought a drink, with where and for how much.</summary>
        public static event Action<Vector3, int> DrinkSold;

        /// <summary>A customer got their drink, with their mood (1 = served right away, 0 = about to leave).</summary>
        public static event Action<Vector3, float> CustomerServed;

        /// <summary>A customer bought a drink: the drink's item asset name.</summary>
        public static event Action<string> DrinkServed;

        /// <summary>A happy customer added a tip (included in the sale), with where and how much.</summary>
        public static event Action<Vector3, int> Tipped;

        /// <summary>A customer ran out of patience and left without buying.</summary>
        public static event Action<Vector3> CustomerLeftUpset;

        /// <summary>A customer thought their drink cost too much and left without buying.</summary>
        public static event Action<Vector3> CustomerRefusedPrice;

        /// <summary>The number of messy café seats changed.</summary>
        public static event Action<int> MessChanged;

        /// <summary>Someone cleaned a table (position of the cleared cup).</summary>
        public static event Action<Vector3> TableCleaned;

        /// <summary>An upgrade level was bought.</summary>
        public static event Action UpgradeBought;

        /// <summary>The player collected a bonus, such as money earned while away.</summary>
        public static event Action BonusCollected;

        public static void RaiseCashCollected(Vector3 position) => CashCollected?.Invoke(position);
        public static void RaisePadPaid(Vector3 position) => PadPaid?.Invoke(position);
        public static void RaiseUnlocked(Vector3 position) => Unlocked?.Invoke(position);
        public static void RaiseDrinkSold(Vector3 position, int amount) => DrinkSold?.Invoke(position, amount);
        public static void RaiseCustomerServed(Vector3 position, float mood) => CustomerServed?.Invoke(position, mood);
        public static void RaiseDrinkServed(string itemId) => DrinkServed?.Invoke(itemId);
        public static void RaiseTipped(Vector3 position, int amount) => Tipped?.Invoke(position, amount);
        public static void RaiseCustomerLeftUpset(Vector3 position) => CustomerLeftUpset?.Invoke(position);
        public static void RaiseCustomerRefusedPrice(Vector3 position) => CustomerRefusedPrice?.Invoke(position);
        public static void RaiseMessChanged(int messes) => MessChanged?.Invoke(messes);
        public static void RaiseTableCleaned(Vector3 position) => TableCleaned?.Invoke(position);
        public static void RaiseUpgradeBought() => UpgradeBought?.Invoke();
        public static void RaiseBonusCollected() => BonusCollected?.Invoke();

        // Domain reload is off, so clear listeners left over from the last Play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            CashCollected = null;
            PadPaid = null;
            Unlocked = null;
            DrinkSold = null;
            Tipped = null;
            DrinkServed = null;
            CustomerServed = null;
            MessChanged = null;
            TableCleaned = null;
            CustomerLeftUpset = null;
            CustomerRefusedPrice = null;
            UpgradeBought = null;
            BonusCollected = null;
        }
    }
}
