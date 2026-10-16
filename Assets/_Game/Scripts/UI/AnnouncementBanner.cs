using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// A banner near the top of the screen for shop events: a tour bus on its way, a tea critic,
    /// today's special. Messages that arrive together are shown one after another.
    /// </summary>
    public class AnnouncementBanner : MonoBehaviour
    {
        [SerializeField, Tooltip("The banner to show and hide (a child of this object).")]
        private GameObject banner;

        [SerializeField, Tooltip("The banner's background, tinted to each message's color.")]
        private Image background;

        [SerializeField, Tooltip("The banner's text.")]
        private Text label;

        [SerializeField, Tooltip("Seconds each message stays up.")]
        private float seconds = 5f;

        private readonly Queue<(string message, Color color)> waiting = new();
        private float timer;

        private void Start()
        {
            if (banner != null) banner.SetActive(false);
        }

        private void OnEnable() => GameEvents.Announced += Queue;
        private void OnDisable() => GameEvents.Announced -= Queue;

        private void Queue(string message, Color color)
        {
            waiting.Enqueue((message, color));
            if (timer <= 0f) ShowNext();
        }

        private void ShowNext()
        {
            if (waiting.Count == 0)
            {
                if (banner != null) banner.SetActive(false);
                return;
            }
            (string message, Color color) = waiting.Dequeue();
            timer = seconds;
            if (label != null) label.text = message;
            if (background != null) background.color = new Color(color.r, color.g, color.b, 0.5f); // see-through (Kenny)
            if (banner != null) banner.SetActive(true);
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f) ShowNext();
        }
    }
}
