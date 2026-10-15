using BubbleTea.Staff;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// A banner near the top of the screen that says workers quit because there wasn't enough
    /// money to pay them, and that they can be hired again. Hides itself after a few seconds.
    /// </summary>
    public class StaffQuitAlert : MonoBehaviour
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

        private void OnEnable() => Payroll.WorkersQuit += Show;
        private void OnDisable() => Payroll.WorkersQuit -= Show;

        private void Show(string who)
        {
            timer = seconds;
            if (label != null) label.text = $"{who} quit: you couldn't pay their wages!\nHire them again at their pad.";
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
