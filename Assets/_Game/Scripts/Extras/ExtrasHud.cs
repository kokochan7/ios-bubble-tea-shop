using System;
using BubbleTea.Building;
using UnityEngine;
using UnityEngine.UI;

namespace BubbleTea.Extras
{
    /// <summary>
    /// The pearl counter beside the money, SHOP under STATS on the right and TEAM, PETS and PLAY down the left side,
    /// with their pop-ups: the decor shop and collections, the staff skill trees, pets, and the
    /// mini-games (only once the whole shop is built). Built in code when a shop opens (the save manager asks for it), so the
    /// scenes don't need anything extra. The game keeps running behind SHOP, TEAM and PETS; PLAY pauses it.
    /// </summary>
    public class ExtrasHud : MonoBehaviour
    {
        private const float Scale = 0.82f;       // same as the HUD's buttons
        private const float Margin = 16f;
        private const float Step = 110f * Scale + 10f;

        private static ExtrasHud instance;

        /// <summary>True while the player has paused the game with the pause button.</summary>
        public static bool Paused { get; private set; }

        private GameObject pauseBars;      // the "II" shown while running
        private GameObject playTriangle;   // the play arrow shown while paused
        private GameObject pausedBanner;

        private RectTransform canvas;
        private RectTransform column;
        private Text pearlLabel;
        private Text playLabel;
        private Text pointsLabel;
        private GameObject pointsBadge;
        private GameObject playButton;
        private BubbleTea.Saving.SaveManager saveManager;
        private float endgameCheck;
        private GameObject popup;     // the open pop-up, if any
        private Action refresh;       // redraws the open pop-up
        private int tab;
        private bool paused;          // whether the open pop-up paused the game

        /// <summary>Called by the save manager as a shop opens.</summary>
        public static void Ensure()
        {
            if (instance != null) return;
            new GameObject("Extras HUD").AddComponent<ExtrasHud>();
        }

        private void Awake()
        {
            instance = this;
            gameObject.AddComponent<HudStyle>(); // the raised, modern look for the whole HUD
            canvas = UiKit.Canvas(transform, "Extras Canvas", 30);
            BuildColumn();
        }

        private void Start()
        {
#if UNITY_2023_1_OR_NEWER
            saveManager = FindFirstObjectByType<BubbleTea.Saving.SaveManager>();
#else
            saveManager = FindObjectOfType<BubbleTea.Saving.SaveManager>();
#endif
            PetCompanion.Refresh();
            StartCoroutine(SetUpMenus());
        }

        // A frame in, once every menu has hidden itself: front of the HUD, one at a time, red X.
        private System.Collections.IEnumerator SetUpMenus()
        {
            yield return null;
            MenuLayers.Scan();
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
            if ((popup != null && paused) || Paused) Time.timeScale = 1f;
            Paused = false;
        }

        private void OnChanged()
        {
            if (pearlLabel != null) pearlLabel.text = Profile.Pearls.ToString("N0");
            ShowPoints();
            refresh?.Invoke();
        }

        // Skill points waiting to be spent across every team, on the TEAM button's red bubble.
        private void ShowPoints()
        {
            if (pointsBadge == null) return;
            int points = 0;
            foreach (TeamSkills.Role role in Enum.GetValues(typeof(TeamSkills.Role))) points += TeamSkills.PointsLeft(role);
            if (pointsBadge.activeSelf != points > 0) pointsBadge.SetActive(points > 0);
            pointsLabel.text = points.ToString();
        }

        private void Update()
        {
            bool show = !MoveMode.Busy;
            if (column.gameObject.activeSelf != show) column.gameObject.SetActive(show);
            // Other pop-ups (game over, mini-games) un-pause as they close; keep the player's pause.
            if (Paused && Time.timeScale != 0f) Time.timeScale = 0f;
            int wait = MiniGames.SecondsUntilReady;
            playLabel.text = wait > 0 ? $"{wait / 60}:{wait % 60:00}" : "PLAY";

            endgameCheck -= Time.unscaledDeltaTime;
            if (endgameCheck <= 0f)
            {
                endgameCheck = 1f;
                ShowPoints();
                bool endgame = EverythingUnlocked();
                if (playButton.activeSelf != endgame) playButton.SetActive(endgame);
            }
        }

        // The endgame: every pad in the shop is bought (the stop to the next shop doesn't count).
        private bool EverythingUnlocked()
        {
            if (saveManager == null || saveManager.Pads == null || saveManager.Pads.Count == 0) return false;
            foreach (BubbleTea.Building.UnlockPad pad in saveManager.Pads)
            {
                if (pad == null || pad.IsUnlocked) continue;
                GameObject prefab = pad.Definition != null ? pad.Definition.Prefab : null;
                if (prefab != null && prefab.GetComponentInChildren<BubbleTea.Travel.ShopTravel>(true) != null) continue;
                return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- the column of buttons

        private void BuildColumn()
        {
            // Inside the safe area, like the HUD, so the buttons line up with MENU, GOALS and STATS.
            column = UiKit.Group(canvas, "Extras Column");
            UiKit.Fill(column);
            column.gameObject.AddComponent<BubbleTea.UI.SafeArea>();

            // Pearls just right of the money (330 wide, centered); MENU and the right column start a slot lower to make room.
            RectTransform counter = UiKit.Box(column, "Pearls", new Color(0.15f, 0.15f, 0.2f, 0.75f));
            UiKit.Pin(counter, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(240f, 100f));
            counter.pivot = new Vector2(0f, 1f);
            counter.anchoredPosition = new Vector2(165f * Scale + 10f, -Margin - 10f * Scale);
            counter.localScale = Vector3.one * Scale;
            counter.GetComponent<Image>().raycastTarget = false;
            RectTransform pearl = UiKit.PearlIcon(counter, 64f);
            UiKit.Pin(pearl, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(64f, 64f));
            pearlLabel = UiKit.Label(counter, Profile.Pearls.ToString("N0"), 52, new Color(1f, 0.92f, 0.7f), TextAnchor.MiddleCenter);
            pearlLabel.rectTransform.anchorMin = Vector2.zero;
            pearlLabel.rectTransform.anchorMax = Vector2.one;
            pearlLabel.rectTransform.offsetMin = new Vector2(84f, 0f);
            pearlLabel.rectTransform.offsetMax = new Vector2(-18f, 0f);

            Place((RectTransform)UiKit.Button(column, "SHOP", new Color(0.95f, 0.66f, 0.3f, 0.92f), Vector2.zero, () => Open(DecorShop), 50).transform, 4, false);
            Button team = UiKit.Button(column, "TEAM", new Color(0.62f, 0.45f, 0.88f, 0.92f), Vector2.zero, () => Open(Team), 50);
            Place((RectTransform)team.transform, 2, true);
            // A red "+1" bubble on the top-left corner while there are skill points to spend.
            RectTransform badge = UiKit.Box(team.transform, "Points", new Color(0.9f, 0.2f, 0.2f), UiKit.Circle);
            UiKit.Pin(badge, new Vector2(0f, 1f), new Vector2(-14f, 12f), new Vector2(60f, 60f));
            badge.pivot = new Vector2(0f, 1f);
            badge.GetComponent<Image>().raycastTarget = false;
            pointsLabel = UiKit.Label(badge, "", 32, Color.white);
            UiKit.Fill(pointsLabel.rectTransform);
            pointsLabel.raycastTarget = false;
            pointsBadge = badge.gameObject;
            ShowPoints();
            Place((RectTransform)UiKit.Button(column, "PETS", new Color(0.4f, 0.75f, 0.7f, 0.92f), Vector2.zero, () => Open(Pets), 50).transform, 3, true);
            Button play = UiKit.Button(column, "PLAY", new Color(0.35f, 0.7f, 0.95f, 0.92f), Vector2.zero, () => Open(Play, true), 50);
            playLabel = UiKit.TextOf(play);
            playButton = play.gameObject;
            Place((RectTransform)play.transform, 4, true);
            playButton.SetActive(false); // shows up once the whole shop is built

            BuildPauseButton();
        }

        // A square pause button just right of the clock in the top-left corner, and a banner while paused.
        private void BuildPauseButton()
        {
            Button button = UiKit.Button(column, "", new Color(0.15f, 0.15f, 0.2f, 0.75f), Vector2.zero, TogglePause);
            RectTransform rect = (RectTransform)button.transform;
            UiKit.Pin(rect, new Vector2(0f, 1f), new Vector2(Margin + 250f * Scale + 10f, -Margin), new Vector2(120f, 120f));
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Margin + 250f * Scale + 10f, -Margin);
            rect.localScale = Vector3.one * Scale;

            RectTransform bars = UiKit.Group(rect, "Pause");
            UiKit.Fill(bars);
            for (int i = 0; i < 2; i++)
            {
                RectTransform bar = UiKit.Box(bars, "Bar", Color.white);
                UiKit.Pin(bar, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -18f : 18f, 0f), new Vector2(22f, 64f));
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.anchoredPosition = new Vector2(i == 0 ? -18f : 18f, 0f);
                bar.GetComponent<Image>().raycastTarget = false;
            }
            pauseBars = bars.gameObject;

            RectTransform arrow = UiKit.Box(rect, "Play", UiKit.Green, UiKit.Triangle);
            UiKit.Pin(arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            arrow.pivot = new Vector2(0.5f, 0.5f);
            arrow.anchoredPosition = new Vector2(6f, 0f);
            arrow.GetComponent<Image>().raycastTarget = false;
            playTriangle = arrow.gameObject;
            playTriangle.SetActive(false);

            RectTransform banner = UiKit.Box(column, "Paused Banner", new Color(0.15f, 0.15f, 0.2f, 0.85f));
            UiKit.Pin(banner, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 200f));
            banner.pivot = new Vector2(0.5f, 0.5f);
            banner.anchoredPosition = new Vector2(0f, 200f);
            banner.GetComponent<Image>().raycastTarget = false;
            Text title = UiKit.Label(banner, "PAUSED", 80, Color.white);
            UiKit.Fill(title.rectTransform);
            title.rectTransform.offsetMax = new Vector2(0f, -10f);
            title.rectTransform.offsetMin = new Vector2(0f, 60f);
            title.raycastTarget = false;
            Text hint = UiKit.Label(banner, "Tap the play button to resume", 36, new Color(0.8f, 0.8f, 0.88f));
            UiKit.Fill(hint.rectTransform);
            hint.rectTransform.offsetMin = new Vector2(0f, 14f);
            hint.rectTransform.offsetMax = new Vector2(0f, -130f);
            hint.raycastTarget = false;
            pausedBanner = banner.gameObject;
            pausedBanner.SetActive(false);
        }

        private void TogglePause()
        {
            Paused = !Paused;
            if (Paused && popup != null) Close();
            Time.timeScale = Paused ? 0f : 1f;
            pauseBars.SetActive(!Paused);
            playTriangle.SetActive(Paused);
            pausedBanner.SetActive(Paused);
            if (Paused) saveManager?.Save();
        }

        // A slot in the right-hand column (under MENU, GOALS and STATS) or the matching column on the
        // left (under the clock and stars), so the buttons are split evenly across the screen.
        private static void Place(RectTransform rect, int slot, bool left)
        {
            Vector2 anchor = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
            UiKit.Pin(rect, anchor, new Vector2(left ? Margin : -Margin, -Margin - slot * Step), new Vector2(200f, 110f));
            rect.localScale = Vector3.one * Scale;
        }

        // ---------------------------------------------------------------- pop-ups

        // SHOP, TEAM and PETS leave the shop running (Kenny); only the mini-games pause it.
        private void Open(Action<RectTransform, Text> draw, bool pause = false)
        {
            Close();
            tab = 0;
            popup = UiKit.Popup(canvas, "", new Vector2(940f, 1560f), Close, out RectTransform body, out Text title).gameObject;
            MenuLayers.Watch(popup, Close); // in front of the HUD; closes any other menu; tap outside to close
            refresh = () =>
            {
                for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
                draw(body, title);
                HudStyle.Request();
            };
            refresh();
            paused = pause;
            if (pause) Time.timeScale = 0f;
        }

        private void Close()
        {
            refresh = null;
            if (popup == null) return;
            popup.SetActive(false); // right away, so it no longer counts as an open menu
            Destroy(popup);
            popup = null;
            if (paused) Time.timeScale = 1f;
            paused = false;
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

        private void Tabs(RectTransform body, string[] names, int fontSize = 36)
        {
            Button[] tabs = UiKit.Tabs(body, names, i =>
            {
                tab = i;
                Redraw();
            }, fontSize);
            for (int i = 0; i < tabs.Length; i++) tabs[i].GetComponent<Image>().color = i == tab ? UiKit.Pink : UiKit.Gray;
        }

        // ---------------------------------------------------------------- SHOP: collectible decor

        private void DecorShop(RectTransform body, Text title)
        {
            title.text = "DECOR SHOP";
            Tabs(body, new[] { "FURNITURE", "LED SIGNS", "PLANTS", "MISC" }, 30);
            string collection = Collections.Names[tab];

            RectTransform header = Row(body, 100f, 130f);
            header.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            RowText(header, collection, 46, 0f, 1f, Color.white, 0f, 60f);
            RowText(header, $"Each piece makes customers in this shop {Collections.BonusPerPiece * 100f:0}% happier. Now: +{Collections.Bonus * 100f:0}%",
                32, 0f, 1f, new Color(0.6f, 0.95f, 0.55f), 60f, 0f);

            float top = 250f;
            foreach (Collections.Item item in Collections.All)
            {
                if (item.Collection != collection) continue;
                RectTransform row = Row(body, top, 108f);
                top += 118f;
                int here = Collections.CountPlaced(item.Id);
                RowText(row, here > 0 ? $"{item.Name}  x{here}" : item.Name, 36, 0f, 0.68f, Color.white);
                PriceButton(row, item.Price, () =>
                {
                    if (Profile.Pearls < item.Price) return;
                    Close(); // out of the way while it's placed
                    Collections.Buy(item);
                });
            }

            Text hint = UiKit.Label(body, "Earn pearls from daily goals, happy VIPs, mini-games and your pet.", 32,
                new Color(0.8f, 0.8f, 0.88f));
            UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(900f, 110f));
        }

        // ---------------------------------------------------------------- TEAM: staff skill trees

        private void Team(RectTransform body, Text title)
        {
            title.text = "STAFF SKILLS";
            var names = new string[TeamSkills.RoleNames.Length + 1];
            TeamSkills.RoleNames.CopyTo(names, 0);
            names[names.Length - 1] = "KITCHEN"; // the shop's cash upgrades
            Tabs(body, names, 28);
            if (tab >= TeamSkills.RoleNames.Length)
            {
                Kitchen(body);
                return;
            }
            var role = (TeamSkills.Role)tab;
            int level = TeamSkills.Level(role);
            int points = TeamSkills.PointsLeft(role);
            Color soft = new(0.8f, 0.8f, 0.88f);

            RectTransform header = Row(body, 95f, 70f);
            header.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            RowText(header, $"{TeamSkills.Title(role)} team", 42, 0f, 0.6f, Color.white);
            string pointText = TeamSkills.Workers(role) == 0
                ? $"Hire a {TeamSkills.Title(role).ToLowerInvariant()} first"
                : points > 0 ? $"{points} point{(points == 1 ? "" : "s")} to spend!" : "";
            Text pointLabel = RowText(header, pointText, 32, 0.55f, 1f, points > 0 ? new Color(1f, 0.85f, 0.4f) : soft);
            pointLabel.alignment = TextAnchor.MiddleRight;
            XpBar(body, role, level, 175f);

            float top = 262f;
            for (int p = 0; p < 3; p++)
            {
                int perk = p;
                TeamSkills.Perk info = TeamSkills.Perks[tab][p];
                int rank = TeamSkills.Rank(role, p);
                RectTransform row = Row(body, top, 160f);
                top += 170f;
                string pips = new string('\u25CF', rank) + new string('\u25CB', TeamSkills.MaxRank - rank);
                RowText(row, $"{info.Name}   {pips}", 38, 0f, 0.68f, Color.white, 10f, 90f);
                RowText(row, info.Description, 28, 0f, 0.68f, soft, 70f, 8f);
                if (rank >= TeamSkills.MaxRank) RowButton(row, "MAXED", UiKit.Gray, () => { }, false);
                else RowButton(row, "LEARN", UiKit.Purple, () => TeamSkills.Learn(role, perk), points > 0);
            }
            if (role == TeamSkills.Role.Barista)
            {
                Text hint = UiKit.Label(body, "Kitchen upgrades (paid in cash) are on the KITCHEN tab.", 30, soft);
                UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 90f));
            }
        }

        // A chunky level bar: a gold level badge on the left and a glossy purple fill with the XP written on it.
        private static void XpBar(RectTransform body, TeamSkills.Role role, int level, float top)
        {
            const float Height = 64f;
            bool maxed = level >= TeamSkills.MaxLevel;
            float fill = maxed ? 1f : TeamSkills.XpIntoLevel(role) / (float)TeamSkills.XpForNext(level);

            RectTransform frame = UiKit.Box(body, "XP Frame", new Color(0.95f, 0.8f, 0.4f));
            frame.anchorMin = new Vector2(0f, 1f);
            frame.anchorMax = new Vector2(1f, 1f);
            frame.pivot = new Vector2(0.5f, 1f);
            frame.offsetMin = new Vector2(70f, -top - Height);
            frame.offsetMax = new Vector2(-16f, -top);

            RectTransform back = UiKit.Box(frame, "XP Back", new Color(0.12f, 0.1f, 0.2f));
            UiKit.Fill(back, 5f);
            RectTransform bar = UiKit.Box(back, "XP Fill", maxed ? new Color(1f, 0.78f, 0.25f) : new Color(0.72f, 0.45f, 1f));
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(Mathf.Clamp01(Mathf.Max(fill, 0.04f)), 1f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            RectTransform gloss = UiKit.Box(bar, "Gloss", new Color(1f, 1f, 1f, 0.28f));
            gloss.anchorMin = new Vector2(0f, 0.55f);
            gloss.anchorMax = new Vector2(1f, 0.92f);
            gloss.offsetMin = new Vector2(8f, 0f);
            gloss.offsetMax = new Vector2(-8f, 0f);

            string text = maxed
                ? "MAX LEVEL!"
                : $"{TeamSkills.XpIntoLevel(role)} / {TeamSkills.XpForNext(level)} XP  to Lv {level + 1}";
            Text label = UiKit.Label(back, text, 30, Color.white);
            UiKit.Fill(label.rectTransform);
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.05f, 0.2f, 0.9f);

            // The level badge sits over the left end of the bar.
            RectTransform badge = UiKit.Box(body, "Level Badge", new Color(0.95f, 0.8f, 0.4f), UiKit.Circle);
            UiKit.Pin(badge, new Vector2(0f, 1f), new Vector2(16f, -top + 14f), new Vector2(Height + 28f, Height + 28f));
            RectTransform inner = UiKit.Box(badge, "Inner", new Color(0.55f, 0.3f, 0.85f), UiKit.Circle);
            UiKit.Fill(inner, 6f);
            Text lv = UiKit.Label(inner, "Lv", 20, new Color(1f, 0.9f, 0.6f));
            UiKit.Pin(lv.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(60f, 24f));
            Text number = UiKit.Label(inner, level.ToString(), 40, Color.white);
            UiKit.Pin(number.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(60f, 46f));
        }

        // ---------------------------------------------------------------- TEAM > KITCHEN: cash upgrades

        private void Kitchen(RectTransform body)
        {
            Upgrades.UpgradeManager upgrades = Upgrades.UpgradeManager.Instance;
            Color soft = new(0.8f, 0.8f, 0.88f);
            if (upgrades == null || upgrades.All.Count == 0)
            {
                Text none = UiKit.Label(body, "No kitchen upgrades in this shop.", 34, soft);
                UiKit.Pin(none.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 80f));
                return;
            }
            int money = upgrades.Wallet != null ? upgrades.Wallet.Money : 0;
            float top = 100f;
            float height = upgrades.All.Count > 7 ? 130f : 145f;
            foreach (Upgrades.UpgradeDefinition upgrade in upgrades.All)
            {
                if (upgrade == null) continue;
                Upgrades.UpgradeDefinition item = upgrade;
                int level = upgrades.LevelOf(item);
                RectTransform row = Row(body, top, height);
                top += height + 8f;
                string pips = new string('\u25CF', level) + new string('\u25CB', Mathf.Max(0, item.MaxLevel - level));
                RowText(row, $"{item.DisplayName}   {pips}", 34, 0f, 0.68f, Color.white, 8f, height * 0.55f);
                RowText(row, UpgradeInfo.Describe(item, level), 24, 0f, 0.68f, soft, height * 0.42f, 6f);
                int cost = upgrades.NextCost(item);
                if (cost < 0) RowButton(row, "MAX", UiKit.Gray, () => { }, false);
                else RowButton(row, "$" + cost.ToString("N0"), UiKit.Green, () =>
                {
                    upgrades.TryBuy(item);
                    Redraw();
                }, money >= cost);
            }
        }

        // ---------------------------------------------------------------- PETS

        private void Pets(RectTransform body, Text title)
        {
            title.text = "PETS";
            float top = 0f;
            {
                foreach (PetCompanion.Pet pet in PetCompanion.Pets)
                {
                    RectTransform row = Row(body, top, 150f);
                    top += 165f;
                    RowText(row, pet.Name, 40, 0f, 0.68f, Color.white, 10f, 60f);
                    RowText(row, $"Patience +{pet.PatienceBack * 100f:0}%  Tips +{pet.Joy * 100f:0}%  Pet tips {pet.TipChance * 100f:0}%",
                        26, 0f, 0.68f, new Color(0.75f, 0.9f, 0.8f), 80f, 6f);
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
                    "Your pet roams the shop and says hello to customers: they wait longer, tip more, and sometimes tip your pet. It naps on the staff sofa when it's free and sniffs out money (and pearls). Tap IN SHOP to send it home.",
                    32, new Color(0.8f, 0.8f, 0.88f));
                UiKit.Pin(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(900f, 220f));

                // Small print for checking a phone build: which build this is and what the pets are made of.
                Text info = UiKit.Label(body, $"Build {ModelKit.BuildTag} - {PetCompanion.Report} - models: {ModelKit.Report}", 22, new Color(0.6f, 0.6f, 0.68f));
                UiKit.Pin(info.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 50f));
            }
        }

        // ---------------------------------------------------------------- PLAY: mini-games

        private void Play(RectTransform body, Text title)
        {
            title.text = "MINI-GAMES";
            int wait = MiniGames.SecondsUntilReady;
            Text intro = UiKit.Label(body, wait > 0
                    ? $"Your next game is ready in {wait / 60}:{wait % 60:00}."
                    : $"Play a quick game for up to {PearlPrize(1f)} pearls!",
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
            int pearls = PearlPrize(score);
            string verdict = score >= 0.9f ? "AMAZING!" : score >= 0.6f ? "GREAT JOB!" : score >= 0.3f ? "NOT BAD!" : "NICE TRY!";

            Text result = UiKit.Label(body, $"{verdict}\n\nScore {Mathf.RoundToInt(score * 100f)}%\n\n+{pearls} pearl{(pearls == 1 ? "" : "s")}", 56, Color.white);
            UiKit.Pin(result.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(860f, 700f));
            Button collect = UiKit.Button(body, "COLLECT", UiKit.Green, new Vector2(460f, 140f), () =>
            {
                Core.GameEvents.RaiseBonusCollected();
                Profile.AddPearls(pearls, "Mini-game prize!");
                Close();
            }, 56);
            UiKit.Pin((RectTransform)collect.transform, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(460f, 140f));
        }

        // Pearls for a score: 1 for playing, up to 5 for a near-perfect game.
        private static int PearlPrize(float score) => score >= 0.9f ? 5 : score >= 0.6f ? 3 : score >= 0.3f ? 2 : 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            instance = null;
            Paused = false;
        }
    }
}
