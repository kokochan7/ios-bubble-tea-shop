using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// Floats a green "+$15" above the counter for every sale, a gold "TIP!" when a happy
    /// customer tips, and a red "Too slow!" when one walks out, then fades them out.
    /// Labels are reused, so selling drinks never creates garbage.
    /// </summary>
    public class SalePopups : MonoBehaviour
    {
        [SerializeField, Tooltip("Seconds a label stays up.")]
        private float lifetime = 1f;

        [SerializeField, Tooltip("How far up a label floats, in meters.")]
        private float rise = 1.5f;

        [SerializeField, Tooltip("Color of sale amounts.")]
        private Color color = new(0.25f, 0.75f, 0.3f);

        [SerializeField, Tooltip("Color of the tip label.")]
        private Color tipColor = new(0.95f, 0.7f, 0.15f);

        [SerializeField, Tooltip("Color of the label when a customer walks out.")]
        private Color upsetColor = new(0.9f, 0.3f, 0.3f);

        private class Popup
        {
            public TextMesh Text;
            public Color Color;
            public Vector3 Start;
            public float Age;
        }

        private readonly List<Popup> live = new();
        private readonly Stack<Popup> spare = new();
        private Transform cameraTransform;

        private void OnEnable()
        {
            GameEvents.DrinkSold += OnSale;
            GameEvents.Tipped += OnTip;
            GameEvents.CustomerLeftUpset += OnUpset;
        }

        private void OnDisable()
        {
            GameEvents.DrinkSold -= OnSale;
            GameEvents.Tipped -= OnTip;
            GameEvents.CustomerLeftUpset -= OnUpset;
        }

        private void OnSale(Vector3 position, int amount) => Show(position, "+$" + amount.ToString("N0"), color, 1.6f);
        private void OnTip(Vector3 position, int amount) => Show(position, "TIP!", tipColor, 2.3f);
        private void OnUpset(Vector3 position) => Show(position, "Too slow!", upsetColor, 2.4f);

        private void Show(Vector3 position, string label, Color labelColor, float height)
        {
            Popup popup = spare.Count > 0 ? spare.Pop() : Create();
            popup.Text.text = label;
            popup.Color = labelColor;
            popup.Start = position + Vector3.up * height;
            popup.Age = 0f;
            popup.Text.gameObject.SetActive(true);
            live.Add(popup);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

            for (int i = live.Count - 1; i >= 0; i--)
            {
                Popup popup = live[i];
                popup.Age += Time.deltaTime;
                float t = popup.Age / Mathf.Max(lifetime, 0.01f);
                if (t >= 1f)
                {
                    popup.Text.gameObject.SetActive(false);
                    live.RemoveAt(i);
                    spare.Push(popup);
                    continue;
                }

                Transform label = popup.Text.transform;
                label.position = popup.Start + Vector3.up * (rise * t);
                if (cameraTransform != null) label.rotation = cameraTransform.rotation; // always face the camera
                Color faded = popup.Color;
                faded.a = 1f - t * t;
                popup.Text.color = faded;
            }
        }

        private Popup Create()
        {
            var go = new GameObject("Sale Popup");
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.fontSize = 64;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            return new Popup { Text = text };
        }
    }
}
