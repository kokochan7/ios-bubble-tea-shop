using BubbleTea.Staff;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// A banner near the top of the screen that says workers stopped because there wasn't enough
    /// money to pay them; they get back to work once they're paid. Hides itself after a few seconds.
    /// </summary>
    public class StaffUnpaidAlert : MonoBehaviour
    {
        [SerializeField, Tooltip("The banner to show and hide (a child of this object).")]
        private GameObject banner;

        [SerializeField, Tooltip("The banner's text.")]
        private Text label;

        [SerializeField, Tooltip("Seconds the banner stays up.")]
        private float seconds = 6f;

        private float timer;

        private void Start()
        {
            if (banner != null) banner.SetActive(false);
        }

        private void OnEnable() => Payroll.WorkersUnpaid += Show;
        private void OnDisable() => Payroll.WorkersUnpaid -= Show;

        private void Show(string who)
        {
            timer = seconds;
            if (label != null) label.text = $"{who} stopped working: not enough money for wages!\nThey'll get back to work once you can pay them.";
            if (banner != null) banner.SetActive(true);
        }

        private void Update()
        {
            if (timer <= 0f) return;
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f && banner != null) banner.SetActive(false);
        }
    }
}
