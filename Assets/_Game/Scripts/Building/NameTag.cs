using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// A name floating above something in the shop ("Counter", "Tea Brewer", "Barista"...),
    /// turned to face the camera. Follows its object, so staff carry theirs around.
    /// </summary>
    public class NameTag : MonoBehaviour
    {
        private const float CharacterSize = 0.045f;

        [SerializeField] private string text;
        [SerializeField, Tooltip("Meters above the object's position.")]
        private float height = 2f;

        private Transform label;

        /// <summary>Puts a name label on an object, just above its top.</summary>
        public static NameTag Add(GameObject target, string text)
        {
            if (target == null || string.IsNullOrEmpty(text)) return null;
            float top = 1.2f;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                top = bounds.max.y - target.transform.position.y;
            }
            var nameTag = target.AddComponent<NameTag>();
            nameTag.text = text;
            nameTag.height = Mathf.Max(top, 0.5f) + 0.45f;
            return nameTag;
        }

        /// <summary>The name shown for a pad's buildable: "Hire Barista 3" shows as "Barista".</summary>
        public static string NameFor(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return displayName;
            string name = displayName.StartsWith("Hire ") ? displayName.Substring(5) : displayName;
            int space = name.LastIndexOf(' ');
            if (space > 0 && int.TryParse(name.Substring(space + 1), out _)) name = name.Substring(0, space);
            return name;
        }

        private void Start()
        {
            label = new GameObject("Name Tag").transform;
            label.SetParent(transform, false);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // A dark copy just behind the white text, so it reads on any floor.
            MakeText(label, font, new Color(0.25f, 0.17f, 0.13f, 0.85f), new Vector3(0.025f, -0.025f, 0.01f));
            MakeText(label, font, Color.white, Vector3.zero);
        }

        private void MakeText(Transform parent, Font font, Color color, Vector3 offset)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            mesh.characterSize = CharacterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
        }

        private void LateUpdate()
        {
            if (label == null) return;
            // World position and size stay fixed while the object turns, grows in or walks around.
            label.position = transform.position + Vector3.up * height;
            Vector3 parentScale = transform.lossyScale;
            label.localScale = new Vector3(SafeInverse(parentScale.x), SafeInverse(parentScale.y), SafeInverse(parentScale.z));
            if (Camera.main != null) label.rotation = Camera.main.transform.rotation;
        }

        private static float SafeInverse(float value) => Mathf.Abs(value) > 0.001f ? 1f / value : 1f;
    }
}
