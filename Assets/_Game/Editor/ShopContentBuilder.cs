using BubbleTea.Building;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Economy;
using BubbleTea.Staff;
using BubbleTea.Upgrades;
using UnityEditor;
using UnityEngine;
using static BubbleTea.EditorTools.PrototypeSceneBuilder;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Creates the shop's data assets (items, recipe steps, drinks, buildables) and the
    /// gray-box prefabs that use them. Data assets are only created when missing, so
    /// prices and timings you tweak in the Inspector survive a rebuild. Prefabs are
    /// rebuilt every time so they always match the current scripts.
    /// </summary>
    internal static class ShopContentBuilder
    {
        private const string DataFolder = "Assets/_Game/Data";
        private const string PrefabFolder = "Assets/_Game/Prefabs";

        internal struct Content
        {
            public BuildableDefinition Counter;
            public BuildableDefinition TeaBrewer;
            public BuildableDefinition ToppingBar;
            public BuildableDefinition TaroStation;
            public BuildableDefinition FruitBar;
            public BuildableDefinition Runner1;
            public BuildableDefinition Runner2;
            public BuildableDefinition Expansion;
            public BuildableDefinition DriveThru;
            public Customer CarPrefab;
            public BuildableDefinition UpgradeDesk;
            public UpgradeDefinition[] Upgrades;
            public Customer CustomerPrefab;
            public DrinkRecipe[] Menu;
        }

        internal static Content Build(CashBill billPrefab)
        {
            EnsureFolder(DataFolder, "Items");
            EnsureFolder(DataFolder, "Recipes");
            EnsureFolder(DataFolder, "Menu");
            EnsureFolder(DataFolder, "Upgrades");
            EnsureFolder(PrefabFolder, "Items");
            EnsureFolder(PrefabFolder, "Stations");

            ItemDefinition milkTea = GetOrCreateItem("Milk Tea", new Color(0.85f, 0.70f, 0.52f), null);
            ItemDefinition bobaTea = GetOrCreateItem("Boba Milk Tea", new Color(0.88f, 0.76f, 0.60f), new Color(0.12f, 0.08f, 0.06f));
            ItemDefinition taroTea = GetOrCreateItem("Taro Milk Tea", new Color(0.66f, 0.50f, 0.84f), null);
            ItemDefinition taroBoba = GetOrCreateItem("Taro Boba", new Color(0.70f, 0.55f, 0.88f), new Color(0.12f, 0.08f, 0.06f));
            ItemDefinition mangoTea = GetOrCreateItem("Mango Fruit Tea", new Color(1f, 0.66f, 0.18f), null);

            RecipeStep brew = GetOrCreateStep("Brew Milk Tea", null, milkTea, 2f);
            RecipeStep addBoba = GetOrCreateStep("Add Boba", milkTea, bobaTea, 1.5f);
            RecipeStep makeTaro = GetOrCreateStep("Make Taro Tea", null, taroTea, 2.5f);
            RecipeStep addBobaToTaro = GetOrCreateStep("Add Boba To Taro", taroTea, taroBoba, 1.5f);
            RecipeStep makeMango = GetOrCreateStep("Make Mango Tea", null, mangoTea, 2f);

            DrinkRecipe sellMilkTea = GetOrCreateDrink("Milk Tea", milkTea, 8);
            DrinkRecipe sellBobaTea = GetOrCreateDrink("Boba Milk Tea", bobaTea, 15);
            DrinkRecipe sellTaroTea = GetOrCreateDrink("Taro Milk Tea", taroTea, 12);
            DrinkRecipe sellTaroBoba = GetOrCreateDrink("Taro Boba", taroBoba, 20);
            DrinkRecipe sellMangoTea = GetOrCreateDrink("Mango Fruit Tea", mangoTea, 14);

            DrinkRecipe[] menu = { sellMilkTea, sellBobaTea, sellTaroTea, sellTaroBoba, sellMangoTea };
            GameObject counter = BuildCounterPrefab("Counter", new Color(0.62f, 0.42f, 0.28f), menu, billPrefab,
                "Main", priceMultiplier: 1f, queueSpacing: 1.1f);
            GameObject driveThru = BuildCounterPrefab("DriveThru", new Color(0.35f, 0.55f, 0.75f), menu, billPrefab,
                "DriveThru", priceMultiplier: 1.5f, queueSpacing: 3.2f);
            GameObject brewer = BuildStationPrefab("TeaBrewer", new Color(0.30f, 0.30f, 0.34f), new Vector3(1.4f, 1f, 1f),
                new Color(0.55f, 0.32f, 0.16f), new[] { brew }, inputCapacity: 0);
            GameObject toppingBar = BuildStationPrefab("ToppingBar", new Color(0.95f, 0.78f, 0.55f), new Vector3(2f, 1f, 1f),
                new Color(0.15f, 0.10f, 0.08f), new[] { addBoba, addBobaToTaro }, inputCapacity: 4);
            GameObject taroStation = BuildStationPrefab("TaroStation", new Color(0.45f, 0.32f, 0.62f), new Vector3(1.4f, 1f, 1f),
                new Color(0.80f, 0.68f, 0.95f), new[] { makeTaro }, inputCapacity: 0);
            GameObject fruitBar = BuildStationPrefab("FruitBar", new Color(0.95f, 0.55f, 0.20f), new Vector3(1.4f, 1f, 1f),
                new Color(1f, 0.85f, 0.25f), new[] { makeMango }, inputCapacity: 0);

            // Values per level start at level 0; costs start at the price of level 1.
            UpgradeDefinition[] upgrades =
            {
                GetOrCreateUpgrade("Walk Speed", StatId.MoveSpeed,
                    new[] { 1f, 1.15f, 1.3f, 1.45f, 1.6f }, new[] { 40, 90, 180, 350 }),
                GetOrCreateUpgrade("Carry More", StatId.CarryBonus,
                    new[] { 0f, 2f, 4f, 6f, 8f }, new[] { 50, 120, 250, 500 }),
                GetOrCreateUpgrade("Brew Speed", StatId.BrewSpeed,
                    new[] { 1f, 1.25f, 1.5f, 1.75f, 2f }, new[] { 60, 140, 280, 550 }),
                GetOrCreateUpgrade("Drink Price", StatId.DrinkPrice,
                    new[] { 1f, 1.2f, 1.4f, 1.6f, 1.8f }, new[] { 80, 180, 360, 700 }),
            };
            GameObject upgradeDesk = BuildUpgradeDeskPrefab();
            GameObject runner = BuildRunnerPrefab();

            return new Content
            {
                Counter = GetOrCreateBuildable("Counter", 30, counter),
                TeaBrewer = GetOrCreateBuildable("Tea Brewer", 80, brewer),
                ToppingBar = GetOrCreateBuildable("Topping Bar", 150, toppingBar),
                TaroStation = GetOrCreateBuildable("Taro Station", 250, taroStation),
                FruitBar = GetOrCreateBuildable("Fruit Bar", 400, fruitBar),
                Runner1 = GetOrCreateBuildable("Hire Runner", 300, runner),
                Runner2 = GetOrCreateBuildable("Hire Runner 2", 600, runner),
                // The expansion builds nothing itself; its pad opens the wall and shows the new floor.
                Expansion = GetOrCreateBuildable("Expand Shop", 1000, null),
                DriveThru = GetOrCreateBuildable("Drive Thru", 600, driveThru),
                CarPrefab = BuildCarPrefab(),
                UpgradeDesk = GetOrCreateBuildable("Upgrades", 40, upgradeDesk),
                Upgrades = upgrades,
                CustomerPrefab = BuildCustomerPrefab(),
                Menu = menu,
            };
        }

        // ---------- Data assets ----------

        private static ItemDefinition GetOrCreateItem(string displayName, Color cupColor, Color? pearlColor)
        {
            var item = GetOrCreateAsset<ItemDefinition>($"{DataFolder}/Items/{FileName(displayName)}.asset",
                serialized => serialized.FindProperty("displayName").stringValue = displayName);

            // The model is rebuilt every time, then linked both ways.
            GameObject prefab = BuildItemPrefab(FileName(displayName), item, cupColor, pearlColor);
            var serializedItem = new SerializedObject(item);
            serializedItem.FindProperty("prefab").objectReferenceValue = prefab;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static RecipeStep GetOrCreateStep(string name, ItemDefinition input, ItemDefinition output, float seconds)
        {
            return GetOrCreateAsset<RecipeStep>($"{DataFolder}/Recipes/{FileName(name)}.asset", serialized =>
            {
                serialized.FindProperty("input").objectReferenceValue = input;
                serialized.FindProperty("output").objectReferenceValue = output;
                serialized.FindProperty("seconds").floatValue = seconds;
            });
        }

        private static DrinkRecipe GetOrCreateDrink(string name, ItemDefinition drink, int price)
        {
            return GetOrCreateAsset<DrinkRecipe>($"{DataFolder}/Menu/{FileName(name)}.asset", serialized =>
            {
                serialized.FindProperty("drink").objectReferenceValue = drink;
                serialized.FindProperty("price").intValue = price;
            });
        }

        private static UpgradeDefinition GetOrCreateUpgrade(string displayName, StatId stat, float[] values, int[] costs)
        {
            return GetOrCreateAsset<UpgradeDefinition>($"{DataFolder}/Upgrades/{FileName(displayName)}.asset", serialized =>
            {
                serialized.FindProperty("displayName").stringValue = displayName;
                serialized.FindProperty("stat").enumValueIndex = (int)stat;

                SerializedProperty valueList = serialized.FindProperty("values");
                valueList.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++) valueList.GetArrayElementAtIndex(i).floatValue = values[i];

                SerializedProperty costList = serialized.FindProperty("costs");
                costList.arraySize = costs.Length;
                for (int i = 0; i < costs.Length; i++) costList.GetArrayElementAtIndex(i).intValue = costs[i];
            });
        }

        private static BuildableDefinition GetOrCreateBuildable(string displayName, int cost, GameObject prefab)
        {
            var buildable = GetOrCreateAsset<BuildableDefinition>($"{DataFolder}/{FileName(displayName)}.asset", serialized =>
            {
                serialized.FindProperty("displayName").stringValue = displayName;
                serialized.FindProperty("cost").intValue = cost;
            });

            // Always point at the freshly built prefab, but leave name and price alone.
            var serializedBuildable = new SerializedObject(buildable);
            serializedBuildable.FindProperty("prefab").objectReferenceValue = prefab;
            serializedBuildable.ApplyModifiedPropertiesWithoutUndo();
            return buildable;
        }

        private static T GetOrCreateAsset<T>(string path, System.Action<SerializedObject> fillNew) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var serialized = new SerializedObject(asset);
            fillNew(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return asset;
        }

        // ---------- Prefabs ----------

        // A cup: a short cylinder, plus a dark layer of pearls at the bottom for boba drinks.
        private static GameObject BuildItemPrefab(string name, ItemDefinition definition, Color cupColor, Color? pearlColor)
        {
            var root = new GameObject(name);

            AddPart(root.transform, PrimitiveType.Cylinder, "Cup", new Vector3(0f, 0.14f, 0f),
                new Vector3(0.24f, 0.14f, 0.24f), GetOrCreateMaterial(name, cupColor));
            AddPart(root.transform, PrimitiveType.Cube, "Straw", new Vector3(0.04f, 0.34f, 0f),
                new Vector3(0.03f, 0.18f, 0.03f), GetOrCreateMaterial("Straw", new Color(0.95f, 0.45f, 0.55f)));
            GameObject pearls = null;
            if (pearlColor.HasValue)
            {
                pearls = AddPart(root.transform, PrimitiveType.Cylinder, "Pearls", new Vector3(0f, 0.05f, 0f),
                    new Vector3(0.25f, 0.05f, 0.25f), GetOrCreateMaterial("Pearls", pearlColor.Value));
            }

            // A cup model can show the drink itself: parts named "Drink" take the drink's color and parts
            // named "Pearls" take the pearl color (or hide for drinks without pearls), like the Boba Art Kit's cup.
            // Other cup models show the drink as a colored lid, with pearls on top for boba.
            GameObject cupArt = ArtLibrary.Attach("Cup", root.transform, new Vector3(0.26f, 0.32f, 0.26f), ArtLibrary.Fit.Height);
            if (cupArt != null)
            {
                ArtLibrary.HideGrayBox(root.transform.Find("Cup").gameObject, root.transform.Find("Straw").gameObject, pearls);
                Transform drink = ArtLibrary.FindChild(cupArt.transform, "Drink");
                if (drink != null)
                {
                    ArtLibrary.Recolor(drink, GetOrCreateMaterial(name, cupColor));
                    Transform artPearls = ArtLibrary.FindChild(cupArt.transform, "Pearls");
                    if (artPearls != null)
                    {
                        if (pearlColor.HasValue) ArtLibrary.Recolor(artPearls, GetOrCreateMaterial("Pearls", pearlColor.Value));
                        else artPearls.gameObject.SetActive(false);
                    }
                }
                else
                {
                    AddPart(root.transform, PrimitiveType.Cylinder, "Lid", new Vector3(0f, 0.33f, 0f),
                        new Vector3(0.22f, 0.01f, 0.22f), GetOrCreateMaterial(name, cupColor));
                    if (pearlColor.HasValue)
                    {
                        AddPart(root.transform, PrimitiveType.Sphere, "PearlsOnTop", new Vector3(0f, 0.35f, 0f),
                            new Vector3(0.1f, 0.04f, 0.1f), GetOrCreateMaterial("Pearls", pearlColor.Value));
                    }
                }
            }

            var item = root.AddComponent<CarriedItem>();
            SetReference(item, "definition", definition);
            return SavePrefab(root, $"{PrefabFolder}/Items/{name}.prefab");
        }

        private static GameObject BuildStationPrefab(string name, Color bodyColor, Vector3 bodySize, Color topColor,
            RecipeStep[] steps, int inputCapacity)
        {
            GameObject root = BuildStationBody(name, bodyColor, bodySize, topColor);
            var station = root.AddComponent<Station>();
            var serialized = new SerializedObject(station);

            SerializedProperty stepList = serialized.FindProperty("steps");
            stepList.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++) stepList.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];

            float edgeX = bodySize.x * 0.5f - 0.3f;
            serialized.FindProperty("output.anchor").objectReferenceValue =
                AddAnchor(root.transform, "OutputStack", new Vector3(edgeX, bodySize.y, 0.2f));
            serialized.FindProperty("output.capacity").intValue = 4;

            serialized.FindProperty("input.capacity").intValue = inputCapacity;
            if (inputCapacity > 0)
            {
                serialized.FindProperty("input.anchor").objectReferenceValue =
                    AddAnchor(root.transform, "InputStack", new Vector3(-edgeX, bodySize.y, 0.2f));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, $"{PrefabFolder}/Stations/{name}.prefab");
        }

        // The counter: player drops drinks on the front, customers queue at the back,
        // and their money piles up beside it.
        // Used for both the main counter and the drive-through window.
        private static GameObject BuildCounterPrefab(string name, Color color, DrinkRecipe[] menu, CashBill billPrefab,
            string lane, float priceMultiplier, float queueSpacing)
        {
            var bodySize = new Vector3(2.2f, 1f, 1f);
            GameObject root = BuildStationBody(name, color, bodySize, null);

            var cashGo = new GameObject("CashPile");
            cashGo.transform.SetParent(root.transform, false);
            cashGo.transform.localPosition = new Vector3(1.8f, 0f, 1.4f);
            var cashPile = cashGo.AddComponent<CashPile>();
            var serializedPile = new SerializedObject(cashPile);
            serializedPile.FindProperty("billPrefab").objectReferenceValue = billPrefab;
            serializedPile.FindProperty("valuePerBill").intValue = 1;
            serializedPile.FindProperty("startingBills").intValue = 0;
            serializedPile.FindProperty("maxBills").intValue = 90;
            serializedPile.ApplyModifiedPropertiesWithoutUndo();

            Transform queueStart = AddAnchor(root.transform, "QueueStart", new Vector3(0f, 0f, -1.5f));
            queueStart.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var counter = root.AddComponent<ServingCounter>();
            var serialized = new SerializedObject(counter);
            SerializedProperty menuList = serialized.FindProperty("menu");
            menuList.arraySize = menu.Length;
            for (int i = 0; i < menu.Length; i++) menuList.GetArrayElementAtIndex(i).objectReferenceValue = menu[i];
            serialized.FindProperty("drinks.anchor").objectReferenceValue =
                AddAnchor(root.transform, "DrinkStack", new Vector3(-0.6f, bodySize.y, 0f));
            serialized.FindProperty("drinks.capacity").intValue = 6;
            serialized.FindProperty("cashPile").objectReferenceValue = cashPile;
            serialized.FindProperty("queueStart").objectReferenceValue = queueStart;
            serialized.FindProperty("lane").stringValue = lane;
            serialized.FindProperty("priceMultiplier").floatValue = priceMultiplier;
            serialized.FindProperty("queueSpacing").floatValue = queueSpacing;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Stations/{name}.prefab");
        }

        // A purple desk. Standing in front of it opens the upgrade panel.
        private static GameObject BuildUpgradeDeskPrefab()
        {
            GameObject root = BuildStationBody("UpgradeDesk", new Color(0.58f, 0.44f, 0.86f), new Vector3(1.6f, 1f, 1f),
                new Color(1f, 0.85f, 0.3f));
            root.AddComponent<UpgradeZone>();
            return SavePrefab(root, $"{PrefabFolder}/Stations/UpgradeDesk.prefab");
        }

        // A green helper with an apron-colored marker, carrying one item at a time.
        private static GameObject BuildRunnerPrefab()
        {
            var root = new GameObject("Runner");
            Material green = GetOrCreateMaterial("Runner", new Color(0.30f, 0.72f, 0.55f));
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), green);
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("RunnerApron", new Color(0.98f, 0.95f, 0.85f)));
            AddCharacterArt("Runner", root, body);

            Transform hands = AddAnchor(root.transform, "HandStack", new Vector3(0f, 0.9f, 0.55f));
            var carrier = root.AddComponent<ItemCarrier>();
            var serialized = new SerializedObject(carrier);
            serialized.FindProperty("stack.anchor").objectReferenceValue = hands;
            serialized.FindProperty("stack.capacity").intValue = 1;
            serialized.FindProperty("usesCarryUpgrade").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<Runner>();
            return SavePrefab(root, $"{PrefabFolder}/Runner.prefab");
        }

        private static Customer BuildCustomerPrefab()
        {
            var root = new GameObject("Customer");
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Customer", new Color(0.98f, 0.55f, 0.35f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("Customer", new Color(0.98f, 0.55f, 0.35f)));
            AddCharacterArt("Customer", root, body);

            // A white disc above the head; the ordered drink's model sits on it.
            Transform bubble = AddAnchor(root.transform, "OrderBubble", new Vector3(0f, 2.17f, 0f));
            AddPart(bubble, PrimitiveType.Cylinder, "Disc", new Vector3(0f, -0.02f, 0f),
                new Vector3(0.75f, 0.02f, 0.75f), GetOrCreateMaterial("OrderBubble", Color.white));

            var customer = root.AddComponent<Customer>();
            var serialized = new SerializedObject(customer);
            serialized.FindProperty("drinkHoldPosition").vector3Value = new Vector3(0f, 0.9f, 0.55f);
            serialized.FindProperty("orderBubble").objectReferenceValue = bubble;
            serialized.FindProperty("moodLight").objectReferenceValue = AddMoodLight(bubble);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Customer.prefab").GetComponent<Customer>();
        }

        // A round light beside the order bubble that shows the customer's mood (its color is set at runtime).
        private static Renderer AddMoodLight(Transform bubble)
        {
            return AddPart(bubble, PrimitiveType.Sphere, "MoodLight", new Vector3(0.55f, 0.1f, 0f),
                Vector3.one * 0.24f, GetOrCreateMaterial("MoodLight", Color.white)).GetComponent<Renderer>();
        }

        // A drive-through customer: a boxy car that drives up, orders, and drives off.
        private static Customer BuildCarPrefab()
        {
            var root = new GameObject("Car");
            Material paint = GetOrCreateMaterial("Car", new Color(0.90f, 0.30f, 0.35f));
            Material glass = GetOrCreateMaterial("CarWindow", new Color(0.55f, 0.75f, 0.90f));
            GameObject carBody = AddPart(root.transform, PrimitiveType.Cube, "Body", new Vector3(0f, 0.5f, 0f),
                new Vector3(1.6f, 0.7f, 2.8f), paint);
            GameObject cabin = AddPart(root.transform, PrimitiveType.Cube, "Cabin", new Vector3(0f, 1.05f, -0.2f),
                new Vector3(1.4f, 0.5f, 1.4f), glass);
            if (ArtLibrary.Attach("Car", root.transform, new Vector3(1.6f, 1.3f, 2.8f), ArtLibrary.Fit.Footprint) != null)
            {
                ArtLibrary.HideGrayBox(carBody, cabin);
            }

            Transform bubble = AddAnchor(root.transform, "OrderBubble", new Vector3(0f, 1.9f, 0f));
            AddPart(bubble, PrimitiveType.Cylinder, "Disc", new Vector3(0f, -0.02f, 0f),
                new Vector3(0.75f, 0.02f, 0.75f), GetOrCreateMaterial("OrderBubble", Color.white));

            var customer = root.AddComponent<Customer>();
            var serialized = new SerializedObject(customer);
            serialized.FindProperty("speed").floatValue = 5f;
            serialized.FindProperty("drinkHoldPosition").vector3Value = new Vector3(0.9f, 1f, 0.4f);
            serialized.FindProperty("orderBubble").objectReferenceValue = bubble;
            serialized.FindProperty("moodLight").objectReferenceValue = AddMoodLight(bubble);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Car.prefab").GetComponent<Customer>();
        }

        // Swaps a capsule person for a character model (if there is one) and makes whichever
        // is showing waddle as it walks.
        private static void AddCharacterArt(string slotName, GameObject root, GameObject capsule)
        {
            GameObject model = ArtLibrary.Attach(slotName, root.transform, new Vector3(0.8f, 1.6f, 0.8f), ArtLibrary.Fit.Height);
            if (model != null) ArtLibrary.HideGrayBox(capsule);

            var bob = root.AddComponent<WalkBob>();
            SetReference(bob, "visual", model != null ? model.transform : capsule.transform);
        }

        // ---------- Helpers ----------

        // A solid block (the player bumps into it) with an optional cylinder on top.
        private static GameObject BuildStationBody(string name, Color bodyColor, Vector3 bodySize, Color? topColor)
        {
            var root = new GameObject(name);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, bodySize.y * 0.5f, 0f);
            body.transform.localScale = bodySize;
            body.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(name, bodyColor);

            GameObject top = null;
            if (topColor.HasValue)
            {
                top = AddPart(root.transform, PrimitiveType.Cylinder, "Top", new Vector3(0f, bodySize.y + 0.3f, -0.15f),
                    new Vector3(0.5f, 0.3f, 0.5f), GetOrCreateMaterial(name + "Top", topColor.Value));
            }

            // A real model, if the Art Set has one, replaces the look but keeps the solid box.
            if (ArtLibrary.Attach(name, root.transform, bodySize, ArtLibrary.Fit.Footprint) != null)
            {
                ArtLibrary.HideGrayBox(body, top);
            }
            return root;
        }

        // A decorative primitive with its collider removed.
        private static GameObject AddPart(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
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

        private static Transform AddAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        private static string FileName(string displayName) => displayName.Replace(" ", "");
    }
}
