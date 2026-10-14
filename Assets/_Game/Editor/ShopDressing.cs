using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// The look of a shop beyond its gameplay pieces: tiled floors (gray tiles in the kitchen),
    /// striped wallpaper, wood planks in the expansion, glass walls between the dining room and
    /// the kitchen, trim along the walls, windows, a shop sign, bunting, wall lamps,
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
            public Color Accent2;     // second bunting color
            public Color Grass;
            public string Prefix;     // material name prefix, so shops don't share colors
        }

        public static void Dress(Style style, Material floor, Material expansionFloor, Material wall, GameObject expansionArea)
        {
            PrototypeSceneBuilder.ShopLayout layout = PrototypeSceneBuilder.Layout;
            float w = layout.HalfWidth, d = layout.HalfDepth;

            // Textures are drawn in near-white, so each material's own color still tints them.
            ApplyTexture(floor, TileTexture(), new Vector2(w / 2f, d / 2f)); // 4 m pattern repeats
            ApplyTexture(expansionFloor, PlankTexture(), new Vector2((layout.LaneOuterX - w) / 2f, d));
            ApplyTexture(wall, StripeTexture(), new Vector2(w, 1f));

            var root = new GameObject("Dressing").transform;
            CreateStreet(root, style, layout);
            CreateTrim(root, style, 2f * w + 0.5f, 0f, d, 3f);
            CreateWindows(root, style, layout.Windows, d);
            CreateSign(root, style, d);
            CreateBunting(root, style, -w + 0.5f, w - 0.5f, d);
            CreateLamps(root, style, layout.Lamps, d);
            CreateFrontDoor(root, style, layout);
            CreateKitchenFloor(root, "KitchenFloor", -w, w, layout);
            float counterX = layout.Counter.x;
            CreatePartition(root, -w, counterX - 1.3f, layout);
            CreatePartition(root, counterX + 1.3f, layout.StaffDoorFrom, layout);
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
            Part(root, PrimitiveType.Cube, "Sidewalk", new Vector3(8f, -0.01f, sidewalk), new Vector3(70f, 0.02f, 5f),
                Mat("Sidewalk", new Color(0.86f, 0.85f, 0.83f), 0.1f));
            Part(root, PrimitiveType.Cube, "Road", new Vector3(8f, -0.012f, sidewalk - 6f), new Vector3(70f, 0.02f, 7f),
                Mat("Road", new Color(0.36f, 0.36f, 0.40f), 0.15f));

            Vector3[] trees = layout.Trees;
            Material trunk = Mat("TreeTrunk", new Color(0.55f, 0.38f, 0.26f), 0.1f);
            Material leaves = Mat(style.Prefix + "TreeLeaves", Color.Lerp(style.Grass, new Color(0.2f, 0.55f, 0.3f), 0.6f), 0.1f);
            for (int i = 0; i < trees.Length; i++)
            {
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
            Material wood = Mat("TrimWood", new Color(0.62f, 0.43f, 0.30f), 0.3f);
            Material cream = Mat("TrimCream", new Color(1f, 0.96f, 0.90f), 0.3f);
            float face = wallZ - 0.02f;
            Part(root, PrimitiveType.Cube, "Baseboard", new Vector3(centerX, 0.15f, face - 0.03f), new Vector3(length - 0.6f, 0.3f, 0.08f), wood);
            Part(root, PrimitiveType.Cube, "Chair Rail", new Vector3(centerX, 1.05f, face - 0.03f), new Vector3(length - 0.6f, 0.08f, 0.06f), cream);
            Part(root, PrimitiveType.Cube, "Cap", new Vector3(centerX, height + 0.04f, wallZ + 0.25f), new Vector3(length + 0.1f, 0.08f, 0.7f), cream);
        }

        private static void CreateWindows(Transform root, Style style, float[] xs, float wallZ)
        {
            Material frame = Mat("WindowFrame", new Color(1f, 0.98f, 0.95f), 0.4f);
            Material glass = Mat("WindowGlass", new Color(0.70f, 0.88f, 0.98f), 0.95f);
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
                Part(window, PrimitiveType.Cube, "Sill", new Vector3(0f, -height / 2f - 0.08f, -0.12f), new Vector3(width + 0.3f, 0.07f, 0.25f), sill);

                // Little potted plants on the sill.
                Material pot = Mat("Pot", new Color(0.90f, 0.60f, 0.45f), 0.2f);
                Material leaf = Mat("SillLeaf", new Color(0.42f, 0.73f, 0.47f), 0.2f);
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 p = new(i * 0.7f, -height / 2f + 0.02f, -0.14f);
                    Part(window, PrimitiveType.Cylinder, "Pot", p, new Vector3(0.18f, 0.08f, 0.18f), pot);
                    Part(window, PrimitiveType.Sphere, "Plant", p + new Vector3(0f, 0.15f, 0f), new Vector3(0.24f, 0.22f, 0.24f), leaf);
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
                Mat("TrimCream", new Color(1f, 0.96f, 0.90f), 0.3f));

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
            label.color = Color.white;
        }

        // A string of little diamond flags along the top of the back wall.
        private static void CreateBunting(Transform root, Style style, float fromX, float toX, float wallZ)
        {
            Transform bunting = new GameObject("Bunting").transform;
            bunting.SetParent(root, false);
            Material cord = Mat("Cord", new Color(0.95f, 0.93f, 0.88f), 0.2f);
            Material[] flags =
            {
                Mat(style.Prefix + "Accent", style.Accent, 0.4f),
                Mat(style.Prefix + "Accent2", style.Accent2, 0.4f),
                Mat("FlagCream", new Color(1f, 0.95f, 0.80f), 0.4f),
            };
            const float y = 2.85f;
            float z = wallZ - 0.12f;
            Part(bunting, PrimitiveType.Cube, "Cord", new Vector3((fromX + toX) / 2f, y, z), new Vector3(toX - fromX, 0.03f, 0.03f), cord);
            int index = 0;
            for (float x = fromX + 0.5f; x < toX; x += 0.9f, index++)
            {
                if (Mathf.Abs(x) < 3.6f) continue; // leave the sign clear
                GameObject flag = Part(bunting, PrimitiveType.Cube, "Flag", new Vector3(x, y - 0.18f, z), new Vector3(0.3f, 0.3f, 0.02f),
                    flags[index % flags.Length]);
                flag.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
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
            Material kitchen = Mat(materialName, new Color(0.84f, 0.86f, 0.88f), 0.35f);
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

        // Planks running front to back, with staggered ends and a little grain.
        private static Texture2D PlankTexture() => SaveTexture("WoodPlanks", 512, (x, y) =>
        {
            const int plankWidth = 128, plankLength = 512;
            int column = x / plankWidth;
            int along = (y + column * 197) % plankLength;
            if (x % plankWidth < 3 || along < 3) return new Color(0.70f, 0.66f, 0.62f);
            float shade = 0.92f + 0.06f * Mathf.Repeat(column * 0.37f, 1f);
            shade += Mathf.Sin(x * 0.35f + Mathf.Sin(y * 0.02f + column) * 4f) * 0.02f;
            shade -= Noise(x, y) * 0.02f;
            return new Color(shade, shade * 0.98f, shade * 0.96f);
        });

        // Soft vertical wallpaper stripes.
        private static Texture2D StripeTexture() => SaveTexture("WallStripes", 256, (x, y) =>
        {
            float shade = (x / 32) % 2 == 0 ? 1f : 0.92f;
            if (x % 32 == 0) shade = 0.88f;
            return new Color(shade, shade, shade);
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
