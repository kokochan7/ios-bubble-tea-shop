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
        /// The first shop is small and each later shop is bigger.
        /// </summary>
        internal sealed class ShopLayout
        {
            public float HalfWidth, HalfDepth;
            public float DoorFrom, DoorTo;              // the front doorway in the south wall
            public float StaffDoorFrom, StaffDoorTo;    // the gap in the glass wall beside the counter
            public float LaneWallX;                     // the east end of the expansion
            public Vector3 Counter, Seating, DriveThru, TrashCan, FloorCash, PlayerStart;
            public Vector3 TeaBrewer, ToppingBar, TaroStation, FruitBar, UpgradeDesk, Cashier;
            public Vector3 Expansion, MoreSeating, Cleaner, DriveThruCashier, SecondShopPad, TravelBack;
            public Vector3[] Baristas;
            public float[] Windows, Lamps, ExpansionWindows, ExpansionLamps; // x positions on the back wall
            public Vector3[] Trees;

            public float PartitionZ => Counter.z;        // glass wall between the dining room and the kitchen
            public float LaneX => LaneWallX + 2.5f;      // middle of the drive-thru lane
            public float LaneOuterX => LaneWallX + 6f;   // far edge of the lane
            public float SidewalkZ => -HalfDepth - 3f;

            // Plants (or whatever the Art Set lists as decorations) in the corners of the shop.
            public Vector3[] DecorationSpots => new Vector3[]
            {
                new(-HalfWidth + 1f, 0f, HalfDepth - 1f), new(HalfWidth - 1f, 0f, HalfDepth - 1f),
                new(-HalfWidth + 1f, 0f, -HalfDepth + 1f), new(HalfWidth - 0.8f, 0f, -HalfDepth + 0.8f),
                new(-HalfWidth + 1f, 0f, -2f), new(HalfWidth - 1f, 0f, -2f),
            };
        }

        // Boba Shop: a small corner café, 22 x 20 meters.
        private static readonly ShopLayout Small = new()
        {
            HalfWidth = 11f, HalfDepth = 10f,
            DoorFrom = -5f, DoorTo = 3f,
            StaffDoorFrom = 3f, StaffDoorTo = 5.5f,
            LaneWallX = 18f,
            Counter = new(-2f, 0f, 0f),
            Seating = new(5.5f, 0f, -6.8f),
            DriveThru = new(17f, 0f, 4f),
            TrashCan = new(-9.5f, 0f, 1f),
            FloorCash = new(7.5f, 0f, 2.2f),
            PlayerStart = new(5f, 1f, 2.2f),
            TeaBrewer = new(-8.5f, 0f, 5.5f),
            ToppingBar = new(-4.5f, 0f, 5.5f),
            TaroStation = new(-0.5f, 0f, 5.5f),
            FruitBar = new(3.5f, 0f, 5.5f),
            UpgradeDesk = new(9f, 0f, 4.5f),
            Cashier = new(1f, 0f, 1.8f),
            Expansion = new(7f, 0f, 8.6f),
            MoreSeating = new(14.5f, 0f, -6f),
            Cleaner = new(-8f, 0f, -7f),
            DriveThruCashier = new(14f, 0f, 6.5f),
            SecondShopPad = new(16.6f, 0f, 8.2f),
            TravelBack = new(8f, 0f, -8f),
            Baristas = new Vector3[] { new(-8f, 0f, 8.6f), new(-5f, 0f, 8.6f), new(-2f, 0f, 8.6f), new(1f, 0f, 8.6f), new(4f, 0f, 8.6f) },
            Windows = new[] { -7.5f, 7.5f },
            Lamps = new[] { -5f, 5f },
            ExpansionWindows = new[] { 13.5f },
            ExpansionLamps = new[] { 16.5f },
            Trees = new Vector3[]
            {
                new(-15f, 0f, -12f), new(-15f, 0f, -3f), new(-15f, 0f, 6f), new(-15f, 0f, 13f),
                new(-8f, 0f, 14f), new(0f, 0f, 14.5f), new(8f, 0f, 14f), new(15f, 0f, 14.5f), new(21f, 0f, 14f),
                new(27f, 0f, 9f), new(27f, 0f, 0f), new(27f, 0f, -9f), new(4f, 0f, -13.5f), new(12f, 0f, -13.5f),
            },
        };

        // Matcha House and later shops: the full 30 x 30 meter café.
        private static readonly ShopLayout Large = new()
        {
            HalfWidth = 15f, HalfDepth = 15f,
            DoorFrom = -5.5f, DoorTo = 6.5f,
            StaffDoorFrom = 2f, StaffDoorTo = 4.5f,
            LaneWallX = 25.5f,
            Counter = new(-3f, 0f, 1f),
            Seating = new(7f, 0f, -7f),
            DriveThru = new(24.5f, 0f, 5f),
            TrashCan = new(-8f, 0f, 2.2f),
            FloorCash = new(8f, 0f, 3f),
            PlayerStart = new(5f, 1f, 3f),
            TeaBrewer = new(-9f, 0f, 7f),
            ToppingBar = new(-4f, 0f, 7f),
            TaroStation = new(1f, 0f, 7f),
            FruitBar = new(6f, 0f, 7f),
            UpgradeDesk = new(12.5f, 0f, 9f),
            Cashier = new(0f, 0f, 2.8f),
            Expansion = new(8f, 0f, 11.5f),
            MoreSeating = new(20f, 0f, -8f),
            Cleaner = new(-10f, 0f, -11f),
            DriveThruCashier = new(21.5f, 0f, 7.5f),
            SecondShopPad = new(18f, 0f, 11.5f),
            TravelBack = new(12f, 0f, -12f),
            Baristas = new Vector3[] { new(-12f, 0f, 12f), new(-8f, 0f, 12f), new(-4f, 0f, 12f), new(0f, 0f, 12f), new(4f, 0f, 12f) },
            Windows = new[] { -11f, -5.5f, 5.5f, 11f },
            Lamps = new[] { -8.25f, 8.25f },
            ExpansionWindows = new[] { 18.5f },
            ExpansionLamps = new[] { 22.5f },
            Trees = new Vector3[]
            {
                new(-19f, 0f, -17f), new(-19f, 0f, -6f), new(-19f, 0f, 5f), new(-19f, 0f, 14f),
                new(-12f, 0f, 19f), new(-2f, 0f, 19.5f), new(9f, 0f, 19f), new(19f, 0f, 19.5f), new(28f, 0f, 19f),
                new(35f, 0f, 12f), new(35f, 0f, 1f), new(35f, 0f, -10f), new(4f, 0f, -18.5f), new(16f, 0f, -18.5f),
            },
        };

        /// <summary>The layout of the shop being built (read by <see cref="ShopDressing"/>).</summary>
        internal static ShopLayout Layout { get; private set; } = Large;

        private static readonly ShopPlan FirstShop = new()
        {
            Number = 1,
            ScenePath = ScenePath,
            SaveFile = "save.json",
            FloorMaterial = "Floor",
            FloorColor = new Color(0.93f, 0.87f, 0.78f),
            WallMaterial = "Wall",
            WallColor = new Color(0.85f, 0.78f, 0.70f),
            ExpansionFloorMaterial = "ExpansionFloor",
            ExpansionFloorColor = new Color(0.86f, 0.90f, 0.82f),
            Look = new ShopDressing.Style
            {
                ShopName = "Boba Shop",
                Prefix = "Shop1",
                Accent = new Color(0.91f, 0.51f, 0.60f),
                Accent2 = new Color(0.66f, 0.88f, 0.80f),
                Grass = new Color(0.62f, 0.80f, 0.52f),
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
            FloorColor = new Color(0.90f, 0.94f, 0.84f),
            WallMaterial = "Shop2Wall",
            WallColor = new Color(0.66f, 0.80f, 0.60f),
            ExpansionFloorMaterial = "Shop2ExpansionFloor",
            ExpansionFloorColor = new Color(0.97f, 0.88f, 0.90f),
            Look = new ShopDressing.Style
            {
                ShopName = "Matcha House",
                Prefix = "Shop2",
                Accent = new Color(0.42f, 0.66f, 0.42f),
                Accent2 = new Color(0.98f, 0.86f, 0.62f),
                Grass = new Color(0.70f, 0.82f, 0.56f),
            },
            Music = MusicMaker.Track.MatchaHouse,
            Layout = Large,
        };

        [MenuItem("Bubble Tea/Build Prototype Scene")]
        public static void Build() => BuildShop(FirstShop);

        /// <summary>Builds the Matcha House, the second shop that the first one unlocks.</summary>
        [MenuItem("Bubble Tea/Build Second Shop Scene")]
        public static void BuildSecondShop() => BuildShop(SecondShop);

        /// <summary>Builds the title screen the game opens on: Play and Settings.</summary>
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

            // A soft pink background with tapioca pearls scattered around it.
            RectTransform background = CreateUiObject("Background", root);
            Stretch(background);
            background.gameObject.AddComponent<Image>().color = new Color(0.99f, 0.86f, 0.88f);
            var pearl = new Color(0.36f, 0.22f, 0.16f, 0.18f);
            Vector2[] pearls =
            {
                new(0.08f, 0.92f), new(0.9f, 0.86f), new(0.15f, 0.62f), new(0.88f, 0.55f), new(0.06f, 0.3f),
                new(0.93f, 0.22f), new(0.25f, 0.08f), new(0.72f, 0.06f), new(0.5f, 0.97f), new(0.8f, 0.38f),
            };
            for (int i = 0; i < pearls.Length; i++)
            {
                RectTransform dot = CreateUiObject("Pearl", background);
                dot.anchorMin = dot.anchorMax = pearls[i];
                float size = 70f + 50f * Mathf.Repeat(i * 0.618f, 1f);
                dot.sizeDelta = new Vector2(size, size);
                var dotImage = dot.gameObject.AddComponent<Image>();
                dotImage.sprite = circle;
                dotImage.color = pearl;
                dotImage.raycastTarget = false;
            }

            // The title.
            Text title = CreateHudText("Title", root, font, 130, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 220f, 300f, 40f, 40f);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.text = "BUBBLE TEA\nSHOP";
            title.color = new Color(0.91f, 0.42f, 0.53f);
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(5f, -5f);
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -12f);

            // A big cup of milk tea, drawn from rounded shapes, bobbing in the middle.
            RectTransform cup = CreateUiObject("Cup", root);
            cup.anchorMin = cup.anchorMax = new Vector2(0.5f, 0.5f);
            cup.anchoredPosition = new Vector2(0f, 60f);
            cup.sizeDelta = new Vector2(360f, 480f);
            RectTransform straw = MenuShape("Straw", cup, rounded, new Color(0.95f, 0.45f, 0.55f), new Vector2(46f, 380f), new Vector2(60f, 250f));
            straw.localRotation = Quaternion.Euler(0f, 0f, -12f);
            MenuShape("Body", cup, rounded, new Color(0.87f, 0.72f, 0.56f), new Vector2(340f, 440f), new Vector2(0f, -20f));
            MenuShape("Shine", cup, rounded, new Color(1f, 1f, 1f, 0.35f), new Vector2(40f, 300f), new Vector2(-115f, 10f));
            for (int i = 0; i < 9; i++)
            {
                float x = -110f + (i % 5) * 55f + (i >= 5 ? 27f : 0f);
                float y = i >= 5 ? -150f : -195f;
                MenuShape("Pearl", cup, circle, new Color(0.27f, 0.16f, 0.12f), new Vector2(56f, 56f), new Vector2(x, y));
            }
            MenuShape("Lid", cup, rounded, new Color(1f, 0.97f, 0.93f), new Vector2(390f, 60f), new Vector2(0f, 210f));

            // Play and Settings.
            Button play = CreateMenuButton("Play", root, "PLAY", rounded, font, new Color(0.35f, 0.78f, 0.38f), out Text playLabel);
            var playRect = (RectTransform)play.transform;
            playRect.anchorMin = playRect.anchorMax = new Vector2(0.5f, 0f);
            playRect.anchoredPosition = new Vector2(0f, 520f);
            playRect.sizeDelta = new Vector2(620f, 180f);
            playLabel.fontSize = 80;
            Button settingsButton = CreateMenuButton("SettingsButton", root, "SETTINGS", rounded, font,
                new Color(0.55f, 0.45f, 0.8f), out _);
            var settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.anchorMin = settingsRect.anchorMax = new Vector2(0.5f, 0f);
            settingsRect.anchoredPosition = new Vector2(0f, 300f);
            settingsRect.sizeDelta = new Vector2(620f, 150f);

            var menu = canvasGo.AddComponent<MainMenu>();
            SetReference(menu, "playButton", play);
            SetReference(menu, "playLabel", playLabel);
            SetReference(menu, "bobbing", cup);

            // The settings panel: sound, music and vibration (reset and GM tools stay in the shop).
            RectTransform settingsRoot = CreateUiObject("Settings", root);
            Stretch(settingsRoot);
            var settings = settingsRoot.gameObject.AddComponent<SettingsMenu>();
            RectTransform panel = CreateUiObject("Panel", settingsRoot);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(800f, 760f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
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
            Debug.Log($"Bubble Tea: main menu built at {MenuScenePath}. It's first in the build, so the game opens on it.");
        }

        // A rounded shape (or circle) on the main menu's cup.
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

        private static void BuildShop(ShopPlan plan)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(plan.ScenePath) != null &&
                !EditorUtility.DisplayDialog("Rebuild shop scene?",
                    $"{plan.ScenePath} already exists. Rebuilding replaces it (and any changes you made to it).",
                    "Rebuild", "Cancel"))
            {
                return;
            }

            Layout = plan.Layout;
            Material floorMat = GetOrCreateMaterial(plan.FloorMaterial, plan.FloorColor);
            Material playerMat = GetOrCreateMaterial("Player", new Color(0.36f, 0.62f, 0.95f));
            Material cashMat = GetOrCreateMaterial("Cash", new Color(0.35f, 0.78f, 0.38f));
            Material padMat = GetOrCreateMaterial("CashSpot", new Color(0.80f, 0.73f, 0.62f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CashBill billPrefab = CreateBillPrefab(cashMat);

            CreateLight();
            CreateFloor(floorMat);
            Material wallMat = GetOrCreateMaterial(plan.WallMaterial, plan.WallColor);
            GameObject eastWall = CreateWalls(wallMat);
            GameObject expansionArea = CreateExpansionArea(wallMat,
                GetOrCreateMaterial(plan.ExpansionFloorMaterial, plan.ExpansionFloorColor));
            CreateDecorations();
            ShopDressing.Dress(plan.Look, floorMat, GetOrCreateMaterial(plan.ExpansionFloorMaterial, plan.ExpansionFloorColor),
                wallMat, expansionArea);
            CreateMusic(plan.Music);

            GameObject player = CreatePlayer(playerMat, out Wallet wallet);
            Camera camera = CreateCamera(player.transform);
            VirtualJoystick joystick = CreateHud(wallet, out Transform hud);
            CreateEventSystem();

            SetReference(player.GetComponent<PlayerMovement>(), "joystick", joystick);
            SetReference(player.GetComponent<PlayerMovement>(), "cameraTransform", camera.transform);

            CashPile[] piles = CreateCashPiles(billPrefab, padMat);
            CreateTrashCan(padMat);
            CashPileRefiller floorCash = CreateRefiller(piles);
            ShopContentBuilder.Content content = ShopContentBuilder.Build(billPrefab, plan.Number);
            UnlockPad[] pads = CreateUnlockPads(content, billPrefab, wallet, eastWall, expansionArea);
            if (content.TravelBack != null)
            {
                var stop = (GameObject)PrefabUtility.InstantiatePrefab(content.TravelBack);
                stop.transform.position = Layout.TravelBack;
            }
            // Customers come in off the sidewalk through the front door and leave the same way.
            float street = Layout.SidewalkZ;
            CreateCustomerSpawner("CustomerSpawner", "Main", content.CustomerPrefab,
                new Vector3(Layout.Counter.x, 0f, street), new Vector3((Layout.DoorFrom + Layout.DoorTo) / 2f, 0f, street),
                secondsBetween: 3f, max: 4);
            // Cars turn in from the road, drive up the lane past the window and out the far end.
            CreateCustomerSpawner("CarSpawner", "DriveThru", content.CarPrefab,
                new Vector3(Layout.LaneX, 0f, street - 4f), new Vector3(Layout.LaneX, 0f, Layout.HalfDepth + 9f),
                secondsBetween: 6f, max: 3);
            UpgradeManager upgrades = CreateUpgradeManager(wallet, content.Upgrades);
            CreateUpgradePanel(hud, upgrades, content.Upgrades);
            OfflineEarnings offlineEarnings = CreateOfflineEarnings(wallet);
            DailyGoals dailyGoals = CreateDailyGoals(wallet, content.Menu, offlineEarnings);
            ShopRating rating = CreateShopRating(pads, content);
            SaveManager saveManager = CreateSaveManager(wallet, pads, upgrades, offlineEarnings, dailyGoals, rating, floorCash);
            var saveFile = new SerializedObject(saveManager);
            saveFile.FindProperty("fileName").stringValue = plan.SaveFile;
            saveFile.ApplyModifiedPropertiesWithoutUndo();
            CreateFeedback();
            CreateSettingsMenu(hud, saveManager, wallet, upgrades);
            CreateGoalsPanel(hud, dailyGoals);
            CreateClock(hud);
            CreatePricePanel(hud, content.Menu);
            CreateRatingView(hud, rating);
            CreateWelcomeBackPopup(hud, offlineEarnings);
            CreateGameOverPopup(hud, rating, saveManager);

            EditorSceneManager.SaveScene(scene, plan.ScenePath);
            // The first shop goes first in the build, right after the main menu if there is one.
            EditorBuildSettingsScene[] inBuild = EditorBuildSettings.scenes;
            bool menuFirst = inBuild.Length > 0 && inBuild[0].path == MenuScenePath;
            AddSceneToBuild(plan.ScenePath, plan.Number == 1 ? (menuFirst ? 1 : 0) : -1);
            Selection.activeGameObject = player;

            Debug.Log($"Bubble Tea: shop {plan.Number} built at {plan.ScenePath}. Press Play to try it.");
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
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.90f); // warm afternoon sun
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Sky, horizon and ground colors light the shop from all sides, softer than one flat color.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.80f, 0.88f);
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.66f, 0.64f);
            RenderSettings.ambientGroundColor = new Color(0.52f, 0.47f, 0.44f);
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
            // The front (south) wall has a wide doorway; an invisible bar across it keeps the
            // player inside (customers don't use physics, so they walk through it).
            CreateWall(root.transform, "North", new Vector3(0f, 1.5f, d + 0.25f), new Vector3(2f * w + 0.5f, 3f, 0.5f), wallMat);
            CreateWall(root.transform, "South (west of door)", new Vector3((-w - 0.25f + from) / 2f, 0.5f, -d - 0.25f),
                new Vector3(from + w + 0.25f, 1f, 0.5f), wallMat);
            CreateWall(root.transform, "South (east of door)", new Vector3((w + 0.25f + to) / 2f, 0.5f, -d - 0.25f),
                new Vector3(w + 0.25f - to, 1f, 0.5f), wallMat);
            CreateBlocker(root.transform, "Door (keeps the player in)", new Vector3((from + to) / 2f, 1f, -d - 0.25f),
                new Vector3(to - from, 2f, 0.5f));
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
            float windowFrom = windowZ - 1.2f, windowTo = windowZ + 1.2f, end = d + 0.5f;
            CreateWall(root.transform, "Lane Wall (south)", new Vector3(laneWall + 0.25f, 1.2f, (-end + windowFrom) / 2f),
                new Vector3(0.5f, 2.4f, windowFrom + end), wallMat);
            CreateWall(root.transform, "Lane Wall (north)", new Vector3(laneWall + 0.25f, 1.2f, (windowTo + end) / 2f),
                new Vector3(0.5f, 2.4f, end - windowTo), wallMat);
            CreateBlocker(root.transform, "Window (keeps the player in)", new Vector3(laneWall + 0.25f, 1f, windowZ),
                new Vector3(0.5f, 2f, 2.4f));

            root.SetActive(false);
            return root;
        }

        private static void CreateDecorations()
        {
            ArtSet art = ArtLibrary.Load();
            if (art.Decorations.Count == 0) return;

            var root = new GameObject("Decorations").transform;
            Vector3[] spots = Layout.DecorationSpots;
            for (int i = 0; i < spots.Length; i++)
            {
                GameObject model = art.Decorations[i % art.Decorations.Count];
                if (model == null) continue;
                var spot = new GameObject(model.name).transform;
                spot.SetParent(root, false);
                spot.localPosition = spots[i];
                AttachLoose(model, spot, 1.2f);
            }
        }

        // Places a model (not tied to a slot) standing on the spot, about this tall.
        private static void AttachLoose(GameObject model, Transform spot, float height)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(spot, false);
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = bounds.size.y > 0.0001f ? height / bounds.size.y : 1f;
            instance.transform.localScale = Vector3.one * scale;
            instance.transform.localPosition = -(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - spot.position) * scale;
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

        private static GameObject CreatePlayer(Material material, out Wallet wallet)
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
            GameObject model = ArtLibrary.Attach("Player", player.transform, new Vector3(0.8f, 1.8f, 0.8f),
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

            // Items the player carries pile up in front of them.
            var hands = new GameObject("HandStack").transform;
            hands.SetParent(player.transform, false);
            hands.localPosition = new Vector3(0f, 0.1f, 0.7f);
            var carrier = player.AddComponent<ItemCarrier>();
            var serializedCarrier = new SerializedObject(carrier);
            serializedCarrier.FindProperty("stack.anchor").objectReferenceValue = hands;
            serializedCarrier.FindProperty("stack.capacity").intValue = 6;
            serializedCarrier.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        private static Camera CreateCamera(Transform player)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.74f, 0.87f, 0.96f);
            camera.allowMSAA = true;
            go.AddComponent<AudioListener>();

            var follow = go.AddComponent<CameraFollow>();
            SetReference(follow, "target", player);

            // Place it now too, so the Scene and Game views look right before pressing Play.
            go.transform.position = player.position + new Vector3(0f, 13f, -9f);
            go.transform.LookAt(player.position);
            return camera;
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

            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // Joystick: an invisible full-screen touch area, with the stick drawn where you touch.
            RectTransform area = CreateUiObject("JoystickArea", canvasGo.transform);
            Stretch(area);
            var areaImage = area.gameObject.AddComponent<Image>();
            areaImage.color = new Color(0f, 0f, 0f, 0f);

            RectTransform ring = CreateUiObject("Background", area);
            ring.sizeDelta = new Vector2(240f, 240f);
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = circle;
            ringImage.color = new Color(1f, 1f, 1f, 0.35f);
            ringImage.raycastTarget = false;

            RectTransform knob = CreateUiObject("Handle", ring);
            knob.sizeDelta = new Vector2(110f, 110f);
            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = circle;
            knobImage.color = new Color(1f, 1f, 1f, 0.85f);
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

            RectTransform panel = CreateUiObject("MoneyCounter", safeArea);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -110f);
            panel.sizeDelta = new Vector2(380f, 120f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.75f);
            panelImage.raycastTarget = false;

            RectTransform labelRect = CreateUiObject("Amount", panel);
            Stretch(labelRect);
            var label = labelRect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 64;
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

        private static CashPile[] CreateCashPiles(CashBill billPrefab, Material padMaterial)
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
                pileSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            return piles;
        }

        // Keeps the floor money topped up until it has handed out $500 in total.
        private static CashPileRefiller CreateRefiller(CashPile[] piles)
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
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return refiller;
        }

        // Pads in a chain: the counter reveals the tea brewer and the upgrade desk, then
        // tea brewer -> topping bar -> taro station -> fruit bar. The topping bar also
        // reveals the seating area, which reveals
        // the cleaner. The fruit bar reveals Expand Shop, which opens the east wall and reveals
        // the drive-through and more seating.
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
            // The drive-thru cashier's pad sits beside the drive-thru's cash pile, like the main one.
            UnlockPad driveThruCashierPad = CreatePad(content.DriveThruCashier, Layout.DriveThruCashier, billPrefab, wallet, padMat);
            // The drive-thru window sits in the lane wall, its customer side facing the cars outside.
            UnlockPad driveThruPad = CreatePad(content.DriveThru, Layout.DriveThru, billPrefab, wallet, padMat,
                driveThruCashierPad);
            Turn(driveThruPad, -90f);
            UnlockPad moreSeatingPad = CreatePad(content.MoreSeating, Layout.MoreSeating, billPrefab, wallet, padMat);
            UnlockPad expansionPad = CreatePad(content.Expansion, Layout.Expansion, billPrefab, wallet, padMat,
                driveThruPad, moreSeatingPad);
            SetObjectList(expansionPad, "showWhenUnlocked", expansionArea);
            SetObjectList(expansionPad, "hideWhenUnlocked", eastWall);
            // The bars stand in a row behind the counter, turned so you serve them from the counter side.
            UnlockPad fruitPad = CreatePad(content.FruitBar, Layout.FruitBar, billPrefab, wallet, padMat,
                expansionPad);
            Turn(fruitPad, 180f);
            UnlockPad taroPad = CreatePad(content.TaroStation, Layout.TaroStation, billPrefab, wallet, padMat, fruitPad);
            Turn(taroPad, 180f);

            // Baristas along the back wall of the kitchen, each pad revealing the next.
            Vector3[] baristaSpots = Layout.Baristas;
            var baristaPads = new UnlockPad[content.Baristas.Length];
            for (int i = baristaPads.Length - 1; i >= 0; i--)
            {
                baristaPads[i] = i + 1 < baristaPads.Length
                    ? CreatePad(content.Baristas[i], baristaSpots[i], billPrefab, wallet, padMat, baristaPads[i + 1])
                    : CreatePad(content.Baristas[i], baristaSpots[i], billPrefab, wallet, padMat);
            }

            // The decorations (Planter, Menu Board, Boba Statue) are left out for now; their
            // prefabs and data are still built, so they can come back as pads later.
            // Dining room: café tables beside the line, the cleaner's pad by the west wall.
            UnlockPad cleanerPad = CreatePad(content.Cleaner, Layout.Cleaner, billPrefab, wallet, padMat);
            UnlockPad seatingPad = CreatePad(content.Seating, Layout.Seating, billPrefab, wallet, padMat, cleanerPad);
            UnlockPad toppingPad = CreatePad(content.ToppingBar, Layout.ToppingBar, billPrefab, wallet, padMat,
                taroPad, seatingPad);
            Turn(toppingPad, 180f);
            // The cashier stands behind the counter beside its cash pile.
            UnlockPad cashierPad = CreatePad(content.Cashier, Layout.Cashier, billPrefab, wallet, padMat);
            UnlockPad brewerPad = CreatePad(content.TeaBrewer, Layout.TeaBrewer, billPrefab, wallet, padMat, toppingPad,
                cashierPad);
            Turn(brewerPad, 180f);
            UnlockPad upgradePad = CreatePad(content.UpgradeDesk, Layout.UpgradeDesk, billPrefab, wallet, padMat);
            UnlockPad counterPad = CreatePad(content.Counter, Layout.Counter, billPrefab, wallet, padMat,
                brewerPad, upgradePad);

            // Only the first pad is visible at the start.
            toppingPad.gameObject.SetActive(false);
            brewerPad.gameObject.SetActive(false);
            upgradePad.gameObject.SetActive(false);
            taroPad.gameObject.SetActive(false);
            fruitPad.gameObject.SetActive(false);
            expansionPad.gameObject.SetActive(false);
            driveThruPad.gameObject.SetActive(false);
            seatingPad.gameObject.SetActive(false);
            cashierPad.gameObject.SetActive(false);
            cleanerPad.gameObject.SetActive(false);
            moreSeatingPad.gameObject.SetActive(false);
            driveThruCashierPad.gameObject.SetActive(false);
            // The first barista can be hired from the start; each one reveals the next.
            for (int i = 1; i < baristaPads.Length; i++) baristaPads[i].gameObject.SetActive(false);
            var all = new System.Collections.Generic.List<UnlockPad>
            {
                counterPad, brewerPad, toppingPad, upgradePad, taroPad, fruitPad,
                expansionPad, driveThruPad, seatingPad, cashierPad, cleanerPad, moreSeatingPad, driveThruCashierPad,
            };
            all.AddRange(baristaPads);
            if (secondShopPad != null)
            {
                var reveal = new GameObject("Level 2 Unlock").AddComponent<RevealWhenAllUnlocked>();
                SetReferences(reveal, "required", all.ToArray());
                SetReference(reveal, "target", secondShopPad);
                secondShopPad.gameObject.SetActive(false);
                all.Add(secondShopPad);
            }
            return all.ToArray();
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
            openRect.anchoredPosition = new Vector2(-40f, -235f);
            openRect.sizeDelta = new Vector2(200f, 110f);

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
            hint.text = "New goals every day at midnight";

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
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.anchoredPosition = new Vector2(0f, -240f);
            root.sizeDelta = new Vector2(5 * starSize + 130f, starSize + 10f);
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
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 1f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            hint.rectTransform.sizeDelta = new Vector2(900f, 100f);
            hint.horizontalOverflow = HorizontalWrapMode.Wrap;
            hint.text = "Every customer who walks out costs a star.\nAt 0 stars your shop closes and you start over!";
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

        // Measures how fast the shop earns and pays part of it for time away.
        private static OfflineEarnings CreateOfflineEarnings(Wallet wallet)
        {
            var earnings = new GameObject("OfflineEarnings").AddComponent<OfflineEarnings>();
            SetReference(earnings, "wallet", wallet);
            return earnings;
        }

        // The "Welcome back!" panel. Built last so it draws on top of the other HUD panels.
        private static void CreateWelcomeBackPopup(Transform hud, OfflineEarnings earnings)
        {
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform root = CreateUiObject("WelcomeBack", hud);
            Stretch(root);
            var popup = root.gameObject.AddComponent<WelcomeBackPopup>();

            // A dark full-screen backdrop that also stops taps reaching the joystick.
            RectTransform backdrop = CreateUiObject("Backdrop", root);
            Stretch(backdrop);
            backdrop.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            RectTransform panel = CreateUiObject("Panel", backdrop);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(860f, 760f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.97f);

            Text title = CreateHudText("Title", panel, font, 64, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 30f, 110f, 0f, 0f);
            title.text = "WELCOME BACK!";

            Text subtitle = CreateHudText("Subtitle", panel, font, 40, TextAnchor.MiddleCenter);
            SetTopRow(subtitle.rectTransform, 150f, 60f, 0f, 0f);
            subtitle.text = "Your shop earned";

            Text amount = CreateHudText("Amount", panel, font, 110, TextAnchor.MiddleCenter);
            SetTopRow(amount.rectTransform, 220f, 150f, 0f, 0f);
            amount.color = new Color(0.55f, 0.95f, 0.5f);
            amount.text = "$0";

            Text away = CreateHudText("Away", panel, font, 34, TextAnchor.MiddleCenter);
            SetTopRow(away.rectTransform, 380f, 140f, 40f, 40f);
            away.horizontalOverflow = HorizontalWrapMode.Wrap;
            away.fontStyle = FontStyle.Normal;
            away.color = new Color(0.85f, 0.85f, 0.9f);

            Button collect = CreateMenuButton("Collect", panel, "COLLECT", rounded, font, new Color(0.35f, 0.78f, 0.38f), out _);
            SetTopRow((RectTransform)collect.transform, 560f, 140f, 160f, 160f);

            SetReference(popup, "panel", backdrop.gameObject);
            SetReference(popup, "amountLabel", amount);
            SetReference(popup, "awayLabel", away);
            SetReference(popup, "collectButton", collect);
            SetReference(popup, "earnings", earnings);
        }

        // The shop clock (rush hours) and its display in the top-left corner.
        private static void CreateClock(Transform hud)
        {
            var clock = new GameObject("GameClock").AddComponent<GameClock>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            RectTransform root = CreateUiObject("Clock", hud);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = new Vector2(40f, -110f);
            root.sizeDelta = new Vector2(250f, 120f);
            var back = root.gameObject.AddComponent<Image>();
            back.sprite = rounded;
            back.type = Image.Type.Sliced;
            back.color = new Color(0.15f, 0.15f, 0.2f, 0.75f);
            back.raycastTarget = false;

            Text time = CreateHudText("Time", root, font, 50, TextAnchor.MiddleCenter);
            SetTopRow(time.rectTransform, 8f, 66f, 0f, 0f);
            Text day = CreateHudText("Day", root, font, 32, TextAnchor.MiddleCenter);
            SetTopRow(day.rectTransform, 70f, 44f, 0f, 0f);
            day.color = new Color(0.8f, 0.8f, 0.88f);

            Text period = CreateHudText("Period", root, font, 36, TextAnchor.UpperLeft);
            period.rectTransform.anchorMin = period.rectTransform.anchorMax = new Vector2(0f, 0f);
            period.rectTransform.pivot = new Vector2(0f, 1f);
            period.rectTransform.anchoredPosition = new Vector2(4f, -8f);
            period.rectTransform.sizeDelta = new Vector2(420f, 50f);
            period.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.7f);

            var view = root.gameObject.AddComponent<ClockView>();
            SetReference(view, "clock", clock);
            SetReference(view, "timeLabel", time);
            SetReference(view, "dayLabel", day);
            SetReference(view, "periodLabel", period);
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
            hint.text = "Higher prices earn more, but customers who think it's too much walk out and cost a star.";

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

        // A bin beside the counter. Stand next to it to throw away what you're carrying.
        private static void CreateTrashCan(Material spotMaterial)
        {
            var go = new GameObject("TrashCan");
            go.transform.position = Layout.TrashCan;

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

            var label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            label.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.text = "TRASH";
            label.characterSize = 0.04f;
            label.fontSize = 64;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.white;

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
            openRect.anchoredPosition = new Vector2(-40f, -110f);
            openRect.sizeDelta = new Vector2(200f, 110f);

            RectTransform panel = CreateUiObject("Panel", root);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
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
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = rounded;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
            SetReference(panelSwitch, "panel", panel.gameObject);

            Text title = CreateHudText("Title", panel, font, 56, TextAnchor.MiddleCenter);
            SetTopRow(title.rectTransform, 0f, titleHeight, 0f, 0f);
            title.text = "UPGRADES";

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
        private static void CreateCustomerSpawner(string name, string lane, Customer prefab, Vector3 position,
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
