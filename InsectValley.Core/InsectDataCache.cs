using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace CarnivorousPlants.Core;

internal static class InsectDataCache
{
    private const string FieldPrefix = "InsectValley.Core/";
    private static readonly HashSet<string> LoggedDataErrors = new(StringComparer.Ordinal);
    private static readonly List<InsectData> AllInsects = new();
    private static readonly List<InsectData> InsectsForCurrentSeason = new();
    private static readonly List<InsectData> InsectsForCurrentLocation = new();
    private static string? _loadedSeason;
    private static string? _loadedLocationName;

    public static void CacheInsectsForSave(GameLocation location, string season, IMonitor monitor)
    {
        AllInsects.Clear();
        InsectsForCurrentSeason.Clear();
        InsectsForCurrentLocation.Clear();
        _loadedSeason = null;
        _loadedLocationName = null;

        foreach (KeyValuePair<string, StardewValley.GameData.Objects.ObjectData> entry in Game1.objectData)
        {
            IDictionary<string, string>? fields = entry.Value.CustomFields;
            if (!TryGetField(fields, "InsectType", out string insectType)
                || !TryReadInsectData(entry.Key, insectType, fields, monitor, out InsectData insect))
            {
                continue;
            }

            AllInsects.Add(insect);
        }

        monitor.Log($"Cached {AllInsects.Count} insect definitions.", LogLevel.Trace);
        UpdateSeasonAndLocation(season, location, monitor);
    }

    public static void UpdateSeasonAndLocation(string season, GameLocation location, IMonitor monitor)
    {
        if (!string.Equals(_loadedSeason, season, StringComparison.OrdinalIgnoreCase))
        {
            InsectsForCurrentSeason.Clear();
            InsectsForCurrentSeason.AddRange(
                AllInsects.Where(insect => InsectCatchRules.IsInSeason(insect, season))
            );
            _loadedSeason = season;
        }

        LoadInsectsForLocation(location, monitor);
    }

    public static void LoadInsectsForLocation(GameLocation location, IMonitor monitor)
    {
        InsectsForCurrentLocation.Clear();
        _loadedLocationName = location.NameOrUniqueName;

        foreach (InsectData insect in InsectsForCurrentSeason)
        {
            if (insect.Locations.Any(catchLocation => MatchesLocation(catchLocation, location)))
                InsectsForCurrentLocation.Add(insect);
        }

        monitor.Log(
            $"Prepared {InsectsForCurrentLocation.Count} insect definitions for '{_loadedLocationName}'.",
            LogLevel.Trace
        );
    }

    public static IReadOnlyList<InsectData> GetInsectsForLocation(GameLocation location, IMonitor monitor)
    {
        if (!string.Equals(_loadedLocationName, location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase))
        {
            LogDataErrorOnce(
                monitor,
                $"location-cache:{location.NameOrUniqueName}",
                $"Insect catch data wasn't prepared for location '{location.NameOrUniqueName}'."
            );
            return Array.Empty<InsectData>();
        }

        return InsectsForCurrentLocation;
    }

    public static IDictionary<string, string>? GetCustomFields(Item item)
    {
        string itemId = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId).ItemId;
        return Game1.objectData.TryGetValue(itemId, out var data) ? data.CustomFields : null;
    }

    public static bool TryGetField(
        IDictionary<string, string>? fields,
        string name,
        out string value
    )
    {
        if (fields is not null
            && fields.TryGetValue(FieldPrefix + name, out string? fieldValue)
            && !string.IsNullOrWhiteSpace(fieldValue))
        {
            value = fieldValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static string[] ParseList(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static void LogDataErrorOnce(IMonitor monitor, string key, string message)
    {
        if (LoggedDataErrors.Add(key))
            monitor.Log(message, LogLevel.Error);
    }

    private static bool TryReadInsectData(
        string itemId,
        string insectType,
        IDictionary<string, string>? fields,
        IMonitor monitor,
        out InsectData insect
    )
    {
        insect = null!;
        if (!TryGetField(fields, "Rarity", out string rarityText)
            || !Enum.TryParse(rarityText, ignoreCase: true, out InsectRarity rarity)
            || !Enum.IsDefined(typeof(InsectRarity), rarity)
            || !TryGetField(fields, "CatchLocations", out string locationsText)
            || !TryGetField(fields, "CatchSeasons", out string seasonsText)
            || !TryGetField(fields, "CatchTimes", out string timesText)
            || !TryParseTimeRange(timesText, out int startTime, out int endTime)
            || !TryGetField(fields, "CatchWeight", out string weightText)
            || !double.TryParse(weightText, NumberStyles.Float, CultureInfo.InvariantCulture, out double weight)
            || !double.IsFinite(weight)
            || weight <= 0)
        {
            LogDataErrorOnce(
                monitor,
                $"insect:{itemId}",
                $"Insect '{itemId}' is missing valid rarity, location, season, time, or catch-weight metadata."
            );
            return false;
        }

        string[] locations = ParseList(locationsText);
        string[] seasons = ParseList(seasonsText);
        if (locations.Length == 0 || seasons.Length == 0)
        {
            LogDataErrorOnce(
                monitor,
                $"insect-lists:{itemId}",
                $"Insect '{itemId}' has an empty location or season list."
            );
            return false;
        }

        insect = new InsectData(itemId, insectType, rarity, locations, seasons, startTime, endTime, weight);
        return true;
    }

    private static bool TryParseTimeRange(string value, out int startTime, out int endTime)
    {
        startTime = 0;
        endTime = 0;
        string[] parts = value.Split('-', StringSplitOptions.TrimEntries);
        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out startTime)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out endTime)
            && startTime <= endTime;
    }

    private static bool MatchesLocation(string catchLocation, GameLocation location)
    {
        MineShaft? mine = location as MineShaft;
        return InsectLocationRules.MatchesLocation(
            catchLocation,
            location.NameOrUniqueName,
            mine is not null,
            mine?.mineLevel ?? 0,
            location is Caldera,
            location.IsOutdoors
        );
    }
}

internal enum InsectRarity
{
    Common,
    Rare,
    Legendary
}

internal sealed class InsectData
{
    public string ItemId { get; }
    public string Type { get; }
    public InsectRarity Rarity { get; }
    public string[] Locations { get; }
    public string[] Seasons { get; }
    public int StartTime { get; }
    public int EndTime { get; }
    public double Weight { get; }

    public InsectData(
        string itemId,
        string type,
        InsectRarity rarity,
        string[] locations,
        string[] seasons,
        int startTime,
        int endTime,
        double weight
    )
    {
        ItemId = itemId;
        Type = type;
        Rarity = rarity;
        Locations = locations;
        Seasons = seasons;
        StartTime = startTime;
        EndTime = endTime;
        Weight = weight;
    }
}
