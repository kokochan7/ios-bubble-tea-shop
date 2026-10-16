using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// The bamboo whisk in the bowl on a hand-whisked bar: while the bar is whisking it zig-zags
    /// quickly through the matcha (the real "M" stroke), tilting as it goes, and the green froth
    /// rises and wobbles. Idle, it stands still in the bowl.
    /// </summary>
    public class WhiskMotion : MonoBehaviour
    {
        [SerializeField, Tooltip("The bar this whisk belongs to.")]
        private Station station;

        [SerializeField, Tooltip("The whisk (pivot at its tip, in the bowl).")]
        private Transform whisk;

        [SerializeField, Tooltip("The froth on top of the tea, which grows as the drink gets whisked.")]
        private Transform froth;

        [SerializeField, Tooltip("Strokes per second.")]
        private float strokes = 7f;

        [SerializeField, Tooltip("How far the whisk swings side to side, in meters.")]
        private float swing = 0.06f;

        private Vector3 restPosition;
        private Vector3 frothScale;
        private float phase;

        private void Awake()
        {
            if (whisk != null) restPosition = whisk.localPosition;
            if (froth != null) frothScale = froth.localScale;
        }

        private void Update()
        {
            bool busy = station != null && station.Whisking;
            if (whisk != null)
            {
                if (busy)
                {
                    phase += Time.deltaTime * strokes * Mathf.PI * 2f;
                    // Side to side fast, front to back at half speed: a zig-zag through the bowl.
                    float side = Mathf.Sin(phase);
                    float depth = Mathf.Sin(phase * 0.5f);
                    whisk.localPosition = restPosition + new Vector3(side * swing, Mathf.Abs(side) * 0.01f, depth * swing * 0.5f);
                    whisk.localRotation = Quaternion.Euler(depth * 8f, 0f, -side * 22f);
                }
                else
                {
                    whisk.localPosition = Vector3.Lerp(whisk.localPosition, restPosition, Time.deltaTime * 8f);
                    whisk.localRotation = Quaternion.Slerp(whisk.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                }
            }
            if (froth != null)
            {
                float progress = station != null ? station.Progress : 0f;
                float wobble = busy ? 1f + 0.06f * Mathf.Sin(phase * 2f) : 1f;
                froth.localScale = new Vector3(frothScale.x * wobble, frothScale.y * (0.3f + 0.7f * progress), frothScale.z * wobble);
            }
        }
    }
}
