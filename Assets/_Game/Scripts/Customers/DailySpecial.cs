using System.Collections.Generic;
using BubbleTea.Cooking;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// Today's special, written on a chalkboard by the front door (Matcha House). Each shop day one
    /// drink the shop can make is picked: it sells for double and customers order it more often, so
    /// it pays to keep its bar stocked and staffed. The pick depends only on the day, so it stays
    /// the same after the game is reloaded.
    /// </summary>
    public class DailySpecial : MonoBehaviour
    {
        [SerializeField, Tooltip("Lane of the counter whose menu the special comes from.")]
        private string lane = "Main";

        [SerializeField, Min(1f), Tooltip("The special sells for this many times its normal price.")]
        private float priceFactor = 2f;

        [SerializeField, Min(1), Tooltip("How many times more often customers order the special.")]
        private int orderWeight = 3;

        [SerializeField, Tooltip("Text on the chalkboard showing the drink's name and price.")]
        private TextMesh drinkLabel;

        [SerializeField, Tooltip("Where a model of the drink stands on the chalkboard.")]
        private Transform iconSpot;

        [SerializeField, Tooltip("Size of the drink model on the chalkboard.")]
        private float iconScale = 2.4f;

        private static DailySpecial current;

        private readonly List<GameObject> icons = new();
        private readonly List<DrinkRecipe> shownRecipe = new();
        private readonly List<DrinkRecipe> choices = new();
        private DrinkRecipe recipe;
        private int day = -1;
        private float timer;

        /// <summary>Today's special drink, or null (no special in this shop, or nothing can be made yet).</summary>
        public static ItemDefinition Drink => current != null && current.recipe != null ? current.recipe.Drink : null;

        /// <summary>The price multiplier for this drink: 2 for today's special, 1 for everything else.</summary>
        public static float PriceFactorFor(ItemDefinition drink) =>
            drink != null && drink == Drink ? current.priceFactor : 1f;

        /// <summary>How likely customers are to pick this drink, compared with the others.</summary>
        public static int WeightFor(ItemDefinition drink) =>
            drink != null && drink == Drink ? current.orderWeight : 1;

        private void OnEnable() => current = this;

        private void OnDisable()
        {
            if (current == this) current = null;
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 1f;

            int today = GameClock.Instance != null ? GameClock.Instance.Day : 1;
            if (today != day || recipe == null) Pick(today);
            ShowPrice();
        }

        private void Pick(int today)
        {
            ServingCounter counter = ServingCounter.FindLane(lane);
            if (counter == null) return;
            choices.Clear();
            foreach (DrinkRecipe candidate in counter.Menu)
            {
                if (candidate != null && Station.AnyCanMake(candidate.Drink)) choices.Add(candidate);
            }
            if (choices.Count == 0) return;

            // The same day always picks the same drink (given the same bars).
            var random = new System.Random(today * 7919 + 17);
            DrinkRecipe picked = choices[random.Next(choices.Count)];
            bool newDay = day >= 0 && today != day;
            day = today;
            if (picked == recipe) return;
            recipe = picked;

            shownRecipe.Clear();
            shownRecipe.Add(recipe);
            DrinkIcons.Show(iconSpot, shownRecipe, iconScale, 0f, icons);
            if (newDay || Time.timeSinceLevelLoad > 5f)
            {
                GameEvents.RaiseAnnouncement($"Today's special: {recipe.Drink.DisplayName}!\nIt sells for {priceFactor:0.#}x and customers love it.",
                    new Color(0.25f, 0.5f, 0.3f));
            }
        }

        private void ShowPrice()
        {
            if (drinkLabel == null) return;
            ServingCounter counter = ServingCounter.FindLane(lane);
            string text = recipe == null || counter == null
                ? "Coming soon!"
                : $"{recipe.Drink.DisplayName}\n${counter.PriceOf(recipe)}  ({priceFactor:0.#}x)";
            if (drinkLabel.text != text) drinkLabel.text = text;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => current = null;
    }
}
