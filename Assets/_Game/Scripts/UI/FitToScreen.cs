using UnityEngine;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shrinks a popup panel just enough to fit on screen (with a small margin), so tall panels
    /// aren't cut off on wide or short screens. It never makes a panel bigger than built.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FitToScreen : MonoBehaviour
    {
        [SerializeField, Tooltip("Space kept free around the panel, in UI units.")]
        private float margin = 30f;

        private RectTransform rect;

        private void Awake() => rect = (RectTransform)transform;

        private void OnEnable() => Fit();

        private void LateUpdate() => Fit();

        private void Fit()
        {
            if (rect == null || !(rect.parent is RectTransform parent)) return;
            Vector2 room = parent.rect.size - 2f * margin * Vector2.one;
            Vector2 size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f || room.x <= 0f || room.y <= 0f) return;
            float scale = Mathf.Min(1f, room.x / size.x, room.y / size.y);
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
