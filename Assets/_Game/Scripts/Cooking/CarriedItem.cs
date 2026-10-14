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
        private Vector3 fromScale = Vector3.one;
        private Vector3 toScale = Vector3.one;
        private float progress = 1f;

        public ItemDefinition Definition => definition;

        public void Init(ItemDefinition itemDefinition) => definition = itemDefinition;

        /// <summary>
        /// Re-parents the item and hops it to <paramref name="localPosition"/>, growing or
        /// shrinking to <paramref name="size"/> times its normal size on the way.
        /// </summary>
        public void MoveTo(Transform parent, Vector3 localPosition, float size = 1f)
        {
            transform.SetParent(parent, true);
            transform.localRotation = Quaternion.identity;
            from = transform.localPosition;
            to = localPosition;
            fromScale = transform.localScale;
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            toScale = new Vector3(size / NonZero(parentScale.x), size / NonZero(parentScale.y), size / NonZero(parentScale.z));
            progress = 0f;
        }

        private static float NonZero(float value) => Mathf.Abs(value) < 0.0001f ? 1f : value;

        private void Update()
        {
            if (progress >= 1f) return;
            progress = Mathf.Min(1f, progress + Time.deltaTime / Mathf.Max(hopSeconds, 0.01f));
            transform.localPosition = Vector3.Lerp(from, to, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * hopHeight);
            transform.localScale = Vector3.Lerp(fromScale, toScale, progress);
        }
    }
}
