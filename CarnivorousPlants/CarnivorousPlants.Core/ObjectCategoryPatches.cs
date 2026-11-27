using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.Core;

/// <summary>Shows custom category labels for insect bait and insects.</summary>
internal static class ObjectCategoryPatches
{
    public static void Apply(string harmonyId)
    {
        var harmony = new Harmony(harmonyId);

        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.getCategoryName)),
            postfix: new HarmonyMethod(typeof(ObjectCategoryPatches), nameof(GetCategoryName_Postfix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(SObject), nameof(SObject.getCategoryColor)),
            postfix: new HarmonyMethod(typeof(ObjectCategoryPatches), nameof(GetCategoryColor_Postfix))
        );
    }

    private static void GetCategoryName_Postfix(SObject __instance, ref string __result)
    {
        if (InsectBait.IsInsectBait(__instance))
            __result = InsectBait.CategoryName;
        else if (__instance.HasContextTag("carnivorousplants_insect"))
            __result = "Insect";
    }

    private static void GetCategoryColor_Postfix(SObject __instance, ref Color __result)
    {
        if (InsectBait.IsInsectBait(__instance))
            __result = new Color(120, 160, 40);
        else if (__instance.HasContextTag("carnivorousplants_insect"))
            __result = new Color(85, 160, 90);
    }
}
