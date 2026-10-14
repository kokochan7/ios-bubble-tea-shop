using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Gives a character a cheerful waddle while it moves: the model hops a little with each
    /// step and rocks side to side. A stand-in for real walk animations.
    /// </summary>
    public class WalkBob : MonoBehaviour
    {
        [SerializeField, Tooltip("The model to bounce (a child of this object).")]
        private Transform visual;

        [SerializeField, Tooltip("How high each hop goes, in meters.")]
        private float hopHeight = 0.08f;

        [SerializeField, Tooltip("Steps per meter walked.")]
        private float stepsPerMeter = 1.6f;

        [SerializeField, Tooltip("Side-to-side rock in degrees.")]
        private float rockDegrees = 6f;

        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 lastPosition;
        private float phase;
        private float amount;

        private void Awake()
        {
            if (visual == null) return;
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            lastPosition = transform.position;
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
            visual.localPosition = restPosition + Vector3.up * (Mathf.Abs(step) * hopHeight * amount);
            visual.localRotation = Quaternion.Euler(0f, 0f, step * rockDegrees * amount) * restRotation;
        }
    }
}
