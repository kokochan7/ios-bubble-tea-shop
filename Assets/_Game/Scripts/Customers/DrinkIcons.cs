using System.Collections.Generic;
using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Shows a row of drink models (just pictures, not real drinks) under a parent, one per
    /// order. Used by the bubble over a customer's head and the order board on the counter.
    /// </summary>
    public static class DrinkIcons
    {
        /// <summary>Replaces the icons in <paramref name="icons"/> with one per order, side by side and centered.</summary>
        public static void Show(Transform parent, IReadOnlyList<DrinkRecipe> orders, float scale, float spacing, List<GameObject> icons)
        {
            Clear(icons);
            if (parent == null || orders == null) return;
            for (int i = 0; i < orders.Count; i++)
            {
                DrinkRecipe order = orders[i];
                if (order == null || order.Drink == null || order.Drink.Prefab == null) continue;
                GameObject icon = Object.Instantiate(order.Drink.Prefab, parent);
                icon.transform.localPosition = new Vector3((i - (orders.Count - 1) * 0.5f) * spacing, 0f, 0f);
                icon.transform.localRotation = Quaternion.identity;
                icon.transform.localScale = Vector3.one * scale;
                var item = icon.GetComponent<CarriedItem>();
                if (item != null) Object.Destroy(item);
                icons.Add(icon);
            }
        }

        public static void Clear(List<GameObject> icons)
        {
            foreach (GameObject icon in icons)
            {
                if (icon != null) Object.Destroy(icon);
            }
            icons.Clear();
        }
    }
}
