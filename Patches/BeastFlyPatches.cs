using HarmonyLib;
using TeamCherry.Localization;

namespace ChoirBeastFly.Patches;
internal static class BeastFlyPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Language), nameof(Language.Get), typeof(string), typeof(string))]
    private static void ChangeBeastflyTitle(string key, string sheetTitle, ref string __result)
    {
        __result = key switch
        {
            "GIANT_BONE_FLYER_SUPER" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Pious",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "虔诚的",
                _ => __result
            },
            "GIANT_BONE_FLYER_MAIN" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Choir Beastfly",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "圣咏兽蝇",
                _ => __result
            },
            "BEASTFLY_01" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Sing...Forever...",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "咏唱……永远……",
                _ => __result
            },
            "BEASTFLY_02" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Salvation...Ascendence...",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "救赎……升天……",
                _ => __result
            },
            "BEASTFLY_03" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Self...Forgotten...",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "忘却……自我……",
                _ => __result
            },
            "BEASTFLY_04" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Sacred...Holy...",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "辉煌……神圣……",
                _ => __result
            },
            "BEASTFLY_05" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "Song...Unending...",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "圣歌……不止……",
                _ => __result
            },
            "BEASTFLY_06" => Language.CurrentLanguage() switch
            {
                // LanguageCode.DE => "Gall",
                LanguageCode.EN => "For the Conductor...For...Her?",
                // LanguageCode.JA => "漆黒の",
                // LanguageCode.KO => "광기에 빠진",
                // LanguageCode.RU => "Пропащая",
                LanguageCode.ZH => "为指挥……为…她？",
                _ => __result
            },
            _ => __result
        };
    }

}
