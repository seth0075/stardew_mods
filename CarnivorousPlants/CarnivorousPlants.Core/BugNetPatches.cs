using HarmonyLib;
using StardewValley;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.Core;

internal static class BugNetPatches
{
    private const string BugNetId = "(T)CarnivorousCrops.ContentPack_BugNet";

    public static void Apply(string harmonyId)
    {
        var harmony = new Harmony(harmonyId);
        harmony.Patch(
            original: AccessTools.Method(typeof(Tool), "canThisBeAttached", new[] { typeof(SObject), typeof(int) }),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(CanAttach))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Tool), nameof(Tool.beginUsing)),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(BeginUsing))
        );
    }

    private static bool CanAttach(Tool __instance, SObject o, int slot, ref bool __result)
    {
        if (__instance.QualifiedItemId != BugNetId)
            return true;

        __result = slot == 0 && InsectBait.IsInsectBait(o);
        return false;
    }

    private static bool BeginUsing(Tool __instance, Farmer who, ref bool __result)
    {
        if (__instance.QualifiedItemId != BugNetId)
            return true;

        who.CanMove = true;
        who.UsingTool = false;
        who.canReleaseTool = false;
        __result = true;
        return false;
    }
}
