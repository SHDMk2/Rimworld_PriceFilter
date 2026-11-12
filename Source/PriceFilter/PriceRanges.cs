using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace PriceFilter
{
    /// <summary>
    /// Market value range attached to each ThingFilter (without modifying the vanilla class).
    /// Absent = no filtering.
    /// </summary>
    public static class PriceRanges
    {
        /// <summary>"No limit" upper bound.</summary>
        public const float Unbounded = 1e9f;

        public static readonly FloatRange Default = new FloatRange(0f, Unbounded);

        private class Box { public FloatRange range; }

        private static readonly ConditionalWeakTable<ThingFilter, Box> table = new ConditionalWeakTable<ThingFilter, Box>();

        public static bool IsDefault(FloatRange r) => r.min <= 0f && r.max >= Unbounded;

        public static FloatRange Get(ThingFilter filter)
        {
            return table.TryGetValue(filter, out Box box) ? box.range : Default;
        }

        public static bool TryGetActive(ThingFilter filter, out FloatRange range)
        {
            if (table.TryGetValue(filter, out Box box) && !IsDefault(box.range))
            {
                range = box.range;
                return true;
            }
            range = Default;
            return false;
        }

        public static void Set(ThingFilter filter, FloatRange range)
        {
            if (range.min < 0f) range.min = 0f;
            if (range.max > Unbounded) range.max = Unbounded;
            if (range.max < range.min) range.max = range.min;

            if (IsDefault(range))
            {
                table.Remove(filter);
                return;
            }
            table.GetOrCreateValue(filter).range = range;
        }

        // --- Market value cache (stat calculation is expensive for corpses/pawns) ---

        private const int CacheTicks = 250;
        private static readonly Dictionary<int, (int tick, float value)> valueCache = new Dictionary<int, (int, float)>();
        private static readonly object cacheLock = new object();

        public static float MarketValueOf(Thing t)
        {
            int now = GenTicks.TicksGame;
            int id = t.thingIDNumber;
            lock (cacheLock)
            {
                if (valueCache.TryGetValue(id, out var e) && now - e.tick < CacheTicks && now >= e.tick)
                    return e.value;
            }
            float v = t.MarketValue;
            lock (cacheLock)
            {
                if (valueCache.Count > 20000) valueCache.Clear();
                valueCache[id] = (now, v);
            }
            return v;
        }
    }
}
