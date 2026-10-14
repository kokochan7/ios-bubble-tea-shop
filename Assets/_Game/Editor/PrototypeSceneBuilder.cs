using BubbleTea.Building;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Economy;
using BubbleTea.Player;
using BubbleTea.Saving;
using BubbleTea.UI;
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

            GameObject player = CreatePlayer(playerMat, out Wallet wallet);
            Camera camera = CreateCamera(player.transform);
            VirtualJoystick joystick = CreateHud(wallet);
            CreateEventSystem();

            SetReference(player.GetComponent<PlayerMovement>(), "joystick", joystick);
            SetReference(player.GetComponent<PlayerMovement>(), "cameraTransform", camera.transform);

            CashPile[] piles = CreateCashPiles(billPrefab, padMat);
            CreateRefiller(piles);
            ShopContentBuilder.Content content = ShopContentBuilder.Build(billPrefab);
            UnlockPad[] pads = CreateUnlockPads(content, billPrefab, wallet);
            CreateCustomerSpawner(content.CustomerPrefab);
            CreateSaveManager(wallet, pads);

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

        private static VirtualJoystick CreateHud(Wallet wallet)
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
            RectTransform panel = CreateUiObject("MoneyCounter", canvasGo.transform);
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

        // Three pads in a chain: buying the counter reveals the tea brewer, which reveals the topping bar.
        private static UnlockPad[] CreateUnlockPads(ShopContentBuilder.Content content, CashBill billPrefab, Wallet wallet)
        {
            Material padMat = GetOrCreateMaterial("UnlockPad", new Color(0.97f, 0.97f, 0.97f));

            UnlockPad toppingPad = CreatePad(content.ToppingBar, new Vector3(5f, 0f, -3f), billPrefab, wallet, padMat, null);
            UnlockPad brewerPad = CreatePad(content.TeaBrewer, new Vector3(0f, 0f, -6f), billPrefab, wallet, padMat, toppingPad);
            UnlockPad counterPad = CreatePad(content.Counter, new Vector3(-5f, 0f, -3f), billPrefab, wallet, padMat, brewerPad);

            // Only the first pad is visible at the start.
            toppingPad.gameObject.SetActive(false);
            brewerPad.gameObject.SetActive(false);
            return new[] { counterPad, brewerPad, toppingPad };
        }

        // Loads progress on Play and keeps saving it. Delete it with Bubble Tea > Delete Save Data.
        private static void CreateSaveManager(Wallet wallet, UnlockPad[] pads)
        {
            var go = new GameObject("SaveManager");
            var saveManager = go.AddComponent<SaveManager>();
            SetReference(saveManager, "wallet", wallet);

            var serialized = new SerializedObject(saveManager);
            SerializedProperty list = serialized.FindProperty("pads");
            list.arraySize = pads.Length;
            for (int i = 0; i < pads.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pads[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Customers walk in from the bottom-left, queue behind the counter, and leave to the left.
        private static void CreateCustomerSpawner(Customer customerPrefab)
        {
            var exit = new GameObject("CustomerExit");
            exit.transform.position = new Vector3(-14f, 0f, -8f);

            var go = new GameObject("CustomerSpawner");
            go.transform.position = new Vector3(-5f, 0f, -14f);
            var spawner = go.AddComponent<CustomerSpawner>();
            SetReference(spawner, "customerPrefab", customerPrefab);
            SetReference(spawner, "exitPoint", exit.transform);
        }

        private static UnlockPad CreatePad(BuildableDefinition definition, Vector3 position, CashBill billPrefab,
            Wallet wallet, Material material, UnlockPad next)
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
            if (next != null)
            {
                var serialized = new SerializedObject(pad);
                SerializedProperty list = serialized.FindProperty("unlocksNext");
                list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = next;
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
