using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Upgrades
{
    /// <summary>
    /// A square in front of the upgrade desk. While the player stands in it, the upgrade
    /// panel opens (the UI checks <see cref="AnyPlayerInside"/>).
    /// </summary>
    public class UpgradeZone : MonoBehaviour
    {
        private static readonly List<UpgradeZone> active = new();

        [SerializeField, Tooltip("Center of the zone, relative to the desk.")]
        private Vector3 offset = new(0f, 0f, 1.4f);

        [SerializeField, Tooltip("Half the zone's width in meters.")]
        private float halfSize = 1f;

        private void OnEnable() => active.Add(this);
        private void OnDisable() => active.Remove(this);

        /// <summary>True while the player stands in any upgrade zone.</summary>
        public static bool AnyPlayerInside()
        {
            UpgradeManager manager = UpgradeManager.Instance;
            if (manager == null || manager.Wallet == null) return false;

            Vector3 player = manager.Wallet.transform.position;
            foreach (UpgradeZone zone in active)
            {
                Vector3 delta = player - zone.transform.TransformPoint(zone.offset);
                if (Mathf.Abs(delta.x) <= zone.halfSize && Mathf.Abs(delta.z) <= zone.halfSize) return true;
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.7f, 0.4f, 1f);
            Gizmos.DrawWireCube(transform.TransformPoint(offset), new Vector3(halfSize * 2f, 0.05f, halfSize * 2f));
        }
    }
}
