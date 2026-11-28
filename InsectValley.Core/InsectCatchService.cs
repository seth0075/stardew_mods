using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.Core;

internal static class InsectCatchService
{
    public static void LogCatchPool(Farmer farmer, IMonitor monitor)
    {
        Tool? net = farmer.CurrentTool;
        if (!BugNetPatches.IsBugNet(net))
        {
            monitor.Log("Equip a bug net before running bugnet_pool.", LogLevel.Info);
            return;
        }

        GameLocation location = farmer.currentLocation;
        SObject? bait = net.attachments[0];
        monitor.Log(
            $"Bug net pool: season={Game1.currentSeason}, time={Game1.timeOfDay}, location={location.NameOrUniqueName}.",
            LogLevel.Info
        );

        if (bait is null || !InsectBait.IsInsectBait(bait))
        {
            monitor.Log("No insect bait is attached; there is no catch pool.", LogLevel.Info);
            return;
        }

        if (!TryReadBaitData(bait, monitor, out BaitCatchRules baitData))
            return;

        List<InsectData> eligibleInsects = GetEligibleInsects(location, baitData, monitor);
        List<InsectData> catchableInsects = FilterCatchableByRarity(eligibleInsects, baitData);
        int commonCount = eligibleInsects.Count(insect => insect.Rarity == InsectRarity.Common);
        int rareCount = eligibleInsects.Count(insect => insect.Rarity == InsectRarity.Rare);
        int legendaryCount = eligibleInsects.Count(insect => insect.Rarity == InsectRarity.Legendary);
        int catchableCommonCount = catchableInsects.Count(insect => insect.Rarity == InsectRarity.Common);
        int catchableRareCount = catchableInsects.Count(insect => insect.Rarity == InsectRarity.Rare);
        int catchableLegendaryCount = catchableInsects.Count(insect => insect.Rarity == InsectRarity.Legendary);
        monitor.Log(
            $"Bait={bait.QualifiedItemId}, rare={baitData.CanCatchRare}, legendary={baitData.CanCatchLegendary}. "
            + $"Candidates before rarity selection={eligibleInsects.Count} "
            + $"(common={commonCount}, rare={rareCount}, legendary={legendaryCount}); "
            + $"catchable pool={catchableInsects.Count} "
            + $"(common={catchableCommonCount}, rare={catchableRareCount}, legendary={catchableLegendaryCount}).",
            LogLevel.Info
        );

        if (eligibleInsects.Count == 0)
        {
            monitor.Log("No insects match this bait, season, location, and time; a swing still consumes bait.", LogLevel.Info);
            return;
        }

        if (catchableInsects.Count == 0)
        {
            monitor.Log("The candidates are all rarities this bait cannot catch; a swing still consumes bait.", LogLevel.Info);
            return;
        }

        foreach (InsectData insect in catchableInsects)
        {
            monitor.Log(
                $"  {insect.ItemId}: type={insect.Type}, rarity={insect.Rarity}, "
                + $"time={insect.StartTime}-{insect.EndTime}, weight={insect.Weight}.",
                LogLevel.Info
            );
        }
    }

    public static List<CatchPoolEntry> GetCatchableInsects(Tool net, GameLocation location, IMonitor monitor)
    {
        if (net.attachments[0] is not SObject bait || !InsectBait.IsInsectBait(bait)
            || !TryReadBaitData(bait, monitor, out BaitCatchRules baitData))
        {
            return new List<CatchPoolEntry>();
        }

        List<InsectData> eligible = GetEligibleInsects(location, baitData, monitor);
        Dictionary<string, double> probabilities = InsectCatchRules.GetCatchProbabilities(eligible, baitData);
        return FilterCatchableByRarity(eligible, baitData)
            .Select(insect => new CatchPoolEntry(insect, probabilities[insect.ItemId]))
            .ToList();
    }

    public static void TryCatch(Tool net, GameLocation location, Farmer farmer, IMonitor monitor)
    {
        if (!farmer.IsLocalPlayer)
            return;

        SObject? bait = net.attachments[0];
        if (bait is null || !InsectBait.IsInsectBait(bait))
            return;

        ConsumeOneBait(net, bait);

        if (!TryReadBaitData(bait, monitor, out BaitCatchRules baitData))
            return;

        List<InsectData> eligibleInsects = GetEligibleInsects(location, baitData, monitor);
        List<InsectData> rarityPool = InsectCatchRules.SelectRarityPool(
            eligibleInsects,
            baitData,
            Game1.random.NextDouble
        );
        if (rarityPool.Count == 0)
            return;

        InsectData selected = SelectByWeight(rarityPool);
        Item? caughtItem = ItemRegistry.Create($"(O){selected.ItemId}", allowNull: true);
        if (caughtItem is not SObject insect)
        {
            InsectDataCache.LogDataErrorOnce(
                monitor,
                $"item:{selected.ItemId}",
                $"Couldn't create insect item '{selected.ItemId}' after selecting it for a catch."
            );
            return;
        }

        InsectCatchCountStore.RecordCatch(farmer, selected.ItemId, monitor);
        if (!farmer.addItemToInventoryBool(insect))
            Game1.createItemDebris(insect, farmer.Position, farmer.FacingDirection, location);
    }

    private static void ConsumeOneBait(Tool net, SObject bait)
    {
        if (bait.Stack > 1)
            bait.Stack--;
        else
            net.attachments[0] = null;
    }

    private static bool TryReadBaitData(SObject bait, IMonitor monitor, out BaitCatchRules baitData)
    {
        baitData = null!;
        IDictionary<string, string>? fields = InsectDataCache.GetCustomFields(bait);
        if (!InsectDataCache.TryGetField(fields, "CatchesInsectTypes", out string insectTypes)
            || !InsectDataCache.TryGetField(fields, "CanCatchRare", out string canCatchRareText)
            || !bool.TryParse(canCatchRareText, out bool canCatchRare)
            || !InsectDataCache.TryGetField(fields, "CanCatchLegendary", out string canCatchLegendaryText)
            || !bool.TryParse(canCatchLegendaryText, out bool canCatchLegendary))
        {
            InsectDataCache.LogDataErrorOnce(
                monitor,
                $"bait:{bait.QualifiedItemId}",
                $"Bait '{bait.QualifiedItemId}' is missing valid insect-catch metadata."
            );
            return false;
        }

        string[] types = InsectDataCache.ParseList(insectTypes);
        if (types.Length == 0)
        {
            InsectDataCache.LogDataErrorOnce(
                monitor,
                $"bait-types:{bait.QualifiedItemId}",
                $"Bait '{bait.QualifiedItemId}' has no insect types in its catch metadata."
            );
            return false;
        }

        baitData = new BaitCatchRules(
            types.Contains("*", StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(types, StringComparer.OrdinalIgnoreCase),
            canCatchRare,
            canCatchLegendary
        );
        return true;
    }

    private static List<InsectData> GetEligibleInsects(GameLocation location, BaitCatchRules bait, IMonitor monitor)
    {
        var eligible = new List<InsectData>();
        int time = Game1.timeOfDay;

        foreach (InsectData insect in InsectDataCache.GetInsectsForLocation(location, monitor))
        {
            if (!InsectCatchRules.IsEligibleForBait(insect, bait, time))
                continue;

            eligible.Add(insect);
        }

        return eligible;
    }

    private static List<InsectData> FilterCatchableByRarity(
        IEnumerable<InsectData> insects,
        BaitCatchRules bait
    )
    {
        return insects
            .Where(insect => InsectCatchRules.CanCatchRarity(insect.Rarity, bait))
            .OrderBy(insect => insect.ItemId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static InsectData SelectByWeight(List<InsectData> insects)
    {
        double totalWeight = insects.Sum(insect => insect.Weight);
        double choice = Game1.random.NextDouble() * totalWeight;

        foreach (InsectData insect in insects)
        {
            choice -= insect.Weight;
            if (choice < 0)
                return insect;
        }

        return insects[^1];
    }

}

internal sealed record CatchPoolEntry(InsectData Insect, double Probability);
