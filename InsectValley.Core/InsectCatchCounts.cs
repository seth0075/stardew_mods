using System;
using System.Collections.Generic;
using System.Globalization;

namespace CarnivorousPlants.Core;

internal static class InsectCatchCounts
{
    internal const string ModDataPrefix = "InsectValley.Core/Caught/";

    internal static int GetCaughtCount(IDictionary<string, string> modData, string itemId)
    {
        return modData.TryGetValue(GetKey(itemId), out string? value) && TryParseCount(value, out int count)
            ? count
            : 0;
    }

    internal static void RecordCatch(IDictionary<string, string> modData, string itemId)
    {
        int nextCount = checked(GetCaughtCount(modData, itemId) + 1);
        modData[GetKey(itemId)] = nextCount.ToString(CultureInfo.InvariantCulture);
    }

    internal static string GetKey(string itemId)
    {
        return ModDataPrefix + itemId;
    }

    internal static bool TryParseCount(string? value, out int count)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) && count >= 0;
    }
}
