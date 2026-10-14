using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Shows a pad only once every pad in a list is unlocked, for example the pad that opens
    /// the next level once the whole shop is built.
    /// </summary>
    public class RevealWhenAllUnlocked : MonoBehaviour
    {
        [SerializeField, Tooltip("Pads that must all be unlocked first.")]
        private UnlockPad[] required;

        [SerializeField, Tooltip("The pad to show (it starts hidden).")]
        private UnlockPad target;

        private void OnEnable()
        {
            foreach (UnlockPad pad in required)
            {
                if (pad != null) pad.Unlocked += OnUnlocked;
            }
        }

        private void OnDisable()
        {
            foreach (UnlockPad pad in required)
            {
                if (pad != null) pad.Unlocked -= OnUnlocked;
            }
        }

        // Saves load in Start too, and each restored pad raises Unlocked, so this also catches up after loading.
        private void Start() => Check();

        private void OnUnlocked(UnlockPad _) => Check();

        private void Check()
        {
            if (target == null || target.IsUnlocked || target.gameObject.activeSelf) return;
            foreach (UnlockPad pad in required)
            {
                if (pad != null && !pad.IsUnlocked) return;
            }
            target.gameObject.SetActive(true);
        }
    }
}
