using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Feedback
{
    /// <summary>
    /// Buzzes the phone for big moments (a station unlocking, an upgrade). Unity's built-in
    /// vibration is a single strong buzz, so it's used sparingly; lighter "taps" need a
    /// native iOS plugin, which can come with the iPhone build milestone.
    /// </summary>
    public class Haptics : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEvents.Unlocked += OnUnlocked;
            GameEvents.UpgradeBought += Buzz;
            GameEvents.BonusCollected += Buzz;
        }

        private void OnDisable()
        {
            GameEvents.Unlocked -= OnUnlocked;
            GameEvents.UpgradeBought -= Buzz;
            GameEvents.BonusCollected -= Buzz;
        }

        private void OnUnlocked(Vector3 _) => Buzz();

        private static void Buzz()
        {
            if (!GameSettings.VibrationOn) return;
#if UNITY_IOS || UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
