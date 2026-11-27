using StardewModdingAPI;
using StardewValley;

namespace CarnivorousPlants.Core;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.SaveLoaded += (_, _) =>
            InsectDataCache.CacheInsectsForSave(Game1.currentLocation, Game1.currentSeason, this.Monitor);
        helper.Events.GameLoop.DayStarted += (_, _) =>
            InsectDataCache.UpdateSeasonAndLocation(Game1.currentSeason, Game1.currentLocation, this.Monitor);
        helper.Events.Player.Warped += (_, e) =>
        {
            if (e.IsLocalPlayer)
                InsectDataCache.LoadInsectsForLocation(e.NewLocation, this.Monitor);
        };

        ObjectCategoryPatches.Apply(this.ModManifest.UniqueID);
        BugNetPatches.Apply(this.ModManifest.UniqueID, this.Monitor);
    }
}
