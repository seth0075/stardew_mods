using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;

namespace CarnivorousPlants.Core;

internal sealed class InsectValleyContent
{
    private const string InsectsAsset = "Mods/InsectValley.Core/insects";
    private const string BaitAsset = "Mods/InsectValley.Core/bait";
    private const string BugNetAsset = "Mods/InsectValley.Core/tools";
    private const string IridiumBugNetAsset = "Mods/InsectValley.Core/iridium_bug_net";

    private readonly IDictionary<string, ObjectData> _objects;
    private readonly IDictionary<string, ToolData> _tools;
    private readonly IDictionary<string, string> _craftingRecipes;

    public InsectValleyContent(IModHelper helper)
    {
        _objects = ReadRequiredData<Dictionary<string, ObjectData>>(helper, "data/insects.json");
        foreach (KeyValuePair<string, ObjectData> bait in
                 ReadRequiredData<Dictionary<string, ObjectData>>(helper, "data/bait.json"))
        {
            _objects.Add(bait.Key, bait.Value);
        }

        _tools = ReadRequiredData<Dictionary<string, ToolData>>(helper, "data/tools.json");
        _craftingRecipes = ReadRequiredData<Dictionary<string, string>>(helper, "data/crafting_recipes.json");
    }

    public void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(asset =>
            {
                IDictionary<string, ObjectData> data = asset.AsDictionary<string, ObjectData>().Data;
                foreach (KeyValuePair<string, ObjectData> item in _objects)
                    data.Add(item.Key, item.Value);
            }, AssetEditPriority.Late);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
        {
            e.Edit(asset =>
            {
                IDictionary<string, ToolData> data = asset.AsDictionary<string, ToolData>().Data;
                foreach (KeyValuePair<string, ToolData> tool in _tools)
                    data.Add(tool.Key, tool.Value);
            }, AssetEditPriority.Late);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(asset =>
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                foreach (KeyValuePair<string, string> recipe in _craftingRecipes)
                    data.Add(recipe.Key, recipe.Value);
            }, AssetEditPriority.Late);
        }
        else if (e.NameWithoutLocale.IsEquivalentTo(InsectsAsset))
            e.LoadFromModFile<Texture2D>("assets/insects.png", AssetLoadPriority.Medium);
        else if (e.NameWithoutLocale.IsEquivalentTo(BaitAsset))
            e.LoadFromModFile<Texture2D>("assets/bait.png", AssetLoadPriority.Medium);
        else if (e.NameWithoutLocale.IsEquivalentTo(BugNetAsset))
            e.LoadFromModFile<Texture2D>("assets/bug_net_tool.png", AssetLoadPriority.Medium);
        else if (e.NameWithoutLocale.IsEquivalentTo(IridiumBugNetAsset))
            e.LoadFromModFile<Texture2D>("assets/iridium_bug_net_tool.png", AssetLoadPriority.Medium);
    }

    private static T ReadRequiredData<T>(IModHelper helper, string path) where T : class
    {
        return helper.Data.ReadJsonFile<T>(path)
            ?? throw new InvalidDataException($"Couldn't read required Insect Valley data file '{path}'.");
    }
}
