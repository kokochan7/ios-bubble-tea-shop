using System.IO;
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

            string path = SaveManager.SavePath;
            bool existed = File.Exists(path) || File.Exists(path + ".tmp");
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            Debug.Log(existed ? "Bubble Tea: save deleted. The next Play starts a fresh game." : "Bubble Tea: there was no save to delete.");
        }

        [MenuItem("Bubble Tea/Show Save File")]
        public static void ShowSave()
        {
            string path = SaveManager.SavePath;
            EditorUtility.RevealInFinder(File.Exists(path) ? path : Application.persistentDataPath);
        }
    }
}
