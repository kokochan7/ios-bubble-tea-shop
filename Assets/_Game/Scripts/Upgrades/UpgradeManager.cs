using System;
using System.Collections.Generic;
using BubbleTea.Core;
using BubbleTea.Economy;
using UnityEngine;

namespace BubbleTea.Upgrades
{
    /// <summary>
    /// Keeps track of each upgrade's level, sells the next level for money from the
    /// wallet, and publishes the resulting values to <see cref="Stats"/>.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The player's wallet. Upgrades are paid from it.")]
        private Wallet wallet;

        [SerializeField, Tooltip("Every upgrade the player can buy.")]
        private UpgradeDefinition[] upgrades;

        private readonly Dictionary<UpgradeDefinition, int> levels = new();

        /// <summary>The manager in the current scene, so prefabs spawned later (like the upgrade desk) can find it.</summary>
        public static UpgradeManager Instance { get; private set; }

        /// <summary>Raised whenever any upgrade's level changes.</summary>
        public event Action Changed;

        public Wallet Wallet => wallet;
        public IReadOnlyList<UpgradeDefinition> All => upgrades ?? Array.Empty<UpgradeDefinition>();

        private void OnEnable()
        {
            Instance = this;
            foreach (UpgradeDefinition upgrade in All)
            {
                if (upgrade != null) Apply(upgrade);
            }
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        public int LevelOf(UpgradeDefinition upgrade) =>
            upgrade != null && levels.TryGetValue(upgrade, out int level) ? level : 0;

        /// <summary>The level of the upgrade that changes this stat in the current shop (0 if none).</summary>
        public static int LevelOfStat(StatId stat)
        {
            if (Instance == null) return 0;
            foreach (UpgradeDefinition upgrade in Instance.All)
            {
                if (upgrade != null && upgrade.Stat == stat) return Instance.LevelOf(upgrade);
            }
            return 0;
        }

        /// <summary>Price of the next level, or -1 if the upgrade is maxed out.</summary>
        public int NextCost(UpgradeDefinition upgrade) =>
            upgrade != null ? upgrade.CostToUpgradeFrom(LevelOf(upgrade)) : -1;

        /// <summary>Buys the next level if the player can afford it.</summary>
        public bool TryBuy(UpgradeDefinition upgrade)
        {
            int cost = NextCost(upgrade);
            if (cost < 0 || wallet == null || !wallet.TrySpend(cost)) return false;
            SetLevel(upgrade, LevelOf(upgrade) + 1);
            GameEvents.RaiseUpgradeBought();
            return true;
        }

        /// <summary>Sets a level directly, without paying. Used when loading a saved game.</summary>
        public void SetLevel(UpgradeDefinition upgrade, int level)
        {
            if (upgrade == null) return;
            levels[upgrade] = Mathf.Clamp(level, 0, upgrade.MaxLevel);
            Apply(upgrade);
            Changed?.Invoke();
        }

        /// <summary>Finds an upgrade by its asset name (what the save file stores).</summary>
        public UpgradeDefinition Find(string id)
        {
            foreach (UpgradeDefinition upgrade in All)
            {
                if (upgrade != null && upgrade.name == id) return upgrade;
            }
            return null;
        }

        private void Apply(UpgradeDefinition upgrade) => Stats.Set(upgrade.Stat, upgrade.ValueAt(LevelOf(upgrade)));
    }
}
