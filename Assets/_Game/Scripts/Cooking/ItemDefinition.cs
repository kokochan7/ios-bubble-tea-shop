using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// Anything that can be carried or stacked: an empty cup, milk tea, boba milk tea...
    /// Create new ones with right-click > Create > Bubble Tea > Item.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Tea/Item", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Name for menus and debugging.")]
        private string displayName = "Item";

        [SerializeField, Tooltip("What the item looks like when it's carried or stacked.")]
        private GameObject prefab;

        public string DisplayName => displayName;
        public GameObject Prefab => prefab;

        /// <summary>Spawns this item's model, ready to be pushed onto an <see cref="ItemStack"/>.</summary>
        public CarriedItem Spawn(Vector3 position)
        {
            GameObject go = prefab != null ? Instantiate(prefab, position, Quaternion.identity) : new GameObject(displayName);
            go.transform.position = position;
            var item = go.GetComponent<CarriedItem>();
            if (item == null) item = go.AddComponent<CarriedItem>();
            item.Init(this);
            return item;
        }
    }
}
