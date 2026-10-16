using System;
using System.Collections.Generic;
using System.IO;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Extras
{
    /// <summary>
    /// What the player owns across every shop: pearls (the second currency, earned from daily goals,
    /// happy VIPs, mini-games and pets), the decor they've collected, and their pets and outfits.
    /// Kept in its own file (save-profile.json) so every shop shares it; starting the whole game over
    /// deletes it with the other saves.
    /// </summary>
    public static class Profile
    {
        [Serializable]
        private class Data
        {
            public int pearls;
            public List<string> decor = new();
            public List<string> pets = new();
            public List<string> outfits = new();
            public string pet = "";
            public string outfit = "";
            public long nextMiniGameUnix;
        }

        private static Data data;

        /// <summary>Raised when pearls or anything owned changes.</summary>
        public static event Action Changed;

        private static string PathOnDisk => Path.Combine(Application.persistentDataPath, "save-profile.json");

        private static Data D
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static int Pearls => D.pearls;
        public static string ActivePet => D.pet;
        public static string ActiveOutfit => D.outfit;

        /// <summary>When the next mini-game can be played (seconds since 1970, UTC).</summary>
        public static long NextMiniGameUnix
        {
            get => D.nextMiniGameUnix;
            set
            {
                D.nextMiniGameUnix = value;
                Save();
            }
        }

        public static bool OwnsDecor(string id) => D.decor.Contains(id);
        public static bool OwnsPet(string id) => D.pets.Contains(id);
        public static bool OwnsOutfit(string id) => D.outfits.Contains(id);

        /// <summary>Gives pearls, with a pop-up saying where they came from.</summary>
        public static void AddPearls(int amount, string why)
        {
            if (amount <= 0) return;
            D.pearls += amount;
            Save();
            if (!string.IsNullOrEmpty(why))
                GameEvents.RaiseAnnouncement($"+{amount} pearl{(amount == 1 ? "" : "s")}! {why}", new Color(0.95f, 0.75f, 0.25f));
        }

        /// <summary>Spends pearls if there are enough. False (and nothing spent) otherwise.</summary>
        public static bool TrySpend(int amount)
        {
            if (amount < 0 || D.pearls < amount) return false;
            D.pearls -= amount;
            Save();
            return true;
        }

        public static void AddDecor(string id) => Add(D.decor, id);
        public static void AddPet(string id) => Add(D.pets, id);
        public static void AddOutfit(string id) => Add(D.outfits, id);

        public static void SetPet(string id)
        {
            D.pet = id ?? "";
            Save();
        }

        public static void SetOutfit(string id)
        {
            D.outfit = id ?? "";
            Save();
        }

        private static void Add(List<string> list, string id)
        {
            if (string.IsNullOrEmpty(id) || list.Contains(id)) return;
            list.Add(id);
            Save();
        }

        private static void Load()
        {
            data = null;
            try
            {
                if (File.Exists(PathOnDisk)) data = JsonUtility.FromJson<Data>(File.ReadAllText(PathOnDisk));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bubble Tea: couldn't read the profile. " + e.Message);
            }
            data ??= new Data();
        }

        private static void Save()
        {
            try
            {
                File.WriteAllText(PathOnDisk, JsonUtility.ToJson(D, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Bubble Tea: couldn't save the profile. " + e.Message);
            }
            Changed?.Invoke();
        }

        // The project has domain reload turned off: read the file again each Play (it may have been deleted).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            data = null;
            Changed = null;
        }

        /// <summary>Forgets what's in memory (after the save files were deleted).</summary>
        public static void Forget() => data = null;
    }
}
