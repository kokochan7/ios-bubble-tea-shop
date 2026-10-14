using BubbleTea.Building;
using BubbleTea.Cooking;
using BubbleTea.Core;
using BubbleTea.Economy;
using BubbleTea.Feedback;
using BubbleTea.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Onboarding
{
    /// <summary>
    /// The first minute of a new game: one short instruction at a time, with the yellow
    /// arrow pointing at the right spot, until the player has built the counter and the
    /// tea brewer and sold and collected their first drink. Each step checks what the
    /// player has actually done, so an old save skips straight past it. Shown once.
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        private enum Step { Walk, GrabCash, BuildCounter, BuildBrewer, PickUp, Serve, Collect, Done }

        [SerializeField, Tooltip("The player.")]
        private Transform player;

        [SerializeField, Tooltip("The player's wallet.")]
        private Wallet wallet;

        [SerializeField, Tooltip("What the player is carrying.")]
        private ItemCarrier carrier;

        [SerializeField, Tooltip("The Counter's unlock pad.")]
        private UnlockPad counterPad;

        [SerializeField, Tooltip("The Tea Brewer's unlock pad.")]
        private UnlockPad brewerPad;

        [SerializeField, Tooltip("Where the counter's money piles up, relative to its pad.")]
        private Vector3 counterCashOffset = new(1.8f, 0f, 1.4f);

        [SerializeField, Tooltip("The banner behind the instruction.")]
        private RectTransform banner;

        [SerializeField, Tooltip("The instruction text.")]
        private Text label;

        [SerializeField, Tooltip("Seconds the last message stays up.")]
        private float finishSeconds = 3f;

        private Step step = Step.Walk;
        private Transform marker; // what the arrow points at during a step
        private Vector3 startPosition;
        private bool sold;
        private bool collectedAfterSale;
        private bool checkedSave;
        private float finishTimer;

        private void Awake()
        {
            marker = new GameObject("Tutorial Target").transform;
            marker.SetParent(transform, false);
            if (PlayerPrefs.GetInt(SaveManager.TutorialDoneKey, 0) == 1) gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.DrinkSold += OnSold;
            GameEvents.CashCollected += OnCash;
        }

        private void OnDisable()
        {
            GameEvents.DrinkSold -= OnSold;
            GameEvents.CashCollected -= OnCash;
            if (GoalArrow.Override == marker) GoalArrow.Override = null;
            if (banner != null) banner.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (player != null) startPosition = player.position;
        }

        private void OnSold(Vector3 _, int amount) => sold = true;

        private void OnCash(Vector3 _)
        {
            if (sold) collectedAfterSale = true;
        }

        private void Update()
        {
            // Saves load in Start, so the first Update is the first moment the pads show their real state.
            if (!checkedSave)
            {
                checkedSave = true;
                if (brewerPad != null && brewerPad.IsUnlocked)
                {
                    Finish(immediately: true);
                    return;
                }
            }

            if (step == Step.Done)
            {
                finishTimer -= Time.deltaTime;
                if (finishTimer <= 0f) gameObject.SetActive(false);
                return;
            }

            // Move on as soon as the current step is done (several can pass in one frame).
            while (step != Step.Done && IsComplete(step)) step++;
            if (step == Step.Done)
            {
                Finish(immediately: false);
                return;
            }

            Show(step);
            Pulse();
        }

        private bool IsComplete(Step current)
        {
            switch (current)
            {
                case Step.Walk:
                    return player == null || Flat(player.position - startPosition).magnitude > 1.5f;
                case Step.GrabCash:
                    return Unlocked(counterPad) || (wallet != null && counterPad != null && wallet.Money >= counterPad.Remaining);
                case Step.BuildCounter:
                    return Unlocked(counterPad);
                case Step.BuildBrewer:
                    return Unlocked(brewerPad);
                case Step.PickUp:
                    return sold || (carrier != null && carrier.Stack != null && carrier.Stack.Count > 0);
                case Step.Serve:
                    return sold;
                case Step.Collect:
                    return collectedAfterSale;
                default:
                    return true;
            }
        }

        private void Show(Step current)
        {
            string text;
            Transform aim = null;
            switch (current)
            {
                case Step.Walk:
                    text = "Drag anywhere to walk";
                    break;
                case Step.GrabCash:
                    text = "Walk over the cash to grab it";
                    aim = NearestCash();
                    break;
                case Step.BuildCounter:
                    text = "Stand on the pad to build your Counter";
                    break;
                case Step.BuildBrewer:
                    bool needCash = wallet != null && brewerPad != null && wallet.Money < brewerPad.Remaining;
                    text = needCash ? "Grab more cash for the Tea Brewer" : "Build the Tea Brewer";
                    if (needCash) aim = NearestCash();
                    break;
                case Step.PickUp:
                    text = "Stand by the Tea Brewer to pick up milk tea";
                    aim = PointAt(brewerPad.transform.position + new Vector3(0f, 0f, 1.2f));
                    break;
                case Step.Serve:
                    text = "Bring it to the Counter for your customer";
                    aim = PointAt(counterPad.transform.position + new Vector3(0f, 0f, 1.2f));
                    break;
                default:
                    text = "Grab the money they paid you";
                    aim = PointAt(counterPad.transform.position + counterCashOffset);
                    break;
            }

            if (label != null) label.text = text;
            if (banner != null && !banner.gameObject.activeSelf) banner.gameObject.SetActive(true);
            GoalArrow.Override = aim;
        }

        private void Finish(bool immediately)
        {
            step = Step.Done;
            PlayerPrefs.SetInt(SaveManager.TutorialDoneKey, 1);
            PlayerPrefs.Save();
            if (GoalArrow.Override == marker) GoalArrow.Override = null;
            if (immediately)
            {
                gameObject.SetActive(false);
                return;
            }
            if (label != null) label.text = "Nice! Keep building your shop";
            if (banner != null) banner.localScale = Vector3.one;
            finishTimer = finishSeconds;
        }

        // A gentle breathing scale so the eye catches each new instruction.
        private void Pulse()
        {
            if (banner == null) return;
            float scale = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            banner.localScale = new Vector3(scale, scale, 1f);
        }

        private Transform NearestCash()
        {
            if (player == null) return null;
            CashPile best = null;
            float bestDistance = float.MaxValue;
            foreach (CashPile pile in CashPile.All)
            {
                if (pile == null || pile.Count == 0) continue;
                float distance = Flat(pile.transform.position - player.position).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = pile;
                }
            }
            return best != null ? PointAt(best.transform.position) : null;
        }

        private Transform PointAt(Vector3 position)
        {
            marker.position = position;
            return marker;
        }

        private static bool Unlocked(UnlockPad pad) => pad == null || pad.IsUnlocked;

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
