using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// Gives a customer or member of staff a random look when they appear: man or woman, and
    /// random shirt, hair and skin colors. Sits on the character models from the Boba Art Kit.
    /// </summary>
    public class CharacterLook : MonoBehaviour
    {
        [SerializeField, Tooltip("Parts shown only on women (long hair, bow).")]
        private GameObject female;

        [SerializeField, Tooltip("Parts shown only on men (short hair).")]
        private GameObject male;

        [SerializeField, Tooltip("Body and sleeves, tinted with a random shirt color.")]
        private Renderer[] shirt;

        [SerializeField, Tooltip("Hair parts, tinted with a random hair color.")]
        private Renderer[] hair;

        [SerializeField, Tooltip("Head and hands, tinted with a random skin tone.")]
        private Renderer[] skin;

        [SerializeField]
        private Color[] shirtColors =
        {
            new(0.98f, 0.89f, 0.55f), new(0.98f, 0.65f, 0.70f), new(0.68f, 0.86f, 0.98f), new(0.70f, 0.90f, 0.72f),
            new(0.80f, 0.70f, 0.95f), new(1f, 0.72f, 0.50f), new(0.95f, 0.95f, 0.92f), new(0.45f, 0.55f, 0.80f),
            new(0.90f, 0.40f, 0.40f), new(0.40f, 0.70f, 0.65f),
        };

        [SerializeField]
        private Color[] hairColors =
        {
            new(0.29f, 0.20f, 0.15f), new(0.12f, 0.10f, 0.10f), new(0.55f, 0.33f, 0.18f), new(0.85f, 0.68f, 0.40f),
            new(0.62f, 0.25f, 0.18f), new(0.75f, 0.75f, 0.78f),
        };

        [SerializeField]
        private Color[] skinColors =
        {
            new(1f, 0.86f, 0.76f), new(0.96f, 0.78f, 0.64f), new(0.85f, 0.64f, 0.48f), new(0.66f, 0.47f, 0.34f),
            new(0.48f, 0.33f, 0.24f),
        };

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColor = Shader.PropertyToID("_Color");

        private void Awake()
        {
            bool isFemale = Random.value < 0.5f;
            if (female != null) female.SetActive(isFemale);
            if (male != null) male.SetActive(!isFemale);
            Tint(shirt, Pick(shirtColors));
            Tint(hair, Pick(hairColors));
            Tint(skin, Pick(skinColors));
        }

        private static Color Pick(Color[] colors) =>
            colors != null && colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;

        private static void Tint(Renderer[] renderers, Color color)
        {
            if (renderers == null) return;
            var block = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColor, color);
                block.SetColor(LegacyColor, color);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
