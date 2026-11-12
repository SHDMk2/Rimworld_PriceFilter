# Price Filter — build

RimWorld 1.7 runs on .NET 10 (Waylith engine): the mod targets `net10.0`.

```bash
cd Source/PriceFilter
dotnet build -c Release
```

The DLL is written directly to `Assemblies/`. References: `Krafs.Rimworld.Ref 1.7.5000-unstable`
and `Lib.Harmony` (provided at runtime by the Harmony mod, not copied).

Clean upload copy (no .git/bin/obj): `./make-release.sh [destination]`.

## Structure

| File | Role |
|---|---|
| `PriceRanges.cs` | market value range per `ThingFilter` (ConditionalWeakTable) + value cache (250 ticks) |
| `Patches.cs` | Harmony: `ExposeData` (saves `pf_allowedMarketValue`), `CopyAllowancesFrom` (copy/paste, duplication), `Allows(Thing)` (filtering), transpiler on `ThingFilterUI.DoThingFilterConfigWindow` (inserts the slider after hit points/quality) |
| `PriceFilterUI.cs` | two-handle slider (cubic scale, far right = ∞) + min/max fields + ↺ reset button |
| `PriceFilterMod.cs` | settings: language, enable, slider end value |
| `Strings.cs` | UI strings (English / French), selected by the mod's language setting, not the game's |

The filter applies wherever the game uses `ThingFilter.Allows(Thing)`: bills, storage, apparel policies.
Tested value = unit `MarketValue` of the item (inner thing if minified).

Adding a language: add a value to `PFLanguage`, a dictionary in `Strings.cs`, and a name in `LanguageName`.
