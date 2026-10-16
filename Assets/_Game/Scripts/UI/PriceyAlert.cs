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
        private string drinkName;

        private void Start()
        {
            if (banner != null) banner.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.DrinkTooPricey += Remember;
            GameEvents.CustomerRefusedPrice += Show;
        }

        private void OnDisable()
        {
            GameEvents.DrinkTooPricey -= Remember;
            GameEvents.CustomerRefusedPrice -= Show;
        }

        private void Remember(string drink) => drinkName = drink;

        private void Show(Vector3 _)
        {
            timer = seconds;
            UnityEngine.UI.Text text = banner != null ? banner.GetComponentInChildren<UnityEngine.UI.Text>(true) : null;
            if (text != null)
            {
                text.text = string.IsNullOrEmpty(drinkName)
                    ? "A customer left: too pricey! (-0.5 stars)\nConsider lowering your prices."
                    : $"A customer left: {drinkName} is too pricey! (-0.5 stars)\nConsider lowering its price.";
            }
            drinkName = null;
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
