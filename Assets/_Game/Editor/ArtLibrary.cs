using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Helps the scene builder swap gray boxes for real models. It loads the
    /// <see cref="ArtSet"/>, fits each model to the size of the gray box it replaces,
    /// and has a menu item that fills the Art Set by searching the art folder for
    /// models with matching names (written for Kenney's free kits, but any models work).
    /// </summary>
    public static class ArtLibrary
    {
        public const string ArtSetPath = "Assets/_Game/Data/ArtSet.asset";
        private const string ArtFolder = "Assets/_Game/Art";

        /// <summary>How a model is sized to its gray box.</summary>
        public enum Fit
        {
            /// <summary>Fit the footprint (width and depth): stations, cars.</summary>
            Footprint,

            /// <summary>Fit the height: characters, cups.</summary>
            Height,
        }

        // Words to look for in model file names, best match first. Names are compared in
        // lower case with spaces, dashes and underscores removed.
        private static readonly Dictionary<string, string[]> Keywords = new()
        {
            { "Player", new[] { "characterfemalea", "charactera", "characterfemale", "character" } },
            { "Customer", new[] { "charactermaleb", "characterb", "charactermale", "character" } },
            { "Runner", new[] { "characterfemalec", "characterc", "characterd", "character" } },
            { "Car", new[] { "sedan", "hatchback", "taxi", "suv", "van", "car" } },
            { "Counter", new[] { "kitchencabinet", "counter", "cabinet" } },
            { "DriveThru", new[] { "kitchenbar", "bar", "kitchencabinetdrawer", "cabinet" } },
            { "TeaBrewer", new[] { "kitchencoffeemachine", "coffeemachine", "kettle", "kitchenstove", "stove" } },
            { "ToppingBar", new[] { "kitchenblender", "blender", "kitchensink" } },
            { "TaroStation", new[] { "kitchenmicrowave", "microwave", "toaster", "kitchenstove" } },
            { "FruitBar", new[] { "kitchenfridge", "fridge", "refrigerator", "bowlfruit", "fruit" } },
            { "UpgradeDesk", new[] { "desk", "computerscreen", "computer", "table" } },
            { "Cup", new[] { "cuptea", "cupcoffee", "cup", "mug", "glass" } },
        };

        private static readonly string[] DecorationKeywords = { "pottedplant", "plant", "cactus", "flower" };

        private static ArtSet cached;

        /// <summary>The project's Art Set, created on first use.</summary>
        public static ArtSet Load()
        {
            if (cached != null) return cached;
            cached = AssetDatabase.LoadAssetAtPath<ArtSet>(ArtSetPath);
            if (cached == null)
            {
                cached = ScriptableObject.CreateInstance<ArtSet>();
                cached.EnsureSlots();
                AssetDatabase.CreateAsset(cached, ArtSetPath);
                AssetDatabase.SaveAssets();
            }
            return cached;
        }

        /// <summary>
        /// If the Art Set has a model for <paramref name="slotName"/>, places it under
        /// <paramref name="parent"/>, sized to <paramref name="targetSize"/> and standing on
        /// the parent's origin (plus <paramref name="localBase"/>), and returns it. Returns null if the slot is empty.
        /// </summary>
        public static GameObject Attach(string slotName, Transform parent, Vector3 targetSize, Fit fit,
            Vector3 localBase = default)
        {
            ArtSlot slot = Load().Get(slotName);
            if (slot == null) return null;

            var model = (GameObject)PrefabUtility.InstantiatePrefab(slot.model);
            if (model == null) model = Object.Instantiate(slot.model);
            model.name = "Model";

            // Measure it unscaled at the origin, keeping any rotation the model file came with
            // (Blender exports are often tipped 90 degrees to stand up in Unity).
            model.transform.SetParent(null, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, slot.rotationY, 0f) * slot.model.transform.localRotation;
            Bounds bounds = MeasureBounds(model);
            Vector3 size = bounds.size;

            float scale = 1f;
            if (fit == Fit.Height && size.y > 0.0001f)
            {
                scale = targetSize.y / size.y;
            }
            else if (size.x > 0.0001f && size.z > 0.0001f)
            {
                scale = Mathf.Min(targetSize.x / size.x, targetSize.z / size.z);
            }
            scale *= Mathf.Max(slot.sizeMultiplier, 0.01f);

            // Stand it on the floor, centered.
            Vector3 bottomCenter = new(bounds.center.x, bounds.min.y, bounds.center.z);
            model.transform.localScale = Vector3.one * scale;
            model.transform.localPosition = localBase - bottomCenter * scale + slot.offset;
            model.transform.SetParent(parent, false);

            foreach (Collider collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            return model;
        }

        /// <summary>Hides the gray-box parts that a model replaced, but keeps their colliders.</summary>
        public static void HideGrayBox(params GameObject[] parts)
        {
            foreach (GameObject part in parts)
            {
                if (part == null) continue;
                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            }
        }

        private static Bounds MeasureBounds(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        [MenuItem("Bubble Tea/Art/Auto-Fill From Art Folder")]
        public static void AutoFill()
        {
            ArtSet set = Load();
            set.EnsureSlots();
            List<GameObject> models = FindModels();
            if (models.Count == 0)
            {
                EditorUtility.DisplayDialog("No models found",
                    $"Put model files (.fbx, .obj, .glb) somewhere under {ArtFolder} first. See docs/ART.md.", "OK");
                return;
            }

            var filled = new List<string>();
            var used = new HashSet<GameObject>();
            foreach (ArtSlot slot in set.Slots)
            {
                if (slot == null || slot.model != null || !Keywords.TryGetValue(slot.name, out string[] words)) continue;
                GameObject match = FindBest(models, words, used);
                if (match == null) continue;
                slot.model = match;
                used.Add(match);
                filled.Add($"{slot.name}: {match.name}");
            }

            if (set.Decorations.Count == 0)
            {
                var decorations = new List<GameObject>();
                foreach (GameObject model in models)
                {
                    string key = Normalize(model.name);
                    foreach (string word in DecorationKeywords)
                    {
                        if (!key.Contains(word)) continue;
                        decorations.Add(model);
                        break;
                    }
                    if (decorations.Count >= 4) break;
                }
                set.SetDecorations(decorations);
                if (decorations.Count > 0) filled.Add($"Decorations: {decorations.Count}");
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            Selection.activeObject = set;
            Debug.Log(filled.Count > 0
                ? "Bubble Tea: art filled in.\n" + string.Join("\n", filled) + "\nNow run Bubble Tea > Build Prototype Scene."
                : "Bubble Tea: no new matches. Drag models into the Art Set slots by hand.");
        }

        [MenuItem("Bubble Tea/Art/Select Art Set")]
        public static void SelectArtSet()
        {
            ArtSet set = Load();
            set.EnsureSlots();
            EditorUtility.SetDirty(set);
            Selection.activeObject = set;
        }

        // Prefers exact-ish names: the earliest keyword wins, then the shortest file name.
        private static GameObject FindBest(List<GameObject> models, string[] words, HashSet<GameObject> used)
        {
            foreach (string word in words)
            {
                GameObject best = null;
                foreach (GameObject model in models)
                {
                    if (used.Contains(model)) continue;
                    string key = Normalize(model.name);
                    if (!key.Contains(word)) continue;
                    if (best == null || key.Length < Normalize(best.name).Length) best = model;
                }
                if (best != null) return best;
            }
            return null;
        }

        private static List<GameObject> FindModels()
        {
            var models = new List<GameObject>();
            if (!AssetDatabase.IsValidFolder(ArtFolder)) return models;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ArtFolder }))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (model != null) models.Add(model);
            }
            return models;
        }

        private static string Normalize(string name) =>
            Path.GetFileNameWithoutExtension(name).ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    }
}
