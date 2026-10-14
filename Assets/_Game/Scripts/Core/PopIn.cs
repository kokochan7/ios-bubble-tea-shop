using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Makes an object grow from nothing to its normal size with a little overshoot,
    /// then removes itself. Add it right after spawning something.
    /// </summary>
    public class PopIn : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds the pop takes.")]
        private float duration = 0.35f;

        private Vector3 targetScale;
        private float progress;

        private void Awake()
        {
            targetScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            progress += Time.deltaTime / Mathf.Max(duration, 0.01f);
            if (progress >= 1f)
            {
                transform.localScale = targetScale;
                Destroy(this);
                return;
            }
            transform.localScale = targetScale * EaseOutBack(progress);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }
    }
}
