using UnityEngine;

namespace BubbleTea.Player
{
    /// <summary>
    /// Keeps the camera at a fixed angle above and behind the player, gliding smoothly
    /// as they move. The camera never rotates, which keeps controls predictable.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField, Tooltip("What the camera follows (the player).")]
        private Transform target;

        [SerializeField, Tooltip("Camera position relative to the player.")]
        private Vector3 offset = new(0f, 13f, -9f);

        [SerializeField, Tooltip("Seconds to catch up with the player. 0 means locked on.")]
        private float smoothTime = 0.12f;

        private Vector3 velocity;

        private void Start()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
