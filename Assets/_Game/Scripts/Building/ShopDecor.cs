using System.Collections.Generic;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// A piece of decor bought on a pad (bamboo plants, paper lanterns, a lucky cat, a koi pond, a
    /// cherry blossom tree). Each one makes the shop a little nicer while it stands: customers wait
    /// longer (patience), come in more often (customer flow) and are happier (more likely to tip,
    /// and tip more). The bonuses of every piece in the shop add up.
    /// </summary>
    public class ShopDecor : MonoBehaviour
    {
        private static readonly List<ShopDecor> active = new();

        [SerializeField, Tooltip("Name shown when it's bought.")]
        private string shownName = "Decor";

        [SerializeField, Range(0f, 1f), Tooltip("Extra customer patience (0.1 = +10%).")]
        private float patience;

        [SerializeField, Range(0f, 1f), Tooltip("Extra customers coming in (0.1 = +10%).")]
        private float flow;

        [SerializeField, Range(0f, 1f), Tooltip("Extra happiness (0.1 = +10%): customers tip more often and a little more.")]
        private float happiness;

        private static readonly Color Pink = new(0.93f, 0.45f, 0.62f);

        /// <summary>Extra patience from all decor in the shop (0.15 = +15%).</summary>
        public static float Patience => Sum(d => d.patience);

        /// <summary>Extra customer flow from all decor in the shop.</summary>
        public static float Flow => Sum(d => d.flow);

        /// <summary>Extra happiness from all decor in the shop.</summary>
        public static float Happiness => Sum(d => d.happiness);

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        private void Start()
        {
            // Bought just now (not put back by a loading save): say what it does.
            if (Time.timeSinceLevelLoad < 1.5f) return;
            var parts = new List<string>();
            if (patience > 0f) parts.Add($"+{patience * 100f:0}% patience");
            if (flow > 0f) parts.Add($"+{flow * 100f:0}% customers");
            if (happiness > 0f) parts.Add($"+{happiness * 100f:0}% happiness");
            GameEvents.RaiseAnnouncement($"{shownName} added!\n{string.Join(", ", parts)}", Pink);
        }

        private static float Sum(System.Func<ShopDecor, float> pick)
        {
            float total = 0f;
            foreach (ShopDecor decor in active)
            {
                if (decor != null) total += pick(decor);
            }
            return total;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => active.Clear();
    }
}
