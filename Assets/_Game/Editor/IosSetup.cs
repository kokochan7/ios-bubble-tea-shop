using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// One click to set the Player Settings an iPhone build needs: app name, bundle ID,
    /// portrait only, minimum iOS version, automatic signing, and only the game's scenes in the build. Run it from the menu bar:
    /// Bubble Tea > Apply iPhone Settings. It only changes ProjectSettings, so commit them afterwards.
    /// </summary>
    public static class IosSetup
    {
        private const string ProductName = "Bubble Tea Shop";
        private const string BundleId = "com.kokochan7.bubbleteashop";
        private const string MinimumIos = "15.0";

        [MenuItem("Bubble Tea/Apply iPhone Settings")]
        public static void Apply()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = "kokochan7";
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, BundleId);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
#endif

            // Portrait only: the game is designed for one-handed play.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.iOS.targetOSVersionString = MinimumIos;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.statusBarHidden = true;

            // Only the game's own scenes go in the app (Unity's starter SampleScene stays out).
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path.StartsWith("Assets/_Game/")) scenes.Add(scene);
            }
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"Bubble Tea: iPhone settings applied ({ProductName}, {BundleId}, portrait, iOS {MinimumIos}+). " +
                      "Commit the changed ProjectSettings files.");
        }
    }
}
