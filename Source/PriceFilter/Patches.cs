using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace PriceFilter
{
    [StaticConstructorOnStartup]
    public static class PriceFilterInit
    {
        static PriceFilterInit()
        {
            new Harmony("com.shdmk2.pricefilter").PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[PriceFilter] Initialized.");
        }
    }

    /// <summary>Save / load with the game (bills, stockpiles...).</summary>
    [HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.ExposeData))]
    public static class Patch_ThingFilter_ExposeData
    {
        public static void Postfix(ThingFilter __instance)
        {
            FloatRange range = PriceRanges.Get(__instance);
            Scribe_Values.Look(ref range, "pf_allowedMarketValue", PriceRanges.Default);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                PriceRanges.Set(__instance, range);
        }
    }

    /// <summary>Copy/paste settings, bill duplication, creation from a default filter.</summary>
    [HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.CopyAllowancesFrom))]
    public static class Patch_ThingFilter_CopyAllowancesFrom
    {
        public static void Postfix(ThingFilter __instance, ThingFilter other)
        {
            if (other != null)
                PriceRanges.Set(__instance, PriceRanges.Get(other));
        }
    }

    /// <summary>The filtering itself: unit market value out of range → rejected.</summary>
    [HarmonyPatch(typeof(ThingFilter), nameof(ThingFilter.Allows), new[] { typeof(Thing) })]
    public static class Patch_ThingFilter_Allows
    {
        public static void Postfix(ThingFilter __instance, Thing t, ref bool __result)
        {
            if (!__result || t == null || !PriceFilterMod.Settings.enabled)
                return;
            if (!PriceRanges.TryGetActive(__instance, out FloatRange range))
                return;

            Thing inner = t.GetInnerIfMinified();
            float value = PriceRanges.MarketValueOf(inner);
            if (value < range.min - 0.001f || (range.max < PriceRanges.Unbounded && value > range.max + 0.001f))
                __result = false;
        }
    }

    /// <summary>
    /// Inserts our slider right after the hit points / quality / mental break sliders,
    /// before the category tree, inside the same scroll view.
    /// </summary>
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public static class Patch_ThingFilterUI_DoThingFilterConfigWindow
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = instructions.ToList();
            MethodInfo hpMethod = AccessTools.Method(typeof(ThingFilterUI), "DrawHitPointsFilterConfig");
            MethodInfo ourMethod = AccessTools.Method(typeof(PriceFilterUI), nameof(PriceFilterUI.DrawPriceFilterConfig));

            // Vanilla pattern: ldloca y ; ldloca viewRect ; call Rect.get_width ; ldarg.2 ; call DrawHitPointsFilterConfig
            int hpCall = code.FindIndex(c => c.Calls(hpMethod));
            if (hpCall < 4 || code[hpCall - 4].opcode != OpCodes.Ldloca_S && code[hpCall - 4].opcode != OpCodes.Ldloca)
            {
                Log.Error("[PriceFilter] DoThingFilterConfigWindow: IL pattern not found, price slider not shown.");
                return code;
            }
            CodeInstruction yAddr = code[hpCall - 4];
            CodeInstruction viewRectAddr = code[hpCall - 3];
            CodeInstruction getWidth = code[hpCall - 2];
            LocalBuilder yLocal = yAddr.operand as LocalBuilder;

            // First by-value read of y ("float num2 = y;") = end of the special sliders.
            int insertAt = -1;
            for (int i = hpCall + 1; i < code.Count; i++)
            {
                if (yLocal != null ? code[i].IsLdloc(yLocal) : (code[i].IsLdloc() && Equals(code[i].operand, yAddr.operand)))
                {
                    insertAt = i;
                    break;
                }
            }
            if (insertAt < 0)
            {
                Log.Error("[PriceFilter] DoThingFilterConfigWindow: read of y not found, price slider not shown.");
                return code;
            }

            var first = new CodeInstruction(yAddr.opcode, yAddr.operand);
            code[insertAt].MoveLabelsTo(first);
            code.InsertRange(insertAt, new[]
            {
                first,                                                     // ref y
                new CodeInstruction(viewRectAddr.opcode, viewRectAddr.operand),
                new CodeInstruction(getWidth.opcode, getWidth.operand),    // viewRect.width
                new CodeInstruction(OpCodes.Ldarg_2),                      // filter
                new CodeInstruction(OpCodes.Ldarg_3),                      // parentFilter
                new CodeInstruction(OpCodes.Call, ourMethod),
            });
            return code;
        }
    }
}
