using System;
using System.Collections.Generic;
using System.IO;
using BubbleTea.Building;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
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

        /// <summary>The first shop's scene, where a new game starts.</summary>
        public const string FirstShopScene = "Prototype";

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

        [SerializeField, Tooltip("The goals. Leave empty if the scene has none.")]
        private DailyGoals dailyGoals;

        [SerializeField, Tooltip("The shop's star rating. Leave empty if the scene has none.")]
        private ShopRating rating;

        [SerializeField, Tooltip("The floor money that stops after a set amount. Leave empty if the scene has none.")]
        private CashPileRefiller floorCash;

        private const int StartingMoneyPaid = 500; // the old floor pile's total, now given at the start

        [SerializeField, Tooltip("Seconds between saves while money keeps changing.")]
        private float autosaveSeconds = 5f;

        [SerializeField, Tooltip("Untick to always start a fresh game (handy while testing). Progress is still saved.")]
        private bool loadOnStart = true;

        private bool loaded;
        private bool resetting;
        private bool dirty;
        private float timer;

        /// <summary>Every unlock pad in the scene, including hidden ones.</summary>
        public IReadOnlyList<UnlockPad> Pads => pads;

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
            PriceBook.Changed += MarkDirty;
            Movable.Moved += Save;
            BubbleTea.Extras.TeamSkills.Changed += MarkDirty;
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
            PriceBook.Changed -= MarkDirty;
            Movable.Moved -= Save;
            BubbleTea.Extras.TeamSkills.Changed -= MarkDirty;
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
            PriceBook.Clear(); // each shop has its own prices
            BubbleTea.Extras.TeamSkills.Restore(null); // and its own staff teams and collectible decor
            BubbleTea.Extras.Collections.Restore(null);
            BubbleTea.Extras.ExtrasHud.Ensure();
            if (loadOnStart) Load();
            if (dailyGoals != null) dailyGoals.EnsureGoals();
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
                ratingLost = rating != null ? rating.Lost : 0f,
                floorCashGiven = floorCash != null ? floorCash.Given : StartingMoneyPaid,
                prices = PriceBook.Snapshot(),
                clockMinutes = GameClock.Instance != null ? GameClock.Instance.MinuteOfDay : -1f,
                clockDay = GameClock.Instance != null ? GameClock.Instance.Day : 1,
            };
            if (ShopRecords.Instance != null) data.records = ShopRecords.Instance.Snapshot();
            data.team = BubbleTea.Extras.TeamSkills.Snapshot();
            data.cosmetics = BubbleTea.Extras.Collections.Snapshot();
            if (VipRegulars.Instance != null) data.vips = VipRegulars.Instance.Snapshot();
            if (dailyGoals != null) data.goals.AddRange(dailyGoals.Goals);
            foreach (Station station in Station.All)
            {
                if (station != null && station.UsesSupplies) data.supplies.Add(new SupplySave { station = station.name, amount = station.Supplies });
            }
            foreach (Movable movable in Movable.All)
            {
                if (movable == null || !movable.HasMoved) continue;
                Vector3 spot = movable.transform.position;
                data.placements.Add(new PlacementSave { id = movable.Id, x = spot.x, y = spot.y, z = spot.z, yaw = movable.transform.eulerAngles.y });
            }
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
            EraseSave();
            BubbleTea.Core.Stats.Clear();
            // Straight into the first shop (scene 0 is the main menu once that's built).
            if (Application.CanStreamedLevelBeLoaded(FirstShopScene)) SceneManager.LoadScene(FirstShopScene);
            else SceneManager.LoadScene(0);
        }

        /// <summary>True in the first shop (the Boba Shop), where a new game starts.</summary>
        public static bool InFirstShop => SceneManager.GetActiveScene().name == FirstShopScene;

        /// <summary>
        /// Deletes only this shop's save and stops saving it, without leaving the scene (losing a
        /// later shop starts that shop over; the shops before it stay as they were).
        /// </summary>
        public void EraseThisShop()
        {
            resetting = true;
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(TempPath)) File.Delete(TempPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bubble Tea: couldn't delete this shop's save. " + e.Message);
            }
        }

        /// <summary>Deletes this shop's save and starts this shop over from scratch.</summary>
        public void RestartThisShop()
        {
            EraseThisShop();
            BubbleTea.Core.Stats.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Deletes every shop's save and stops saving, without leaving the scene (the game-over
        /// screen does this straight away, so quitting there can't keep a lost game).
        /// </summary>
        public void EraseSave()
        {
            resetting = true;
            try
            {
                foreach (string path in AllSaveFiles()) File.Delete(path); // pearls and collections (save-profile.json) too
                BubbleTea.Extras.Profile.Forget();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bubble Tea: couldn't delete the save. " + e.Message);
            }
            PlayerPrefs.DeleteKey(LastShopKey);
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

            // Collectible decor comes back out, then everything the player moved goes back where they put it.
            BubbleTea.Extras.Collections.Restore(data.cosmetics);
            BubbleTea.Extras.TeamSkills.Restore(data.team);
            if (data.placements != null)
            {
                foreach (PlacementSave entry in data.placements)
                {
                    Movable movable = entry != null ? Movable.Find(entry.id) : null;
                    if (movable != null) movable.Place(new Vector3(entry.x, entry.y, entry.z), entry.yaw, byPlayer: false);
                }
            }

            // Bars built by the pads above exist now, so their supplies can go back in.
            if (data.supplies != null)
            {
                foreach (SupplySave entry in data.supplies)
                {
                    if (entry == null) continue;
                    foreach (Station station in Station.All)
                    {
                        if (station != null && station.UsesSupplies && station.name == entry.station) station.RestoreSupplies(entry.amount);
                    }
                }
            }

            if (ShopRecords.Instance != null) ShopRecords.Instance.Restore(data.records);
            if (VipRegulars.Instance != null) VipRegulars.Instance.Restore(data.vips);
            if (dailyGoals != null) dailyGoals.Restore(data.goalsDay, data.goals);
            if (rating != null && data.ratingService >= 0f) rating.Restore(data.ratingService, data.ratingLost);
            if (floorCash != null) floorCash.Restore(data.floorCashGiven);
            // The floor money pile is gone (shops now just start with $500); a save from before
            // that hadn't picked it all up gets the rest straight into the wallet, once.
            else if (wallet != null && data.floorCashGiven < StartingMoneyPaid)
            {
                wallet.Add(StartingMoneyPaid - Mathf.Max(0, data.floorCashGiven));
                MarkDirty();
            }
            PriceBook.Restore(data.prices);
            if (GameClock.Instance != null && data.clockMinutes >= 0f) GameClock.Instance.Restore(data.clockMinutes, data.clockDay);

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
