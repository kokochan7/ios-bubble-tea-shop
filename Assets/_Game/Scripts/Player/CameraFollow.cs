using UnityEngine;

namespace BubbleTea.Player
{
    /// <summary>
    /// Keeps the camera at a fixed angle above and behind the player, gliding smoothly
    /// as they move. The camera never rotates, which keeps controls predictable.
    /// On a phone only, it backs off along the same angle so at least <see cref="PhoneViewWidth"/>
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

        // Phones only. Set in code (not in the scene) so changing them only needs a new build,
        // not a scene rebuild.
        private const float PhoneViewWidth = 12f;    // meters across the screen at the player; bigger = more zoomed out
        private const float PhoneFieldOfView = 55f;  // vertical, in degrees

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
        // to show PhoneViewWidth meters from the normal distance.
        private Vector3 Offset()
        {
            if (view == null || !Application.isMobilePlatform) return offset;
            view.fieldOfView = PhoneFieldOfView;
            float halfWidthAtOneMeter = Mathf.Tan(PhoneFieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Max(view.aspect, 0.01f);
            float needed = PhoneViewWidth * 0.5f / halfWidthAtOneMeter;
            float distance = Mathf.Max(offset.magnitude, needed);
            return offset.normalized * distance;
        }
    }
}
