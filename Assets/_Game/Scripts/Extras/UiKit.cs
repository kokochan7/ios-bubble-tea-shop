using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// Builds simple UI in code (canvas, panels, buttons, labels) in the same style as the rest of the
    /// HUD: dark rounded panels, bold white text, colored rounded buttons.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color PanelColor = new(0.15f, 0.15f, 0.2f, 0.96f);
        public static readonly Color Pink = new(0.91f, 0.51f, 0.6f);
        public static readonly Color Green = new(0.35f, 0.78f, 0.38f);
        public static readonly Color Blue = new(0.45f, 0.62f, 0.9f);
        public static readonly Color Gray = new(0.45f, 0.45f, 0.55f);
        public static readonly Color Red = new(0.85f, 0.35f, 0.35f);
        public static readonly Color Gold = new(0.98f, 0.8f, 0.3f);
        public static readonly Color Purple = new(0.62f, 0.45f, 0.88f);

        private static Sprite rounded;
        private static Sprite circle;
        private static Font font;

        public static Font Font => font != null ? font : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeSprite(64, 24f, true);
        public static Sprite Circle => circle != null ? circle : circle = MakeSprite(128, 63f, false);

        /// <summary>A screen-wide canvas drawn above the HUD.</summary>
        public static RectTransform Canvas(Transform parent, string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return (RectTransform)go.transform;
        }

        public static RectTransform Box(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite != null ? sprite : Rounded;
            image.type = sprite == null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            return (RectTransform)go.transform;
        }

        /// <summary>An empty object to group things under.</summary>
        public static RectTransform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = Font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        public static Button Button(Transform parent, string text, Color color, Vector2 size, UnityAction onClick, int fontSize = 40)
        {
            RectTransform rect = Box(parent, text, color);
            rect.sizeDelta = size;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            if (onClick != null) button.onClick.AddListener(onClick);
            Text label = Label(rect, text, fontSize, Color.white);
            Fill(label.rectTransform);
            return button;
        }

        /// <summary>The text on a button made by <see cref="Button"/>.</summary>
        public static Text TextOf(Button button) => button.GetComponentInChildren<Text>();

        /// <summary>Stretches to fill its parent, with an optional inset.</summary>
        public static void Fill(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Anchors at a point of the parent (0..1 each way), with its pivot there too.</summary>
        public static void Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// A centered pop-up panel with a title and a close button, over a dark backdrop that blocks taps.
        /// Returns the panel; <paramref name="body"/> is the area under the title.
        /// </summary>
        public static RectTransform Popup(Transform canvas, string title, Vector2 size, UnityAction onClose, out RectTransform body, out Text titleLabel)
        {
            RectTransform backdrop = Box(canvas, title + " Backdrop", new Color(0f, 0f, 0f, 0.45f));
            backdrop.GetComponent<Image>().type = Image.Type.Simple;
            backdrop.GetComponent<Image>().sprite = null;
            Fill(backdrop);

            RectTransform panel = Box(backdrop, "Panel", PanelColor);
            Pin(panel, new Vector2(0.5f, 0.5f), Vector2.zero, size);

            titleLabel = Label(panel, title, 56, Color.white);
            Pin(titleLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(size.x - 240f, 100f));

            if (onClose != null)
            {
                Button close = Button(panel, "X", Red, new Vector2(90f, 80f), onClose, 46);
                Pin((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-20f, -15f), new Vector2(90f, 80f));
            }

            body = Group(panel, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(30f, 30f);
            body.offsetMax = new Vector2(-30f, -120f);
            return backdrop;
        }

        /// <summary>A row of tab buttons along the top of an area; returns the buttons.</summary>
        public static Button[] Tabs(RectTransform parent, string[] names, UnityAction<int> onPick)
        {
            var buttons = new Button[names.Length];
            float width = (parent.rect.width > 1f ? parent.rect.width : 900f) / names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                buttons[i] = Button(parent, names[i], Gray, new Vector2(width - 12f, 80f), () => onPick(index), 36);
                var rect = (RectTransform)buttons[i].transform;
                rect.anchorMin = new Vector2((float)i / names.Length, 1f);
                rect.anchorMax = new Vector2((float)(i + 1) / names.Length, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(6f, -80f);
                rect.offsetMax = new Vector2(-6f, 0f);
            }
            return buttons;
        }

        /// <summary>A pearl: a pale gold disc with a shine, for prices and the pearl counter.</summary>
        public static RectTransform PearlIcon(Transform parent, float size)
        {
            RectTransform pearl = Box(parent, "Pearl", new Color(1f, 0.88f, 0.55f), Circle);
            pearl.sizeDelta = new Vector2(size, size);
            pearl.GetComponent<Image>().raycastTarget = false;
            RectTransform shine = Box(pearl, "Shine", new Color(1f, 1f, 1f, 0.85f), Circle);
            Pin(shine, new Vector2(0.32f, 0.68f), Vector2.zero, new Vector2(size * 0.3f, size * 0.3f));
            shine.pivot = new Vector2(0.5f, 0.5f);
            shine.GetComponent<Image>().raycastTarget = false;
            return pearl;
        }

        // A white rounded square (stretches without blurring its corners) or a disc.
        private static Sprite MakeSprite(int size, float radius, bool sliced)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha;
                    if (sliced)
                    {
                        float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                        float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                        alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    }
                    else
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                        alpha = Mathf.Clamp01(radius - d + 0.5f);
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Vector4 border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
