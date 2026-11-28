using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace CarnivorousPlants.Core;

internal static class InsectCollectionPatches
{
    private const int InsectTab = 8;
    private const int InsectTabComponentId = 7009;
    private const int ItemsPerRow = 10;
    private const int TabSize = 64;
    private const string BugIconAsset = "Mods/InsectValley.Core/bug_icon";

    private static IMonitor _monitor = null!;

    public static void Apply(string harmonyId, IMonitor monitor)
    {
        _monitor = monitor;
        var harmony = new Harmony(harmonyId);
        harmony.Patch(
            original: AccessTools.Constructor(typeof(CollectionsPage), new[] { typeof(int), typeof(int), typeof(int), typeof(int) }),
            postfix: new HarmonyMethod(typeof(InsectCollectionPatches), nameof(CollectionsPage_Constructor_Postfix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(CollectionsPage), nameof(CollectionsPage.createDescription), new[] { typeof(string) }),
            prefix: new HarmonyMethod(typeof(InsectCollectionPatches), nameof(CreateDescription_Prefix))
        );
    }

    private static void CollectionsPage_Constructor_Postfix(CollectionsPage __instance)
    {
        var insectIds = new HashSet<string>(
            InsectDataCache.GetAllInsects().Select(insect => insect.ItemId),
            StringComparer.Ordinal
        );

        foreach (List<ClickableTextureComponent> page in __instance.collections[0])
            page.RemoveAll(component => insectIds.Contains(component.name.Split(' ')[0]));

        ClickableTextureComponent previousTab = __instance.sideTabs.Values
            .OrderByDescending(tab => tab.bounds.Y)
            .First();

        var insectTab = new ClickableTextureComponent(
            InsectTab.ToString(),
            new Rectangle(previousTab.bounds.X, previousTab.bounds.Bottom, 64, 64),
            null,
            "Insects",
            Game1.content.Load<Texture2D>(BugIconAsset),
            new Rectangle(0, 0, 16, 16),
            4f
        )
        {
            myID = InsectTabComponentId,
            upNeighborID = previousTab.myID,
            downNeighborID = -1,
            downNeighborImmutable = true,
            rightNeighborID = 0
        };
        __instance.sideTabs.Add(InsectTab, insectTab);
        FitSideTabsToViewport(__instance);

        List<List<ClickableTextureComponent>> insectPages = new()
        {
            new List<ClickableTextureComponent>()
        };
        __instance.collections.Add(InsectTab, insectPages);

        int startX = __instance.xPositionOnScreen
            + IClickableMenu.borderWidth
            + IClickableMenu.spaceToClearSideBorder;
        int startY = __instance.yPositionOnScreen
            + IClickableMenu.borderWidth
            + IClickableMenu.spaceToClearTopBorder
            - 16;
        int lastRowY = __instance.yPositionOnScreen + __instance.height - 128;
        int itemIndex = 0;

        var orderedInsects = InsectDataCache.GetAllInsects()
            .Select(insect => (Insect: insect, Item: ItemRegistry.GetDataOrErrorItem(insect.ItemId)))
            .OrderBy(entry => entry.Item.TextureName, StringComparer.Ordinal)
            .ThenBy(entry => entry.Item.SpriteIndex)
            .ToList();

        foreach ((InsectData insect, ParsedItemData itemData) in orderedInsects)
        {
            int x = startX + itemIndex % ItemsPerRow * 68;
            int y = startY + itemIndex / ItemsPerRow * 68;
            if (y > lastRowY)
            {
                insectPages.Add(new List<ClickableTextureComponent>());
                itemIndex = 0;
                x = startX;
                y = startY;
            }

            List<ClickableTextureComponent> page = insectPages[^1];
            bool caught = InsectCatchCountStore.GetCaughtCount(Game1.player, insect.ItemId, _monitor) > 0;
            page.Add(new ClickableTextureComponent(
                $"{insect.ItemId} {caught} false",
                new Rectangle(x, y, 64, 64),
                null,
                "",
                itemData.GetTexture(),
                itemData.GetSourceRect(),
                4f,
                caught
            )
            {
                myID = page.Count,
                rightNeighborID = ((page.Count + 1) % ItemsPerRow == 0) ? -1 : page.Count + 1,
                leftNeighborID = (page.Count % ItemsPerRow == 0) ? InsectTabComponentId : page.Count - 1,
                downNeighborID = (y + 68 > lastRowY) ? -7777 : page.Count + ItemsPerRow,
                upNeighborID = (page.Count < ItemsPerRow) ? 12347 : page.Count - ItemsPerRow,
                fullyImmutable = true
            });
            itemIndex++;
        }
    }

    private static void FitSideTabsToViewport(CollectionsPage page)
    {
        List<ClickableTextureComponent> tabs = page.sideTabs.Values
            .OrderBy(tab => Array.IndexOf(
                new[] { 0, 1, InsectTab, 2, 3, 4, 5, 6, 7 },
                int.Parse(tab.name)
            ))
            .ToList();
        int firstY = tabs[0].bounds.Y;
        int availableHeight = page.yPositionOnScreen + page.height - firstY - 8;
        int spacing = Math.Max(1, Math.Min(TabSize, (availableHeight - TabSize) / (tabs.Count - 1)));

        for (int i = 0; i < tabs.Count; i++)
        {
            ClickableTextureComponent tab = tabs[i];
            tab.bounds = new Rectangle(
                tab.bounds.X,
                firstY + spacing * i,
                TabSize,
                spacing
            );
            tab.scale = TabSize / 16f;
            tab.baseScale = tab.scale;
            tab.upNeighborID = i == 0 ? -1 : tabs[i - 1].myID;
            tab.downNeighborID = i == tabs.Count - 1 ? -1 : tabs[i + 1].myID;
            tab.upNeighborImmutable = i == 0;
            tab.downNeighborImmutable = i == tabs.Count - 1;
        }
    }

    private static bool CreateDescription_Prefix(
        CollectionsPage __instance,
        string id,
        ref string __result
    )
    {
        if (__instance.currentTab != InsectTab)
            return true;

        ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(id);
        string title = Game1.content.LoadStringReturnNullIfNotFound(
            $"Strings\\Objects:{itemData.ItemId}_CollectionsTabName"
        ) ?? itemData.DisplayName;
        string description = Game1.content.LoadStringReturnNullIfNotFound(
            $"Strings\\Objects:{itemData.ItemId}_CollectionsTabDescription"
        ) ?? itemData.Description;
        int count = InsectCatchCountStore.GetCaughtCount(Game1.player, id, _monitor);

        __result = $"{title}{Environment.NewLine}{Environment.NewLine}"
            + $"{Game1.parseText(description, Game1.smallFont, 256)}{Environment.NewLine}{Environment.NewLine}"
            + $"Caught: {count}";
        return false;
    }
}
