using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Items laid out at an anchor point: side by side in a grid of columns and rows, and
    /// only piled upward once every spot is taken. Used for trays, station inputs and
    /// outputs, and the serving counter.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        [SerializeField, Tooltip("Where the bottom item sits. Items pile upward from here.")]
        private Transform anchor;

        [SerializeField, Min(0), Tooltip("Most items this stack holds.")]
        private int capacity = 4;

        [SerializeField, Tooltip("Height of each item in the pile (at normal size).")]
        private float itemHeight = 0.32f;

        [SerializeField, Min(1), Tooltip("Items side by side across (x).")]
        private int columns = 1;

        [SerializeField, Min(1), Tooltip("Rows of items, front to back (z).")]
        private int rows = 1;

        [SerializeField, Tooltip("Gap between item centers across (x) and front to back (y).")]
        private Vector2 spacing = new(0.3f, 0.3f);

        [SerializeField, Min(0.1f), Tooltip("Size of the items here (1 = normal).")]
        private float itemScale = 1f;

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
            item.MoveTo(anchor, SlotPosition(Items.Count - 1), itemScale);
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
                for (int j = i; j < Items.Count; j++) Items[j].MoveTo(anchor, SlotPosition(j), itemScale);
                return true;
            }
            item = null;
            return false;
        }

        /// <summary>Removes the bottom-most (oldest) item that matches, letting the rest settle down.</summary>
        public bool TryTakeOldest(Func<ItemDefinition, bool> match, out CarriedItem item)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] == null || !match(Items[i].Definition)) continue;
                item = Items[i];
                Items.RemoveAt(i);
                for (int j = i; j < Items.Count; j++) Items[j].MoveTo(anchor, SlotPosition(j), itemScale);
                return true;
            }
            item = null;
            return false;
        }

        /// <summary>The item at <paramref name="index"/>, oldest (bottom) first.</summary>
        public CarriedItem At(int index) => Items[index];

        /// <summary>Takes this exact item out, letting the rest settle down.</summary>
        public bool Remove(CarriedItem item)
        {
            int i = Items.IndexOf(item);
            if (i < 0) return false;
            Items.RemoveAt(i);
            for (int j = i; j < Items.Count; j++) Items[j].MoveTo(anchor, SlotPosition(j), itemScale);
            return true;
        }

        /// <summary>How many items in the stack match.</summary>
        public int CountOf(Func<ItemDefinition, bool> match)
        {
            int count = 0;
            foreach (CarriedItem item in Items)
            {
                if (item != null && match(item.Definition)) count++;
            }
            return count;
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

        // Fills the front row left to right, then the rows behind it, then starts a layer on top.
        private Vector3 SlotPosition(int index)
        {
            int across = Mathf.Max(1, columns);
            int deep = Mathf.Max(1, rows);
            int perLayer = across * deep;
            int layer = index / perLayer;
            int slot = index % perLayer;
            int column = slot % across;
            int row = slot / across;
            float x = (column - (across - 1) * 0.5f) * spacing.x;
            float z = ((deep - 1) * 0.5f - row) * spacing.y;
            return new Vector3(x, layer * itemHeight * itemScale, z);
        }
    }
}
