using System;
using System.Collections.Generic;
using BubbleTea.Building;
using BubbleTea.Core;
using BubbleTea.Player;
using UnityEngine;

namespace BubbleTea.Extras
{
    /// <summary>
    /// The decor shop: cosmetic pieces bought with pearls, in four categories of eight. Each purchase
    /// puts one more piece out in the open shop (buy the same piece as often as you like), placed and
    /// moved like other decor. Every piece out makes customers in that shop 1% happier (they tip more).
    /// </summary>
    public static class Collections
    {
        public class Item
        {
            public string Id;
            public string Name;
            public string Collection;
            public int Price;
            public Vector3 Size;              // the collider: width, height, depth
            public Action<Transform> Build;   // adds the model's parts
        }

        /// <summary>The collections, in the order the shop shows them.</summary>
        public static readonly string[] Names = { "Furniture", "LED Signs", "Plants", "Misc" };

        /// <summary>Extra happiness in this shop for each piece out in it (0.01 = +1%).</summary>
        public const float BonusPerPiece = 0.01f;

        private const string Prefix = "Decor ";

        private static readonly List<GameObject> placed = new();

        public static readonly Item[] All =
        {
            // Furniture
            new Item { Id = "lamp", Name = "Floor Lamp", Collection = "Furniture", Price = 4, Size = new Vector3(0.6f, 1.9f, 0.6f), Build = FloorLamp },
            new Item { Id = "bookshelf", Name = "Bookshelf", Collection = "Furniture", Price = 6, Size = new Vector3(1.2f, 1.8f, 0.45f), Build = Bookshelf },
            new Item { Id = "armchair", Name = "Armchair", Collection = "Furniture", Price = 6, Size = new Vector3(0.95f, 1f, 0.9f), Build = Armchair },
            new Item { Id = "coatrack", Name = "Coat Rack", Collection = "Furniture", Price = 4, Size = new Vector3(0.6f, 1.9f, 0.6f), Build = CoatRack },
            new Item { Id = "sofa", Name = "Comfy Sofa", Collection = "Furniture", Price = 9, Size = new Vector3(1.8f, 0.95f, 0.85f), Build = Sofa },
            new Item { Id = "coffeetable", Name = "Coffee Table", Collection = "Furniture", Price = 5, Size = new Vector3(1.1f, 0.5f, 0.7f), Build = CoffeeTable },
            new Item { Id = "rug", Name = "Round Rug", Collection = "Furniture", Price = 4, Size = new Vector3(1.8f, 0.04f, 1.8f), Build = Rug },
            new Item { Id = "clock", Name = "Tall Clock", Collection = "Furniture", Price = 8, Size = new Vector3(0.6f, 2f, 0.4f), Build = TallClock },
            // LED signs
            new Item { Id = "neonheart", Name = "LED Heart", Collection = "LED Signs", Price = 8, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonHeart) },
            new Item { Id = "neonboba", Name = "LED Boba", Collection = "LED Signs", Price = 8, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonBoba) },
            new Item { Id = "neonstar", Name = "LED Star", Collection = "LED Signs", Price = 8, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonStar) },
            new Item { Id = "neonmoon", Name = "LED Moon", Collection = "LED Signs", Price = 8, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonMoon) },
            new Item { Id = "neoncat", Name = "LED Cat", Collection = "LED Signs", Price = 9, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonCat) },
            new Item { Id = "neonbolt", Name = "LED Lightning", Collection = "LED Signs", Price = 8, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonBolt) },
            new Item { Id = "neoncup", Name = "LED Tea Cup", Collection = "LED Signs", Price = 9, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonCup) },
            new Item { Id = "neonrainbow", Name = "LED Rainbow", Collection = "LED Signs", Price = 10, Size = new Vector3(1.1f, 1.9f, 0.35f), Build = t => NeonSign(t, NeonRainbow) },
            // Plants
            new Item { Id = "monstera", Name = "Monstera", Collection = "Plants", Price = 3, Size = new Vector3(0.9f, 1.4f, 0.9f), Build = Monstera },
            new Item { Id = "cactus", Name = "Cactus", Collection = "Plants", Price = 3, Size = new Vector3(0.6f, 1.2f, 0.6f), Build = Cactus },
            new Item { Id = "bonsai", Name = "Bonsai", Collection = "Plants", Price = 5, Size = new Vector3(0.8f, 1.1f, 0.8f), Build = Bonsai },
            new Item { Id = "sunflower", Name = "Sunflowers", Collection = "Plants", Price = 4, Size = new Vector3(0.6f, 1.4f, 0.6f), Build = Sunflowers },
            new Item { Id = "palm", Name = "Palm Tree", Collection = "Plants", Price = 6, Size = new Vector3(1f, 2.2f, 1f), Build = Palm },
            new Item { Id = "tulips", Name = "Tulips", Collection = "Plants", Price = 3, Size = new Vector3(0.6f, 0.9f, 0.6f), Build = Tulips },
            new Item { Id = "fern", Name = "Fern", Collection = "Plants", Price = 4, Size = new Vector3(0.9f, 1f, 0.9f), Build = Fern },
            new Item { Id = "succulents", Name = "Succulent Trio", Collection = "Plants", Price = 4, Size = new Vector3(1f, 0.5f, 0.45f), Build = Succulents },
            // Misc
            new Item { Id = "bigcup", Name = "Giant Boba Cup", Collection = "Misc", Price = 10, Size = new Vector3(0.9f, 1.9f, 0.9f), Build = GiantCup },
            new Item { Id = "teapot", Name = "Big Teapot", Collection = "Misc", Price = 7, Size = new Vector3(1f, 1f, 0.8f), Build = Teapot },
            new Item { Id = "mochi", Name = "Mochi Plush", Collection = "Misc", Price = 6, Size = new Vector3(0.9f, 0.8f, 0.9f), Build = Mochi },
            new Item { Id = "cuptower", Name = "Cup Tower", Collection = "Misc", Price = 7, Size = new Vector3(0.9f, 1.5f, 0.9f), Build = CupTower },
            new Item { Id = "gumball", Name = "Gumball Machine", Collection = "Misc", Price = 7, Size = new Vector3(0.6f, 1.4f, 0.6f), Build = Gumball },
            new Item { Id = "arcade", Name = "Arcade Machine", Collection = "Misc", Price = 12, Size = new Vector3(0.8f, 1.8f, 0.8f), Build = Arcade },
            new Item { Id = "jukebox", Name = "Jukebox", Collection = "Misc", Price = 11, Size = new Vector3(0.9f, 1.5f, 0.6f), Build = Jukebox },
            new Item { Id = "balloon", Name = "Boba Balloon", Collection = "Misc", Price = 5, Size = new Vector3(0.5f, 2.2f, 0.5f), Build = Balloon },
        };

        /// <summary>Extra happiness from the pieces out in the open shop (read by the shop's decor bonus).</summary>
        public static float Bonus
        {
            get
            {
                placed.RemoveAll(p => p == null);
                return placed.Count * BonusPerPiece;
            }
        }

        public static Item Find(string id) => Array.Find(All, i => i.Id == id);

        /// <summary>How many of this piece are out in the open shop.</summary>
        public static int CountPlaced(string id) => placed.FindAll(p => p != null && ItemId(p.name.Substring(Prefix.Length)) == id).Count;

        // A copy's save key is the piece's ID, then "#2", "#3"... for the second and later copies.
        private static string ItemId(string key)
        {
            int hash = key.IndexOf('#');
            return hash < 0 ? key : key.Substring(0, hash);
        }

        private static string NextKey(string id)
        {
            for (int copy = 1; ; copy++)
            {
                string key = copy == 1 ? id : id + "#" + copy;
                if (!placed.Exists(p => p != null && p.name == Prefix + key)) return key;
            }
        }

        /// <summary>
        /// Buys a piece with pearls and puts it out next to the player to place. CANCEL puts it back
        /// and gives the pearls back.
        /// </summary>
        public static bool Buy(Item item)
        {
            if (!Profile.TrySpend(item.Price)) return false;
            Profile.AddDecor(item.Id);
            Vector3 spot = Vector3.zero;
            Quaternion facing = Quaternion.Euler(0f, 180f, 0f);
            if (PlayerMovement.Current != null)
            {
                Transform player = PlayerMovement.Current;
                Vector3 forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
                spot = player.position + forward * 1.4f;
                spot.y = 0f;
            }
            string key = NextKey(item.Id);
            Movable movable = Spawn(item, key, spot, facing);
            MoveMode.Begin(movable, mustFit: true, ended: kept =>
            {
                if (kept)
                {
                    GameEvents.RaiseAnnouncement($"{item.Name} placed! Customers here are {Bonus * 100f:0}% happier.", new Color(0.95f, 0.75f, 0.25f));
                    return;
                }
                Remove(key);
                Profile.AddPearls(item.Price, null); // changed their mind: pearls back
            });
            return true;
        }

        private static void Remove(string key)
        {
            for (int i = placed.Count - 1; i >= 0; i--)
            {
                if (placed[i] != null && placed[i].name != Prefix + key) continue;
                if (placed[i] != null) UnityEngine.Object.Destroy(placed[i]);
                placed.RemoveAt(i);
            }
            Movable.NotifyChanged();
        }

        /// <summary>The pieces out in the open shop, for the save file.</summary>
        public static List<string> Snapshot()
        {
            var keys = new List<string>();
            foreach (GameObject piece in placed) if (piece != null) keys.Add(piece.name.Substring(Prefix.Length));
            return keys;
        }

        /// <summary>Puts the saved pieces back (their spots come back with the other moved things).</summary>
        public static void Restore(List<string> keys)
        {
            placed.Clear();
            if (keys == null) return;
            foreach (string key in keys)
            {
                Item item = Find(ItemId(key));
                if (item != null && !placed.Exists(p => p != null && p.name == Prefix + key)) Spawn(item, key, Vector3.zero, Quaternion.identity);
            }
        }

        private static Movable Spawn(Item item, string key, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject(Prefix + key);
            root.transform.SetPositionAndRotation(position, rotation);
            item.Build(root.transform);
            ModelKit.Collider(root, new Vector3(0f, item.Size.y * 0.5f, 0f), item.Size);
            placed.Add(root);
            Movable movable = Movable.Make(root, "decor:" + key, tapToMove: true);
            movable.Delete = () => Remove(key); // DELETE on the move bar throws it away
            return movable;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => placed.Clear();

        // ---------------------------------------------------------------- models

        private static readonly Color Wood = new(0.55f, 0.36f, 0.22f);
        private static readonly Color LightWood = new(0.78f, 0.6f, 0.4f);
        private static readonly Color Cream = new(0.98f, 0.94f, 0.85f);
        private static readonly Color Leaf = new(0.3f, 0.62f, 0.32f);
        private static readonly Color DarkLeaf = new(0.2f, 0.45f, 0.25f);
        private static readonly Color Pot = new(0.85f, 0.5f, 0.35f);
        private static readonly Color Charcoal = new(0.18f, 0.18f, 0.22f);

        private static Transform P(Transform t, PrimitiveType type, float x, float y, float z, float sx, float sy, float sz, Color c,
            float rx = 0f, float ry = 0f, float rz = 0f, bool glow = false) =>
            ModelKit.Part(t, type, new Vector3(x, y, z), new Vector3(sx, sy, sz), c, new Vector3(rx, ry, rz), glow);

        private static void FloorLamp(Transform t)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.03f, 0f, 0.45f, 0.03f, 0.45f, Charcoal);
            P(t, PrimitiveType.Cylinder, 0f, 0.8f, 0f, 0.06f, 0.78f, 0.06f, Charcoal);
            P(t, PrimitiveType.Cylinder, 0f, 1.62f, 0f, 0.55f, 0.2f, 0.55f, new Color(1f, 0.9f, 0.7f), glow: true);
            P(t, PrimitiveType.Sphere, 0f, 1.5f, 0f, 0.18f, 0.18f, 0.18f, new Color(1f, 0.95f, 0.75f), glow: true);
        }

        private static void Bookshelf(Transform t)
        {
            P(t, PrimitiveType.Cube, 0f, 0.9f, 0f, 1.2f, 1.8f, 0.4f, Wood);
            Color[] books = { new(0.85f, 0.35f, 0.35f), new(0.35f, 0.55f, 0.85f), new(0.95f, 0.8f, 0.3f), new(0.45f, 0.7f, 0.45f), new(0.7f, 0.5f, 0.85f) };
            for (int shelf = 0; shelf < 3; shelf++)
            {
                float y = 0.35f + shelf * 0.55f;
                P(t, PrimitiveType.Cube, 0f, y - 0.2f, 0.06f, 1.1f, 0.04f, 0.32f, LightWood);
                for (int b = 0; b < 6; b++)
                {
                    float h = 0.28f + (b * 7 + shelf * 3) % 5 * 0.03f;
                    P(t, PrimitiveType.Cube, -0.42f + b * 0.16f, y - 0.18f + h * 0.5f, 0.1f, 0.12f, h, 0.26f, books[(b + shelf) % books.Length]);
                }
            }
        }

        private static void Armchair(Transform t)
        {
            Color fabric = new(0.95f, 0.62f, 0.55f);
            P(t, PrimitiveType.Cube, 0f, 0.3f, 0f, 0.9f, 0.35f, 0.85f, fabric);
            P(t, PrimitiveType.Cube, 0f, 0.7f, -0.35f, 0.9f, 0.6f, 0.18f, fabric);
            P(t, PrimitiveType.Cube, -0.42f, 0.55f, 0f, 0.14f, 0.3f, 0.85f, fabric);
            P(t, PrimitiveType.Cube, 0.42f, 0.55f, 0f, 0.14f, 0.3f, 0.85f, fabric);
            P(t, PrimitiveType.Cube, 0f, 0.5f, 0.05f, 0.7f, 0.1f, 0.65f, Cream);
        }

        private static void CoatRack(Transform t)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.03f, 0f, 0.45f, 0.03f, 0.45f, Wood);
            P(t, PrimitiveType.Cylinder, 0f, 0.9f, 0f, 0.07f, 0.88f, 0.07f, Wood);
            for (int i = 0; i < 4; i++)
                P(t, PrimitiveType.Cylinder, Mathf.Sin(i * 1.57f) * 0.15f, 1.65f, Mathf.Cos(i * 1.57f) * 0.15f, 0.04f, 0.15f, 0.04f, Wood, 45f, i * 90f, 0f);
            P(t, PrimitiveType.Sphere, 0.18f, 1.45f, 0f, 0.3f, 0.45f, 0.2f, new Color(0.9f, 0.45f, 0.5f));
            P(t, PrimitiveType.Sphere, -0.12f, 1.62f, 0.1f, 0.3f, 0.2f, 0.3f, new Color(0.35f, 0.55f, 0.85f));
        }

        // A standing sign: a dark board on two legs with a glowing shape on the front.
        private static void NeonSign(Transform t, Action<Transform> shape)
        {
            P(t, PrimitiveType.Cube, -0.45f, 0.5f, 0f, 0.06f, 1f, 0.06f, Charcoal);
            P(t, PrimitiveType.Cube, 0.45f, 0.5f, 0f, 0.06f, 1f, 0.06f, Charcoal);
            P(t, PrimitiveType.Cube, 0f, 0.02f, 0f, 1.05f, 0.04f, 0.3f, Charcoal);
            P(t, PrimitiveType.Cube, 0f, 1.4f, 0f, 1.05f, 0.95f, 0.06f, new Color(0.1f, 0.1f, 0.14f));
            var front = new GameObject("Glow").transform;
            front.SetParent(t, false);
            front.localPosition = new Vector3(0f, 1.4f, 0.05f);
            shape(front);
        }

        private static void Tube(Transform t, float x, float y, float length, float angle, Color c) =>
            P(t, PrimitiveType.Cylinder, x, y, 0f, 0.06f, length * 0.5f, 0.06f, c, 0f, 0f, angle, glow: true);

        private static void NeonHeart(Transform t)
        {
            Color pink = new(1f, 0.35f, 0.65f);
            Tube(t, -0.15f, 0.12f, 0.35f, 60f, pink);
            Tube(t, 0.15f, 0.12f, 0.35f, -60f, pink);
            Tube(t, -0.14f, -0.12f, 0.42f, 45f, pink);
            Tube(t, 0.14f, -0.12f, 0.42f, -45f, pink);
            Tube(t, -0.3f, 0.02f, 0.2f, 0f, pink);
            Tube(t, 0.3f, 0.02f, 0.2f, 0f, pink);
        }

        private static void NeonBoba(Transform t)
        {
            Color cyan = new(0.35f, 0.95f, 1f);
            Tube(t, -0.17f, 0f, 0.62f, -8f, cyan);
            Tube(t, 0.17f, 0f, 0.62f, 8f, cyan);
            Tube(t, 0f, 0.3f, 0.42f, 90f, cyan);
            Tube(t, 0f, -0.3f, 0.3f, 90f, cyan);
            Tube(t, 0.08f, 0.42f, 0.3f, -20f, new Color(1f, 0.6f, 0.9f));
            foreach (float x in new[] { -0.08f, 0.06f, 0f })
                P(t, PrimitiveType.Sphere, x, -0.2f + (x == 0f ? 0.1f : 0f), 0f, 0.08f, 0.08f, 0.08f, new Color(1f, 0.85f, 0.4f), glow: true);
        }

        private static void NeonStar(Transform t)
        {
            Color yellow = new(1f, 0.9f, 0.35f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                Vector2 dir = new(Mathf.Sin(a * Mathf.Deg2Rad), Mathf.Cos(a * Mathf.Deg2Rad));
                Tube(t, dir.x * 0.17f, dir.y * 0.17f, 0.34f, -a, yellow);
            }
        }

        private static void NeonMoon(Transform t)
        {
            Color lavender = new(0.75f, 0.6f, 1f);
            for (int i = 0; i < 7; i++)
            {
                float a = 40f + i * 40f;
                Vector2 dir = new(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Tube(t, dir.x * 0.3f, dir.y * 0.3f, 0.2f, a, lavender);
            }
            P(t, PrimitiveType.Sphere, 0.15f, 0.1f, 0f, 0.07f, 0.07f, 0.07f, new Color(1f, 0.95f, 0.6f), glow: true);
        }

        private static void PotBase(Transform t, float width, Color color)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.22f, 0f, width, 0.22f, width, color);
            P(t, PrimitiveType.Cylinder, 0f, 0.43f, 0f, width * 0.9f, 0.02f, width * 0.9f, new Color(0.35f, 0.25f, 0.18f));
        }

        private static void Monstera(Transform t)
        {
            PotBase(t, 0.55f, Cream);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                P(t, PrimitiveType.Cylinder, dir.x * 0.12f, 0.7f, dir.z * 0.12f, 0.03f, 0.3f, 0.03f, DarkLeaf, 20f, a, 0f);
                P(t, PrimitiveType.Sphere, dir.x * 0.32f, 0.95f + (i % 2) * 0.15f, dir.z * 0.32f, 0.45f, 0.05f, 0.32f, i % 2 == 0 ? Leaf : DarkLeaf, -25f, a + 90f, 0f);
            }
        }

        private static void Cactus(Transform t)
        {
            PotBase(t, 0.45f, Pot);
            Color green = new(0.35f, 0.65f, 0.4f);
            P(t, PrimitiveType.Capsule, 0f, 0.8f, 0f, 0.28f, 0.38f, 0.28f, green);
            P(t, PrimitiveType.Capsule, 0.18f, 0.85f, 0f, 0.12f, 0.15f, 0.12f, green);
            P(t, PrimitiveType.Capsule, -0.16f, 0.75f, 0f, 0.12f, 0.12f, 0.12f, green);
            P(t, PrimitiveType.Sphere, 0f, 1.16f, 0f, 0.12f, 0.08f, 0.12f, new Color(1f, 0.5f, 0.65f));
        }

        private static void Bonsai(Transform t)
        {
            P(t, PrimitiveType.Cube, 0f, 0.12f, 0f, 0.7f, 0.2f, 0.45f, new Color(0.3f, 0.35f, 0.5f));
            P(t, PrimitiveType.Cylinder, 0f, 0.4f, 0f, 0.1f, 0.2f, 0.1f, Wood, 0f, 0f, 15f);
            P(t, PrimitiveType.Cylinder, 0.12f, 0.62f, 0f, 0.08f, 0.15f, 0.08f, Wood, 0f, 0f, -35f);
            P(t, PrimitiveType.Sphere, 0.2f, 0.8f, 0f, 0.5f, 0.25f, 0.4f, Leaf);
            P(t, PrimitiveType.Sphere, -0.15f, 0.68f, 0.05f, 0.35f, 0.18f, 0.3f, DarkLeaf);
            P(t, PrimitiveType.Sphere, 0.05f, 0.95f, -0.05f, 0.3f, 0.15f, 0.25f, Leaf);
        }

        private static void Sunflowers(Transform t)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.25f, 0f, 0.35f, 0.25f, 0.35f, new Color(0.45f, 0.65f, 0.85f));
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.12f;
                float h = 0.95f + i % 2 * 0.18f;
                P(t, PrimitiveType.Cylinder, x, h * 0.5f + 0.2f, 0f, 0.03f, h * 0.5f - 0.1f, 0.03f, Leaf);
                P(t, PrimitiveType.Cylinder, x, h + 0.12f, 0.05f, 0.3f, 0.02f, 0.3f, new Color(1f, 0.82f, 0.2f), 80f, 0f, 0f);
                P(t, PrimitiveType.Cylinder, x, h + 0.12f, 0.07f, 0.13f, 0.02f, 0.13f, new Color(0.4f, 0.25f, 0.12f), 80f, 0f, 0f);
            }
        }

        private static void GiantCup(Transform t)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.75f, 0f, 0.75f, 0.75f, 0.75f, new Color(0.88f, 0.75f, 0.6f));
            P(t, PrimitiveType.Cylinder, 0f, 1.52f, 0f, 0.8f, 0.03f, 0.8f, new Color(0.95f, 0.95f, 1f));
            P(t, PrimitiveType.Cylinder, 0.12f, 1.75f, 0f, 0.09f, 0.3f, 0.09f, new Color(1f, 0.5f, 0.65f), 0f, 0f, -12f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                P(t, PrimitiveType.Sphere, Mathf.Cos(a) * 0.3f, 0.15f + (i % 3) * 0.08f, Mathf.Sin(a) * 0.3f, 0.16f, 0.16f, 0.16f, new Color(0.22f, 0.14f, 0.1f));
            }
            P(t, PrimitiveType.Cube, 0f, 0.9f, 0.38f, 0.4f, 0.25f, 0.02f, new Color(1f, 0.6f, 0.7f));
        }

        private static void Teapot(Transform t)
        {
            Color glaze = new(0.45f, 0.7f, 0.65f);
            P(t, PrimitiveType.Sphere, 0f, 0.42f, 0f, 0.75f, 0.65f, 0.7f, glaze);
            P(t, PrimitiveType.Cylinder, 0f, 0.78f, 0f, 0.3f, 0.05f, 0.3f, glaze);
            P(t, PrimitiveType.Sphere, 0f, 0.85f, 0f, 0.1f, 0.1f, 0.1f, Cream);
            P(t, PrimitiveType.Cylinder, 0.42f, 0.5f, 0f, 0.08f, 0.2f, 0.08f, glaze, 0f, 0f, -50f);
            P(t, PrimitiveType.Cylinder, -0.4f, 0.48f, 0f, 0.06f, 0.18f, 0.25f, glaze, 0f, 0f, 0f);
        }

        private static void Mochi(Transform t)
        {
            Color skin = new(1f, 0.85f, 0.88f);
            P(t, PrimitiveType.Sphere, 0f, 0.35f, 0f, 0.85f, 0.65f, 0.8f, skin);
            P(t, PrimitiveType.Sphere, -0.15f, 0.42f, 0.36f, 0.07f, 0.09f, 0.04f, Charcoal);
            P(t, PrimitiveType.Sphere, 0.15f, 0.42f, 0.36f, 0.07f, 0.09f, 0.04f, Charcoal);
            P(t, PrimitiveType.Sphere, -0.25f, 0.33f, 0.33f, 0.1f, 0.05f, 0.03f, new Color(1f, 0.55f, 0.6f));
            P(t, PrimitiveType.Sphere, 0.25f, 0.33f, 0.33f, 0.1f, 0.05f, 0.03f, new Color(1f, 0.55f, 0.6f));
            P(t, PrimitiveType.Sphere, 0f, 0.7f, 0f, 0.2f, 0.08f, 0.2f, new Color(0.5f, 0.75f, 0.45f));
        }

        private static void CupTower(Transform t)
        {
            Color[] cups = { new(1f, 0.7f, 0.75f), new(0.7f, 0.85f, 1f), new(1f, 0.9f, 0.6f), new(0.75f, 0.95f, 0.75f) };
            P(t, PrimitiveType.Cylinder, 0f, 0.05f, 0f, 0.85f, 0.05f, 0.85f, Wood);
            int n = 0;
            for (int row = 0; row < 3; row++)
            {
                int count = 3 - row;
                for (int i = 0; i < count; i++)
                {
                    float x = (i - (count - 1) * 0.5f) * 0.26f;
                    P(t, PrimitiveType.Cylinder, x, 0.28f + row * 0.4f, 0f, 0.22f, 0.18f, 0.22f, cups[n++ % cups.Length]);
                }
            }
        }

        // ---------------------------------------------------------------- more furniture

        private static void Sofa(Transform t)
        {
            Color fabric = new(0.45f, 0.6f, 0.85f);
            P(t, PrimitiveType.Cube, 0f, 0.3f, 0.05f, 1.8f, 0.35f, 0.8f, fabric);
            P(t, PrimitiveType.Cube, 0f, 0.7f, -0.3f, 1.8f, 0.6f, 0.2f, fabric);
            P(t, PrimitiveType.Cube, -0.85f, 0.55f, 0.05f, 0.16f, 0.35f, 0.8f, fabric);
            P(t, PrimitiveType.Cube, 0.85f, 0.55f, 0.05f, 0.16f, 0.35f, 0.8f, fabric);
            P(t, PrimitiveType.Cube, -0.38f, 0.52f, 0.1f, 0.72f, 0.1f, 0.6f, Cream);
            P(t, PrimitiveType.Cube, 0.38f, 0.52f, 0.1f, 0.72f, 0.1f, 0.6f, Cream);
            P(t, PrimitiveType.Cube, 0.5f, 0.72f, -0.12f, 0.35f, 0.3f, 0.12f, new Color(1f, 0.75f, 0.4f), 0f, 0f, 10f); // cushion
        }

        private static void CoffeeTable(Transform t)
        {
            P(t, PrimitiveType.Cube, 0f, 0.42f, 0f, 1.1f, 0.06f, 0.7f, LightWood);
            foreach (float x in new[] { -0.45f, 0.45f })
            foreach (float z in new[] { -0.27f, 0.27f })
                P(t, PrimitiveType.Cube, x, 0.2f, z, 0.06f, 0.4f, 0.06f, Wood);
            P(t, PrimitiveType.Cylinder, 0.25f, 0.5f, 0.1f, 0.12f, 0.05f, 0.12f, Cream);
            P(t, PrimitiveType.Cube, -0.2f, 0.47f, -0.05f, 0.3f, 0.04f, 0.22f, new Color(0.85f, 0.35f, 0.35f));
        }

        private static void Rug(Transform t)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.01f, 0f, 1.8f, 0.01f, 1.8f, new Color(0.95f, 0.7f, 0.55f));
            P(t, PrimitiveType.Cylinder, 0f, 0.015f, 0f, 1.3f, 0.01f, 1.3f, Cream);
            P(t, PrimitiveType.Cylinder, 0f, 0.02f, 0f, 0.7f, 0.01f, 0.7f, new Color(0.55f, 0.75f, 0.7f));
        }

        private static void TallClock(Transform t)
        {
            P(t, PrimitiveType.Cube, 0f, 0.95f, 0f, 0.55f, 1.9f, 0.38f, Wood);
            P(t, PrimitiveType.Cylinder, 0f, 1.55f, 0.2f, 0.4f, 0.02f, 0.4f, Cream, 90f, 0f, 0f);
            P(t, PrimitiveType.Cube, 0f, 1.6f, 0.22f, 0.03f, 0.14f, 0.02f, Charcoal);
            P(t, PrimitiveType.Cube, 0.05f, 1.55f, 0.22f, 0.1f, 0.03f, 0.02f, Charcoal);
            P(t, PrimitiveType.Cube, 0f, 0.8f, 0.2f, 0.35f, 0.9f, 0.02f, new Color(0.3f, 0.2f, 0.12f));
            P(t, PrimitiveType.Cylinder, 0f, 0.65f, 0.22f, 0.16f, 0.01f, 0.16f, new Color(0.95f, 0.8f, 0.35f), 90f, 0f, 0f); // pendulum
        }

        // ---------------------------------------------------------------- more LED signs

        private static void NeonCat(Transform t)
        {
            Color orange = new(1f, 0.6f, 0.25f);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                Vector2 dir = new(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                Tube(t, dir.x * 0.26f, dir.y * 0.22f - 0.04f, 0.2f, a + 90f, orange);
            }
            Tube(t, -0.2f, 0.25f, 0.18f, -20f, orange);
            Tube(t, 0.2f, 0.25f, 0.18f, 20f, orange);
            P(t, PrimitiveType.Sphere, -0.09f, 0f, 0f, 0.06f, 0.06f, 0.06f, new Color(0.5f, 1f, 0.6f), glow: true);
            P(t, PrimitiveType.Sphere, 0.09f, 0f, 0f, 0.06f, 0.06f, 0.06f, new Color(0.5f, 1f, 0.6f), glow: true);
        }

        private static void NeonBolt(Transform t)
        {
            Color yellow = new(1f, 0.95f, 0.3f);
            Tube(t, 0.05f, 0.22f, 0.3f, -25f, yellow);
            Tube(t, 0f, 0.05f, 0.22f, 90f, yellow);
            Tube(t, -0.04f, -0.15f, 0.32f, -25f, yellow);
        }

        private static void NeonCup(Transform t)
        {
            Color mint = new(0.4f, 1f, 0.75f);
            Tube(t, -0.17f, -0.05f, 0.4f, -10f, mint);
            Tube(t, 0.17f, -0.05f, 0.4f, 10f, mint);
            Tube(t, 0f, -0.25f, 0.28f, 90f, mint);
            Tube(t, 0f, 0.15f, 0.44f, 90f, mint);
            Tube(t, 0.27f, -0.03f, 0.16f, 0f, new Color(1f, 0.55f, 0.8f)); // handle
            Tube(t, -0.05f, 0.3f, 0.14f, 30f, new Color(1f, 1f, 1f)); // steam
            Tube(t, 0.06f, 0.32f, 0.14f, -30f, new Color(1f, 1f, 1f));
        }

        private static void NeonRainbow(Transform t)
        {
            Color[] bands = { new(1f, 0.3f, 0.35f), new(1f, 0.75f, 0.3f), new(0.4f, 0.95f, 0.5f), new(0.4f, 0.7f, 1f) };
            for (int b = 0; b < bands.Length; b++)
            {
                float radius = 0.36f - b * 0.07f;
                for (int i = 0; i < 6; i++)
                {
                    float a = 15f + i * 30f;
                    Vector2 dir = new(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    Tube(t, dir.x * radius, dir.y * radius - 0.15f, radius * 0.55f, a + 90f, bands[b]);
                }
            }
        }

        // ---------------------------------------------------------------- more plants

        private static void Palm(Transform t)
        {
            PotBase(t, 0.6f, Pot);
            for (int i = 0; i < 4; i++)
                P(t, PrimitiveType.Cylinder, 0.02f * i, 0.62f + i * 0.35f, 0f, 0.13f - i * 0.015f, 0.18f, 0.13f - i * 0.015f, new Color(0.6f, 0.45f, 0.3f), 0f, 0f, -3f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                P(t, PrimitiveType.Sphere, dir.x * 0.38f, 1.95f, dir.z * 0.38f, 0.22f, 0.04f, 0.75f, i % 2 == 0 ? Leaf : DarkLeaf, 20f, a, 0f);
            }
        }

        private static void Tulips(Transform t)
        {
            PotBase(t, 0.4f, new Color(0.95f, 0.85f, 0.4f));
            Color[] petals = { new(1f, 0.4f, 0.5f), new(1f, 0.85f, 0.3f), new(0.85f, 0.45f, 0.95f) };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.1f;
                P(t, PrimitiveType.Cylinder, x, 0.62f, 0f, 0.025f, 0.18f, 0.025f, Leaf);
                P(t, PrimitiveType.Capsule, x, 0.86f + (i % 2) * 0.04f, 0f, 0.1f, 0.08f, 0.1f, petals[i]);
            }
        }

        private static void Fern(Transform t)
        {
            PotBase(t, 0.5f, new Color(0.5f, 0.55f, 0.65f));
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                P(t, PrimitiveType.Sphere, dir.x * 0.25f, 0.6f, dir.z * 0.25f, 0.14f, 0.04f, 0.6f, i % 2 == 0 ? Leaf : DarkLeaf, -35f, a, 0f);
            }
        }

        private static void Succulents(Transform t)
        {
            Color[] pots = { Cream, Pot, new(0.55f, 0.75f, 0.85f) };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.32f;
                P(t, PrimitiveType.Cylinder, x, 0.1f, 0f, 0.26f, 0.1f, 0.26f, pots[i]);
                for (int leaf = 0; leaf < 5; leaf++)
                {
                    float a = leaf * 72f;
                    Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                    P(t, PrimitiveType.Sphere, x + dir.x * 0.05f, 0.26f, dir.z * 0.05f, 0.07f, 0.12f, 0.07f, new Color(0.45f, 0.7f, 0.55f), -30f, a, 0f);
                }
            }
        }

        // ---------------------------------------------------------------- more misc

        private static void Gumball(Transform t)
        {
            Color red = new(0.85f, 0.25f, 0.25f);
            P(t, PrimitiveType.Cylinder, 0f, 0.3f, 0f, 0.35f, 0.3f, 0.35f, red);
            P(t, PrimitiveType.Cube, 0f, 0.68f, 0f, 0.4f, 0.16f, 0.4f, red);
            P(t, PrimitiveType.Sphere, 0f, 1.02f, 0f, 0.52f, 0.52f, 0.52f, new Color(0.85f, 0.95f, 1f));
            Color[] gum = { new(1f, 0.4f, 0.5f), new(1f, 0.85f, 0.3f), new(0.4f, 0.8f, 1f), new(0.5f, 0.9f, 0.5f) };
            for (int i = 0; i < 10; i++)
            {
                float a = i * 1.3f;
                P(t, PrimitiveType.Sphere, Mathf.Cos(a) * 0.13f, 0.88f + (i % 3) * 0.1f, Mathf.Sin(a) * 0.13f, 0.1f, 0.1f, 0.1f, gum[i % gum.Length]);
            }
            P(t, PrimitiveType.Sphere, 0f, 1.3f, 0f, 0.12f, 0.08f, 0.12f, red);
        }

        private static void Arcade(Transform t)
        {
            Color body = new(0.35f, 0.25f, 0.6f);
            P(t, PrimitiveType.Cube, 0f, 0.85f, 0f, 0.75f, 1.7f, 0.7f, body);
            P(t, PrimitiveType.Cube, 0f, 1.25f, 0.33f, 0.58f, 0.45f, 0.04f, new Color(0.3f, 0.9f, 1f), glow: true);
            P(t, PrimitiveType.Cube, 0f, 0.9f, 0.42f, 0.7f, 0.08f, 0.25f, Charcoal);
            P(t, PrimitiveType.Cylinder, -0.15f, 0.98f, 0.42f, 0.05f, 0.05f, 0.05f, Charcoal);
            P(t, PrimitiveType.Sphere, -0.15f, 1.05f, 0.42f, 0.08f, 0.08f, 0.08f, new Color(1f, 0.3f, 0.35f));
            P(t, PrimitiveType.Cylinder, 0.1f, 0.95f, 0.45f, 0.07f, 0.02f, 0.07f, new Color(1f, 0.85f, 0.3f));
            P(t, PrimitiveType.Cylinder, 0.22f, 0.95f, 0.45f, 0.07f, 0.02f, 0.07f, new Color(0.4f, 0.9f, 0.5f));
            P(t, PrimitiveType.Cube, 0f, 1.62f, 0.3f, 0.72f, 0.14f, 0.1f, new Color(1f, 0.5f, 0.75f), glow: true);
        }

        private static void Jukebox(Transform t)
        {
            P(t, PrimitiveType.Cube, 0f, 0.55f, 0f, 0.85f, 1.1f, 0.55f, new Color(0.6f, 0.2f, 0.25f));
            P(t, PrimitiveType.Cylinder, 0f, 1.1f, 0f, 0.85f, 0.28f, 0.55f, new Color(0.6f, 0.2f, 0.25f), 90f, 0f, 0f);
            P(t, PrimitiveType.Cylinder, 0f, 1.1f, 0.02f, 0.7f, 0.29f, 0.45f, new Color(1f, 0.8f, 0.35f), 90f, 0f, 0f, glow: true);
            P(t, PrimitiveType.Cube, 0f, 0.75f, 0.28f, 0.6f, 0.3f, 0.02f, new Color(0.85f, 0.95f, 1f));
            P(t, PrimitiveType.Cube, 0f, 0.3f, 0.28f, 0.6f, 0.25f, 0.02f, Charcoal);
            P(t, PrimitiveType.Cube, -0.36f, 0.6f, 0.28f, 0.05f, 1f, 0.03f, new Color(0.4f, 0.9f, 1f), glow: true);
            P(t, PrimitiveType.Cube, 0.36f, 0.6f, 0.28f, 0.05f, 1f, 0.03f, new Color(1f, 0.45f, 0.8f), glow: true);
        }

        private static void Balloon(Transform t)
        {
            P(t, PrimitiveType.Cylinder, 0f, 0.05f, 0f, 0.3f, 0.05f, 0.3f, Charcoal);
            P(t, PrimitiveType.Cylinder, 0f, 0.95f, 0f, 0.015f, 0.85f, 0.015f, Cream);
            P(t, PrimitiveType.Sphere, 0f, 1.95f, 0f, 0.5f, 0.55f, 0.5f, new Color(0.88f, 0.75f, 0.6f));
            P(t, PrimitiveType.Sphere, -0.1f, 2.0f, 0.22f, 0.07f, 0.08f, 0.04f, Charcoal);
            P(t, PrimitiveType.Sphere, 0.1f, 2.0f, 0.22f, 0.07f, 0.08f, 0.04f, Charcoal);
            P(t, PrimitiveType.Sphere, 0f, 1.88f, 0.24f, 0.08f, 0.03f, 0.02f, new Color(1f, 0.5f, 0.6f));
            for (int i = 0; i < 4; i++)
                P(t, PrimitiveType.Sphere, (i - 1.5f) * 0.12f, 1.62f, 0.12f, 0.1f, 0.1f, 0.1f, new Color(0.22f, 0.14f, 0.1f));
        }
    }
}
