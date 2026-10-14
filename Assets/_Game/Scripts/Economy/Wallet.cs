using System;
using UnityEngine;

namespace BubbleTea.Economy
{
    /// <summary>
    /// Holds the player's money. Other scripts listen to <see cref="MoneyChanged"/>
    /// instead of checking the amount every frame.
    /// </summary>
    public class Wallet : MonoBehaviour
    {
        [SerializeField, Tooltip("Money the player starts with.")]
        private int money;

        public int Money => money;

        /// <summary>Raised with the new total whenever money is added or spent.</summary>
        public event Action<int> MoneyChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            money += amount;
            MoneyChanged?.Invoke(money);
        }

        /// <summary>Spends the amount if the player can afford it. Returns false (and spends nothing) otherwise.</summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (money < amount) return false;
            money -= amount;
            MoneyChanged?.Invoke(money);
            return true;
        }
    }
}
