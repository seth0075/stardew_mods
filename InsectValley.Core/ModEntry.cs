using StardewModdingAPI;
using StardewValley;

namespace CarnivorousPlants.Core;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        var content = new InsectValleyContent(helper);
        helper.Events.Content.AssetRequested += content.OnAssetRequested;

        helper.ConsoleCommands.Add(
            "bugnet_pool",
            "Show the insects currently eligible for the held bug net and its attached bait.",
            (_, _) => InsectCatchService.LogCatchPool(Game1.player, this.Monitor)
        );

        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            InsectDataCache.CacheInsectsForSave(Game1.currentLocation, Game1.currentSeason, this.Monitor);
            BugNetHud.Reset();
        };
        helper.Events.GameLoop.DayStarted += (_, _) =>
            InsectDataCache.UpdateSeasonAndLocation(Game1.currentSeason, Game1.currentLocation, this.Monitor);
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => BugNetHud.Reset();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => BugNetPatches.ResetPendingSwing();
        helper.Events.Player.Warped += (_, e) =>
        {
            if (e.IsLocalPlayer)
            {
                BugNetPatches.ResetPendingSwing();
                InsectDataCache.LoadInsectsForLocation(e.NewLocation, this.Monitor);
                BugNetHud.Reset();
            }
        };
        helper.Events.Input.ButtonReleased += (_, e) => BugNetPatches.OnButtonReleased(e.Button);
        helper.Events.Display.RenderedHud += (_, e) =>
            BugNetHud.Draw(e.SpriteBatch, Game1.player, this.Monitor);

        ObjectCategoryPatches.Apply(this.ModManifest.UniqueID);
        BugNetPatches.Apply(this.ModManifest.UniqueID, this.Monitor);
        InsectCollectionPatches.Apply(this.ModManifest.UniqueID, this.Monitor);
    }
}
