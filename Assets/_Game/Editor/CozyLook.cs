using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// The finishing touch on the picture, like a cozy café game: warm color grading, a soft glow
    /// on bright things (lamps, screens), gentle contrast and a light vignette. Saved as a URP
    /// Volume Profile (Assets/_Game/Settings/CozyLook.asset) that every shop scene uses; edit it
    /// in the Inspector to tweak the look, or change the numbers here and rebuild the scenes.
    /// </summary>
    internal static class CozyLook
    {
        private const string Folder = "Assets/_Game/Settings";
        private const string ProfilePath = Folder + "/CozyLook.asset";

        /// <summary>Turns on post-processing for the camera and adds a global volume with the cozy look.</summary>
        public static void AddTo(Camera camera)
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var go = new GameObject("Cozy Look");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = BuildProfile();
        }

        // Rebuilt each time so changes here always take effect.
        private static VolumeProfile BuildProfile()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Game", "Settings");
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath) != null) AssetDatabase.DeleteAsset(ProfilePath);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            var tonemapping = Add<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var color = Add<ColorAdjustments>(profile);
            color.postExposure.Override(0.35f); // bright and sunny
            color.contrast.Override(8f);
            color.saturation.Override(16f);
            color.colorFilter.Override(new Color(1f, 0.98f, 0.94f)); // a touch of warmth

            var whiteBalance = Add<WhiteBalance>(profile);
            whiteBalance.temperature.Override(10f);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1f, 0.85f, 0.65f));

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.12f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0.18f, 0.10f, 0.08f));

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }
    }
}
