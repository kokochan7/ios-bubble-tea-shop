using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Upgrades
{
    /// <summary>
    /// One upgrade the player can buy several times, like "Walk Speed". Each level has a
    /// price and a value for its stat. Create new ones with
    /// right-click > Create > Bubble Tea > Upgrade.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Tea/Upgrade", fileName = "NewUpgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        [SerializeField, Tooltip("Name shown in the upgrade panel.")]
        private string displayName = "Upgrade";

        [SerializeField, Tooltip("Which game number this upgrade changes.")]
        private StatId stat;

        [SerializeField, Tooltip("The stat's value at each level. The first entry is level 0 (before buying anything).")]
        private float[] values = { 1f, 1.25f, 1.5f };

        [SerializeField, Tooltip("Price of each level. The first entry buys level 1, the second level 2, and so on.")]
        private int[] costs = { 50, 100 };

        public string DisplayName => displayName;
        public StatId Stat => stat;

        /// <summary>Highest level you can buy (limited by whichever list is shorter).</summary>
        public int MaxLevel => Mathf.Max(0, Mathf.Min(values != null ? values.Length - 1 : 0, costs != null ? costs.Length : 0));

        public float ValueAt(int level)
        {
            if (values == null || values.Length == 0) return 0f;
            return values[Mathf.Clamp(level, 0, values.Length - 1)];
        }

        /// <summary>Price to go from <paramref name="currentLevel"/> to the next level, or -1 if it's maxed out.</summary>
        public int CostToUpgradeFrom(int currentLevel)
        {
            if (currentLevel < 0 || currentLevel >= MaxLevel) return -1;
            return costs[currentLevel];
        }
    }
}
