using System;
using System.Collections.Generic;
using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Economy;
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
    /// Three milestone goals that grow with the shop, with no randomness:
    ///  - sell drinks (10, 25, 50, 100, 200...),
    ///  - earn money from sales ($100, $250, $500, $1,000...),
    ///  - sell the newest drink on the menu (5, 15, 30, 60...), which moves on to each new drink as
    ///    the shop unlocks it.
    /// Progress is the shop's lifetime total from <see cref="ShopRecords"/>. Claiming a finished
    /// goal pays its bonus and sets the next milestone on the same track.
    /// (The class keeps its old name so saves and scenes still find it.)
    /// </summary>
    public class DailyGoals : MonoBehaviour
    {
        [SerializeField, Tooltip("Where rewards go.")]
        private Wallet wallet;

        [SerializeField, Tooltip("Every drink on the menu, in unlock order. The drink goal follows the newest one the shop can make.")]
        private DrinkRecipe[] menu;

        [SerializeField, Tooltip("Kept for older scenes; no longer used.")]
        private OfflineEarnings earnings;

        [SerializeField, Min(0.1f), Tooltip("Multiplies every reward.")]
        private float rewardMultiplier = 1f;

        private static readonly int[] DrinkMilestones = { 10, 25, 50, 100, 200, 350, 500, 750, 1000 };
        private static readonly int[] MoneyMilestones = { 100, 250, 500, 1000, 2500, 5000, 10000, 20000, 40000 };
        private static readonly int[] OneDrinkMilestones = { 5, 15, 30, 60, 100, 150, 250 };
        private static readonly GoalKind[] Tracks = { GoalKind.SellAny, GoalKind.EarnMoney, GoalKind.SellDrink };

        private readonly List<GoalState> goals = new();
        private string day;

        public IReadOnlyList<GoalState> Goals => goals;

        /// <summary>Kept for older saves.</summary>
        public string Day => day;

        /// <summary>True if any goal is finished and waiting to be claimed.</summary>
        public bool HasClaimable => goals.Exists(g => g.IsComplete && !g.claimed);

        /// <summary>Raised whenever progress, claims or the goal list change.</summary>
        public event Action Changed;

        /// <summary>Raised the moment a goal is finished.</summary>
        public event Action<GoalState> Completed;

        private void OnEnable() => ShopRecords.Changed += UpdateProgress;
        private void OnDisable() => ShopRecords.Changed -= UpdateProgress;

        /// <summary>Puts back goals from the save file (only milestone goals; anything else is replaced).</summary>
        public void Restore(string savedDay, List<GoalState> saved)
        {
            day = savedDay;
            goals.Clear();
            if (saved != null)
            {
                foreach (GoalKind kind in Tracks)
                {
                    GoalState goal = saved.Find(g => g != null && g.kind == kind && g.target > 0 && !g.claimed);
                    if (goal != null) goals.Add(goal);
                }
            }
            Changed?.Invoke();
        }

        /// <summary>Makes sure every track has a goal. Call after the save is loaded.</summary>
        public void EnsureGoals()
        {
            goals.RemoveAll(g => g.claimed);
            for (int i = 0; i < Tracks.Length; i++)
            {
                if (goals.Exists(g => g.kind == Tracks[i])) continue;
                GoalState goal = MakeGoal(Tracks[i], 0);
                if (goal != null) goals.Insert(Mathf.Min(i, goals.Count), goal);
            }
            UpdateProgress();
            Changed?.Invoke();
        }

        /// <summary>Pays the reward for a finished goal and sets the next milestone on its track.</summary>
        public bool Claim(int index)
        {
            if (index < 0 || index >= goals.Count) return false;
            GoalState goal = goals[index];
            if (!goal.IsComplete || goal.claimed) return false;

            goal.claimed = true;
            if (wallet != null) wallet.Add(goal.reward);
            GameEvents.RaiseBonusCollected();

            goals.RemoveAt(index);
            GoalState next = MakeGoal(goal.kind, goal.target);
            if (next != null) goals.Insert(index, next);
            UpdateProgress();
            Changed?.Invoke();
            return true;
        }

        // Progress is the lifetime total, so it's always right after loading too.
        private void UpdateProgress()
        {
            bool changed = false;
            foreach (GoalState goal in goals)
            {
                bool wasComplete = goal.IsComplete;
                int progress = Mathf.Min(goal.target, Current(goal.kind, goal.item));
                if (progress == goal.progress) continue;
                goal.progress = progress;
                changed = true;
                if (!wasComplete && goal.IsComplete) Completed?.Invoke(goal);
            }
            // A newer drink just unlocked: an unfinished drink goal moves on to it (or the drink
            // goal appears, once the first drink can be made).
            DrinkRecipe newest = NewestDrink();
            if (newest != null && !goals.Exists(g => g.kind == GoalKind.SellDrink))
            {
                GoalState first = MakeGoal(GoalKind.SellDrink, 0);
                if (first != null)
                {
                    goals.Add(first);
                    changed = true;
                }
            }
            GoalState drinkGoal = goals.Find(g => g.kind == GoalKind.SellDrink && !g.IsComplete);
            if (drinkGoal != null && newest != null && drinkGoal.item != newest.Drink.name)
            {
                int at = goals.IndexOf(drinkGoal);
                GoalState replacement = MakeGoal(GoalKind.SellDrink, 0);
                if (replacement != null)
                {
                    goals[at] = replacement;
                    replacement.progress = Mathf.Min(replacement.target, Current(replacement.kind, replacement.item));
                    changed = true;
                }
            }
            if (changed) Changed?.Invoke();
        }

        private static int Current(GoalKind kind, string item)
        {
            ShopRecords records = ShopRecords.Instance;
            if (records == null) return 0;
            return kind switch
            {
                GoalKind.SellAny => records.DrinksSold,
                GoalKind.EarnMoney => records.MoneyEarned,
                GoalKind.SellDrink => records.SoldOf(item),
                _ => 0,
            };
        }

        // The next milestone on a track that is above both the last claimed one and the current total.
        private GoalState MakeGoal(GoalKind kind, int lastTarget)
        {
            var goal = new GoalState { kind = kind };
            switch (kind)
            {
                case GoalKind.SellAny:
                    goal.target = NextMilestone(DrinkMilestones, 500, Mathf.Max(lastTarget, Current(kind, null)));
                    goal.reward = Reward(Shrink(goal.target) * AveragePrice() * 0.4f);
                    goal.label = $"Sell {goal.target:N0} drinks";
                    break;
                case GoalKind.EarnMoney:
                    goal.target = NextMilestone(MoneyMilestones, -2, Mathf.Max(lastTarget, Current(kind, null)));
                    goal.reward = Reward(Shrink(goal.target) * 0.3f);
                    goal.label = $"Earn ${goal.target:N0} from sales";
                    break;
                case GoalKind.SellDrink:
                    DrinkRecipe recipe = NewestDrink();
                    if (recipe == null) return null;
                    goal.item = recipe.Drink.name;
                    goal.target = NextMilestone(OneDrinkMilestones, 150, Current(kind, goal.item));
                    goal.reward = Reward(Shrink(goal.target) * recipe.Price * 0.5f);
                    goal.label = $"Sell {goal.target:N0} {recipe.Drink.DisplayName}";
                    break;
                default:
                    return null;
            }
            return goal;
        }

        // The first milestone above the value. Past the end of the list, milestones keep growing:
        // by `step` each time, or (negative step) doubling.
        private static int NextMilestone(int[] milestones, int step, int above)
        {
            foreach (int milestone in milestones)
            {
                if (milestone > above) return milestone;
            }
            long next = milestones[milestones.Length - 1];
            while (next <= above) next = step < 0 ? next * 2 : next + step;
            return (int)Mathf.Min(next, int.MaxValue / 2);
        }

        // The last drink on the menu that the shop can make right now.
        private DrinkRecipe NewestDrink()
        {
            if (menu == null) return null;
            for (int i = menu.Length - 1; i >= 0; i--)
            {
                DrinkRecipe recipe = menu[i];
                if (recipe != null && recipe.Drink != null && Station.AnyCanMake(recipe.Drink)) return recipe;
            }
            return null;
        }

        private float AveragePrice()
        {
            float total = 0f;
            int count = 0;
            if (menu != null)
            {
                foreach (DrinkRecipe recipe in menu)
                {
                    if (recipe == null || recipe.Drink == null || !Station.AnyCanMake(recipe.Drink)) continue;
                    total += recipe.Price;
                    count++;
                }
            }
            return count > 0 ? total / count : 10f;
        }

        // Rewards grow slower than the milestones, so later goals don't pay out huge sums
        // (a target 10 times bigger pays about 5.6 times more).
        private static float Shrink(int target) => Mathf.Pow(Mathf.Max(1, target), 0.75f);

        private int Reward(float amount) => Mathf.Max(25, RoundTo(amount * rewardMultiplier, 5));

        private static int RoundTo(float value, int step) => Mathf.Max(step, Mathf.RoundToInt(value / step) * step);
    }
}
