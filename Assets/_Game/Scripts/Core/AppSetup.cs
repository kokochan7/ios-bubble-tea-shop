using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Phone-wide settings applied once when the game starts: run at 60 frames per second
    /// (iPhones default to 30) and keep the screen awake while playing.
    /// </summary>
    public static class AppSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
