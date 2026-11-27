using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.Core;

internal static class BugNetPatches
{
    private const string BugNetId = "(T)InsectValley.Core_BugNet";
    private const string IridiumBugNetId = "(T)InsectValley.Core_IridiumBugNet";
    private const int SwingSpriteIndex = 1;
    private static IMonitor _monitor = null!;

    internal static bool IsBugNet(Tool? tool)
    {
        return tool?.QualifiedItemId is BugNetId or IridiumBugNetId;
    }

    public static void Apply(string harmonyId, IMonitor monitor)
    {
        _monitor = monitor;
        var harmony = new Harmony(harmonyId);
        harmony.Patch(
            original: AccessTools.Method(typeof(Tool), "canThisBeAttached", new[] { typeof(SObject), typeof(int) }),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(CanAttach))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Tool), nameof(Tool.beginUsing)),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(BeginUsing))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Tool), nameof(Tool.DoFunction)),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(DoFunction))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Game1), nameof(Game1.pressUseToolButton)),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(PressUseToolButton))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(Game1), nameof(Game1.drawTool), new[] { typeof(Farmer), typeof(int) }),
            prefix: new HarmonyMethod(typeof(BugNetPatches), nameof(DrawTool))
        );
    }

    private static bool CanAttach(Tool __instance, SObject o, int slot, ref bool __result)
    {
        if (!IsBugNet(__instance))
            return true;

        __result = slot == 0 && InsectBait.IsInsectBait(o);
        return false;
    }

    private static bool BeginUsing(Tool __instance, GameLocation location, int x, int y, Farmer who, ref bool __result)
    {
        if (!IsBugNet(__instance))
            return true;

        __instance.SetSpriteIndex(SwingSpriteIndex);
        __instance.IndexOfMenuItemView = 0;
        who.Halt();
        who.canReleaseTool = false;
        who.FarmerSprite.PauseForSingleAnimation = false;
        who.FarmerSprite.StopAnimation();

        int animation = who.FacingDirection switch
        {
            0 => 248,
            1 => 240,
            2 => 232,
            3 => 256,
            _ => throw new System.InvalidOperationException($"Invalid facing direction: {who.FacingDirection}")
        };
        // Match the sword-type scythe timing without invoking weapon damage.
        float interval = (400f - who.addedSpeed * 40f) * (1f - who.buffs.WeaponSpeedMultiplier) / 8f * 1.3f;
        who.FarmerSprite.animateOnce(animation, interval, 6);
        if (__instance.PlayUseSounds && who.ShouldHandleAnimationSound())
            who.playNearbySoundLocal("swordswipe");

        InsectCatchService.TryCatch(__instance, location, who, _monitor);

        __result = false;
        return false;
    }

    private static bool DoFunction(Tool __instance)
    {
        return !IsBugNet(__instance);
    }

    private static bool PressUseToolButton()
    {
        return !IsBugNet(Game1.player?.CurrentTool)
            || Game1.didPlayerJustLeftClick(ignoreNonMouseHeldInput: true);
    }

    private static bool DrawTool(Farmer f)
    {
        if (f.CurrentTool is not Tool net || !IsBugNet(net) || !f.FarmerSprite.isUsingWeapon())
            return true;

        // The default weapon-animation path treats the tool's sprite index as a weapon ID.
        var position = f.getLocalPosition(Game1.viewport) + f.jitter + f.armOffset;
        MeleeWeapon.drawDuringUse(f.FarmerSprite.currentAnimationIndex, f.FacingDirection, Game1.spriteBatch, position, f, net.QualifiedItemId, 3, false);
        return false;
    }
}
