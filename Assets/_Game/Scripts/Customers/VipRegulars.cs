using System.Collections.Generic;
using BubbleTea.Building;
using BubbleTea.Cooking;
using BubbleTea.Core;
using UnityEngine;

namespace BubbleTea.Customers
{
    /// <summary>
    /// A few named regulars (VIPs) who come back once a shop day, at a random hour, and always
    /// order their favorite drink. They wear a pink bow and their name floats over their head.
    /// Serve a VIP quickly (before they're half out of patience) a few times and they become a
    /// friend of the shop for good: every friend makes all customers' tips 5% bigger.
    /// </summary>
    public class VipRegulars : MonoBehaviour
    {
        [SerializeField, Tooltip("The spawner VIPs walk in from.")]
        private CustomerSpawner spawner;

        [SerializeField, Tooltip("The regulars' names.")]
        private string[] names = { "Mei", "Kenji", "Aiko", "Leo", "Hana" };

        [SerializeField, Min(1), Tooltip("Quick visits it takes to win a VIP over.")]
        private int fastVisitsForPerk = 3;

        [SerializeField, Range(0f, 0.5f), Tooltip("Bigger tips from every customer, per VIP won over (0.05 = +5%).")]
        private float tipBonusPerFriend = 0.05f;

        [SerializeField, Tooltip("Hours of the shop day VIPs may visit between.")]
        private Vector2 visitHours = new(9f, 20f);

        private static readonly Color Pink = new(0.93f, 0.4f, 0.62f);

        /// <summary>The VIPs in the current shop, if any.</summary>
        public static VipRegulars Instance { get; private set; }

        /// <summary>Extra tip share from VIPs won over in this shop (0.1 = tips 10% bigger).</summary>
        public static float TipBonus => Instance != null ? Instance.Friends * Instance.tipBonusPerFriend : 0f;

        private class Regular
        {
            public string name;
            public string favorite;   // the drink's asset name; picked on their first visit
            public int fastVisits;
            public bool introduced;   // their arrival banner has been shown (only ever once)
            public float visitAt = -1f; // minute of today they come in, -1 = not today / already came
        }

        private readonly List<Regular> regulars = new();
        private int plannedDay = -1;

        private int Friends
        {
            get
            {
                int count = 0;
                foreach (Regular r in regulars) if (r.fastVisits >= fastVisitsForPerk) count++;
                return count;
            }
        }

        private void Awake()
        {
            Instance = this;
            foreach (string vipName in names) regulars.Add(new Regular { name = vipName });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            GameClock clock = GameClock.Instance;
            if (clock == null || spawner == null) return;
            if (clock.Day != plannedDay) PlanDay(clock);

            float now = clock.MinuteOfDay;
            foreach (Regular r in regulars)
            {
                if (r.visitAt < 0f || now < r.visitAt) continue;
                r.visitAt = -1f;
                Visit(r);
            }
        }

        // Each VIP picks an hour to drop by today.
        private void PlanDay(GameClock clock)
        {
            plannedDay = clock.Day;
            float now = clock.MinuteOfDay;
            foreach (Regular r in regulars)
            {
                float at = Random.Range(visitHours.x, visitHours.y) * 60f;
                r.visitAt = at > now ? at : -1f; // too late for today: they'll come tomorrow
            }
        }

        private void Visit(Regular r)
        {
            ServingCounter counter = ServingCounter.FindLane("Main");
            if (counter == null || !counter.CanMakeAnything) return;
            DrinkRecipe favorite = Favorite(r, counter);
            if (favorite == null) return;

            Customer vip = spawner.SpawnVisitor(spawner.transform.position, Customer.Visit.Vip, gone => OnGone(r, gone));
            if (vip == null) return;
            vip.MakeVip(r.name, favorite);
            NameTag.Add(vip.gameObject, "VIP " + r.name, 1.6f);
            if (r.introduced) return; // the banner shows only on a VIP's first visit
            r.introduced = true;
            string progress = r.fastVisits >= fastVisitsForPerk ? "a friend of the shop" : $"serve them fast to win them over ({r.fastVisits}/{fastVisitsForPerk})";
            GameEvents.RaiseAnnouncement($"VIP {r.name} is here for a {favorite.Drink.DisplayName}!\nThey're {progress}.", Pink);
        }

        // Their favorite drink: picked from what the shop makes on their first visit, then kept.
        // If it can't be made right now, they have something else today.
        private static DrinkRecipe Favorite(Regular r, ServingCounter counter)
        {
            var makeable = new List<DrinkRecipe>();
            foreach (DrinkRecipe recipe in counter.Menu)
            {
                if (recipe == null || !Station.AnyCanMake(recipe.Drink)) continue;
                if (recipe.Drink.name == r.favorite) return recipe;
                makeable.Add(recipe);
            }
            if (makeable.Count == 0) return null;
            DrinkRecipe pick = makeable[Random.Range(0, makeable.Count)];
            if (string.IsNullOrEmpty(r.favorite)) r.favorite = pick.Drink.name;
            return pick;
        }

        private void OnGone(Regular r, Customer vip)
        {
            if (!vip.WasServed || vip.ServedMood < 0.5f) return;
            BubbleTea.Extras.Profile.AddPearls(1, $"{r.name} loved the visit");
            bool wasFriend = r.fastVisits >= fastVisitsForPerk;
            r.fastVisits++;
            if (!wasFriend && r.fastVisits >= fastVisitsForPerk)
            {
                GameEvents.RaiseAnnouncement(
                    $"{r.name} is now a friend of the shop!\nAll tips are {tipBonusPerFriend * 100f:0}% bigger, for good.", Pink);
                GameEvents.RaiseStarsAwarded(0.5f);
            }
        }

        /// <summary>For the save file.</summary>
        public List<VipSave> Snapshot()
        {
            var list = new List<VipSave>();
            foreach (Regular r in regulars) list.Add(new VipSave { name = r.name, favorite = r.favorite, fastVisits = r.fastVisits, introduced = r.introduced });
            return list;
        }

        /// <summary>Puts back saved progress.</summary>
        public void Restore(List<VipSave> saved)
        {
            if (saved == null) return;
            foreach (VipSave entry in saved)
            {
                if (entry == null) continue;
                Regular r = regulars.Find(x => x.name == entry.name);
                if (r == null) continue;
                r.favorite = entry.favorite;
                r.fastVisits = Mathf.Max(0, entry.fastVisits);
                r.introduced = entry.introduced || !string.IsNullOrEmpty(entry.favorite); // older saves: already met
            }
        }
    }

    /// <summary>One VIP's progress in the save file.</summary>
    [System.Serializable]
    public class VipSave
    {
        public string name;
        public string favorite;
        public int fastVisits;
        public bool introduced;
    }
}
