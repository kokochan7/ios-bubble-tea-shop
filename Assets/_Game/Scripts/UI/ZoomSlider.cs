using BubbleTea.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The Zoom slider in the Settings menu: drag (or tap) along the bar to move the camera
    /// closer (right) or further away (left). The choice is remembered on this device.
    /// </summary>
    public class ZoomSlider : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [SerializeField, Tooltip("The bar you drag along (this object's rect is used if empty).")]
        private RectTransform track;

        [SerializeField, Tooltip("The colored part of the bar, up to the handle (a Filled image).")]
        private Image fill;

        [SerializeField, Tooltip("The knob that sits at the current value.")]
        private RectTransform handle;

        private void OnEnable() => Show(ToSlider(CameraFollow.Zoom));

        public void OnPointerDown(PointerEventData eventData) => Pick(eventData);

        public void OnDrag(PointerEventData eventData) => Pick(eventData);

        private void Pick(PointerEventData eventData)
        {
            RectTransform bar = track != null ? track : (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(bar, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            Rect rect = bar.rect;
            float value = Mathf.Clamp01((local.x - rect.xMin) / Mathf.Max(rect.width, 1f));
            CameraFollow.Zoom = FromSlider(value);
            Show(value);
        }

        // Left end = furthest away, right end = closest.
        private static float FromSlider(float value) => Mathf.Lerp(CameraFollow.MaxZoom, CameraFollow.MinZoom, value);

        private static float ToSlider(float zoom) => Mathf.InverseLerp(CameraFollow.MaxZoom, CameraFollow.MinZoom, zoom);

        private void Show(float value)
        {
            if (fill != null) fill.fillAmount = value;
            if (handle != null)
            {
                handle.anchorMin = new Vector2(value, 0.5f);
                handle.anchorMax = new Vector2(value, 0.5f);
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
