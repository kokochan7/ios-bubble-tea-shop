using BubbleTea.Building;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Economy;
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
            public Customer CustomerPrefab;
        }

        internal static Content Build(CashBill billPrefab)
        {
            EnsureFolder(DataFolder, "Items");
            EnsureFolder(DataFolder, "Recipes");
            EnsureFolder(DataFolder, "Menu");
            EnsureFolder(PrefabFolder, "Items");
            EnsureFolder(PrefabFolder, "Stations");

            ItemDefinition milkTea = GetOrCreateItem("Milk Tea", new Color(0.85f, 0.70f, 0.52f), null);
            ItemDefinition bobaTea = GetOrCreateItem("Boba Milk Tea", new Color(0.88f, 0.76f, 0.60f), new Color(0.12f, 0.08f, 0.06f));

            RecipeStep brew = GetOrCreateStep("Brew Milk Tea", null, milkTea, 2f);
            RecipeStep addBoba = GetOrCreateStep("Add Boba", milkTea, bobaTea, 1.5f);

            DrinkRecipe sellMilkTea = GetOrCreateDrink("Milk Tea", milkTea, 8);
            DrinkRecipe sellBobaTea = GetOrCreateDrink("Boba Milk Tea", bobaTea, 15);

            GameObject counter = BuildCounterPrefab(new[] { sellMilkTea, sellBobaTea }, billPrefab);
            GameObject brewer = BuildStationPrefab("TeaBrewer", new Color(0.30f, 0.30f, 0.34f), new Vector3(1.4f, 1f, 1f),
                new Color(0.55f, 0.32f, 0.16f), new[] { brew }, inputCapacity: 0);
            GameObject toppingBar = BuildStationPrefab("ToppingBar", new Color(0.95f, 0.78f, 0.55f), new Vector3(2f, 1f, 1f),
                new Color(0.15f, 0.10f, 0.08f), new[] { addBoba }, inputCapacity: 4);

            return new Content
            {
                Counter = GetOrCreateBuildable("Counter", 30, counter),
                TeaBrewer = GetOrCreateBuildable("Tea Brewer", 80, brewer),
                ToppingBar = GetOrCreateBuildable("Topping Bar", 150, toppingBar),
                CustomerPrefab = BuildCustomerPrefab(),
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
            if (pearlColor.HasValue)
            {
                AddPart(root.transform, PrimitiveType.Cylinder, "Pearls", new Vector3(0f, 0.05f, 0f),
                    new Vector3(0.25f, 0.05f, 0.25f), GetOrCreateMaterial("Pearls", pearlColor.Value));
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
        private static GameObject BuildCounterPrefab(DrinkRecipe[] menu, CashBill billPrefab)
        {
            var bodySize = new Vector3(2.2f, 1f, 1f);
            GameObject root = BuildStationBody("Counter", new Color(0.62f, 0.42f, 0.28f), bodySize, null);

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
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Stations/Counter.prefab");
        }

        private static Customer BuildCustomerPrefab()
        {
            var root = new GameObject("Customer");
            GameObject body = AddPart(root.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.8f, 0f),
                new Vector3(0.7f, 0.8f, 0.7f), GetOrCreateMaterial("Customer", new Color(0.98f, 0.55f, 0.35f)));
            AddPart(body.transform, PrimitiveType.Cube, "FacingMarker", new Vector3(0f, 0.4f, 0.5f),
                new Vector3(0.4f, 0.2f, 0.2f), GetOrCreateMaterial("Customer", new Color(0.98f, 0.55f, 0.35f)));

            var customer = root.AddComponent<Customer>();
            var serialized = new SerializedObject(customer);
            serialized.FindProperty("drinkHoldPosition").vector3Value = new Vector3(0f, 0.9f, 0.55f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, $"{PrefabFolder}/Customer.prefab").GetComponent<Customer>();
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

            if (topColor.HasValue)
            {
                AddPart(root.transform, PrimitiveType.Cylinder, "Top", new Vector3(0f, bodySize.y + 0.3f, -0.15f),
                    new Vector3(0.5f, 0.3f, 0.5f), GetOrCreateMaterial(name + "Top", topColor.Value));
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
