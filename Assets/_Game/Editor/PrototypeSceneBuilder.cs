using BubbleTea.Building;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Economy;
using BubbleTea.Feedback;
using BubbleTea.Goals;
using BubbleTea.Player;
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
        private const string PrefabFolder = "Assets/_Game/Prefabs";
        private const string MaterialFolder = "Assets/_Game/Materials";

        private static readonly Vector3[] CashPilePositions =
        {
            new(-4f, 0f, 3f),
            new(4f, 0f, 3f),
            new(0f, 0f, 7f),
        };

        [MenuItem("Bubble Tea/Build Prototype Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Rebuild prototype scene?",
                    $"{ScenePath} already exists. Rebuilding replaces it (and any changes you made to it).",
                    "Rebuild", "Cancel"))
            {
                return;
            }

            Material floorMat = GetOrCreateMaterial("Floor", new Color(0.93f, 0.87f, 0.78f));
            Material playerMat = GetOrCreateMaterial("Player", new Color(0.36f, 0.62f, 0.95f));
            Material cashMat = GetOrCreateMaterial("Cash", new Color(0.35f, 0.78f, 0.38f));
            Material padMat = GetOrCreateMaterial("CashSpot", new Color(0.80f, 0.73f, 0.62f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CashBill billPrefab = CreateBillPrefab(cashMat);

            CreateLight();
            CreateFloor(floorMat);
            GameObject eastWall = CreateWalls();
            GameObject expansionArea = CreateExpansionArea();
            CreateDecorations();

            GameObject player = CreatePlayer(playerMat, out Wallet wallet);
            Camera camera = CreateCamera(player.transform);
            VirtualJoystick joystick = CreateHud(wallet, out Transform hud);
            CreateEventSystem();

            SetReference(player.GetComponent<PlayerMovement>(), "joystick", joystick);
            SetReference(player.GetComponent<PlayerMovement>(), "cameraTransform", camera.transform);

            CashPile[] piles = CreateCashPiles(billPrefab, padMat);
            CreateRefiller(piles);
            ShopContentBuilder.Content content = ShopContentBuilder.Build(billPrefab);
            UnlockPad[] pads = CreateUnlockPads(content, billPrefab, wallet, eastWall, expansionArea);
            CreateCustomerSpawner("CustomerSpawner", "Main", content.CustomerPrefab,
                new Vector3(-5f, 0f, -14f), new Vector3(-14f, 0f, -8f), secondsBetween: 3f, max: 4);
            CreateCustomerSpawner("CarSpawner", "DriveThru", content.CarPrefab,
                new Vector3(22f, 0f, -14f), new Vector3(30f, 0f, -1.5f), secondsBetween: 6f, max: 3);
            UpgradeManager upgrades = CreateUpgradeManager(wallet, content.Upgrades);
            CreateUpgradePanel(hud, upgrades, content.Upgrades);
            OfflineEarnings offlineEarnings = CreateOfflineEarnings(wallet);
            DailyGoals dailyGoals = CreateDailyGoals(wallet, content.Menu, offlineEarnings);
            SaveManager saveManager = CreateSaveManager(wallet, pads, upgrades, offlineEarnings, dailyGoals);
            CreateFeedback(player.transform);
            CreateSettingsMenu(hud, saveManager);
            CreateGoalsPanel(hud, dailyGoals);
            CreateWelcomeBackPopup(hud, offlineEarnings);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Selection.activeGameObject = player;

            Debug.Log("Bubble Tea: prototype scene built at " + ScenePath + ". Press Play to try it.");
        }

        private static void CreateLight()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.66f);
        }

        private static void CreateFloor(Material material)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30 meters
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }

        // Low walls around the 30 x 30 m shop so nobody walks off the edge. Returns the east
        // wall, which the Expand Shop pad removes.
        private static GameObject CreateWalls()
        {
            Material wallMat = GetOrCreateMaterial("Wall", new Color(0.85f, 0.78f, 0.70f));
            var root = new GameObject("Walls");
            CreateWall(root.transform, "North", new Vector3(0f, 0.5f, 15.25f), new Vector3(30.5f, 1f, 0.5f), wallMat);
            CreateWall(root.transform, "South", new Vector3(0f, 0.5f, -15.25f), new Vector3(30.5f, 1f, 0.5f), wallMat);
            CreateWall(root.transform, "West", new Vector3(-15.25f, 0.5f, 0f), new Vector3(0.5f, 1f, 30.5f), wallMat);
            return CreateWall(root.transform, "East (opens with Expand Shop)", new Vector3(15.25f, 0.5f, 0f),
                new Vector3(0.5f, 1f, 30.5f), wallMat);
        }

        // The extra 16 x 30 m to the east, hidden until Expand Shop is bought.
        private static GameObject CreateExpansionArea()
        {
            Material wallMat = GetOrCreateMaterial("Wall", new Color(0.85f, 0.78f, 0.70f));
            Material expansionFloor = GetOrCreateMaterial("ExpansionFloor", new Color(0.86f, 0.90f, 0.82f));
            var root = new GameObject("Expansion Area");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(23f, 0f, 0f);
            floor.transform.localScale = new Vector3(1.6f, 1f, 3f); // 16 x 30 meters
            floor.GetComponent<Renderer>().sharedMaterial = expansionFloor;

            CreateWall(root.transform, "North", new Vector3(23f, 0.5f, 15.25f), new Vector3(16.5f, 1f, 0.5f), wallMat);
            CreateWall(root.transform, "South", new Vector3(23f, 0.5f, -15.25f), new Vector3(16.5f, 1f, 0.5f), wallMat);
            CreateWall(root.transform, "East", new Vector3(31.25f, 0.5f, 0f), new Vector3(0.5f, 1f, 30.5f), wallMat);

            root.SetActive(false);
            return root;
        }

        // Plants (or whatever the Art Set lists as decorations) in the corners of the shop.
        private static readonly Vector3[] DecorationSpots =
        {
            new(-14f, 0f, 14f), new(14f, 0f, 14f), new(-14f, 0f, 9f), new(14f, 0f, -14f), new(-14f, 0f, -3f), new(6f, 0f, 14f),
        };

        private static void CreateDecorations()
        {
            ArtSet art = ArtLibrary.Load();
            if (art.Decorations.Count == 0) return;

            var root = new GameObject("Decorations").transform;
            for (int i = 0; i < DecorationSpots.Length; i++)
            {
                GameObject model = art.Decorations[i % art.Decorations.Count];
                if (model == null) continue;
                var spot = new GameObject(model.name).transform;
                spot.SetParent(root, false);
                spot.localPosition = DecorationSpots[i];
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
            player.transform.position = new Vector3(0f, 1f, 0f);
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
            camera.backgroundColor = new Color(0.98f, 0.85f, 0.80f);
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
            var piles = new CashPile[CashPilePositions.Length];
            for (int i = 0; i < piles.Length; i++)
            {
                var go = new GameObject($"CashPile {i + 1}");
                go.transform.position = CashPilePositions[i];

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
            }
            return piles;
        }

        private static void CreateRefiller(CashPile[] piles)
        {
            var go = new GameObject("CashPileRefiller (test helper)");
            var refiller = go.AddComponent<CashPileRefiller>();

            var serialized = new SerializedObject(refiller);
            SerializedProperty list = serialized.FindProperty("piles");
            list.arraySize = piles.Length;
            for (int i = 0; i < piles.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = piles[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Pads in a chain: the counter reveals the tea brewer and the upgrade desk, then
        // tea brewer -> topping bar -> taro station -> fruit bar. The topping bar also
        // reveals the first runner, who reveals the second. The fruit bar reveals Expand Shop,
        // which opens the east wall and reveals the drive-through.
        private static UnlockPad[] CreateUnlockPads(ShopContentBuilder.Content content, CashBill billPrefab, Wallet wallet,
            GameObject eastWall, GameObject expansionArea)
        {
            Material padMat = GetOrCreateMaterial("UnlockPad", new Color(0.97f, 0.97f, 0.97f));

            UnlockPad driveThruPad = CreatePad(content.DriveThru, new Vector3(22f, 0f, 0f), billPrefab, wallet, padMat);
            UnlockPad expansionPad = CreatePad(content.Expansion, new Vector3(12f, 0f, 10f), billPrefab, wallet, padMat,
                driveThruPad);
            SetObjectList(expansionPad, "showWhenUnlocked", expansionArea);
            SetObjectList(expansionPad, "hideWhenUnlocked", eastWall);
            UnlockPad fruitPad = CreatePad(content.FruitBar, new Vector3(10f, 0f, -5f), billPrefab, wallet, padMat,
                expansionPad);
            UnlockPad taroPad = CreatePad(content.TaroStation, new Vector3(5f, 0f, -8f), billPrefab, wallet, padMat, fruitPad);
            UnlockPad runner2Pad = CreatePad(content.Runner2, new Vector3(-10f, 0f, 5f), billPrefab, wallet, padMat);
            UnlockPad runner1Pad = CreatePad(content.Runner1, new Vector3(-10f, 0f, 0f), billPrefab, wallet, padMat, runner2Pad);
            UnlockPad toppingPad = CreatePad(content.ToppingBar, new Vector3(5f, 0f, -3f), billPrefab, wallet, padMat,
                taroPad, runner1Pad);
            UnlockPad brewerPad = CreatePad(content.TeaBrewer, new Vector3(0f, 0f, -6f), billPrefab, wallet, padMat, toppingPad);
            UnlockPad upgradePad = CreatePad(content.UpgradeDesk, new Vector3(9f, 0f, 1f), billPrefab, wallet, padMat);
            UnlockPad counterPad = CreatePad(content.Counter, new Vector3(-5f, 0f, -3f), billPrefab, wallet, padMat,
                brewerPad, upgradePad);

            // Only the first pad is visible at the start.
            toppingPad.gameObject.SetActive(false);
            brewerPad.gameObject.SetActive(false);
            upgradePad.gameObject.SetActive(false);
            taroPad.gameObject.SetActive(false);
            fruitPad.gameObject.SetActive(false);
            runner1Pad.gameObject.SetActive(false);
            runner2Pad.gameObject.SetActive(false);
            expansionPad.gameObject.SetActive(false);
            driveThruPad.gameObject.SetActive(false);
            return new[]
            {
                counterPad, brewerPad, toppingPad, upgradePad, taroPad, fruitPad, runner1Pad, runner2Pad,
                expansionPad, driveThruPad,
            };
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
            OfflineEarnings offlineEarnings, DailyGoals dailyGoals)
        {
            var go = new GameObject("SaveManager");
            var saveManager = go.AddComponent<SaveManager>();
            SetReference(saveManager, "wallet", wallet);
            SetReference(saveManager, "upgradeManager", upgrades);
            SetReference(saveManager, "offlineEarnings", offlineEarnings);
            SetReference(saveManager, "dailyGoals", dailyGoals);

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

        // A GOALS button under MENU that opens today's goals, plus a banner when one is finished.
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
            title.text = "DAILY GOALS";
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
            banner.rectTransform.anchoredPosition = new Vector2(0f, -330f);
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

        // Sounds, vibration, confetti, sale pop-ups and the guide arrow.
        private static void CreateFeedback(Transform player)
        {
            var go = new GameObject("Feedback");
            go.AddComponent<AudioSource>();
            go.AddComponent<SoundEffects>();
            go.AddComponent<Haptics>();
            go.AddComponent<UnlockConfetti>();
            go.AddComponent<SalePopups>();

            Material arrowMat = GetOrCreateMaterial("GuideArrow", new Color(1f, 0.82f, 0.2f));

            // The marker: a fat yellow "down arrow" made of a post and a diamond tip.
            var marker = new GameObject("GoalMarker").transform;
            marker.SetParent(go.transform, false);
            AddShape(marker, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.6f, 0.3f), Quaternion.identity, arrowMat);
            AddShape(marker, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.5f, 0.5f),
                Quaternion.Euler(45f, 0f, 45f), arrowMat);

            // The pointer: a flat arrow on the floor, pointing along its forward (z) direction.
            var pointer = new GameObject("GoalPointer").transform;
            pointer.SetParent(go.transform, false);
            AddShape(pointer, PrimitiveType.Cube, new Vector3(0f, 0f, 1.1f), new Vector3(0.18f, 0.02f, 0.6f), Quaternion.identity, arrowMat);
            AddShape(pointer, PrimitiveType.Cube, new Vector3(0f, 0f, 1.45f), new Vector3(0.35f, 0.02f, 0.35f),
                Quaternion.Euler(0f, 45f, 0f), arrowMat);

            var arrow = go.AddComponent<GoalArrow>();
            SetReference(arrow, "player", player);
            SetReference(arrow, "marker", marker);
            SetReference(arrow, "pointer", pointer);
        }

        private static void AddShape(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Quaternion rotation,
            Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localRotation = rotation;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
        }

        // A MENU button in the top-right corner that opens sound, vibration and reset options.
        private static void CreateSettingsMenu(Transform hud, SaveManager saveManager)
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
            panel.sizeDelta = new Vector2(800f, 760f);
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
            Button vibration = CreateMenuButton("Vibration", panel, "Vibration: ON", rounded, font, green, out Text vibrationLabel);
            SetTopRow((RectTransform)vibration.transform, 310f, 120f, 60f, 60f);
            Button reset = CreateMenuButton("Reset", panel, "Reset Progress", rounded, font, new Color(0.85f, 0.35f, 0.35f),
                out Text resetLabel);
            SetTopRow((RectTransform)reset.transform, 460f, 120f, 60f, 60f);
            Button close = CreateMenuButton("Close", panel, "Close", rounded, font, new Color(0.45f, 0.45f, 0.55f), out _);
            SetTopRow((RectTransform)close.transform, 610f, 110f, 200f, 200f);

            SetReference(menu, "panel", panel.gameObject);
            SetReference(menu, "openButton", openButton);
            SetReference(menu, "closeButton", close);
            SetReference(menu, "soundButton", sound);
            SetReference(menu, "soundLabel", soundLabel);
            SetReference(menu, "vibrationButton", vibration);
            SetReference(menu, "vibrationLabel", vibrationLabel);
            SetReference(menu, "resetButton", reset);
            SetReference(menu, "resetLabel", resetLabel);
            SetReference(menu, "saveManager", saveManager);
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

        private static void AddSceneToBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
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
