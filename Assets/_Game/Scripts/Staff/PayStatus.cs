using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A worker's pay light, a round light above their head like a customer's mood light:
    /// green while they're paid and working, red while their wages are owed. While unpaid,
    /// the worker stands still and does nothing until the <see cref="Payroll"/> can pay them.
    /// </summary>
    public class PayStatus : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color Paid = new(0.35f, 0.85f, 0.4f);
        private static readonly Color Owed = new(0.95f, 0.3f, 0.3f);

        [SerializeField, Tooltip("The round light above the worker's head. Optional.")]
        private Renderer statusLight;

        [SerializeField, Tooltip("The worker's own script (Barista, Cashier or Cleaner), switched off while unpaid.")]
        private Behaviour work;

        [SerializeField, Tooltip("Meters above the top of the worker (and their name) where the light floats.")]
        private float aboveHead = 0.85f;

        private MaterialPropertyBlock block;
        private bool placed;

        /// <summary>True while this worker's wages are owed: they stand still.</summary>
        public bool Unpaid { get; private set; }

        private void Start() => Show();

        // Floats the light above the tallest visible part of the worker's model (measured once
        // the worker has its full size, since new hires pop in from small).
        private void PlaceLight()
        {
            float scale = transform.lossyScale.y;
            if (scale < 0.9f) return;
            float top = 1.6f;
            foreach (Renderer part in GetComponentsInChildren<Renderer>())
            {
                if (part == statusLight || !part.enabled || part.GetComponent<TextMesh>() != null) continue;
                top = Mathf.Max(top, (part.bounds.max.y - transform.position.y) / scale);
            }
            statusLight.transform.localPosition = Vector3.up * (top + aboveHead);
            placed = true;
        }

        /// <summary>Marks the worker as owed (they stop) or paid (they get back to work).</summary>
        public void SetUnpaid(bool unpaid)
        {
            if (Unpaid == unpaid) return;
            Unpaid = unpaid;
            if (work != null) work.enabled = !unpaid;
            Show();
        }

        private void Show()
        {
            if (statusLight == null) return;
            Color color = Unpaid ? Owed : Paid;
            block ??= new MaterialPropertyBlock();
            statusLight.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            statusLight.SetPropertyBlock(block);
        }

        private void Update()
        {
            if (!placed && statusLight != null) PlaceLight();
            // Blink while unpaid, so it catches the eye.
            if (statusLight != null && Unpaid) statusLight.enabled = Mathf.Repeat(Time.time * 2f, 1f) < 0.7f;
            else if (statusLight != null && !statusLight.enabled) statusLight.enabled = true;
        }
    }
}
