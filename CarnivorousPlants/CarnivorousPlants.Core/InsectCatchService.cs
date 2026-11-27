using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.Core;

internal static class InsectCatchService
{
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
