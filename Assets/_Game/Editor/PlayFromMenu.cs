using UnityEditor;
using UnityEditor.SceneManagement;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Makes Play in the Editor start at the main menu, like the game on a phone, whichever
    /// scene is open. Turn it off with Bubble Tea > Play From Main Menu to test a shop directly.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayFromMenu
    {
        private const string MenuPath = "Bubble Tea/Play From Main Menu";
        private const string PrefKey = "BubbleTea.PlayFromMenu";
        internal const string MenuScenePath = "Assets/_Game/Scenes/MainMenu.unity";

        static PlayFromMenu() => EditorApplication.delayCall += Apply;

        private static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        /// <summary>Points Play at the menu scene (if it's built and the option is on).</summary>
        internal static void Apply()
        {
            SceneAsset menu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
            EditorSceneManager.playModeStartScene = Enabled ? menu : null;
        }

        [MenuItem(MenuPath, priority = 100)]
        private static void Toggle()
        {
            Enabled = !Enabled;
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }
    }
}
