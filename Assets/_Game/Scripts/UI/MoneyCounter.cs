using BubbleTea.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// Shows the wallet's money on screen and gives the label a little bounce
    /// every time it changes.
    /// </summary>
    public class MoneyCounter : MonoBehaviour
    {
        [SerializeField, Tooltip("Wallet to display.")]
        private Wallet wallet;

        [SerializeField, Tooltip("Text that shows the amount.")]
        private Text label;

        [SerializeField, Tooltip("Extra size at the peak of the bounce (0.15 = 15% bigger).")]
        private float bounceAmount = 0.15f;

        [SerializeField, Tooltip("How quickly the bounce settles.")]
        private float bounceSettleSpeed = 10f;

        private float bounce;

        private void OnEnable()
        {
            if (wallet == null) return;
            wallet.MoneyChanged += Show;
            Show(wallet.Money);
            bounce = 0f;
        }

        private void OnDisable()
        {
            if (wallet != null) wallet.MoneyChanged -= Show;
        }

        private void Update()
        {
            bounce = Mathf.MoveTowards(bounce, 0f, bounceSettleSpeed * Time.deltaTime);
            transform.localScale = Vector3.one * (1f + bounce * bounceAmount);
        }

        private void Show(int amount)
        {
            if (label != null) label.text = "$" + amount.ToString("N0");
            bounce = 1f;
        }
    }
}
