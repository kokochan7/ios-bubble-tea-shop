using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// A line of text floating over something in the shop ("MATCHA 14/20", "STAND HERE TO WHISK!"),
    /// drawn with a dark outline like the name tags and turned to face the camera. It isn't a
    /// child of what it follows, so it keeps its size while that grows in or turns.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private Transform follow;
        private Vector3 offset;
        private TextMesh main;
        private TextMesh[] layers;
        private string shown;
        private Color shownColor;

        /// <summary>Makes a hidden label that floats <paramref name="worldOffset"/> away from <paramref name="target"/>.</summary>
        public static FloatingText Create(Transform target, Vector3 worldOffset, float characterSize = 0.045f)
        {
            var go = new GameObject("Floating Text");
            var label = go.AddComponent<FloatingText>();
            label.follow = target;
            label.offset = worldOffset;
            label.Build(characterSize);
            go.SetActive(false);
            return label;
        }

        private void Build(float characterSize)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var outline = new Color(0.18f, 0.1f, 0.08f, 1f);
            layers = new TextMesh[9];
            float o = characterSize * 0.55f;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                layers[i] = MakeText(font, outline, new Vector3(Mathf.Cos(angle) * o, Mathf.Sin(angle) * o, 0.01f), characterSize, 0);
            }
            main = MakeText(font, Color.white, Vector3.zero, characterSize, 1);
            layers[8] = main;
        }

        private TextMesh MakeText(Font font, Color color, Vector3 localOffset, float characterSize, int sortingOrder)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset;
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = font;
            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = font.material;
            meshRenderer.sortingOrder = sortingOrder;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            return mesh;
        }

        /// <summary>Shows the label with this text and color.</summary>
        public void Show(string text, Color color)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (text == shown && color == shownColor) return;
            shown = text;
            shownColor = color;
            foreach (TextMesh layer in layers) layer.text = text;
            main.color = color;
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (follow == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = follow.position + offset;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }
}
