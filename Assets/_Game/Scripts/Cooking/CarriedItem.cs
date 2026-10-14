using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// The model of an item out in the world. It hops in a little arc whenever it moves
    /// from one stack to another (station, player, counter, customer).
    /// </summary>
    public class CarriedItem : MonoBehaviour
    {
        [SerializeField, Tooltip("What this item is.")]
        private ItemDefinition definition;

        [SerializeField, Tooltip("Seconds a hop between stacks takes.")]
        private float hopSeconds = 0.2f;

        [SerializeField, Tooltip("How high the hop goes.")]
        private float hopHeight = 0.6f;

        private Vector3 from;
        private Vector3 to;
        private float progress = 1f;

        public ItemDefinition Definition => definition;

        public void Init(ItemDefinition itemDefinition) => definition = itemDefinition;

        /// <summary>Re-parents the item and hops it to <paramref name="localPosition"/>.</summary>
        public void MoveTo(Transform parent, Vector3 localPosition)
        {
            transform.SetParent(parent, true);
            transform.localRotation = Quaternion.identity;
            from = transform.localPosition;
            to = localPosition;
            progress = 0f;
        }

        private void Update()
        {
            if (progress >= 1f) return;
            progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(hopSeconds, 0.01f));
            transform.localPosition = Vector3.Lerp(from, to, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * hopHeight);
        }
    }
}
