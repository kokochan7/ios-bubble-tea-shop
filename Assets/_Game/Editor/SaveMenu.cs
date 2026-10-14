using System;
using System.IO;
using BubbleTea.Economy;
using BubbleTea.Saving;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Menu items for the save file, so you can start over without hunting for it on disk.
    /// </summary>
    public static class SaveMenu
    {
        [MenuItem("Bubble Tea/Delete Save Data")]
        public static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play first",
                    "The game saves again when you stop playing, so stop Play mode and then delete the save.", "OK");
                return;
            }

            // Every shop has its own file; delete them all, and forget which shop was open last.
            string[] files = SaveManager.AllSaveFiles();
            foreach (string path in files) File.Delete(path);
            PlayerPrefs.DeleteKey(SaveManager.LastShopKey);
            PlayerPrefs.DeleteKey(SaveManager.TutorialDoneKey);
            bool existed = files.Length > 0;
            Debug.Log(existed ? "Bubble Tea: save deleted. The next Play starts a fresh game." : "Bubble Tea: there was no save to delete.");
        }

        [MenuItem("Bubble Tea/Show Save Files")]
        public static void ShowSave()
        {
            string[] files = SaveManager.AllSaveFiles();
            EditorUtility.RevealInFinder(files.Length > 0 ? files[0] : Application.persistentDataPath);
        }

        // Shows the "Welcome back!" pop-up without waiting, as if you'd been away for an hour.
        [MenuItem("Bubble Tea/Test Away Pop-Up (1 hour)")]
        public static void TestAway()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Press Play first", "This only works while the game is running.", "OK");
                return;
            }

#if UNITY_2023_1_OR_NEWER
            var earnings = UnityEngine.Object.FindFirstObjectByType<OfflineEarnings>();
#else
            var earnings = UnityEngine.Object.FindObjectOfType<OfflineEarnings>();
#endif
            if (earnings == null)
            {
                Debug.LogWarning("Bubble Tea: no OfflineEarnings in the scene. Rebuild it with Bubble Tea > Build Prototype Scene.");
                return;
            }
            if (earnings.RatePerMinute <= 0f)
            {
                Debug.Log("Bubble Tea: the shop hasn't earned anything yet. Sell drinks for a minute or two, then try again.");
                return;
            }
            earnings.Grant(TimeSpan.FromHours(1));
        }
    }
}
