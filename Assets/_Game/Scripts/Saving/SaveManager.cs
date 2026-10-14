using System;
using System.Collections.Generic;
using System.IO;
using BubbleTea.Building;
using BubbleTea.Economy;
using BubbleTea.Goals;
using BubbleTea.Rating;
using BubbleTea.Upgrades;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BubbleTea.Saving
{
    /// <summary>
    /// Loads the player's progress when the scene starts and saves it as they play:
    /// right away when a station unlocks, every few seconds while money changes, and
    /// when the app goes to the background (on iPhone that's the last chance before
    /// the app may be closed).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        /// <summary>PlayerPrefs key holding the scene of the shop the player was last in.</summary>
        public const string LastShopKey = "lastShop";

        [SerializeField, Tooltip("Save file name. Each shop has its own, so their progress stays apart.")]
        private string fileName = "save.json";

        [SerializeField, Tooltip("The player's wallet.")]
        private Wallet wallet;

        [SerializeField, Tooltip("Every unlock pad in the scene, including hidden ones.")]
        private UnlockPad[] pads;

        [SerializeField, Tooltip("The upgrade manager. Leave empty if the scene has no upgrades.")]
        private UpgradeManager upgradeManager;

        [SerializeField, Tooltip("Pays for time away. Leave empty to turn that off.")]
        private OfflineEarnings offlineEarnings;

        [SerializeField, Tooltip("Today's goals. Leave empty if the scene has none.")]
        private DailyGoals dailyGoals;

        [SerializeField, Tooltip("The shop's star rating. Leave empty if the scene has none.")]
        private ShopRating rating;

        [SerializeField, Tooltip("Seconds between saves while money keeps changing.")]
        private float autosaveSeconds = 5f;

        [SerializeField, Tooltip("Untick to always start a fresh game (handy while testing). Progress is still saved.")]
        private bool loadOnStart = true;

        private bool loaded;
        private bool resetting;
        private bool dirty;
        private float timer;

        /// <summary>Full path of this shop's save file on this device.</summary>
        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);
        private string TempPath => SavePath + ".tmp";

        /// <summary>Every shop's save file on this device (save.json, save-shop2.json, and their .tmp copies).</summary>
        public static string[] AllSaveFiles()
        {
            try
            {
                return Directory.GetFiles(Application.persistentDataPath, "save*.json*");
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        private void OnEnable()
        {
            if (wallet != null) wallet.MoneyChanged += OnMoneyChanged;
            if (upgradeManager != null) upgradeManager.Changed += Save;
            if (dailyGoals != null) dailyGoals.Changed += MarkDirty;
            foreach (UnlockPad pad in pads)
            {
                if (pad == null) continue;
                pad.ProgressChanged += OnPadProgress;
                pad.Unlocked += OnPadUnlocked;
            }
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.MoneyChanged -= OnMoneyChanged;
            if (upgradeManager != null) upgradeManager.Changed -= Save;
            if (dailyGoals != null) dailyGoals.Changed -= MarkDirty;
            foreach (UnlockPad pad in pads)
            {
                if (pad == null) continue;
                pad.ProgressChanged -= OnPadProgress;
                pad.Unlocked -= OnPadUnlocked;
            }
        }

        // Start runs after every Awake and OnEnable, so the pads and HUD are ready to be updated.
        private void Start()
        {
            if (loadOnStart) Load();
            if (dailyGoals != null) dailyGoals.EnsureToday();
            loaded = true;
            dirty = false;
        }

        private void Update()
        {
            if (!dirty) return;
            timer += Time.deltaTime;
            if (timer >= autosaveSeconds) Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        private void OnMoneyChanged(int _) => dirty = true;
        private void MarkDirty() => dirty = true;
        private void OnPadProgress(int remaining, int cost) => dirty = true;
        private void OnPadUnlocked(UnlockPad _) => Save();

        /// <summary>Writes the current progress to disk.</summary>
        public void Save()
        {
            // Saving before the old file is loaded would overwrite it with a fresh game.
            if (!loaded || resetting) return;

            var data = new SaveData
            {
                // Money earned while away but not collected yet is kept, so closing the pop-up by quitting loses nothing.
                money = (wallet != null ? wallet.Money : 0) + (offlineEarnings != null ? offlineEarnings.Pending : 0),
                savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                earningsPerMinute = offlineEarnings != null ? offlineEarnings.RatePerMinute : 0f,
                goalsDay = dailyGoals != null ? dailyGoals.Day : null,
                ratingService = rating != null ? rating.Service : -1f,
            };
            if (dailyGoals != null) data.goals.AddRange(dailyGoals.Goals);
            foreach (UnlockPad pad in pads)
            {
                if (pad == null) continue;
                data.pads.Add(new PadSave { id = pad.SaveId, paid = pad.Paid, unlocked = pad.IsUnlocked });
            }
            if (upgradeManager != null)
            {
                foreach (UpgradeDefinition upgrade in upgradeManager.All)
                {
                    if (upgrade == null) continue;
                    data.upgrades.Add(new UpgradeSave { id = upgrade.name, level = upgradeManager.LevelOf(upgrade) });
                }
            }

            try
            {
                // Write to a temporary file first, so a crash mid-write can't corrupt the real save.
                File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(TempPath, SavePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bubble Tea: couldn't save the game. " + e.Message);
                return;
            }

            dirty = false;
            timer = 0f;
        }

        /// <summary>Deletes every shop's save and starts a brand-new game in the first shop.</summary>
        public void ResetProgress()
        {
            resetting = true;
            try
            {
                foreach (string path in AllSaveFiles()) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bubble Tea: couldn't delete the save. " + e.Message);
            }
            PlayerPrefs.DeleteKey(LastShopKey);
            BubbleTea.Core.Stats.Clear();
            SceneManager.LoadScene(0);
        }

        private void Load()
        {
            SaveData data = ReadFile(SavePath) ?? ReadFile(TempPath);
            if (data == null) return;

            if (wallet != null) wallet.SetMoney(data.money);

            var byId = new Dictionary<string, PadSave>();
            foreach (PadSave entry in data.pads)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id)) byId[entry.id] = entry;
            }

            var seen = new HashSet<string>();
            foreach (UnlockPad pad in pads)
            {
                if (pad == null) continue;
                if (!seen.Add(pad.SaveId))
                {
                    Debug.LogWarning($"Bubble Tea: two unlock pads share the save ID '{pad.SaveId}'. Give one a different Save Id.");
                }
                if (byId.TryGetValue(pad.SaveId, out PadSave entry)) pad.Restore(entry.paid, entry.unlocked);
            }

            if (upgradeManager != null && data.upgrades != null)
            {
                foreach (UpgradeSave entry in data.upgrades)
                {
                    if (entry == null) continue;
                    UpgradeDefinition upgrade = upgradeManager.Find(entry.id);
                    if (upgrade != null) upgradeManager.SetLevel(upgrade, entry.level);
                }
            }

            if (dailyGoals != null) dailyGoals.Restore(data.goalsDay, data.goals);
            if (rating != null && data.ratingService >= 0f) rating.Restore(data.ratingService);

            if (offlineEarnings != null && data.savedAtUnix > 0)
            {
                offlineEarnings.Restore(data.earningsPerMinute);
                long secondsAway = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - data.savedAtUnix;
                offlineEarnings.Grant(TimeSpan.FromSeconds(secondsAway));
            }
        }

        private static SaveData ReadFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Bubble Tea: couldn't read {path}, starting fresh. " + e.Message);
                return null;
            }
        }
    }
}
