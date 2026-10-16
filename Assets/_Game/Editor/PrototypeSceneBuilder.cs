using BubbleTea.Building;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Economy;
using BubbleTea.Feedback;
using BubbleTea.Goals;
using BubbleTea.Player;
using BubbleTea.Rating;
using BubbleTea.Saving;
using BubbleTea.Staff;
using BubbleTea.UI;
using BubbleTea.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Menu item that builds the gray-box prototype scene from scratch, so nobody has to
    /// wire up the player, camera, joystick and cash by hand.
    /// Run it from the menu bar: Bubble Tea > Build Prototype Scene.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Prototype.unity";
        private const string Shop2ScenePath = "Assets/_Game/Scenes/Shop2.unity";
        private const string MenuScenePath = "Assets/_Game/Scenes/MainMenu.unity";
        private const string PrefabFolder = "Assets/_Game/Prefabs";
        private const string MaterialFolder = "Assets/_Game/Materials";

        // What differs between shops. Everything else (layout, pads, staff) is shared.
        private sealed class ShopPlan
        {
            public int Number;
            public string ScenePath;
            public string SaveFile;
            public string FloorMaterial;
            public Color FloorColor;
            public string WallMaterial;
            public Color WallColor;
            public string ExpansionFloorMaterial;
            public Color ExpansionFloorColor;
            public ShopDressing.Style Look;
            public MusicMaker.Track Music;
            public ShopLayout Layout;
        }

        /// <summary>
        /// Where everything stands in a shop. The main room runs from -HalfWidth to +HalfWidth
        /// (west to east) and -HalfDepth to +HalfDepth (front door to back wall); Expand Shop adds
        /// the space from the east wall to the lane wall, with the drive-thru lane outside it.
        /// Every shop uses the same compact plan (see <see cref="CompactLayout"/>).
        /// </summary>
        internal sealed class ShopLayout
        {
            public float HalfWidth, HalfDepth;
            public float DoorFrom, DoorTo;              // the front doorway in the south wall
            public float StaffDoorFrom, StaffDoorTo;    // the gap in the glass wall beside the counter
            public float LaneWallX;                     // the east end of the expansion
            public Vector3 Counter, Seating, DriveThru, TrashCan, FloorCash, PlayerStart;
            public Vector3 TeaBrewer, TaroStation, FruitBar, ExtraBar, UpgradeDesk, Cashier;
            public Vector3 Expansion, Cleaner, DriveThruCashier, SecondShopPad, TravelBack;
            public Vector3 Stocker, SupplyDock, Chalkboard;      // Matcha House only
            public Vector3 StaffSofa;                            // where baristas take their breaks
            public Vector3[] Decor;                              // decor pads, in the order they appear
            public Vector3[] Baristas;
            public float[] Windows, Lamps, ExpansionWindows, ExpansionLamps; // x positions on the back wall
            public Vector3[] Trees;

            public float PartitionZ => Counter.z;        // glass wall between the dining room and the kitchen
            public float LaneX => LaneWallX + 2.5f;      // middle of the drive-thru lane
            public float LaneOuterX => LaneWallX + 6f;   // far edge of the lane
            public float SidewalkZ => -HalfDepth - 3f;
        }

        // Both shops share one compact floor plan, 18 x 15 meters, so nobody walks far:
        //  - the kitchen (north of the counter): every bar in one row against the back wall, a row of
        //    staff pads in front of them (cashier beside the counter's cash, then the baristas), the
        //    upgrade desk against the glass wall right of the counter (tap it to open the upgrades), the floor cash
        //    beside the counter, and the trash can in the front-west corner, well away from the bars;
        //  - the dining room (south): the queue runs from the front door up to the counter, the café
        //    tables sit east of it, and the staff door is at the far east end of the glass wall so
        //    nothing stands in front of it.
        // Pads are 2.4 m squares and bars hand off 1.4 m in front of themselves, so the spacing below
        // keeps every pad, hand-off spot and zone apart.
        private static ShopLayout CompactLayout(Vector3[] bars, Vector3 extraBar) => new()
        {
            HalfWidth = 9f, HalfDepth = 7.5f,
            DoorFrom = -4.5f, DoorTo = 1.5f,
            StaffDoorFrom = 6.2f, StaffDoorTo = 8.6f,
            LaneWallX = 11f,                   // the expansion adds just a 2 m strip on the east side
            Counter = new(-2f, 0f, 0f),
            Seating = new(3f, 0f, -4.6f),      // east of the queue, clear of the staff door
            DriveThru = new(10f, 0f, 4.6f),     // in the 2 m strip; you serve it from the kitchen side
            TrashCan = new(-8.3f, 0f, 0.8f),   // front-west corner of the kitchen, away from the bars
            FloorCash = new(-4.6f, 0f, 0.9f),  // in the kitchen, just left of the counter
            PlayerStart = new(-4.4f, 1f, 1.9f), // clear of the Matcha House's stocker pad
            TeaBrewer = bars[0],
            TaroStation = bars[1],
            FruitBar = bars[2],
            ExtraBar = extraBar,
            UpgradeDesk = new(3f, 0f, 0.55f),  // right of the counter, its back to the glass wall
            Cashier = new(0.6f, 0f, 1.8f),
            Expansion = new(7.6f, 0f, -3.4f),   // by the east wall it opens up, clear of the koi pond
            Cleaner = new(-6.4f, 0f, -4.6f),
            DriveThruCashier = new(10f, 0f, 1.4f), // small pad in the strip, south of the window
            // Both travel stops stand outside on the sidewalk, just right of the gate's east pillar.
            SecondShopPad = new(3.1f, 0f, -9f),
            TravelBack = new(3.1f, 0f, -9f),
            // Matcha House: the supply dock against the west wall (its spot faces into the kitchen), the
            // stocker's small pad right beside it, and the specials chalkboard by the front door.
            SupplyDock = new(-8.1f, 0f, 3.5f),
            Stocker = new(-8.25f, 0f, 1.95f),  // just south of the dock, against the west wall
            Chalkboard = new(-4.2f, 0f, -3.4f),
            // A little back from the glass wall, so the first one clears the upgrade desk's pad.
            // The third sits a little further forward, clear of the drive-thru window's spot.
            Baristas = new Vector3[] { new(3f, 0f, 2.75f), new(5.4f, 0f, 2.75f), new(7.8f, 0f, 2.2f) },
            StaffSofa = new(7.25f, 0f, 6.95f),  // against the back wall, east of the bars, clear of the drive-thru cash
            // Decor pads in the dining room: bamboo and lanterns down the west wall, the lucky cat
            // against the glass wall right of the staff door (facing the camera), the koi pond in the front-east corner and
            // the cherry tree out toward the middle, clear of the queue and the cleaner's pad.
            // Order: bamboo plants, paper lanterns, lucky cat, koi pond, cherry blossom tree.
            Decor = new Vector3[]
            {
                new(-8.3f, 0f, -6.8f), new(-8.3f, 0f, -0.8f), new(8.8f, 0f, -0.55f), new(7.8f, 0f, -6.6f), new(-6.2f, 0f, -2.4f),
            },
            Windows = new[] { -6.2f, 6.2f },
            Lamps = new[] { -4.4f, 4.4f },
            ExpansionWindows = new float[0],
            ExpansionLamps = new[] { 10f },
            Trees = new Vector3[]
            {
                new(-12f, 0f, -9.5f), new(-12f, 0f, -2f), new(-12f, 0f, 5f), new(-12f, 0f, 10f),
                new(-6f, 0f, 10.5f), new(0f, 0f, 11f), new(6f, 0f, 10.5f), new(12f, 0f, 11f), new(18f, 0f, 10.5f),
                new(24f, 0f, 7f), new(24f, 0f, 0f), new(24f, 0f, -7f), new(3f, 0f, -11.5f), new(10f, 0f, -11.5f),
            },
        };

        // Bars sit with their backs 0.5 m off the back wall.
        private const float BarRowZ = 6.4f;

        // Boba Shop: three bars, evenly spread along the back wall.
        private static readonly ShopLayout Small = CompactLayout(
            new Vector3[] { new(-6.6f, 0f, BarRowZ), new(-2.1f, 0f, BarRowZ), new(2.4f, 0f, BarRowZ) },
            Vector3.zero);

        // Matcha House: the same room with a fourth bar (the Tenju Bar) at the east end of the row.
        private static readonly ShopLayout Large = CompactLayout(
            new Vector3[] { new(-6.6f, 0f, BarRowZ), new(-2.73f, 0f, BarRowZ), new(1.13f, 0f, BarRowZ) },
            new Vector3(5f, 0f, BarRowZ));

        /// <summary>The layout of the shop being built (read by <see cref="ShopDressing"/>).</summary>
        internal static ShopLayout Layout { get; private set; } = Large;

        private static readonly ShopPlan FirstShop = new()
        {
            Number = 1,
            ScenePath = ScenePath,
            SaveFile = "save.json",
            FloorMaterial = "Floor",
            FloorColor = new Color(0.86f, 0.66f, 0.50f),          // light honey planks
            WallMaterial = "Wall",
            WallColor = new Color(0.96f, 0.80f, 0.62f),           // pale warm wood boards
            ExpansionFloorMaterial = "ExpansionFloor",
            ExpansionFloorColor = new Color(0.84f, 0.64f, 0.50f),
            Look = new ShopDressing.Style
            {
                ShopName = "Boba Shop",
                Prefix = "Shop1",
                Accent = new Color(0.78f, 0.24f, 0.20f),  // Taiwanese tea house red
                Accent2 = new Color(0.95f, 0.75f, 0.32f), // and gold
                Grass = new Color(0.46f, 0.70f, 0.34f),
            },
            Music = MusicMaker.Track.BobaShop,
            Layout = Small,
        };

        private static readonly ShopPlan SecondShop = new()
        {
            Number = 2,
            ScenePath = Shop2ScenePath,
            SaveFile = "save-shop2.json",
            FloorMaterial = "Shop2Floor",
            FloorColor = new Color(0.90f, 0.74f, 0.54f),          // light oak planks
            WallMaterial = "Shop2Wall",
            WallColor = new Color(0.76f, 0.87f, 0.68f),           // pale sage boards
            ExpansionFloorMaterial = "Shop2ExpansionFloor",
            ExpansionFloorColor = new Color(0.88f, 0.71f, 0.52f),
            Look = new ShopDressing.Style
            {
                ShopName = "Matcha House",
                Prefix = "Shop2",
                Accent = new Color(0.30f, 0.48f, 0.26f),  // deep matcha green
                Accent2 = new Color(0.98f, 0.86f, 0.62f),
                Grass = new Color(0.50f, 0.72f, 0.36f),
                // A Japanese tea room: white paper lanterns, a matcha-green noren, matcha tins, cream lettering.
                Lantern = new Color(0.98f, 0.95f, 0.86f),
                LanternGlow = new Color(1f, 0.85f, 0.55f),
                Noren = new Color(0.32f, 0.47f, 0.24f),
                TeaTin = new Color(0.40f, 0.58f, 0.30f),
                SignText = new Color(0.99f, 0.97f, 0.90f),
            },
            Music = MusicMaker.Track.MatchaHouse,
            Layout = Large,
        };

        [MenuItem("Bubble Tea/Build Prototype Scene")]
        public static void Build() => BuildShop(FirstShop);

        /// <summary>Builds the Matcha House, the second shop that the first one unlocks.</summary>
        [MenuItem("Bubble Tea/Build Second Shop Scene")]
        public static void BuildSecondShop() => BuildShop(SecondShop);

        /// <summary>
        /// Rebuilds the main menu and every shop in one go (asks once). Later shops come first so the
        /// Boba Shop is the scene left open. A new level only needs adding to <see cref="AllShops"/>.
        /// </summary>
        [MenuItem("Bubble Tea/Build All Shops", priority = 0)]
        public static void BuildAllShops()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!EditorUtility.DisplayDialog("Rebuild all shops?",
                    "This rebuilds the main menu and every shop scene, replacing any changes you made to them by hand.", "Rebuild All", "Cancel"))
                return;
            BuildMainMenu();
            foreach (ShopPlan plan in AllShops) BuildShop(plan, ask: false);
        }

        // Every shop, built in this order by Build All Shops.
        private static ShopPlan[] AllShops => new[] { SecondShop, FirstShop };

        /// <summary>Builds the title screen the game opens on (BubbleTea House): Play and Settings.</summary>
        [MenuItem("Bubble Tea/Build Main Menu Scene")]
        public static void BuildMainMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.99f, 0.86f, 0.88f);
            cameraGo.AddComponent<AudioListener>();
            CreateMusic(MusicMaker.Track.BobaShop);
            CreateEventSystem();

            var canvasGo = new GameObject("Menu", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;

            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // A modern sunset gradient, from deep plum at the top to warm peach at the bottom,
            // with two big soft glows and bubbles drifting slowly upward.
            RectTransform background = CreateUiObject("Background", root);
            Stretch(background);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = Color.white;
            backgroundImage.raycastTarget = false;
            background.gameObject.AddComponent<UIGradient>().Set(new Color(0.33f, 0.18f, 0.42f), new Color(0.98f, 0.6f, 0.52f));
            MenuGlow("GlowTop", background, circle, new Vector2(0.95f, 0.88f), 900f, 0.1f);
            MenuGlow("GlowBottom", background, circle, new Vector2(0.02f, 0.22f), 1000f, 0.08f);

            var floaters = new RectTransform[16];
            for (int i = 0; i < floaters.Length; i++)
            {
                float t = Mathf.Repeat(i * 0.618f, 1f);
                bool pearl = i % 4 == 0; // a few dark tapioca pearls among the bubbles
                floaters[i] = MenuGlow(pearl ? "Pearl" : "Bubble", background, circle,
                    new Vector2(Mathf.Repeat(i * 0.37f + 0.05f, 0.95f) + 0.025f, Mathf.Repeat(i * 0.29f, 1f)),
                    28f + 80f * t, pearl ? 0.35f : 0.12f + 0.12f * t, pearl ? new Color(0.2f, 0.1f, 0.08f) : Color.white);
            }

            // The name: "BubbleTea" in big white letters, "HOUSE" spaced out in gold, and a tagline.
            Text title = CreateHudText("Title", root, font, 150, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 200f, 190f, 20f, 20f);
            title.text = "BubbleTea";
            title.color = Color.white;
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.2f, 0.05f, 0.2f, 0.35f);
            titleShadow.effectDistance = new Vector2(0f, -10f);
            Text house = CreateHudText("House", root, font, 66, TextAnchor.MiddleCenter);
            SetTopRow(house.rectTransform, 385f, 90f, 20f, 20f);
            house.text = "H  O  U  S  E";
            house.color = new Color(1f, 0.84f, 0.45f);
            Text tagline = CreateHudText("Tagline", root, font, 38, TextAnchor.MiddleCenter);
            SetTopRow(tagline.rectTransform, 480f, 60f, 20f, 20f);
            tagline.fontStyle = FontStyle.Normal;
            tagline.text = "Brew  \u00B7  Serve  \u00B7  Grow";
            tagline.color = new Color(1f, 1f, 1f, 0.75f);

            // A big cup of milk tea in a soft halo, bobbing in the middle, with a shadow under it.
            MenuShape("Halo", root, circle, new Color(1f, 1f, 1f, 0.12f), new Vector2(680f, 680f), new Vector2(0f, 80f));
            MenuShape("HaloInner", root, circle, new Color(1f, 1f, 1f, 0.12f), new Vector2(540f, 540f), new Vector2(0f, 80f));
            MenuShape("CupShadow", root, circle, new Color(0.2f, 0.05f, 0.15f, 0.2f), new Vector2(320f, 50f), new Vector2(0f, -190f));
            RectTransform cup = CreateUiObject("Cup", root);
            cup.anchorMin = cup.anchorMax = new Vector2(0.5f, 0.5f);
            cup.anchoredPosition = new Vector2(0f, 80f);
            cup.sizeDelta = new Vector2(360f, 480f);
            cup.localScale = Vector3.one * 0.85f;
            RectTransform straw = MenuShape("Straw", cup, rounded, new Color(0.98f, 0.5f, 0.58f), new Vector2(46f, 380f), new Vector2(60f, 250f));
            straw.localRotation = Quaternion.Euler(0f, 0f, -12f);
            RectTransform body = MenuShape("Body", cup, rounded, Color.white, new Vector2(340f, 440f), new Vector2(0f, -20f));
            body.gameObject.AddComponent<UIGradient>().Set(new Color(0.93f, 0.8f, 0.66f), new Color(0.8f, 0.6f, 0.44f));
            MenuShape("Shine", cup, rounded, new Color(1f, 1f, 1f, 0.35f), new Vector2(40f, 300f), new Vector2(-115f, 10f));
            for (int i = 0; i < 9; i++)
            {
                float x = -110f + (i % 5) * 55f + (i >= 5 ? 27f : 0f);
                float y = i >= 5 ? -150f : -195f;
                MenuShape("Pearl", cup, circle, new Color(0.27f, 0.16f, 0.12f), new Vector2(56f, 56f), new Vector2(x, y));
            }
            MenuShape("Lid", cup, rounded, new Color(1f, 0.97f, 0.93f), new Vector2(390f, 60f), new Vector2(0f, 210f));

            // A frosted glass card holding the buttons.
            RectTransform card = MenuShape("Card", root, rounded, new Color(1f, 1f, 1f, 0.16f), new Vector2(880f, 520f), Vector2.zero);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0f);
            card.pivot = new Vector2(0.5f, 0f);
            card.anchoredPosition = new Vector2(0f, 150f);
            card.GetComponent<Image>().pixelsPerUnitMultiplier = 0.3f; // rounder corners

            // Play: a glossy pink pill that gently breathes. Settings: a frosted pill under it.
            Button play = CreateMenuButton("Play", root, "PLAY", rounded, font, Color.white, out Text playLabel);
            var playRect = (RectTransform)play.transform;
            playRect.anchorMin = playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.anchoredPosition = new Vector2(0f, 500f);
            playRect.sizeDelta = new Vector2(700f, 170f);
            var playImage = play.GetComponent<Image>();
            playImage.pixelsPerUnitMultiplier = 0.12f; // fully rounded ends
            play.gameObject.AddComponent<UIGradient>().Set(new Color(1f, 0.56f, 0.6f), new Color(0.93f, 0.33f, 0.5f));
            var playShadow = play.gameObject.AddComponent<Shadow>();
            playShadow.effectColor = new Color(0.35f, 0.05f, 0.2f, 0.3f);
            playShadow.effectDistance = new Vector2(0f, -10f);
            playLabel.fontSize = 76;

            Button settingsButton = CreateMenuButton("SettingsButton", root, "SETTINGS", rounded, font,
                new Color(1f, 1f, 1f, 0.22f), out Text settingsLabel);
            var settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(0.5f, 0f);
            settingsRect.anchoredPosition = new Vector2(0f, 300f);
            settingsRect.sizeDelta = new Vector2(700f, 130f);
            settingsButton.GetComponent<Image>().pixelsPerUnitMultiplier = 0.15f;
            settingsLabel.fontSize = 46;

            Text footer = CreateHudText("Footer", root, font, 30, TextAnchor.MiddleCenter);
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            footer.rectTransform.anchoredPosition = new Vector2(0f, 80f);
            footer.rectTransform.sizeDelta = new Vector2(900f, 50f);
            footer.fontStyle = FontStyle.Normal;
            footer.text = "Made with boba \u2665";
            footer.color = new Color(1f, 1f, 1f, 0.6f);

            var menu = canvasGo.AddComponent<MainMenu>();
            SetReference(menu, "playButton", play);
            SetReference(menu, "playLabel", playLabel);
            SetReference(menu, "bobbing", cup);
            SetReference(menu, "pulsing", playRect);
            SetReferences(menu, "floaters", floaters);

            // The settings panel: sound, music and vibration (reset and GM tools stay in the shop).
            RectTransform settingsRoot = CreateUiObject("Settings", root);
            Stretch(settingsRoot);
            var settings = settingsRoot.gameObject.AddComponent<SettingsMenu>();
            RectTransform panel = CreateUiObject("Panel", settingsRoot);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.gameObject.AddComponent<FitToScreen>();
            panel.sizeDelta = new Vector2(800f, 760f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
            panelImage.pixelsPerUnitMultiplier = 0.3f;
            panel.gameObject.AddComponent<UIGradient>().Set(new Color(0.38f, 0.22f, 0.48f, 0.97f), new Color(0.26f, 0.14f, 0.34f, 0.97f));
            Text settingsTitle = CreateHudText("Title", panel, font, 60, TextAnchor.MiddleCenter);
            SetTopRow(settingsTitle.rectTransform, 20f, 110f, 0f, 0f);
            settingsTitle.text = "SETTINGS";
            var green = new Color(0.35f, 0.78f, 0.38f);
            Button sound = CreateMenuButton("Sound", panel, "Sound: ON", rounded, font, green, out Text soundLabel);
            SetTopRow((RectTransform)sound.transform, 160f, 120f, 60f, 60f);
            Button music = CreateMenuButton("Music", panel, "Music: ON", rounded, font, green, out Text musicLabel);
            SetTopRow((RectTransform)music.transform, 310f, 120f, 60f, 60f);
            Button vibration = CreateMenuButton("Vibration", panel, "Vibration: ON", rounded, font, green, out Text vibrationLabel);
            SetTopRow((RectTransform)vibration.transform, 460f, 120f, 60f, 60f);
            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            SetTopRow((RectTransform)close.transform, 610f, 110f, 200f, 200f);
            SetReference(settings, "panel", panel.gameObject);
            SetReference(settings, "openButton", settingsButton);
            SetReference(settings, "closeButton", close);
            SetReference(settings, "soundButton", sound);
            SetReference(settings, "soundLabel", soundLabel);
            SetReference(settings, "musicButton", music);
            SetReference(settings, "musicLabel", musicLabel);
            SetReference(settings, "vibrationButton", vibration);
            SetReference(settings, "vibrationLabel", vibrationLabel);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
            AddSceneToBuild(MenuScenePath, 0);
            PlayFromMenu.Apply();
            Debug.Log($"Bubble Tea: main menu built at {MenuScenePath}. It's first in the build, so the game opens on it.");
        }

        // A soft round glow or bubble on the main menu background, placed by screen fraction.
        private static RectTransform MenuGlow(string name, Transform parent, Sprite circle, Vector2 anchor, float size, float alpha,
            Color? color = null)
        {
            RectTransform rect = CreateUiObject(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(size, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = circle;
            Color tint = color ?? Color.white;
            image.color = new Color(tint.r, tint.g, tint.b, alpha);
            image.raycastTarget = false;
            return rect;
        }

        // A rounded shape (or circle) on the main menu.
        private static RectTransform MenuShape(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 position)
        {
            RectTransform rect = CreateUiObject(name, parent);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static void BuildShop(ShopPlan plan, bool ask = true)
        {
            if (ask && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (ask && AssetDatabase.LoadAssetAtPath<SceneAsset>(plan.ScenePath) != null &&
                !EditorUtility.DisplayDialog("Rebuild shop scene?",
                    $"{plan.ScenePath} already exists. Rebuilding replaces it (and any changes you made to it).",
                    "Rebuild", "Cancel"))
            {
                return;
            }

            Layout = plan.Layout;
            // Colors are set every build (the materials may already exist with older colors).
            Material floorMat = GetOrCreateMaterial(plan.FloorMaterial, plan.FloorColor);
            floorMat.color = plan.FloorColor;
            Material expansionFloorMat = GetOrCreateMaterial(plan.ExpansionFloorMaterial, plan.ExpansionFloorColor);
            expansionFloorMat.color = plan.ExpansionFloorColor;
            Material playerMat = GetOrCreateMaterial("Player", new Color(0.36f, 0.62f, 0.95f));
            Material cashMat = GetOrCreateMaterial("Cash", new Color(0.35f, 0.78f, 0.38f));
            Material padMat = GetOrCreateMaterial("CashSpot", new Color(0.80f, 0.73f, 0.62f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CashBill billPrefab = CreateBillPrefab(cashMat);

            CreateLight();
            CreateFloor(floorMat);
            Material wallMat = GetOrCreateMaterial(plan.WallMaterial, plan.WallColor);
            wallMat.color = plan.WallColor;
            GameObject eastWall = CreateWalls(wallMat);
            GameObject expansionArea = CreateExpansionArea(wallMat,
                GetOrCreateMaterial(plan.ExpansionFloorMaterial, plan.ExpansionFloorColor));
            ShopDressing.Dress(plan.Look, floorMat, GetOrCreateMaterial(plan.ExpansionFloorMaterial, plan.ExpansionFloorColor),
                wallMat, expansionArea);
            CreateMusic(plan.Music);

            // The Matcha House dresses the player in its green uniform (if the Art Set has it).
            string playerSlot = plan.Number > 1 && ArtLibrary.Load().Get("MatchaPlayer") != null ? "MatchaPlayer" : "Player";
            GameObject player = CreatePlayer(playerMat, playerSlot, plan.Number > 1, out Wallet wallet);
            Camera camera = CreateCamera(player.transform);
            VirtualJoystick joystick = CreateHud(wallet, out Transform hud);
            CreateEventSystem();

            SetReference(player.GetComponent<PlayerMovement>(), "joystick", joystick);
            SetReference(player.GetComponent<PlayerMovement>(), "cameraTransform", camera.transform);

            CashPile[] piles = CreateCashPiles(billPrefab, padMat, plan.Number);
            CreateTrashCan(padMat);
            CashPileRefiller floorCash = CreateRefiller(piles, plan.Number);
            ShopContentBuilder.Content content = ShopContentBuilder.Build(billPrefab, plan.Number);
            UnlockPad[] pads = CreateUnlockPads(content, billPrefab, wallet, eastWall, expansionArea);
            if (content.TravelBack != null)
            {
                var stop = (GameObject)PrefabUtility.InstantiatePrefab(content.TravelBack);
                stop.transform.position = Layout.TravelBack;
            }
            // Customers come in off the sidewalk through the front door and leave the same way.
            float street = Layout.SidewalkZ;
            CustomerSpawner mainSpawner = CreateCustomerSpawner("CustomerSpawner", "Main", content.CustomerPrefab,
                new Vector3(Layout.Counter.x, 0f, street), new Vector3((Layout.DoorFrom + Layout.DoorTo) / 2f, 0f, street),
                secondsBetween: 3f, max: 4);
            // Their order bubble and patience start as they walk in through the front door.
            var doorSettings = new SerializedObject(mainSpawner);
            doorSettings.FindProperty("startOrdersAtDoor").boolValue = true;
            doorSettings.FindProperty("doorZ").floatValue = -Layout.HalfDepth;
            doorSettings.ApplyModifiedPropertiesWithoutUndo();
            // Named VIP regulars who drop by once a shop day for their favorite drink.
            var vips = new GameObject("VipRegulars").AddComponent<VipRegulars>();
            SetReference(vips, "spawner", mainSpawner);
            if (plan.Number > 1)
            {
                var vipSettings = new SerializedObject(vips);
                SerializedProperty vipNames = vipSettings.FindProperty("names");
                string[] matchaRegulars = { "Yui", "Ren", "Sakura", "Haru", "Nana" };
                vipNames.arraySize = matchaRegulars.Length;
                for (int i = 0; i < matchaRegulars.Length; i++) vipNames.GetArrayElementAtIndex(i).stringValue = matchaRegulars[i];
                vipSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            // The Matcha House's own events: tea critics, tour buses, the daily special and supply deliveries.
            if (plan.Number > 1)
            {
                var criticSettings = new SerializedObject(mainSpawner);
                criticSettings.FindProperty("criticChance").floatValue = 0.08f;
                criticSettings.ApplyModifiedPropertiesWithoutUndo();
                CreateTourBus(mainSpawner);
                CreateDailySpecialBoard();
            }
            if (content.Crate != null) CreateSupplyDelivery(content.Crate, padMat);
            CreateStaffSofa();
            // Cars turn in from the road, drive up the lane past the window and out the far end.
            CreateCustomerSpawner("CarSpawner", "DriveThru", content.CarPrefab,
                new Vector3(Layout.LaneX, 0f, street - 4f), new Vector3(Layout.LaneX, 0f, Layout.HalfDepth + 9f),
                secondsBetween: 6f, max: 3);
            UpgradeManager upgrades = CreateUpgradeManager(wallet, content.Upgrades);
            CreateUpgradePanel(hud, upgrades, content.Upgrades);
            OfflineEarnings offlineEarnings = null; // no reward for time away from the game
            new GameObject("ShopRecords").AddComponent<ShopRecords>(); // lifetime totals for goals and stats
            new GameObject("Payroll").AddComponent<Payroll>(); // pays the hired workers every shop hour
            DailyGoals dailyGoals = CreateDailyGoals(wallet, content.Menu, offlineEarnings);
            ShopRating rating = CreateShopRating(pads, content);
            SaveManager saveManager = CreateSaveManager(wallet, pads, upgrades, offlineEarnings, dailyGoals, rating, floorCash);
            var saveFile = new SerializedObject(saveManager);
            saveFile.FindProperty("fileName").stringValue = plan.SaveFile;
            saveFile.ApplyModifiedPropertiesWithoutUndo();
            CreateFeedback();
            CreateSettingsMenu(hud, saveManager, wallet, upgrades);
            CreateGoalsPanel(hud, dailyGoals);
            CreateStatsPanel(hud, content.Menu);
            CreateClock(hud);
            CreatePricePanel(hud, content.Menu);
            CreateRatingView(hud, rating);
            CreatePriceyAlert(hud);
            CreateStaffUnpaidAlert(hud);
            CreateAnnouncementBanner(hud);
            CreateGameOverPopup(hud, rating, saveManager);

            EditorSceneManager.SaveScene(scene, plan.ScenePath);
            // The first shop goes first in the build, right after the main menu if there is one.
            EditorBuildSettingsScene[] inBuild = EditorBuildSettings.scenes;
            bool menuFirst = inBuild.Length > 0 && inBuild[0].path == MenuScenePath;
            AddSceneToBuild(plan.ScenePath, plan.Number == 1 ? (menuFirst ? 1 : 0) : -1);
            Selection.activeGameObject = player;

            Debug.Log($"Bubble Tea: shop {plan.Number} built at {plan.ScenePath}. Press Play to try it.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) == null)
            {
                Debug.Log("Bubble Tea: click Bubble Tea > Build Main Menu Scene so the game (and Play) starts at the main menu.");
            }
            if (plan.Number == 1 && AssetDatabase.LoadAssetAtPath<SceneAsset>(Shop2ScenePath) == null)
            {
                Debug.Log("Bubble Tea: the second shop isn't built yet. Click Bubble Tea > Build Second Shop Scene " +
                          "so its bus stop has somewhere to go.");
            }
        }

        private static void CreateLight()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.95f, 0.86f); // bright, warm daylight
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;
            go.transform.rotation = Quaternion.Euler(55f, -35f, 0f);

            // Sky, horizon and ground colors light the shop from all sides, bright and warm.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.96f, 0.93f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.90f, 0.80f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.64f, 0.56f, 0.50f);
        }

        private static void CreateFloor(Material material)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(Layout.HalfWidth / 5f, 1f, Layout.HalfDepth / 5f); // a plane is 10 x 10 meters
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        // Low walls around the shop so nobody walks off the edge. Returns the east wall,
        // which the Expand Shop pad removes.
        private static GameObject CreateWalls(Material wallMat)
        {
            var root = new GameObject("Walls");
            float w = Layout.HalfWidth, d = Layout.HalfDepth, from = Layout.DoorFrom, to = Layout.DoorTo;
            // The back wall is tall (windows, sign); the others stay low so they never hide the shop.
            // The front (south) wall has a wide doorway. The player can step out onto a patch of
            // sidewalk in front of it (to reach the travel stop), fenced by invisible walls
            // (customers don't use physics, so they walk through them).
            CreateWall(root.transform, "North", new Vector3(0f, 1.5f, d + 0.25f), new Vector3(2f * w + 0.5f, 3f, 0.5f), wallMat);
            CreateWall(root.transform, "South (west of door)", new Vector3((-w - 0.25f + from) / 2f, 0.5f, -d - 0.25f),
                new Vector3(from + w + 0.25f, 1f, 0.5f), wallMat);
            CreateWall(root.transform, "South (east of door)", new Vector3((w + 0.25f + to) / 2f, 0.5f, -d - 0.25f),
                new Vector3(w + 0.25f - to, 1f, 0.5f), wallMat);
            float yardEast = to + 2.6f, yardSouth = -d - 2.6f;
            CreateBlocker(root.transform, "Front Yard (west)", new Vector3(from - 0.25f, 1f, (-d + yardSouth) / 2f),
                new Vector3(0.5f, 2f, -d - yardSouth));
            CreateBlocker(root.transform, "Front Yard (east)", new Vector3(yardEast + 0.25f, 1f, (-d + yardSouth) / 2f),
                new Vector3(0.5f, 2f, -d - yardSouth));
            CreateBlocker(root.transform, "Front Yard (south)", new Vector3((from + yardEast) / 2f, 1f, yardSouth - 0.25f),
                new Vector3(yardEast - from + 1f, 2f, 0.5f));
            // The sidewalk outside has no collider of its own, so the yard gets an invisible floor.
            CreateBlocker(root.transform, "Front Yard (floor)", new Vector3((from + yardEast) / 2f, -0.25f, (-d + yardSouth) / 2f),
                new Vector3(yardEast - from + 1f, 0.5f, -d - yardSouth + 1f));
            CreateWall(root.transform, "West", new Vector3(-w - 0.25f, 0.5f, 0f), new Vector3(0.5f, 1f, 2f * d + 0.5f), wallMat);
            return CreateWall(root.transform, "East (opens with Expand Shop)", new Vector3(w + 0.25f, 0.5f, 0f),
                new Vector3(0.5f, 1f, 2f * d + 0.5f), wallMat);
        }

        // The extra space to the east, hidden until Expand Shop is bought.
        private static GameObject CreateExpansionArea(Material wallMat, Material expansionFloor)
        {
            var root = new GameObject("Expansion Area");
            float w = Layout.HalfWidth, d = Layout.HalfDepth, laneWall = Layout.LaneWallX;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3((w + Layout.LaneOuterX) / 2f, 0f, 0f);
            floor.transform.localScale = new Vector3((Layout.LaneOuterX - w) / 10f, 1f, d / 5f);
            floor.GetComponent<Renderer>().sharedMaterial = expansionFloor;

            // The building ends at the lane wall; the lane outside runs the full length, open at both ends.
            float buildingLength = laneWall - w;
            CreateWall(root.transform, "North", new Vector3(w + buildingLength / 2f, 1.5f, d + 0.25f), new Vector3(buildingLength, 3f, 0.5f), wallMat);
            CreateWall(root.transform, "South", new Vector3(w + buildingLength / 2f, 0.5f, -d - 0.25f), new Vector3(buildingLength, 1f, 0.5f), wallMat);
            // The lane wall, with a gap for the drive-thru window.
            float windowZ = Layout.DriveThru.z;
            float windowFrom = windowZ - 1.5f, windowTo = windowZ + 1.5f, end = d + 0.5f;
            CreateWall(root.transform, "Lane Wall (south)", new Vector3(laneWall + 0.25f, 1.2f, (-end + windowFrom) / 2f),
                new Vector3(0.5f, 2.4f, windowFrom + end), wallMat);
            CreateWall(root.transform, "Lane Wall (north)", new Vector3(laneWall + 0.25f, 1.2f, (windowTo + end) / 2f),
                new Vector3(0.5f, 2.4f, end - windowTo), wallMat);
            CreateBlocker(root.transform, "Window (keeps the player in)", new Vector3(laneWall + 0.25f, 1f, windowZ),
                new Vector3(0.5f, 2f, 3f));

            root.SetActive(false);
            return root;
        }

        // An invisible wall: only the player (who moves with physics) bumps into it.
        private static void CreateBlocker(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<BoxCollider>().size = size;
        }

        // Turns a pad (and what it builds) to face another way, keeping its label readable.
        private static void Turn(UnlockPad pad, float yaw)
        {
            pad.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            Transform label = pad.transform.Find("Label");
            if (label != null) label.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private static GameObject CreateWall(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = size;
            wall.GetComponent<Renderer>().sharedMaterial = material;
            return wall;
        }

        private static GameObject CreatePlayer(Material material, string artSlot, bool matchaTray, out Wallet wallet)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = Layout.PlayerStart;
            player.GetComponent<Renderer>().sharedMaterial = material;

            // The CharacterController brings its own collider.
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = Vector3.zero;

            // A small block on the front so you can see which way the player faces.
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "FacingMarker";
            Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
            nose.transform.SetParent(player.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.4f, 0.5f);
            nose.transform.localScale = new Vector3(0.3f, 0.2f, 0.2f);
            nose.GetComponent<Renderer>().sharedMaterial = material;

            // A character model, if the Art Set has one. The capsule stays as the collider.
            GameObject model = ArtLibrary.Attach(artSlot, player.transform, new Vector3(0.8f, 1.8f, 0.8f),
                ArtLibrary.Fit.Height, new Vector3(0f, -1f, 0f));
            if (model != null)
            {
                ArtLibrary.HideGrayBox(player, nose);
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>()) renderer.enabled = true;
                var bob = player.AddComponent<WalkBob>();
                SetReference(bob, "visual", model.transform);
            }

            player.AddComponent<PlayerMovement>();
            wallet = player.AddComponent<Wallet>();
            var collector = player.AddComponent<CashCollector>();
            SetReference(collector, "wallet", wallet);

            // Items the player carries ride on a tray in front of them.
            var hands = new GameObject("HandStack").transform;
            hands.SetParent(player.transform, false);
            hands.localPosition = new Vector3(0f, -0.22f, 0.55f); // the tray rests on the player's hands
            var carrier = player.AddComponent<ItemCarrier>();
            var serializedCarrier = new SerializedObject(carrier);
            serializedCarrier.FindProperty("stack.anchor").objectReferenceValue = hands;
            serializedCarrier.FindProperty("stack.capacity").intValue = 1; // +1 per Carry More level, like baristas
            // Drinks ride side by side on a tray instead of piling up.
            SetStackGrid(serializedCarrier, "stack", 3, 2, TraySpacing, TrayDrinkSize);
            serializedCarrier.FindProperty("tray").objectReferenceValue = AddTray(hands, matchaTray);
            serializedCarrier.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        private static Camera CreateCamera(Transform player)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.48f, 0.70f, 0.36f); // grass, past the edge of the ground
            camera.allowMSAA = true;
            go.AddComponent<AudioListener>();

            var follow = go.AddComponent<CameraFollow>();
            SetReference(follow, "target", player);

            // Place it now too, so the Scene and Game views look right before pressing Play.
            go.transform.position = player.position + new Vector3(0f, 13f, -9f);
            go.transform.LookAt(player.position);
            CozyLook.AddTo(camera);
            return camera;
        }

        // Draws a 512px anti-aliased circle into a PNG and imports it as a UI sprite. The ring is a
        // soft disc with a bright rim; the knob is a solid disc with a darker edge and a top highlight.
        private static Sprite JoystickSprite(string name, bool ring)
        {
            const string folder = "Assets/_Game/Art/Generated";
            const int size = 512;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 4f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center, dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = Mathf.Clamp01(radius - d + 0.5f); // 1 inside, fading over one pixel at the edge
                    Color c;
                    if (ring)
                    {
                        float rim = Mathf.Clamp01(1f - Mathf.Abs(d - (radius - 9f)) / 9f);
                        c = Color.Lerp(new Color(0.15f, 0.15f, 0.18f, 0.35f), new Color(1f, 1f, 1f, 1f), rim);
                    }
                    else
                    {
                        float edge = Mathf.Clamp01((d - (radius - 14f)) / 14f);
                        float shine = Mathf.Clamp01(dy / radius) * 0.12f;
                        float shade = Mathf.Lerp(1f, 0.82f, edge) + shine;
                        c = new Color(shade, shade, shade, 1f);
                    }
                    c.a *= inside;
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = $"{folder}/{name}.png";
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Trilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static VirtualJoystick CreateHud(Wallet wallet, out Transform hud)
        {
            var canvasGo = new GameObject("HUD", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Joystick: an invisible full-screen touch area, with the stick drawn where you touch.
            RectTransform area = CreateUiObject("JoystickArea", canvasGo.transform);
            Stretch(area);
            var areaImage = area.gameObject.AddComponent<Image>();
            areaImage.color = new Color(0f, 0f, 0f, 0f);

            // Smooth, high-resolution stick art (the built-in knob sprite is tiny and looks blurry on a phone).
            RectTransform ring = CreateUiObject("Background", area);
            ring.sizeDelta = new Vector2(250f, 250f);
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = JoystickSprite("JoystickRing", true);
            ringImage.color = new Color(1f, 1f, 1f, 0.6f);
            ringImage.raycastTarget = false;

            RectTransform knob = CreateUiObject("Handle", ring);
            knob.sizeDelta = new Vector2(115f, 115f);
            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = JoystickSprite("JoystickKnob", false);
            knobImage.color = new Color(1f, 1f, 1f, 0.92f);
            knobImage.raycastTarget = false;

            var joystick = area.gameObject.AddComponent<VirtualJoystick>();
            SetReference(joystick, "background", ring);
            SetReference(joystick, "handle", knob);

            // Money counter at the top of the screen.
            // Everything except the joystick lives inside the safe area, clear of the iPhone notch.
            RectTransform safeArea = CreateUiObject("SafeArea", canvasGo.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<BubbleTea.UI.SafeArea>();
            hud = safeArea;

            // The top bar is kept small so it doesn't hide the shop: money at the top center,
            // clock and stars down the top-left, MENU and GOALS down the top-right.
            RectTransform panel = CreateUiObject("MoneyCounter", safeArea);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -HudMargin);
            panel.sizeDelta = new Vector2(330f, 120f);
            panel.localScale = Vector3.one * HudScale;
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.75f);
            panelImage.raycastTarget = false;

            RectTransform labelRect = CreateUiObject("Amount", panel);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 72;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "$0";
            label.raycastTarget = false;

            var counter = panel.gameObject.AddComponent<MoneyCounter>();
            SetReference(counter, "wallet", wallet);
            SetReference(counter, "label", label);

            return joystick;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static CashPile[] CreateCashPiles(CashBill billPrefab, Material padMaterial, int shopNumber)
        {
            Vector3[] spots = { Layout.FloorCash };
            var piles = new CashPile[spots.Length];
            for (int i = 0; i < piles.Length; i++)
            {
                var go = new GameObject($"CashPile {i + 1}");
                go.transform.position = spots[i];

                // A flat tile under the pile so you can see where cash lands.
                GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spot.name = "Spot";
                Object.DestroyImmediate(spot.GetComponent<BoxCollider>());
                spot.transform.SetParent(go.transform, false);
                spot.transform.localPosition = new Vector3(0f, 0.005f, 0f);
                spot.transform.localScale = new Vector3(1.4f, 0.01f, 1.2f);
                spot.GetComponent<Renderer>().sharedMaterial = padMaterial;

                piles[i] = go.AddComponent<CashPile>();
                SetReference(piles[i], "billPrefab", billPrefab);
                // Starts empty: the refiller drops the first bills right away and counts them
                // toward its $500 limit.
                var pileSettings = new SerializedObject(piles[i]);
                pileSettings.FindProperty("startingBills").intValue = 0;
                // Bigger bills, fewer ticks. The Matcha House floor pays $20 bills (5 per refill = $100).
                pileSettings.FindProperty("valuePerBill").intValue = shopNumber > 1 ? 20 : 10;
                pileSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            return piles;
        }

        // Keeps the floor money topped up until it has handed out $500 in total.
        private static CashPileRefiller CreateRefiller(CashPile[] piles, int shopNumber)
        {
            var go = new GameObject("FloorCash");
            var refiller = go.AddComponent<CashPileRefiller>();

            var serialized = new SerializedObject(refiller);
            SerializedProperty list = serialized.FindProperty("piles");
            list.arraySize = piles.Length;
            for (int i = 0; i < piles.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = piles[i];
            }
            // $30 a refill in the Boba Shop ($10 x 3), $100 in the Matcha House ($20 x 5).
            serialized.FindProperty("billsPerRefill").intValue = shopNumber > 1 ? 5 : 3;

            // "$30/$500" floating over the first pile: money lying there now / money still to collect.
            if (piles.Length > 0)
            {
                var labelGo = new GameObject("Amount Left");
                labelGo.transform.SetParent(piles[0].transform, false);
                labelGo.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                labelGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f); // faces the camera
                var text = labelGo.AddComponent<TextMesh>();
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.font = font;
                labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
                text.fontSize = 64;
                text.characterSize = 0.05f;
                text.fontStyle = FontStyle.Bold;
                text.anchor = TextAnchor.MiddleCenter;
                text.alignment = TextAlignment.Center;
                text.color = new Color(0.25f, 0.6f, 0.25f);
                serialized.FindProperty("label").objectReferenceValue = text;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return refiller;
        }

        // Pads in a chain, a few new things at a time like levels in an RPG:
        //   counter -> tea brewer -> first barista + upgrade desk + topping bar;
        //   topping bar -> taro station + seating area + cashier; seating area -> cleaner;
        //   each barista -> the next; taro station -> fruit bar -> Expand Shop, which opens
        //   the east wall and reveals the drive-through and more seating.
        //
        // The floor plan works like a real café, seen from the street (south):
        //   - the dining room at the front: the door, the line at the counter, café tables;
        //   - the counter across the middle, with a glass wall either side and a staff door;
        //   - the kitchen behind it: the bars in a row facing the counter, the baristas' pads
        //     along the back wall, the upgrade desk, the trash can and the starting cash;
        //   - Expand Shop adds more kitchen and dining space to the east, and a drive-thru
        //     window onto a car lane outside the east wall.
        private static UnlockPad[] CreateUnlockPads(ShopContentBuilder.Content content, CashBill billPrefab, Wallet wallet,
            GameObject eastWall, GameObject expansionArea)
        {
            Material padMat = GetOrCreateMaterial("UnlockPad", new Color(0.97f, 0.97f, 0.97f));

            // Shop 1 only: once everything else in the shop is built, a pad opens level 2 (see below).
            UnlockPad secondShopPad = content.SecondShop != null
                ? CreatePad(content.SecondShop, Layout.SecondShopPad, billPrefab, wallet, padMat)
                : null;
            if (secondShopPad != null) ResizePad(secondShopPad, 1.6f); // small, to fit on the sidewalk by the gate
            // The drive-thru cashier's pad sits beside the drive-thru's cash pile, like the main one.
            UnlockPad driveThruCashierPad = CreatePad(content.DriveThruCashier, Layout.DriveThruCashier, billPrefab, wallet, padMat);
            ResizePad(driveThruCashierPad, 1.5f);
            // The drive-thru window sits in the lane wall, its customer side facing the cars outside.
            UnlockPad driveThruPad = CreatePad(content.DriveThru, Layout.DriveThru, billPrefab, wallet, padMat,
                driveThruCashierPad);
            Turn(driveThruPad, -90f);
            UnlockPad expansionPad = CreatePad(content.Expansion, Layout.Expansion, billPrefab, wallet, padMat,
                driveThruPad);
            SetObjectList(expansionPad, "showWhenUnlocked", expansionArea);
            SetObjectList(expansionPad, "hideWhenUnlocked", eastWall);
            // The bars stand in a row behind the counter, turned so you serve them from the counter side.
            // A shop with a fifth bar (the Matcha House's Tenju Bar) unlocks it after the fruit bar (the Sencha Bar).
            UnlockPad extraPad = content.ExtraBar != null
                ? CreatePad(content.ExtraBar, Layout.ExtraBar, billPrefab, wallet, padMat)
                : null;
            if (extraPad != null) Turn(extraPad, 180f);
            UnlockPad fruitPad = extraPad != null
                ? CreatePad(content.FruitBar, Layout.FruitBar, billPrefab, wallet, padMat, expansionPad, extraPad)
                : CreatePad(content.FruitBar, Layout.FruitBar, billPrefab, wallet, padMat, expansionPad);
            Turn(fruitPad, 180f);
            UnlockPad taroPad = CreatePad(content.TaroStation, Layout.TaroStation, billPrefab, wallet, padMat, fruitPad);
            Turn(taroPad, 180f);

            // The decorations (Planter, Menu Board, Boba Statue) are left out for now; their
            // prefabs and data are still built, so they can come back as pads later.
            // Dining room: café tables beside the line, the cleaner's pad right beside them.
            UnlockPad cleanerPad = CreatePad(content.Cleaner, Layout.Cleaner, billPrefab, wallet, padMat);
            UnlockPad seatingPad = CreatePad(content.Seating, Layout.Seating, billPrefab, wallet, padMat, cleanerPad);
            // The cashier stands behind the counter beside its cash pile.
            UnlockPad cashierPad = CreatePad(content.Cashier, Layout.Cashier, billPrefab, wallet, padMat);
            // Baristas in the row of staff pads in front of the bars, each pad revealing the next. Hiring
            // the first one also brings out the next bar, the café tables and the cashier.
            // Matcha House: hiring the first barista also brings out the stocker's pad.
            // A smaller pad than the rest, right beside the supply dock.
            UnlockPad stockerPad = content.Stocker != null
                ? CreatePad(content.Stocker, Layout.Stocker, billPrefab, wallet, padMat)
                : null;
            if (stockerPad != null) ResizePad(stockerPad, 1.3f);
            Vector3[] baristaSpots = Layout.Baristas;
            var baristaPads = new UnlockPad[content.Baristas.Length];
            for (int i = baristaPads.Length - 1; i >= 0; i--)
            {
                var reveals = new System.Collections.Generic.List<UnlockPad>();
                if (i + 1 < baristaPads.Length) reveals.Add(baristaPads[i + 1]);
                if (i == 0) reveals.AddRange(new[] { taroPad, seatingPad, cashierPad });
                if (i == 0 && stockerPad != null) reveals.Add(stockerPad);
                baristaPads[i] = CreatePad(content.Baristas[i], baristaSpots[i], billPrefab, wallet, padMat, reveals.ToArray());
            }
            UnlockPad upgradePad = CreatePad(content.UpgradeDesk, Layout.UpgradeDesk, billPrefab, wallet, padMat);
            UnlockPad brewerPad = CreatePad(content.TeaBrewer, Layout.TeaBrewer, billPrefab, wallet, padMat,
                baristaPads[0], upgradePad);
            Turn(brewerPad, 180f);
            UnlockPad counterPad = CreatePad(content.Counter, Layout.Counter, billPrefab, wallet, padMat, brewerPad);

            // Only the first pad is visible at the start.
            brewerPad.gameObject.SetActive(false);
            upgradePad.gameObject.SetActive(false);
            taroPad.gameObject.SetActive(false);
            fruitPad.gameObject.SetActive(false);
            expansionPad.gameObject.SetActive(false);
            driveThruPad.gameObject.SetActive(false);
            seatingPad.gameObject.SetActive(false);
            cashierPad.gameObject.SetActive(false);
            cleanerPad.gameObject.SetActive(false);
            driveThruCashierPad.gameObject.SetActive(false);
            // The tea brewer reveals the first barista; each one reveals the next.
            for (int i = 0; i < baristaPads.Length; i++) baristaPads[i].gameObject.SetActive(false);
            var all = new System.Collections.Generic.List<UnlockPad>
            {
                counterPad, brewerPad, upgradePad, taroPad, fruitPad,
                expansionPad, driveThruPad, seatingPad, cashierPad, cleanerPad, driveThruCashierPad,
            };
            all.AddRange(baristaPads);
            if (extraPad != null)
            {
                extraPad.gameObject.SetActive(false);
                all.Add(extraPad);
            }
            if (stockerPad != null)
            {
                stockerPad.gameObject.SetActive(false);
                all.Add(stockerPad);
            }
            if (secondShopPad != null)
            {
                var reveal = new GameObject("Level 2 Unlock").AddComponent<RevealWhenAllUnlocked>();
                SetReferences(reveal, "required", all.ToArray());
                SetReference(reveal, "target", secondShopPad);
                secondShopPad.gameObject.SetActive(false);
                all.Add(secondShopPad);
            }
            // Decor: small pads that appear one after another once the café tables are built. They
            // don't count toward opening the next shop.
            UnlockPad previous = seatingPad;
            for (int i = 0; i < content.Decor.Length && i < Layout.Decor.Length; i++)
            {
                UnlockPad decorPad = CreatePad(content.Decor[i], Layout.Decor[i], billPrefab, wallet, padMat);
                ResizePad(decorPad, 1.3f);
                if (i == 2) Turn(decorPad, 180f); // the lucky cat waves at the player, not at the wall
                AddToReveal(previous, decorPad);
                decorPad.gameObject.SetActive(false);
                all.Add(decorPad);
                previous = decorPad;
            }
            return all.ToArray();
        }

        // Adds a pad to the ones another pad brings out when it's bought.
        private static void AddToReveal(UnlockPad pad, UnlockPad next)
        {
            var serialized = new SerializedObject(pad);
            SerializedProperty list = serialized.FindProperty("unlocksNext");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = next;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectList(Object component, string fieldName, params GameObject[] objects)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty list = serialized.FindProperty(fieldName);
            list.arraySize = objects.Length;
            for (int i = 0; i < objects.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Loads progress on Play and keeps saving it. Delete it with Bubble Tea > Delete Save Data.
        private static SaveManager CreateSaveManager(Wallet wallet, UnlockPad[] pads, UpgradeManager upgrades,
            OfflineEarnings offlineEarnings, DailyGoals dailyGoals, ShopRating rating, CashPileRefiller floorCash)
        {
            var go = new GameObject("SaveManager");
            var saveManager = go.AddComponent<SaveManager>();
            SetReference(saveManager, "wallet", wallet);
            SetReference(saveManager, "upgradeManager", upgrades);
            SetReference(saveManager, "offlineEarnings", offlineEarnings);
            SetReference(saveManager, "dailyGoals", dailyGoals);
            SetReference(saveManager, "rating", rating);
            SetReference(saveManager, "floorCash", floorCash);

            var serialized = new SerializedObject(saveManager);
            SerializedProperty list = serialized.FindProperty("pads");
            list.arraySize = pads.Length;
            for (int i = 0; i < pads.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pads[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return saveManager;
        }

        // Three small goals a day that pay a bonus.
        private static DailyGoals CreateDailyGoals(Wallet wallet, DrinkRecipe[] menu, OfflineEarnings earnings)
        {
            var goals = new GameObject("DailyGoals").AddComponent<DailyGoals>();
            SetReference(goals, "wallet", wallet);
            SetReference(goals, "earnings", earnings);
            SetReferences(goals, "menu", menu);
            return goals;
        }

        // A GOALS button under MENU that opens the current goals, plus a banner when one is finished.
        private static void CreateGoalsPanel(Transform hud, DailyGoals goals)
        {
            const int rows = 3;
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("Goals", hud);
            Stretch(root);
            var view = root.gameObject.AddComponent<GoalsPanel>();

            Button openButton = CreateMenuButton("GoalsButton", root, "GOALS", rounded, font, new Color(0.91f, 0.51f, 0.6f, 0.9f), out _);
            var openRect = (RectTransform)openButton.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.anchoredPosition = new Vector2(-HudMargin, -HudMargin - 110f * HudScale - 10f);
            openRect.sizeDelta = new Vector2(200f, 110f);
            openRect.localScale = Vector3.one * HudScale;

            // A red "!" in the button's corner when a reward is waiting.
            RectTransform badge = CreateUiObject("Badge", openRect);
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 1f);
            badge.sizeDelta = new Vector2(64f, 64f);
            badge.anchoredPosition = new Vector2(6f, -6f);
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            badgeImage.color = new Color(0.9f, 0.25f, 0.25f);
            badgeImage.raycastTarget = false;
            Text badgeText = CreateHudText("Mark", badge, font, 44, TextAnchor.MiddleCenter);
            Stretch(badgeText.rectTransform);
            badgeText.text = "!";

            RectTransform panel = CreateUiObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.gameObject.AddComponent<FitToScreen>();
            panel.sizeDelta = new Vector2(940f, 220f + rows * 170f + 140f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            Text title = CreateHudText("Title", panel, font, 60, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 20f, 100f, 0f, 0f);
            title.text = "GOALS";
            Text hint = CreateHudText("Hint", panel, font, 32, TextAnchor.MiddleCenter);
            SetTopRow(hint.rectTransform, 115f, 60f, 0f, 0f);
            hint.fontStyle = FontStyle.Normal;
            hint.color = new Color(0.8f, 0.8f, 0.85f);
            hint.text = "Reach each milestone for a bonus";

            var descriptions = new Text[rows];
            var progress = new Text[rows];
            var claims = new Button[rows];
            var claimLabels = new Text[rows];
            for (int i = 0; i < rows; i++)
            {
                RectTransform row = CreateUiObject("Goal" + (i + 1), panel);
                SetTopRow(row, 200f + i * 170f, 150f, 40f, 40f);
                var rowImage = row.gameObject.AddComponent<Image>();
                rowImage.sprite = rounded;
                rowImage.type = Image.Type.Sliced;
                rowImage.color = new Color(1f, 1f, 1f, 0.08f);

                descriptions[i] = CreateHudText("Description", row, font, 40, TextAnchor.UpperLeft);
                SetTopRow(descriptions[i].rectTransform, 18f, 60f, 30f, 300f);
                progress[i] = CreateHudText("Progress", row, font, 36, TextAnchor.UpperLeft);
                SetTopRow(progress[i].rectTransform, 82f, 50f, 30f, 300f);
                progress[i].fontStyle = FontStyle.Normal;
                progress[i].color = new Color(0.8f, 0.8f, 0.85f);

                claims[i] = CreateMenuButton("Claim", row, "$0", rounded, font, new Color(0.45f, 0.45f, 0.55f), out claimLabels[i]);
                var claimRect = (RectTransform)claims[i].transform;
                claimRect.anchorMin = claimRect.anchorMax = new Vector2(1f, 0.5f);
                claimRect.pivot = new Vector2(1f, 0.5f);
                claimRect.anchoredPosition = new Vector2(-20f, 0f);
                claimRect.sizeDelta = new Vector2(250f, 110f);
            }

            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            SetTopRow((RectTransform)close.transform, 220f + rows * 170f, 110f, 250f, 250f);

            // Banner under the money counter when a goal is finished.
            Text banner = CreateHudText("Banner", root, font, 44, TextAnchor.MiddleCenter);
            banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            banner.rectTransform.anchoredPosition = new Vector2(0f, -370f);
            banner.rectTransform.sizeDelta = new Vector2(1000f, 80f);
            banner.color = new Color(1f, 0.85f, 0.3f);
            var outline = banner.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);

            SetReference(view, "goals", goals);
            SetReference(view, "panel", panel.gameObject);
            SetReference(view, "openButton", openButton);
            SetReference(view, "closeButton", close);
            SetReference(view, "badge", badge.gameObject);
            SetReferences(view, "descriptions", descriptions);
            SetReferences(view, "progressLabels", progress);
            SetReferences(view, "claimButtons", claims);
            SetReferences(view, "claimLabels", claimLabels);
            SetReference(view, "banner", banner);
        }

        // A STATS button under GOALS that opens this shop's lifetime totals.
        private static void CreateStatsPanel(Transform hud, DrinkRecipe[] menu)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("Stats", hud);
            Stretch(root);
            var view = root.gameObject.AddComponent<StatsPanel>();

            Button openButton = CreateMenuButton("StatsButton", root, "STATS", rounded, font, new Color(0.45f, 0.62f, 0.9f, 0.9f), out _);
            var openRect = (RectTransform)openButton.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.anchoredPosition = new Vector2(-HudMargin, -HudMargin - 2f * (110f * HudScale + 10f));
            openRect.sizeDelta = new Vector2(200f, 110f);
            openRect.localScale = Vector3.one * HudScale;

            // Centered; the panel grows to fit the list at runtime and shrinks to fit short screens.
            RectTransform panel = CreateUiObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(900f, 1500f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            Text title = CreateHudText("Title", panel, font, 64, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 25f, 100f, 0f, 0f);
            title.text = "STATS";

            Text body = CreateHudText("Labels", panel, font, 40, TextAnchor.UpperLeft);
            SetTopRow(body.rectTransform, 150f, 1160f, 60f, 60f);
            body.fontStyle = FontStyle.Normal;
            body.horizontalOverflow = HorizontalWrapMode.Overflow;
            body.verticalOverflow = VerticalWrapMode.Overflow;

            Text values = CreateHudText("Values", panel, font, 40, TextAnchor.UpperRight);
            SetTopRow(values.rectTransform, 150f, 1160f, 60f, 60f);
            values.horizontalOverflow = HorizontalWrapMode.Overflow;
            values.verticalOverflow = VerticalWrapMode.Overflow;

            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 40f);
            closeRect.sizeDelta = new Vector2(400f, 110f);
            panel.gameObject.AddComponent<FitToScreen>();

            SetReference(view, "panel", panel.gameObject);
            SetReference(view, "openButton", openButton);
            SetReference(view, "closeButton", close);
            SetReference(view, "body", body);
            SetReference(view, "values", values);
            SetReferences(view, "menu", menu);
        }

        // The star rating. Every pad counts; decorations and big expansions count more.
        private static ShopRating CreateShopRating(UnlockPad[] pads, ShopContentBuilder.Content content)
        {
            var rating = new GameObject("ShopRating").AddComponent<ShopRating>();
            var serialized = new SerializedObject(rating);
            SerializedProperty list = serialized.FindProperty("pads");
            // The bus stop to the next shop doesn't make this shop any nicer.
            pads = System.Array.FindAll(pads, pad => content.SecondShop == null || pad.Definition != content.SecondShop);
            list.arraySize = pads.Length;
            for (int i = 0; i < pads.Length; i++)
            {
                BuildableDefinition definition = pads[i].Definition;
                int points = definition == content.Statue ? 3
                    : System.Array.IndexOf(content.Decor, definition) >= 0 ? 2
                    : definition == content.Planter || definition == content.MenuBoard || definition == content.Seating ||
                      definition == content.Expansion ||
                      definition == content.DriveThru ? 2
                    : 1;
                SerializedProperty entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("pad").objectReferenceValue = pads[i];
                entry.FindPropertyRelative("points").intValue = points;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rating;
        }

        // Five stars under the money counter. Tapping them explains how to earn more.
        private static void CreateRatingView(Transform hud, ShopRating rating)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            const float starSize = 62f;

            RectTransform root = CreateUiObject("Rating", hud);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(HudMargin, -HudMargin - 120f * HudScale - 10f); // under the clock
            root.sizeDelta = new Vector2(5 * starSize + 130f, starSize + 10f);
            root.localScale = Vector3.one * HudScale;
            var hitArea = root.gameObject.AddComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0.25f);
            var help = root.gameObject.AddComponent<Button>();
            help.targetGraphic = hitArea;
            var view = root.gameObject.AddComponent<RatingView>();

            var empty = new Image[5];
            var filled = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                RectTransform back = CreateUiObject("Star" + (i + 1), root);
                back.anchorMin = back.anchorMax = new Vector2(0f, 0.5f);
                back.pivot = new Vector2(0f, 0.5f);
                back.anchoredPosition = new Vector2(10f + i * starSize, 0f);
                back.sizeDelta = new Vector2(starSize, starSize);
                empty[i] = back.gameObject.AddComponent<Image>();
                empty[i].color = new Color(1f, 1f, 1f, 0.3f);
                empty[i].raycastTarget = false;

                RectTransform front = CreateUiObject("Fill", back);
                Stretch(front);
                filled[i] = front.gameObject.AddComponent<Image>();
                filled[i].color = new Color(1f, 0.82f, 0.25f);
                filled[i].raycastTarget = false;
            }

            Text value = CreateHudText("Value", root, font, 48, TextAnchor.MiddleLeft);
            value.rectTransform.anchorMin = value.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            value.rectTransform.pivot = new Vector2(0f, 0.5f);
            value.rectTransform.anchoredPosition = new Vector2(20f + 5 * starSize, 0f);
            value.rectTransform.sizeDelta = new Vector2(110f, starSize);

            Text hint = CreateHudText("Hint", root, font, 34, TextAnchor.MiddleCenter);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0f, 0f);
            hint.rectTransform.pivot = new Vector2(0f, 1f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            hint.alignment = TextAnchor.UpperLeft;
            hint.rectTransform.sizeDelta = new Vector2(900f, 160f);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.verticalOverflow = VerticalWrapMode.Overflow;
            hint.text = "Every customer who walks out (tired of waiting or prices too high) costs half a star.\nServing customers earns stars back. At 0 stars your shop closes!";
            hint.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

            SetReference(view, "rating", rating);
            SetReferences(view, "emptyStars", empty);
            SetReferences(view, "filledStars", filled);
            SetReference(view, "valueLabel", value);
            SetReference(view, "helpButton", help);
            SetReference(view, "helpLabel", hint);
        }

        // Fills a private [SerializeField] array the same way dragging items into it in the Inspector would.
        internal static void SetReferences(Object component, string fieldName, Object[] values)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty list = serialized.FindProperty(fieldName);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // The shop clock (random customer traffic) and its display in the top-left corner.
        // Size and edge gap of the top-corner HUD widgets (clock, money, stars, MENU, GOALS).
        private const float HudScale = 0.82f; // bigger so money, time, stars and buttons are easy to read on a phone
        private const float HudMargin = 16f;

        private static void CreateClock(Transform hud)
        {
            var clock = new GameObject("GameClock").AddComponent<GameClock>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            RectTransform root = CreateUiObject("Clock", hud);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(HudMargin, -HudMargin);
            root.sizeDelta = new Vector2(250f, 120f);
            root.localScale = Vector3.one * HudScale;
            var back = root.gameObject.AddComponent<Image>();
            back.sprite = rounded;
            back.type = Image.Type.Sliced;
            back.color = new Color(0.15f, 0.15f, 0.2f, 0.75f);
            back.raycastTarget = false;

            Text time = CreateHudText("Time", root, font, 56, TextAnchor.MiddleCenter);
            SetTopRow(time.rectTransform, 8f, 66f, 0f, 0f);
            Text day = CreateHudText("Day", root, font, 36, TextAnchor.MiddleCenter);
            SetTopRow(day.rectTransform, 70f, 44f, 0f, 0f);
            day.color = new Color(0.8f, 0.8f, 0.88f);

            var view = root.gameObject.AddComponent<ClockView>();
            SetReference(view, "clock", clock);
            SetReference(view, "timeLabel", time);
            SetReference(view, "dayLabel", day);
        }

        // Opens when you tap a counter: a row per drink to raise or lower its price.
        private static void CreatePricePanel(Transform hud, DrinkRecipe[] menu)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            const float rowHeight = 120f, top = 140f;

            RectTransform root = CreateUiObject("Prices", hud);
            Stretch(root);
            var prices = root.gameObject.AddComponent<PricePanel>();

            RectTransform backdrop = CreateUiObject("Backdrop", root);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);

            RectTransform panel = CreateUiObject("Panel", backdrop);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.gameObject.AddComponent<FitToScreen>();
            panel.sizeDelta = new Vector2(940f, top + rowHeight * menu.Length + 260f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.97f);

            Text title = CreateHudText("Title", panel, font, 60, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 20f, 100f, 0f, 0f);
            title.text = "DRINK PRICES";

            var serialized = new SerializedObject(prices);
            SerializedProperty rows = serialized.FindProperty("rows");
            rows.arraySize = menu.Length;
            var green = new Color(0.35f, 0.78f, 0.38f);
            var red = new Color(0.85f, 0.45f, 0.4f);
            for (int i = 0; i < menu.Length; i++)
            {
                RectTransform row = CreateUiObject(menu[i] != null ? menu[i].name : "Row", panel);
                SetTopRow(row, top + i * rowHeight, rowHeight, 40f, 40f);

                Text name = CreateHudText("Name", row, font, 40, TextAnchor.MiddleLeft);
                SetColumn(name.rectTransform, 0f, 0.42f);
                Text price = CreateHudText("Price", row, font, 40, TextAnchor.MiddleCenter);
                SetColumn(price.rectTransform, 0.42f, 0.72f);
                Button lower = CreateMenuButton("Lower", row, "-", rounded, font, red, out _);
                SetColumn((RectTransform)lower.transform, 0.73f, 0.85f);
                ((RectTransform)lower.transform).offsetMin = new Vector2(0f, 15f);
                ((RectTransform)lower.transform).offsetMax = new Vector2(0f, -15f);
                Button raise = CreateMenuButton("Raise", row, "+", rounded, font, green, out _);
                SetColumn((RectTransform)raise.transform, 0.87f, 1f);
                ((RectTransform)raise.transform).offsetMin = new Vector2(0f, 15f);
                ((RectTransform)raise.transform).offsetMax = new Vector2(0f, -15f);

                SerializedProperty entry = rows.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("recipe").objectReferenceValue = menu[i];
                entry.FindPropertyRelative("root").objectReferenceValue = row.gameObject;
                entry.FindPropertyRelative("nameLabel").objectReferenceValue = name;
                entry.FindPropertyRelative("priceLabel").objectReferenceValue = price;
                entry.FindPropertyRelative("lowerButton").objectReferenceValue = lower;
                entry.FindPropertyRelative("raiseButton").objectReferenceValue = raise;
            }

            Text hint = CreateHudText("Hint", panel, font, 32, TextAnchor.MiddleCenter);
            SetTopRow(hint.rectTransform, top + rowHeight * menu.Length + 10f, 100f, 40f, 40f);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.fontStyle = FontStyle.Normal;
            hint.color = new Color(0.85f, 0.85f, 0.9f);
            hint.text = "Higher prices earn more, but customers who think it's too much walk out and cost half a star.";

            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            SetTopRow((RectTransform)close.transform, top + rowHeight * menu.Length + 130f, 110f, 260f, 260f);

            serialized.FindProperty("panel").objectReferenceValue = backdrop.gameObject;
            serialized.FindProperty("titleLabel").objectReferenceValue = title;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // This shop's background music, looping.
        private static void CreateMusic(MusicMaker.Track track)
        {
            var go = new GameObject("Music");
            var source = go.AddComponent<AudioSource>();
            source.clip = MusicMaker.Get(track);
            source.loop = true;
            source.playOnAwake = false;
            go.AddComponent<MusicPlayer>();
        }

        // "Shop closed" screen shown when the rating hits 0 stars, with a Start Over button.
        private static void CreateGameOverPopup(Transform hud, ShopRating rating, SaveManager saveManager)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("GameOver", hud);
            Stretch(root);
            var popup = root.gameObject.AddComponent<GameOverPopup>();

            RectTransform backdrop = CreateUiObject("Backdrop", root);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            RectTransform panel = CreateUiObject("Panel", backdrop);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.gameObject.AddComponent<FitToScreen>();
            panel.sizeDelta = new Vector2(860f, 620f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.97f);

            Text title = CreateHudText("Title", panel, font, 64, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 40f, 110f, 0f, 0f);
            title.color = new Color(0.95f, 0.45f, 0.45f);
            title.text = "SHOP CLOSED";

            Text reason = CreateHudText("Reason", panel, font, 38, TextAnchor.MiddleCenter);
            SetTopRow(reason.rectTransform, 170f, 180f, 50f, 50f);
            reason.horizontalOverflow = HorizontalWrapMode.Wrap;
            reason.fontStyle = FontStyle.Normal;
            reason.color = new Color(0.85f, 0.85f, 0.9f);
            reason.text = "Too many customers walked out and your rating hit 0 stars.";

            Button startOver = CreateMenuButton("StartOver", panel, "START OVER", rounded, font, new Color(0.35f, 0.78f, 0.38f), out _);
            SetTopRow((RectTransform)startOver.transform, 420f, 140f, 160f, 160f);

            SetReference(popup, "panel", backdrop.gameObject);
            SetReference(popup, "startOverButton", startOver);
            SetReference(popup, "rating", rating);
            SetReference(popup, "saveManager", saveManager);
        }

        // ---------- Matcha House events ----------

        // A blue tour bus that drives along the road out front, stops by the door and lets a group of
        // tourists off (see TourBus).
        private static void CreateTourBus(CustomerSpawner spawner)
        {
            float roadZ = Layout.SidewalkZ - 5.5f;
            var go = new GameObject("TourBus");
            var tour = go.AddComponent<TourBus>();

            Transform bus = new GameObject("Bus").transform;
            bus.SetParent(go.transform, false);
            Material paint = GetOrCreateMaterial("TourBusPaint", new Color(0.33f, 0.62f, 0.9f));
            Material white = GetOrCreateMaterial("TourBusRoof", new Color(0.97f, 0.97f, 0.95f));
            Material glass = GetOrCreateMaterial("CarWindow", new Color(0.55f, 0.75f, 0.90f));
            Material tire = GetOrCreateMaterial("Tire", new Color(0.12f, 0.12f, 0.13f));
            Material stripe = GetOrCreateMaterial("TourBusStripe", new Color(1f, 0.8f, 0.25f));
            // Built facing +z, the way it drives.
            BusPart(bus, PrimitiveType.Cube, "Body", new Vector3(0f, 1.45f, 0f), new Vector3(2.4f, 2.1f, 7.2f), paint);
            BusPart(bus, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.55f, 0f), new Vector3(2.46f, 0.14f, 7.26f), white);
            BusPart(bus, PrimitiveType.Cube, "Windows", new Vector3(0f, 1.95f, 0.2f), new Vector3(2.46f, 0.75f, 6.2f), glass);
            BusPart(bus, PrimitiveType.Cube, "Stripe", new Vector3(0f, 1.05f, 0f), new Vector3(2.46f, 0.18f, 7.26f), stripe);
            BusPart(bus, PrimitiveType.Cube, "Windshield", new Vector3(0f, 1.8f, 3.6f), new Vector3(2.1f, 1f, 0.06f), glass);
            foreach (float x in new[] { -1.12f, 1.12f })
            {
                foreach (float z in new[] { -2.5f, 2.4f })
                {
                    GameObject wheel = BusPart(bus, PrimitiveType.Cylinder, "Wheel", new Vector3(x, 0.45f, z),
                        new Vector3(0.9f, 0.14f, 0.9f), tire);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            SetReference(tour, "spawner", spawner);
            SetReference(tour, "bus", bus);
            var serialized = new SerializedObject(tour);
            serialized.FindProperty("roadStart").vector3Value = new Vector3(-36f, 0f, roadZ);
            serialized.FindProperty("stop").vector3Value = new Vector3(Layout.Counter.x, 0f, roadZ);
            serialized.FindProperty("roadEnd").vector3Value = new Vector3(46f, 0f, roadZ);
            serialized.FindProperty("doorOffset").vector3Value = new Vector3(0f, 0f, 1.8f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Out of sight until the first bus comes.
            bus.position = new Vector3(-36f, 0f, roadZ);
            bus.gameObject.SetActive(false);
        }

        private static GameObject BusPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        // A chalkboard sign by the front door: "TODAY'S SPECIAL", a model of the drink, its name and price.
        private static void CreateDailySpecialBoard()
        {
            var go = new GameObject("DailySpecial");
            go.transform.position = Layout.Chalkboard;
            Material wood = GetOrCreateMaterial("ChalkboardWood", new Color(0.55f, 0.38f, 0.24f));
            Material chalk = GetOrCreateMaterial("Chalkboard", new Color(0.16f, 0.26f, 0.2f));
            BusPart(go.transform, PrimitiveType.Cube, "Leg", new Vector3(-0.6f, 0.45f, 0.12f), new Vector3(0.08f, 0.9f, 0.08f), wood);
            BusPart(go.transform, PrimitiveType.Cube, "Leg", new Vector3(0.6f, 0.45f, 0.12f), new Vector3(0.08f, 0.9f, 0.08f), wood);

            // The board leans back a little, toward the shop, so it reads well from the camera.
            Transform face = new GameObject("Face").transform;
            face.SetParent(go.transform, false);
            face.localPosition = new Vector3(0f, 0.35f, 0f);
            face.localRotation = Quaternion.Euler(18f, 0f, 0f);
            BusPart(face, PrimitiveType.Cube, "Frame", new Vector3(0f, 0.8f, 0.03f), new Vector3(1.62f, 1.62f, 0.05f), wood);
            BusPart(face, PrimitiveType.Cube, "Board", new Vector3(0f, 0.8f, 0f), new Vector3(1.5f, 1.5f, 0.05f), chalk);
            BusPart(face, PrimitiveType.Cube, "Ledge", new Vector3(0f, 0.62f, -0.17f), new Vector3(0.9f, 0.04f, 0.32f), wood);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TextMesh title = BoardText(face, font, "TODAY'S SPECIAL", new Vector3(0f, 1.38f, -0.04f), 0.024f, new Color(1f, 0.86f, 0.4f));
            title.fontStyle = FontStyle.Bold;
            TextMesh drink = BoardText(face, font, "", new Vector3(0f, 0.33f, -0.04f), 0.02f, new Color(0.97f, 0.97f, 0.95f));

            Transform icon = new GameObject("Icon").transform;
            icon.SetParent(face, false);
            icon.localPosition = new Vector3(0f, 0.64f, -0.2f);

            var special = go.AddComponent<DailySpecial>();
            SetReference(special, "drinkLabel", drink);
            SetReference(special, "iconSpot", icon);
            var serialized = new SerializedObject(special);
            serialized.FindProperty("iconScale").floatValue = 1.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TextMesh BoardText(Transform parent, Font font, string text, Vector3 position, float size, Color color)
        {
            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = position;
            var mesh = labelGo.AddComponent<TextMesh>();
            mesh.font = font;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = color;
            return mesh;
        }

        // The supply dock against the kitchen's west wall, and the little truck that keeps it stocked
        // with crates (it drives in along the road and up the west side of the shop).
        private static void CreateSupplyDelivery(ItemDefinition crate, Material spotMaterial)
        {
            var go = new GameObject("SupplyDock");
            go.transform.position = Layout.SupplyDock;
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // its stand-here spot faces east, into the kitchen

            GameObject pallet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pallet.name = "Pallet";
            pallet.transform.SetParent(go.transform, false);
            pallet.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            pallet.transform.localScale = new Vector3(1.95f, 0.16f, 1.45f);
            pallet.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Pallet", new Color(0.72f, 0.55f, 0.36f));

            Transform anchor = new GameObject("Crates").transform;
            anchor.SetParent(go.transform, false);
            anchor.localPosition = new Vector3(0f, 0.16f, 0f);

            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spot.name = "Spot";
            Object.DestroyImmediate(spot.GetComponent<BoxCollider>());
            spot.transform.SetParent(go.transform, false);
            spot.transform.localPosition = new Vector3(0f, 0.005f, 1.45f);
            spot.transform.localScale = new Vector3(1.4f, 0.01f, 1.2f);
            spot.GetComponent<Renderer>().sharedMaterial = spotMaterial;

            NameTag.Add(go, "Supplies", 1.1f); // same yellow outlined label as the stations, above a full pallet

            var dock = go.AddComponent<SupplyDock>();
            var serialized = new SerializedObject(dock);
            serialized.FindProperty("crate").objectReferenceValue = crate;
            serialized.FindProperty("crates.anchor").objectReferenceValue = anchor;
            // Holds 20 crates: 4 x 3 on the pallet, a second layer on top.
            serialized.FindProperty("crates.capacity").intValue = 20;
            serialized.FindProperty("crates.itemHeight").floatValue = 0.235f;
            SetStackGrid(serialized, "crates", 4, 3, 0.48f, DrinkSize * 0.75f);
            serialized.FindProperty("zone.offset").vector3Value = new Vector3(0f, 0f, 1.45f);
            serialized.FindProperty("zone.halfSize").floatValue = 0.7f;
            serialized.FindProperty("zone.secondsPerHandoff").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The truck: a white cab and a matcha-green box, built facing +z.
            var truckGo = new GameObject("DeliveryTruck");
            var delivery = truckGo.AddComponent<DeliveryTruck>();
            Transform truck = new GameObject("Truck").transform;
            truck.SetParent(truckGo.transform, false);
            Material cab = GetOrCreateMaterial("TruckCab", new Color(0.97f, 0.97f, 0.95f));
            Material box = GetOrCreateMaterial("TruckBox", new Color(0.42f, 0.62f, 0.32f));
            Material glass = GetOrCreateMaterial("CarWindow", new Color(0.55f, 0.75f, 0.90f));
            Material tire = GetOrCreateMaterial("Tire", new Color(0.12f, 0.12f, 0.13f));
            BusPart(truck, PrimitiveType.Cube, "Cab", new Vector3(0f, 0.95f, 1.5f), new Vector3(1.7f, 1.3f, 1.2f), cab);
            BusPart(truck, PrimitiveType.Cube, "Windshield", new Vector3(0f, 1.25f, 2.1f), new Vector3(1.5f, 0.5f, 0.05f), glass);
            BusPart(truck, PrimitiveType.Cube, "Box", new Vector3(0f, 1.3f, -0.5f), new Vector3(1.8f, 1.9f, 2.7f), box);
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                foreach (float z in new[] { -1.2f, 1.4f })
                {
                    GameObject wheel = BusPart(truck, PrimitiveType.Cylinder, "Wheel", new Vector3(x, 0.38f, z),
                        new Vector3(0.76f, 0.12f, 0.76f), tire);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            float roadZ = Layout.SidewalkZ - 5.5f;
            float sideX = -Layout.HalfWidth - 1.3f; // just outside the west wall
            SetReference(delivery, "dock", dock);
            SetReference(delivery, "truck", truck);
            var truckSettings = new SerializedObject(delivery);
            SerializedProperty route = truckSettings.FindProperty("route");
            Vector3[] points = { new(-36f, 0f, roadZ), new(sideX, 0f, roadZ), new(sideX, 0f, Layout.SupplyDock.z) };
            route.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++) route.GetArrayElementAtIndex(i).vector3Value = points[i];
            truckSettings.ApplyModifiedPropertiesWithoutUndo();
            truck.position = points[0];
            truck.gameObject.SetActive(false);
        }

        // A comfy two-seat sofa against the kitchen's back wall, where baristas take their breaks.
        private static void CreateStaffSofa()
        {
            var go = new GameObject("StaffSofa");
            go.transform.position = Layout.StaffSofa;
            go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // faces into the kitchen (south)
            Material fabric = GetOrCreateMaterial("SofaFabric", new Color(0.93f, 0.55f, 0.6f));
            Material cushion = GetOrCreateMaterial("SofaCushion", new Color(1f, 0.78f, 0.8f));
            Material wood = GetOrCreateMaterial("SofaLegs", new Color(0.45f, 0.29f, 0.2f));

            // The base keeps its collider so the player walks around the sofa, not through it.
            GameObject seatBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seatBase.name = "Base";
            seatBase.transform.SetParent(go.transform, false);
            seatBase.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            seatBase.transform.localScale = new Vector3(1.9f, 0.3f, 0.75f);
            seatBase.GetComponent<Renderer>().sharedMaterial = fabric;
            SofaPart(go.transform, "Back", new Vector3(0f, 0.62f, -0.3f), new Vector3(1.9f, 0.6f, 0.18f), fabric);
            foreach (float x in new[] { -1f, 1f })
            {
                SofaPart(go.transform, "Arm", new Vector3(x * 0.98f, 0.5f, 0f), new Vector3(0.18f, 0.42f, 0.75f), fabric);
                SofaPart(go.transform, "Cushion", new Vector3(x * 0.44f, 0.43f, 0.05f), new Vector3(0.84f, 0.08f, 0.62f), cushion);
                SofaPart(go.transform, "BackCushion", new Vector3(x * 0.44f, 0.68f, -0.19f), new Vector3(0.82f, 0.36f, 0.08f), cushion);
                foreach (float z in new[] { -0.3f, 0.3f })
                    SofaPart(go.transform, "Leg", new Vector3(x * 0.85f, 0.05f, z), new Vector3(0.08f, 0.1f, 0.08f), wood);
            }

            var seats = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                seats[i] = new GameObject("Seat " + (i + 1)).transform;
                seats[i].SetParent(go.transform, false);
                seats[i].localPosition = new Vector3(i == 0 ? -0.44f : 0.44f, 0.2f, 0.08f); // hips at cushion height
            }
            var sofa = go.AddComponent<StaffSofa>();
            var serialized = new SerializedObject(sofa);
            SerializedProperty list = serialized.FindProperty("seats");
            list.arraySize = seats.Length;
            for (int i = 0; i < seats.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = seats[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SofaPart(Transform parent, string name, Vector3 position, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        // A banner for shop events (tour bus, critic, today's special), below the other alerts.
        private static void CreateAnnouncementBanner(Transform hud)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("AnnouncementBanner", hud);
            Stretch(root);
            var announcer = root.gameObject.AddComponent<AnnouncementBanner>();

            RectTransform banner = CreateUiObject("Banner", root);
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);
            banner.anchoredPosition = new Vector2(0f, -HudMargin - 120f * HudScale - 320f); // under the staff alert
            banner.sizeDelta = new Vector2(860f, 140f);
            var image = banner.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.2f, 0.45f, 0.8f, 0.92f);
            image.raycastTarget = false;

            Text text = CreateHudText("Text", banner, font, 36, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            SetReference(announcer, "banner", banner.gameObject);
            SetReference(announcer, "background", image);
            SetReference(announcer, "label", text);
        }

        // A bin beside the counter. Stand next to it to throw away what you're carrying.
        private static void CreateTrashCan(Material spotMaterial)
        {
            var go = new GameObject("TrashCan");
            go.transform.position = Layout.TrashCan;
            go.transform.rotation = Quaternion.Euler(0f, 90f, 0f); // stand-here tile on its east side, facing into the kitchen

            GameObject bin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bin.name = "Bin";
            bin.transform.SetParent(go.transform, false);
            bin.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            bin.transform.localScale = new Vector3(0.8f, 0.45f, 0.8f);
            bin.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("TrashCan", new Color(0.35f, 0.38f, 0.42f));

            var opening = new GameObject("Opening").transform;
            opening.SetParent(go.transform, false);
            opening.localPosition = new Vector3(0f, 0.8f, 0f);

            // A tile on the floor showing where to stand.
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spot.name = "Spot";
            Object.DestroyImmediate(spot.GetComponent<BoxCollider>());
            spot.transform.SetParent(go.transform, false);
            spot.transform.localPosition = new Vector3(0f, 0.005f, 1.2f);
            spot.transform.localScale = new Vector3(1.4f, 0.01f, 1.2f);
            spot.GetComponent<Renderer>().sharedMaterial = spotMaterial;

            NameTag.Add(go, "Trash"); // same yellow outlined label as the stations

            var trash = go.AddComponent<TrashCan>();
            SetReference(trash, "opening", opening);
            var serialized = new SerializedObject(trash);
            serialized.FindProperty("zone.offset").vector3Value = new Vector3(0f, 0f, 1.2f);
            serialized.FindProperty("zone.halfSize").floatValue = 0.8f;
            serialized.FindProperty("zone.secondsPerHandoff").floatValue = 0.25f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Sounds, vibration, confetti and sale pop-ups.
        private static void CreateFeedback()
        {
            var go = new GameObject("Feedback");
            go.AddComponent<AudioSource>();
            go.AddComponent<SoundEffects>();
            go.AddComponent<Haptics>();
            go.AddComponent<UnlockConfetti>();
            go.AddComponent<SalePopups>();
        }

        // A MENU button in the top-right corner that opens sound, vibration and reset options,
        // plus GM Tools for testing (Editor and Development builds only).
        private static void CreateSettingsMenu(Transform hud, SaveManager saveManager, Wallet wallet, UpgradeManager upgrades)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("Settings", hud);
            Stretch(root);
            var menu = root.gameObject.AddComponent<SettingsMenu>();

            Button openButton = CreateMenuButton("MenuButton", root, "MENU", rounded, font, new Color(0.15f, 0.15f, 0.2f, 0.75f), out _);
            var openRect = (RectTransform)openButton.transform;
            openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 1f);
            openRect.pivot = new Vector2(1f, 1f);
            openRect.anchoredPosition = new Vector2(-HudMargin, -HudMargin);
            openRect.sizeDelta = new Vector2(200f, 110f);
            openRect.localScale = Vector3.one * HudScale;

            RectTransform panel = CreateUiObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.gameObject.AddComponent<FitToScreen>();
            panel.sizeDelta = new Vector2(800f, 1210f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            Text title = CreateHudText("Title", panel, font, 60, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 20f, 110f, 0f, 0f);
            title.text = "SETTINGS";

            var green = new Color(0.35f, 0.78f, 0.38f);
            Button sound = CreateMenuButton("Sound", panel, "Sound: ON", rounded, font, green, out Text soundLabel);
            SetTopRow((RectTransform)sound.transform, 160f, 120f, 60f, 60f);
            Button music = CreateMenuButton("Music", panel, "Music: ON", rounded, font, green, out Text musicLabel);
            SetTopRow((RectTransform)music.transform, 310f, 120f, 60f, 60f);
            Button vibration = CreateMenuButton("Vibration", panel, "Vibration: ON", rounded, font, green, out Text vibrationLabel);
            SetTopRow((RectTransform)vibration.transform, 460f, 120f, 60f, 60f);
            Button mainMenu = CreateMenuButton("MainMenu", panel, "Main Menu", rounded, font, new Color(0.91f, 0.51f, 0.60f), out _);
            SetTopRow((RectTransform)mainMenu.transform, 610f, 120f, 60f, 60f);
            Button reset = CreateMenuButton("Reset", panel, "Reset Progress", rounded, font, new Color(0.85f, 0.35f, 0.35f),
                out Text resetLabel);
            SetTopRow((RectTransform)reset.transform, 760f, 120f, 60f, 60f);
            Button gmOpen = CreateMenuButton("GmTools", panel, "GM Tools", rounded, font, new Color(0.55f, 0.4f, 0.8f), out _);
            SetTopRow((RectTransform)gmOpen.transform, 910f, 120f, 60f, 60f);
            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            SetTopRow((RectTransform)close.transform, 1060f, 110f, 200f, 200f);

            SetReference(menu, "panel", panel.gameObject);
            SetReference(menu, "openButton", openButton);
            SetReference(menu, "closeButton", close);
            SetReference(menu, "soundButton", sound);
            SetReference(menu, "soundLabel", soundLabel);
            SetReference(menu, "musicButton", music);
            SetReference(menu, "musicLabel", musicLabel);
            SetReference(menu, "vibrationButton", vibration);
            SetReference(menu, "vibrationLabel", vibrationLabel);
            SetReference(menu, "resetButton", reset);
            SetReference(menu, "resetLabel", resetLabel);
            SetReference(menu, "mainMenuButton", mainMenu);
            SetReference(menu, "saveManager", saveManager);

            CreateGmPanel(root, gmOpen, rounded, font, wallet, saveManager, upgrades);
        }

        // The GM Tools panel: free money, build everything, instant building and maxed upgrades.
        private static void CreateGmPanel(RectTransform root, Button openButton, Sprite rounded, Font font,
            Wallet wallet, SaveManager saveManager, UpgradeManager upgrades)
        {
            var gm = root.gameObject.AddComponent<GmMenu>();

            RectTransform panel = CreateUiObject("GmPanel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.gameObject.AddComponent<FitToScreen>();
            panel.sizeDelta = new Vector2(800f, 910f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.2f, 0.12f, 0.3f, 0.97f);

            Text title = CreateHudText("Title", panel, font, 60, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 20f, 110f, 0f, 0f);
            title.text = "GM TOOLS";

            var green = new Color(0.35f, 0.78f, 0.38f);
            Button money = CreateMenuButton("Money", panel, "+ $10,000", rounded, font, green, out _);
            SetTopRow((RectTransform)money.transform, 160f, 120f, 60f, 60f);
            Button buildAll = CreateMenuButton("BuildAll", panel, "Build Everything", rounded, font, green, out _);
            SetTopRow((RectTransform)buildAll.transform, 310f, 120f, 60f, 60f);
            Button instant = CreateMenuButton("Instant", panel, "Instant Build: OFF", rounded, font, green, out Text instantLabel);
            SetTopRow((RectTransform)instant.transform, 460f, 120f, 60f, 60f);
            Button maxUpgrades = CreateMenuButton("MaxUpgrades", panel, "Max Upgrades", rounded, font, green, out _);
            SetTopRow((RectTransform)maxUpgrades.transform, 610f, 120f, 60f, 60f);
            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            SetTopRow((RectTransform)close.transform, 760f, 110f, 200f, 200f);

            SetReference(gm, "openButton", openButton);
            SetReference(gm, "panel", panel.gameObject);
            SetReference(gm, "moneyButton", money);
            SetReference(gm, "buildAllButton", buildAll);
            SetReference(gm, "instantButton", instant);
            SetReference(gm, "instantLabel", instantLabel);
            SetReference(gm, "upgradesButton", maxUpgrades);
            SetReference(gm, "closeButton", close);
            SetReference(gm, "wallet", wallet);
            SetReference(gm, "saveManager", saveManager);
            SetReference(gm, "upgradeManager", upgrades);
        }

        private static Button CreateMenuButton(string name, Transform parent, string text, Sprite sprite, Font font, Color color,
            out Text label)
        {
            RectTransform rect = CreateUiObject(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            label = CreateHudText("Label", rect, font, 50, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.text = text;
            return button;
        }

        private static UpgradeManager CreateUpgradeManager(Wallet wallet, UpgradeDefinition[] upgrades)
        {
            var go = new GameObject("UpgradeManager");
            var manager = go.AddComponent<UpgradeManager>();
            SetReference(manager, "wallet", wallet);

            var serialized = new SerializedObject(manager);
            SerializedProperty list = serialized.FindProperty("upgrades");
            list.arraySize = upgrades.Length;
            for (int i = 0; i < upgrades.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        // A panel along the bottom of the screen with one row per upgrade. It's created
        // after the joystick so its buttons sit on top and get taps first.
        // A red banner under the money when a customer walks out over a price.
        private static void CreatePriceyAlert(Transform hud)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("PriceyAlert", hud);
            Stretch(root);
            var alert = root.gameObject.AddComponent<BubbleTea.UI.PriceyAlert>();

            RectTransform banner = CreateUiObject("Banner", root);
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);
            banner.anchoredPosition = new Vector2(0f, -HudMargin - 120f * HudScale - 20f);
            banner.sizeDelta = new Vector2(760f, 130f);
            var image = banner.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.82f, 0.26f, 0.24f, 0.92f);
            image.raycastTarget = false;

            Text text = CreateHudText("Text", banner, font, 36, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            text.text = "A customer left: too pricey! (-0.5 stars)\nConsider lowering your prices.";
            SetReference(alert, "banner", banner.gameObject);
        }

        // A banner when workers stop because their wages couldn't be paid.
        private static void CreateStaffUnpaidAlert(Transform hud)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("StaffUnpaidAlert", hud);
            Stretch(root);
            var alert = root.gameObject.AddComponent<StaffUnpaidAlert>();

            RectTransform banner = CreateUiObject("Banner", root);
            banner.anchorMin = banner.anchorMax = new Vector2(0.5f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);
            banner.anchoredPosition = new Vector2(0f, -HudMargin - 120f * HudScale - 170f); // under the pricey alert
            banner.sizeDelta = new Vector2(760f, 130f);
            var image = banner.gameObject.AddComponent<Image>();
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.color = new Color(0.55f, 0.2f, 0.5f, 0.92f);
            image.raycastTarget = false;

            Text text = CreateHudText("Text", banner, font, 36, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.raycastTarget = false;
            SetReference(alert, "banner", banner.gameObject);
            SetReference(alert, "label", text);
        }

        private static void CreateUpgradePanel(Transform hud, UpgradeManager manager, UpgradeDefinition[] upgrades)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            const float rowHeight = 130f;
            const float titleHeight = 110f;

            RectTransform root = CreateUiObject("Upgrades", hud);
            Stretch(root);
            var panelSwitch = root.gameObject.AddComponent<UpgradePanel>();

            RectTransform panel = CreateUiObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
            panel.pivot = new Vector2(0.5f, 0f);
            panel.anchoredPosition = new Vector2(0f, 60f);
            panel.sizeDelta = new Vector2(960f, titleHeight + rowHeight * upgrades.Length + 30f);
            panel.gameObject.AddComponent<FitToScreen>();
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            SetReference(panelSwitch, "panel", panel.gameObject);

            Text title = CreateHudText("Title", panel, font, 56, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 0f, titleHeight, 0f, 0f);
            title.text = "UPGRADES";

            // The game is paused while the menu is open; this closes it and carries on.
            Button close = CreateMenuButton("Close", panel, "X", rounded, font, new Color(0.85f, 0.35f, 0.35f), out _);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-20f, -15f);
            closeRect.sizeDelta = new Vector2(90f, 80f);
            SetReference(panelSwitch, "closeButton", close);

            for (int i = 0; i < upgrades.Length; i++)
            {
                RectTransform row = CreateUiObject(upgrades[i].DisplayName, panel);
                SetTopRow(row, titleHeight + i * rowHeight, rowHeight, 40f, 40f);

                Text nameLabel = CreateHudText("Name", row, font, 46, TextAnchor.MiddleLeft);
                SetColumn(nameLabel.rectTransform, 0f, 0.5f);
                Text levelLabel = CreateHudText("Level", row, font, 40, TextAnchor.MiddleCenter);
                SetColumn(levelLabel.rectTransform, 0.5f, 0.68f);

                RectTransform buttonRect = CreateUiObject("Buy", row);
                SetColumn(buttonRect, 0.7f, 1f);
                buttonRect.offsetMin = new Vector2(0f, 15f);
                buttonRect.offsetMax = new Vector2(0f, -15f);
                var buttonImage = buttonRect.gameObject.AddComponent<Image>();
                buttonImage.sprite = rounded;
                buttonImage.type = Image.Type.Sliced;
                buttonImage.color = new Color(0.35f, 0.78f, 0.38f);
                var button = buttonRect.gameObject.AddComponent<Button>();
                button.targetGraphic = buttonImage;

                Text priceLabel = CreateHudText("Price", buttonRect, font, 44, TextAnchor.MiddleCenter);
                Stretch(priceLabel.rectTransform);

                var view = row.gameObject.AddComponent<UpgradeRow>();
                SetReference(view, "manager", manager);
                SetReference(view, "upgrade", upgrades[i]);
                SetReference(view, "nameLabel", nameLabel);
                SetReference(view, "levelLabel", levelLabel);
                SetReference(view, "buyButton", button);
                SetReference(view, "priceLabel", priceLabel);
            }
        }

        private static Text CreateHudText(string name, Transform parent, Font font, int size, TextAnchor alignment)
        {
            RectTransform rect = CreateUiObject(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        // Stretches across the parent's width, at a fixed distance from its top edge.
        private static void SetTopRow(RectTransform rect, float top, float height, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        // Fills the parent's height, between two fractions of its width.
        private static void SetColumn(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(from, 0f);
            rect.anchorMax = new Vector2(to, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Main customers walk in from the bottom-left, queue behind the counter, and leave to
        // the left. Cars drive up from the bottom of the expansion and leave to the east.
        private static CustomerSpawner CreateCustomerSpawner(string name, string lane, Customer prefab, Vector3 position,
            Vector3 exitPosition, float secondsBetween, int max)
        {
            var exit = new GameObject(name + " Exit");
            exit.transform.position = exitPosition;

            var go = new GameObject(name);
            go.transform.position = position;
            var spawner = go.AddComponent<CustomerSpawner>();
            SetReference(spawner, "customerPrefab", prefab);
            SetReference(spawner, "exitPoint", exit.transform);

            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("lane").stringValue = lane;
            serialized.FindProperty("secondsBetweenCustomers").floatValue = secondsBetween;
            serialized.FindProperty("maxCustomers").intValue = max;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return spawner;
        }

        private static UnlockPad CreatePad(BuildableDefinition definition, Vector3 position, CashBill billPrefab,
            Wallet wallet, Material material, params UnlockPad[] next)
        {
            var go = new GameObject($"UnlockPad - {definition.DisplayName}");
            go.transform.position = position;

            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Tile";
            Object.DestroyImmediate(tile.GetComponent<BoxCollider>());
            tile.transform.SetParent(go.transform, false);
            tile.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            tile.transform.localScale = new Vector3(2.4f, 0.02f, 2.4f);
            tile.GetComponent<Renderer>().sharedMaterial = material;

            var pad = go.AddComponent<UnlockPad>();
            SetReference(pad, "definition", definition);
            SetReference(pad, "wallet", wallet);
            SetReference(pad, "billPrefab", billPrefab);
            var serializedPad = new SerializedObject(pad);
            serializedPad.FindProperty("saveId").stringValue = definition.name;
            serializedPad.ApplyModifiedPropertiesWithoutUndo();
            if (next.Length > 0)
            {
                var serialized = new SerializedObject(pad);
                SerializedProperty list = serialized.FindProperty("unlocksNext");
                list.arraySize = next.Length;
                for (int i = 0; i < next.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = next[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            CreatePadLabel(go.transform, pad);
            return pad;
        }

        // Makes a pad's tile, label and stand-on area a different size (pads are 2.4 m by default).
        private static void ResizePad(UnlockPad pad, float size)
        {
            float factor = size / 2.4f;
            Transform tile = pad.transform.Find("Tile");
            if (tile != null) tile.localScale = new Vector3(size, tile.localScale.y, size);
            Transform label = pad.transform.Find("Label");
            if (label != null) label.localScale *= factor;
            var serialized = new SerializedObject(pad);
            serialized.FindProperty("halfSize").floatValue = size * 0.5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // A world-space canvas lying flat on the pad, readable from the top-down camera.
        private static void CreatePadLabel(Transform parent, UnlockPad pad)
        {
            var canvasGo = new GameObject("Label", typeof(RectTransform));
            var rect = (RectTransform)canvasGo.transform;
            rect.SetParent(parent, false);
            rect.localPosition = new Vector3(0f, 0.03f, 0f);
            rect.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rect.localScale = Vector3.one * 0.01f;
            rect.sizeDelta = new Vector2(220f, 220f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            Sprite square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform fillRect = CreateUiObject("Fill", rect);
            Stretch(fillRect);
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = square;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;
            fill.color = new Color(0.35f, 0.78f, 0.38f, 0.8f);
            fill.raycastTarget = false;

            Text nameLabel = CreateLabel("Name", rect, font, 30, new Vector2(0f, 60f));
            Text priceLabel = CreateLabel("Price", rect, font, 64, new Vector2(0f, -15f));

            var view = canvasGo.AddComponent<UnlockPadView>();
            SetReference(view, "pad", pad);
            SetReference(view, "nameLabel", nameLabel);
            SetReference(view, "priceLabel", priceLabel);
            SetReference(view, "fill", fill);
        }

        private static Text CreateLabel(string name, RectTransform parent, Font font, int size, Vector2 position)
        {
            RectTransform labelRect = CreateUiObject(name, parent);
            labelRect.sizeDelta = new Vector2(220f, 80f);
            labelRect.anchoredPosition = position;
            var label = labelRect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.color = new Color(0.2f, 0.2f, 0.25f);
            label.raycastTarget = false;
            return label;
        }

        private static CashBill CreateBillPrefab(Material material)
        {
            GameObject bill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bill.name = "CashBill";
            Object.DestroyImmediate(bill.GetComponent<BoxCollider>());
            bill.transform.localScale = new Vector3(0.44f, 0.05f, 0.24f);
            bill.GetComponent<Renderer>().sharedMaterial = material;
            bill.AddComponent<CashBill>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bill, $"{PrefabFolder}/CashBill.prefab");
            Object.DestroyImmediate(bill);
            return prefab.GetComponent<CashBill>();
        }

        // Lays a stack out side by side (columns across, rows front to back) at the given item size.
        internal static void SetStackGrid(SerializedObject serialized, string stack, int columns, int rows,
            float spacing, float itemScale)
        {
            serialized.FindProperty(stack + ".columns").intValue = columns;
            serialized.FindProperty(stack + ".rows").intValue = rows;
            serialized.FindProperty(stack + ".spacing").vector2Value = new Vector2(spacing, spacing);
            serialized.FindProperty(stack + ".itemScale").floatValue = itemScale;
        }

        /// <summary>Every drink in the world (bars, counter, trays, order bubbles) is shown this big.</summary>
        internal const float DrinkSize = 2f;

        // Drinks on a tray are a bit smaller than elsewhere, so the tray stays compact.
        internal const float TrayDrinkSize = DrinkSize * 0.72f;

        // Gap between drinks on a tray, so the smaller drinks fit side by side.
        internal const float TraySpacing = 0.33f;

        // A lacquered serving tray under a carrier's hands: red in the Boba Shop, green in the
        // Matcha House. The ItemCarrier shows it only while something is being carried. Drinks sit
        // in a 3 x 2 grid on top.
        internal static GameObject AddTray(Transform hands, bool matcha)
        {
            var tray = new GameObject("Tray");
            tray.transform.SetParent(hands, false);
            Material lacquer = matcha
                ? GetOrCreateMaterial("TrayLacquerMatcha", new Color(0.33f, 0.52f, 0.26f))
                : GetOrCreateMaterial("TrayLacquer", new Color(0.72f, 0.16f, 0.14f));
            Material gold = GetOrCreateMaterial("TrayGold", new Color(0.93f, 0.72f, 0.30f));
            const float width = 1.08f, depth = 0.74f;
            TrayPart(tray.transform, new Vector3(0f, -0.02f, 0f), new Vector3(width, 0.035f, depth), lacquer);
            TrayPart(tray.transform, new Vector3(0f, 0.01f, depth / 2f), new Vector3(width, 0.05f, 0.03f), gold);
            TrayPart(tray.transform, new Vector3(0f, 0.01f, -depth / 2f), new Vector3(width, 0.05f, 0.03f), gold);
            TrayPart(tray.transform, new Vector3(width / 2f, 0.01f, 0f), new Vector3(0.03f, 0.05f, depth), gold);
            TrayPart(tray.transform, new Vector3(-width / 2f, 0.01f, 0f), new Vector3(0.03f, 0.05f, depth), gold);
            tray.SetActive(false);
            return tray;
        }

        private static void TrayPart(Transform parent, Vector3 position, Vector3 size, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.name = "TrayPart";
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        internal static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // The first shop must be scene 0 (the game starts there); other shops go after it.
        // Puts a scene in the build at this index (-1 = at the end).
        private static void AddSceneToBuild(string path, int index)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            if (index >= 0) scenes.Insert(Mathf.Min(index, scenes.Count), new EditorBuildSettingsScene(path, true));
            else scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Fills in a private [SerializeField] the same way dragging it in the Inspector would.
        internal static void SetReference(Object component, string fieldName, Object value)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"Bubble Tea: {component.GetType().Name} has no field '{fieldName}'.");
                return;
            }
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
