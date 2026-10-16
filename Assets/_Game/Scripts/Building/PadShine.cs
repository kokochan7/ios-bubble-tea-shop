using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Makes a floor pad easy to spot: its tile glows brighter and dimmer, and little sparkles
    /// circle above it.
    /// </summary>
    public class PadShine : MonoBehaviour
    {
        [SerializeField, Tooltip("The tile that glows.")]
        private Renderer tile;

        [SerializeField, Tooltip("The sparkles that circle above the pad.")]
        private Transform[] sparkles = System.Array.Empty<Transform>();

        [SerializeField, Tooltip("How far the sparkles circle from the middle, in meters.")]
        private float radius = 0.55f;

        [SerializeField, Tooltip("Glow pulses per second.")]
        private float pulseSpeed = 1.2f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock block;
        private Color baseColor = Color.white;

        private void Start()
        {
            if (tile != null && tile.sharedMaterial != null) baseColor = tile.sharedMaterial.color;
            if (tile != null && tile.sharedMaterial != null) tile.sharedMaterial.EnableKeyword("_EMISSION");
        }

        private void Update()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
            if (tile != null)
            {
                block ??= new MaterialPropertyBlock();
                tile.GetPropertyBlock(block);
                Color bright = Color.Lerp(baseColor, Color.white, 0.25f + 0.35f * pulse);
                block.SetColor(BaseColorId, bright);
                block.SetColor(ColorId, bright);
                block.SetColor(EmissionId, baseColor * (0.3f + 0.7f * pulse));
                tile.SetPropertyBlock(block);
            }
            for (int i = 0; i < sparkles.Length; i++)
            {
                if (sparkles[i] == null) continue;
                float angle = Time.time * 1.6f + i * Mathf.PI * 2f / sparkles.Length;
                sparkles[i].localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.35f + 0.15f * Mathf.Sin(Time.time * 3f + i), Mathf.Sin(angle) * radius);
                sparkles[i].localScale = Vector3.one * (0.08f + 0.04f * pulse);
            }
        }
    }
}
