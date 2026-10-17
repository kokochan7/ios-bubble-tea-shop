using System;
using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Lets the player carry a stack of items. Stations and the counter look for carriers
    /// standing in their zone and hand items over automatically. Each level of the Carry More
    /// upgrade adds room for one more item. Drinks left on the player's tray for a few seconds are
    /// tossed automatically, so there's no need to walk to the trash.
    /// </summary>
    public class ItemCarrier : MonoBehaviour, IHoldsItems
    {
        private static readonly List<ItemCarrier> active = new();
        public static IReadOnlyList<ItemCarrier> All => active;

        [SerializeField, Tooltip("The stack in the player's hands.")]
        private ItemStack stack = new();

        [SerializeField, Tooltip("Whether the Carry More upgrade adds room. On for the player, off for staff.")]
        private bool usesCarryUpgrade = true;

        [SerializeField, Tooltip("Optional tray shown under the items while carrying something.")]
        private GameObject tray;

        [SerializeField, Min(0f), Tooltip("Seconds a drink can sit on the player's tray before it's tossed (0 = never).")]
        private float tossDrinksAfter = 5f;

        private readonly Dictionary<CarriedItem, float> landedAt = new();
        private readonly List<CarriedItem> scratch = new();

        public ItemStack Stack => stack;

        /// <summary>True while carrying anything (the character's arms hold it up).</summary>
        public bool IsHolding => stack.Count > 0;

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
            if (usesCarryUpgrade && tossDrinksAfter > 0f) TossOldDrinks();
        }

        // Only drinks from the bars expire (not supply crates), and only on the player's tray.
        private void TossOldDrinks()
        {
            scratch.Clear();
            for (int i = 0; i < stack.Count; i++)
            {
                CarriedItem item = stack.At(i);
                if (item == null) continue;
                scratch.Add(item);
                if (!landedAt.TryGetValue(item, out float since)) landedAt[item] = since = Time.time;
                if (Time.time - since >= tossDrinksAfter && Station.AnyMakes(item.Definition)) scratch[scratch.Count - 1] = null;
            }
            foreach (CarriedItem item in new List<CarriedItem>(landedAt.Keys))
            {
                if (!scratch.Contains(item)) landedAt.Remove(item);
            }
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                CarriedItem item = stack.At(i);
                if (item != null && !scratch.Contains(item)) Toss(item);
            }
        }

        /// <summary>Takes an item off the tray and makes it vanish with a little hop.</summary>
        public void Toss(CarriedItem item)
        {
            if (item == null || !stack.Remove(item)) return;
            landedAt.Remove(item);
            item.MoveTo(transform, Vector3.up * 2.4f, 0.05f);
            Destroy(item.gameObject, 0.35f);
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
