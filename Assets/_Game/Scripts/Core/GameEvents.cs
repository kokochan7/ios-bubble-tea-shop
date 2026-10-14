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

        /// <summary>An upgrade level was bought.</summary>
        public static event Action UpgradeBought;

        public static void RaiseCashCollected(Vector3 position) => CashCollected?.Invoke(position);
        public static void RaisePadPaid(Vector3 position) => PadPaid?.Invoke(position);
        public static void RaiseUnlocked(Vector3 position) => Unlocked?.Invoke(position);
        public static void RaiseDrinkSold(Vector3 position, int amount) => DrinkSold?.Invoke(position, amount);
        public static void RaiseUpgradeBought() => UpgradeBought?.Invoke();

        // Domain reload is off, so clear listeners left over from the last Play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            CashCollected = null;
            PadPaid = null;
            Unlocked = null;
            DrinkSold = null;
            UpgradeBought = null;
        }
    }
}
