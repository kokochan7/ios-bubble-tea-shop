using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Makes a river flow: its ripple texture slides downstream (+x), and floating bits (leaves,
    /// ripples) drift along with it, wrapping back upstream once they float past the far end.
    /// </summary>
    public class WaterFlow : MonoBehaviour
    {
        [SerializeField, Tooltip("The water surface whose texture slides along.")]
        private Renderer water;

        [SerializeField, Tooltip("How fast the texture slides, in texture repeats per second.")]
        private float textureSpeed = 0.05f;

        [SerializeField, Tooltip("Things floating on the water.")]
        private Transform[] drifters = System.Array.Empty<Transform>();

        [SerializeField, Tooltip("How fast floating things drift downstream, in meters per second.")]
        private float driftSpeed = 0.9f;

        [SerializeField, Tooltip("Floating things wrap back to the start once past this x.")]
        private float endX = 40f;

        [SerializeField, Tooltip("Where floating things wrap back to.")]
        private float startX = -30f;

        private Material material;
        private Vector2 offset;

        private void Start()
        {
            if (water != null) material = water.material; // its own copy, so only this river moves
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (material != null)
            {
                offset.x = Mathf.Repeat(offset.x - textureSpeed * dt, 1f); // slides toward +x
                material.mainTextureOffset = offset;
            }
            for (int i = 0; i < drifters.Length; i++)
            {
                Transform bit = drifters[i];
                if (bit == null) continue;
                Vector3 spot = bit.position;
                spot.x += driftSpeed * (0.8f + 0.1f * (i % 5)) * dt;
                if (spot.x > endX) spot.x = startX;
                bit.position = spot;
            }
        }
    }
}
