using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubbleTea.Core
{
    /// <summary>
    /// Game-wide numbers that upgrades change, such as walking speed. Whoever owns the
    /// upgrades writes them with <see cref="Set"/>; the player, stations and counter read
    /// them with <see cref="Get"/>. Nothing here knows about any gameplay module.
    /// </summary>
    public static class Stats
    {
        private static readonly Dictionary<StatId, float> values = new();

        /// <summary>Raised with the stat that changed.</summary>
        public static event Action<StatId> Changed;

        /// <summary>The stat's current value, or <paramref name="fallback"/> if nothing has set it.</summary>
        public static float Get(StatId stat, float fallback) => values.TryGetValue(stat, out float value) ? value : fallback;

        public static void Set(StatId stat, float value)
        {
            values[stat] = value;
            Changed?.Invoke(stat);
        }

        // The project has domain reload turned off, so static data survives between Play
        // sessions unless we clear it ourselves when Play starts.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            values.Clear();
            Changed = null;
        }
    }
}
