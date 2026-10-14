using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Café swing doors: they open when the player or a member of staff walks up and swing
    /// shut behind them. Only for looks (no collider), so they never block anyone.
    /// </summary>
    public class SwingDoor : MonoBehaviour
    {
        [SerializeField, Tooltip("The hinges of the door leaves; each turns to its open angle.")]
        private Transform[] leaves;

        [SerializeField, Tooltip("How far each leaf turns when open, in degrees (same order as the leaves).")]
        private float[] openAngles;

        [SerializeField, Tooltip("Opens when anyone carrying things (player or staff) is this close, in meters.")]
        private float openDistance = 2f;

        [SerializeField, Tooltip("How fast the doors swing.")]
        private float speed = 5f;

        private float open; // 0 shut, 1 open

        private void Update()
        {
            bool someoneNear = false;
            Vector3 center = transform.position;
            foreach (ItemCarrier carrier in ItemCarrier.All)
            {
                Vector3 delta = carrier.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude <= openDistance * openDistance)
                {
                    someoneNear = true;
                    break;
                }
            }

            open = Mathf.MoveTowards(open, someoneNear ? 1f : 0f, speed * Time.deltaTime);
            float eased = Mathf.SmoothStep(0f, 1f, open);
            for (int i = 0; i < leaves.Length; i++)
            {
                if (leaves[i] == null) continue;
                float angle = openAngles != null && i < openAngles.Length ? openAngles[i] : 90f;
                leaves[i].localRotation = Quaternion.Euler(0f, angle * eased, 0f);
            }
        }
    }
}
