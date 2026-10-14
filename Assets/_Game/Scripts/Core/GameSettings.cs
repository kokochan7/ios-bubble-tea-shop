using System;
using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Player preferences (sound and vibration on or off). Stored in PlayerPrefs, separate
    /// from the save file, so resetting progress keeps your settings.
    /// </summary>
    public static class GameSettings
    {
        private const string SoundKey = "BubbleTea.SoundOn";
        private const string VibrationKey = "BubbleTea.VibrationOn";

        /// <summary>Raised whenever a setting changes.</summary>
        public static event Action Changed;

        public static bool SoundOn
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set => Set(SoundKey, value);
        }

        public static bool VibrationOn
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set => Set(VibrationKey, value);
        }

        private static void Set(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Changed = null;
    }
}
