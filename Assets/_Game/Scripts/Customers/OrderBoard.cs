using System.Collections.Generic;
using System.Text;
using BubbleTea.Cooking;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// A high-tech screen over the counter showing the drinks the customer at the front of the line
    /// still wants, with the order written above the pictures (like "1x Milk Tea, 3x Taro Boba").
    /// Each drink disappears as it's handed over, and the screen hides when nobody is waiting. The
    /// bezel and neon rim stretch with the screen, and a light blinks.
    /// </summary>
    public class OrderBoard : MonoBehaviour
    {
        [SerializeField, Tooltip("The counter whose front customer is shown.")]
        private ServingCounter counter;

        [SerializeField, Tooltip("The sign to show and hide (turned to face the camera).")]
        private Transform board;

        [SerializeField, Tooltip("Where the drink models go, a child of the board.")]
        private Transform iconRow;

        [SerializeField, Tooltip("The sign's background, stretched to fit the drinks. Optional.")]
        private Transform backing;

        [SerializeField, Tooltip("Text above the pictures listing the order. Optional.")]
        private TextMesh label;

        [SerializeField, Tooltip("Header text along the top of the screen. Optional.")]
        private TextMesh header;

        [SerializeField, Tooltip("The frame around the screen, stretched with it. Optional.")]
        private Transform bezel;

        [SerializeField, Tooltip("The glowing outline behind the frame, stretched with it. Optional.")]
        private Transform rim;

        [SerializeField, Tooltip("A small blinking light in the frame's corner. Optional.")]
        private Transform statusLight;

        [SerializeField, Tooltip("Size of the drink models.")]
        private float iconScale = 2f;

        [SerializeField, Tooltip("Gap between drink models, in meters.")]
        private float iconSpacing = 0.55f;

        private readonly List<GameObject> icons = new();
        private Customer shown;
        private int shownCount = -1;

        private void LateUpdate()
        {
            if (counter == null || board == null) return;
            Customer front = counter.FrontCustomer;
            int count = front != null ? front.Orders.Count : 0;

            if (front != shown || count != shownCount)
            {
                shown = front;
                shownCount = count;
                if (count > 0) DrinkIcons.Show(iconRow, front.Orders, iconScale, iconSpacing, icons);
                else DrinkIcons.Clear(icons);
                board.gameObject.SetActive(count > 0);
                if (label != null) label.text = count > 0 ? Describe(front.Orders) : "";
                if (backing != null && count > 0) FitBacking(count);
            }

            if (count > 0 && Camera.main != null) board.rotation = Camera.main.transform.rotation;
            if (count > 0) Animate();
        }

        // The status light blinks.
        private void Animate()
        {
            if (statusLight != null) statusLight.gameObject.SetActive(Mathf.Repeat(Time.time, 1.2f) < 0.8f);
        }

        // Stretches the sign to fit the pictures and, above them, the lines of text.
        private void FitBacking(int count)
        {
            float width = count * iconSpacing + 0.3f;
            float height = 1.1f;
            if (label != null && label.text.Length > 0)
            {
                string[] lines = label.text.Split('\n');
                float lineHeight = label.fontSize * label.characterSize * 0.1f * label.lineSpacing;
                int longest = 0;
                foreach (string line in lines) longest = Mathf.Max(longest, line.Length);
                width = Mathf.Max(width, longest * lineHeight * 0.5f + 0.3f);
                height = label.transform.localPosition.y + lines.Length * lineHeight + 0.08f + BackingBottom;
            }
            if (header != null)
            {
                float headerHeight = header.fontSize * header.characterSize * 0.1f;
                header.transform.localPosition = new Vector3(0f, height - BackingBottom + 0.02f, header.transform.localPosition.z);
                width = Mathf.Max(width, header.text.Length * headerHeight * 0.55f + 0.3f);
                height += headerHeight + 0.06f;
            }
            Vector3 position = backing.localPosition;
            backing.localScale = new Vector3(width, height, backing.localScale.z);
            float middle = height * 0.5f - BackingBottom;
            backing.localPosition = new Vector3(position.x, middle, position.z);
            Frame(bezel, width + 0.1f, height + 0.1f, middle);
            Frame(rim, width + 0.16f, height + 0.16f, middle);
            if (statusLight != null)
                statusLight.localPosition = new Vector3(width * 0.5f + 0.02f, middle - height * 0.5f - 0.02f, statusLight.localPosition.z);
        }

        private static void Frame(Transform part, float width, float height, float middle)
        {
            if (part == null) return;
            part.localScale = new Vector3(width, height, part.localScale.z);
            part.localPosition = new Vector3(0f, middle, part.localPosition.z);
        }

        private const float BackingBottom = 0.13f; // how far the sign reaches below the pictures

        // "1x Milk Tea" with each kind on its own line.
        private static string Describe(IReadOnlyList<Cooking.DrinkRecipe> orders)
        {
            var kinds = new List<ItemDefinition>();
            var counts = new List<int>();
            foreach (Cooking.DrinkRecipe order in orders)
            {
                if (order == null || order.Drink == null) continue;
                int at = kinds.IndexOf(order.Drink);
                if (at >= 0) counts[at]++;
                else
                {
                    kinds.Add(order.Drink);
                    counts.Add(1);
                }
            }
            var text = new StringBuilder();
            for (int i = 0; i < kinds.Count; i++)
            {
                if (i > 0) text.Append('\n');
                text.Append(counts[i]).Append("x ").Append(kinds[i].DisplayName);
            }
            return text.ToString();
        }
    }
}
