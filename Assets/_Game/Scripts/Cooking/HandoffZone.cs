using System;
using UnityEngine;

namespace BubbleTea.Cooking
{
    /// <summary>
    /// A square on the floor next to a station. While a carrier stands in it, the owner
    /// gets a callback every few moments to swap items with them.
    /// </summary>
    [Serializable]
    public class HandoffZone
    {
        [SerializeField, Tooltip("Center of the zone, relative to the station.")]
        private Vector3 offset = new(0f, 0f, 1.4f);

        [SerializeField, Tooltip("Half the zone's width in meters.")]
        private float halfSize = 1f;

        [SerializeField, Tooltip("Seconds between item hand-offs.")]
        private float secondsPerHandoff = 0.15f;

        private float timer;

        /// <summary>Middle of the zone in world space, where a worker should stand.</summary>
        public Vector3 Center(Transform owner) => owner.TransformPoint(offset);

        /// <summary>True if <paramref name="point"/> is inside the zone (seen from above).</summary>
        public bool Contains(Transform owner, Vector3 point)
        {
            Vector3 delta = point - owner.TransformPoint(offset);
            return Mathf.Abs(delta.x) <= halfSize && Mathf.Abs(delta.z) <= halfSize;
        }

        /// <summary>Calls <paramref name="handoff"/> for the first carrier in the zone when it's time.</summary>
        public void Tick(Transform owner, Func<ItemCarrier, bool> handoff)
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            Vector3 center = owner.TransformPoint(offset);
            var carriers = ItemCarrier.All;
            for (int i = 0; i < carriers.Count; i++)
            {
                Vector3 delta = carriers[i].transform.position - center;
                if (Mathf.Abs(delta.x) > halfSize || Mathf.Abs(delta.z) > halfSize) continue;
                if (handoff(carriers[i]))
                {
                    timer = secondsPerHandoff;
                    return;
                }
            }
        }

        public void DrawGizmo(Transform owner)
        {
            Gizmos.color = new Color(0.3f, 0.7f, 1f);
            Gizmos.DrawWireCube(owner.TransformPoint(offset), new Vector3(halfSize * 2f, 0.05f, halfSize * 2f));
        }
    }
}
