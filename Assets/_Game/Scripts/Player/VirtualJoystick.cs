using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BubbleTea.Player
{
    /// <summary>
    /// Floating on-screen joystick. Touch (or click) anywhere in this area and the stick
    /// appears under your finger; drag to steer. Works with mouse in the editor too.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField, Tooltip("The ring that appears where you touch.")]
        private RectTransform background;

        [SerializeField, Tooltip("The knob that follows your finger.")]
        private RectTransform handle;

        [SerializeField, Tooltip("How far (in canvas units) the knob can move from the center.")]
        private float radius = 110f;

        [SerializeField, Tooltip("Hide the joystick while nobody is touching the screen.")]
        private bool hideWhenIdle = true;

        private RectTransform area;
        private Canvas canvas;
        private int activePointerId;
        private bool isHeld;

        /// <summary>A quick tap (not a drag) on the play area, with its screen position. Used to tap on things in the shop.</summary>
        public static event Action<Vector2> Tapped;

        private Vector2 pressPosition;
        private float pressTime;

        /// <summary>Stick direction, each axis from -1 to 1. Zero when released.</summary>
        public Vector2 Value { get; private set; }

        private void Awake()
        {
            area = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            Release();
        }

        private void OnDisable() => Release();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isHeld) return;
            if (!TryGetLocalPoint(eventData, out Vector2 point)) return;

            isHeld = true;
            activePointerId = eventData.pointerId;
            pressPosition = eventData.position;
            pressTime = Time.unscaledTime;
            background.anchoredPosition = point;
            handle.anchoredPosition = Vector2.zero;
            SetVisible(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isHeld || eventData.pointerId != activePointerId) return;
            if (!TryGetLocalPoint(eventData, out Vector2 point)) return;

            Vector2 offset = point - background.anchoredPosition;
            Value = Vector2.ClampMagnitude(offset / Mathf.Max(radius, 1f), 1f);
            handle.anchoredPosition = Value * radius;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            bool tap = Time.unscaledTime - pressTime < 0.35f && (eventData.position - pressPosition).magnitude < Screen.dpi * 0.15f + 10f;
            Release();
            if (tap) Tapped?.Invoke(eventData.position);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Tapped = null;

        private void Release()
        {
            isHeld = false;
            Value = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            SetVisible(!hideWhenIdle);
        }

        private void SetVisible(bool visible)
        {
            if (background != null) background.gameObject.SetActive(visible);
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 point)
        {
            Camera eventCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : eventData.pressEventCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventCamera, out point);
        }
    }
}
