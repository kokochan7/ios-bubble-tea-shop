using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.UI
{
    /// <summary>
    /// A banner near the top of the screen that says a customer left because a price was too
    /// high and suggests lowering it. Hides itself after a few seconds.
    /// </summary>
    public class PriceyAlert : MonoBehaviour
    {
        [SerializeField, Tooltip("The banner to show and hide (a child of this object).")]
        private GameObject banner;

        [SerializeField, Tooltip("Seconds the banner stays up.")]
        private float seconds = 5f;

        private float timer;

        private void Start()
        {
            if (banner != null) banner.SetActive(false);
        }

        private void OnEnable() => GameEvents.CustomerRefusedPrice += Show;
        private void OnDisable() => GameEvents.CustomerRefusedPrice -= Show;

        private void Show(Vector3 _)
        {
            timer = seconds;
            if (banner != null) banner.SetActive(true);
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f && banner != null) banner.SetActive(false);
        }
    }
}
