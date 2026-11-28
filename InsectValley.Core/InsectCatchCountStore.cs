using StardewModdingAPI;
using StardewValley;

namespace CarnivorousPlants.Core;

internal static class InsectCatchCountStore
{
    public static int GetCaughtCount(Farmer farmer, string itemId, IMonitor monitor)
    {
        string key = InsectCatchCounts.GetKey(itemId);
        if (!farmer.modData.ContainsKey(key))
            return 0;

        string value = farmer.modData[key];
        if (InsectCatchCounts.TryParseCount(value, out int count))
            return count;

        InsectDataCache.LogDataErrorOnce(
            monitor,
            $"catch-count:{itemId}",
            $"Stored catch count for insect '{itemId}' isn't a nonnegative integer."
        );
        return 0;
    }

    public static void RecordCatch(Farmer farmer, string itemId, IMonitor monitor)
    {
        string key = InsectCatchCounts.GetKey(itemId);
        int nextCount = checked(GetCaughtCount(farmer, itemId, monitor) + 1);
        farmer.modData[key] = nextCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
