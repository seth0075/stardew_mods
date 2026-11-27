using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace CarnivorousCrops.Tests;

public sealed class CarnivorousCropsContentTests
{
    private const string CropItemPrefix = "{{ModId}}_";

    [Fact]
    public void CropSchedulesAndHarvestItemsMatchDefinitions()
    {
        var expected = new Dictionary<string, (string Seasons, string Phases, int RegrowDays, string Harvest)>
        {
            ["FlytrapSeeds"] = ("spring,summer", "1,2,2,3,4", -1, "Flytrap"),
            ["PitcherSeeds"] = ("summer", "2,2,3,3,4", -1, "Pitcher"),
            ["SundewSeeds"] = ("summer,fall", "1,1,2,3,3", -1, "Sundew"),
            ["TrumpetSeeds"] = ("spring,summer", "2,2,3,3,4", 5, "TrumpetTube"),
            ["ButterwortSeeds"] = ("spring,fall", "1,2,2,3,4", 4, "ButterwortLeaf"),
            ["CobraSeeds"] = ("summer,fall", "2,3,3,4,6", -1, "CobraHood"),
            ["AstralSeeds"] = ("spring,summer,fall", "3,5,5,7,8", 14, "AstralBlossom"),
            ["AustralianPitcherSeeds"] = ("spring,fall", "2,3,3,4,4", -1, "AustralianPitcher"),
            ["SunPitcherSeeds"] = ("summer", "2,3,4,4,5", 5, "SunPitcher"),
            ["RainbowPlantSeeds"] = ("spring,summer", "1,2,2,3,4", -1, "RainbowPlant"),
            ["BladderwortSeeds"] = ("spring,summer", "1,2,2,2,3", -1, "Bladderwort"),
            ["DewyPineSeeds"] = ("summer", "2,2,3,3,4", 5, "DewyPine"),
            ["WaterwheelPlantSeeds"] = ("summer,fall", "2,3,3,4,4", 6, "WaterwheelPlant")
        };
        Dictionary<string, JsonElement> crops = ReadEntries("Crops/content.json", "Data/Crops");
        Dictionary<string, JsonElement> objects = ReadEntries("Crops/content.json", "Data/Objects");

        Assert.Equal(expected.Count, crops.Count);
        foreach (var (seedId, definition) in expected)
        {
            JsonElement crop = crops[CropItemPrefix + seedId];
            Assert.Equal(definition.Seasons, string.Join(",", crop.GetProperty("Seasons").EnumerateArray().Select(value => value.GetString())));
            Assert.Equal(definition.Phases, string.Join(",", crop.GetProperty("DaysInPhase").EnumerateArray().Select(value => value.GetInt32())));
            Assert.All(crop.GetProperty("DaysInPhase").EnumerateArray(), phase => Assert.True(phase.GetInt32() > 0));
            Assert.Equal(definition.RegrowDays, crop.GetProperty("RegrowDays").GetInt32());
            Assert.Equal(CropItemPrefix + definition.Harvest, crop.GetProperty("HarvestItemId").GetString());
            Assert.Contains(CropItemPrefix + definition.Harvest, objects.Keys);
        }
    }

    [Fact]
    public void ArtisanMachineRulesProduceEveryConfiguredGoodFromExpectedInput()
    {
        var expected = new Dictionary<string, (string Input, int Minutes)>
        {
            ["FlytrapExtract"] = ("Flytrap", 1440),
            ["PitcherNectar"] = ("Pitcher", 1440),
            ["SundewResin"] = ("Sundew", 1080),
            ["TrumpetEssence"] = ("TrumpetTube", 1440),
            ["ButterwortMucilage"] = ("ButterwortLeaf", 1440),
            ["CobraSecretion"] = ("CobraHood", 2880),
            ["AstralEssence"] = ("AstralBlossom", 4320),
            ["CephalotusTincture"] = ("AustralianPitcher", 2880),
            ["SolarElixir"] = ("SunPitcher", 2880),
            ["IridescentElixir"] = ("RainbowPlant", 2880),
            ["BladderwortTincture"] = ("Bladderwort", 2160),
            ["DewdropElixir"] = ("DewyPine", 2880),
            ["AquaticTincture"] = ("WaterwheelPlant", 2880),
            ["FlytrapTincture"] = ("Flytrap", 2880),
            ["PitcherElixir"] = ("Pitcher", 2880),
            ["TrumpetCordial"] = ("TrumpetTube", 2880),
            ["CobraDistillate"] = ("CobraHood", 4320),
            ["CelestialElixir"] = ("AstralBlossom", 10080),
            ["SundewSyrup"] = ("Sundew", 2880),
            ["ButterwortJelly"] = ("ButterwortLeaf", 2880)
        };
        Dictionary<string, JsonElement> artisanGoods = ReadEntries("Crops/data/artisan_goods.json", "Data/Objects");
        List<JsonElement> rules = ReadMachineRules();

        Assert.Equal(expected.Count, artisanGoods.Count);
        Assert.Equal(expected.Count, rules.Count);
        var actual = new Dictionary<string, (string Input, int Minutes)>();

        foreach (JsonElement rule in rules)
        {
            JsonElement trigger = Assert.Single(rule.GetProperty("Triggers").EnumerateArray());
            JsonElement output = Assert.Single(rule.GetProperty("OutputItem").EnumerateArray());
            string outputId = RemoveObjectPrefix(output.GetProperty("ItemId").GetString()!);
            Assert.True(actual.TryAdd(outputId, (
                RemoveObjectPrefix(trigger.GetProperty("RequiredItemId").GetString()!),
                rule.GetProperty("MinutesUntilReady").GetInt32()
            )), $"Duplicate machine output rule for {outputId}.");
            Assert.Equal(1, trigger.GetProperty("RequiredCount").GetInt32());
            Assert.Equal(1, output.GetProperty("MinStack").GetInt32());
            Assert.Equal(1, output.GetProperty("MaxStack").GetInt32());
        }

        foreach (var (goodId, recipe) in expected)
        {
            string itemId = CropItemPrefix + goodId;
            Assert.Contains(itemId, artisanGoods.Keys);
            Assert.Equal((CropItemPrefix + recipe.Input, recipe.Minutes), actual[itemId]);
        }
    }

    private static List<JsonElement> ReadMachineRules()
    {
        using JsonDocument document = ReadDocument("Crops/data/machines.json");
        var rules = new List<JsonElement>();
        foreach (JsonElement change in document.RootElement.GetProperty("Changes").EnumerateArray())
        {
            if (change.GetProperty("Target").GetString() != "Data/Machines")
                continue;

            foreach (JsonProperty machine in change.GetProperty("Entries").EnumerateObject())
            {
                if (machine.Value.TryGetProperty("OutputRules", out JsonElement outputRules))
                    rules.AddRange(outputRules.EnumerateArray().Select(rule => rule.Clone()));
                else if (machine.Value.TryGetProperty("Triggers", out _))
                    rules.Add(machine.Value.Clone());
            }
        }

        return rules;
    }

    private static Dictionary<string, JsonElement> ReadEntries(string file, string target)
    {
        using JsonDocument document = ReadDocument(file);
        var entries = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (!document.RootElement.TryGetProperty("Changes", out JsonElement changes))
        {
            foreach (JsonProperty entry in document.RootElement.EnumerateObject())
                entries[entry.Name] = entry.Value.Clone();
            return entries;
        }

        foreach (JsonElement change in changes.EnumerateArray())
        {
            if (!change.TryGetProperty("Target", out JsonElement changeTarget)
                || changeTarget.GetString() != target)
                continue;
            foreach (JsonProperty entry in change.GetProperty("Entries").EnumerateObject())
                entries[entry.Name] = entry.Value.Clone();
        }
        return entries;
    }

    private static JsonDocument ReadDocument(string file)
    {
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content", file)));
    }

    private static string RemoveObjectPrefix(string itemId)
    {
        Assert.StartsWith("(O)", itemId, StringComparison.Ordinal);
        return itemId.Substring(3);
    }
}
