using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Builds a matching set of cute, pastel bubble tea shop models out of simple shapes and puts
    /// them in the art slots: tea urns, a topping bar, a cup sealer, a fruit bar, a register counter,
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

        /// <summary>The kit's colors. Soft pastels, warm wood and cream, like a modern boba shop.</summary>
        private static class Palette
        {
            public static readonly Color Cream = Hex("FFF3E3");
            public static readonly Color Blush = Hex("F6B8C4");
            public static readonly Color DeepPink = Hex("E9839A");
            public static readonly Color Mint = Hex("A9E2CC");
            public static readonly Color DeepMint = Hex("6CC4A6");
            public static readonly Color Lavender = Hex("C7B3E8");
            public static readonly Color Peach = Hex("FFC9A3");
            public static readonly Color Butter = Hex("FFE39A");
            public static readonly Color Wood = Hex("D3A276");
            public static readonly Color DarkWood = Hex("9C6B48");
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
                { "Player", Save("Barista", BuildCharacter(Palette.Blush, Palette.Cream, CharacterStyle.Buns)) },
                { "Runner", Save("Runner", BuildCharacter(Palette.Mint, Palette.Cream, CharacterStyle.Cap)) },
                { "Customer", Save("Customer", BuildCharacter(Palette.Butter, null, CharacterStyle.Bob)) },
                { "Car", Save("Car", BuildCar()) },
            };
            GameObject plant = Save("Plant", BuildPlant());
            models["Planter"] = plant;
            models["MenuBoard"] = Save("MenuBoard", BuildMenuBoard());
            models["Statue"] = Save("BobaStatue", BuildBobaStatue());

            // Free plants in the corners; the menu board and statue are bought on pads.
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

        // A pastel cabinet with a wood top, a colored trim band and a darker kick plate.
        // Footprint is width x depth, so it fits the gray box exactly.
        private static Transform CounterBase(string name, float width, float depth, Color body, Color trim)
        {
            var root = new GameObject(name).transform;
            float h = CounterHeight;
            Box(root, "Kick", new Vector3(0f, 0.05f, 0f), new Vector3(width - 0.12f, 0.1f, depth - 0.12f), Palette.DarkWood);
            Box(root, "Cabinet", new Vector3(0f, (h - 0.07f) * 0.5f + 0.05f, 0f), new Vector3(width - 0.06f, h - 0.17f, depth - 0.06f), body);
            Box(root, "Trim", new Vector3(0f, h * 0.62f, 0f), new Vector3(width - 0.04f, 0.07f, depth - 0.04f), trim);
            Box(root, "Top", new Vector3(0f, h - 0.035f, 0f), new Vector3(width, 0.07f, depth), Palette.Wood);

            // Two inset door panels on each long side, so it reads as furniture.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int door = -1; door <= 1; door += 2)
                {
                    Box(root, "Door", new Vector3(door * width * 0.24f, h * 0.33f, side * (depth * 0.5f - 0.025f)),
                        new Vector3(width * 0.4f, h * 0.38f, 0.02f), Color.Lerp(body, Color.white, 0.35f));
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

        private static GameObject BuildTeaBar()
        {
            Transform root = CounterBase("TeaBar", 1.4f, 1f, Palette.Cream, Palette.DeepPink);
            float top = CounterHeight;
            Color[] teas = { Palette.BlackTea, Palette.GreenTea, Palette.Oolong };
            for (int i = 0; i < teas.Length; i++)
            {
                float x = -0.38f + i * 0.38f;
                Box(root, "UrnStand", new Vector3(x, top + 0.05f, -0.22f), new Vector3(0.28f, 0.1f, 0.28f), Palette.Steel);
                Cylinder(root, "Urn", new Vector3(x, top + 0.33f, -0.22f), 0.14f, 0.46f, Palette.Steel);
                Cylinder(root, "Label", new Vector3(x, top + 0.36f, -0.22f), 0.145f, 0.12f, teas[i]);
                Sphere(root, "Lid", new Vector3(x, top + 0.56f, -0.22f), new Vector3(0.28f, 0.1f, 0.28f), Palette.Steel);
                Sphere(root, "Knob", new Vector3(x, top + 0.62f, -0.22f), Vector3.one * 0.06f, Palette.Charcoal);
                Box(root, "Tap", new Vector3(x, top + 0.17f, -0.06f), new Vector3(0.05f, 0.05f, 0.08f), Palette.Charcoal);
            }
            return root.gameObject;
        }

        private static GameObject BuildToppingBar()
        {
            Transform root = CounterBase("ToppingBar", 2f, 1f, Palette.Mint, Palette.Cream);
            float top = CounterHeight;

            // A row of topping tubs: tapioca, golden pearls, coconut jelly, red bean, strawberry popping boba.
            Color[] toppings = { Palette.Pearl, Palette.GoldenPearl, Palette.Jelly, Palette.RedBean, Palette.Strawberry };
            for (int i = 0; i < toppings.Length; i++)
            {
                float x = -0.48f + i * 0.24f;
                Cylinder(root, "Tub", new Vector3(x, top + 0.07f, -0.22f), 0.1f, 0.14f, Palette.Steel);
                Cylinder(root, "Topping", new Vector3(x, top + 0.13f, -0.22f), 0.088f, 0.04f, toppings[i]);
                for (int p = 0; p < 4; p++)
                {
                    float angle = p * Mathf.PI * 0.5f + i;
                    Sphere(root, "Bead", new Vector3(x + Mathf.Cos(angle) * 0.04f, top + 0.16f, -0.22f + Mathf.Sin(angle) * 0.04f),
                        Vector3.one * 0.045f, toppings[i]);
                }
            }

            // Sneeze guard and a ladle.
            Transform guard = Box(root, "Guard", new Vector3(0f, top + 0.32f, -0.05f), new Vector3(1.3f, 0.32f, 0.02f), Palette.Glass);
            guard.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            Cylinder(root, "GuardPostL", new Vector3(-0.64f, top + 0.15f, -0.05f), 0.015f, 0.3f, Palette.Steel);
            Cylinder(root, "GuardPostR", new Vector3(0.64f, top + 0.15f, -0.05f), 0.015f, 0.3f, Palette.Steel);
            Transform ladle = Cylinder(root, "Ladle", new Vector3(-0.48f, top + 0.22f, -0.3f), 0.012f, 0.25f, Palette.Steel);
            ladle.localRotation = Quaternion.Euler(-25f, 0f, 0f);
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

        private static GameObject BuildFruitBar()
        {
            Transform root = CounterBase("FruitBar", 1.4f, 1f, Palette.Peach, Palette.DeepMint);
            float top = CounterHeight;

            // Blender.
            Box(root, "BlenderBase", new Vector3(-0.35f, top + 0.07f, -0.2f), new Vector3(0.22f, 0.14f, 0.22f), Palette.Charcoal);
            Cylinder(root, "Jar", new Vector3(-0.35f, top + 0.3f, -0.2f), 0.09f, 0.32f, Palette.Glass);
            Cylinder(root, "Smoothie", new Vector3(-0.35f, top + 0.22f, -0.2f), 0.085f, 0.15f, Palette.Mango);
            Cylinder(root, "JarLid", new Vector3(-0.35f, top + 0.47f, -0.2f), 0.095f, 0.03f, Palette.Charcoal);

            // Fruit bowl piled with mango, strawberries, kiwi and lemon.
            Cylinder(root, "Bowl", new Vector3(0.22f, top + 0.05f, -0.22f), 0.26f, 0.1f, Palette.Cream);
            Color[] fruit = { Palette.Mango, Palette.Strawberry, Palette.Kiwi, Palette.Lemon, Palette.Mango, Palette.Strawberry, Palette.Kiwi };
            for (int i = 0; i < fruit.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                float radius = i == fruit.Length - 1 ? 0f : 0.14f;
                float y = top + (i == fruit.Length - 1 ? 0.2f : 0.13f);
                Sphere(root, "Fruit", new Vector3(0.22f + Mathf.Cos(angle) * radius, y, -0.22f + Mathf.Sin(angle) * radius),
                    Vector3.one * 0.12f, fruit[i]);
            }
            Sphere(root, "Leaf", new Vector3(0.22f, top + 0.27f, -0.22f), new Vector3(0.06f, 0.02f, 0.1f), Palette.Leaf);
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

        // A drink-filled boba cup. The parts named "Drink" and "Pearls" are recolored per drink by the scene builder.
        private static GameObject BuildCup()
        {
            var root = new GameObject("BobaCup").transform;
            Mesh cupMesh = Taper("CupBody", 0.085f, 0.11f, 0.28f);
            Part(root, "Drink", cupMesh, new Vector3(0f, 0f, 0f), Palette.MilkTea);
            Part(root, "Rim", Taper("CupRim", 0.112f, 0.115f, 0.025f), new Vector3(0f, 0.27f, 0f), Color.white);
            Sphere(root, "Dome", new Vector3(0f, 0.295f, 0f), new Vector3(0.22f, 0.09f, 0.22f), Palette.Glass);

            // Pearls pressed against the bottom of the cup, as if seen through it.
            var pearls = new GameObject("Pearls").transform;
            pearls.SetParent(root, false);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f;
                float y = 0.035f + (i % 2) * 0.04f;
                float r = 0.085f + y * 0.09f;
                Sphere(pearls, "Pearl", new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r), Vector3.one * 0.045f, Palette.Pearl);
            }

            // A fat pink boba straw, tipped a little.
            Transform straw = Cylinder(root, "Straw", new Vector3(0.025f, 0.36f, 0f), 0.018f, 0.24f, Palette.DeepPink);
            straw.localRotation = Quaternion.Euler(0f, 0f, -8f);
            return root.gameObject;
        }

        // ---------- Characters ----------

        private enum CharacterStyle { Buns, Cap, Bob }

        // A chibi character about 1.6 m tall, facing +Z, with an optional apron.
        private static GameObject BuildCharacter(Color shirt, Color? apron, CharacterStyle style)
        {
            var root = new GameObject("Character").transform;

            Sphere(root, "FootL", new Vector3(-0.11f, 0.05f, 0.03f), new Vector3(0.16f, 0.1f, 0.24f), Palette.Charcoal);
            Sphere(root, "FootR", new Vector3(0.11f, 0.05f, 0.03f), new Vector3(0.16f, 0.1f, 0.24f), Palette.Charcoal);
            Capsule(root, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.4f, 0.44f), shirt);
            if (apron.HasValue)
            {
                Box(root, "Apron", new Vector3(0f, 0.45f, 0.21f), new Vector3(0.34f, 0.44f, 0.03f), apron.Value);
                Box(root, "Pocket", new Vector3(0f, 0.36f, 0.23f), new Vector3(0.18f, 0.1f, 0.01f), Color.Lerp(apron.Value, shirt, 0.5f));
            }
            Transform armL = Capsule(root, "ArmL", new Vector3(-0.29f, 0.55f, 0.02f), new Vector3(0.14f, 0.2f, 0.14f), shirt);
            armL.localRotation = Quaternion.Euler(0f, 0f, -14f);
            Transform armR = Capsule(root, "ArmR", new Vector3(0.29f, 0.55f, 0.02f), new Vector3(0.14f, 0.2f, 0.14f), shirt);
            armR.localRotation = Quaternion.Euler(0f, 0f, 14f);
            Sphere(root, "HandL", new Vector3(-0.33f, 0.36f, 0.04f), Vector3.one * 0.11f, Palette.Skin);
            Sphere(root, "HandR", new Vector3(0.33f, 0.36f, 0.04f), Vector3.one * 0.11f, Palette.Skin);

            // A big round head with dot eyes and rosy cheeks.
            Sphere(root, "Head", new Vector3(0f, 1.14f, 0f), Vector3.one * 0.62f, Palette.Skin);
            Sphere(root, "EyeL", new Vector3(-0.11f, 1.15f, 0.28f), new Vector3(0.07f, 0.09f, 0.04f), Palette.Charcoal);
            Sphere(root, "EyeR", new Vector3(0.11f, 1.15f, 0.28f), new Vector3(0.07f, 0.09f, 0.04f), Palette.Charcoal);
            Sphere(root, "CheekL", new Vector3(-0.18f, 1.07f, 0.25f), new Vector3(0.09f, 0.05f, 0.03f), Palette.Cheek);
            Sphere(root, "CheekR", new Vector3(0.18f, 1.07f, 0.25f), new Vector3(0.09f, 0.05f, 0.03f), Palette.Cheek);

            Sphere(root, "Hair", new Vector3(0f, 1.22f, -0.04f), new Vector3(0.66f, 0.56f, 0.64f), Palette.Hair);
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
                case CharacterStyle.Bob:
                    Sphere(root, "Bob", new Vector3(0f, 1.1f, -0.08f), new Vector3(0.7f, 0.42f, 0.6f), Palette.Hair);
                    Box(root, "BagStrap", new Vector3(0f, 0.6f, 0f), new Vector3(0.06f, 0.5f, 0.47f), Palette.DarkWood);
                    Box(root, "Bag", new Vector3(0f, 0.55f, -0.27f), new Vector3(0.3f, 0.32f, 0.14f), Palette.Peach);
                    break;
            }
            return root.gameObject;
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

        private static GameObject BuildPlant()
        {
            var root = new GameObject("Plant").transform;
            Part(root, "Pot", Taper("PlantPot", 0.16f, 0.22f, 0.36f), Vector3.zero, Palette.Cream);
            Part(root, "PotBand", Taper("PlantPotBand", 0.215f, 0.225f, 0.05f), new Vector3(0f, 0.3f, 0f), Palette.Terracotta);
            Cylinder(root, "Soil", new Vector3(0f, 0.34f, 0f), 0.2f, 0.02f, Palette.DarkWood);
            for (int i = 0; i < 7; i++)
            {
                float angle = i * 360f / 7f;
                float tilt = 35f + (i % 3) * 12f;
                var stem = new GameObject("Leaf").transform;
                stem.SetParent(root, false);
                stem.localPosition = new Vector3(0f, 0.36f, 0f);
                stem.localRotation = Quaternion.Euler(0f, angle, 0f) * Quaternion.Euler(tilt, 0f, 0f);
                Sphere(stem, "Blade", new Vector3(0f, 0.32f + (i % 2) * 0.1f, 0f), new Vector3(0.24f, 0.42f, 0.05f),
                    i % 2 == 0 ? Palette.Leaf : Palette.DarkLeaf);
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
        }

        private static void Recolor(string materialName, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Game/Materials/{materialName}.mat");
            if (material == null) return;
            material.color = color;
            EditorUtility.SetDirty(material);
        }

        // ---------- Shape helpers ----------

        private static Transform Box(Transform parent, string name, Vector3 position, Vector3 size, Color color, bool emissive = false) =>
            Primitive(parent, PrimitiveType.Cube, name, position, size, color, emissive);

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
