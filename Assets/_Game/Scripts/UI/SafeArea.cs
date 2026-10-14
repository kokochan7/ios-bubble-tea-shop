using UnityEngine;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shrinks this RectTransform to the screen's safe area, so buttons and labels stay
    /// clear of the iPhone notch, the Dynamic Island and the home bar. Put HUD elements
    /// inside an object with this component. In the editor the safe area is the whole screen,
    /// unless you use the Device Simulator.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect applied;
        private Vector2Int appliedScreen;

        private void Awake()
        {
            rect = (RectTransform)transform;
            Apply();
        }

        // The safe area changes when the phone rotates, so keep checking (it's cheap).
        private void Update()
        {
            if (Screen.safeArea != applied || Screen.width != appliedScreen.x || Screen.height != appliedScreen.y) Apply();
        }

        private void Apply()
        {
            Rect safe = Screen.safeArea;
            applied = safe;
            appliedScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
