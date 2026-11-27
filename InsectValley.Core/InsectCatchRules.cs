using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CarnivorousPlants.Core;

internal sealed class BaitCatchRules
{
    private readonly bool _catchesAllTypes;
    private readonly HashSet<string> _catchTypes;

    public bool CanCatchRare { get; }
    public bool CanCatchLegendary { get; }

    public BaitCatchRules(bool catchesAllTypes, HashSet<string> catchTypes, bool canCatchRare, bool canCatchLegendary)
    {
        _catchesAllTypes = catchesAllTypes;
        _catchTypes = catchTypes;
        CanCatchRare = canCatchRare;
        CanCatchLegendary = canCatchLegendary;
    }

    public bool CatchesType(string insectType)
    {
        return _catchesAllTypes || _catchTypes.Contains(insectType);
    }
}

internal static class InsectCatchRules
{
    public static bool IsInSeason(InsectData insect, string season)
    {
        return insect.Seasons.Contains(season, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsEligibleForBait(InsectData insect, BaitCatchRules bait, int time)
    {
        return bait.CatchesType(insect.Type)
            && time >= insect.StartTime
            && time <= insect.EndTime;
    }

    public static bool CanCatchRarity(InsectRarity rarity, BaitCatchRules bait)
    {
        return rarity switch
        {
            InsectRarity.Common => true,
            InsectRarity.Rare => bait.CanCatchRare,
            InsectRarity.Legendary => bait.CanCatchLegendary,
            _ => false
        };
    }

    public static Dictionary<string, double> GetCatchProbabilities(
        IReadOnlyList<InsectData> eligible,
        BaitCatchRules bait
    )
    {
        List<InsectData> common = eligible.Where(insect => insect.Rarity == InsectRarity.Common).ToList();
        List<InsectData> rare = eligible.Where(insect => insect.Rarity == InsectRarity.Rare).ToList();
        List<InsectData> legendary = eligible.Where(insect => insect.Rarity == InsectRarity.Legendary).ToList();

        double legendaryChance = bait.CanCatchLegendary && legendary.Count > 0 ? 0.05 : 0;
        double fallbackChance = 1 - legendaryChance;
        double commonChance = 0;
        double rareChance = 0;
        double totalLegendaryChance = legendaryChance;

        if (bait.CanCatchRare && common.Count > 0 && rare.Count > 0)
        {
            commonChance = fallbackChance * 0.5;
            rareChance = fallbackChance * 0.5;
        }
        else if (common.Count > 0)
        {
            commonChance = fallbackChance;
        }
        else if (bait.CanCatchRare && rare.Count > 0)
        {
            rareChance = fallbackChance;
        }
        else if (bait.CanCatchLegendary && legendary.Count > 0)
        {
            totalLegendaryChance += fallbackChance;
        }

        var probabilities = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        AddWeightedProbabilities(probabilities, common, commonChance);
        AddWeightedProbabilities(probabilities, rare, rareChance);
        AddWeightedProbabilities(probabilities, legendary, totalLegendaryChance);
        return probabilities;
    }

    public static List<InsectData> SelectRarityPool(
        IReadOnlyList<InsectData> eligible,
        BaitCatchRules bait,
        Func<double> nextDouble
    )
    {
        List<InsectData> common = eligible.Where(insect => insect.Rarity == InsectRarity.Common).ToList();
        List<InsectData> rare = eligible.Where(insect => insect.Rarity == InsectRarity.Rare).ToList();
        List<InsectData> legendary = eligible.Where(insect => insect.Rarity == InsectRarity.Legendary).ToList();

        if (bait.CanCatchLegendary && legendary.Count > 0 && nextDouble() < 0.05)
            return legendary;

        if (bait.CanCatchRare && common.Count > 0 && rare.Count > 0)
            return nextDouble() < 0.5 ? common : rare;

        if (common.Count > 0)
            return common;
        if (bait.CanCatchRare && rare.Count > 0)
            return rare;
        if (bait.CanCatchLegendary && legendary.Count > 0)
            return legendary;
        return new List<InsectData>();
    }

    private static void AddWeightedProbabilities(
        IDictionary<string, double> probabilities,
        IReadOnlyList<InsectData> insects,
        double rarityChance
    )
    {
        if (rarityChance == 0 || insects.Count == 0)
            return;

        double totalWeight = insects.Sum(insect => insect.Weight);
        foreach (InsectData insect in insects)
            probabilities[insect.ItemId] = rarityChance * insect.Weight / totalWeight;
    }
}

internal static class InsectLocationRules
{
    public static bool MatchesLocation(
        string catchLocation,
        string locationName,
        bool isMine,
        int mineLevel,
        bool isCaldera,
        bool isOutdoors = false
    )
    {
        if (catchLocation.Equals("Outdoors", StringComparison.OrdinalIgnoreCase))
            return isOutdoors;
        if (catchLocation.Equals("Mines", StringComparison.OrdinalIgnoreCase))
            return isMine && mineLevel is > 0 and <= 120;
        if (catchLocation.StartsWith("Mines Floor ", StringComparison.OrdinalIgnoreCase))
            return isMine
                && mineLevel <= 120
                && TryParseFloor(catchLocation, "Mines Floor ", out int mineFloor)
                && mineLevel == mineFloor;
        if (catchLocation.StartsWith("Skull Cavern Floor ", StringComparison.OrdinalIgnoreCase))
            return isMine
                && TryParseFloor(catchLocation, "Skull Cavern Floor ", out int skullCavernFloor)
                && mineLevel == 120 + skullCavernFloor;
        if (catchLocation.Equals("Ginger Island Caldera", StringComparison.OrdinalIgnoreCase))
            return isCaldera || locationName.Equals("Caldera", StringComparison.OrdinalIgnoreCase);
        if (catchLocation.Equals("Ginger Island", StringComparison.OrdinalIgnoreCase))
        {
            return isCaldera
                || locationName.StartsWith("Island", StringComparison.OrdinalIgnoreCase)
                || locationName.StartsWith("VolcanoDungeon", StringComparison.OrdinalIgnoreCase);
        }

        string expectedName = catchLocation switch
        {
            "Witch's Swamp" => "WitchSwamp",
            _ => catchLocation
        };
        return locationName.Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseFloor(string value, string prefix, out int floor)
    {
        return int.TryParse(value.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out floor);
    }
}
