using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Builds a matching set of cute, pastel bubble tea shop models out of simple shapes and puts
    /// them in the art slots: tea urns, a tapioca topping bar, a cup sealer, a fruit bar, a register counter,
    /// chibi characters in aprons, a toy-like car, boba cups, plants, a menu board and a giant boba
    /// cup statue. It also recolors the floor and walls to match.
    ///
    /// Run Bubble Tea > Art > Build Boba Art Kit, then Bubble Tea > Build Prototype Scene.
    /// Change the colors in <see cref="Palette"/> and run it again to restyle everything at once.
    /// Any slot can still be swapped for another model afterwards.
    /// </summary>
    public static class BobaArtKit
    {
        private const string KitFolder = "Assets/_Game/Art/BobaKit";
        private const string MaterialFolder = KitFolder + "/Materials";
        private const string MeshFolder = KitFolder + "/Meshes";

        /// <summary>The kit's colors: warm honey wood with soft pastel accents, for a cozy café.</summary>
        private static class Palette
        {
            public static readonly Color Cream = Hex("FFF3E3");
            public static readonly Color Blush = Hex("F6B8C4");
            public static readonly Color DeepPink = Hex("E9839A");
            // Taiwanese tea house accents: lacquer red, lantern gold and jade.
            public static readonly Color TeaRed = Hex("C8433A");
            public static readonly Color Jade = Hex("4E9C7E");
            // Matcha House: matcha green uniforms and bars, hojicha brown, yuzu yellow.
            public static readonly Color MatchaGreen = Hex("5E8C3A");
            public static readonly Color Matcha = Hex("8DB255");
            public static readonly Color Hojicha = Hex("9A6A44");
            public static readonly Color Sencha = Hex("5E9E3E");
            public static readonly Color Tenju = Hex("1F3D1A");
            public static readonly Color Stone = Hex("A7A39C");
            public static readonly Color Yuzu = Hex("F7D84A");
            public static readonly Color Ceramic = Hex("F4EFE4");
            public static readonly Color Bamboo = Hex("D9C48A");          // dried bamboo (whisks, crates)
            public static readonly Color BambooGreen = Hex("7DB65A");     // living bamboo canes
            public static readonly Color DarkBambooGreen = Hex("4F8A3C");
            public static readonly Color Mint = Hex("A9E2CC");
            public static readonly Color DeepMint = Hex("6CC4A6");
            public static readonly Color Lavender = Hex("C7B3E8");
            public static readonly Color Peach = Hex("FFC9A3");
            public static readonly Color Butter = Hex("FFE39A");
            public static readonly Color Wood = Hex("E3A26C");      // warm honey wood for cabinets
            public static readonly Color LightWood = Hex("F0BF8C"); // worktops
            public static readonly Color PanelWood = Hex("C98450"); // carved door panels
            public static readonly Color DarkWood = Hex("6E4636");
            public static readonly Color Steel = Hex("D9DEE3");
            public static readonly Color Charcoal = Hex("3B3540");
            public static readonly Color Glass = Hex("DDF3F7");
            public static readonly Color Screen = Hex("8FE3D2");
            public static readonly Color Gold = Hex("F2C35B");

            public static readonly Color BlackTea = Hex("7A4A2E");
            public static readonly Color GreenTea = Hex("A9C46C");
            public static readonly Color Oolong = Hex("D19A52");
            public static readonly Color MilkTea = Hex("D8B48E");
            public static readonly Color Taro = Hex("A98BD6");
            public static readonly Color Pearl = Hex("3A2420");
            public static readonly Color GoldenPearl = Hex("E0A23B");
            public static readonly Color Jelly = Hex("F4F1E6");
            public static readonly Color RedBean = Hex("8E3B3B");
            public static readonly Color Mango = Hex("FFB43A");
            public static readonly Color Strawberry = Hex("F0525F");
            public static readonly Color Kiwi = Hex("8CC63F");
            public static readonly Color Lemon = Hex("FFE45C");

            public static readonly Color Skin = Hex("FFDCC2");
            public static readonly Color Cheek = Hex("FFA9B4");
            public static readonly Color Hair = Hex("4A3226");
            public static readonly Color Leaf = Hex("6DBB78");
            public static readonly Color DarkLeaf = Hex("4E9B5E");
            public static readonly Color Terracotta = Hex("E59A73");
            public static readonly Color Chalk = Hex("3E4A47");
            public static readonly Color Tire = Hex("44404A");

            public static readonly Color Floor = Hex("F3E2CC");
            public static readonly Color Wall = Hex("F7C6CF");
            public static readonly Color ExpansionFloor = Hex("DCEFE4");

            // The Matcha House (second shop): soft greens instead of pink.
            public static readonly Color Shop2Floor = Hex("EEF3DF");
            public static readonly Color Shop2Wall = Hex("B9D9A8");
            public static readonly Color Shop2ExpansionFloor = Hex("FBE3E8");
        }

        private const float CounterHeight = 1f;
        private static readonly Dictionary<string, Material> Materials = new();

        [MenuItem("Bubble Tea/Art/Build Boba Art Kit")]
        public static void BuildKit()
        {
            Materials.Clear();
            EnsureFolder(KitFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);

            var models = new Dictionary<string, GameObject>
            {
                { "Counter", Save("Counter", BuildCounter(Palette.Blush, Palette.Cream)) },
                { "DriveThru", Save("DriveThruCounter", BuildDriveThru()) },
                { "TeaBrewer", Save("TeaBar", BuildTeaBar()) },
                { "ToppingBar", Save("ToppingBar", BuildToppingBar()) },
                { "TaroStation", Save("TaroSealer", BuildTaroStation()) },
                { "FruitBar", Save("FruitBar", BuildFruitBar()) },
                { "UpgradeDesk", Save("UpgradeDesk", BuildUpgradeDesk()) },
                { "Cup", Save("BobaCup", BuildCup()) },
                { "Player", Save("Barista", BuildCharacter(Palette.TeaRed, Palette.Cream, CharacterStyle.Buns)) },
                // Customers and staff get a random look (man or woman, colors) when they appear.
                { "Customer", Save("Customer", BuildCharacter(Palette.Butter, null, CharacterStyle.Bob, randomLook: true)) },
                { "Barista", Save("BaristaHelper", BuildCharacter(Palette.TeaRed, Palette.Cream, CharacterStyle.Uniform, randomLook: true)) },
                { "Cashier", Save("Cashier", BuildCharacter(Palette.TeaRed, Palette.Cream, CharacterStyle.Uniform, randomLook: true)) },
                { "Cleaner", Save("Cleaner", BuildCharacter(Palette.TeaRed, Palette.Cream, CharacterStyle.Uniform, randomLook: true)) },
                { "Car", Save("Car", BuildCar()) },
                // The Matcha House: its own bars, and staff in green uniforms.
                { "MatchaPlayer", Save("MatchaPlayer", BuildCharacter(Palette.MatchaGreen, Palette.Cream, CharacterStyle.Buns)) },
                { "MatchaBarista", Save("MatchaBarista", BuildCharacter(Palette.MatchaGreen, Palette.Cream, CharacterStyle.Uniform, randomLook: true)) },
                { "MatchaCashier", Save("MatchaCashier", BuildCharacter(Palette.MatchaGreen, Palette.Cream, CharacterStyle.Uniform, randomLook: true)) },
                { "MatchaCleaner", Save("MatchaCleaner", BuildCharacter(Palette.MatchaGreen, Palette.Cream, CharacterStyle.Uniform, randomLook: true)) },
                { "WhiskBar", Save("WhiskBar", BuildWhiskBar()) },
                { "FoamBar", Save("FoamBar", BuildFoamBar()) },
                { "HojichaBar", Save("HojichaBar", BuildHojichaBar()) },
                { "StrawberryBar", Save("StrawberryBar", BuildStrawberryBar()) },
                { "YuzuBar", Save("YuzuBar", BuildYuzuBar()) },
                { "SenchaBar", Save("SenchaBar", BuildSenchaBar()) },
                { "TenjuBar", Save("TenjuBar", BuildTenjuBar()) },
            };
            GameObject plant = Save("Plant", BuildPlant());
            models["Planter"] = plant;
            models["MenuBoard"] = Save("MenuBoard", BuildMenuBoard());
            models["Statue"] = Save("BobaStatue", BuildBobaStatue());
            models["Table"] = Save("CafeTable", BuildCafeTable());
            models["BusStop"] = Save("BusStop", BuildBusStop());

            // Free potted bamboo in the corners; the menu board and statue are bought on pads.
            var decorations = new List<GameObject> { plant };

            ArtSet set = ArtLibrary.Load();
            set.EnsureSlots();
            foreach (ArtSlot slot in set.Slots)
            {
                if (slot == null || !models.TryGetValue(slot.name, out GameObject model)) continue;
                slot.model = model;
                slot.rotationY = 0f;
                slot.sizeMultiplier = 1f;
                slot.offset = Vector3.zero;
            }
            set.SetDecorations(decorations);
            EditorUtility.SetDirty(set);

            RecolorShop();
            AssetDatabase.SaveAssets();
            Debug.Log("Bubble Tea: Boba Art Kit built in " + KitFolder +
                      " and put in every art slot. Now click Bubble Tea > Build Prototype Scene to see it.");
        }

        // ---------- Stations ----------

        // A cozy wooden cabinet: honey wood with carved door panels, a band in the station's color
        // (body) with a thin trim line, a lighter worktop and a dark kick plate.
        // Footprint is width x depth, so it fits the gray box exactly.
        private static Transform CounterBase(string name, float width, float depth, Color body, Color trim)
        {
            var root = new GameObject(name).transform;
            float h = CounterHeight;
            Box(root, "Kick", new Vector3(0f, 0.05f, 0f), new Vector3(width - 0.12f, 0.1f, depth - 0.12f), Palette.DarkWood);
            Box(root, "Cabinet", new Vector3(0f, (h - 0.07f) * 0.5f + 0.05f, 0f), new Vector3(width - 0.06f, h - 0.17f, depth - 0.06f), Palette.Wood);
            Box(root, "Band", new Vector3(0f, h * 0.78f, 0f), new Vector3(width - 0.03f, 0.12f, depth - 0.03f), body);
            Box(root, "Trim", new Vector3(0f, h * 0.71f, 0f), new Vector3(width - 0.02f, 0.025f, depth - 0.02f), trim);
            Box(root, "Top", new Vector3(0f, h - 0.04f, 0f), new Vector3(width + 0.04f, 0.08f, depth + 0.04f), Palette.LightWood);

            // Carved door panels on each long side (a frame with a raised middle), so it reads as furniture.
            int doors = Mathf.Max(2, Mathf.RoundToInt(width / 0.55f));
            float doorWidth = (width - 0.12f) / doors;
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * (depth * 0.5f - 0.02f);
                for (int door = 0; door < doors; door++)
                {
                    float x = -width * 0.5f + 0.06f + doorWidth * (door + 0.5f);
                    Box(root, "Door", new Vector3(x, h * 0.37f, z), new Vector3(doorWidth - 0.06f, h * 0.5f, 0.03f), Palette.PanelWood);
                    Box(root, "DoorPanel", new Vector3(x, h * 0.37f, z + side * 0.015f),
                        new Vector3(doorWidth - 0.16f, h * 0.38f, 0.02f), Palette.Wood);
                }
            }
            return root;
        }

        private static GameObject BuildCounter(Color body, Color trim)
        {
            Transform root = CounterBase("Counter", 2.2f, 1f, body, trim);
            float top = CounterHeight;

            // Cash register on the right (drinks are set down on the left).
            Box(root, "RegisterBase", new Vector3(0.65f, top + 0.08f, 0.05f), new Vector3(0.42f, 0.16f, 0.34f), Palette.Cream);
            Box(root, "Keys", new Vector3(0.65f, top + 0.165f, 0.13f), new Vector3(0.34f, 0.01f, 0.14f), Palette.Steel);
            Transform screen = Box(root, "Screen", new Vector3(0.65f, top + 0.3f, -0.05f), new Vector3(0.32f, 0.2f, 0.03f), Palette.Charcoal);
            screen.localRotation = Quaternion.Euler(-15f, 0f, 0f);
            Box(screen, "Glow", new Vector3(0f, 0f, 0.6f), new Vector3(0.85f, 0.8f, 0.4f), Palette.Screen, emissive: true);
            Cylinder(root, "Pole", new Vector3(0.65f, top + 0.2f, -0.08f), 0.03f, 0.12f, Palette.Steel);

            // Tip jar and a service bell.
            Cylinder(root, "TipJar", new Vector3(0.25f, top + 0.1f, 0.25f), 0.08f, 0.2f, Palette.Glass);
            Box(root, "Tips", new Vector3(0.25f, top + 0.06f, 0.25f), new Vector3(0.1f, 0.05f, 0.08f), Palette.DeepMint);
            Sphere(root, "Bell", new Vector3(0.95f, top + 0.03f, 0.32f), new Vector3(0.12f, 0.08f, 0.12f), Palette.Gold);
            return root.gameObject;
        }

        private static GameObject BuildDriveThru()
        {
            GameObject counter = BuildCounter(Palette.Mint, Palette.Blush);
            counter.name = "DriveThruCounter";
            Transform root = counter.transform;

            // A little striped awning over the window.
            for (int i = 0; i < 6; i++)
            {
                Transform stripe = Box(root, "Awning", new Vector3(-0.92f + i * 0.367f, 2.15f, -0.35f),
                    new Vector3(0.367f, 0.04f, 0.8f), i % 2 == 0 ? Palette.DeepPink : Palette.Cream);
                stripe.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            }
            Cylinder(root, "PostL", new Vector3(-1.05f, 1.55f, 0.35f), 0.04f, 1.1f, Palette.DarkWood);
            Cylinder(root, "PostR", new Vector3(1.05f, 1.55f, 0.35f), 0.04f, 1.1f, Palette.DarkWood);
            return counter;
        }

        // One large stainless steel tea brewer: a tall urn on a stand with a sight glass,
        // a front tap over a drip tray, side handles and a little control panel.
        private static GameObject BuildTeaBar()
        {
            Transform root = CounterBase("TeaBar", 1.4f, 1f, Palette.Cream, Palette.DeepPink);
            float top = CounterHeight;
            const float x = -0.12f, z = -0.15f;
            Box(root, "Stand", new Vector3(x, top + 0.04f, z), new Vector3(0.62f, 0.08f, 0.56f), Palette.Charcoal);
            Cylinder(root, "Urn", new Vector3(x, top + 0.42f, z), 0.26f, 0.68f, Palette.Steel);
            Cylinder(root, "Band", new Vector3(x, top + 0.2f, z), 0.265f, 0.04f, Color.Lerp(Palette.Steel, Palette.Charcoal, 0.25f));
            Cylinder(root, "Band", new Vector3(x, top + 0.68f, z), 0.265f, 0.04f, Color.Lerp(Palette.Steel, Palette.Charcoal, 0.25f));
            Sphere(root, "Lid", new Vector3(x, top + 0.76f, z), new Vector3(0.52f, 0.16f, 0.52f), Palette.Steel);
            Sphere(root, "Knob", new Vector3(x, top + 0.86f, z), Vector3.one * 0.08f, Palette.Charcoal);
            // Sight glass showing the tea level.
            Cylinder(root, "SightGlass", new Vector3(x + 0.16f, top + 0.42f, z + 0.21f), 0.025f, 0.4f, Palette.Glass);
            Cylinder(root, "Tea", new Vector3(x + 0.16f, top + 0.35f, z + 0.21f), 0.02f, 0.26f, Palette.BlackTea);
            // Tap and drip tray.
            Box(root, "Tap", new Vector3(x, top + 0.2f, z + 0.3f), new Vector3(0.07f, 0.07f, 0.12f), Palette.Charcoal);
            Box(root, "TapHandle", new Vector3(x, top + 0.27f, z + 0.33f), new Vector3(0.04f, 0.1f, 0.04f), Palette.Charcoal);
            Box(root, "DripTray", new Vector3(x, top + 0.02f, z + 0.38f), new Vector3(0.3f, 0.04f, 0.16f), Palette.Steel);
            // Handles on each side.
            Box(root, "HandleL", new Vector3(x - 0.3f, top + 0.55f, z), new Vector3(0.05f, 0.04f, 0.18f), Palette.Charcoal);
            Box(root, "HandleR", new Vector3(x + 0.3f, top + 0.55f, z), new Vector3(0.05f, 0.04f, 0.18f), Palette.Charcoal);
            // Control panel with a glowing display.
            Box(root, "Panel", new Vector3(x + 0.48f, top + 0.12f, z + 0.05f), new Vector3(0.24f, 0.24f, 0.2f), Palette.Steel);
            Box(root, "Display", new Vector3(x + 0.48f, top + 0.17f, z + 0.155f), new Vector3(0.14f, 0.06f, 0.01f), Palette.Screen, emissive: true);
            Sphere(root, "Button", new Vector3(x + 0.48f, top + 0.08f, z + 0.16f), new Vector3(0.05f, 0.05f, 0.02f), Palette.TeaRed);
            return root.gameObject;
        }

        // ---------- Matcha House bars ----------

        // Matcha whisking: ceramic chawan bowls, bamboo whisks on their stands and a matcha tin.
        private static GameObject BuildWhiskBar()
        {
            Transform root = CounterBase("WhiskBar", 1.4f, 1f, Palette.MatchaGreen, Palette.Cream);
            float top = CounterHeight;
            for (int i = 0; i < 2; i++)
            {
                float x = -0.4f + i * 0.34f;
                Sphere(root, "Chawan", new Vector3(x, top + 0.06f, -0.2f), new Vector3(0.26f, 0.14f, 0.26f), Palette.Ceramic);
                Cylinder(root, "Matcha", new Vector3(x, top + 0.11f, -0.2f), 0.1f, 0.01f, Palette.Matcha);
                Cylinder(root, "WhiskStand", new Vector3(x + 0.05f, top + 0.06f, -0.38f), 0.05f, 0.12f, Palette.Ceramic);
                Cylinder(root, "Whisk", new Vector3(x + 0.05f, top + 0.18f, -0.38f), 0.045f, 0.14f, Palette.Bamboo);
                Cylinder(root, "WhiskHandle", new Vector3(x + 0.05f, top + 0.3f, -0.38f), 0.018f, 0.12f, Palette.Bamboo);
            }
            Cylinder(root, "MatchaTin", new Vector3(0.38f, top + 0.12f, -0.25f), 0.09f, 0.24f, Palette.MatchaGreen);
            Cylinder(root, "TinLid", new Vector3(0.38f, top + 0.25f, -0.25f), 0.095f, 0.03f, Palette.Gold);
            Transform scoop = Box(root, "Chashaku", new Vector3(0.15f, top + 0.015f, -0.05f), new Vector3(0.22f, 0.012f, 0.025f), Palette.Bamboo);
            scoop.localRotation = Quaternion.Euler(0f, 25f, 0f);
            return root.gameObject;
        }

        // Cream foam: a milk frother, steel pitchers and a jug of sweet cream.
        private static GameObject BuildFoamBar()
        {
            Transform root = CounterBase("FoamBar", 2f, 1f, Palette.Cream, Palette.MatchaGreen);
            float top = CounterHeight;
            Box(root, "FrotherBase", new Vector3(-0.3f, top + 0.05f, -0.22f), new Vector3(0.3f, 0.1f, 0.28f), Palette.Charcoal);
            Box(root, "FrotherBody", new Vector3(-0.3f, top + 0.3f, -0.32f), new Vector3(0.3f, 0.42f, 0.1f), Palette.Steel);
            Cylinder(root, "Wand", new Vector3(-0.3f, top + 0.3f, -0.22f), 0.012f, 0.2f, Palette.Steel);
            Cylinder(root, "Pitcher", new Vector3(-0.3f, top + 0.16f, -0.18f), 0.06f, 0.14f, Palette.Steel);
            for (int i = 0; i < 3; i++)
            {
                Cylinder(root, "Pitcher", new Vector3(0.05f + i * 0.16f, top + 0.08f, -0.3f), 0.06f, 0.16f, Palette.Steel);
            }
            Cylinder(root, "CreamJug", new Vector3(0.6f, top + 0.12f, -0.25f), 0.09f, 0.24f, Palette.Ceramic);
            Sphere(root, "Cream", new Vector3(0.6f, top + 0.25f, -0.25f), new Vector3(0.16f, 0.06f, 0.16f), Palette.Jelly);
            return root.gameObject;
        }

        // Hojicha: a clay roasting pan (houroku) on a little burner, and a cast-iron kettle.
        private static GameObject BuildHojichaBar()
        {
            Transform root = CounterBase("HojichaBar", 1.4f, 1f, Palette.Hojicha, Palette.Cream);
            float top = CounterHeight;
            Box(root, "Burner", new Vector3(-0.3f, top + 0.05f, -0.2f), new Vector3(0.36f, 0.1f, 0.36f), Palette.Charcoal);
            Cylinder(root, "Flame", new Vector3(-0.3f, top + 0.11f, -0.2f), 0.1f, 0.02f, Palette.Mango);
            Sphere(root, "Houroku", new Vector3(-0.3f, top + 0.17f, -0.2f), new Vector3(0.34f, 0.1f, 0.34f), Palette.Terracotta);
            Sphere(root, "Leaves", new Vector3(-0.3f, top + 0.2f, -0.2f), new Vector3(0.22f, 0.04f, 0.22f), Palette.Hojicha);
            Transform handle = Cylinder(root, "Handle", new Vector3(-0.08f, top + 0.2f, -0.2f), 0.03f, 0.2f, Palette.Terracotta);
            handle.localRotation = Quaternion.Euler(0f, 0f, 80f);
            Sphere(root, "Kettle", new Vector3(0.3f, top + 0.14f, -0.22f), new Vector3(0.3f, 0.26f, 0.3f), Palette.Charcoal);
            Transform spout = Cylinder(root, "Spout", new Vector3(0.47f, top + 0.18f, -0.22f), 0.025f, 0.14f, Palette.Charcoal);
            spout.localRotation = Quaternion.Euler(0f, 0f, -50f);
            Transform bail = Box(root, "KettleHandle", new Vector3(0.3f, top + 0.33f, -0.22f), new Vector3(0.24f, 0.02f, 0.03f), Palette.DarkWood);
            bail.localRotation = Quaternion.Euler(0f, 0f, 0f);
            return root.gameObject;
        }

        // Strawberry matcha: a bowl of strawberries, a jar of strawberry milk and a muddler.
        private static GameObject BuildStrawberryBar()
        {
            Transform root = CounterBase("StrawberryBar", 2f, 1f, Palette.Blush, Palette.MatchaGreen);
            float top = CounterHeight;
            Cylinder(root, "Bowl", new Vector3(-0.1f, top + 0.05f, -0.24f), 0.24f, 0.1f, Palette.Ceramic);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                float radius = i == 6 ? 0f : 0.13f;
                Sphere(root, "Strawberry", new Vector3(-0.1f + Mathf.Cos(angle) * radius, top + (i == 6 ? 0.17f : 0.12f), -0.24f + Mathf.Sin(angle) * radius),
                    new Vector3(0.1f, 0.12f, 0.1f), Palette.Strawberry);
            }
            Cylinder(root, "MilkJar", new Vector3(0.35f, top + 0.16f, -0.28f), 0.1f, 0.32f, Palette.Glass);
            Cylinder(root, "StrawberryMilk", new Vector3(0.35f, top + 0.12f, -0.28f), 0.09f, 0.22f, Palette.Blush);
            Transform muddler = Cylinder(root, "Muddler", new Vector3(0.6f, top + 0.03f, -0.1f), 0.02f, 0.3f, Palette.DarkWood);
            muddler.localRotation = Quaternion.Euler(90f, 30f, 0f);
            return root.gameObject;
        }

        // Yuzu matcha fizz: a crate of yuzu, a juicer and a soda tap.
        private static GameObject BuildYuzuBar()
        {
            Transform root = CounterBase("YuzuBar", 1.4f, 1f, Palette.Yuzu, Palette.MatchaGreen);
            float top = CounterHeight;
            Box(root, "Crate", new Vector3(-0.32f, top + 0.08f, -0.22f), new Vector3(0.4f, 0.16f, 0.32f), Palette.Bamboo);
            for (int i = 0; i < 6; i++)
            {
                Sphere(root, "Yuzu", new Vector3(-0.44f + (i % 3) * 0.12f, top + 0.19f, -0.28f + (i / 3) * 0.12f), Vector3.one * 0.11f, Palette.Yuzu);
            }
            Cylinder(root, "Juicer", new Vector3(0.05f, top + 0.05f, -0.2f), 0.1f, 0.1f, Palette.Ceramic);
            Sphere(root, "Ridge", new Vector3(0.05f, top + 0.12f, -0.2f), new Vector3(0.09f, 0.1f, 0.09f), Palette.Ceramic);
            Box(root, "SodaTower", new Vector3(0.38f, top + 0.3f, -0.3f), new Vector3(0.16f, 0.6f, 0.14f), Palette.Steel);
            Box(root, "SodaTap", new Vector3(0.38f, top + 0.46f, -0.18f), new Vector3(0.05f, 0.05f, 0.12f), Palette.Charcoal);
            Box(root, "Label", new Vector3(0.38f, top + 0.35f, -0.225f), new Vector3(0.12f, 0.08f, 0.01f), Palette.Matcha);
            return root.gameObject;
        }

        // Sencha: a round kyusu teapot with a side handle, tins of loose leaves, a tray of
        // little yunomi cups of green tea and a pile of fresh tea leaves.
        private static GameObject BuildSenchaBar()
        {
            Transform root = CounterBase("SenchaBar", 1.4f, 1f, Palette.Sencha, Palette.Cream);
            float top = CounterHeight;
            Sphere(root, "Kyusu", new Vector3(-0.3f, top + 0.12f, -0.22f), new Vector3(0.28f, 0.22f, 0.28f), Palette.Terracotta);
            Cylinder(root, "KyusuLid", new Vector3(-0.3f, top + 0.23f, -0.22f), 0.08f, 0.03f, Palette.Terracotta);
            Sphere(root, "LidKnob", new Vector3(-0.3f, top + 0.26f, -0.22f), Vector3.one * 0.04f, Palette.Terracotta);
            Transform spout = Cylinder(root, "Spout", new Vector3(-0.13f, top + 0.14f, -0.22f), 0.025f, 0.12f, Palette.Terracotta);
            spout.localRotation = Quaternion.Euler(0f, 0f, -60f);
            Transform side = Cylinder(root, "SideHandle", new Vector3(-0.3f, top + 0.13f, -0.06f), 0.03f, 0.2f, Palette.Terracotta);
            side.localRotation = Quaternion.Euler(80f, 0f, 0f);
            Box(root, "CupTray", new Vector3(0.18f, top + 0.015f, -0.12f), new Vector3(0.4f, 0.03f, 0.2f), Palette.DarkWood);
            for (int i = 0; i < 3; i++)
            {
                float x = 0.06f + i * 0.12f;
                Cylinder(root, "Yunomi", new Vector3(x, top + 0.08f, -0.12f), 0.045f, 0.1f, Palette.Ceramic);
                Cylinder(root, "Tea", new Vector3(x, top + 0.125f, -0.12f), 0.038f, 0.01f, Palette.Sencha);
            }
            for (int i = 0; i < 2; i++)
            {
                Cylinder(root, "LeafTin", new Vector3(0.22f + i * 0.2f, top + 0.12f, -0.36f), 0.07f, 0.24f, i == 0 ? Palette.Sencha : Palette.Jade);
                Cylinder(root, "TinLid", new Vector3(0.22f + i * 0.2f, top + 0.25f, -0.36f), 0.075f, 0.03f, Palette.Gold);
            }
            Sphere(root, "Leaves", new Vector3(-0.45f, top + 0.03f, 0.05f), new Vector3(0.2f, 0.05f, 0.14f), Palette.Leaf);
            return root.gameObject;
        }

        // Tenju, the finest matcha: a stone mill grinding the leaves, a dark tea bowl of thick
        // koicha and a gold-lidded tin.
        private static GameObject BuildTenjuBar()
        {
            Transform root = CounterBase("TenjuBar", 1.4f, 1f, Palette.Tenju, Palette.Gold);
            float top = CounterHeight;
            Cylinder(root, "MillBase", new Vector3(-0.3f, top + 0.04f, -0.22f), 0.2f, 0.08f, Palette.Stone);
            Cylinder(root, "MillLower", new Vector3(-0.3f, top + 0.13f, -0.22f), 0.15f, 0.1f, Palette.Stone);
            Cylinder(root, "MillUpper", new Vector3(-0.3f, top + 0.23f, -0.22f), 0.15f, 0.1f, Palette.Stone);
            Cylinder(root, "Powder", new Vector3(-0.3f, top + 0.085f, -0.22f), 0.19f, 0.01f, Palette.Tenju);
            Transform crank = Cylinder(root, "Crank", new Vector3(-0.12f, top + 0.33f, -0.22f), 0.015f, 0.14f, Palette.DarkWood);
            crank.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Box(root, "CrankArm", new Vector3(-0.2f, top + 0.26f, -0.22f), new Vector3(0.18f, 0.02f, 0.03f), Palette.DarkWood);
            Sphere(root, "Chawan", new Vector3(0.18f, top + 0.06f, -0.18f), new Vector3(0.28f, 0.15f, 0.28f), Palette.Charcoal);
            Cylinder(root, "Koicha", new Vector3(0.18f, top + 0.115f, -0.18f), 0.11f, 0.01f, Palette.Tenju);
            Cylinder(root, "GoldTin", new Vector3(0.45f, top + 0.12f, -0.3f), 0.08f, 0.24f, Palette.Tenju);
            Cylinder(root, "TinLid", new Vector3(0.45f, top + 0.25f, -0.3f), 0.085f, 0.03f, Palette.Gold);
            Transform scoop = Box(root, "Chashaku", new Vector3(0.18f, top + 0.015f, 0.05f), new Vector3(0.22f, 0.012f, 0.025f), Palette.Bamboo);
            scoop.localRotation = Quaternion.Euler(0f, -20f, 0f);
            return root.gameObject;
        }

        // The tapioca bar: a big steel pot of pearls simmering on a burner, a tall glass jar packed
        // with black pearls, a brown sugar syrup dispenser, a warming tray of fresh pearls with a
        // slotted scoop, smaller tubs of other toppings and a pearl sign on the back.
        private static GameObject BuildToppingBar()
        {
            Transform root = CounterBase("ToppingBar", 2f, 1f, Palette.Mint, Palette.Cream);
            float top = CounterHeight;
            Color brownSugar = Hex("8A4B23");
            Color pearlShine = Color.Lerp(Palette.Pearl, Palette.BlackTea, 0.35f);

            // Pearl cooker: a burner, a wide steel pot and pearls heaped above the water.
            Box(root, "Burner", new Vector3(-0.66f, top + 0.04f, -0.24f), new Vector3(0.44f, 0.08f, 0.42f), Palette.Charcoal);
            Cylinder(root, "Flame", new Vector3(-0.66f, top + 0.085f, -0.24f), 0.14f, 0.01f, Palette.Mango);
            Cylinder(root, "Pot", new Vector3(-0.66f, top + 0.24f, -0.24f), 0.2f, 0.3f, Palette.Steel);
            Cylinder(root, "PotRim", new Vector3(-0.66f, top + 0.39f, -0.24f), 0.21f, 0.025f, Color.Lerp(Palette.Steel, Palette.Charcoal, 0.3f));
            Cylinder(root, "Syrup", new Vector3(-0.66f, top + 0.375f, -0.24f), 0.185f, 0.01f, brownSugar);
            Box(root, "PotHandleL", new Vector3(-0.89f, top + 0.33f, -0.24f), new Vector3(0.06f, 0.03f, 0.1f), Palette.Charcoal);
            Box(root, "PotHandleR", new Vector3(-0.43f, top + 0.33f, -0.24f), new Vector3(0.06f, 0.03f, 0.1f), Palette.Charcoal);
            for (int i = 0; i < 14; i++)
            {
                float angle = i * 2.4f;
                float radius = 0.03f + (i % 5) * 0.03f;
                Sphere(root, "Pearl", new Vector3(-0.66f + Mathf.Cos(angle) * radius, top + 0.39f + (i % 3) * 0.015f, -0.24f + Mathf.Sin(angle) * radius),
                    Vector3.one * 0.055f, i % 4 == 0 ? pearlShine : Palette.Pearl);
            }
            // Steam rising from the pot.
            for (int i = 0; i < 3; i++)
            {
                Transform puff = Sphere(root, "Steam", new Vector3(-0.66f + (i - 1) * 0.06f, top + 0.5f + i * 0.09f, -0.26f),
                    Vector3.one * (0.12f + i * 0.03f), Color.white);
                puff.GetComponent<Renderer>().sharedMaterial = GetClearMaterial(Color.white, 0.45f);
            }

            // A tall glass jar packed with black tapioca pearls.
            Transform jar = Cylinder(root, "PearlJar", new Vector3(-0.17f, top + 0.27f, -0.3f), 0.15f, 0.54f, Palette.Glass);
            jar.GetComponent<Renderer>().sharedMaterial = GetClearMaterial(Palette.Glass, 0.3f);
            Cylinder(root, "JarLid", new Vector3(-0.17f, top + 0.555f, -0.3f), 0.16f, 0.04f, Palette.TeaRed);
            Sphere(root, "LidKnob", new Vector3(-0.17f, top + 0.6f, -0.3f), Vector3.one * 0.06f, Palette.Gold);
            for (int layer = 0; layer < 6; layer++)
            {
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI / 3f + layer * 0.5f;
                    Sphere(root, "Pearl", new Vector3(-0.17f + Mathf.Cos(angle) * 0.085f, top + 0.05f + layer * 0.075f, -0.3f + Mathf.Sin(angle) * 0.085f),
                        Vector3.one * 0.07f, (i + layer) % 5 == 0 ? pearlShine : Palette.Pearl);
                }
            }

            // Brown sugar syrup dispenser with a pump.
            Cylinder(root, "SyrupBottle", new Vector3(0.15f, top + 0.17f, -0.34f), 0.09f, 0.34f, brownSugar);
            Cylinder(root, "PumpNeck", new Vector3(0.15f, top + 0.38f, -0.34f), 0.025f, 0.08f, Palette.Cream);
            Box(root, "PumpHead", new Vector3(0.15f, top + 0.43f, -0.3f), new Vector3(0.05f, 0.04f, 0.12f), Palette.Cream);
            Box(root, "SyrupLabel", new Vector3(0.15f, top + 0.18f, -0.25f), new Vector3(0.1f, 0.09f, 0.01f), Palette.Gold);

            // Warming tray of fresh pearls with a slotted scoop resting in it.
            Box(root, "WarmingTray", new Vector3(0.48f, top + 0.05f, -0.24f), new Vector3(0.34f, 0.1f, 0.28f), Palette.Steel);
            Box(root, "TrayPearls", new Vector3(0.48f, top + 0.1f, -0.24f), new Vector3(0.3f, 0.01f, 0.24f), brownSugar);
            for (int i = 0; i < 12; i++)
            {
                Sphere(root, "Pearl", new Vector3(0.36f + (i % 4) * 0.08f, top + 0.115f, -0.33f + (i / 4) * 0.08f), Vector3.one * 0.06f,
                    i % 3 == 0 ? pearlShine : Palette.Pearl);
            }
            Transform scoopHandle = Cylinder(root, "ScoopHandle", new Vector3(0.6f, top + 0.18f, -0.2f), 0.012f, 0.24f, Palette.Steel);
            scoopHandle.localRotation = Quaternion.Euler(0f, 0f, -55f);
            Cylinder(root, "Scoop", new Vector3(0.5f, top + 0.12f, -0.2f), 0.05f, 0.015f, Palette.Steel);

            // Small tubs of the other toppings: golden pearls, coconut jelly, red bean.
            Color[] toppings = { Palette.GoldenPearl, Palette.Jelly, Palette.RedBean };
            for (int i = 0; i < toppings.Length; i++)
            {
                float x = 0.78f;
                float z = -0.4f + i * 0.14f;
                Cylinder(root, "Tub", new Vector3(x, top + 0.05f, z), 0.06f, 0.1f, Palette.Steel);
                Cylinder(root, "Topping", new Vector3(x, top + 0.095f, z), 0.052f, 0.02f, toppings[i]);
            }

            // A cream sign on the back with a big black pearl on it.
            Box(root, "Sign", new Vector3(0.15f, top + 0.75f, -0.47f), new Vector3(0.5f, 0.32f, 0.03f), Palette.Cream);
            Box(root, "SignTrim", new Vector3(0.15f, top + 0.75f, -0.48f), new Vector3(0.56f, 0.38f, 0.02f), Palette.TeaRed);
            Sphere(root, "SignPearl", new Vector3(0.05f, top + 0.75f, -0.45f), new Vector3(0.16f, 0.16f, 0.04f), Palette.Pearl);
            Sphere(root, "SignPearl", new Vector3(0.21f, top + 0.71f, -0.45f), new Vector3(0.11f, 0.11f, 0.04f), Palette.Pearl);
            Sphere(root, "SignPearl", new Vector3(0.31f, top + 0.8f, -0.45f), new Vector3(0.08f, 0.08f, 0.04f), Palette.Pearl);
            Cylinder(root, "SignPost", new Vector3(0.15f, top + 0.3f, -0.48f), 0.015f, 0.6f, Palette.DarkWood);
            return root.gameObject;
        }

        private static GameObject BuildTaroStation()
        {
            Transform root = CounterBase("TaroSealer", 1.4f, 1f, Palette.Lavender, Palette.Cream);
            float top = CounterHeight;

            // Cup sealing machine.
            Box(root, "SealerBase", new Vector3(-0.3f, top + 0.06f, -0.15f), new Vector3(0.4f, 0.12f, 0.4f), Palette.Cream);
            Box(root, "SealerBack", new Vector3(-0.3f, top + 0.32f, -0.3f), new Vector3(0.4f, 0.5f, 0.12f), Palette.Cream);
            Box(root, "SealerHead", new Vector3(-0.3f, top + 0.5f, -0.15f), new Vector3(0.38f, 0.14f, 0.36f), Palette.Lavender);
            Box(root, "FilmRoll", new Vector3(-0.3f, top + 0.6f, -0.32f), new Vector3(0.3f, 0.08f, 0.08f), Palette.DeepPink);
            Box(root, "Display", new Vector3(-0.3f, top + 0.3f, -0.235f), new Vector3(0.16f, 0.07f, 0.01f), Palette.Screen, emissive: true);
            Cylinder(root, "CupSlot", new Vector3(-0.3f, top + 0.13f, -0.12f), 0.08f, 0.02f, Palette.Charcoal);

            // Bowl of taro paste and a couple of taro roots.
            Cylinder(root, "Bowl", new Vector3(0.28f, top + 0.06f, -0.15f), 0.17f, 0.12f, Palette.Cream);
            Sphere(root, "Paste", new Vector3(0.28f, top + 0.12f, -0.15f), new Vector3(0.3f, 0.12f, 0.3f), Palette.Taro);
            Transform taro = Sphere(root, "Taro", new Vector3(0.12f, top + 0.06f, -0.38f), new Vector3(0.16f, 0.12f, 0.12f), Palette.Taro);
            taro.localRotation = Quaternion.Euler(0f, 30f, 0f);
            Sphere(root, "Taro", new Vector3(0.36f, top + 0.06f, -0.4f), new Vector3(0.14f, 0.11f, 0.11f), Color.Lerp(Palette.Taro, Palette.DarkWood, 0.3f));
            return root.gameObject;
        }

        // The Mango Fruit Bar: a crate of whole mangoes, a cutting board with a mango cut
        // "hedgehog" style, a blender of yellow mango smoothie and a stack of cubes in a bowl.
        private static GameObject BuildFruitBar()
        {
            Transform root = CounterBase("FruitBar", 1.4f, 1f, Palette.Butter, Palette.Mango);
            float top = CounterHeight;
            Color ripe = Palette.Mango;
            Color blush = Color.Lerp(Palette.Mango, Palette.Strawberry, 0.45f);
            Color flesh = Hex("FFD23F");

            // Blender full of mango smoothie.
            Box(root, "BlenderBase", new Vector3(-0.45f, top + 0.07f, -0.24f), new Vector3(0.22f, 0.14f, 0.22f), Palette.Charcoal);
            Transform jar = Cylinder(root, "Jar", new Vector3(-0.45f, top + 0.3f, -0.24f), 0.09f, 0.32f, Palette.Glass);
            jar.GetComponent<Renderer>().sharedMaterial = GetClearMaterial(Palette.Glass, 0.35f);
            Cylinder(root, "Smoothie", new Vector3(-0.45f, top + 0.25f, -0.24f), 0.082f, 0.2f, flesh);
            Cylinder(root, "JarLid", new Vector3(-0.45f, top + 0.47f, -0.24f), 0.095f, 0.03f, Palette.Charcoal);

            // A wooden crate heaped with whole mangoes (oval, yellow-orange with a red blush, tiny stems).
            Box(root, "Crate", new Vector3(0.05f, top + 0.08f, -0.3f), new Vector3(0.44f, 0.16f, 0.3f), Palette.Bamboo);
            Box(root, "CrateSlat", new Vector3(0.05f, top + 0.1f, -0.145f), new Vector3(0.44f, 0.03f, 0.01f), Palette.DarkWood);
            for (int i = 0; i < 7; i++)
            {
                float x = -0.1f + (i % 4) * 0.1f + (i >= 4 ? 0.05f : 0f);
                float z = -0.34f + (i >= 4 ? 0.1f : 0f);
                float y = top + (i >= 4 ? 0.23f : 0.19f);
                Transform mango = Sphere(root, "Mango", new Vector3(x, y, z), new Vector3(0.1f, 0.09f, 0.14f), i % 3 == 0 ? blush : ripe);
                mango.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
                Cylinder(root, "Stem", new Vector3(x, y + 0.045f, z), 0.008f, 0.025f, Palette.DarkWood);
            }
            Sphere(root, "Leaf", new Vector3(0.1f, top + 0.29f, -0.3f), new Vector3(0.05f, 0.015f, 0.1f), Palette.Leaf);

            // Cutting board with a halved mango cut into cubes, and a knife.
            Box(root, "Board", new Vector3(0.45f, top + 0.015f, -0.2f), new Vector3(0.26f, 0.03f, 0.2f), Palette.LightWood);
            Sphere(root, "MangoHalf", new Vector3(0.42f, top + 0.06f, -0.22f), new Vector3(0.12f, 0.06f, 0.15f), flesh);
            for (int i = 0; i < 6; i++)
            {
                Box(root, "Cube", new Vector3(0.39f + (i % 2) * 0.05f, top + 0.1f, -0.27f + (i / 2) * 0.05f), Vector3.one * 0.035f, flesh);
            }
            Box(root, "Knife", new Vector3(0.53f, top + 0.035f, -0.15f), new Vector3(0.02f, 0.008f, 0.16f), Palette.Steel);
            Box(root, "KnifeHandle", new Vector3(0.53f, top + 0.04f, -0.26f), new Vector3(0.025f, 0.02f, 0.06f), Palette.DarkWood);

            // A bowl of mango cubes ready to blend.
            Cylinder(root, "Bowl", new Vector3(-0.2f, top + 0.04f, -0.08f), 0.1f, 0.08f, Palette.Ceramic);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 1.25f;
                Box(root, "Cube", new Vector3(-0.2f + Mathf.Cos(angle) * 0.04f, top + 0.1f + (i == 4 ? 0.02f : 0f), -0.08f + Mathf.Sin(angle) * 0.04f),
                    Vector3.one * 0.04f, flesh);
            }
            return root.gameObject;
        }

        private static GameObject BuildUpgradeDesk()
        {
            Transform root = CounterBase("UpgradeDesk", 1.6f, 1f, Palette.Lavender, Palette.Butter);
            float top = CounterHeight;

            // A tablet on a stand, a stack of menu cards and a tiny succulent.
            Transform tablet = Box(root, "Tablet", new Vector3(-0.2f, top + 0.18f, -0.1f), new Vector3(0.42f, 0.3f, 0.03f), Palette.Charcoal);
            tablet.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            Box(tablet, "Glow", new Vector3(0f, 0f, 0.6f), new Vector3(0.9f, 0.86f, 0.4f), Palette.Screen, emissive: true);
            Box(root, "Stand", new Vector3(-0.2f, top + 0.04f, -0.16f), new Vector3(0.16f, 0.08f, 0.12f), Palette.Steel);
            for (int i = 0; i < 3; i++)
            {
                Transform card = Box(root, "Card", new Vector3(0.35f, top + 0.012f + i * 0.02f, 0.05f), new Vector3(0.24f, 0.015f, 0.32f),
                    i % 2 == 0 ? Palette.Cream : Palette.Blush);
                card.localRotation = Quaternion.Euler(0f, i * 9f, 0f);
            }
            Cylinder(root, "Pot", new Vector3(0.5f, top + 0.05f, -0.28f), 0.06f, 0.1f, Palette.Terracotta);
            Sphere(root, "Succulent", new Vector3(0.5f, top + 0.12f, -0.28f), new Vector3(0.12f, 0.08f, 0.12f), Palette.Leaf);
            return root.gameObject;
        }

        // ---------- Cup ----------

        // A boba drink in a clear cup: the pearls sit inside at the bottom, the drink fills the cup above
        // them, and an optional "Foam" layer sits on top. The parts named "Drink", "Pearls" and "Foam" are
        // recolored per drink by the scene builder (which hides the pearls and foam a drink doesn't have).
        private static GameObject BuildCup()
        {
            var root = new GameObject("BobaCup").transform;
            Transform shell = Part(root, "Cup", Taper("CupBody", 0.085f, 0.11f, 0.28f), Vector3.zero, Palette.Glass);
            shell.GetComponent<MeshRenderer>().sharedMaterial = GetClearMaterial(Palette.Glass, 0.3f);
            Part(root, "Drink", Taper("CupDrink", 0.079f, 0.103f, 0.2f), new Vector3(0f, 0.07f, 0f), Palette.MilkTea);
            Part(root, "Foam", Taper("CupFoam", 0.1f, 0.104f, 0.05f), new Vector3(0f, 0.222f, 0f), Palette.Jelly);
            Part(root, "Rim", Taper("CupRim", 0.112f, 0.115f, 0.025f), new Vector3(0f, 0.27f, 0f), Color.white);
            Transform dome = Sphere(root, "Dome", new Vector3(0f, 0.295f, 0f), new Vector3(0.22f, 0.09f, 0.22f), Palette.Glass);
            dome.GetComponent<Renderer>().sharedMaterial = GetClearMaterial(Palette.Glass, 0.35f);

            // Pearls settled inside the bottom of the cup, seen through the clear plastic.
            var pearls = new GameObject("Pearls").transform;
            pearls.SetParent(root, false);
            for (int i = 0; i < 14; i++)
            {
                bool lower = i < 8;
                int ring = lower ? 8 : 6;
                float angle = (i % ring) * Mathf.PI * 2f / ring + (lower ? 0f : 0.4f);
                float r = lower ? 0.052f : 0.05f;
                float y = lower ? 0.024f : 0.056f;
                Sphere(pearls, "Pearl", new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r), Vector3.one * 0.04f, Palette.Pearl);
            }
            Sphere(pearls, "Pearl", new Vector3(0f, 0.024f, 0f), Vector3.one * 0.04f, Palette.Pearl);

            // A fat pink boba straw reaching down into the pearls, tipped a little.
            Transform straw = Cylinder(root, "Straw", new Vector3(0.025f, 0.27f, 0f), 0.018f, 0.42f, Palette.DeepPink);
            straw.localRotation = Quaternion.Euler(0f, 0f, -8f);
            return root.gameObject;
        }

        // ---------- Characters ----------

        // Uniform: the shop's staff uniform (red shirt, cream apron and a red cap), with a random
        // face and hair underneath.
        private enum CharacterStyle { Buns, Cap, Bob, Headband, Uniform }

        // A chibi character about 1.6 m tall, facing +Z, with an optional apron. With randomLook,
        // it has both long (woman) and short (man) hair and a CharacterLook that picks one, plus
        // random colors, when it appears; the style then only adds the cap or bag.
        private static GameObject BuildCharacter(Color shirt, Color? apron, CharacterStyle style, bool randomLook = false)
        {
            var root = new GameObject("Character").transform;

            // Legs and arms hang from pivots (hips and shoulders) so WalkBob can swing them: the feet
            // step while walking, and the arms reach forward to hold drinks or a tray.
            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0f ? "L" : "R";
                Transform leg = Pivot(root, "Leg" + s, new Vector3(side * 0.11f, 0.24f, 0f));
                Capsule(leg, "Shin" + s, new Vector3(0f, -0.09f, 0f), new Vector3(0.13f, 0.1f, 0.13f), Palette.Charcoal);
                Sphere(leg, "Foot" + s, new Vector3(0f, -0.19f, 0.03f), new Vector3(0.16f, 0.1f, 0.24f), Palette.Charcoal);
            }
            Capsule(root, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.4f, 0.44f), shirt);
            if (apron.HasValue)
            {
                Box(root, "Apron", new Vector3(0f, 0.45f, 0.21f), new Vector3(0.34f, 0.44f, 0.03f), apron.Value);
                Box(root, "Pocket", new Vector3(0f, 0.36f, 0.23f), new Vector3(0.18f, 0.1f, 0.01f), Color.Lerp(apron.Value, shirt, 0.5f));
                // The shop's logo: a little gold boba cup badge on the chest.
                Sphere(root, "Badge", new Vector3(0f, 0.58f, 0.235f), new Vector3(0.09f, 0.09f, 0.02f), Palette.Gold);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                string s = side < 0f ? "L" : "R";
                Transform shoulder = Pivot(root, "Shoulder" + s, new Vector3(side * 0.27f, 0.72f, 0.02f));
                Transform arm = Capsule(shoulder, "Arm" + s, new Vector3(side * 0.02f, -0.17f, 0f), new Vector3(0.14f, 0.2f, 0.14f), shirt);
                arm.localRotation = Quaternion.Euler(0f, 0f, side * 14f);
                Sphere(shoulder, "Hand" + s, new Vector3(side * 0.06f, -0.36f, 0.02f), Vector3.one * 0.11f, Palette.Skin);
            }

            // A big round head with dot eyes and rosy cheeks.
            Sphere(root, "Head", new Vector3(0f, 1.14f, 0f), Vector3.one * 0.62f, Palette.Skin);
            Sphere(root, "EyeL", new Vector3(-0.11f, 1.15f, 0.28f), new Vector3(0.07f, 0.09f, 0.04f), Palette.Charcoal);
            Sphere(root, "EyeR", new Vector3(0.11f, 1.15f, 0.28f), new Vector3(0.07f, 0.09f, 0.04f), Palette.Charcoal);
            Sphere(root, "CheekL", new Vector3(-0.18f, 1.07f, 0.25f), new Vector3(0.09f, 0.05f, 0.03f), Palette.Cheek);
            Sphere(root, "CheekR", new Vector3(0.18f, 1.07f, 0.25f), new Vector3(0.09f, 0.05f, 0.03f), Palette.Cheek);

            Sphere(root, "Hair", new Vector3(0f, 1.22f, -0.04f), new Vector3(0.66f, 0.56f, 0.64f), Palette.Hair);
            if (randomLook)
            {
                AddRandomLook(root, style, shirt);
                return root.gameObject;
            }
            switch (style)
            {
                case CharacterStyle.Buns:
                    Sphere(root, "BunL", new Vector3(-0.22f, 1.47f, -0.04f), Vector3.one * 0.2f, Palette.Hair);
                    Sphere(root, "BunR", new Vector3(0.22f, 1.47f, -0.04f), Vector3.one * 0.2f, Palette.Hair);
                    break;
                case CharacterStyle.Cap:
                    Sphere(root, "Cap", new Vector3(0f, 1.32f, -0.02f), new Vector3(0.64f, 0.42f, 0.64f), Palette.DeepMint);
                    Box(root, "Visor", new Vector3(0f, 1.36f, 0.3f), new Vector3(0.4f, 0.03f, 0.22f), Palette.DeepMint);
                    break;
                case CharacterStyle.Headband:
                    Sphere(root, "Band", new Vector3(0f, 1.3f, -0.01f), new Vector3(0.68f, 0.1f, 0.66f), Palette.DeepPink);
                    Sphere(root, "Bow", new Vector3(0.24f, 1.42f, 0.05f), new Vector3(0.16f, 0.1f, 0.06f), Palette.DeepPink);
                    break;
                case CharacterStyle.Bob:
                    Sphere(root, "Bob", new Vector3(0f, 1.1f, -0.08f), new Vector3(0.7f, 0.42f, 0.6f), Palette.Hair);
                    Box(root, "BagStrap", new Vector3(0f, 0.6f, 0f), new Vector3(0.06f, 0.5f, 0.47f), Palette.DarkWood);
                    Box(root, "Bag", new Vector3(0f, 0.55f, -0.27f), new Vector3(0.3f, 0.32f, 0.14f), Palette.Peach);
                    break;
            }
            return root.gameObject;
        }

        private static void AddRandomLook(Transform root, CharacterStyle style, Color uniform)
        {
            var female = new GameObject("Female").transform;
            female.SetParent(root, false);
            Sphere(female, "LongHair", new Vector3(0f, 1.0f, -0.12f), new Vector3(0.7f, 0.66f, 0.52f), Palette.Hair);
            if (style != CharacterStyle.Cap && style != CharacterStyle.Uniform) Sphere(female, "Bow", new Vector3(0.24f, 1.42f, 0.05f), new Vector3(0.16f, 0.1f, 0.06f), Palette.DeepPink);

            var male = new GameObject("Male").transform;
            male.SetParent(root, false);
            Sphere(male, "Fringe", new Vector3(0f, 1.36f, 0.1f), new Vector3(0.54f, 0.2f, 0.36f), Palette.Hair);

            switch (style)
            {
                case CharacterStyle.Cap:
                    Sphere(root, "Cap", new Vector3(0f, 1.32f, -0.02f), new Vector3(0.64f, 0.42f, 0.64f), Palette.DeepMint);
                    Box(root, "Visor", new Vector3(0f, 1.36f, 0.3f), new Vector3(0.4f, 0.03f, 0.22f), Palette.DeepMint);
                    break;
                case CharacterStyle.Bob:
                    Box(root, "BagStrap", new Vector3(0f, 0.6f, 0f), new Vector3(0.06f, 0.5f, 0.47f), Palette.DarkWood);
                    Box(root, "Bag", new Vector3(0f, 0.55f, -0.27f), new Vector3(0.3f, 0.32f, 0.14f), Palette.Peach);
                    break;
                case CharacterStyle.Uniform:
                    Sphere(root, "Cap", new Vector3(0f, 1.32f, -0.02f), new Vector3(0.64f, 0.42f, 0.64f), uniform);
                    Sphere(root, "CapBand", new Vector3(0f, 1.27f, -0.02f), new Vector3(0.66f, 0.08f, 0.66f), Palette.Gold);
                    Box(root, "Visor", new Vector3(0f, 1.36f, 0.3f), new Vector3(0.4f, 0.03f, 0.22f), uniform);
                    break;
            }

            var look = root.gameObject.AddComponent<BubbleTea.Feedback.CharacterLook>();
            var serialized = new SerializedObject(look);
            serialized.FindProperty("female").objectReferenceValue = female.gameObject;
            serialized.FindProperty("male").objectReferenceValue = male.gameObject;
            // Staff keep their uniform color; only customers get a random shirt.
            if (style == CharacterStyle.Uniform) SetRenderers(serialized, "shirt", root);
            else SetRenderers(serialized, "shirt", root, "Body", "ShoulderL/ArmL", "ShoulderR/ArmR");
            SetRenderers(serialized, "hair", root, "Hair", "Female/LongHair", "Male/Fringe");
            SetRenderers(serialized, "skin", root, "Head", "ShoulderL/HandL", "ShoulderR/HandR");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRenderers(SerializedObject serialized, string field, Transform root, params string[] paths)
        {
            SerializedProperty list = serialized.FindProperty(field);
            list.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                Transform part = root.Find(paths[i]);
                list.GetArrayElementAtIndex(i).objectReferenceValue = part != null ? part.GetComponent<Renderer>() : null;
            }
        }

        // ---------- Car ----------

        // A toy-like pastel car, 1.6 m wide and 2.8 m long, facing +Z.
        private static GameObject BuildCar()
        {
            var root = new GameObject("Car").transform;
            Color paint = Palette.Mint;
            Box(root, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(1.5f, 0.5f, 2.6f), paint);
            Box(root, "Hood", new Vector3(0f, 0.72f, 0.85f), new Vector3(1.4f, 0.08f, 0.8f), paint);
            Box(root, "Windows", new Vector3(0f, 0.98f, -0.2f), new Vector3(1.3f, 0.42f, 1.3f), Palette.Glass);
            Box(root, "Roof", new Vector3(0f, 1.22f, -0.2f), new Vector3(1.36f, 0.08f, 1.38f), Palette.Cream);
            Box(root, "Pillar", new Vector3(0f, 0.98f, -0.2f), new Vector3(1.34f, 0.42f, 0.1f), paint);
            Box(root, "BumperF", new Vector3(0f, 0.32f, 1.33f), new Vector3(1.5f, 0.14f, 0.12f), Palette.Cream);
            Box(root, "BumperB", new Vector3(0f, 0.32f, -1.33f), new Vector3(1.5f, 0.14f, 0.12f), Palette.Cream);
            Sphere(root, "LightL", new Vector3(-0.5f, 0.55f, 1.3f), new Vector3(0.22f, 0.16f, 0.08f), Palette.Butter);
            Sphere(root, "LightR", new Vector3(0.5f, 0.55f, 1.3f), new Vector3(0.22f, 0.16f, 0.08f), Palette.Butter);
            Box(root, "Stripe", new Vector3(0f, 0.5f, 0f), new Vector3(1.52f, 0.08f, 2.4f), Palette.DeepPink);
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Transform wheel = Cylinder(root, "Wheel", new Vector3(x * 0.68f, 0.3f, z * 0.85f), 0.3f, 0.24f, Palette.Tire);
                    wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Transform hub = Cylinder(root, "Hub", new Vector3(x * 0.81f, 0.3f, z * 0.85f), 0.13f, 0.02f, Palette.Cream);
                    hub.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }
            return root.gameObject;
        }

        // ---------- Decorations ----------

        // Potted bamboo: a cluster of green stalks of different heights, each with node rings and
        // a few slender leaves sprouting near the top, in a low cream pot.
        private static GameObject BuildPlant()
        {
            var root = new GameObject("Plant").transform;
            Part(root, "Pot", Taper("PlantPot", 0.2f, 0.25f, 0.3f), Vector3.zero, Palette.Cream);
            Part(root, "PotBand", Taper("PlantPotBand", 0.245f, 0.255f, 0.05f), new Vector3(0f, 0.25f, 0f), Palette.Terracotta);
            Cylinder(root, "Soil", new Vector3(0f, 0.29f, 0f), 0.23f, 0.02f, Palette.DarkWood);

            // x, z, height and lean (degrees) of each stalk.
            Vector4[] stalks =
            {
                new(0f, 0f, 1.75f, 0f), new(0.11f, 0.06f, 1.45f, 5f), new(-0.1f, 0.07f, 1.3f, -6f),
                new(0.04f, -0.12f, 1.55f, 4f), new(-0.08f, -0.08f, 1.1f, -4f),
            };
            for (int i = 0; i < stalks.Length; i++)
            {
                Vector4 s = stalks[i];
                var stalk = Pivot(root, "Stalk", new Vector3(s.x, 0.3f, s.y));
                stalk.localRotation = Quaternion.Euler(s.w, i * 67f, -s.w * 0.6f);
                Color green = i % 2 == 0 ? Palette.BambooGreen : Palette.DarkBambooGreen;
                Cylinder(stalk, "Cane", new Vector3(0f, s.z * 0.5f, 0f), 0.032f, s.z, green);
                // Node rings every 30 cm.
                for (float y = 0.3f; y < s.z - 0.05f; y += 0.3f)
                    Cylinder(stalk, "Node", new Vector3(0f, y, 0f), 0.038f, 0.025f, Palette.DarkBambooGreen);
                // Leaves near the top, fanning out to the sides.
                for (int leaf = 0; leaf < 3; leaf++)
                {
                    var stem = Pivot(stalk, "Leaf", new Vector3(0f, s.z - 0.15f - leaf * 0.22f, 0f));
                    stem.localRotation = Quaternion.Euler(0f, leaf * 120f + i * 40f, 0f) * Quaternion.Euler(0f, 0f, 62f);
                    Sphere(stem, "Blade", new Vector3(0f, 0.15f, 0f), new Vector3(0.07f, 0.3f, 0.015f),
                        leaf % 2 == 0 ? Palette.Leaf : Palette.DarkLeaf);
                }
            }
            return root.gameObject;
        }

        private static GameObject BuildMenuBoard()
        {
            var root = new GameObject("MenuBoard").transform;
            for (int side = -1; side <= 1; side += 2)
            {
                var face = new GameObject("Face").transform;
                face.SetParent(root, false);
                face.localPosition = new Vector3(0f, 0f, side * 0.18f);
                face.localRotation = Quaternion.Euler(-12f, side > 0 ? 0f : 180f, 0f);
                Box(face, "Frame", new Vector3(0f, 0.55f, 0f), new Vector3(0.66f, 1.1f, 0.04f), Palette.Wood);
                Box(face, "Chalk", new Vector3(0f, 0.6f, 0.025f), new Vector3(0.56f, 0.8f, 0.01f), Palette.Chalk);
                Color[] drinks = { Palette.MilkTea, Palette.Taro, Palette.Mango, Palette.GreenTea };
                for (int i = 0; i < drinks.Length; i++)
                {
                    float y = 0.88f - i * 0.18f;
                    Sphere(face, "Dot", new Vector3(-0.18f, y, 0.032f), new Vector3(0.09f, 0.09f, 0.01f), drinks[i]);
                    Box(face, "Line", new Vector3(0.07f, y, 0.032f), new Vector3(0.3f, 0.025f, 0.005f), Palette.Cream);
                }
            }
            return root.gameObject;
        }

        // A bus stop sign on a pole with a little bench beside it, for travelling between shops.
        private static GameObject BuildBusStop()
        {
            var root = new GameObject("BusStop").transform;
            Cylinder(root, "Pole", new Vector3(-0.5f, 1f, 0f), 0.05f, 2f, Palette.Cream);
            Cylinder(root, "Base", new Vector3(-0.5f, 0.03f, 0f), 0.16f, 0.06f, Palette.DarkWood);
            Transform sign = Cylinder(root, "Sign", new Vector3(-0.5f, 2.05f, 0f), 0.3f, 0.06f, Palette.Mint);
            sign.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Transform cup = Cylinder(root, "SignCup", new Vector3(-0.5f, 2.05f, 0.04f), 0.14f, 0.02f, Palette.Blush);
            cup.localRotation = Quaternion.Euler(90f, 0f, 0f);

            Box(root, "Seat", new Vector3(0.3f, 0.45f, 0f), new Vector3(1f, 0.08f, 0.4f), Palette.Wood);
            Box(root, "Back", new Vector3(0.3f, 0.75f, 0.17f), new Vector3(1f, 0.4f, 0.06f), Palette.Wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(root, "Leg", new Vector3(0.3f + side * 0.42f, 0.21f, 0f), new Vector3(0.07f, 0.42f, 0.32f), Palette.DarkWood);
            }
            return root.gameObject;
        }

        // A round café table with a pink chair and a mint chair, 2.6 m wide.
        // A round wooden café table on a dark pedestal, with a bentwood chair on each side.
        private static GameObject BuildCafeTable()
        {
            var root = new GameObject("CafeTable").transform;
            Cylinder(root, "Foot", new Vector3(0f, 0.025f, 0f), 0.32f, 0.05f, Palette.Charcoal);
            Cylinder(root, "Stem", new Vector3(0f, 0.38f, 0f), 0.06f, 0.66f, Palette.Charcoal);
            Cylinder(root, "Top", new Vector3(0f, 0.72f, 0f), 0.6f, 0.06f, Palette.Wood);
            Cylinder(root, "Rim", new Vector3(0f, 0.685f, 0f), 0.61f, 0.03f, Palette.PanelWood);

            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1f;
                // Seat cushion on a wooden ring, splayed legs and a curved slatted back.
                Cylinder(root, "SeatRing", new Vector3(x, 0.43f, 0f), 0.24f, 0.05f, Palette.PanelWood);
                Cylinder(root, "Seat", new Vector3(x, 0.47f, 0f), 0.22f, 0.05f, Palette.DarkWood);
                for (int leg = 0; leg < 4; leg++)
                {
                    float angle = leg * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                    Transform legPart = Cylinder(root, "Leg",
                        new Vector3(x + Mathf.Cos(angle) * 0.17f, 0.21f, Mathf.Sin(angle) * 0.17f), 0.025f, 0.43f, Palette.PanelWood);
                    legPart.localRotation = Quaternion.Euler(-Mathf.Sin(angle) * 8f, 0f, Mathf.Cos(angle) * 8f); // feet splay out
                }
                float backX = side * 1.2f;
                for (int post = -1; post <= 1; post += 2)
                {
                    Transform upright = Cylinder(root, "BackPost", new Vector3(backX, 0.72f, post * 0.17f), 0.025f, 0.52f, Palette.PanelWood);
                    upright.localRotation = Quaternion.Euler(0f, 0f, -side * 8f); // leans back
                }
                Box(root, "BackRail", new Vector3(backX + side * 0.04f, 0.95f, 0f), new Vector3(0.05f, 0.09f, 0.42f), Palette.PanelWood);
                Box(root, "BackSlat", new Vector3(backX + side * 0.02f, 0.75f, 0f), new Vector3(0.04f, 0.32f, 0.08f), Palette.PanelWood);
            }
            return root.gameObject;
        }

        // A giant milk tea cup, like the statues outside boba shops.
        private static GameObject BuildBobaStatue()
        {
            var root = new GameObject("BobaStatue").transform;
            Cylinder(root, "Plinth", new Vector3(0f, 0.06f, 0f), 0.36f, 0.12f, Palette.Blush);
            Part(root, "Cup", Taper("StatueCup", 0.26f, 0.34f, 0.85f), new Vector3(0f, 0.12f, 0f), Palette.MilkTea);
            Part(root, "PearlBand", Taper("StatueBand", 0.27f, 0.3f, 0.22f), new Vector3(0f, 0.14f, 0f), Palette.Pearl);
            Part(root, "Rim", Taper("StatueRim", 0.345f, 0.355f, 0.06f), new Vector3(0f, 0.95f, 0f), Color.white);
            Sphere(root, "Dome", new Vector3(0f, 1.01f, 0f), new Vector3(0.68f, 0.26f, 0.68f), Palette.Glass);
            Transform straw = Cylinder(root, "Straw", new Vector3(0.08f, 1.25f, 0f), 0.05f, 0.6f, Palette.DeepPink);
            straw.localRotation = Quaternion.Euler(0f, 0f, -10f);
            Sphere(root, "EyeL", new Vector3(-0.1f, 0.62f, 0.31f), new Vector3(0.07f, 0.09f, 0.04f), Palette.Charcoal);
            Sphere(root, "EyeR", new Vector3(0.1f, 0.62f, 0.31f), new Vector3(0.07f, 0.09f, 0.04f), Palette.Charcoal);
            Sphere(root, "CheekL", new Vector3(-0.18f, 0.55f, 0.3f), new Vector3(0.09f, 0.05f, 0.03f), Palette.Cheek);
            Sphere(root, "CheekR", new Vector3(0.18f, 0.55f, 0.3f), new Vector3(0.09f, 0.05f, 0.03f), Palette.Cheek);
            return root.gameObject;
        }

        // ---------- Floor and walls ----------

        private static void RecolorShop()
        {
            Recolor("Floor", Palette.Floor);
            Recolor("Wall", Palette.Wall);
            Recolor("ExpansionFloor", Palette.ExpansionFloor);
            Recolor("Shop2Floor", Palette.Shop2Floor);
            Recolor("Shop2Wall", Palette.Shop2Wall);
            Recolor("Shop2ExpansionFloor", Palette.Shop2ExpansionFloor);
        }

        private static void Recolor(string materialName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Game/Materials/{materialName}.mat");
            if (material == null) return;
            material.color = color;
            EditorUtility.SetDirty(material);
        }

        // ---------- Shape helpers ----------

        // A box with softly rounded edges (scaled like a cube, so children keep working the same way).
        private static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Color color, bool emissive = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = RoundedBox(WorldSize(parent, size));
            go.AddComponent<MeshRenderer>().sharedMaterial = GetMaterial(color, emissive);
            return go.transform;
        }

        // A box's real size, counting the scale of the boxes it sits inside.
        private static Vector3 WorldSize(Transform parent, Vector3 size)
        {
            Vector3 scale = parent != null ? parent.lossyScale : Vector3.one;
            return Vector3.Scale(size, scale);
        }

        private static readonly Dictionary<string, Mesh> RoundedBoxes = new();

        // A 1 x 1 x 1 box mesh whose edges are rounded as if it were this size, so once it's scaled
        // to that size every edge has the same soft bevel. Cached per size and saved as an asset.
        private static Mesh RoundedBox(Vector3 size)
        {
            size = new Vector3(Mathf.Max(size.x, 0.002f), Mathf.Max(size.y, 0.002f), Mathf.Max(size.z, 0.002f));
            string key = $"RoundBox_{Mathf.RoundToInt(size.x * 1000)}_{Mathf.RoundToInt(size.y * 1000)}_{Mathf.RoundToInt(size.z * 1000)}";
            if (RoundedBoxes.TryGetValue(key, out Mesh cached)) return cached;

            float radius = Mathf.Min(0.035f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.3f);
            Vector3 half = size * 0.5f;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // Each face is a grid, denser near its edges, pushed out onto the rounded shape.
            Vector3[] faceNormals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 n in faceNormals)
            {
                Vector3 u = Mathf.Abs(n.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                int start = vertices.Count;
                List<float> coords = GridCoordinates(BevelSteps.Length);
                int count = coords.Count;
                for (int j = 0; j < count; j++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 point = Vector3.Scale(n, half)
                                        + Vector3.Scale(u, half) * Along(coords[i], Vector3.Dot(Abs(u), half), radius)
                                        + Vector3.Scale(v, half) * Along(coords[j], Vector3.Dot(Abs(v), half), radius);
                        Vector3 inner = new(
                            Mathf.Clamp(point.x, -half.x + radius, half.x - radius),
                            Mathf.Clamp(point.y, -half.y + radius, half.y - radius),
                            Mathf.Clamp(point.z, -half.z + radius, half.z - radius));
                        Vector3 outward = point - inner;
                        outward = outward.sqrMagnitude > 1e-10f ? outward.normalized : n;
                        Vector3 position = inner + outward * radius;
                        vertices.Add(new Vector3(position.x / size.x, position.y / size.y, position.z / size.z));
                        normals.Add(Vector3.Scale(outward, size).normalized); // undoes the scale the object gets
                    }
                }
                for (int j = 0; j < count - 1; j++)
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        int a = start + j * count + i, b = a + 1, c = a + count, d = c + 1;
                        AddFacing(triangles, vertices, n, a, b, d);
                        AddFacing(triangles, vertices, n, a, d, c);
                    }
                }
            }

            string path = $"{MeshFolder}/{key}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh { name = key };
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            RoundedBoxes[key] = mesh;
            return mesh;
        }

        // How far into the bevel each grid line sits, from the edge (0) to where the flat face starts (1).
        private static readonly float[] BevelSteps = { 0f, 0.12f, 0.45f, 1f };

        // Grid lines across a face: -4..-1 are the bevel lines at the low edge (-4 on the edge),
        // 1..4 the ones at the high edge; the flat middle needs no lines in between.
        private static List<float> GridCoordinates(int steps)
        {
            var list = new List<float>();
            for (int i = steps; i >= 1; i--) list.Add(-i);
            for (int i = 1; i <= steps; i++) list.Add(i);
            return list;
        }

        // Turns a grid line into a fraction of the half size (-1 to 1).
        private static float Along(float coordinate, float halfSize, float radius)
        {
            int index = Mathf.Abs(Mathf.RoundToInt(coordinate)) - 1; // 0 = bevel's end ... 3 = on the edge
            float fromEdge = BevelSteps[BevelSteps.Length - 1 - index] * radius;
            float value = (halfSize - fromEdge) / halfSize;
            return coordinate < 0f ? -value : value;
        }

        private static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // Adds a triangle wound so it faces outward.
        private static void AddFacing(List<int> triangles, List<Vector3> vertices, Vector3 outward, int a, int b, int c)
        {
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (Vector3.Dot(normal, outward) >= 0f) triangles.AddRange(new[] { a, b, c });
            else triangles.AddRange(new[] { a, c, b });
        }

        // An empty joint (hip or shoulder) that the parts below it turn around.
        private static Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = position;
            return pivot;
        }

        private static Transform Sphere(Transform parent, string name, Vector3 position, Vector3 size, Color color) =>
            Primitive(parent, PrimitiveType.Sphere, name, position, size, color, false);

        // Unity's capsule is 2 units tall, so size.y is half the height.
        private static Transform Capsule(Transform parent, string name, Vector3 position, Vector3 size, Color color) =>
            Primitive(parent, PrimitiveType.Capsule, name, position, size, color, false);

        // Centered on position, with this radius and full height.
        private static Transform Cylinder(Transform parent, string name, Vector3 position, float radius, float height, Color color) =>
            Primitive(parent, PrimitiveType.Cylinder, name, position, new Vector3(radius * 2f, height * 0.5f, radius * 2f), color, false);

        private static Transform Primitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale,
            Color color, bool emissive)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = GetMaterial(color, emissive);
            return go.transform;
        }

        // A part made from a generated mesh, standing on position.
        private static Transform Part(Transform parent, string name, Mesh mesh, Vector3 position, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = GetMaterial(color, false);
            return go.transform;
        }

        // A cylinder that widens from bottomRadius to topRadius, standing on y = 0. Saved as an asset
        // so prefabs can use it; rebuilt in place on every run.
        private static Mesh Taper(string name, float bottomRadius, float topRadius, float height, int segments = 24)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();
            float slope = (bottomRadius - topRadius) / height;

            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                Vector3 normal = new Vector3(dir.x, slope, dir.z).normalized;
                vertices.Add(dir * bottomRadius);
                vertices.Add(dir * topRadius + Vector3.up * height);
                normals.Add(normal);
                normals.Add(normal);
                if (i == segments) continue;
                int b = i * 2;
                triangles.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 });
            }

            AddCap(vertices, normals, triangles, topRadius, height, Vector3.up, segments);
            AddCap(vertices, normals, triangles, bottomRadius, 0f, Vector3.down, segments);

            string path = $"{MeshFolder}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew) mesh = new Mesh { name = name };
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            if (isNew) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, float radius, float y,
            Vector3 normal, int segments)
        {
            int center = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f));
            normals.Add(normal);
            for (int i = 0; i <= segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius));
                normals.Add(normal);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = center + 1 + i;
                if (normal.y > 0f) triangles.AddRange(new[] { center, a + 1, a });
                else triangles.AddRange(new[] { center, a, a + 1 });
            }
        }

        // One material per color, shared by every model in the kit.
        private static Material GetMaterial(Color color, bool emissive)
        {
            string key = (emissive ? "Glow_" : "") + ColorUtility.ToHtmlStringRGB(color);
            if (Materials.TryGetValue(key, out Material cached)) return cached;

            string path = $"{MaterialFolder}/{key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.8f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            Materials[key] = material;
            return material;
        }

        // A see-through material (URP Lit, transparent), for the clear plastic cup.
        private static Material GetClearMaterial(Color color, float alpha)
        {
            string key = "Clear_" + ColorUtility.ToHtmlStringRGB(color) + "_" + Mathf.RoundToInt(alpha * 100f);
            if (Materials.TryGetValue(key, out Material cached)) return cached;

            string path = $"{MaterialFolder}/{key}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = new Color(color.r, color.g, color.b, alpha);
            material.SetFloat("_Surface", 1f); // transparent
            material.SetFloat("_Blend", 0f);   // alpha blending
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(material);
            Materials[key] = material;
            return material;
        }

        private static GameObject Save(string name, GameObject root)
        {
            root.name = name;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{KitFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
