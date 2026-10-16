using System;
using System.Collections.Generic;
using BubbleTea.Cooking;
using BubbleTea.Customers;
using BubbleTea.Travel;
using BubbleTea.Upgrades;
using UnityEngine;

namespace BubbleTea.Building
{
    /// <summary>
    /// Something the player can move or turn: bars and decor (tap them), the trash can and the staff
    /// sofa (tap them too), and the upgrade desk (its MOVE button is in the Upgrades menu). Pads add
    /// this when they build things. Where it was moved to is kept in the save file (by its ID), so it
    /// stays put next time.
    /// </summary>
    public class Movable : MonoBehaviour
    {
        private static readonly List<Movable> active = new();

        /// <summary>Every movable thing in the open shop.</summary>
        public static IReadOnlyList<Movable> All => active;

        /// <summary>Raised when the player puts something in a new spot (so the game saves).</summary>
        public static event Action Moved;

        /// <summary>The save ID of the pad that built it.</summary>
        public string Id { get; private set; }

        /// <summary>False for things moved another way (the upgrade desk moves from its menu, since a tap opens it).</summary>
        public bool TapToMove { get; private set; } = true;

        /// <summary>True once it stands somewhere other than where its pad built it.</summary>
        public bool HasMoved { get; private set; }

        /// <summary>
        /// Makes a just-built thing movable if it's the kind that can be moved: bars and decor, and the
        /// upgrade desk (from its menu). Counters, tables and travel spots stay put.
        /// </summary>
        public static void AddTo(GameObject built, string id)
        {
            if (built == null) return;
            if (built.GetComponentInChildren<ServingCounter>(true) != null) return;
            if (built.GetComponentInChildren<ShopTravel>(true) != null) return;
            if (built.GetComponentInChildren<SeatingTable>(true) != null) return;
            if (built.GetComponentInChildren<UpgradeZone>(true) != null) Make(built, id, tapToMove: false);
            else if (built.GetComponentInChildren<Station>(true) != null || built.GetComponentInChildren<ShopDecor>(true) != null)
                Make(built, id, tapToMove: true);
        }

        /// <summary>Makes something movable, saved under this ID (unique in the shop).</summary>
        public static Movable Make(GameObject thing, string id, bool tapToMove)
        {
            if (thing.TryGetComponent(out Movable movable)) return movable;
            movable = thing.AddComponent<Movable>();
            movable.Id = id;
            movable.TapToMove = tapToMove;
            return movable;
        }

        /// <summary>The movable thing built by the pad with this save ID, or null.</summary>
        public static Movable Find(string id)
        {
            foreach (Movable movable in active)
            {
                if (movable != null && movable.Id == id) return movable;
            }
            return null;
        }

        /// <summary>Puts it in a spot (from the save file, or where the player just placed it).</summary>
        public void Place(Vector3 position, float yaw, bool byPlayer)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            HasMoved = true;
            if (byPlayer) Moved?.Invoke();
        }

        /// <summary>False (with the reason) while it can't be moved, like a table with customers at it.</summary>
        public bool CanMoveNow(out string reason)
        {
            reason = null;
            BubbleTea.Staff.StaffSofa sofa = GetComponentInChildren<BubbleTea.Staff.StaffSofa>();
            if (sofa != null && sofa.InUse)
            {
                reason = "Wait until nobody is taking a break on the sofa.";
                return false;
            }
            return true;
        }

        /// <summary>The box it takes up in the world (from its colliders, or its models if it has none).</summary>
        public Bounds Footprint()
        {
            bool any = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (Collider part in GetComponentsInChildren<Collider>())
            {
                if (part.isTrigger || !part.enabled) continue;
                if (any) bounds.Encapsulate(part.bounds);
                else bounds = part.bounds;
                any = true;
            }
            if (!any)
            {
                foreach (Renderer part in GetComponentsInChildren<Renderer>())
                {
                    if (part.GetComponentInParent<NameTag>() != null || part is ParticleSystemRenderer) continue;
                    if (any) bounds.Encapsulate(part.bounds);
                    else bounds = part.bounds;
                    any = true;
                }
            }
            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 0.5f);
            size.z = Mathf.Max(size.z, 0.5f);
            bounds.size = size;
            return bounds;
        }

        private void OnEnable()
        {
            active.Add(this);
            MoveMode.Ensure();
        }

        private void OnDisable() => active.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            active.Clear();
            Moved = null;
        }
    }
}
