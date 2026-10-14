using BubbleTea.Rating;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Five stars under the money counter, filled to the shop's rating (3.4 shows three and a
    /// bit), with the number beside them. The star picture is drawn in code, so no image file is needed.
    /// Tapping the stars shows how to raise the rating.
    /// </summary>
    public class RatingView : MonoBehaviour
    {
        [SerializeField] private ShopRating rating;

        [SerializeField, Tooltip("Gray star backgrounds, left to right.")]
        private Image[] emptyStars;

        [SerializeField, Tooltip("Gold stars on top, filled from left to right.")]
        private Image[] filledStars;

        [SerializeField] private Text valueLabel;

        [SerializeField, Tooltip("Tapping the stars shows this hint for a few seconds.")]
        private Button helpButton;

        [SerializeField] private Text helpLabel;

        private static Sprite starSprite;
        private float helpTimer;

        private void Awake()
        {
            Sprite sprite = StarSprite();
            foreach (Image star in emptyStars) if (star != null) star.sprite = sprite;
            foreach (Image star in filledStars)
            {
                if (star == null) continue;
                star.sprite = sprite;
                star.type = Image.Type.Filled;
                star.fillMethod = Image.FillMethod.Horizontal;
            }
            if (helpButton != null) helpButton.onClick.AddListener(() => helpTimer = 4f);
            if (helpLabel != null) helpLabel.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (rating == null) return;
            rating.Changed += Show;
            Show(rating.Stars);
        }

        private void OnDisable()
        {
            if (rating != null) rating.Changed -= Show;
        }

        private void Update()
        {
            if (helpLabel == null) return;
            helpTimer -= Time.deltaTime;
            helpLabel.gameObject.SetActive(helpTimer > 0f);
        }

        private void Show(float stars)
        {
            for (int i = 0; i < filledStars.Length; i++)
            {
                if (filledStars[i] != null) filledStars[i].fillAmount = Mathf.Clamp01(stars - i);
            }
            if (valueLabel != null) valueLabel.text = stars.ToString("0.0");
        }

        // A five-pointed star, drawn once into a small texture.
        private static Sprite StarSprite()
        {
            if (starSprite != null) return starSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? 0.5f : 0.21f;
                points[i] = new Vector2(0.5f + Mathf.Cos(angle) * radius, 0.48f + Mathf.Sin(angle) * radius);
            }

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 4x supersampling for smooth edges.
                    int hits = 0;
                    for (int s = 0; s < 4; s++)
                    {
                        var p = new Vector2((x + 0.25f + 0.5f * (s % 2)) / size, (y + 0.25f + 0.5f * (s / 2)) / size);
                        if (Inside(points, p)) hits++;
                    }
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 4));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            starSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return starSprite;
        }

        private static bool Inside(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                    p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }
    }
}
