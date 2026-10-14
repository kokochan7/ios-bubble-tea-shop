using System;
using System.Collections.Generic;
using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Economy;
using BubbleTea.Upgrades;
using UnityEngine;

namespace BubbleTea.Goals
{
    /// <summary>What a daily goal asks for.</summary>
    public enum GoalKind
    {
        SellDrink,
        SellAny,
        Tips,
        EarnMoney,
        BuyUpgrades,
    }

    /// <summary>One daily goal and how far along it is. Saved as-is in the save file.</summary>
    [Serializable]
    public class GoalState
    {
        public GoalKind kind;

        /// <summary>For <see cref="GoalKind.SellDrink"/>: the drink's item asset name.</summary>
        public string item;

        public string label;
        public int target;
        public int progress;
        public int reward;
        public bool claimed;

        public bool IsComplete => progress >= target;
    }

    /// <summary>
    /// Three small goals a day ("Sell 20 Taro Milk Tea", "Get 8 tips"...) that pay a cash bonus.
    /// New goals arrive at local midnight; goals finished but not claimed by then are paid
    /// automatically. Targets and rewards grow as the shop unlocks more drinks.
    /// </summary>
    public class DailyGoals : MonoBehaviour
    {
        [SerializeField, Tooltip("Where rewards go.")]
        private Wallet wallet;

        [SerializeField, Tooltip("Every drink on the menu. Goals only ask for drinks the shop can make.")]
        private DrinkRecipe[] menu;

        [SerializeField, Tooltip("Used to size money goals to how fast the shop earns. Optional.")]
        private OfflineEarnings earnings;

        [SerializeField, Range(1, 5), Tooltip("Goals per day.")]
        private int goalsPerDay = 3;

        [SerializeField, Min(0.1f), Tooltip("Multiplies every reward.")]
        private float rewardMultiplier = 1f;

        private readonly List<GoalState> goals = new();
        private string day;
        private float dayCheckTimer;

        public IReadOnlyList<GoalState> Goals => goals;

        /// <summary>The day these goals belong to, as yyyy-MM-dd. Saved with the game.</summary>
        public string Day => day;

        /// <summary>True if any goal is finished and waiting to be claimed.</summary>
        public bool HasClaimable => goals.Exists(g => g.IsComplete && !g.claimed);

        /// <summary>Raised whenever progress, claims or the goal list change.</summary>
        public event Action Changed;

        /// <summary>Raised the moment a goal is finished.</summary>
        public event Action<GoalState> Completed;

        private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

        private void OnEnable()
        {
            GameEvents.DrinkServed += OnDrinkServed;
            GameEvents.DrinkSold += OnDrinkSold;
            GameEvents.Tipped += OnTipped;
            GameEvents.UpgradeBought += OnUpgradeBought;
        }

        private void OnDisable()
        {
            GameEvents.DrinkServed -= OnDrinkServed;
            GameEvents.DrinkSold -= OnDrinkSold;
            GameEvents.Tipped -= OnTipped;
            GameEvents.UpgradeBought -= OnUpgradeBought;
        }

        private void Update()
        {
            // Roll over to new goals at midnight, even if the game is left running.
            dayCheckTimer += Time.unscaledDeltaTime;
            if (dayCheckTimer < 30f) return;
            dayCheckTimer = 0f;
            EnsureToday();
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) EnsureToday();
        }

        private void OnDrinkServed(string itemId)
        {
            Advance(GoalKind.SellAny, null, 1);
            Advance(GoalKind.SellDrink, itemId, 1);
        }

        private void OnDrinkSold(Vector3 _, int amount) => Advance(GoalKind.EarnMoney, null, amount);
        private void OnTipped(Vector3 _, int amount) => Advance(GoalKind.Tips, null, 1);
        private void OnUpgradeBought() => Advance(GoalKind.BuyUpgrades, null, 1);

        /// <summary>Puts back goals from the save file.</summary>
        public void Restore(string savedDay, List<GoalState> saved)
        {
            day = savedDay;
            goals.Clear();
            if (saved != null) goals.AddRange(saved.FindAll(g => g != null && g.target > 0));
            Changed?.Invoke();
        }

        /// <summary>Makes new goals if there are none for today. Call after the save is loaded.</summary>
        public void EnsureToday()
        {
            if (day == Today && goals.Count > 0) return;

            // Yesterday's finished goals still pay out.
            foreach (GoalState goal in goals)
            {
                if (!goal.IsComplete || goal.claimed) continue;
                goal.claimed = true;
                if (wallet != null) wallet.Add(goal.reward);
            }

            day = Today;
            Generate();
            Changed?.Invoke();
        }

        /// <summary>Pays the reward for a finished goal.</summary>
        public bool Claim(int index)
        {
            if (index < 0 || index >= goals.Count) return false;
            GoalState goal = goals[index];
            if (!goal.IsComplete || goal.claimed) return false;

            goal.claimed = true;
            if (wallet != null) wallet.Add(goal.reward);
            GameEvents.RaiseBonusCollected();
            Changed?.Invoke();
            return true;
        }

        private void Advance(GoalKind kind, string item, int amount)
        {
            bool changed = false;
            foreach (GoalState goal in goals)
            {
                if (goal.kind != kind || goal.IsComplete) continue;
                if (kind == GoalKind.SellDrink && goal.item != item) continue;

                goal.progress = Mathf.Min(goal.target, goal.progress + amount);
                changed = true;
                if (goal.IsComplete) Completed?.Invoke(goal);
            }
            if (changed) Changed?.Invoke();
        }

        private void Generate()
        {
            goals.Clear();

            // The same day always rolls the same goals, so reinstalling doesn't reshuffle them.
            var rng = new System.Random(int.Parse(day.Replace("-", "")));

            var makeable = new List<DrinkRecipe>();
            if (menu != null)
            {
                foreach (DrinkRecipe recipe in menu)
                {
                    if (recipe != null && recipe.Drink != null && Station.AnyCanMake(recipe.Drink)) makeable.Add(recipe);
                }
            }
            int level = makeable.Count;
            float averagePrice = 10f;
            if (makeable.Count > 0)
            {
                averagePrice = 0f;
                foreach (DrinkRecipe recipe in makeable) averagePrice += recipe.Price;
                averagePrice /= makeable.Count;
            }

            var kinds = new List<GoalKind> { GoalKind.SellAny, GoalKind.Tips, GoalKind.EarnMoney };
            if (makeable.Count > 0) kinds.Add(GoalKind.SellDrink);
            if (HasUpgradeToBuy()) kinds.Add(GoalKind.BuyUpgrades);

            // A second "sell this drink" goal makes days with many drinks more interesting.
            if (makeable.Count >= 3) kinds.Add(GoalKind.SellDrink);

            while (goals.Count < goalsPerDay && kinds.Count > 0)
            {
                int pick = rng.Next(kinds.Count);
                GoalKind kind = kinds[pick];
                kinds.RemoveAt(pick);
                GoalState goal = MakeGoal(kind, rng, level, averagePrice, makeable);
                if (goal != null) goals.Add(goal);
            }
        }

        private GoalState MakeGoal(GoalKind kind, System.Random rng, int level, float averagePrice, List<DrinkRecipe> makeable)
        {
            var goal = new GoalState { kind = kind };
            switch (kind)
            {
                case GoalKind.SellDrink:
                    DrinkRecipe recipe = makeable[rng.Next(makeable.Count)];
                    if (goals.Exists(g => g.kind == GoalKind.SellDrink && g.item == recipe.Drink.name)) return null;
                    goal.item = recipe.Drink.name;
                    goal.target = 8 + 3 * level + rng.Next(5);
                    goal.reward = Reward(goal.target * recipe.Price * 1f);
                    goal.label = $"Sell {goal.target} {recipe.Drink.DisplayName}";
                    break;
                case GoalKind.SellAny:
                    goal.target = 15 + 5 * level + rng.Next(6);
                    goal.reward = Reward(goal.target * averagePrice * 0.6f);
                    goal.label = $"Sell {goal.target} drinks";
                    break;
                case GoalKind.Tips:
                    goal.target = 5 + 2 * level + rng.Next(4);
                    goal.reward = Reward(goal.target * averagePrice * 1.2f);
                    goal.label = $"Get {goal.target} tips";
                    break;
                case GoalKind.EarnMoney:
                    float rate = earnings != null ? earnings.RatePerMinute : 0f;
                    goal.target = RoundTo(Mathf.Max(150f + 100f * level, rate * 10f), 50);
                    goal.reward = Reward(goal.target * 0.4f);
                    goal.label = $"Earn ${goal.target:N0} from sales";
                    break;
                case GoalKind.BuyUpgrades:
                    goal.target = 1 + rng.Next(2);
                    goal.reward = Reward(100f + 60f * level * goal.target);
                    goal.label = goal.target == 1 ? "Buy an upgrade" : $"Buy {goal.target} upgrades";
                    break;
            }
            return goal;
        }

        private static bool HasUpgradeToBuy()
        {
            UpgradeManager manager = UpgradeManager.Instance;
            if (manager == null) return false;
            foreach (UpgradeDefinition upgrade in manager.All)
            {
                if (upgrade != null && manager.NextCost(upgrade) >= 0) return true;
            }
            return false;
        }

        private int Reward(float amount) => Mathf.Max(25, RoundTo(amount * rewardMultiplier, 5));

        private static int RoundTo(float value, int step) => Mathf.Max(step, Mathf.RoundToInt(value / step) * step);
    }
}
