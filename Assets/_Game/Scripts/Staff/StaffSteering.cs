using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Staff
{
    /// <summary>
    /// Walking for workers that steers around solid things (bars, counters, desks, tables, walls)
    /// instead of walking straight through them. Each step it looks a little way ahead; if
    /// something solid is in the way it tries turning further and further left and right until
    /// it finds a clear direction. Once it picks a side to go round, it sticks to that side for a
    /// moment (so it doesn't dither left-right in front of a table), and swaps sides if it stops
    /// making progress. Triggers and people (the player, other workers, customers) don't count.
    /// </summary>
    public static class StaffSteering
    {
        private const float Radius = 0.3f;     // about a person's half-width
        private const float LookAhead = 1.2f;  // meters checked in front each step
        private const float FinalStretch = 0.9f; // this close, walk straight in (spots sit right against counters and bars)
        private static readonly float[] Turns = { 30f, 60f, 90f, 120f, 150f };
        private const float CommitSeconds = 1.2f;  // how long to keep going round the same side
        private const float StuckSeconds = 0.8f;   // no progress for this long: try the other side
        private static readonly RaycastHit[] hits = new RaycastHit[8];

        private class Detour
        {
            public float side;      // +1 = turn right of the target, -1 = left
            public float until;     // keep this side until then
            public Vector3 checkPoint;
            public float checkTime;
        }

        private static readonly Dictionary<Transform, Detour> detours = new();
        private static readonly float[] Right = { 1f }, Left = { -1f }, Both = { 1f, -1f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => detours.Clear();

        /// <summary>
        /// Moves <paramref name="mover"/> up to <paramref name="distance"/> meters toward the target, around obstacles.
        /// The last stretch is walked straight, since work spots are right up against the furniture.
        /// </summary>
        public static void Step(Transform mover, Vector3 target, float distance)
        {
            Vector3 from = mover.position;
            target.y = from.y;
            Vector3 toTarget = target - from;
            float remaining = toTarget.magnitude;
            if (remaining < 0.0001f) return;
            Vector3 direction = toTarget / remaining;
            float probe = Mathf.Min(LookAhead, remaining);

            if (remaining > FinalStretch && Blocked(mover, from, direction, probe))
            {
                if (!detours.TryGetValue(mover, out Detour detour))
                {
                    detour = new Detour { checkPoint = from, checkTime = Time.time };
                    detours[mover] = detour;
                }
                // Not getting anywhere: go round the other side instead.
                if (Time.time - detour.checkTime > StuckSeconds)
                {
                    if ((from - detour.checkPoint).sqrMagnitude < 0.25f && detour.side != 0f)
                    {
                        detour.side = -detour.side;
                        detour.until = Time.time + CommitSeconds;
                    }
                    detour.checkPoint = from;
                    detour.checkTime = Time.time;
                }
                if (Time.time > detour.until) detour.side = 0f;

                Vector3 chosen = Vector3.zero;
                foreach (float turn in Turns)
                {
                    // A committed side is tried alone; otherwise both, nearest the target first.
                    foreach (float side in detour.side > 0f ? Right : detour.side < 0f ? Left : Both)
                    {
                        Vector3 tryDirection = Quaternion.Euler(0f, turn * side, 0f) * direction;
                        if (Blocked(mover, from, tryDirection, LookAhead * 0.75f)) continue;
                        chosen = tryDirection;
                        if (detour.side == 0f)
                        {
                            detour.side = side;
                            detour.until = Time.time + CommitSeconds;
                        }
                        break;
                    }
                    if (chosen != Vector3.zero) break;
                }
                if (chosen != Vector3.zero) direction = chosen;
                else if (detour.side != 0f) detour.side = -detour.side; // boxed in on this side: try the other next step
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
