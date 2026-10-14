using BubbleTea.Building;
using BubbleTea.Core;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Economy;
using BubbleTea.Staff;
using BubbleTea.Travel;
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
        // Shop 1 uses the top-level folders; later shops get their own subfolder, so their
        // drinks, prices and prefabs never overwrite the first shop's.
        private static string DataFolder = "Assets/_Game/Data";
        private static string PrefabFolder = "Assets/_Game/Prefabs";
        private static int costScale = 1;

        internal struct Content
        {
            public BuildableDefinition Counter;
            public BuildableDefinition TeaBrewer;
            public BuildableDefinition ToppingBar;
            public BuildableDefinition TaroStation;
            public BuildableDefinition FruitBar;
            public BuildableDefinition Expansion;
            public BuildableDefinition DriveThru;
            public Customer CarPrefab;
            public BuildableDefinition UpgradeDesk;
            public UpgradeDefinition[] Upgrades;
            public Customer CustomerPrefab;
            public DrinkRecipe[] Menu;
            public BuildableDefinition Planter;
            public BuildableDefinition MenuBoard;
            public BuildableDefinition Statue;
            /// <summary>Hire Barista pads in the order they unlock.</summary>
            public BuildableDefinition[] Baristas;
            public BuildableDefinition Cleaner;
            public BuildableDefinition MoreSeating;
            public BuildableDefinition Seating;
            public BuildableDefinition Cashier;
            public BuildableDefinition DriveThruCashier;

            /// <summary>Shop 1 only: the pad that opens the second shop. Null in other shops.</summary>
            public BuildableDefinition SecondShop;

            /// <summary>Shop 2 only: the bus stop back to the first shop. Null in other shops.</summary>
            public GameObject TravelBack;
        }

        // What a shop sells and the three stations that start each drink. The station slots
        // match shop 1's (brewer, topping bar, taro, fruit) so the pad chain and art slots carry over.
        private struct MenuSet
        {
            public DrinkRecipe[] Menu;
            public GameObject Brewer;
            public GameObject ToppingBar;
            public GameObject TaroStation;
            public GameObject FruitBar;
            public string BrewerName;
            public string TaroName;
            public string FruitName;
        }

        internal static Content Build(CashBill billPrefab, int shop)
        {
            DataFolder = "Assets/_Game/Data";
            PrefabFolder = "Assets/_Game/Prefabs";
            costScale = 1;
            if (shop > 1)
            {
                string sub = "Shop" + shop;
                EnsureFolder(DataFolder, sub);
                EnsureFolder(PrefabFolder, sub);
                DataFolder += "/" + sub;
                PrefabFolder += "/" + sub;
                costScale = 3; // everything costs (and pays) more in the second shop
            }

            EnsureFolder(DataFolder, "Items");
            EnsureFolder(DataFolder, "Recipes");
            EnsureFolder(DataFolder, "Menu");
            EnsureFolder(DataFolder, "Upgrades");
            EnsureFolder(PrefabFolder, "Items");
            EnsureFolder(PrefabFolder, "Stations");
            EnsureFolder(PrefabFolder, "Decor");

            MenuSet set = shop > 1 ? BuildMatchaMenu() : BuildClassicMenu();
            DrinkRecipe[] menu = set.Menu;
            GameObject counter = BuildCounterPrefab("Counter", new Color(0.62f, 0.42f, 0.28f), menu, billPrefab,
                "Main", priceMultiplier: 1f, queueSpacing: 1.1f);
            GameObject driveThru = BuildCounterPrefab("DriveThru", new Color(0.35f, 0.55f, 0.75f), menu, billPrefab,
                "DriveThru", priceMultiplier: 1.5f, queueSpacing: 3.2f,
                // The window faces the car lane: cars queue along the lane, which runs past the
                // counter's customer side, instead of straight out from it.
                queueOffset: new Vector3(0f, 0f, -3.5f), queueYaw: -90f);

            // Values per level start at level 0; costs start at the price of level 1.
            UpgradeDefinition[] upgrades =
            {
                GetOrCreateUpgrade("Walk Speed", StatId.MoveSpeed,
                    new[] { 1f, 1.15f, 1.3f, 1.45f, 1.6f }, Scaled(40, 90, 180, 350)),
                GetOrCreateUpgrade("Carry More", StatId.CarryBonus,
                    new[] { 0f, 2f, 4f, 6f, 8f }, Scaled(50, 120, 250, 500)),
                GetOrCreateUpgrade("Brew Speed", StatId.BrewSpeed,
                    new[] { 1f, 1.25f, 1.5f, 1.75f, 2f }, Scaled(60, 140, 280, 550)),
                GetOrCreateUpgrade("Drink Price", StatId.DrinkPrice,
                    new[] { 1f, 1.2f, 1.4f, 1.6f, 1.8f }, Scaled(80, 180, 360, 700)),
            };
            GameObject upgradeDesk = BuildUpgradeDeskPrefab();
            GameObject barista = BuildBaristaPrefab();

            return new Content
            {
                Counter = GetOrCreateBuildable("Counter", 30, counter),
                TeaBrewer = GetOrCreateBuildable(set.BrewerName, 80, set.Brewer),
                ToppingBar = GetOrCreateBuildable("Topping Bar", 150, set.ToppingBar),
                TaroStation = GetOrCreateBuildable(set.TaroName, 250, set.TaroStation),
                FruitBar = GetOrCreateBuildable(set.FruitName, 400, set.FruitBar),
                // The expansion builds nothing itself; its pad opens the wall and shows the new floor.
                Expansion = GetOrCreateBuildable("Expand Shop", 1000, null),
                DriveThru = GetOrCreateBuildable("Drive Thru", 600, driveThru),
                CarPrefab = BuildCarPrefab(),
                UpgradeDesk = GetOrCreateBuildable("Upgrades", 40, upgradeDesk),
                Upgrades = upgrades,
                CustomerPrefab = BuildCustomerPrefab(),
                Menu = menu,
                Seating = GetOrCreateBuildable("Seating Area", 500, BuildSeatingPrefab(billPrefab)),
                MoreSeating = GetOrCreateBuildable("More Seating", 1200, BuildSeatingPrefab(billPrefab)),
                Baristas = new[]
                {
                    GetOrCreateBuildable("Hire Barista", 200, barista, keepPriceUpToDate: true),
                    GetOrCreateBuildable("Hire Barista 2", 600, barista),
                    GetOrCreateBuildable("Hire Barista 3", 900, barista),
                    GetOrCreateBuildable("Hire Barista 4", 1300, barista),
                    GetOrCreateBuildable("Hire Barista 5", 1800, barista),
                },
                Cleaner = GetOrCreateBuildable("Hire Cleaner", 400, BuildCleanerPrefab()),
                Cashier = GetOrCreateBuildable("Hire Cashier", 150, BuildCashierPrefab("Cashier", "Main")),
                DriveThruCashier = GetOrCreateBuildable("Hire Drive-Thru Cashier", 400,
                    BuildCashierPrefab("DriveThruCashier", "DriveThru")),
                Planter = GetOrCreateBuildable("Planter", 120,
                    BuildDecorPrefab("Planter", new Color(0.42f, 0.73f, 0.47f), new Vector3(1.2f, 1.3f, 1.2f))),
                MenuBoard = GetOrCreateBuildable("Menu Board", 200,
                    BuildDecorPrefab("MenuBoard", new Color(0.3f, 0.32f, 0.3f), new Vector3(1f, 1.4f, 0.6f))),
                Statue = GetOrCreateBuildable("Boba Statue", 350,
                    BuildDecorPrefab("Statue", new Color(0.85f, 0.7f, 0.55f), new Vector3(1.2f, 1.8f, 1.2f))),
                // Built once the first shop is finished; the stop it builds leads to the Matcha House.
                SecondShop = shop == 1
                    ? GetOrCreateBuildable("Open Matcha House", 2500,
                        BuildTravelPrefab("ToMatchaHouse", "Shop2", "Matcha House", new Color(0.55f, 0.78f, 0.45f)))
                    : null,
                TravelBack = shop > 1
                    ? BuildTravelPrefab("ToBobaShop", "Prototype", "Boba Shop", new Color(0.95f, 0.62f, 0.55f))
                    : null,
            };
        }

        // Shop 1: classic milk teas, taro and mango.
        private static MenuSet BuildClassicMenu()
        {
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

            return new MenuSet
            {
                Menu = new[]
                {
                    GetOrCreateDrink("Milk Tea", milkTea, 8),
                    GetOrCreateDrink("Boba Milk Tea", bobaTea, 15),
                    GetOrCreateDrink("Taro Milk Tea", taroTea, 12),
                    GetOrCreateDrink("Taro Boba", taroBoba, 20),
                    GetOrCreateDrink("Mango Fruit Tea", mangoTea, 14),
                },
                Brewer = BuildStationPrefab("TeaBrewer", new Color(0.30f, 0.30f, 0.34f), new Vector3(1.4f, 1f, 1f),
                    new Color(0.55f, 0.32f, 0.16f), new[] { brew }, inputCapacity: 0),
                ToppingBar = BuildStationPrefab("ToppingBar", new Color(0.95f, 0.78f, 0.55f), new Vector3(2f, 1f, 1f),
                    new Color(0.15f, 0.10f, 0.08f), new[] { addBoba, addBobaToTaro }, inputCapacity: 4),
                TaroStation = BuildStationPrefab("TaroStation", new Color(0.45f, 0.32f, 0.62f), new Vector3(1.4f, 1f, 1f),
                    new Color(0.80f, 0.68f, 0.95f), new[] { makeTaro }, inputCapacity: 0),
                FruitBar = BuildStationPrefab("FruitBar", new Color(0.95f, 0.55f, 0.20f), new Vector3(1.4f, 1f, 1f),
                    new Color(1f, 0.85f, 0.25f), new[] { makeMango }, inputCapacity: 0),
                BrewerName = "Tea Brewer",
                TaroName = "Taro Station",
                FruitName = "Fruit Bar",
            };
        }

        // Shop 2, the Matcha House: matcha, brown sugar and strawberry drinks at about double the price.
        // The stations borrow shop 1's art slots (TeaBrewer, TaroStation, FruitBar) until they get their own.
        private static MenuSet BuildMatchaMenu()
        {
            Color pearls = new(0.12f, 0.08f, 0.06f);
            ItemDefinition matcha = GetOrCreateItem("Matcha Latte", new Color(0.55f, 0.75f, 0.38f), null);
            ItemDefinition matchaBoba = GetOrCreateItem("Matcha Boba", new Color(0.62f, 0.80f, 0.45f), pearls);
            ItemDefinition brownSugar = GetOrCreateItem("Brown Sugar Milk", new Color(0.78f, 0.56f, 0.36f), null);
            ItemDefinition brownSugarBoba = GetOrCreateItem("Brown Sugar Boba", new Color(0.72f, 0.48f, 0.28f), pearls);
            ItemDefinition strawberry = GetOrCreateItem("Strawberry Milk", new Color(0.98f, 0.62f, 0.72f), null);

            RecipeStep whisk = GetOrCreateStep("Whisk Matcha", null, matcha, 2f);
            RecipeStep addBoba = GetOrCreateStep("Add Boba To Matcha", matcha, matchaBoba, 1.5f);
            RecipeStep makeBrownSugar = GetOrCreateStep("Make Brown Sugar Milk", null, brownSugar, 2.5f);
            RecipeStep addBobaToBrownSugar = GetOrCreateStep("Add Boba To Brown Sugar", brownSugar, brownSugarBoba, 1.5f);
            RecipeStep makeStrawberry = GetOrCreateStep("Make Strawberry Milk", null, strawberry, 2f);

            return new MenuSet
            {
                Menu = new[]
                {
                    GetOrCreateDrink("Matcha Latte", matcha, 16),
                    GetOrCreateDrink("Matcha Boba", matchaBoba, 28),
                    GetOrCreateDrink("Brown Sugar Milk", brownSugar, 22),
                    GetOrCreateDrink("Brown Sugar Boba", brownSugarBoba, 36),
                    GetOrCreateDrink("Strawberry Milk", strawberry, 26),
                },
                Brewer = BuildStationPrefab("MatchaBar", new Color(0.36f, 0.52f, 0.32f), new Vector3(1.4f, 1f, 1f),
                    new Color(0.55f, 0.75f, 0.38f), new[] { whisk }, inputCapacity: 0, artSlot: "TeaBrewer"),
                ToppingBar = BuildStationPrefab("ToppingBar", new Color(0.95f, 0.78f, 0.55f), new Vector3(2f, 1f, 1f),
                    new Color(0.15f, 0.10f, 0.08f), new[] { addBoba, addBobaToBrownSugar }, inputCapacity: 4),
                TaroStation = BuildStationPrefab("BrownSugarBar", new Color(0.50f, 0.33f, 0.20f), new Vector3(1.4f, 1f, 1f),
                    new Color(0.78f, 0.56f, 0.36f), new[] { makeBrownSugar }, inputCapacity: 0, artSlot: "TaroStation"),
                FruitBar = BuildStationPrefab("StrawberryBar", new Color(0.92f, 0.45f, 0.55f), new Vector3(1.4f, 1f, 1f),
                    new Color(0.98f, 0.62f, 0.72f), new[] { makeStrawberry }, inputCapacity: 0, artSlot: "FruitBar"),
                BrewerName = "Matcha Bar",
                TaroName = "Brown Sugar Bar",
                FruitName = "Strawberry Bar",
            };
        }

        private static int[] Scaled(params int[] costs)
        {
            var result = new int[costs.Length];
            for (int i = 0; i < costs.Length; i++) result[i] = costs[i] * costScale;
            return result;
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

        // keepPriceUpToDate: also reset the price of an existing asset (for a price change that
        // should reach scenes built before it).
        private static BuildableDefinition GetOrCreateBuildable(string displayName, int cost, GameObject prefab,
            bool keepPriceUpToDate = false)
        {
            var buildable = GetOrCreateAsset<BuildableDefinition>($"{DataFolder}/{FileName(displayName)}.asset", serialized =>
            {
                serialized.FindProperty("displayName").stringValue = displayName;
                serialized.FindProperty("cost").intValue = cost * costScale;
            });

            // Always point at the freshly built prefab, but leave name and price alone.
            var serializedBuildable = new SerializedObject(buildable);
            serializedBuildable.FindProperty("prefab").objectReferenceValue = prefab;
            if (keepPriceUpToDate) serializedBuildable.FindProperty("cost").intValue = cost * costScale;
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
            RecipeStep[] steps, int inputCapacity, string artSlot = null)
        {
            GameObject root = BuildStationBody(name, bodyColor, bodySize, topColor, artSlot);
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
            string lane, float priceMultiplier, float queueSpacing, Vector3? queueOffset = null, float queueYaw = 180f)
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

            Transform queueStart = AddAnchor(root.transform, "QueueStart", queueOffset ?? new Vector3(0f, 0f, -1.5f));
            queueStart.localRotation = Quaternion.Euler(0f, queueYaw, 0f);

            var counter = root.AddComponent<ServingCounter>();
            var serialized = new SerializedObject(counter);
            SerializedProperty menuList = serialized.FindProperty("menu");
            menuList.arraySize = menu.Length;
            for (int i = 0; i < menu.Length; i++) menuList.GetArrayElementAtIndex(i).objectReferenceValue = menu[i];
            serialized.FindProperty("drinks.anchor").objectReferenceValue =
                AddAnchor(root.transform, "DrinkStack", new Vector3(-0.6f, bodySize.y, 0f));
            serialized.FindProperty("drinks.capacity").intValue = 8;
            serialized.FindProperty("cashPile").objectReferenceValue = cashPile;
            serialized.FindProperty("queueStart").objectReferenceValue = queueStart;
            serialized.FindProperty("lane").stringValue = lane;
            serialized.FindProperty("priceMultiplier").floatValue = priceMultiplier;
            serialized.FindProperty("queueSpacing").floatValue = queueSpacing;
            // The drop-off spot is on the left half, so a cashier can stand behind the right half.
            serialized.FindProperty("zone.offset").vector3Value = new Vector3(-0.5f, 0f, 1.4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Stations/{name}.prefab");
        }

        // A bus stop to another shop: a colored tile to stand on (a ring fills while you wait)
        // and a sign behind it. The "BusStop" art slot replaces the sign.
        private static GameObject BuildTravelPrefab(string name, string targetScene, string destination, Color color)
        {
            var root = new GameObject(name);
            AddPart(root.transform, PrimitiveType.Cube, "Tile", new Vector3(0f, 0.01f, 0f),
                new Vector3(2.2f, 0.02f, 2.2f), GetOrCreateMaterial(name, color));
            Transform fill = AddAnchor(root.transform, "Fill", new Vector3(0f, 0.025f, 0f));
            AddPart(fill, PrimitiveType.Cylinder, "Ring", Vector3.zero, new Vector3(2f, 0.005f, 2f),
                GetOrCreateMaterial("TravelFill", new Color(1f, 1f, 1f)));

            var signBase = new Vector3(0f, 0f, 1.5f);
            GameObject pole = AddPart(root.transform, PrimitiveType.Cylinder, "Pole", signBase + new Vector3(0f, 1f, 0f),
                new Vector3(0.1f, 1f, 0.1f), GetOrCreateMaterial("SignPole", new Color(0.4f, 0.4f, 0.45f)));
            GameObject sign = AddPart(root.transform, PrimitiveType.Cylinder, "Sign", signBase + new Vector3(0f, 2.1f, 0f),
                new Vector3(0.7f, 0.03f, 0.7f), GetOrCreateMaterial(name, color));
            sign.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (ArtLibrary.Attach("BusStop", root.transform, new Vector3(1.6f, 2.4f, 0.6f), ArtLibrary.Fit.Height, signBase) != null)
            {
                ArtLibrary.HideGrayBox(pole, sign);
            }

            // Floating text, turned toward the camera while playing.
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 2.9f, 1.5f);
            labelGo.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
            var text = labelGo.AddComponent<TextMesh>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.font = font;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.text = "To " + destination;
            text.fontSize = 64;
            text.characterSize = 0.06f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.25f, 0.2f, 0.2f);

            var travel = root.AddComponent<ShopTravel>();
            var serialized = new SerializedObject(travel);
            serialized.FindProperty("targetScene").stringValue = targetScene;
            serialized.FindProperty("fillRing").objectReferenceValue = fill;
            serialized.FindProperty("label").objectReferenceValue = labelGo.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/{name}.prefab");
        }

        // A purple desk. Standing in front of it opens the upgrade panel.
        // A decoration you can buy: it only makes the shop nicer (and raises the rating).
        // Solid, so the player walks around it; the art slot of the same name replaces the gray box.
        private static GameObject BuildDecorPrefab(string name, Color color, Vector3 size)
        {
            GameObject root = BuildStationBody(name, color, size, null);
            return SavePrefab(root, $"{PrefabFolder}/Decor/{name}.prefab");
        }

        private static GameObject BuildUpgradeDeskPrefab()
        {
            GameObject root = BuildStationBody("UpgradeDesk", new Color(0.58f, 0.44f, 0.86f), new Vector3(1.6f, 1f, 1f),
                new Color(1f, 0.85f, 0.3f));
            root.AddComponent<UpgradeZone>();
            return SavePrefab(root, $"{PrefabFolder}/Stations/UpgradeDesk.prefab");
        }

        // A green helper with an apron-colored marker, carrying one item at a time.

        // Two café tables with two seats each. Seated customers leave a little money in the pile
        // and an empty cup that blocks the seat until someone cleans it.
        private static GameObject BuildSeatingPrefab(CashBill billPrefab)
        {
            var root = new GameObject("SeatingArea");
            Material cup = GetOrCreateMaterial("EmptyCup", new Color(0.97f, 0.97f, 0.95f));
            Material straw = GetOrCreateMaterial("Straw", new Color(0.95f, 0.45f, 0.55f));

            var cashGo = new GameObject("CashPile");
            cashGo.transform.SetParent(root.transform, false);
            cashGo.transform.localPosition = new Vector3(0f, 0f, -1.5f);
            var cashPile = cashGo.AddComponent<CashPile>();
            var serializedPile = new SerializedObject(cashPile);
            serializedPile.FindProperty("billPrefab").objectReferenceValue = billPrefab;
            serializedPile.FindProperty("valuePerBill").intValue = 1;
            serializedPile.FindProperty("startingBills").intValue = 0;
            serializedPile.FindProperty("maxBills").intValue = 40;
            serializedPile.ApplyModifiedPropertiesWithoutUndo();

            // Four tables in two rows of two, eight seats in all.
            for (int t = 0; t < 4; t++)
            {
                GameObject table = BuildStationBody("Table", new Color(0.83f, 0.64f, 0.46f), new Vector3(2.6f, 0.75f, 1.2f), null);
                table.transform.SetParent(root.transform, false);
                table.transform.localPosition = new Vector3(t % 2 == 0 ? -1.8f : 1.8f, 0f, t < 2 ? 0f : 2.8f);

                var seats = new Transform[2];
                var messes = new GameObject[2];
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? -1f : 1f;
                    seats[s] = AddAnchor(table.transform, "Seat", new Vector3(side * 1f, 0f, 0f));
                    seats[s].localRotation = Quaternion.Euler(0f, side > 0f ? -90f : 90f, 0f);

                    // An empty cup with its straw, left behind when a customer leaves.
                    Transform mess = AddAnchor(table.transform, "Mess", new Vector3(side * 0.3f, 0.75f, 0.1f));
                    AddPart(mess, PrimitiveType.Cylinder, "Cup", new Vector3(0f, 0.09f, 0f), new Vector3(0.16f, 0.09f, 0.16f), cup);
                    GameObject strawPart = AddPart(mess, PrimitiveType.Cube, "Straw", new Vector3(0.02f, 0.22f, 0f),
                        new Vector3(0.02f, 0.12f, 0.02f), straw);
                    strawPart.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
                    messes[s] = mess.gameObject;
                }

                var seating = table.AddComponent<SeatingTable>();
                var serialized = new SerializedObject(seating);
                SerializedProperty seatList = serialized.FindProperty("seats");
                SerializedProperty messList = serialized.FindProperty("messes");
                seatList.arraySize = 2;
                messList.arraySize = 2;
                for (int s = 0; s < 2; s++)
                {
                    seatList.GetArrayElementAtIndex(s).objectReferenceValue = seats[s];
                    messList.GetArrayElementAtIndex(s).objectReferenceValue = messes[s];
                }
                serialized.FindProperty("zone.offset").vector3Value = Vector3.zero;
                serialized.FindProperty("zone.halfSize").floatValue = 1.8f;
                serialized.FindProperty("cashPile").objectReferenceValue = cashPile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            return SavePrefab(root, $"{PrefabFolder}/Decor/SeatingArea.prefab");
        }

        // A lavender barista who stands behind the busiest station and speeds it up.
        // A cashier who stands by the counter and banks customers' payments for you.
        private static GameObject BuildCashierPrefab(string prefabName, string lane)
        {
            var root = new GameObject(prefabName);
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Cashier", new Color(0.95f, 0.78f, 0.35f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("RunnerApron", new Color(0.98f, 0.95f, 0.85f)));
            AddCharacterArt("Cashier", root, body);
            Transform hands = AddAnchor(root.transform, "Hands", new Vector3(0f, 0.9f, 0.4f));
            var cashier = root.AddComponent<Cashier>();
            SetReference(cashier, "hands", hands);
            var serialized = new SerializedObject(cashier);
            serialized.FindProperty("lane").stringValue = lane;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, $"{PrefabFolder}/{prefabName}.prefab");
        }

        // A barista: carries drinks and ingredients where they're needed, and helps at the
        // busiest station when there's nothing to carry.
        private static GameObject BuildBaristaPrefab()
        {
            var root = new GameObject("Barista");
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Barista", new Color(0.66f, 0.56f, 0.86f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("RunnerApron", new Color(0.98f, 0.95f, 0.85f)));
            AddCharacterArt("Barista", root, body);

            Transform hands = AddAnchor(root.transform, "HandStack", new Vector3(0f, 0.9f, 0.55f));
            var carrier = root.AddComponent<ItemCarrier>();
            var serialized = new SerializedObject(carrier);
            serialized.FindProperty("stack.anchor").objectReferenceValue = hands;
            serialized.FindProperty("stack.capacity").intValue = 1;
            serialized.FindProperty("usesCarryUpgrade").boolValue = true; // Carry More lets baristas carry more too
            serialized.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<Barista>();
            return SavePrefab(root, $"{PrefabFolder}/Barista.prefab");
        }

        // A cleaner who clears cups off the tables and collects what seated customers left.
        private static GameObject BuildCleanerPrefab()
        {
            var root = new GameObject("Cleaner");
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Cleaner", new Color(0.45f, 0.75f, 0.85f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("RunnerApron", new Color(0.98f, 0.95f, 0.85f)));
            AddCharacterArt("Cleaner", root, body);
            Transform hands = AddAnchor(root.transform, "Hands", new Vector3(0f, 0.9f, 0.4f));
            var cleaner = root.AddComponent<Cleaner>();
            SetReference(cleaner, "hands", hands);
            return SavePrefab(root, $"{PrefabFolder}/Cleaner.prefab");
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
            serialized.FindProperty("canSit").boolValue = false;
            serialized.FindProperty("faceCounter").boolValue = false; // cars keep facing up the lane
            serialized.FindProperty("drinkHoldPosition").vector3Value = new Vector3(-0.9f, 1f, 0.4f); // driver's side, at the window
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
        private static GameObject BuildStationBody(string name, Color bodyColor, Vector3 bodySize, Color? topColor,
            string artSlot = null)
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
            if (ArtLibrary.Attach(artSlot ?? name, root.transform, bodySize, ArtLibrary.Fit.Footprint) != null)
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
