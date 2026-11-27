using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.Core;

internal static class BugNetHud
{
    private const int PanelX = 16;
    private const int PanelY = 64;
    private const int MaxSlotSize = 32;
    private const int SlotGap = 2;
    private const int MaxSlotsPerRow = 15;
    private const int SlotTextureSize = 32;
    private const int SlotCornerRadius = 6;

    private static readonly Dictionary<string, SObject?> InsectIcons = new(StringComparer.OrdinalIgnoreCase);
    private static Texture2D? _slotBackground;
    private static IReadOnlyList<CatchPoolEntry> _catchableInsects = Array.Empty<CatchPoolEntry>();
    private static string? _cachedToolId;
    private static string? _cachedLocationName;
    private static string? _cachedSeason;
    private static string? _cachedBaitId;
    private static int _cachedTime;
    private static bool _hasCachedPool;

    public static void Reset()
    {
        _hasCachedPool = false;
        _catchableInsects = Array.Empty<CatchPoolEntry>();
    }

    public static void Draw(SpriteBatch spriteBatch, Farmer? farmer, IMonitor monitor)
    {
        if (farmer?.CurrentTool is not Tool net || !BugNetPatches.IsBugNet(net))
        {
            Reset();
            return;
        }

        GameLocation location = farmer.currentLocation;
        SObject? attachedBait = net.attachments[0];
        SObject? bait = attachedBait is not null && InsectBait.IsInsectBait(attachedBait)
            ? attachedBait
            : null;
        if (bait is null)
        {
            Reset();
            return;
        }

        string? baitId = bait?.QualifiedItemId;
        string? season = Game1.currentSeason;
        int time = Game1.timeOfDay;

        if (!_hasCachedPool
            || !string.Equals(_cachedToolId, net.QualifiedItemId, StringComparison.Ordinal)
            || !string.Equals(_cachedLocationName, location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_cachedSeason, season, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_cachedBaitId, baitId, StringComparison.Ordinal)
            || _cachedTime != time)
        {
            _catchableInsects = bait is null
                ? Array.Empty<CatchPoolEntry>()
                : InsectCatchService.GetCatchableInsects(net, location, monitor);
            _cachedToolId = net.QualifiedItemId;
            _cachedLocationName = location.NameOrUniqueName;
            _cachedSeason = season;
            _cachedBaitId = baitId;
            _cachedTime = time;
            _hasCachedPool = true;
        }

        DrawPanel(spriteBatch, bait, monitor);
    }

    private static void DrawPanel(SpriteBatch spriteBatch, SObject? bait, IMonitor monitor)
    {
        int slotCount = _catchableInsects.Count + 1;
        int slotsPerRow = Math.Min(slotCount, MaxSlotsPerRow);
        int availableWidth = Math.Max(1, Game1.uiViewport.Width - PanelX * 2);
        int gap = Math.Min(SlotGap, availableWidth / Math.Max(1, slotsPerRow - 1));
        int slotSize = Math.Min(
            MaxSlotSize,
            (availableWidth - gap * (slotsPerRow - 1)) / slotsPerRow
        );
        slotSize = Math.Max(1, slotSize);

        DrawSlot(spriteBatch, PanelX, PanelY, slotSize, bait, bait?.Stack.ToString());
        for (int i = 0; i < _catchableInsects.Count; i++)
        {
            CatchPoolEntry catchPoolEntry = _catchableInsects[i];
            SObject? icon = GetInsectIcon(catchPoolEntry.Insect, monitor);
            if (icon is null)
                continue;

            int slotIndex = i + 1;
            int column = slotIndex % MaxSlotsPerRow;
            int row = slotIndex / MaxSlotsPerRow;
            int x = PanelX + column * (slotSize + gap);
            int y = PanelY + row * (slotSize + gap);
            DrawSlot(spriteBatch, x, y, slotSize, icon, FormatChance(catchPoolEntry.Probability));
        }
    }

    private static void DrawSlot(
        SpriteBatch spriteBatch,
        int x,
        int y,
        int size,
        SObject? item,
        string? badgeText = null
    )
    {
        spriteBatch.Draw(
            GetSlotBackground(spriteBatch.GraphicsDevice),
            new Rectangle(x, y, size, size),
            Color.White
        );

        if (item is null)
            return;

        int iconSize = Math.Max(1, size * 7 / 10);
        var iconPosition = new Vector2(x + size / 2f - 32f, y + size / 2f - 32f);
        DrawItem(spriteBatch, item, iconPosition, iconSize / 64f);

        if (badgeText is null)
            return;

        string count = badgeText;
        const float countScale = 0.45f;
        Vector2 countSize = Game1.smallFont.MeasureString(count) * countScale;
        var countPosition = new Vector2(
            x + size - countSize.X - 2,
            y + size - countSize.Y - 1
        );
        spriteBatch.DrawString(
            Game1.smallFont,
            count,
            countPosition + Vector2.One,
            Color.Black,
            0f,
            Vector2.Zero,
            countScale,
            SpriteEffects.None,
            0.99f
        );
        spriteBatch.DrawString(
            Game1.smallFont,
            count,
            countPosition,
            Color.White,
            0f,
            Vector2.Zero,
            countScale,
            SpriteEffects.None,
            0.99f
        );
    }

    private static string FormatChance(double probability)
    {
        double percentage = probability * 100;
        if (percentage < 0.1)
            return "<0.1%";

        string format = percentage < 10 ? "0.#" : "0";
        return percentage.ToString(format, CultureInfo.InvariantCulture) + "%";
    }

    private static Texture2D GetSlotBackground(GraphicsDevice graphicsDevice)
    {
        if (_slotBackground is not null && !_slotBackground.IsDisposed)
            return _slotBackground;

        var texture = new Texture2D(graphicsDevice, SlotTextureSize, SlotTextureSize);
        var pixels = new Color[SlotTextureSize * SlotTextureSize];
        var fill = new Color(0, 0, 0, 160);

        for (int y = 0; y < SlotTextureSize; y++)
        {
            int nearestY = Math.Clamp(y, SlotCornerRadius, SlotTextureSize - SlotCornerRadius - 1);
            int offsetY = y - nearestY;
            for (int x = 0; x < SlotTextureSize; x++)
            {
                int nearestX = Math.Clamp(x, SlotCornerRadius, SlotTextureSize - SlotCornerRadius - 1);
                int offsetX = x - nearestX;
                if (offsetX * offsetX + offsetY * offsetY <= SlotCornerRadius * SlotCornerRadius)
                    pixels[y * SlotTextureSize + x] = fill;
            }
        }

        texture.SetData(pixels);
        _slotBackground = texture;
        return texture;
    }

    private static SObject? GetInsectIcon(InsectData insect, IMonitor monitor)
    {
        if (InsectIcons.TryGetValue(insect.ItemId, out SObject? cached))
            return cached;

        Item? item = ItemRegistry.Create($"(O){insect.ItemId}", allowNull: true);
        if (item is SObject icon)
        {
            InsectIcons.Add(insect.ItemId, icon);
            return icon;
        }

        InsectIcons.Add(insect.ItemId, null);
        InsectDataCache.LogDataErrorOnce(
            monitor,
            $"hud-item:{insect.ItemId}",
            $"Couldn't create insect item '{insect.ItemId}' for the bug-net HUD."
        );
        return null;
    }

    private static void DrawItem(SpriteBatch spriteBatch, Item item, Vector2 position, float scale)
    {
        item.drawInMenu(
            spriteBatch,
            position,
            scale,
            1f,
            0.9f,
            StackDrawType.Hide,
            Color.White,
            drawShadow: false
        );
    }
}
