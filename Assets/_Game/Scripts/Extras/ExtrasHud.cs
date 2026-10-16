using System;
using BubbleTea.Building;
using BubbleTea.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// The pearl counter and the SHOP, TEAM, PETS and PLAY buttons down the right side of the screen,
    /// with their pop-ups: the decor shop and collections, the staff skill trees, pets and outfits,
    /// and the mini-games. Built in code when a shop opens (the save manager asks for it), so the
    /// scenes don't need anything extra. The game is paused while a pop-up is open.
    /// </summary>
    public class ExtrasHud : MonoBehaviour
    {
        private const float Scale = 0.82f;       // same as the HUD's buttons
        private const float Margin = 16f;
        private const float Step = 110f * Scale + 10f;

        private static ExtrasHud instance;

        private RectTransform canvas;
        private RectTransform column;
        private Text pearlLabel;
        private Text playLabel;
        private GameObject popup;     // the open pop-up, if any
        private Action refresh;       // redraws the open pop-up
        private int tab;
        private Wallet wallet;
        private Economy.OfflineEarnings earnings;

        /// <summary>Called by the save manager as a shop opens.</summary>
        public static void Ensure()
        {
            if (instance != null) return;
            new GameObject("Extras HUD").AddComponent<ExtrasHud>();
        }

        private void Awake()
        {
            instance = this;
            canvas = UiKit.Canvas(transform, "Extras Canvas", 30);
            BuildColumn();
        }

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            wallet = FindFirstObjectByType<Wallet>();
            earnings = FindFirstObjectByType<Economy.OfflineEarnings>();
#else
            wallet = FindObjectOfType<Wallet>();
            earnings = FindObjectOfType<Economy.OfflineEarnings>();
#endif
            PetCompanion.Refresh();
        }

        private void OnEnable()
        {
            Profile.Changed += OnChanged;
            TeamSkills.Changed += OnChanged;
        }

        private void OnDisable()
        {
            Profile.Changed -= OnChanged;
            TeamSkills.Changed -= OnChanged;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (popup != null) Time.timeScale = 1f;
        }

        private void OnChanged()
        {
            if (pearlLabel != null) pearlLabel.text = Profile.Pearls.ToString("N0");
            refresh?.Invoke();
        }

        private void Update()
        {
            bool show = !MoveMode.Busy;
            if (column.gameObject.activeSelf != show) column.gameObject.SetActive(show);
            int wait = MiniGames.SecondsUntilReady;
            playLabel.text = wait > 0 ? $"{wait / 60}:{wait % 60:00}" : "PLAY";
        }

        // ---------------------------------------------------------------- the column of buttons

        private void BuildColumn()
        {
            // Inside the safe area, like the HUD, so the buttons line up with MENU, GOALS and STATS.
            column = UiKit.Group(canvas, "Extras Column");
            UiKit.Fill(column);
            column.gameObject.AddComponent<BubbleTea.UI.SafeArea>();

            // Pearls, under the STATS button.
            RectTransform counter = UiKit.Box(column, "Pearls", new Color(0.15f, 0.15f, 0.2f, 0.75f));
            Place(counter, 3);
            counter.GetComponent<Image>().raycastTarget = false;
            RectTransform pearl = UiKit.PearlIcon(counter, 70f);
            UiKit.Pin(pearl, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(70f, 70f));
            pearlLabel = UiKit.Label(counter, Profile.Pearls.ToString("N0"), 50, new Color(1f, 0.92f, 0.7f), TextAnchor.MiddleRight);
            pearlLabel.rectTransform.anchorMin = Vector2.zero;
            pearlLabel.rectTransform.anchorMax = Vector2.one;
            pearlLabel.rectTransform.offsetMin = new Vector2(90f, 0f);
            pearlLabel.rectTransform.offsetMax = new Vector2(-18f, 0f);

            Place((RectTransform)UiKit.Button(column, "SHOP", new Color(0.95f, 0.66f, 0.3f, 0.92f), Vector2.zero, () => Open(DecorShop), 50).transform, 4);
            Place((RectTransform)UiKit.Button(column, "TEAM", new Color(0.62f, 0.45f, 0.88f, 0.92f), Vector2.zero, () => Open(Team), 50).transform, 5);
            Place((RectTransform)UiKit.Button(column, "PETS", new Color(0.4f, 0.75f, 0.7f, 0.92f), Vector2.zero, () => Open(Pets), 50).transform, 6);
            Button play = UiKit.Button(column, "PLAY", new Color(0.35f, 0.7f, 0.95f, 0.92f), Vector2.zero, () => Open(Play), 50);
            playLabel = UiKit.TextOf(play);
            Place((RectTransform)play.transform, 7);
        }

        // Top-right, in the same column as the MENU, GOALS and STATS buttons.
        private static void Place(RectTransform rect, int slot)
        {
            UiKit.Pin(rect, new Vector2(1f, 1f), new Vector2(-Margin, -Margin - slot * Step), new Vector2(200f, 110f));
            rect.localScale = Vector3.one * Scale;
        }

        // ---------------------------------------------------------------- pop-ups

        private void Open(Action<RectTransform, Text> draw)
        {
            Close();
            tab = 0;
            popup = UiKit.Popup(canvas, "", new Vector2(940f, 1560f), Close, out RectTransform body, out Text title).gameObject;
            refresh = () =>
            {
                for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
                draw(body, title);
            };
            refresh();
            Time.timeScale = 0f;
        }

        private void Close()
        {
            refresh = null;
            if (popup == null) return;
            Destroy(popup);
            popup = null;
            Time.timeScale = 1f;
        }

        private void Redraw() => refresh?.Invoke();

        // A row across the body, rows counted from the top.
        private static RectTransform Row(RectTransform body, float top, float height)
        {
            RectTransform row = UiKit.Box(body, "Row", new Color(1f, 1f, 1f, 0.06f));
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.offsetMin = new Vector2(0f, -top - height);
            row.offsetMax = new Vector2(0f, -top);
            return row;
        }

        private static Text RowText(RectTransform row, string text, int size, float from, float to, Color color, float top = 0f, float bottom = 0f)
        {
            Text label = UiKit.Label(row, text, size, color, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = new Vector2(from, 0f);
            label.rectTransform.anchorMax = new Vector2(to, 1f);
            label.rectTransform.offsetMin = new Vector2(24f, bottom);
            label.rectTransform.offsetMax = new Vector2(-8f, -top);
            return label;
        }

        private static Button RowButton(RectTransform row, string text, Color color, Action onClick, bool enabled = true)
        {
            Button button = UiKit.Button(row, text, enabled ? color : UiKit.Gray, Vector2.zero, () => onClick(), 38);
            button.interactable = enabled;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0.68f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(8f, 16f);
            rect.offsetMax = new Vector2(-16f, -16f);
            return button;
        }

        // A price button with a pearl in it.
        private static void PriceButton(RectTransform row, int price, Action onBuy)
        {
            bool afford = Profile.Pearls >= price;
            Button button = RowButton(row, "     " + price, UiKit.Green, onBuy, afford);
            RectTransform pearl = UiKit.PearlIcon(button.transform, 52f);
            UiKit.Pin(pearl, new Vector2(0.5f, 0.5f), new Vector2(-30f, 0f), new Vector2(52f, 52f));
            pearl.pivot = new Vector2(0.5f, 0.5f);
        }

        private void Tabs(RectTransform body, string[] names)
        {
            Button[] tabs = UiKit.Tabs(body, names, i =>
            {
                tab = i;
                Redraw();
            });
            for (int i = 0; i < tabs.Length; i++) tabs[i].GetComponent<Image>().color = i == tab ? UiKit.Pink : UiKit.Gray;
        }

        // ---------------------------------------------------------------- SHOP: collectible decor

        private void DecorShop(RectTransform body, Text title)
        {
            title.text = "DECOR SHOP";
            Tabs(body, new[] { "COZY", "NEON", "GREEN", "CUPS" });
            string collection = Collections.Names[tab];
            bool complete = Collections.IsComplete(collection);

            RectTransform header = Row(body, 100f, 130f);
            header.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            RowText(header, $"{collection}   {Collections.OwnedIn(collection)}/4", 46, 0f, 1f, Color.white, 0f, 60f);
            RowText(header, complete
                ? $"Complete! Customers are {Collections.BonusPerCollection * 100f:0}% happier in every shop."
                : $"Collect all 4 to make customers {Collections.BonusPerCollection * 100f:0}% happier in every shop.",
                32, 0f, 1f, complete ? new Color(0.6f, 0.95f, 0.55f) : new Color(0.8f, 0.8f, 0.88f), 60f, 0f);

            float top = 250f;
            foreach (Collections.Item item in Collections.All)
            {
                if (item.Collection != collection) continue;
                RectTransform row = Row(body, top, 150f);
                top += 165f;
                RowText(row, item.Name, 40, 0f, 0.68f, Color.white);
                if (!Profile.OwnsDecor(item.Id))
                {
                    PriceButton(row, item.Price, () =>
                    {
                        Collections.Buy(item);
                        Redraw();
                    });
                }
                else if (Collections.IsPlaced(item.Id))
                {
                    RowButton(row, "PUT AWAY", UiKit.Gray, () =>
                    {
                        Collections.PutAway(item.Id);
                        Redraw();
                    });
                }
                else
                {
                    RowButton(row, "PLACE", UiKit.Pink, () =>
                    {
                        Close();
                        Collections.PlaceNew(item);
                    });
                }
            }

            Text hint = UiKit.Label(body, "Earn pearls from daily goals, happy VIPs, mini-games and your pet.", 32,
                new Color(0.8f, 0.8f, 0.88f));
            UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(900f, 110f));
        }

        // ---------------------------------------------------------------- TEAM: staff skill trees

        private void Team(RectTransform body, Text title)
        {
            title.text = "STAFF SKILLS";
            Tabs(body, TeamSkills.RoleNames);
            var role = (TeamSkills.Role)tab;
            int level = TeamSkills.Level(role);
            int points = TeamSkills.PointsLeft(role);

            RectTransform header = Row(body, 100f, 190f);
            header.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            RowText(header, $"{TeamSkills.Title(role)} team   Level {level}", 46, 0f, 1f, Color.white, 0f, 125f);
            string progress = level >= TeamSkills.MaxLevel
                ? "Max level!"
                : $"{TeamSkills.XpIntoLevel(role)} / {TeamSkills.XpForNext(level)} XP to level {level + 1}";
            RowText(header, progress, 34, 0f, 1f, new Color(0.8f, 0.8f, 0.88f), 65f, 65f);
            string pointText = TeamSkills.Workers(role) == 0
                ? $"Hire a {TeamSkills.Title(role).ToLowerInvariant()} to start earning XP."
                : points > 0 ? $"{points} skill point{(points == 1 ? "" : "s")} to spend!" : "Keep working to earn skill points.";
            RowText(header, pointText, 34, 0f, 1f, points > 0 ? new Color(1f, 0.85f, 0.4f) : new Color(0.8f, 0.8f, 0.88f), 125f, 0f);

            // XP bar.
            RectTransform barBack = UiKit.Box(body, "XP Bar", new Color(0.3f, 0.3f, 0.38f));
            barBack.anchorMin = new Vector2(0f, 1f);
            barBack.anchorMax = new Vector2(1f, 1f);
            barBack.pivot = new Vector2(0.5f, 1f);
            barBack.offsetMin = new Vector2(24f, -320f);
            barBack.offsetMax = new Vector2(-24f, -296f);
            float fill = level >= TeamSkills.MaxLevel ? 1f : TeamSkills.XpIntoLevel(role) / (float)TeamSkills.XpForNext(level);
            RectTransform barFill = UiKit.Box(barBack, "Fill", UiKit.Purple);
            barFill.anchorMin = Vector2.zero;
            barFill.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            barFill.offsetMin = barFill.offsetMax = Vector2.zero;

            float top = 350f;
            for (int p = 0; p < 3; p++)
            {
                int perk = p;
                TeamSkills.Perk info = TeamSkills.Perks[tab][p];
                int rank = TeamSkills.Rank(role, p);
                RectTransform row = Row(body, top, 230f);
                top += 250f;
                string pips = new string('●', rank) + new string('○', TeamSkills.MaxRank - rank);
                RowText(row, $"{info.Name}   {pips}", 40, 0f, 0.68f, Color.white, 20f, 120f);
                RowText(row, info.Description + (rank > 0 ? $"\n(rank {rank} of {TeamSkills.MaxRank})" : ""), 32, 0f, 0.68f,
                    new Color(0.8f, 0.8f, 0.88f), 100f, 10f);
                if (rank >= TeamSkills.MaxRank) RowButton(row, "MAXED", UiKit.Gray, () => { }, false);
                else RowButton(row, "LEARN", UiKit.Purple, () => TeamSkills.Learn(role, perk), points > 0);
            }
        }

        // ---------------------------------------------------------------- PETS: pets and outfits

        private void Pets(RectTransform body, Text title)
        {
            title.text = "PETS";
            Tabs(body, new[] { "PETS", "OUTFITS" });
            float top = 110f;
            if (tab == 0)
            {
                foreach (PetCompanion.Pet pet in PetCompanion.Pets)
                {
                    RectTransform row = Row(body, top, 150f);
                    top += 165f;
                    RowText(row, pet.Name, 40, 0f, 0.68f, Color.white);
                    if (!Profile.OwnsPet(pet.Id))
                    {
                        PriceButton(row, pet.Price, () =>
                        {
                            if (!Profile.TrySpend(pet.Price)) return;
                            Profile.AddPet(pet.Id);
                            Profile.SetPet(pet.Id);
                            PetCompanion.Refresh();
                        });
                    }
                    else if (Profile.ActivePet == pet.Id)
                    {
                        RowButton(row, "IN SHOP", UiKit.Gray, () =>
                        {
                            Profile.SetPet("");
                            PetCompanion.Refresh();
                        });
                    }
                    else
                    {
                        RowButton(row, "CHOOSE", UiKit.Pink, () =>
                        {
                            Profile.SetPet(pet.Id);
                            PetCompanion.Refresh();
                        });
                    }
                }
                Text hint = UiKit.Label(body,
                    "Your pet roams the shop and says hello to customers: they wait longer, tip more, and sometimes tip your pet. It also naps on empty chairs and sniffs out money (and pearls). Tap IN SHOP to send it home.",
                    32, new Color(0.8f, 0.8f, 0.88f));
                UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 220f));
            }
            else
            {
                foreach (PetCompanion.Outfit outfit in PetCompanion.Outfits)
                {
                    RectTransform row = Row(body, top, 150f);
                    top += 165f;
                    RowText(row, outfit.Name, 40, 0f, 0.68f, Color.white);
                    if (!Profile.OwnsOutfit(outfit.Id))
                    {
                        PriceButton(row, outfit.Price, () =>
                        {
                            if (!Profile.TrySpend(outfit.Price)) return;
                            Profile.AddOutfit(outfit.Id);
                            Profile.SetOutfit(outfit.Id);
                            PetCompanion.Refresh();
                        });
                    }
                    else if (Profile.ActiveOutfit == outfit.Id)
                    {
                        RowButton(row, "TAKE OFF", UiKit.Gray, () =>
                        {
                            Profile.SetOutfit("");
                            PetCompanion.Refresh();
                        });
                    }
                    else
                    {
                        RowButton(row, "WEAR", UiKit.Pink, () =>
                        {
                            Profile.SetOutfit(outfit.Id);
                            PetCompanion.Refresh();
                        });
                    }
                }
                Text hint = UiKit.Label(body, "Outfits fit every pet.", 32, new Color(0.8f, 0.8f, 0.88f));
                UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 110f));
            }
        }

        // ---------------------------------------------------------------- PLAY: mini-games

        private void Play(RectTransform body, Text title)
        {
            title.text = "MINI-GAMES";
            int wait = MiniGames.SecondsUntilReady;
            int prize = Prize(1f);

            Text intro = UiKit.Label(body, wait > 0
                    ? $"Your next game is ready in {wait / 60}:{wait % 60:00}."
                    : $"Play a quick game for up to ${prize:N0}.\nA great score also earns pearls!",
                40, Color.white);
            UiKit.Pin(intro.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(900f, 200f));
            if (wait > 0) return;

            GameCard(body, 260f, "SHAKE IT!", "Tap when the line is in the green zone. Five rounds, each one faster.", true);
            GameCard(body, 640f, "PEARL DROP", "Tap the falling tapioca pearls before they land in the cup.", false);
        }

        private void GameCard(RectTransform body, float top, string name, string description, bool shake)
        {
            RectTransform row = Row(body, top, 340f);
            Text label = UiKit.Label(row, name, 56, Color.white);
            UiKit.Pin(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(880f, 80f));
            Text about = UiKit.Label(row, description, 34, new Color(0.8f, 0.8f, 0.88f));
            UiKit.Pin(about.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(860f, 100f));
            Button start = UiKit.Button(row, "START", UiKit.Blue, new Vector2(360f, 100f), () => StartGame(shake), 46);
            UiKit.Pin((RectTransform)start.transform, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(360f, 100f));
        }

        private void StartGame(bool shake)
        {
            refresh = null; // the game draws itself
            RectTransform body = (RectTransform)popup.transform.Find("Panel/Body");
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            MiniGames.Play(body, shake, score => ShowResult(body, score));
        }

        private void ShowResult(RectTransform body, float score)
        {
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
            int money = Prize(score);
            int pearls = score >= 0.9f ? 2 : score >= 0.6f ? 1 : 0;
            string verdict = score >= 0.9f ? "AMAZING!" : score >= 0.6f ? "GREAT JOB!" : score >= 0.3f ? "NOT BAD!" : "NICE TRY!";

            Text result = UiKit.Label(body, $"{verdict}\n\nScore {Mathf.RoundToInt(score * 100f)}%\n\n+${money:N0}" +
                (pearls > 0 ? $"\n+{pearls} pearl{(pearls == 1 ? "" : "s")}" : ""), 56, Color.white);
            UiKit.Pin(result.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 700f));
            Button collect = UiKit.Button(body, "COLLECT", UiKit.Green, new Vector2(460f, 140f), () =>
            {
                if (wallet != null) wallet.Add(money);
                Core.GameEvents.RaiseBonusCollected();
                if (pearls > 0) Profile.AddPearls(pearls, "Mini-game bonus!");
                Close();
            }, 56);
            UiKit.Pin((RectTransform)collect.transform, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(460f, 140f));
        }

        // Money for a score: about two minutes of the shop's earnings for a perfect game.
        private int Prize(float score)
        {
            float rate = earnings != null ? earnings.RatePerMinute : 0f;
            return Mathf.RoundToInt(Mathf.Max(50f, rate * 2f) * Mathf.Clamp01(score));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => instance = null;
    }
}
