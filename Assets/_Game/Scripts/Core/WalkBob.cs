using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Walks a character: with a model that has legs and arms on joints (the Boba Art Kit's
    /// characters), the feet step forward and back while the arms swing, and the body only lifts a
    /// little with each step. While the character holds something (drinks, a tray, a crate) both
    /// arms reach forward to hold it. Models without joints get a cheerful waddle instead: the
    /// whole model hops and rocks side to side. While <see cref="Sitting"/> is set the legs bend
    /// forward, as on a chair or sofa.
    /// </summary>
    public class WalkBob : MonoBehaviour
    {
        [SerializeField, Tooltip("The model to animate (a child of this object).")]
        private Transform visual;

        [SerializeField, Tooltip("How high each hop goes for models without legs, in meters.")]
        private float hopHeight = 0.08f;

        [SerializeField, Tooltip("How much the body lifts with each step for models with legs, in meters.")]
        private float stepLift = 0.025f;

        [SerializeField, Tooltip("Steps per meter walked.")]
        private float stepsPerMeter = 1.6f;

        [SerializeField, Tooltip("Side-to-side rock in degrees, for models without legs.")]
        private float rockDegrees = 6f;

        [SerializeField, Tooltip("How far the legs swing forward and back, in degrees.")]
        private float legSwing = 32f;

        [SerializeField, Tooltip("How far the arms swing while walking empty-handed, in degrees.")]
        private float armSwing = 22f;

        [SerializeField, Tooltip("How far the arms reach forward while holding something, in degrees.")]
        private float holdAngle = 78f;

        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 lastPosition;
        private float phase;
        private float amount;
        private float hold;
        private float sit;
        private Transform legL, legR, shoulderL, shoulderR;
        private IHoldsItems holder;

        private bool HasLegs => legL != null && legR != null;

        /// <summary>Set while the character sits (customers at a table, baristas on a break).</summary>
        public bool Sitting { get; set; }

        private void Awake()
        {
            holder = GetComponent<IHoldsItems>();
            if (visual == null) return;
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            lastPosition = transform.position;
            legL = FindDeep(visual, "LegL");
            legR = FindDeep(visual, "LegR");
            shoulderL = FindDeep(visual, "ShoulderL");
            shoulderR = FindDeep(visual, "ShoulderR");
        }

        private void LateUpdate()
        {
            if (visual == null || Time.deltaTime <= 0f) return;

            Vector3 moved = transform.position - lastPosition;
            moved.y = 0f;
            lastPosition = transform.position;
            float distance = moved.magnitude;

            bool walking = distance / Time.deltaTime > 0.2f;
            amount = Mathf.MoveTowards(amount, walking ? 1f : 0f, Time.deltaTime * 6f);
            phase += distance * stepsPerMeter * Mathf.PI;
            float step = Mathf.Sin(phase);

            if (!HasLegs)
            {
                visual.localPosition = restPosition + Vector3.up * (Mathf.Abs(step) * hopHeight * amount);
                visual.localRotation = Quaternion.Euler(0f, 0f, step * rockDegrees * amount) * restRotation;
                return;
            }

            // The feet step: one leg forward while the other goes back, settling together when stopped.
            visual.localPosition = restPosition + Vector3.up * (Mathf.Abs(step) * stepLift * amount);
            sit = Mathf.MoveTowards(sit, Sitting ? 1f : 0f, Time.deltaTime * 6f);
            const float SitAngle = -80f; // thighs forward, as on a seat
            legL.localRotation = Quaternion.Euler(-step * legSwing * amount + SitAngle * sit, 0f, 0f);
            legR.localRotation = Quaternion.Euler(step * legSwing * amount + SitAngle * sit, 0f, 0f);

            // Arms: swing opposite the legs, or reach forward together while holding something.
            bool holding = holder != null && holder.IsHolding;
            hold = Mathf.MoveTowards(hold, holding ? 1f : 0f, Time.deltaTime * 5f);
            float swing = step * armSwing * amount * (1f - hold);
            if (shoulderL != null) shoulderL.localRotation = Quaternion.Euler(swing - holdAngle * hold, 0f, 0f);
            if (shoulderR != null) shoulderR.localRotation = Quaternion.Euler(-swing - holdAngle * hold, 0f, 0f);
        }

        private static Transform FindDeep(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName) return child;
                Transform found = FindDeep(child, childName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
