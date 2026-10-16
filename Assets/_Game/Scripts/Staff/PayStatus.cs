using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// A worker's pay light, a round light above their head like a customer's mood light.
    /// Green: paid and working. Yellow: wages owed for up to <see cref="GraceHours"/> shop hours;
    /// they keep working, but slower. Red: owed for longer; they stand still and do nothing.
    /// Once the <see cref="Payroll"/> pays what they're owed, they're back to green.
    /// </summary>
    public class PayStatus : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly Color Paid = new(0.35f, 0.85f, 0.4f);
        private static readonly Color Late = new(1f, 0.82f, 0.2f);
        private static readonly Color Stopped_ = new(0.95f, 0.3f, 0.3f); // red

        /// <summary>Shop hours a worker keeps working (slower) without pay before they stop.</summary>
        public const int GraceHours = 6;

        /// <summary>How fast a worker works while their pay is late (1 = normal).</summary>
        public const float LateSpeed = 0.5f;

        [SerializeField, Tooltip("The round light above the worker's head. Optional.")]
        private Renderer statusLight;

        [SerializeField, Tooltip("The worker's own script (Barista, Cashier or Cleaner), switched off while unpaid.")]
        private Behaviour work;

        [SerializeField, Tooltip("Meters above the top of the worker (and their name) where the light floats.")]
        private float aboveHead = 0.85f;

        private MaterialPropertyBlock block;
        private bool placed;

        /// <summary>Shop hours of wages this worker is owed (0 = paid up).</summary>
        public int HoursOwed { get; private set; }

        /// <summary>True while any wages are owed (yellow or red).</summary>
        public bool Unpaid => HoursOwed > 0;

        /// <summary>True once they've gone unpaid past the grace hours: they stand still.</summary>
        public bool Stopped => HoursOwed > GraceHours;

        /// <summary>Multiplies how fast this worker walks and works: slower while their pay is late.</summary>
        public float Speed => Unpaid ? LateSpeed : 1f;

        /// <summary>Speed for any worker: 1 for workers without a pay light.</summary>
        public static float SpeedOf(Component worker)
        {
            var status = worker.GetComponent<PayStatus>();
            float skill = BubbleTea.Extras.TeamSkills.SpeedFor(worker); // the team's speed perk
            return (status != null ? status.Speed : 1f) * skill;
        }

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

        /// <summary>One more shop hour went unpaid.</summary>
        public void AddHourOwed() => SetHoursOwed(HoursOwed + 1);

        /// <summary>Everything they were owed has been paid: back to full speed.</summary>
        public void PaidUp() => SetHoursOwed(0);

        private void SetHoursOwed(int hours)
        {
            HoursOwed = Mathf.Max(0, hours);
            if (work != null) work.enabled = !Stopped;
            Show();
        }

        private void Show()
        {
            if (statusLight == null) return;
            Color color = Stopped ? Stopped_ : Unpaid ? Late : Paid;
            block ??= new MaterialPropertyBlock();
            statusLight.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            statusLight.SetPropertyBlock(block);
        }

        private void Update()
        {
            if (!placed && statusLight != null) PlaceLight();
            // Blink once they've stopped, so it catches the eye.
            if (statusLight != null && Stopped) statusLight.enabled = Mathf.Repeat(Time.time * 2f, 1f) < 0.7f;
            else if (statusLight != null && !statusLight.enabled) statusLight.enabled = true;
        }
    }
}
