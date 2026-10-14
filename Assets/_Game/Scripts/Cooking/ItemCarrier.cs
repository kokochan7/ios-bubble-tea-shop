using System;
using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Lets the player carry a stack of items. Stations and the counter look for carriers
    /// standing in their zone and hand items over automatically. Each level of the Carry More
    /// upgrade adds room for one more item.
    /// </summary>
    public class ItemCarrier : MonoBehaviour
    {
        private static readonly List<ItemCarrier> active = new();
        public static IReadOnlyList<ItemCarrier> All => active;

        [SerializeField, Tooltip("The stack in the player's hands.")]
        private ItemStack stack = new();

        [SerializeField, Tooltip("Whether the Carry More upgrade adds room. On for the player, off for staff.")]
        private bool usesCarryUpgrade = true;

        [SerializeField, Tooltip("Optional tray shown under the items while carrying something.")]
        private GameObject tray;

        public ItemStack Stack => stack;

        /// <summary>
        /// When set, this carrier only hands items to this station or counter. Staff use it
        /// so they don't drop things off at every station they walk past. Empty means anyone.
        /// </summary>
        public UnityEngine.Object GiveOnlyTo { get; set; }

        /// <summary>When set, the carrier only picks up items that match. Empty means anything.</summary>
        public Func<ItemDefinition, bool> TakeFilter { get; set; }

        public bool WillGiveTo(UnityEngine.Object receiver) => GiveOnlyTo == null || GiveOnlyTo == receiver;
        public bool WillTake(ItemDefinition item) => TakeFilter == null || TakeFilter(item);

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

        private void LateUpdate()
        {
            if (tray != null && tray.activeSelf != stack.Count > 0) tray.SetActive(stack.Count > 0);
        }

        private void OnStatChanged(StatId stat)
        {
            if (stat == StatId.CarryBonus) ApplyCarryBonus();
        }

        private void ApplyCarryBonus()
        {
            stack.ExtraCapacity = usesCarryUpgrade ? BubbleTea.Upgrades.UpgradeManager.LevelOfStat(StatId.CarryBonus) : 0;
        }
    }
}
