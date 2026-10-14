using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Lets the player carry a stack of items. Stations and the counter look for carriers
    /// standing in their zone and hand items over automatically.
    /// </summary>
    public class ItemCarrier : MonoBehaviour
    {
        private static readonly List<ItemCarrier> active = new();
        public static IReadOnlyList<ItemCarrier> All => active;

        [SerializeField, Tooltip("The stack in the player's hands.")]
        private ItemStack stack = new();

        public ItemStack Stack => stack;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);
    }
}
