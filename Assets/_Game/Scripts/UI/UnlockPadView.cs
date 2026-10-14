using BubbleTea.Building;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The label painted on an unlock pad: the station's name, the money still needed,
    /// and a fill that rises as you pay.
    /// </summary>
    public class UnlockPadView : MonoBehaviour
    {
        [SerializeField, Tooltip("The pad to display.")]
        private UnlockPad pad;

        [SerializeField, Tooltip("Shows the station's name.")]
        private Text nameLabel;

        [SerializeField, Tooltip("Shows the money still needed.")]
        private Text priceLabel;

        [SerializeField, Tooltip("A Filled image whose fill shows how much is paid.")]
        private Image fill;

        private void OnEnable()
        {
            if (pad == null) return;
            pad.ProgressChanged += Show;
            if (nameLabel != null && pad.Definition != null) nameLabel.text = pad.Definition.DisplayName;
            Show(pad.Remaining, pad.Cost);
        }

        private void OnDisable()
        {
            if (pad != null) pad.ProgressChanged -= Show;
        }

        private void Show(int remaining, int cost)
        {
            if (priceLabel != null) priceLabel.text = "$" + remaining.ToString("N0");
            if (fill != null) fill.fillAmount = cost > 0 ? 1f - (float)remaining / cost : 0f;
        }
    }
}
