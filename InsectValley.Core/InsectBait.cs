using StardewValley;

namespace CarnivorousPlants.Core;

internal static class InsectBait
{
    public const string ContextTag = "insect_bait";
    public const string CategoryName = "Insect Bait";

    public static bool IsInsectBait(Item? item)
    {
        return item?.HasContextTag(ContextTag) == true;
    }
}
