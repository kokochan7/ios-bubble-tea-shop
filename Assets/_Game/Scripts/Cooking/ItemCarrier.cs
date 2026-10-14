using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Lets the player carry a stack of items. Stations and the counter look for carriers
    /// standing in their zone and hand items over automatically. The carry upgrade adds
    /// room to the stack.
    /// </summary>
    public class ItemCarrier : MonoBehaviour
    {
        private static readonly List<ItemCarrier> active = new();
        public static IReadOnlyList<ItemCarrier> All => active;

        [SerializeField, Tooltip("The stack in the player's hands.")]
        private ItemStack stack = new();

        public ItemStack Stack => stack;

        private void OnEnable()
        {
            active.Add(this);
            Stats.Changed += OnStatChanged;
            ApplyCarryBonus();
        }

        private void OnDisable()
        {
            active.Remove(this);
            Stats.Changed -= OnStatChanged;
        }

        private void OnStatChanged(StatId stat)
        {
            if (stat == StatId.CarryBonus) ApplyCarryBonus();
        }

        private void ApplyCarryBonus() => stack.ExtraCapacity = Mathf.RoundToInt(Stats.Get(StatId.CarryBonus, 0f));
    }
}
