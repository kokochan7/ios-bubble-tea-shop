using UnityEngine;

namespace BubbleTea.Player
{
    /// <summary>
    /// Keeps the camera at a fixed angle above and behind the player, gliding smoothly
    /// as they move. The camera never rotates, which keeps controls predictable.
    /// On a phone only, it backs off along the same angle so at least <see cref="viewWidth"/>
    /// meters fit across the tall, narrow screen. On the PC (and in the Editor) it stays as set.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField, Tooltip("What the camera follows (the player).")]
        private Transform target;

        [SerializeField, Tooltip("Camera position relative to the player.")]
        private Vector3 offset = new(0f, 13f, -9f);

        [SerializeField, Tooltip("Seconds to catch up with the player. 0 means locked on.")]
        private float smoothTime = 0.12f;

        [SerializeField, Tooltip("Phones only: meters of the shop that fit across the screen at the player. Bigger = more zoomed out.")]
        private float viewWidth = 10f;

        [SerializeField, Tooltip("Phones only: the camera's vertical field of view in degrees.")]
        private float fieldOfView = 50f;

        private Vector3 velocity;
        private Camera view;

        private void Start()
        {
            view = GetComponent<Camera>();
            if (target == null) return;
            transform.position = target.position + Offset();
            transform.LookAt(target.position);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + Offset();
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }

        // The offset, pushed further back along the same angle when the screen is too narrow
        // to show viewWidth meters from the normal distance.
        private Vector3 Offset()
        {
            if (view == null || !Application.isMobilePlatform) return offset;
            view.fieldOfView = fieldOfView;
            float halfWidthAtOneMeter = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Max(view.aspect, 0.01f);
            float needed = viewWidth * 0.5f / halfWidthAtOneMeter;
            float distance = Mathf.Max(offset.magnitude, needed);
            return offset.normalized * distance;
        }
    }
}
