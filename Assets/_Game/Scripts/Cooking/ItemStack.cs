using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A vertical pile of items at an anchor point. Used for the player's hands, station
    /// inputs and outputs, and the serving counter.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        [SerializeField, Tooltip("Where the bottom item sits. Items pile upward from here.")]
        private Transform anchor;

        [SerializeField, Min(0), Tooltip("Most items this stack holds.")]
        private int capacity = 4;

        [SerializeField, Tooltip("Height of each item in the pile.")]
        private float itemHeight = 0.32f;

        private List<CarriedItem> items;
        private List<CarriedItem> Items => items ??= new List<CarriedItem>();

        /// <summary>Extra room on top of the capacity set in the Inspector, for upgrades.</summary>
        public int ExtraCapacity { get; set; }

        public int Count => Items.Count;
        public bool IsFull => Count >= capacity + ExtraCapacity || anchor == null;

        /// <summary>Adds an item on top. Returns false if the stack is full.</summary>
        public bool Push(CarriedItem item)
        {
            if (item == null || IsFull) return false;
            Items.Add(item);
            item.MoveTo(anchor, SlotPosition(Items.Count - 1));
            return true;
        }

        /// <summary>Removes the top-most item that matches, letting the rest settle down.</summary>
        public bool TryTake(Func<ItemDefinition, bool> match, out CarriedItem item)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i] == null || !match(Items[i].Definition)) continue;
                item = Items[i];
                Items.RemoveAt(i);
                for (int j = i; j < Items.Count; j++) Items[j].MoveTo(anchor, SlotPosition(j));
                return true;
            }
            item = null;
            return false;
        }

        /// <summary>True if any item in the stack matches.</summary>
        public bool Contains(Func<ItemDefinition, bool> match)
        {
            foreach (CarriedItem item in Items)
            {
                if (item != null && match(item.Definition)) return true;
            }
            return false;
        }

        private Vector3 SlotPosition(int index) => new(0f, index * itemHeight, 0f);
    }
}
