using System.Collections.Generic;

namespace PriceFilter
{
    public enum PFLanguage { English, French }

    /// <summary>
    /// Mod strings, chosen by the mod's own language setting (independent of the game language).
    /// </summary>
    public static class Strings
    {
        private static readonly Dictionary<string, string> en = new Dictionary<string, string>
        {
            ["MarketValueRange"] = "Market value: {0} - {1}",
            ["Tooltip"] = "Allowed market value per unit (as shown in the info card).\nFar right = no limit.",
            ["Min"] = "Min",
            ["Max"] = "Max",
            ["SettingLanguage"] = "Language",
            ["SettingEnabled"] = "Enable market value filter",
            ["SettingEnabledTip"] = "Disabled: the slider is hidden and nothing is filtered by price (settings stay saved).",
            ["SettingSliderMax"] = "Slider end value: {0} (type in higher values)",
            ["Reset"] = "Reset price filter",
        };

        private static readonly Dictionary<string, string> fr = new Dictionary<string, string>
        {
            ["MarketValueRange"] = "Valeur marchande : {0} - {1}",
            ["Tooltip"] = "Valeur marchande unitaire autorisée (celle de la fiche d'info).\nTout à droite = sans limite.",
            ["Min"] = "Min",
            ["Max"] = "Max",
            ["SettingLanguage"] = "Langue",
            ["SettingEnabled"] = "Activer le filtre de valeur marchande",
            ["SettingEnabledTip"] = "Désactivé : le curseur disparaît et aucun objet n'est filtré par prix (les réglages restent sauvegardés).",
            ["SettingSliderMax"] = "Valeur en fin de curseur : {0} (au-delà : saisie manuelle)",
            ["Reset"] = "Réinitialiser le filtre de prix",
        };

        public static string LanguageName(PFLanguage lang) => lang == PFLanguage.French ? "Français" : "English";

        public static string T(string key, params object[] args)
        {
            var table = PriceFilterMod.Settings?.language == PFLanguage.French ? fr : en;
            if (!table.TryGetValue(key, out string s) && !en.TryGetValue(key, out s))
                s = key;
            return args.Length == 0 ? s : string.Format(s, args);
        }
    }
}
