using UnityEngine;
using UnityEngine.InputSystem;

namespace BubbleTea.Player
{
    /// <summary>
    /// Moves the player on the floor using the on-screen joystick, or WASD / arrow keys
    /// in the editor. "Up" on the stick always means "away from the camera".
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Tooltip("The on-screen joystick. Leave empty to use only the keyboard.")]
        private VirtualJoystick joystick;

        [SerializeField, Tooltip("Camera used to work out which way is 'up' on the stick.")]
        private Transform cameraTransform;

        [SerializeField, Tooltip("Top speed in meters per second.")]
        private float moveSpeed = 6f;

        [SerializeField, Tooltip("How fast the player turns to face where they walk, in degrees per second.")]
        private float turnSpeed = 900f;

        private CharacterController controller;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Vector2 input = joystick != null ? joystick.Value : Vector2.zero;
            if (input.sqrMagnitude < 0.0001f) input = ReadKeyboard();
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            }
            Vector3 move = forward * input.y + right * input.x;

            // Simple gravity so the player stays on the floor.
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;

            controller.Move((move * moveSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);

            if (move.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, turnSpeed * Time.deltaTime);
            }
        }

        private static Vector2 ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            Vector2 value = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) value.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) value.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) value.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) value.x -= 1f;
            return value;
        }
    }
}
