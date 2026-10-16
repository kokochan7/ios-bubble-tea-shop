using BubbleTea.Customers;
using BubbleTea.Player;
using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Automatic glass doors: the two panels slide apart when the player or a customer comes
    /// near, and close again once nobody is around.
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        [SerializeField, Tooltip("The left panel (slides toward -X).")]
        private Transform left;

        [SerializeField, Tooltip("The right panel (slides toward +X).")]
        private Transform right;

        [SerializeField, Tooltip("How far each panel slides open, in meters.")]
        private float slide = 2.6f;

        [SerializeField, Tooltip("How close someone must be (meters from the middle of the doorway) to open it.")]
        private float openDistance = 3.5f;

        [SerializeField, Tooltip("Opening/closing speed (0 to 1 per second).")]
        private float speed = 2.5f;

        private Vector3 leftClosed, rightClosed;
        private float open;

        private void Awake()
        {
            if (left != null) leftClosed = left.localPosition;
            if (right != null) rightClosed = right.localPosition;
        }

        private void Update()
        {
            float target = SomeoneNear() ? 1f : 0f;
            open = Mathf.MoveTowards(open, target, Time.deltaTime * speed);
            float eased = Mathf.SmoothStep(0f, 1f, open);
            if (left != null) left.localPosition = leftClosed + Vector3.left * slide * eased;
            if (right != null) right.localPosition = rightClosed + Vector3.right * slide * eased;
        }

        private bool SomeoneNear()
        {
            Vector3 middle = transform.position;
            float range = openDistance * openDistance;
            if (PlayerMovement.Current != null && Flat(PlayerMovement.Current.position - middle) < range) return true;
            foreach (Customer customer in Customer.Active)
            {
                if (customer != null && Flat(customer.transform.position - middle) < range) return true;
            }
            return false;
        }

        private static float Flat(Vector3 delta) => delta.x * delta.x + delta.z * delta.z;
    }
}
