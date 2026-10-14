using BubbleTea.Building;
using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// Shows the player what to do next: a bouncing marker over the cheapest unlock pad
    /// that's still waiting, and a pointer at the player's feet aiming toward it.
    /// Both hide while the player is standing on that pad.
    /// </summary>
    public class GoalArrow : MonoBehaviour
    {
        [SerializeField, Tooltip("The player.")]
        private Transform player;

        [SerializeField, Tooltip("Bounces above the target pad.")]
        private Transform marker;

        [SerializeField, Tooltip("Sits around the player and turns to face the target pad.")]
        private Transform pointer;

        [SerializeField, Tooltip("Height of the marker above the pad.")]
        private float markerHeight = 2.5f;

        [SerializeField, Tooltip("How far the marker bobs up and down.")]
        private float bobAmount = 0.35f;

        [SerializeField, Tooltip("Bobs per second.")]
        private float bobSpeed = 2f;

        [SerializeField, Tooltip("The pointer hides when the player is closer than this to the pad.")]
        private float hidePointerWithin = 3f;

        [SerializeField, Tooltip("Seconds between choosing the target pad again.")]
        private float secondsBetweenChecks = 0.5f;

        private UnlockPad target;
        private float timer;

        private void LateUpdate()
        {
            timer -= Time.deltaTime;
            if (timer <= 0f || target == null || !target.isActiveAndEnabled)
            {
                timer = secondsBetweenChecks;
                target = PickTarget();
            }

            bool hasTarget = target != null && player != null;
            Vector3 toTarget = hasTarget ? target.transform.position - player.position : Vector3.zero;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (marker != null)
            {
                bool show = hasTarget && distance > 1.2f;
                if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
                if (show)
                {
                    float bob = Mathf.Sin(Time.time * bobSpeed * Mathf.PI * 2f) * bobAmount;
                    marker.position = target.transform.position + Vector3.up * (markerHeight + bob);
                }
            }

            if (pointer != null)
            {
                bool show = hasTarget && distance > hidePointerWithin;
                if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);
                if (show)
                {
                    pointer.position = new Vector3(player.position.x, 0.05f, player.position.z);
                    pointer.rotation = Quaternion.LookRotation(toTarget / distance, Vector3.up);
                }
            }
        }

        // The pad with the least money left to pay is usually the next sensible goal.
        private static UnlockPad PickTarget()
        {
            UnlockPad best = null;
            foreach (UnlockPad pad in UnlockPad.Active)
            {
                if (pad == null || pad.IsUnlocked || pad.Definition == null) continue;
                if (best == null || pad.Remaining < best.Remaining) best = pad;
            }
            return best;
        }
    }
}
