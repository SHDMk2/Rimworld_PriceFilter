using UnityEngine;
using Verse;

namespace PriceFilter
{
    public class PriceFilterSettings : ModSettings
    {
        public PFLanguage language = PFLanguage.English;
        public bool enabled = true;
        public float sliderMax = 5000f;   // slider end value (beyond that: type it in, or "no limit")

        public override void ExposeData()
        {
            Scribe_Values.Look(ref language, "language", PFLanguage.English);
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref sliderMax, "sliderMax", 5000f);
        }
    }

    public class PriceFilterMod : Mod
    {
        public static PriceFilterSettings Settings;

        public PriceFilterMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<PriceFilterSettings>();
        }

        public override string SettingsCategory() => "Price Filter";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var list = new Listing_Standard();
            list.Begin(inRect);

            Rect langRow = list.GetRect(30f);
            Widgets.Label(langRow.LeftHalf(), Strings.T("SettingLanguage"));
            if (Widgets.ButtonText(langRow.RightHalf().LeftPartPixels(160f), Strings.LanguageName(Settings.language)))
            {
                var options = new System.Collections.Generic.List<FloatMenuOption>();
                foreach (PFLanguage lang in System.Enum.GetValues(typeof(PFLanguage)))
                {
                    PFLanguage l = lang;
                    options.Add(new FloatMenuOption(Strings.LanguageName(l), () => Settings.language = l));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            list.Gap();

            list.CheckboxLabeled(Strings.T("SettingEnabled"), ref Settings.enabled, Strings.T("SettingEnabledTip"));
            list.Gap();
            list.Label(Strings.T("SettingSliderMax", Settings.sliderMax.ToStringMoney("F0")));
            float v = list.Slider(Settings.sliderMax, 100f, 50000f);
            Settings.sliderMax = v < 1000f ? Mathf.Round(v / 50f) * 50f : Mathf.Round(v / 500f) * 500f;
            list.End();
        }
    }
}
