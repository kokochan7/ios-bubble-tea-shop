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
        private static int shopNumber = 1;
        private static string staffSlotPrefix = ""; // "Matcha" in the Matcha House: green uniforms

        /// <summary>
        /// The art slot for a staff member (or the player) in the shop being built: the Matcha
        /// House has its own green-uniformed versions, when the Art Set has them.
        /// </summary>
        internal static string StaffSlot(string slotName) =>
            staffSlotPrefix.Length > 0 && ArtLibrary.Load().Get(staffSlotPrefix + slotName) != null
                ? staffSlotPrefix + slotName
                : slotName;

        internal struct Content
        {
            public BuildableDefinition Counter;
            public BuildableDefinition TeaBrewer;
            public BuildableDefinition TaroStation;
            public BuildableDefinition FruitBar;
            /// <summary>A fifth bar (the Matcha House's Yuzu Bar). Null in shops without one.</summary>
            public BuildableDefinition ExtraBar;
            public BuildableDefinition Expansion;
            public BuildableDefinition DriveThru;
            public Customer CarPrefab;
            public BuildableDefinition UpgradeDesk;
            public UpgradeDefinition[] Upgrades;
            public Customer CustomerPrefab;
            public DrinkRecipe[] Menu;
            public BuildableDefinition Planter;
            /// <summary>Decor bought on pads, in the order the pads appear (each makes the shop nicer, see ShopDecor).</summary>
            public BuildableDefinition[] Decor;
            public BuildableDefinition MenuBoard;
            public BuildableDefinition Statue;
            /// <summary>Hire Barista pads in the order they unlock.</summary>
            public BuildableDefinition[] Baristas;
            public BuildableDefinition Cleaner;
            public BuildableDefinition Seating;
            public BuildableDefinition Cashier;
            public BuildableDefinition DriveThruCashier;

            /// <summary>Matcha House only: the stocker who carries crates to the bars. Null in other shops.</summary>
            public BuildableDefinition Stocker;

            /// <summary>Matcha House only: the crate of supplies the delivery truck brings. Null in other shops.</summary>
            public ItemDefinition Crate;

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
            public GameObject TaroStation;
            public GameObject FruitBar;
            public GameObject ExtraBar; // optional fifth bar
            public ItemDefinition Supply; // optional crate some bars use up
            public string ExtraName;
            public string BrewerName;
            public string TaroName;
            public string FruitName;
            // New names for the pads and labels; the save files keep the names above.
            public string TaroShownName, FruitShownName, ExtraShownName;
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

            shopNumber = shop;
            staffSlotPrefix = shop > 1 ? "Matcha" : "";
            MenuSet set = shop > 1 ? BuildMatchaMenu() : BuildClassicMenu();
            DrinkRecipe[] menu = set.Menu;
            GameObject counter = BuildCounterPrefab("Counter", new Color(0.62f, 0.42f, 0.28f), menu, billPrefab,
                "Main", priceMultiplier: 1f, queueSpacing: 1.1f);
            GameObject driveThru = BuildCounterPrefab("DriveThru", new Color(0.35f, 0.55f, 0.75f), menu, billPrefab,
                "DriveThru", priceMultiplier: 1.5f, queueSpacing: 3.2f,
                // The window faces the car lane: cars queue along the lane, which runs past the
                // counter's customer side, instead of straight out from it.
                queueOffset: new Vector3(0f, 0f, -3.5f), queueYaw: -90f);

            // Values per level start at level 0; costs start at the price of level 1. The Boba Shop's
            // upgrades max out at Lv 3/3, the Matcha House's at Lv 4/4. Brew Speed starts slow (0.75x)
            // so the first upgrade feels worth it. The Boba Shop has no Drink Shelf Space upgrade.
            int maxLevel = shop > 1 ? 4 : 3;
            var upgradeList = new System.Collections.Generic.List<UpgradeDefinition>
            {
                GetOrCreateUpgrade("Carry More", StatId.CarryBonus,
                    Levels(maxLevel, 0f, 1f, 2f, 3f, 4f), Costs(maxLevel, 50, 120, 250, 500)), // +1 item per level (see ItemCarrier)
                GetOrCreateUpgrade("Brew Speed", StatId.BrewSpeed,
                    Levels(maxLevel, 0.75f, 1f, 1.25f, 1.5f, 1.75f), Costs(maxLevel, 60, 140, 280, 550)),
            };
            if (shop > 1)
            {
                // Saved as "Bigger Bars" (the asset name), shown as "Drink Shelf Space".
                upgradeList.Add(GetOrCreateUpgrade("Bigger Bars", StatId.StationCapacity,
                    Levels(maxLevel, 0f, 1f, 2f, 3f, 4f), Costs(maxLevel, 70, 150, 300, 600), // +1 finished drink per bar (see Station)
                    shownName: "Drink Shelf Space"));
                // The Whisk Bar is whisked by hand until Whisk Master lets it run on its own.
                upgradeList.Add(GetOrCreateUpgrade("Whisk Master", StatId.AutoWhisk, new[] { 0f, 1f }, new[] { 500 }));
            }
            UpgradeDefinition[] upgrades = upgradeList.ToArray();
            GameObject upgradeDesk = BuildUpgradeDeskPrefab();
            GameObject barista = BuildBaristaPrefab();

            return new Content
            {
                Counter = GetOrCreateBuildable("Counter", 30, counter),
                TeaBrewer = GetOrCreateBuildable(set.BrewerName, 80, set.Brewer),
                TaroStation = GetOrCreateBuildable(set.TaroName, 250, set.TaroStation, shownName: set.TaroShownName),
                FruitBar = GetOrCreateBuildable(set.FruitName, 400, set.FruitBar, shownName: set.FruitShownName),
                ExtraBar = set.ExtraBar != null ? GetOrCreateBuildable(set.ExtraName, 700, set.ExtraBar, shownName: set.ExtraShownName) : null,
                // The expansion builds nothing itself; its pad opens the wall and shows the new floor.
                Expansion = GetOrCreateBuildable("Expand Shop", 1000, null),
                DriveThru = GetOrCreateBuildable("Drive Thru", 600, driveThru),
                CarPrefab = BuildCarPrefab(),
                UpgradeDesk = GetOrCreateBuildable("Upgrades", 40, upgradeDesk),
                Upgrades = upgrades,
                CustomerPrefab = BuildCustomerPrefab(),
                Menu = menu,
                Seating = GetOrCreateBuildable("Seating Area", 500, BuildSeatingPrefab(billPrefab)),
                // 2 baristas in the Boba Shop, 3 in the Matcha House.
                Baristas = shop > 1
                    ? new[]
                    {
                        GetOrCreateBuildable("Hire Barista", 150, barista, keepPriceUpToDate: true),
                        GetOrCreateBuildable("Hire Barista 2", 500, barista, keepPriceUpToDate: true),
                        GetOrCreateBuildable("Hire Barista 3", 900, barista),
                    }
                    : new[]
                    {
                        GetOrCreateBuildable("Hire Barista", 150, barista, keepPriceUpToDate: true),
                        GetOrCreateBuildable("Hire Barista 2", 500, barista, keepPriceUpToDate: true),
                    },
                Cleaner = GetOrCreateBuildable("Hire Cleaner", 400, BuildCleanerPrefab()),
                Cashier = GetOrCreateBuildable("Hire Cashier", 150, BuildCashierPrefab("Cashier", "Main")),
                DriveThruCashier = GetOrCreateBuildable("Hire Drive-Thru Cashier", 400,
                    BuildCashierPrefab("DriveThruCashier", "DriveThru")),
                // Decor that pays: patience, customer flow and happiness (prices x3 in the Matcha House).
                Decor = new[]
                {
                    GetOrCreateBuildable("Bamboo Plants", 150, BuildShopDecorPrefab("BambooPlant", "Bamboo Plants",
                        new Color(0.49f, 0.71f, 0.35f), new Vector3(0.55f, 1.8f, 0.55f), new Vector3(0.6f, 1.9f, 0.6f), ArtLibrary.Fit.Height,
                        patience: 0.05f, flow: 0f, happiness: 0f)),
                    GetOrCreateBuildable("Paper Lanterns", 300, BuildShopDecorPrefab("PaperLanterns", "Paper Lanterns",
                        new Color(0.9f, 0.28f, 0.23f), new Vector3(0.45f, 2.3f, 0.45f), new Vector3(1f, 2.35f, 0.5f), ArtLibrary.Fit.Height,
                        patience: 0f, flow: 0f, happiness: 0.05f)),
                    GetOrCreateBuildable("Lucky Cat", 500, BuildShopDecorPrefab("LuckyCat", "Lucky Cat",
                        new Color(0.97f, 0.97f, 0.95f), new Vector3(0.6f, 1.1f, 0.6f), new Vector3(0.6f, 1.15f, 0.6f), ArtLibrary.Fit.Height,
                        patience: 0f, flow: 0.05f, happiness: 0.05f)),
                    GetOrCreateBuildable("Koi Pond", 900, BuildShopDecorPrefab("KoiPond", "Koi Pond",
                        new Color(0.44f, 0.76f, 0.85f), new Vector3(1.9f, 0.25f, 1.25f), new Vector3(2f, 0.3f, 1.3f), ArtLibrary.Fit.Footprint,
                        patience: 0.1f, flow: 0.05f, happiness: 0f)),
                    GetOrCreateBuildable("Cherry Blossom Tree", 1400, BuildShopDecorPrefab("SakuraTree", "Cherry Blossom Tree",
                        new Color(1f, 0.72f, 0.81f), new Vector3(0.7f, 2.6f, 0.7f), new Vector3(1.6f, 2.7f, 1.6f), ArtLibrary.Fit.Height,
                        patience: 0.05f, flow: 0.1f, happiness: 0.1f)),
                },
                Planter = GetOrCreateBuildable("Planter", 120,
                    BuildDecorPrefab("Planter", new Color(0.42f, 0.73f, 0.47f), new Vector3(1.2f, 1.3f, 1.2f))),
                MenuBoard = GetOrCreateBuildable("Menu Board", 200,
                    BuildDecorPrefab("MenuBoard", new Color(0.3f, 0.32f, 0.3f), new Vector3(1f, 1.4f, 0.6f))),
                Statue = GetOrCreateBuildable("Boba Statue", 350,
                    BuildDecorPrefab("Statue", new Color(0.85f, 0.7f, 0.55f), new Vector3(1.2f, 1.8f, 1.2f))),
                // Built once the first shop is finished; the stop it builds leads to the Matcha House.
                SecondShop = shop == 1
                    ? GetOrCreateBuildable("Open Matcha House", 5000,
                        BuildTravelPrefab("ToMatchaHouse", "Shop2", "Matcha House", new Color(0.55f, 0.78f, 0.45f)),
                        keepPriceUpToDate: true)
                    : null,
                TravelBack = shop > 1
                    ? BuildTravelPrefab("ToBobaShop", "Prototype", "Boba Shop", new Color(0.95f, 0.62f, 0.55f))
                    : null,
                Stocker = set.Supply != null ? GetOrCreateBuildable("Hire Stocker", 200, BuildStockerPrefab()) : null,
                Crate = set.Supply,
            };
        }

        // Shop 1: classic milk teas, taro and mango.
        private static MenuSet BuildClassicMenu()
        {
            ItemDefinition milkTea = GetOrCreateItem("Milk Tea", new Color(0.85f, 0.70f, 0.52f), null);
            ItemDefinition taroTea = GetOrCreateItem("Taro Milk Tea", new Color(0.66f, 0.50f, 0.84f), null);
            ItemDefinition mangoTea = GetOrCreateItem("Mango Fruit Tea", new Color(1f, 0.86f, 0.2f), null); // mango yellow

            RecipeStep brew = GetOrCreateStep("Brew Milk Tea", null, milkTea, 2f);
            RecipeStep makeTaro = GetOrCreateStep("Make Taro Tea", null, taroTea, 2.5f);
            RecipeStep makeMango = GetOrCreateStep("Make Mango Tea", null, mangoTea, 2f);

            return new MenuSet
            {
                Menu = new[]
                {
                    GetOrCreateDrink("Milk Tea", milkTea, 10),
                    GetOrCreateDrink("Taro Milk Tea", taroTea, 14),
                    GetOrCreateDrink("Mango Fruit Tea", mangoTea, 17),
                },
                Brewer = BuildStationPrefab("TeaBrewer", new Color(0.30f, 0.30f, 0.34f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    new Color(0.55f, 0.32f, 0.16f), new[] { brew }, inputCapacity: 0),
                TaroStation = BuildStationPrefab("TaroStation", new Color(0.45f, 0.32f, 0.62f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    new Color(0.80f, 0.68f, 0.95f), new[] { makeTaro }, inputCapacity: 0),
                FruitBar = BuildStationPrefab("FruitBar", new Color(0.95f, 0.55f, 0.20f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    new Color(1f, 0.85f, 0.25f), new[] { makeMango }, inputCapacity: 0),
                BrewerName = "Tea Brewer",
                // Saved under their first names, shown under Kenny's newer ones.
                TaroName = "Taro Station",
                TaroShownName = "Taro Bar",
                FruitName = "Fruit Bar",
                FruitShownName = "Mango Bar",
            };
        }

        // Shop 2, the Matcha House: a Japanese-style matcha café with its own four bars, unlocked in
        // this order: lattes are whisked at the Whisk Bar, the Hojicha Bar roasts hojicha lattes, the Sencha Bar brews sencha green tea and the
        // Tenju Bar grinds the finest matcha. Sencha and Tenju use up matcha powder (brought in crates
        // by the delivery truck), so they sell for double.
        private static MenuSet BuildMatchaMenu()
        {
            Color lightGreen = new(0.74f, 0.88f, 0.52f);   // matcha latte
            Color darkBrown = new(0.40f, 0.25f, 0.14f);    // hojicha
            Color senchaGreen = new(0.36f, 0.64f, 0.22f);
            Color tenjuGreen = new(0.08f, 0.22f, 0.06f);   // super dark
            ItemDefinition matcha = GetOrCreateItem("Matcha Latte", lightGreen, null);
            ItemDefinition hojicha = GetOrCreateItem("Hojicha Latte", darkBrown, null);
            ItemDefinition sencha = GetOrCreateItem("Sencha", senchaGreen, null);
            ItemDefinition tenju = GetOrCreateItem("Tenju Matcha", tenjuGreen, null);

            RecipeStep whisk = GetOrCreateStep("Whisk Matcha Latte", null, matcha, 2f);
            RecipeStep roast = GetOrCreateStep("Roast Hojicha Latte", null, hojicha, 2.5f);
            RecipeStep brewSencha = GetOrCreateStep("Brew Sencha", null, sencha, 2.4f);
            RecipeStep grindTenju = GetOrCreateStep("Grind Tenju Matcha", null, tenju, 3f);
            ItemDefinition matchaPowder = GetOrCreateCrate("Matcha Powder", new Color(0.55f, 0.72f, 0.33f));

            return new MenuSet
            {
                Menu = new[]
                {
                    GetOrCreateDrink("Matcha Latte", matcha, 19),
                    GetOrCreateDrink("Hojicha Latte", hojicha, 22),
                    // Double price: they're expensive to make.
                    GetOrCreateDrink("Sencha", sencha, 64),
                    GetOrCreateDrink("Tenju Matcha", tenju, 72),
                },
                Brewer = BuildStationPrefab("WhiskBar", new Color(0.36f, 0.52f, 0.32f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    new Color(0.55f, 0.75f, 0.38f), new[] { whisk }, inputCapacity: 0, artSlot: "WhiskBar", handMade: true),
                TaroStation = BuildStationPrefab("HojichaBar", new Color(0.40f, 0.26f, 0.16f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    darkBrown, new[] { roast }, inputCapacity: 0, artSlot: "HojichaBar"),
                FruitBar = BuildStationPrefab("SenchaBar", new Color(0.36f, 0.6f, 0.26f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    senchaGreen, new[] { brewSencha }, inputCapacity: 0, artSlot: "SenchaBar", supply: matchaPowder),
                ExtraBar = BuildStationPrefab("TenjuBar", new Color(0.14f, 0.26f, 0.12f), new Vector3(1.4f, 1f, 1f) * BarScale,
                    tenjuGreen, new[] { grindTenju }, inputCapacity: 0, artSlot: "TenjuBar", supply: matchaPowder),
                Supply = matchaPowder,
                BrewerName = "Whisk Bar",
                TaroName = "Hojicha Bar",
                // They replace the Strawberry and Yuzu bars: saves keep those names, so a bar already
                // bought comes back as its replacement.
                FruitName = "Strawberry Bar",
                FruitShownName = "Sencha Bar",
                ExtraName = "Yuzu Bar",
                ExtraShownName = "Tenju Bar",
            };
        }

        // The values for levels 0 to maxLevel.
        private static float[] Levels(int maxLevel, params float[] values) =>
            System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(values, maxLevel + 1));

        // The prices of levels 1 to maxLevel, scaled for the shop.
        private static int[] Costs(int maxLevel, params int[] costs) =>
            Scaled(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(costs, maxLevel)));

        private static int[] Scaled(params int[] costs)
        {
            var result = new int[costs.Length];
            for (int i = 0; i < costs.Length; i++) result[i] = costs[i] * costScale;
            return result;
        }

        // ---------- Data assets ----------

        // The drink's color, kept up to date with the code on every build (so a color change reaches
        // cups made before it).
        private static Material DrinkMaterial(string name, Color color)
        {
            Material material = GetOrCreateMaterial(name, color);
            if (material.color != color)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
            }
            return material;
        }

        private static ItemDefinition GetOrCreateItem(string displayName, Color cupColor, Color? pearlColor, Color? foamColor = null)
        {
            var item = GetOrCreateAsset<ItemDefinition>($"{DataFolder}/Items/{FileName(displayName)}.asset",
                serialized => serialized.FindProperty("displayName").stringValue = displayName);

            // The model is rebuilt every time, then linked both ways.
            GameObject prefab = BuildItemPrefab(FileName(displayName), item, cupColor, pearlColor, foamColor);
            var serializedItem = new SerializedObject(item);
            serializedItem.FindProperty("prefab").objectReferenceValue = prefab;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        // A cardboard crate of supplies (a box with a colored band and lid), carried like a drink.
        private static ItemDefinition GetOrCreateCrate(string displayName, Color bandColor)
        {
            var item = GetOrCreateAsset<ItemDefinition>($"{DataFolder}/Items/{FileName(displayName)}.asset",
                serialized => serialized.FindProperty("displayName").stringValue = displayName);

            var root = new GameObject(FileName(displayName));
            Material cardboard = GetOrCreateMaterial("Crate", new Color(0.82f, 0.64f, 0.42f));
            Material band = GetOrCreateMaterial(FileName(displayName) + "Band", bandColor);
            AddPart(root.transform, PrimitiveType.Cube, "Box", new Vector3(0f, 0.11f, 0f), new Vector3(0.28f, 0.22f, 0.28f), cardboard);
            AddPart(root.transform, PrimitiveType.Cube, "Band", new Vector3(0f, 0.11f, 0f), new Vector3(0.29f, 0.07f, 0.29f), band);
            AddPart(root.transform, PrimitiveType.Cube, "Lid", new Vector3(0f, 0.225f, 0f), new Vector3(0.3f, 0.02f, 0.3f), band);
            var carried = root.AddComponent<CarriedItem>();
            SetReference(carried, "definition", item);
            GameObject prefab = SavePrefab(root, $"{PrefabFolder}/Items/{FileName(displayName)}.prefab");

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

        // Prices are 15% above the original menu (rounded up) and are kept up to date on every build.
        private static DrinkRecipe GetOrCreateDrink(string name, ItemDefinition drink, int price)
        {
            DrinkRecipe recipe = GetOrCreateAsset<DrinkRecipe>($"{DataFolder}/Menu/{FileName(name)}.asset", serialized =>
            {
                serialized.FindProperty("drink").objectReferenceValue = drink;
            });
            var serializedRecipe = new SerializedObject(recipe);
            serializedRecipe.FindProperty("price").intValue = price;
            serializedRecipe.ApplyModifiedPropertiesWithoutUndo();
            return recipe;
        }

        // Values and costs are kept up to date on every build.
        // shownName renames an upgrade in the menu without changing its asset (which save files refer to).
        private static UpgradeDefinition GetOrCreateUpgrade(string displayName, StatId stat, float[] values, int[] costs,
            string shownName = null)
        {
            UpgradeDefinition upgrade = GetOrCreateAsset<UpgradeDefinition>($"{DataFolder}/Upgrades/{FileName(displayName)}.asset", serialized =>
            {
                serialized.FindProperty("displayName").stringValue = displayName;
                serialized.FindProperty("stat").enumValueIndex = (int)stat;
            });
            var serialized = new SerializedObject(upgrade);
            if (shownName != null) serialized.FindProperty("displayName").stringValue = shownName;
            SerializedProperty valueList = serialized.FindProperty("values");
            valueList.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) valueList.GetArrayElementAtIndex(i).floatValue = values[i];

            SerializedProperty costList = serialized.FindProperty("costs");
            costList.arraySize = costs.Length;
            for (int i = 0; i < costs.Length; i++) costList.GetArrayElementAtIndex(i).intValue = costs[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return upgrade;
        }

        // keepPriceUpToDate: also reset the price of an existing asset (for a price change that
        // should reach scenes built before it).
        // shownName renames it on its pad and label without changing its asset (which save files refer to).
        private static BuildableDefinition GetOrCreateBuildable(string displayName, int cost, GameObject prefab,
            bool keepPriceUpToDate = false, string shownName = null)
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
            if (shownName != null) serializedBuildable.FindProperty("displayName").stringValue = shownName;
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
        private static GameObject BuildItemPrefab(string name, ItemDefinition definition, Color cupColor, Color? pearlColor,
            Color? foamColor)
        {
            var root = new GameObject(name);

            AddPart(root.transform, PrimitiveType.Cylinder, "Cup", new Vector3(0f, 0.14f, 0f),
                new Vector3(0.24f, 0.14f, 0.24f), DrinkMaterial(name, cupColor));
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
                    ArtLibrary.Recolor(drink, DrinkMaterial(name, cupColor));
                    Transform artPearls = ArtLibrary.FindChild(cupArt.transform, "Pearls");
                    if (artPearls != null)
                    {
                        if (pearlColor.HasValue) ArtLibrary.Recolor(artPearls, GetOrCreateMaterial("Pearls", pearlColor.Value));
                        else
                        {
                            // No pearls: the drink fills the cup right to the bottom.
                            artPearls.gameObject.SetActive(false);
                            drink.localPosition = new Vector3(drink.localPosition.x, 0.005f, drink.localPosition.z);
                            drink.localScale = new Vector3(drink.localScale.x, drink.localScale.y * 1.33f, drink.localScale.z);
                        }
                    }
                    // A layer on top: cream foam, or matcha poured over strawberry milk.
                    Transform foam = ArtLibrary.FindChild(cupArt.transform, "Foam");
                    if (foam != null)
                    {
                        if (foamColor.HasValue) ArtLibrary.Recolor(foam, GetOrCreateMaterial(name + "Foam", foamColor.Value));
                        else foam.gameObject.SetActive(false);
                    }
                }
                else
                {
                    AddPart(root.transform, PrimitiveType.Cylinder, "Lid", new Vector3(0f, 0.33f, 0f),
                        new Vector3(0.22f, 0.01f, 0.22f), DrinkMaterial(name, cupColor));
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

        // The bars and the counter are built this much bigger than their art, so the drinks on
        // them can be big too.
        private const float BarScale = 1.25f;

        // Size of the drinks on the bars and the counter: the same as everywhere else.
        private const float DrinkSizeOnBars = DrinkSize;

        // A floating high-tech screen low on the customers' side of the counter, showing what the
        // customer at the front still wants: a dark glass screen with glowing cyan text in a metal
        // bezel with a neon rim, a "NOW ORDERING" header and a status light.
        // Kept low so it doesn't hide the kitchen behind it.
        private static void AddOrderBoard(GameObject root, ServingCounter counter, float counterTop)
        {
            Transform board = AddAnchor(root.transform, "OrderBoard", new Vector3(0f, counterTop - 0.1f, -0.35f));
            board.localScale = Vector3.one * 0.95f;
            Color cyan = new Color(0.45f, 0.95f, 1f);
            Transform screen = AddScreenPart(board, "Screen", new Vector3(0f, 0.42f, 0.08f), new Vector3(1f, 1.1f, 0.04f),
                ScreenMaterial("OrderScreen", new Color(0.03f, 0.06f, 0.13f), new Color(0.02f, 0.06f, 0.12f)));
            Transform bezel = AddScreenPart(board, "Bezel", new Vector3(0f, 0.42f, 0.11f), new Vector3(1.1f, 1.2f, 0.05f),
                ScreenMaterial("OrderScreenBezel", new Color(0.13f, 0.14f, 0.18f), Color.black));
            Transform rim = AddScreenPart(board, "NeonRim", new Vector3(0f, 0.42f, 0.13f), new Vector3(1.16f, 1.26f, 0.02f),
                ScreenMaterial("OrderScreenRim", cyan, cyan * 1.4f));
            Transform led = AddScreenPart(board, "StatusLight", new Vector3(0f, 0f, 0.08f), new Vector3(0.05f, 0.05f, 0.02f),
                ScreenMaterial("OrderScreenLed", new Color(0.3f, 1f, 0.45f), new Color(0.3f, 1f, 0.45f) * 1.5f));
            Transform icons = AddAnchor(board, "Icons", Vector3.zero);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // The order in words above the pictures.
            TextMesh label = AddScreenText(board, "OrderText", font, new Vector3(0f, 0.7f, 0f), 64, 0.035f, cyan);
            // A small header along the top of the screen.
            TextMesh header = AddScreenText(board, "Header", font, new Vector3(0f, 1f, 0f), 48, 0.022f, new Color(1f, 0.55f, 0.85f));
            header.text = "NOW ORDERING";
            board.gameObject.SetActive(false);

            var view = root.AddComponent<OrderBoard>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("counter").objectReferenceValue = counter;
            serialized.FindProperty("board").objectReferenceValue = board;
            serialized.FindProperty("iconRow").objectReferenceValue = icons;
            serialized.FindProperty("backing").objectReferenceValue = screen;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("header").objectReferenceValue = header;
            serialized.FindProperty("bezel").objectReferenceValue = bezel;
            serialized.FindProperty("rim").objectReferenceValue = rim;
            serialized.FindProperty("statusLight").objectReferenceValue = led;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform AddScreenPart(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return part.transform;
        }

        private static TextMesh AddScreenText(Transform parent, string name, Font font, Vector3 position, int size, float characterSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var text = go.AddComponent<TextMesh>();
            text.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            text.fontSize = size;
            text.characterSize = characterSize;
            text.fontStyle = FontStyle.Bold;
            text.anchor = TextAnchor.LowerCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            return text;
        }

        // A glowing material (set every build, so older materials pick up the new look).
        private static Material ScreenMaterial(string name, Color color, Color emission)
        {
            Material material = GetOrCreateMaterial(name, color);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.85f);
            if (material.HasProperty("_EmissionColor"))
            {
                if (emission.maxColorComponent > 0f) material.EnableKeyword("_EMISSION");
                else material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        // supply: a crate the bar uses up (one scoop per drink). handMade: only works while the player stands at it.
        private static GameObject BuildStationPrefab(string name, Color bodyColor, Vector3 bodySize, Color topColor,
            RecipeStep[] steps, int inputCapacity, string artSlot = null, ItemDefinition supply = null, bool handMade = false)
        {
            GameObject root = BuildStationBody(name, bodyColor, bodySize, topColor, artSlot);
            var station = root.AddComponent<Station>();
            var serialized = new SerializedObject(station);
            serialized.FindProperty("supplyItem").objectReferenceValue = supply;
            serialized.FindProperty("needsPlayer").boolValue = handMade;

            SerializedProperty stepList = serialized.FindProperty("steps");
            stepList.arraySize = steps.Length;
            for (int i = 0; i < steps.Length; i++) stepList.GetArrayElementAtIndex(i).objectReferenceValue = steps[i];

            // Finished drinks stand side by side along the front of the bar (the machines are at
            // the back), big enough to read at a glance. 3 to start; Drink Shelf Space adds one per level.
            float halfWidth = bodySize.x * 0.5f;
            float outputX = inputCapacity > 0 ? halfWidth - 0.8f : 0f;
            serialized.FindProperty("output.anchor").objectReferenceValue =
                AddAnchor(root.transform, "OutputStack", new Vector3(outputX, bodySize.y, 0.22f));
            serialized.FindProperty("output.capacity").intValue = 3;
            SetStackGrid(serialized, "output", 4, 2, 0.4f, DrinkSizeOnBars);

            serialized.FindProperty("input.capacity").intValue = inputCapacity;
            if (inputCapacity > 0)
            {
                serialized.FindProperty("input.anchor").objectReferenceValue =
                    AddAnchor(root.transform, "InputStack", new Vector3(-halfWidth + 0.42f, bodySize.y, 0.22f));
                SetStackGrid(serialized, "input", 2, 2, 0.4f, DrinkSizeOnBars);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (handMade) AddWhiskRig(root.transform, station, bodySize.y);
            return SavePrefab(root, $"{PrefabFolder}/Stations/{name}.prefab");
        }

        // A tea bowl with a bamboo whisk standing in it, at the back of the bar, which whisks
        // back and forth while the drink is being made (see WhiskMotion).
        private static void AddWhiskRig(Transform bar, Station station, float topY)
        {
            Transform rig = AddAnchor(bar, "WhiskRig", new Vector3(0.24f, topY, -0.3f));
            AddPart(rig, PrimitiveType.Sphere, "Bowl", new Vector3(0f, 0.06f, 0f), new Vector3(0.3f, 0.2f, 0.3f),
                GetOrCreateMaterial("WhiskBowl", new Color(0.93f, 0.9f, 0.84f)));
            AddPart(rig, PrimitiveType.Cylinder, "Tea", new Vector3(0f, 0.11f, 0f), new Vector3(0.22f, 0.006f, 0.22f),
                GetOrCreateMaterial("WhiskTea", new Color(0.42f, 0.62f, 0.2f)));
            GameObject froth = AddPart(rig, PrimitiveType.Sphere, "Froth", new Vector3(0f, 0.115f, 0f), new Vector3(0.2f, 0.035f, 0.2f),
                GetOrCreateMaterial("WhiskFroth", new Color(0.72f, 0.86f, 0.45f)));

            // The whisk pivots at its tines, in the tea; the handle sticks up out of the bowl.
            Transform whisk = AddAnchor(rig, "Whisk", new Vector3(0f, 0.1f, 0f));
            Material bamboo = GetOrCreateMaterial("WhiskBamboo", new Color(0.85f, 0.77f, 0.54f));
            AddPart(whisk, PrimitiveType.Cylinder, "Tines", new Vector3(0f, 0.07f, 0f), new Vector3(0.09f, 0.07f, 0.09f), bamboo);
            AddPart(whisk, PrimitiveType.Cylinder, "Handle", new Vector3(0f, 0.22f, 0f), new Vector3(0.035f, 0.09f, 0.035f), bamboo);

            var motion = rig.gameObject.AddComponent<WhiskMotion>();
            var serialized = new SerializedObject(motion);
            serialized.FindProperty("station").objectReferenceValue = station;
            serialized.FindProperty("whisk").objectReferenceValue = whisk;
            serialized.FindProperty("froth").objectReferenceValue = froth.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // The counter: player drops drinks on the front, customers queue at the back,
        // and their money piles up beside it.
        // Used for both the main counter and the drive-through window.
        private static GameObject BuildCounterPrefab(string name, Color color, DrinkRecipe[] menu, CashBill billPrefab,
            string lane, float priceMultiplier, float queueSpacing, Vector3? queueOffset = null, float queueYaw = 180f)
        {
            var bodySize = new Vector3(2.2f, 1f, 1f) * BarScale;
            GameObject root = BuildStationBody(name, color, bodySize, null);

            var cashGo = new GameObject("CashPile");
            cashGo.transform.SetParent(root.transform, false);
            cashGo.transform.localPosition = new Vector3(2f, 0f, 1.4f);
            var cashPile = cashGo.AddComponent<CashPile>();
            var serializedPile = new SerializedObject(cashPile);
            serializedPile.FindProperty("billPrefab").objectReferenceValue = billPrefab;
            serializedPile.FindProperty("valuePerBill").intValue = 5;
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
            serialized.FindProperty("maxDrinksPerCustomer").intValue = shopNumber > 1 ? 4 : 3;
            serialized.FindProperty("cashPile").objectReferenceValue = cashPile;
            serialized.FindProperty("queueStart").objectReferenceValue = queueStart;
            serialized.FindProperty("lane").stringValue = lane;
            serialized.FindProperty("priceMultiplier").floatValue = priceMultiplier;
            serialized.FindProperty("queueSpacing").floatValue = queueSpacing;
            // The drop-off spot is on the left half, so a cashier can stand behind the right half.
            serialized.FindProperty("zone.offset").vector3Value = new Vector3(-0.5f, 0f, 1.4f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AddOrderBoard(root, counter, bodySize.y);
            return SavePrefab(root, $"{PrefabFolder}/Stations/{name}.prefab");
        }

        // A bus stop to another shop: a colored tile to stand on (a ring fills while you wait)
        // and a sign behind it. The "BusStop" art slot replaces the sign.
        private static GameObject BuildTravelPrefab(string name, string targetScene, string destination, Color color)
        {
            // A small, glowing pad with sparkles circling over it, so it's easy to spot.
            var root = new GameObject(name);
            GameObject tile = AddPart(root.transform, PrimitiveType.Cube, "Tile", new Vector3(0f, 0.01f, 0f),
                new Vector3(1.4f, 0.02f, 1.4f), GetOrCreateMaterial(name, color));
            Transform fill = AddAnchor(root.transform, "Fill", new Vector3(0f, 0.025f, 0f));
            AddPart(fill, PrimitiveType.Cylinder, "Ring", Vector3.zero, new Vector3(1.3f, 0.005f, 1.3f),
                GetOrCreateMaterial("TravelFill", new Color(1f, 1f, 1f)));
            var sparkles = new Transform[3];
            Material sparkle = GetOrCreateMaterial("PadSparkle", new Color(1f, 0.97f, 0.75f));
            sparkle.EnableKeyword("_EMISSION");
            sparkle.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.5f));
            for (int i = 0; i < sparkles.Length; i++)
                sparkles[i] = AddPart(root.transform, PrimitiveType.Sphere, "Sparkle", new Vector3(0f, 0.4f, 0f), Vector3.one * 0.1f, sparkle).transform;
            var shine = root.AddComponent<PadShine>();
            var shineSettings = new SerializedObject(shine);
            shineSettings.FindProperty("tile").objectReferenceValue = tile.GetComponent<Renderer>();
            SerializedProperty sparkleList = shineSettings.FindProperty("sparkles");
            sparkleList.arraySize = sparkles.Length;
            for (int i = 0; i < sparkles.Length; i++) sparkleList.GetArrayElementAtIndex(i).objectReferenceValue = sparkles[i];
            shineSettings.ApplyModifiedPropertiesWithoutUndo();

            var signBase = new Vector3(0f, 0f, 1.1f);
            GameObject pole = AddPart(root.transform, PrimitiveType.Cylinder, "Pole", signBase + new Vector3(0f, 1f, 0f),
                new Vector3(0.1f, 1f, 0.1f), GetOrCreateMaterial("SignPole", new Color(0.4f, 0.4f, 0.45f)));
            GameObject sign = AddPart(root.transform, PrimitiveType.Cylinder, "Sign", signBase + new Vector3(0f, 2.1f, 0f),
                new Vector3(0.7f, 0.03f, 0.7f), GetOrCreateMaterial(name, color));
            sign.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            if (ArtLibrary.Attach("BusStop", root.transform, new Vector3(1.6f, 2.4f, 0.6f), ArtLibrary.Fit.Height, signBase) != null)
            {
                ArtLibrary.HideGrayBox(pole, sign);
            }

            // The same yellow outlined label as the stations.
            NameTag.Add(root, "To " + destination);

            var travel = root.AddComponent<ShopTravel>();
            var serialized = new SerializedObject(travel);
            serialized.FindProperty("targetScene").stringValue = targetScene;
            serialized.FindProperty("fillRing").objectReferenceValue = fill;
            serialized.FindProperty("halfSize").floatValue = 0.7f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/{name}.prefab");
        }

        // A decoration you can buy: it only makes the shop nicer (and raises the rating).
        // Solid, so the player walks around it; the art slot of the same name replaces the gray box.
        // Decor bought on a pad: a solid box you can't walk through (the gray box until the art kit
        // gives it a model) and a ShopDecor with its bonuses.
        private static GameObject BuildShopDecorPrefab(string name, string shownName, Color color, Vector3 solidSize,
            Vector3 artSize, ArtLibrary.Fit fit, float patience, float flow, float happiness)
        {
            var root = new GameObject(name);
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, solidSize.y * 0.5f, 0f);
            body.transform.localScale = solidSize;
            body.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(name, color);
            if (ArtLibrary.Attach(name, root.transform, artSize, fit) != null) ArtLibrary.HideGrayBox(body);

            var decor = root.AddComponent<ShopDecor>();
            var serialized = new SerializedObject(decor);
            serialized.FindProperty("shownName").stringValue = shownName;
            serialized.FindProperty("patience").floatValue = patience;
            serialized.FindProperty("flow").floatValue = flow;
            serialized.FindProperty("happiness").floatValue = happiness;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, $"{PrefabFolder}/Decor/{name}.prefab");
        }

        private static GameObject BuildDecorPrefab(string name, Color color, Vector3 size)
        {
            GameObject root = BuildStationBody(name, color, size, null);
            return SavePrefab(root, $"{PrefabFolder}/Decor/{name}.prefab");
        }

        // A purple desk. Tapping it opens the upgrade menu.
        private static GameObject BuildUpgradeDeskPrefab()
        {
            GameObject root = BuildStationBody("UpgradeDesk", new Color(0.58f, 0.44f, 0.86f), new Vector3(1.6f, 1f, 1f),
                new Color(1f, 0.85f, 0.3f));
            // Tapping the desk opens the upgrade menu (like tapping a counter opens its prices).
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
            serializedPile.FindProperty("valuePerBill").intValue = 2;
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
            AddCharacterArt(StaffSlot("Cashier"), root, body);
            Transform hands = AddAnchor(root.transform, "Hands", new Vector3(0f, 0.9f, 0.4f));
            var cashier = root.AddComponent<Cashier>();
            SetReference(cashier, "hands", hands);
            var serialized = new SerializedObject(cashier);
            serialized.FindProperty("lane").stringValue = lane;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AddPayLight(root, cashier);
            return SavePrefab(root, $"{PrefabFolder}/{prefabName}.prefab");
        }

        // A round light above a worker's head: green while paid, red while their wages are owed.
        private static void AddPayLight(GameObject root, Behaviour work)
        {
            GameObject light = AddPart(root.transform, PrimitiveType.Sphere, "PayLight", new Vector3(0f, 2.6f, 0f),
                Vector3.one * 0.26f, GetOrCreateMaterial("MoodLight", Color.white));
            var status = root.AddComponent<PayStatus>();
            SetReference(status, "statusLight", light.GetComponent<Renderer>());
            SetReference(status, "work", work);
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
            AddCharacterArt(StaffSlot("Barista"), root, body);

            Transform hands = AddAnchor(root.transform, "HandStack", new Vector3(0f, 0.69f, 0.5f)); // the tray rests on their hands
            var carrier = root.AddComponent<ItemCarrier>();
            var serialized = new SerializedObject(carrier);
            serialized.FindProperty("stack.anchor").objectReferenceValue = hands;
            serialized.FindProperty("stack.capacity").intValue = 1;
            // Drinks ride side by side on a tray.
            SetStackGrid(serialized, "stack", 3, 2, TraySpacing, TrayDrinkSize);
            serialized.FindProperty("tray").objectReferenceValue = AddTray(hands, shopNumber > 1);
            serialized.FindProperty("usesCarryUpgrade").boolValue = true; // Carry More lets baristas carry more too
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AddPayLight(root, root.AddComponent<Barista>());
            return SavePrefab(root, $"{PrefabFolder}/Barista.prefab");
        }

        // A stocker (Matcha House) who carries crates of supplies from the dock to the bars, one at a time.
        private static GameObject BuildStockerPrefab()
        {
            var root = new GameObject("Stocker");
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Stocker", new Color(0.86f, 0.6f, 0.32f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("RunnerApron", new Color(0.98f, 0.95f, 0.85f)));
            // Their own art slot if the Art Set has one, otherwise they look like the cleaner.
            AddCharacterArt(ArtLibrary.Load().Get(StaffSlot("Stocker")) != null ? StaffSlot("Stocker") : StaffSlot("Cleaner"), root, body);

            Transform hands = AddAnchor(root.transform, "HandStack", new Vector3(0f, 0.69f, 0.48f)); // held in both hands
            var carrier = root.AddComponent<ItemCarrier>();
            var serialized = new SerializedObject(carrier);
            serialized.FindProperty("stack.anchor").objectReferenceValue = hands;
            serialized.FindProperty("stack.capacity").intValue = 1;
            serialized.FindProperty("stack.itemScale").floatValue = TrayDrinkSize;
            serialized.FindProperty("usesCarryUpgrade").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AddPayLight(root, root.AddComponent<Stocker>());
            return SavePrefab(root, $"{PrefabFolder}/Stocker.prefab");
        }

        // A cleaner who clears cups off the tables and collects what seated customers left.
        private static GameObject BuildCleanerPrefab()
        {
            var root = new GameObject("Cleaner");
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Cleaner", new Color(0.45f, 0.75f, 0.85f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("RunnerApron", new Color(0.98f, 0.95f, 0.85f)));
            AddCharacterArt(StaffSlot("Cleaner"), root, body);
            Transform hands = AddAnchor(root.transform, "Hands", new Vector3(0f, 0.9f, 0.4f));
            var cleaner = root.AddComponent<Cleaner>();
            SetReference(cleaner, "hands", hands);
            AddPayLight(root, cleaner);
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

            var customer = root.AddComponent<Customer>();
            var serialized = new SerializedObject(customer);
            serialized.FindProperty("drinkHoldPosition").vector3Value = new Vector3(0f, 0.62f, 0.48f); // in their hands
            serialized.FindProperty("orderBubble").objectReferenceValue = bubble;
            serialized.FindProperty("moodLight").objectReferenceValue = AddMoodLight(bubble);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Customer.prefab").GetComponent<Customer>();
        }

        // A round light beside the order bubble that shows the customer's mood (its color is set at runtime).
        private static Renderer AddMoodLight(Transform bubble)
        {
            return AddPart(bubble, PrimitiveType.Sphere, "MoodLight", new Vector3(0.7f, 0.3f, 0f),
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
