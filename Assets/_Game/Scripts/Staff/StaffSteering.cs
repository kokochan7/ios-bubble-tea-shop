using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// Walking for workers that steers around solid things (bars, counters, desks, tables, walls)
    /// instead of walking straight through them. Each step it looks a little way ahead; if
    /// something solid is in the way it tries turning further and further left and right until
    /// it finds a clear direction. Triggers and people (the player, other workers, customers) don't count.
    /// </summary>
    public static class StaffSteering
    {
        private const float Radius = 0.3f;     // about a person's half-width
        private const float LookAhead = 1.2f;  // meters checked in front each step
        private static readonly float[] Turns = { 30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f };
        private static readonly RaycastHit[] hits = new RaycastHit[8];

        /// <summary>Moves <paramref name="mover"/> up to <paramref name="distance"/> meters toward the target, around obstacles.</summary>
        public static void Step(Transform mover, Vector3 target, float distance)
        {
            Vector3 from = mover.position;
            target.y = from.y;
            Vector3 toTarget = target - from;
            float remaining = toTarget.magnitude;
            if (remaining < 0.0001f) return;
            Vector3 direction = toTarget / remaining;
            float probe = Mathf.Min(LookAhead, remaining);

            if (Blocked(mover, from, direction, probe))
            {
                // Prefer turning to the side that keeps heading toward the target.
                foreach (float turn in Turns)
                {
                    Vector3 tryDirection = Quaternion.Euler(0f, turn, 0f) * direction;
                    if (Blocked(mover, from, tryDirection, LookAhead * 0.75f)) continue;
                    direction = tryDirection;
                    break;
                }
            }

            mover.position = Vector3.MoveTowards(from, from + direction * Mathf.Min(distance, remaining), distance);
        }

        private static bool Blocked(Transform mover, Vector3 from, Vector3 direction, float distance)
        {
            Vector3 origin = from + Vector3.up * 0.5f;
            int count = Physics.SphereCastNonAlloc(origin, Radius, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = hits[i].collider;
                if (other == null || other.transform.IsChildOf(mover)) continue;
                // People (the player's controller, capsule bodies) and anything moving aren't furniture.
                if (other.attachedRigidbody != null || other is CharacterController || other is CapsuleCollider) continue;
                if (hits[i].distance <= 0f) continue;           // already overlapping: let it walk out
                return true;
            }
            return false;
        }
    }
}
