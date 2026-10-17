using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.EditorTools
{
    /// <summary>
    /// Which 3D model to use for each thing in the shop. Any slot left empty keeps its
    /// gray-box shape. Fill the slots by hand, or with Bubble Tea > Art > Auto-Fill From Art Folder,
    /// then rebuild the scene. Lives at Assets/_Game/Data/ArtSet.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Bubble Tea/Art Set", fileName = "ArtSet")]
    public class ArtSet : ScriptableObject
    {
        /// <summary>Every slot the scene builder knows about, in the order shown in the Inspector.</summary>
        public static readonly string[] SlotNames =
        {
            "Player", "Customer", "Barista", "Cashier", "Cleaner", "Car",
            "Counter", "DriveThru", "TeaBrewer", "ToppingBar", "TaroStation", "FruitBar", "UpgradeDesk",
            "Cup",
            "Planter", "MenuBoard", "Statue", "Table", "BusStop",
            // The Matcha House's own bars and green-uniformed staff.
            "MatchaPlayer", "MatchaBarista", "MatchaCashier", "MatchaCleaner",
            "WhiskBar", "FoamBar", "HojichaBar", "StrawberryBar", "YuzuBar", "SenchaBar", "TenjuBar",
            // The Night Market's fruit bars and indigo-uniformed staff.
            "NightPlayer", "NightBarista", "NightCashier", "NightCleaner",
            "WinterMelonBar", "PassionFruitBar", "LycheeBar", "PeachBar",
            // Decor bought on pads.
            "BambooPlant", "PaperLanterns", "LuckyCat", "KoiPond", "SakuraTree",
        };

        [SerializeField, Tooltip("One entry per thing in the shop.")]
        private List<ArtSlot> slots = new();

        [SerializeField, Tooltip("Plants and other decorations placed along the walls. Leave empty for none.")]
        private List<GameObject> decorations = new();

        public IReadOnlyList<GameObject> Decorations => decorations;
        public List<ArtSlot> Slots => slots;

        /// <summary>The slot called <paramref name="slotName"/>, or null if it has no model.</summary>
        public ArtSlot Get(string slotName)
        {
            foreach (ArtSlot slot in slots)
            {
                if (slot != null && slot.name == slotName && slot.model != null) return slot;
            }
            return null;
        }

        /// <summary>Makes sure every known slot has an entry (handy after adding new slots).</summary>
        public void EnsureSlots()
        {
            foreach (string slotName in SlotNames)
            {
                if (slots.Exists(s => s != null && s.name == slotName)) continue;
                slots.Add(new ArtSlot { name = slotName });
            }
        }

        public void SetDecorations(List<GameObject> models)
        {
            decorations = models;
        }
    }

    /// <summary>A model plus small fixes for how it sits: turn it, nudge it, or make it bigger.</summary>
    [Serializable]
    public class ArtSlot
    {
        [Tooltip("Which thing this is (don't rename).")]
        public string name;

        [Tooltip("The model (an .fbx, .obj or prefab). Empty keeps the gray box.")]
        public GameObject model;

        [Tooltip("Turn the model if it faces the wrong way. Stations should face +Z (toward where the player stands).")]
        public float rotationY;

        [Tooltip("Multiplies the automatic size. 1 = fit the gray box exactly.")]
        public float sizeMultiplier = 1f;

        [Tooltip("Moves the model after fitting, in meters.")]
        public Vector3 offset;
    }
}
