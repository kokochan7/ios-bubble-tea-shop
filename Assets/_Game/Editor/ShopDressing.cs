using System.IO;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// The look of a shop beyond its gameplay pieces: tiled floors, striped wallpaper, wood
    /// planks in the expansion, trim along the walls, windows, a shop sign, bunting, wall lamps,
    /// a rug under the café tables, a door mat, and a street with trees outside. Everything is
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
            // Textures are drawn in near-white, so each material's own color still tints them.
            ApplyTexture(floor, TileTexture(), new Vector2(7.5f, 7.5f)); // 2 m tiles on the 30 m floor
            ApplyTexture(expansionFloor, PlankTexture(), new Vector2(8f, 15f));
            ApplyTexture(wall, StripeTexture(), new Vector2(15f, 1f));

            var root = new GameObject("Dressing").transform;
            CreateStreet(root, style);
            CreateTrim(root, style, 30.5f, 0f, 15f, 3f);
            CreateWindows(root, style, new[] { -11f, -5.5f, 5.5f, 11f }, 15f);
            CreateSign(root, style);
            CreateBunting(root, style, -14.5f, 14.5f, 15f);
            CreateLamps(root, style, new[] { -8.25f, 8.25f }, 15f);
            CreateRug(root, style, new Vector3(6f, 0f, 7.4f), new Vector2(6.4f, 5.2f));
            CreateDoorMat(root, style, new Vector3(-5f, 0f, -14.3f));
            CreateDoorMat(root, style, new Vector3(22f, 0f, -14.3f), expansionArea != null ? expansionArea.transform : root);

            if (expansionArea != null)
            {
                Transform expansion = new GameObject("Dressing").transform;
                expansion.SetParent(expansionArea.transform, false);
                CreateTrim(expansion, style, 16.5f, 23f, 15f, 3f);
                CreateWindows(expansion, style, new[] { 19f, 27f }, 15f);
                CreateLamps(expansion, style, new[] { 23f }, 15f);
            }
        }

        // ---------- Outside ----------

        private static void CreateStreet(Transform root, Style style)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<MeshCollider>());
            ground.transform.SetParent(root, false);
            ground.transform.localPosition = new Vector3(8f, -0.02f, 0f);
            ground.transform.localScale = new Vector3(14f, 1f, 14f);
            ground.GetComponent<Renderer>().sharedMaterial = Mat(style.Prefix + "Grass", style.Grass, 0.05f);

            // A sidewalk along the front of the shop.
            Part(root, PrimitiveType.Cube, "Sidewalk", new Vector3(8f, -0.01f, -18f), new Vector3(70f, 0.02f, 5f),
                Mat("Sidewalk", new Color(0.86f, 0.85f, 0.83f), 0.1f));
            Part(root, PrimitiveType.Cube, "Road", new Vector3(8f, -0.012f, -24f), new Vector3(70f, 0.02f, 7f),
                Mat("Road", new Color(0.36f, 0.36f, 0.40f), 0.15f));

            Vector3[] trees =
            {
                new(-19f, 0f, -17f), new(-19f, 0f, -6f), new(-19f, 0f, 5f), new(-19f, 0f, 14f),
                new(-12f, 0f, 19f), new(-2f, 0f, 19.5f), new(9f, 0f, 19f), new(19f, 0f, 19.5f), new(28f, 0f, 19f),
                new(35f, 0f, 12f), new(35f, 0f, 1f), new(35f, 0f, -10f), new(4f, 0f, -18.5f), new(16f, 0f, -18.5f),
            };
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
        private static void CreateSign(Transform root, Style style)
        {
            Transform sign = new GameObject("Shop Sign").transform;
            sign.SetParent(root, false);
            sign.localPosition = new Vector3(0f, 2.15f, 14.9f);
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

        // ---------- Floor details ----------

        private static void CreateRug(Transform root, Style style, Vector3 center, Vector2 size)
        {
            Part(root, PrimitiveType.Cube, "Rug Border", center + new Vector3(0f, 0.004f, 0f), new Vector3(size.x, 0.008f, size.y),
                Mat(style.Prefix + "Accent", style.Accent, 0.4f));
            Part(root, PrimitiveType.Cube, "Rug", center + new Vector3(0f, 0.006f, 0f), new Vector3(size.x - 0.5f, 0.008f, size.y - 0.5f),
                Mat("RugCream", new Color(1f, 0.95f, 0.86f), 0.1f));
        }

        private static void CreateDoorMat(Transform root, Style style, Vector3 center, Transform parent = null)
        {
            Part(parent != null ? parent : root, PrimitiveType.Cube, "Door Mat", center + new Vector3(0f, 0.005f, 0f),
                new Vector3(2.4f, 0.01f, 1.2f), Mat("DoorMat", new Color(0.55f, 0.42f, 0.34f), 0.05f));
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
