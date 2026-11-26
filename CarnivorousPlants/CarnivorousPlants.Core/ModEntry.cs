using StardewModdingAPI;

namespace CarnivorousPlants.Core;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        ObjectCategoryPatches.Apply(this.ModManifest.UniqueID);
    }
}
