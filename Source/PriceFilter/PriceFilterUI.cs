using UnityEngine;
using Verse;

namespace PriceFilter
{
    public static class PriceFilterUI
    {
        private const int SliderId = 715382904;
        private const float SliderHeight = 32f;
        private const float FieldsHeight = 24f;

        // Input buffers: tied to the displayed filter, refreshed when the range changes elsewhere (slider, paste...).
        private static ThingFilter bufferFilter;
        private static FloatRange bufferRange;
        private static string minBuffer = "";
        private static string maxBuffer = "";

        /// <summary>Called by the transpiler inside ThingFilterUI.DoThingFilterConfigWindow.</summary>
        public static void DrawPriceFilterConfig(ref float y, float width, ThingFilter filter, ThingFilter parentFilter)
        {
            if (filter == null || !PriceFilterMod.Settings.enabled)
                return;

            float sliderMax = Mathf.Max(10f, PriceFilterMod.Settings.sliderMax);
            FloatRange range = PriceRanges.Get(filter);

            // --- Slider (non-linear scale: precision on small values) ---
            Rect sliderRect = new Rect(20f, y, width - 20f, SliderHeight);
            FloatRange pos = new FloatRange(ToSlider(range.min, sliderMax), ToSlider(range.max, sliderMax));
            FloatRange oldPos = pos;

            Rect labelRect = sliderRect;
            labelRect.xMin += 8f;
            labelRect.xMax -= 8f;
            labelRect.yMin -= 2f;
            GUI.color = new Color(0.6f, 0.6f, 0.6f); // Widgets.RangeControlTextColor (private)
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, Strings.T("MarketValueRange", Format(range.min), Format(range.max)));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;

            // Vanilla label hidden (it would show the 0..1 slider position).
            Widgets.FloatRange(sliderRect, SliderId, ref pos, 0f, 1f, null, ToStringStyle.FloatTwo, 0f, GameFont.Small, Color.clear);
            if (pos != oldPos)
            {
                float newMin = pos.min != oldPos.min ? FromSlider(pos.min, sliderMax) : range.min;
                float newMax = pos.max != oldPos.max ? FromSlider(pos.max, sliderMax) : range.max;
                range = new FloatRange(newMin, Mathf.Max(newMin, newMax));
                PriceRanges.Set(filter, range);
            }
            TooltipHandler.TipRegion(sliderRect, Strings.T("Tooltip"));
            y += SliderHeight;

            // --- Exact input ---
            if (bufferFilter != filter || bufferRange != range)
            {
                bufferFilter = filter;
                bufferRange = range;
                minBuffer = range.min <= 0f ? "" : Mathf.RoundToInt(range.min).ToString();
                maxBuffer = range.max >= PriceRanges.Unbounded ? "" : Mathf.RoundToInt(range.max).ToString();
            }

            float x = 28f;
            float fieldW = 70f;
            float lblW = 40f;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(x, y, lblW, FieldsHeight), Strings.T("Min"));
            Text.Anchor = TextAnchor.UpperLeft;
            x += lblW + 4f;
            string newMinBuf = Widgets.TextField(new Rect(x, y, fieldW, FieldsHeight), minBuffer);
            x += fieldW + 10f;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(x, y, lblW, FieldsHeight), Strings.T("Max"));
            Text.Anchor = TextAnchor.UpperLeft;
            x += lblW + 4f;
            Rect maxRect = new Rect(x, y, fieldW, FieldsHeight);
            string newMaxBuf = Widgets.TextField(maxRect, maxBuffer);
            if (newMaxBuf.NullOrEmpty())
            {
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(maxRect, "∞");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            x += fieldW + 10f;

            if (newMinBuf != minBuffer || newMaxBuf != maxBuffer)
            {
                minBuffer = newMinBuf;
                maxBuffer = newMaxBuf;
                float min = ParseOr(minBuffer, 0f);
                float max = ParseOr(maxBuffer, PriceRanges.Unbounded);
                // Only apply consistent input; otherwise wait for the user to finish typing.
                if (min <= max)
                {
                    range = new FloatRange(min, max);
                    PriceRanges.Set(filter, range);
                    bufferRange = PriceRanges.Get(filter);
                }
            }

            Rect resetRect = new Rect(x, y, FieldsHeight, FieldsHeight);
            if (!PriceRanges.IsDefault(range))
                TooltipHandler.TipRegion(resetRect, Strings.T("Reset"));
            if (!PriceRanges.IsDefault(range) && Widgets.ButtonText(resetRect, "↺"))
            {
                PriceRanges.Set(filter, PriceRanges.Default);
                bufferFilter = null;
            }

            y += FieldsHeight;
            y += 5f;
            Text.Font = GameFont.Small;
        }

        // s ∈ [0,1] ↔ value ∈ [0, sliderMax] as s³ ; s = 1 → no limit.
        private static float ToSlider(float value, float sliderMax)
        {
            if (value >= PriceRanges.Unbounded) return 1f;
            return Mathf.Clamp01(Mathf.Pow(Mathf.Clamp(value, 0f, sliderMax) / sliderMax, 1f / 3f));
        }

        private static float FromSlider(float s, float sliderMax)
        {
            if (s >= 0.999f) return PriceRanges.Unbounded;
            if (s <= 0.001f) return 0f;
            return RoundNice(sliderMax * s * s * s);
        }

        private static float RoundNice(float v)
        {
            float step = v < 100f ? 1f : v < 1000f ? 5f : v < 10000f ? 50f : 500f;
            return Mathf.Round(v / step) * step;
        }

        private static string Format(float v)
        {
            if (v >= PriceRanges.Unbounded) return "∞";
            return v.ToStringMoney("F0");
        }

        private static float ParseOr(string s, float fallback)
        {
            if (s.NullOrEmpty()) return fallback;
            s = s.Trim().Replace("∞", "");
            if (s.Length == 0) return fallback;
            return float.TryParse(s, out float v) && v >= 0f ? v : fallback;
        }
    }
}
