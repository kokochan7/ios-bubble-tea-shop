using BubbleTea.Goals;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.UI
{
    /// <summary>
    /// The GOALS button and panel: today's goals with their progress and a Claim button,
    /// a "!" on the button when a reward is waiting, and a short "Goal complete!" banner.
    /// </summary>
    public class GoalsPanel : MonoBehaviour
    {
        [SerializeField] private DailyGoals goals;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        [SerializeField, Tooltip("The \"!\" shown on the GOALS button when a reward is waiting.")]
        private GameObject badge;

        [SerializeField, Tooltip("One entry per goal row.")]
        private Text[] descriptions;

        [SerializeField] private Text[] progressLabels;
        [SerializeField] private Button[] claimButtons;
        [SerializeField] private Text[] claimLabels;

        [SerializeField, Tooltip("Banner shown for a moment when a goal is finished.")]
        private Text banner;

        [SerializeField] private float bannerSeconds = 2.5f;

        private static readonly Color Ready = new(0.35f, 0.78f, 0.38f);
        private static readonly Color NotReady = new(0.45f, 0.45f, 0.55f);
        private static readonly Color Done = new(0.3f, 0.3f, 0.36f);

        private float bannerTimer;

        private void Awake()
        {
            if (openButton != null) openButton.onClick.AddListener(Toggle);
            if (closeButton != null) closeButton.onClick.AddListener(() => panel.SetActive(false));
            for (int i = 0; i < claimButtons.Length; i++)
            {
                int index = i;
                if (claimButtons[i] != null) claimButtons[i].onClick.AddListener(() => goals.Claim(index));
            }
        }

        private void OnEnable()
        {
            if (panel != null) panel.SetActive(false);
            if (banner != null) banner.gameObject.SetActive(false);
            if (goals == null) return;
            goals.Changed += Refresh;
            goals.Completed += ShowBanner;
            Refresh();
        }

        private void OnDisable()
        {
            if (goals == null) return;
            goals.Changed -= Refresh;
            goals.Completed -= ShowBanner;
        }

        private void Update()
        {
            if (banner == null || bannerTimer <= 0f) return;
            bannerTimer -= Time.deltaTime;
            Color color = banner.color;
            color.a = Mathf.Clamp01(bannerTimer / 0.5f);
            banner.color = color;
            if (bannerTimer <= 0f) banner.gameObject.SetActive(false);
        }

        private void Toggle()
        {
            if (panel == null) return;
            panel.SetActive(!panel.activeSelf);
            Refresh();
        }

        private void ShowBanner(GoalState goal)
        {
            if (banner == null) return;
            banner.text = "Goal complete! " + goal.label;
            banner.gameObject.SetActive(true);
            bannerTimer = bannerSeconds;
        }

        private void Refresh()
        {
            if (goals == null) return;
            if (badge != null) badge.SetActive(goals.HasClaimable);

            for (int i = 0; i < descriptions.Length; i++)
            {
                bool used = i < goals.Goals.Count;
                GoalState goal = used ? goals.Goals[i] : null;
                if (descriptions[i] != null) descriptions[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;

                descriptions[i].text = goal.label;
                if (progressLabels[i] != null) progressLabels[i].text = $"{goal.progress:N0} / {goal.target:N0}";

                Button claim = claimButtons[i];
                if (claim == null) continue;
                claim.interactable = goal.IsComplete && !goal.claimed;
                claim.targetGraphic.color = goal.claimed ? Done : goal.IsComplete ? Ready : NotReady;
                if (claimLabels[i] != null) claimLabels[i].text = goal.claimed ? "CLAIMED" : "$" + goal.reward.ToString("N0");
            }
        }
    }
}
