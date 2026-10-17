using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Gives the whole on-screen HUD a raised, modern look without rebuilding the scenes: every
    /// rounded box and button (the MENU/GOALS/STATS column, money, clock, pop-ups, the extras)
    /// gets a bevelled sprite with a bright top edge and a darker bottom lip, a soft drop shadow,
    /// white text gets a shadow, and buttons squash a little when pressed. Runs on the HUD
    /// canvases only (not the signs over the pads), and looks again now and then for pop-up rows
    /// made later.
    /// </summary>
    public class HudStyle : MonoBehaviour
    {
        private const int Size = 128;
        private const float Radius = 40f;
        private const float Lip = 12f;        // the darker band along the bottom that makes it look raised
        private const float Highlight = 5f;   // the bright line along the top

        private static Sprite bevel;
        private readonly HashSet<Object> done = new(); // (not instance ids: GetInstanceID is obsolete in Unity 6.6)
        private float timer;

        /// <summary>A white rounded square with a top highlight and a bottom lip (tinted by the Image's color).</summary>
        public static Sprite Bevel => bevel != null ? bevel : bevel = MakeBevel();

        private static bool requested;

        /// <summary>Restyles on the next frame (pop-ups call this after drawing their rows).</summary>
        public static void Request() => requested = true;

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f && !requested) return;
            requested = false;
            timer = 3f; // a full look round is costly on a phone; pop-ups ask for one with Request()
            Restyle();
        }

        private void Restyle()
        {
#if UNITY_2023_1_OR_NEWER
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
#endif
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace || !canvas.isRootCanvas) continue;
                foreach (Image image in canvas.GetComponentsInChildren<Image>(true)) Style(image);
                foreach (Text text in canvas.GetComponentsInChildren<Text>(true)) Style(text);
            }
        }

        private void Style(Image image)
        {
            if (done.Count > 5000) done.Clear(); // pop-ups redraw their rows; forget the old ones now and then
            if (!done.Add(image)) return;
            Sprite sprite = image.sprite;
            if (sprite == null) return;
            bool rounded = sprite == bevel || sprite.name == "UISprite" || sprite.name == UiKit.RoundedName;
            if (!rounded) return;

            image.sprite = Bevel;
            if (image.type == Image.Type.Simple) image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;

            // Fills, bars and see-through rows stay flat; boxes and buttons float above the screen.
            if (image.type == Image.Type.Filled || image.color.a < 0.3f) return;
            if (image.GetComponent<Shadow>() == null)
            {
                Shadow shadow = image.gameObject.AddComponent<Shadow>();
                bool button = image.GetComponent<Button>() != null;
                shadow.effectColor = new Color(0f, 0f, 0f, button ? 0.4f : 0.3f);
                shadow.effectDistance = new Vector2(0f, button ? -8f : -6f);
            }
            if (image.GetComponent<Button>() != null && image.GetComponent<PressPop>() == null) image.gameObject.AddComponent<PressPop>();
        }

        private void Style(Text text)
        {
            if (!done.Add(text)) return;
            Color c = text.color;
            if (c.r + c.g + c.b < 1.8f || text.GetComponent<Shadow>() != null) return; // only light text
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        private static Sprite MakeBevel()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(Radius - x - 0.5f, x + 0.5f - (Size - Radius)));
                    float dy = Mathf.Max(0f, Mathf.Max(Radius - y - 0.5f, y + 0.5f - (Size - Radius)));
                    float depth = Radius - Mathf.Sqrt(dx * dx + dy * dy); // distance inside the edge
                    float alpha = Mathf.Clamp01(depth + 0.5f);

                    // Measured straight up from the shape's bottom edge and down from its top edge
                    // in this column: a darker lip along the bottom, a bright line along the top and
                    // a faint gloss fading in towards the top.
                    float side = x + 0.5f - Mathf.Clamp(x + 0.5f, Radius, Size - Radius);
                    float edge = Radius - Mathf.Sqrt(Mathf.Max(0f, Radius * Radius - side * side));
                    float fromBottom = y + 0.5f - edge;
                    float fromTop = Size - edge - (y + 0.5f);
                    float shade = 0.93f + 0.04f * Mathf.Clamp01((y - Size * 0.65f) / (Size * 0.35f));
                    if (fromBottom < Lip) shade = Mathf.Lerp(0.62f, shade, Mathf.Clamp01(fromBottom / Lip));
                    if (fromTop < Highlight) shade = Mathf.Lerp(1f, shade, Mathf.Clamp01(fromTop / Highlight));
                    byte v = (byte)(shade * 255f);
                    pixels[y * Size + x] = new Color32(v, v, v, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            float border = Radius + 4f;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = "HudBevel";
            return sprite;
        }
    }

    /// <summary>Squashes a button a little while it's held down.</summary>
    public class PressPop : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 rest;
        private bool down;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (down) return;
            down = true;
            rest = transform.localScale;
            transform.localScale = rest * 0.93f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!down) return;
            down = false;
            transform.localScale = rest;
        }

        private void OnDisable()
        {
            if (down) transform.localScale = rest;
            down = false;
        }
    }
}
