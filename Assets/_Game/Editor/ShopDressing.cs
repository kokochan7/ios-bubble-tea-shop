using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// The look of a shop beyond its gameplay pieces, styled like a Taiwanese tea house: wood
    /// floors and paneling, glass walls between the dining room and the kitchen, trim along the
    /// walls, lattice windows, a red and gold shop sign, a noren curtain over the front door, a
    /// gate with a tiled roof and couplet banners, paper-cut window flowers, shelves of tea tins and celadon cups, wall lamps,
    /// swing doors on the staff entrance, the front door, the drive-thru lane, and a street with trees outside. Everything is
    /// built from primitives and textures drawn in code, so nothing needs downloading.
    /// Called by <see cref="PrototypeSceneBuilder"/> each time a shop scene is built.
    /// </summary>
    internal static class ShopDressing
    {
        private const string TextureFolder = "Assets/_Game/Art/Generated";
        private const string MaterialFolder = "Assets/_Game/Materials";

        /// <summary>What changes between shops.</summary>
        internal sealed class Style
        {
            public string ShopName;
            public Color Accent;      // sign, bunting, rug
            public Color Accent2;     // second accent color
            public Color Grass;
            // Theme: the Boba Shop is a red-and-gold Taiwanese tea house; the Matcha House is a
            // calm Japanese tea room with white paper lanterns, a green noren and matcha tins.
            public Color Lantern = new(0.86f, 0.18f, 0.14f);
            public Color LanternGlow = new(0.9f, 0.22f, 0.10f);
            public Color Noren = new(0.20f, 0.26f, 0.45f);
            public Color TeaTin = new(0.76f, 0.20f, 0.17f);
            public Color SignText = new(1f, 0.90f, 0.62f);
            public Color RoofTile = new(0.22f, 0.56f, 0.46f);  // jade-glazed roof tiles on the front gate
            public Color Blossom = new(1f, 0.70f, 0.78f);      // plum blossoms
            public string Prefix;     // material name prefix, so shops don't share colors
        }

        private static Style current;

        public static void Dress(Style style, Material floor, Material expansionFloor, Material wall, GameObject expansionArea)
        {
            current = style;
            PrototypeSceneBuilder.ShopLayout layout = PrototypeSceneBuilder.Layout;
            float w = layout.HalfWidth, d = layout.HalfDepth;

            // Textures are drawn in near-white, so each material's own color still tints them.
            // Wooden planks everywhere you walk (half-meter boards), wood paneling on the walls.
            ApplyTexture(floor, PlankTexture(), new Vector2(w, d));
            ApplyTexture(expansionFloor, PlankTexture(), new Vector2((layout.LaneOuterX - w) / 2f, d));
            ApplyTexture(wall, PanelTexture(), new Vector2(w, 1f));

            var root = new GameObject("Dressing").transform;
            CreateStreet(root, style, layout);
            CreateTrim(root, style, 2f * w + 0.5f, 0f, d, 3f);
            CreateWallCaps(GameObject.Find("Walls"));
            CreateWallCaps(expansionArea);
            CreateShelves(root, layout.Lamps, d);
            CreateWindows(root, style, layout.Windows, d);
            CreateSign(root, style, d);
            CreateLamps(root, style, layout.Lamps, d);
            CreateFrontDoor(root, style, layout);
            CreateFestiveDecor(root, style, layout);
            CreateKitchenFloor(root, "KitchenFloor", -w, w, layout);
            float counterX = layout.Counter.x;
            CreatePartition(root, -w, counterX - 1.4f, layout); // the counter is 2.75 m wide
            CreatePartition(root, counterX + 1.4f, layout.StaffDoorFrom, layout);
            CreatePartition(root, layout.StaffDoorTo, w, layout);
            CreateStaffDoor(root, layout);

            if (expansionArea != null)
            {
                Transform expansion = new GameObject("Dressing").transform;
                expansion.SetParent(expansionArea.transform, false);
                float laneWall = layout.LaneWallX;
                CreateTrim(expansion, style, laneWall - w + 0.5f, (w + laneWall) / 2f, d, 3f);
                CreateWindows(expansion, style, layout.ExpansionWindows, d);
                CreateLamps(expansion, style, layout.ExpansionLamps, d);
                CreateShelves(expansion, layout.ExpansionLamps, d);
                CreateKitchenFloor(expansion, "KitchenFloorEast", w, laneWall, layout);
                CreatePartition(expansion, w, laneWall, layout);
                CreateDriveThruLane(expansion, style, layout);
            }
        }

        // ---------- Outside ----------

        private static void CreateStreet(Transform root, Style style, PrototypeSceneBuilder.ShopLayout layout)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<MeshCollider>());
            ground.transform.SetParent(root, false);
            ground.transform.localPosition = new Vector3(8f, -0.02f, 0f);
            ground.transform.localScale = new Vector3(14f, 1f, 14f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat(style.Prefix + "Grass", style.Grass, 0.05f);

            // A sidewalk along the front of the shop.
            float sidewalk = layout.SidewalkZ;
            Material cobbles = Mat("Cobbles", new Color(0.84f, 0.82f, 0.80f), 0.15f);
            ApplyTexture(cobbles, CobbleTexture(), new Vector2(35f, 2.5f)); // stones about 25 cm across
            Part(root, PrimitiveType.Cube, "Sidewalk", new Vector3(8f, -0.01f, sidewalk), new Vector3(70f, 0.02f, 5f), cobbles);
            Part(root, PrimitiveType.Cube, "Road", new Vector3(8f, -0.012f, sidewalk - 6f), new Vector3(70f, 0.02f, 7f),
                Mat("Road", new Color(0.36f, 0.36f, 0.40f), 0.15f));

            Vector3[] trees = layout.Trees;
            Material trunk = Mat("TreeTrunk", new Color(0.55f, 0.38f, 0.26f), 0.1f);
            Material green = Mat(style.Prefix + "TreeLeaves", Color.Lerp(style.Grass, new Color(0.3f, 0.65f, 0.35f), 0.5f), 0.1f);
            Material blossom = Mat(style.Prefix + "BlossomTree", current.Blossom, 0.1f);
            for (int i = 0; i < trees.Length; i++)
            {
                Material leaves = i % 2 == 0 ? blossom : green; // every other tree is a plum tree in bloom
                Transform tree = new GameObject("Tree").transform;
                tree.SetParent(root, false);
                tree.localPosition = trees[i];
                float size = 0.85f + 0.3f * Mathf.Repeat(i * 0.618f, 1f);
                Part(tree, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, 0.9f * size, 0f), new Vector3(0.35f, 0.9f, 0.35f) * size, trunk);
                Part(tree, PrimitiveType.Sphere, "Leaves", new Vector3(0f, 2.4f * size, 0f), new Vector3(2.2f, 2f, 2.2f) * size, leaves);
                Part(tree, PrimitiveType.Sphere, "Leaves Top", new Vector3(0.3f, 3.2f * size, 0.1f), new Vector3(1.4f, 1.3f, 1.4f) * size, leaves);
            }
        }

        // ---------- Walls ----------

        // A dark wood baseboard and a cream cap along the back wall, and caps on the low walls.
        private static void CreateTrim(Transform root, Style style, float length, float centerX, float wallZ, float height)
        {
            Material wood = Mat("TrimWood", new Color(0.45f, 0.29f, 0.22f), 0.3f);
            Material cream = Mat("TrimRail", new Color(0.72f, 0.48f, 0.33f), 0.3f);
            float face = wallZ - 0.02f;
            Part(root, PrimitiveType.Cube, "Baseboard", new Vector3(centerX, 0.15f, face - 0.03f), new Vector3(length - 0.6f, 0.3f, 0.08f), wood);
            Part(root, PrimitiveType.Cube, "Chair Rail", new Vector3(centerX, 1.05f, face - 0.03f), new Vector3(length - 0.6f, 0.08f, 0.06f), cream);
        }

        // A thick dark wooden cap along the top of every wall, like a cutaway dollhouse.
        private static void CreateWallCaps(GameObject walls)
        {
            if (walls == null) return;
            Material cap = Mat("WallCap", new Color(0.33f, 0.22f, 0.19f), 0.25f);
            foreach (Transform wall in walls.transform)
            {
                if (wall.GetComponent<MeshRenderer>() == null || wall.name == "Floor") continue;
                Vector3 scale = wall.localScale;
                // A child of the wall, so it hides with it (the east wall goes when the shop expands).
                GameObject part = Part(wall, PrimitiveType.Cube, "Cap", new Vector3(0f, 0.5f + 0.05f / scale.y, 0f),
                    new Vector3(1f + 0.24f / scale.x, 0.1f / scale.y, 1f + 0.24f / scale.z), cap);
                part.transform.localRotation = Quaternion.identity;
            }
        }

        // Little wooden shelves on the back wall with rows of mugs and jars.
        private static void CreateShelves(Transform root, float[] xs, float wallZ)
        {
            Material board = Mat("ShelfWood", new Color(0.62f, 0.40f, 0.27f), 0.3f);
            Material[] mugs =
            {
                Mat("MugTeal", new Color(0.62f, 0.80f, 0.70f), 0.7f),  // celadon tea cups
                Mat("MugCream", new Color(0.98f, 0.94f, 0.86f), 0.6f),
                Mat(current.Prefix + "TeaTin", current.TeaTin, 0.5f), // tea tins in the shop's color
            };
            foreach (float x in xs)
            {
                for (int row = 0; row < 2; row++)
                {
                    float y = 1.15f + row * 0.42f;
                    Part(root, PrimitiveType.Cube, "Shelf", new Vector3(x, y, wallZ - 0.16f), new Vector3(1.3f, 0.05f, 0.3f), board);
                    for (int i = 0; i < 4; i++)
                    {
                        Material mug = mugs[(i + row) % mugs.Length];
                        bool jar = mug == mugs[2];
                        Vector3 spot = new(x - 0.45f + i * 0.3f, y + (jar ? 0.13f : 0.09f), wallZ - 0.16f);
                        Part(root, PrimitiveType.Cylinder, jar ? "Tea Tin" : "Cup", spot,
                            jar ? new Vector3(0.14f, 0.1f, 0.14f) : new Vector3(0.13f, 0.065f, 0.13f), mug);
                        if (jar)
                        {
                            Part(root, PrimitiveType.Cylinder, "Tin Lid", spot + new Vector3(0f, 0.105f, 0f),
                                new Vector3(0.15f, 0.012f, 0.15f), Mat("LanternGold", new Color(0.95f, 0.75f, 0.32f), 0.7f));
                        }
                    }
                }
                // Brackets under the bottom shelf.
                for (int side = -1; side <= 1; side += 2)
                {
                    Part(root, PrimitiveType.Cube, "Bracket", new Vector3(x + side * 0.5f, 1.05f, wallZ - 0.1f),
                        new Vector3(0.05f, 0.18f, 0.16f), board);
                }
            }
        }

        private static void CreateWindows(Transform root, Style style, float[] xs, float wallZ)
        {
            Material frame = Mat("WindowFrameWood", new Color(0.42f, 0.27f, 0.21f), 0.35f);
            Material glass = Mat("WindowGlass", new Color(0.62f, 0.80f, 0.70f), 0.95f); // a hint of garden outside
            Material sill = Mat("TrimWood", new Color(0.62f, 0.43f, 0.30f), 0.3f);
            const float width = 2.4f, height = 1.3f, centerY = 1.95f;
            float z = wallZ - 0.04f;
            foreach (float x in xs)
            {
                Transform window = new GameObject("Window").transform;
                window.SetParent(root, false);
                window.localPosition = new Vector3(x, centerY, z);
                Part(window, PrimitiveType.Cube, "Glass", Vector3.zero, new Vector3(width, height, 0.02f), glass);
                Part(window, PrimitiveType.Cube, "Top", new Vector3(0f, height / 2f, -0.03f), new Vector3(width + 0.16f, 0.1f, 0.08f), frame);
                Part(window, PrimitiveType.Cube, "Bottom", new Vector3(0f, -height / 2f, -0.03f), new Vector3(width + 0.16f, 0.1f, 0.08f), frame);
                Part(window, PrimitiveType.Cube, "Left", new Vector3(-width / 2f, 0f, -0.03f), new Vector3(0.1f, height, 0.08f), frame);
                Part(window, PrimitiveType.Cube, "Right", new Vector3(width / 2f, 0f, -0.03f), new Vector3(0.1f, height, 0.08f), frame);
                Part(window, PrimitiveType.Cube, "Middle", new Vector3(0f, 0f, -0.03f), new Vector3(0.06f, height, 0.06f), frame);
                // A Chinese-style lattice of thin wooden bars over the glass.
                for (int i = 1; i < 4; i++)
                {
                    float y = -height / 2f + i * height / 4f;
                    Part(window, PrimitiveType.Cube, "Lattice", new Vector3(0f, y, -0.02f), new Vector3(width, 0.025f, 0.025f), frame);
                }
                for (int i = 1; i < 8; i++)
                {
                    if (i == 4) continue; // the middle bar is already there
                    float x2 = -width / 2f + i * width / 8f;
                    Part(window, PrimitiveType.Cube, "Lattice", new Vector3(x2, 0f, -0.02f), new Vector3(0.025f, height, 0.025f), frame);
                }
                Part(window, PrimitiveType.Cube, "Sill", new Vector3(0f, -height / 2f - 0.08f, -0.12f), new Vector3(width + 0.3f, 0.07f, 0.25f), sill);

                // Red paper-cut flowers on each pane, as at Chinese New Year.
                foreach (float side in new[] { -0.6f, 0.6f })
                {
                    GameObject cut = Part(window, PrimitiveType.Cylinder, "Paper Cut", new Vector3(side, 0.05f, -0.04f),
                        new Vector3(0.5f, 0.004f, 0.5f), PaperRed());
                    cut.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    GameObject petal = Part(window, PrimitiveType.Cube, "Paper Cut", new Vector3(side, 0.05f, -0.045f),
                        new Vector3(0.36f, 0.36f, 0.004f), PaperRed());
                    petal.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    GameObject heart = Part(window, PrimitiveType.Cylinder, "Paper Cut Center", new Vector3(side, 0.05f, -0.05f),
                        new Vector3(0.16f, 0.004f, 0.16f), Gold());
                    heart.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                }
            }
        }

        // The shop's name on a board above the back wall, facing the camera.
        private static void CreateSign(Transform root, Style style, float wallZ)
        {
            Transform sign = new GameObject("Shop Sign").transform;
            sign.SetParent(root, false);
            sign.localPosition = new Vector3(0f, 2.15f, wallZ - 0.1f);
            Part(sign, PrimitiveType.Cube, "Board", Vector3.zero, new Vector3(6.4f, 1.3f, 0.12f), Mat(style.Prefix + "Accent", style.Accent, 0.4f));
            Part(sign, PrimitiveType.Cube, "Border", new Vector3(0f, 0f, 0.02f), new Vector3(6.7f, 1.6f, 0.08f),
                Mat("LanternGold", new Color(0.95f, 0.75f, 0.32f), 0.7f));

            var label = new GameObject("Name").AddComponent<TextMesh>();
            label.transform.SetParent(sign, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.08f);
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.text = style.ShopName.ToUpperInvariant();
            label.fontSize = 96;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = 0.07f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = style.SignText;
        }

        // One red paper lantern with gold caps and a tassel, hanging with its top at position + 0.3 m.
        private static void Lantern(Transform parent, Vector3 position, float size)
        {
            Material red = Mat(current.Prefix + "Lantern", current.Lantern, 0.4f, current.LanternGlow * 0.6f);
            Material gold = Mat("LanternGold", new Color(0.95f, 0.75f, 0.32f), 0.7f);
            Material cord = Mat("Cord", new Color(0.30f, 0.20f, 0.16f), 0.2f);
            Transform lantern = new GameObject("Lantern").transform;
            lantern.SetParent(parent, false);
            lantern.localPosition = position;
            lantern.localScale = Vector3.one * size;
            Part(lantern, PrimitiveType.Cylinder, "String", new Vector3(0f, 0.38f, 0f), new Vector3(0.02f, 0.1f, 0.02f), cord);
            Part(lantern, PrimitiveType.Sphere, "Paper", Vector3.zero, new Vector3(0.46f, 0.5f, 0.46f), red);
            Part(lantern, PrimitiveType.Cylinder, "TopCap", new Vector3(0f, 0.25f, 0f), new Vector3(0.24f, 0.03f, 0.24f), gold);
            Part(lantern, PrimitiveType.Cylinder, "BottomCap", new Vector3(0f, -0.25f, 0f), new Vector3(0.24f, 0.03f, 0.24f), gold);
            Part(lantern, PrimitiveType.Cylinder, "Tassel", new Vector3(0f, -0.38f, 0f), new Vector3(0.05f, 0.1f, 0.05f), red);
        }

        // Glowing wall lamps (no real light, so they cost nothing on a phone).
        private static void CreateLamps(Transform root, Style style, float[] xs, float wallZ)
        {
            Material brass = Mat("Brass", new Color(0.85f, 0.68f, 0.35f), 0.7f);
            Material glow = Mat("LampGlow", new Color(1f, 0.92f, 0.70f), 0.5f, new Color(1f, 0.80f, 0.45f) * 1.5f);
            foreach (float x in xs)
            {
                Transform lamp = new GameObject("Wall Lamp").transform;
                lamp.SetParent(root, false);
                lamp.localPosition = new Vector3(x, 2.1f, wallZ - 0.05f);
                Part(lamp, PrimitiveType.Cube, "Plate", Vector3.zero, new Vector3(0.18f, 0.3f, 0.04f), brass);
                Part(lamp, PrimitiveType.Cube, "Arm", new Vector3(0f, 0f, -0.15f), new Vector3(0.05f, 0.05f, 0.3f), brass);
                Part(lamp, PrimitiveType.Sphere, "Bulb", new Vector3(0f, 0.05f, -0.32f), new Vector3(0.26f, 0.26f, 0.26f), glow);
            }
        }

        // ---------- Chinese decor ----------

        private static Material PaperRed() => Mat("PaperRed", new Color(0.92f, 0.16f, 0.14f), 0.2f);
        private static Material Gold() => Mat("LanternGold", new Color(0.95f, 0.75f, 0.32f), 0.7f);

        // The shop front: automatic sliding glass doors across the front doorway.
        private static void CreateFestiveDecor(Transform root, Style style, PrototypeSceneBuilder.ShopLayout layout)
        {
            CreateGlassDoor(root, layout);
        }

        // Two framed glass panels that meet in the middle of the doorway and slide apart when
        // the player or a customer comes near (see SlidingDoor). A wooden header and two posts
        // frame the opening.
        private static void CreateGlassDoor(Transform root, PrototypeSceneBuilder.ShopLayout layout)
        {
            float from = layout.DoorFrom, to = layout.DoorTo, z = -layout.HalfDepth - 0.55f;
            float center = (from + to) / 2f, half = (to - from) / 2f, height = 2.2f;
            Material frame = Mat("DoorFrameWood", new Color(0.42f, 0.27f, 0.21f), 0.35f);
            Material handle = Gold();

            var door = new GameObject("Glass Door").transform;
            door.SetParent(root, false);
            door.localPosition = new Vector3(center, 0f, z);
            Part(door, PrimitiveType.Cube, "Header", new Vector3(0f, height + 0.1f, 0f), new Vector3(2f * half + 0.4f, 0.2f, 0.2f), frame);
            Part(door, PrimitiveType.Cube, "Post", new Vector3(-half - 0.1f, height / 2f, 0f), new Vector3(0.2f, height, 0.2f), frame);
            Part(door, PrimitiveType.Cube, "Post", new Vector3(half + 0.1f, height / 2f, 0f), new Vector3(0.2f, height, 0.2f), frame);

            Transform[] panels = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var panel = new GameObject(i == 0 ? "Left Panel" : "Right Panel").transform;
                panel.SetParent(door, false);
                panel.localPosition = new Vector3(side * half / 2f, 0f, i == 0 ? 0f : -0.08f); // one slides past the other
                float w = half;
                Part(panel, PrimitiveType.Cube, "Glass", new Vector3(0f, height / 2f, 0f), new Vector3(w - 0.1f, height - 0.1f, 0.03f), Glass());
                Part(panel, PrimitiveType.Cube, "Frame Top", new Vector3(0f, height - 0.04f, 0f), new Vector3(w, 0.08f, 0.06f), frame);
                Part(panel, PrimitiveType.Cube, "Frame Bottom", new Vector3(0f, 0.06f, 0f), new Vector3(w, 0.12f, 0.06f), frame);
                Part(panel, PrimitiveType.Cube, "Frame Side", new Vector3(-w / 2f + 0.04f, height / 2f, 0f), new Vector3(0.08f, height, 0.06f), frame);
                Part(panel, PrimitiveType.Cube, "Frame Side", new Vector3(w / 2f - 0.04f, height / 2f, 0f), new Vector3(0.08f, height, 0.06f), frame);
                // A long gold pull handle near the middle edge.
                Part(panel, PrimitiveType.Cube, "Handle", new Vector3(-side * (w / 2f - 0.25f), 1.05f, -0.06f), new Vector3(0.04f, 0.7f, 0.04f), handle);
                panels[i] = panel;
            }

            var sliding = door.gameObject.AddComponent<BubbleTea.Building.SlidingDoor>();
            var serialized = new SerializedObject(sliding);
            serialized.FindProperty("left").objectReferenceValue = panels[0];
            serialized.FindProperty("right").objectReferenceValue = panels[1];
            serialized.FindProperty("slide").floatValue = half - 0.15f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Floor plan ----------

        // Waist-high glass between the dining room and the kitchen, on a wooden base. Solid, so the
        // player goes round through the staff door.
        private static void CreatePartition(Transform root, float fromX, float toX, PrototypeSceneBuilder.ShopLayout layout)
        {
            if (toX - fromX < 0.2f) return;
            float z = layout.PartitionZ;
            float center = (fromX + toX) / 2f, length = toX - fromX;
            var wall = new GameObject("Glass Wall").transform;
            wall.SetParent(root, false);
            Material wood = Mat("TrimWood", new Color(0.62f, 0.43f, 0.30f), 0.3f);

            Part(wall, PrimitiveType.Cube, "Base", new Vector3(center, 0.2f, z), new Vector3(length, 0.4f, 0.16f), wood);
            GameObject glass = Part(wall, PrimitiveType.Cube, "Glass", new Vector3(center, 1.05f, z), new Vector3(length, 1.3f, 0.06f), Glass());
            glass.AddComponent<BoxCollider>().size = new Vector3(1f, 2.5f / 1.3f, 5f); // taller and thicker than it looks
            Part(wall, PrimitiveType.Cube, "Rail", new Vector3(center, 1.72f, z), new Vector3(length, 0.06f, 0.12f), wood);
            int posts = Mathf.Max(1, Mathf.RoundToInt(length / 2.5f));
            for (int i = 0; i <= posts; i++)
            {
                float x = Mathf.Lerp(fromX + 0.05f, toX - 0.05f, i / (float)posts);
                Part(wall, PrimitiveType.Cube, "Post", new Vector3(x, 0.88f, z), new Vector3(0.1f, 1.76f, 0.14f), wood);
            }
        }

        // Posts and a sign over the gap in the glass wall where the player walks between the
        // kitchen and the dining room.
        private static void CreateStaffDoor(Transform root, PrototypeSceneBuilder.ShopLayout layout)
        {
            Material wood = Mat("TrimWood", new Color(0.62f, 0.43f, 0.30f), 0.3f);
            float z = layout.PartitionZ, StaffDoorFrom = layout.StaffDoorFrom, StaffDoorTo = layout.StaffDoorTo;
            Part(root, PrimitiveType.Cube, "Staff Door Header", new Vector3((StaffDoorFrom + StaffDoorTo) / 2f, 2.1f, z),
                new Vector3(StaffDoorTo - StaffDoorFrom + 0.2f, 0.12f, 0.14f), wood);
            Part(root, PrimitiveType.Cube, "Staff Door Post", new Vector3(StaffDoorFrom, 1.05f, z), new Vector3(0.12f, 2.1f, 0.14f), wood);
            Part(root, PrimitiveType.Cube, "Staff Door Post", new Vector3(StaffDoorTo, 1.05f, z), new Vector3(0.12f, 2.1f, 0.14f), wood);
            Label(root, "STAFF", new Vector3((StaffDoorFrom + StaffDoorTo) / 2f, 2.35f, z), 0.035f, new Color(0.45f, 0.30f, 0.22f));

            // Two swing doors that open as the player or staff walk through.
            var door = new GameObject("Staff Door").transform;
            door.SetParent(root, false);
            door.localPosition = new Vector3((StaffDoorFrom + StaffDoorTo) / 2f, 0f, z);
            Material panel = Mat("DoorPanel", new Color(0.78f, 0.58f, 0.42f), 0.35f);
            float leafWidth = (StaffDoorTo - StaffDoorFrom) / 2f - 0.1f;
            var hinges = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f; // left leaf hangs on the left post, right leaf on the right
                Transform hinge = new GameObject(i == 0 ? "Left Leaf" : "Right Leaf").transform;
                hinge.SetParent(door, false);
                hinge.localPosition = new Vector3(side * ((StaffDoorTo - StaffDoorFrom) / 2f - 0.06f), 0f, 0f);
                Vector3 middle = new(-side * leafWidth / 2f, 0f, 0f);
                Part(hinge, PrimitiveType.Cube, "Panel", middle + new Vector3(0f, 0.95f, 0f), new Vector3(leafWidth, 1.1f, 0.06f), panel);
                Part(hinge, PrimitiveType.Cube, "Top Rail", middle + new Vector3(0f, 1.52f, 0f), new Vector3(leafWidth, 0.06f, 0.08f), wood);
                Part(hinge, PrimitiveType.Cube, "Window", middle + new Vector3(0f, 1.15f, -0.035f), new Vector3(leafWidth * 0.5f, 0.35f, 0.01f), Glass());
                hinges[i] = hinge;
            }
            var swing = door.gameObject.AddComponent<BubbleTea.Building.SwingDoor>();
            var serialized = new SerializedObject(swing);
            SerializedProperty leaves = serialized.FindProperty("leaves");
            SerializedProperty angles = serialized.FindProperty("openAngles");
            leaves.arraySize = 2;
            angles.arraySize = 2;
            for (int i = 0; i < 2; i++) leaves.GetArrayElementAtIndex(i).objectReferenceValue = hinges[i];
            // Both leaves swing into the kitchen.
            angles.GetArrayElementAtIndex(0).floatValue = -85f;
            angles.GetArrayElementAtIndex(1).floatValue = 85f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Gray kitchen tiles behind the counter, so the two rooms read as different spaces.
        private static void CreateKitchenFloor(Transform root, string materialName, float fromX, float toX,
            PrototypeSceneBuilder.ShopLayout layout)
        {
            float z0 = layout.PartitionZ, z1 = layout.HalfDepth;
            Material kitchen = Mat(materialName, new Color(0.93f, 0.85f, 0.74f), 0.35f); // warm cream tiles
            ApplyTexture(kitchen, TileTexture(), new Vector2((toX - fromX) / 2f, (z1 - z0) / 2f)); // 1 m tiles
            Part(root, PrimitiveType.Cube, "Kitchen Floor", new Vector3((fromX + toX) / 2f, 0.003f, (z0 + z1) / 2f),
                new Vector3(toX - fromX, 0.004f, z1 - z0), kitchen);
        }

        // The front doorway: posts, a header with an awning outside, and a mat.
        private static void CreateFrontDoor(Transform root, Style style, PrototypeSceneBuilder.ShopLayout layout)
        {
            float from = layout.DoorFrom, to = layout.DoorTo, z = -layout.HalfDepth - 0.25f;
            Material wood = Mat("TrimWood", new Color(0.62f, 0.43f, 0.30f), 0.3f);
            Part(root, PrimitiveType.Cube, "Door Post", new Vector3(from, 1.1f, z), new Vector3(0.3f, 2.2f, 0.6f), wood);
            Part(root, PrimitiveType.Cube, "Door Post", new Vector3(to, 1.1f, z), new Vector3(0.3f, 2.2f, 0.6f), wood);
            GameObject awning = Part(root, PrimitiveType.Cube, "Awning", new Vector3((from + to) / 2f, 2.3f, z - 0.6f),
                new Vector3(to - from + 0.6f, 0.08f, 1.4f), Mat(style.Prefix + "Accent", style.Accent, 0.4f));
            awning.transform.localRotation = Quaternion.Euler(-15f, 0f, 0f);

            // A noren: split fabric panels hanging over the doorway, and a lantern by each post.
            Material rod = Mat("WindowFrameWood", new Color(0.42f, 0.27f, 0.21f), 0.35f);
            Material[] cloth =
            {
                Mat(style.Prefix + "Noren", style.Noren, 0.1f),
                Mat(style.Prefix + "Accent", style.Accent, 0.4f),
            };
            Part(root, PrimitiveType.Cube, "Noren Rod", new Vector3((from + to) / 2f, 2.42f, z), new Vector3(to - from, 0.05f, 0.05f), rod);
            int panels = Mathf.Max(3, Mathf.RoundToInt((to - from) / 1.1f));
            float panelWidth = (to - from) / panels;
            for (int i = 0; i < panels; i++)
            {
                float x = from + (i + 0.5f) * panelWidth;
                Part(root, PrimitiveType.Cube, "Noren", new Vector3(x, 2.17f, z), new Vector3(panelWidth - 0.06f, 0.48f, 0.02f), cloth[i % 2]);
            }
            Lantern(root, new Vector3(from, 1.85f, z - 0.45f), 0.9f);
            Lantern(root, new Vector3(to, 1.85f, z - 0.45f), 0.9f);
            Part(root, PrimitiveType.Cube, "Door Mat", new Vector3((from + to) / 2f, 0.005f, z + 0.95f),
                new Vector3(4f, 0.01f, 1.2f), Mat("DoorMat", new Color(0.55f, 0.42f, 0.34f), 0.05f));
        }

        // The car lane outside the east wall: asphalt, edge lines, arrows, an awning over the
        // window, a menu board and a DRIVE THRU sign at the entrance.
        private static void CreateDriveThruLane(Transform root, Style style, PrototypeSceneBuilder.ShopLayout layout)
        {
            float laneX = layout.LaneX, wallX = layout.LaneWallX, outerX = layout.LaneOuterX, d = layout.HalfDepth;
            float windowZ = layout.DriveThru.z;
            // From the road out front to past the back wall.
            float laneFrom = layout.SidewalkZ - 3f, laneTo = d + 9f;
            float laneZ = (laneFrom + laneTo) / 2f, laneLength = laneTo - laneFrom;
            Material asphalt = Mat("Road", new Color(0.36f, 0.36f, 0.40f), 0.15f);
            Material line = Mat("LaneLine", new Color(1f, 0.85f, 0.35f), 0.2f);
            Material white = Mat("RoadPaint", new Color(0.97f, 0.97f, 0.97f), 0.2f);

            float width = outerX - wallX;
            Part(root, PrimitiveType.Cube, "Lane", new Vector3(wallX + width / 2f, 0.004f, laneZ), new Vector3(width, 0.008f, laneLength), asphalt);
            Part(root, PrimitiveType.Cube, "Lane Edge", new Vector3(wallX + 0.6f, 0.01f, laneZ), new Vector3(0.12f, 0.01f, laneLength), line);
            Part(root, PrimitiveType.Cube, "Lane Edge", new Vector3(outerX - 0.5f, 0.01f, laneZ), new Vector3(0.12f, 0.01f, laneLength), line);
            Part(root, PrimitiveType.Cube, "Curb", new Vector3(outerX + 0.1f, 0.08f, laneZ), new Vector3(0.3f, 0.16f, laneLength),
                Mat("Sidewalk", new Color(0.86f, 0.85f, 0.83f), 0.1f));
            Part(root, PrimitiveType.Cube, "Stop Line", new Vector3(laneX, 0.01f, windowZ + 1.6f), new Vector3(3.6f, 0.01f, 0.25f), white);

            foreach (float z in new[] { -d + 4f, d - 2f })
            {
                Part(root, PrimitiveType.Cube, "Arrow", new Vector3(laneX, 0.01f, z), new Vector3(0.3f, 0.01f, 1.8f), white);
                GameObject left = Part(root, PrimitiveType.Cube, "Arrow Head", new Vector3(laneX - 0.3f, 0.01f, z + 0.7f),
                    new Vector3(0.3f, 0.01f, 1f), white);
                left.transform.localRotation = Quaternion.Euler(0f, 40f, 0f);
                GameObject right = Part(root, PrimitiveType.Cube, "Arrow Head", new Vector3(laneX + 0.3f, 0.01f, z + 0.7f),
                    new Vector3(0.3f, 0.01f, 1f), white);
                right.transform.localRotation = Quaternion.Euler(0f, -40f, 0f);
            }

            // Awning over the window, sloping down toward the lane.
            GameObject awning = Part(root, PrimitiveType.Cube, "Window Awning", new Vector3(wallX + 1.1f, 2.6f, windowZ),
                new Vector3(1.8f, 0.08f, 3.4f), Mat(style.Prefix + "Accent", style.Accent, 0.4f));
            awning.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
            Material frame = Mat("WindowFrame", new Color(1f, 0.98f, 0.95f), 0.4f);
            Part(root, PrimitiveType.Cube, "Window Frame", new Vector3(wallX + 0.55f, 2.3f, windowZ), new Vector3(0.12f, 0.12f, 2.6f), frame);

            Material pole = Mat("SignPole", new Color(0.4f, 0.4f, 0.45f), 0.5f);
            Sign(root, "DRIVE THRU", new Vector3(outerX - 0.3f, 0f, -d + 1f), Mat(style.Prefix + "Accent", style.Accent, 0.4f), pole, 2.8f);
            Sign(root, "MENU", new Vector3(outerX - 0.3f, 0f, windowZ - 6f), Mat("Chalkboard", new Color(0.24f, 0.29f, 0.27f), 0.2f), pole, 2.2f);
        }

        // A board on a pole facing the street, with white text.
        private static void Sign(Transform root, string text, Vector3 position, Material board, Material pole, float width)
        {
            Part(root, PrimitiveType.Cylinder, "Sign Pole", position + new Vector3(0f, 1.1f, 0f), new Vector3(0.12f, 1.1f, 0.12f), pole);
            Part(root, PrimitiveType.Cube, "Sign Board", position + new Vector3(0f, 2.5f, 0f), new Vector3(width, 0.9f, 0.1f), board);
            Label(root, text, position + new Vector3(0f, 2.5f, -0.07f), 0.05f, Color.white);
        }

        private static void Label(Transform root, string text, Vector3 position, float size, Color color)
        {
            var label = new GameObject(text).AddComponent<TextMesh>();
            label.transform.SetParent(root, false);
            label.transform.localPosition = position;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.font = font;
            label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            label.text = text;
            label.fontSize = 96;
            label.fontStyle = FontStyle.Bold;
            label.characterSize = size;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
        }

        // See-through glass (a transparent Lit material).
        private static Material Glass()
        {
            Material glass = Mat("PartitionGlass", new Color(0.80f, 0.92f, 0.98f, 0.28f), 0.95f);
            glass.SetFloat("_Surface", 1f);
            glass.SetFloat("_Blend", 0f);
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            glass.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            glass.SetInt("_ZWrite", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(glass);
            return glass;
        }

        // ---------- Helpers ----------

        private static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider); // decoration only
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return part;
        }

        // A material that is updated to these values on every build, so tweaks here take effect.
        private static Material Mat(string name, Color color, float smoothness, Color? emission = null)
        {
            string path = $"{MaterialFolder}/Decor_{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                material.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyTexture(Material material, Texture2D texture, Vector2 tiling)
        {
            if (material == null || texture == null) return;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }
            EditorUtility.SetDirty(material);
        }

        // ---------- Textures drawn in code ----------

        // Two by two soft tiles with thin grout lines.
        private static Texture2D TileTexture() => SaveTexture("FloorTiles", 512, (x, y) =>
        {
            const int tile = 256, grout = 3;
            int tx = x % tile, ty = y % tile;
            if (tx < grout || ty < grout) return new Color(0.80f, 0.78f, 0.76f);
            bool alternate = (x / tile + y / tile) % 2 == 0;
            float shade = alternate ? 1f : 0.93f;
            shade -= Noise(x, y) * 0.025f;
            // A soft bevel near the grout.
            float edge = Mathf.Min(Mathf.Min(tx, tile - tx), Mathf.Min(ty, tile - ty));
            if (edge < 10) shade -= (10 - edge) * 0.004f;
            return new Color(shade, shade, shade);
        });

        // Planks running front to back, with staggered ends, dark seams, wavy grain and a few knots.
        private static Texture2D PlankTexture() => SaveTexture("WoodPlanks", 512, (x, y) =>
        {
            const int plankWidth = 128, plankLength = 512;
            int column = x / plankWidth;
            int along = (y + column * 197) % plankLength;
            int across = x % plankWidth;
            if (across < 4 || along < 4) return new Color(0.42f, 0.38f, 0.36f);
            float shade = 0.86f + 0.12f * Mathf.Repeat(column * 0.37f + (y + column * 197) / plankLength * 0.23f, 1f);
            shade += Mathf.Sin(across * 0.28f + Mathf.Sin(y * 0.015f + column * 1.7f) * 5f) * 0.035f;
            shade -= Noise(x, y) * 0.03f;
            // A soft edge on each board and a knot here and there.
            float edge = Mathf.Min(across, plankWidth - across);
            if (edge < 10) shade -= (10 - edge) * 0.006f;
            float knotX = across - 64f, knotY = along - 256f;
            float knot = knotX * knotX + knotY * knotY * 0.3f;
            if ((column % 3 == 1) && knot < 120f) shade -= 0.12f * (1f - knot / 120f);
            return new Color(shade, shade * 0.97f, shade * 0.95f);
        });

        // Vertical wooden boards for the walls, with dark grooves between them.
        private static Texture2D PanelTexture() => SaveTexture("WallBoards", 256, (x, y) =>
        {
            const int board = 64;
            int across = x % board;
            if (across < 3) return new Color(0.62f, 0.58f, 0.56f);
            float shade = 0.9f + 0.07f * Mathf.Repeat((x / board) * 0.41f, 1f);
            shade += Mathf.Sin(across * 0.4f + Mathf.Sin(y * 0.03f + x / board) * 3f) * 0.025f;
            shade -= Noise(x, y) * 0.03f;
            if (across < 8) shade -= (8 - across) * 0.01f;
            return new Color(shade, shade * 0.98f, shade * 0.96f);
        });

        // Rounded paving stones in staggered rows.
        private static Texture2D CobbleTexture() => SaveTexture("Cobbles", 256, (x, y) =>
        {
            const int stone = 64;
            int row = y / stone;
            int sx = (x + (row % 2) * stone / 2) % stone, sy = y % stone;
            float dx = (sx - stone / 2f) / (stone / 2f), dy = (sy - stone / 2f) / (stone / 2f);
            float d = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 0.6f + Mathf.Sqrt(dx * dx + dy * dy) * 0.4f;
            if (d > 0.92f) return new Color(0.55f, 0.53f, 0.52f);
            int id = (x + (row % 2) * stone / 2) / stone + row * 7;
            float shade = 0.86f + 0.1f * Mathf.Repeat(id * 0.618f, 1f) - d * 0.12f - Noise(x, y) * 0.04f;
            return new Color(shade, shade, shade * 0.98f);
        });

        private static float Noise(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177;
                return (h & 0xFFFF) / 65535f;
            }
        }

        private static Texture2D SaveTexture(string name, int size, System.Func<int, int, Color> pixel)
        {
            if (!AssetDatabase.IsValidFolder(TextureFolder))
            {
                Directory.CreateDirectory(TextureFolder);
                AssetDatabase.Refresh();
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++) pixels[y * size + x] = pixel(x, y);
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string path = $"{TextureFolder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 8;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
