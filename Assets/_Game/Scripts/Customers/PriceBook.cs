using System;
using System.Collections.Generic;
using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// The prices the player sets for this shop's drinks, as a share of each drink's normal
    /// price (1 = normal, 1.5 = half as much again). Set from the price panel that opens when
    /// you tap a counter; saved with the shop.
    /// </summary>
    public static class PriceBook
    {
        public const float Min = 0.5f;
        public const float Max = 3f;
        public const float Step = 0.1f;

        private static readonly Dictionary<string, float> factors = new();

        /// <summary>Raised when a price changes.</summary>
        public static event Action Changed;

        /// <summary>This drink's price as a share of its normal price.</summary>
        public static float Factor(ItemDefinition drink) =>
            drink != null && factors.TryGetValue(drink.name, out float factor) ? factor : 1f;

        public static void SetFactor(ItemDefinition drink, float factor)
        {
            if (drink == null) return;
            factors[drink.name] = Mathf.Round(Mathf.Clamp(factor, Min, Max) * 10f) / 10f;
            Changed?.Invoke();
        }

        /// <summary>Every price that isn't normal, for the save file.</summary>
        public static List<PriceSave> Snapshot()
        {
            var list = new List<PriceSave>();
            foreach (KeyValuePair<string, float> entry in factors)
            {
                if (!Mathf.Approximately(entry.Value, 1f)) list.Add(new PriceSave { drink = entry.Key, factor = entry.Value });
            }
            return list;
        }

        public static void Restore(List<PriceSave> saved)
        {
            factors.Clear();
            if (saved != null)
            {
                foreach (PriceSave entry in saved)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.drink)) factors[entry.drink] = Mathf.Clamp(entry.factor, Min, Max);
                }
            }
            Changed?.Invoke();
        }

        /// <summary>Back to normal prices (each shop loads its own).</summary>
        public static void Clear() => factors.Clear();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            factors.Clear();
            Changed = null;
        }
    }

    /// <summary>One drink's price in the save file.</summary>
    [Serializable]
    public class PriceSave
    {
        public string drink;
        public float factor;
    }
}
