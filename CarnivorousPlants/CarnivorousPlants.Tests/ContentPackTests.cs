using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CarnivorousPlants.Core;
using Xunit;

namespace CarnivorousPlants.Tests;

public sealed class ContentPackTests
{
    private const string ModItemPrefix = "{{ModId}}_";
    private const string CustomFieldPrefix = "CarnivorousCrops.ContentPack/";

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
        Dictionary<string, JsonElement> crops = ReadEntries("content.json", "Data/Crops");
        Dictionary<string, JsonElement> objects = ReadEntries("content.json", "Data/Objects");

        Assert.Equal(expected.Count, crops.Count);
        foreach (var (seedId, definition) in expected)
        {
            JsonElement crop = crops[ModItemPrefix + seedId];
            Assert.Equal(definition.Seasons, string.Join(",", crop.GetProperty("Seasons").EnumerateArray().Select(value => value.GetString())));
            Assert.Equal(definition.Phases, string.Join(",", crop.GetProperty("DaysInPhase").EnumerateArray().Select(value => value.GetInt32())));
            Assert.All(crop.GetProperty("DaysInPhase").EnumerateArray(), phase => Assert.True(phase.GetInt32() > 0));
            Assert.Equal(definition.RegrowDays, crop.GetProperty("RegrowDays").GetInt32());
            Assert.Equal(ModItemPrefix + definition.Harvest, crop.GetProperty("HarvestItemId").GetString());
            Assert.Contains(ModItemPrefix + definition.Harvest, objects.Keys);
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
        Dictionary<string, JsonElement> artisanGoods = ReadEntries("data/artisan_goods.json", "Data/Objects");
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
            string itemId = ModItemPrefix + goodId;
            Assert.Contains(itemId, artisanGoods.Keys);
            Assert.Equal((ModItemPrefix + recipe.Input, recipe.Minutes), actual[itemId]);
        }
    }

    [Fact]
    public void BaitDefinitionsRestrictTypesAndRarityAsIntended()
    {
        var expected = new Dictionary<string, (string Types, bool CanCatchRare, bool CanCatchLegendary)>
        {
            ["ProteinBait"] = ("beetle,ant,wasp,mantis,true bug,arachnid,centipede,crustacean,isopod,mollusk", false, false),
            ["SweetBait"] = ("butterfly,bee,moth,cicada,grasshopper,cricket,stick insect,termite", false, false),
            ["StickyBait"] = ("fly,dragonfly,lacewing,planthopper,scorpionfly", false, false),
            ["AromaticBait"] = ("butterfly,bee,moth,cicada,grasshopper,cricket,stick insect,termite", true, false),
            ["AdhesiveBait"] = ("fly,dragonfly,lacewing,planthopper,scorpionfly", true, false),
            ["VenomBait"] = ("beetle,ant,wasp,mantis,true bug,arachnid,centipede,crustacean,isopod,mollusk", true, false),
            ["CelestialBait"] = ("*", true, true)
        };
        Dictionary<string, JsonElement> baits = ReadEntries("data/bait.json", "Data/Objects");

        Assert.Equal(expected.Count, baits.Count);
        foreach (var (baitId, definition) in expected)
        {
            JsonElement fields = baits[ModItemPrefix + baitId].GetProperty("CustomFields");
            string typesText = GetField(fields, "CatchesInsectTypes");
            bool canCatchRare = bool.Parse(GetField(fields, "CanCatchRare"));
            bool canCatchLegendary = bool.Parse(GetField(fields, "CanCatchLegendary"));
            string[] types = ParseList(typesText);
            var rules = new BaitCatchRules(
                types.Contains("*", StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(types, StringComparer.OrdinalIgnoreCase),
                canCatchRare,
                canCatchLegendary
            );

            Assert.Equal(definition.Types, typesText);
            Assert.Equal(definition.CanCatchRare, rules.CanCatchRare);
            Assert.Equal(definition.CanCatchLegendary, rules.CanCatchLegendary);
            Assert.Equal(definition.Types == "*", rules.CatchesType("moth") && rules.CatchesType("beetle"));
        }
    }

    [Fact]
    public void EveryBaitHasThreeCommonCatchesAvailableAtAllOutdoorTimesAndSeasons()
    {
        Dictionary<string, JsonElement> insects = ReadEntries("data/insects.json", "Data/Objects");
        Dictionary<string, JsonElement> baits = ReadEntries("data/bait.json", "Data/Objects");
        string[] allSeasons = { "Spring", "Summer", "Fall", "Winter" };

        foreach (var (baitId, bait) in baits)
        {
            string[] baitTypes = ParseList(GetField(bait.GetProperty("CustomFields"), "CatchesInsectTypes"));
            bool catchesAllTypes = baitTypes.Contains("*", StringComparer.OrdinalIgnoreCase);
            int commonCatchCount = 0;

            foreach (JsonElement insect in insects.Values)
            {
                JsonElement fields = insect.GetProperty("CustomFields");
                if (GetField(fields, "Rarity") != "common"
                    || !ParseList(GetField(fields, "CatchLocations")).Contains("Outdoors", StringComparer.OrdinalIgnoreCase)
                    || !allSeasons.All(season => ParseList(GetField(fields, "CatchSeasons"))
                        .Contains(season, StringComparer.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string insectType = GetField(fields, "InsectType");
                if (!catchesAllTypes && !baitTypes.Contains(insectType, StringComparer.OrdinalIgnoreCase))
                    continue;

                string[] times = GetField(fields, "CatchTimes").Split('-');
                if (times.Length == 2
                    && int.TryParse(times[0], out int startTime)
                    && int.TryParse(times[1], out int endTime)
                    && startTime <= 600
                    && endTime >= 2600)
                {
                    commonCatchCount++;
                }
            }

            Assert.True(commonCatchCount >= 3, $"{baitId} only has {commonCatchCount} common all-season outdoor catches.");
        }
    }

    [Fact]
    public void InsectDefinitionsHaveUsableAvailabilityAndRarityMetadata()
    {
        Dictionary<string, JsonElement> insects = ReadEntries("data/insects.json", "Data/Objects");
        Assert.Equal(268, insects.Count);

        foreach (var (itemId, item) in insects)
        {
            JsonElement fields = item.GetProperty("CustomFields");
            Assert.Equal("Mods/{{ModId}}/insects", item.GetProperty("Texture").GetString());
            string description = item.GetProperty("Description").GetString()!;
            Assert.False(description.StartsWith("Scientific:", StringComparison.Ordinal), itemId);
            Assert.DoesNotContain("Scientific name:", description);
            string type = GetField(fields, "InsectType");
            string rarity = GetField(fields, "Rarity");
            string[] locations = ParseList(GetField(fields, "CatchLocations"));
            string[] seasons = ParseList(GetField(fields, "CatchSeasons"));
            string[] times = GetField(fields, "CatchTimes").Split('-');
            double weight = double.Parse(GetField(fields, "CatchWeight"), System.Globalization.CultureInfo.InvariantCulture);

            Assert.False(string.IsNullOrWhiteSpace(type), itemId);
            Assert.Contains(rarity, new[] { "common", "rare", "legendary" });
            Assert.NotEmpty(locations);
            Assert.NotEmpty(seasons);
            Assert.Equal(2, times.Length);
            Assert.True(int.TryParse(times[0], out int startTime), itemId);
            Assert.True(int.TryParse(times[1], out int endTime), itemId);
            Assert.InRange(startTime, 0, endTime);
            Assert.True(weight > 0, itemId);
        }

        string[] legendaryIds =
        {
            ModItemPrefix + "InsectGlowingCaveButterfly",
            ModItemPrefix + "InsectLavaButterfly",
            ModItemPrefix + "InsectIceButterfly",
            ModItemPrefix + "InsectDesertButterfly",
            ModItemPrefix + "InsectVoidMoth",
            ModItemPrefix + "InsectPrismaticButterfly"
        };
        Assert.Equal(legendaryIds.OrderBy(value => value), insects
            .Where(entry => GetField(entry.Value.GetProperty("CustomFields"), "Rarity") == "legendary")
            .Select(entry => entry.Key)
            .OrderBy(value => value));
        Assert.All(legendaryIds, itemId =>
            Assert.Equal("true", GetField(insects[itemId].GetProperty("CustomFields"), "IsLegendary"))
        );
    }

    [Fact]
    public void RealInsectAvailabilityUsesItsSeasonLocationAndTimeMetadata()
    {
        JsonElement monarch = ReadEntries("data/insects.json", "Data/Objects")[ModItemPrefix + "InsectMonarchButterfly"];
        InsectData insect = CreateInsectData(monarch);
        var butterflyBait = new BaitCatchRules(
            catchesAllTypes: false,
            new HashSet<string> { "butterfly" },
            canCatchRare: false,
            canCatchLegendary: false
        );

        Assert.True(InsectCatchRules.IsInSeason(insect, "spring"));
        Assert.True(InsectCatchRules.IsInSeason(insect, "winter"));
        Assert.True(InsectCatchRules.IsInSeason(insect, "fall"));
        Assert.True(InsectCatchRules.IsInSeason(insect, "summer"));
        Assert.True(InsectCatchRules.IsEligibleForBait(insect, butterflyBait, 600));
        Assert.True(InsectCatchRules.IsEligibleForBait(insect, butterflyBait, 2600));
        Assert.False(InsectCatchRules.IsEligibleForBait(insect, butterflyBait, 2601));
        Assert.True(InsectLocationRules.MatchesLocation(insect.Locations[0], "Forest", false, 0, false));
        Assert.False(InsectLocationRules.MatchesLocation(insect.Locations[0], "Beach", false, 0, false));
    }

    [Fact]
    public void LegendaryInsectsHaveTheirConfiguredLocationsAndCanBeSelectedByCelestialBait()
    {
        var expectedLocations = new Dictionary<string, (string CatchLocation, string LocationName, bool IsMine, int MineLevel)>
        {
            ["InsectGlowingCaveButterfly"] = ("Mines Floor 20", "Mine", true, 20),
            ["InsectLavaButterfly"] = ("Mines Floor 100", "Mine", true, 100),
            ["InsectIceButterfly"] = ("Mines Floor 60", "Mine", true, 60),
            ["InsectDesertButterfly"] = ("Skull Cavern Floor 100", "Mine", true, 220),
            ["InsectVoidMoth"] = ("Witch's Swamp", "WitchSwamp", false, 0),
            ["InsectPrismaticButterfly"] = ("Summit", "Summit", false, 0)
        };
        Dictionary<string, JsonElement> insects = ReadEntries("data/insects.json", "Data/Objects");
        var celestialBait = new BaitCatchRules(true, new HashSet<string>(), true, true);

        foreach (var (insectName, location) in expectedLocations)
        {
            InsectData insect = CreateInsectData(insects[ModItemPrefix + insectName]);
            Assert.Equal(InsectRarity.Legendary, insect.Rarity);
            Assert.Contains("Winter", insect.Seasons);
            Assert.Contains(location.CatchLocation, insect.Locations);
            Assert.True(InsectCatchRules.IsEligibleForBait(insect, celestialBait, insect.StartTime));
            Assert.True(InsectCatchRules.IsEligibleForBait(insect, celestialBait, insect.EndTime));
            Assert.True(InsectLocationRules.MatchesLocation(
                location.CatchLocation,
                location.LocationName,
                location.IsMine,
                location.MineLevel,
                isCaldera: false
            ));

            List<InsectData> pool = InsectCatchRules.SelectRarityPool(
                new[] { insect },
                celestialBait,
                () => 0.01
            );
            Assert.Equal(new[] { insect }, pool);
        }
    }

    private static List<JsonElement> ReadMachineRules()
    {
        using JsonDocument document = ReadDocument("data/machines.json");
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
        foreach (JsonElement change in document.RootElement.GetProperty("Changes").EnumerateArray())
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

    private static string GetField(JsonElement fields, string name)
    {
        return fields.GetProperty(CustomFieldPrefix + name).GetString()!;
    }

    private static InsectData CreateInsectData(JsonElement item)
    {
        JsonElement fields = item.GetProperty("CustomFields");
        string[] timeRange = GetField(fields, "CatchTimes").Split('-');
        return new InsectData(
            item.GetProperty("Name").GetString()!,
            GetField(fields, "InsectType"),
            Enum.Parse<InsectRarity>(GetField(fields, "Rarity"), ignoreCase: true),
            ParseList(GetField(fields, "CatchLocations")),
            ParseList(GetField(fields, "CatchSeasons")),
            int.Parse(timeRange[0], System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(timeRange[1], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(GetField(fields, "CatchWeight"), System.Globalization.CultureInfo.InvariantCulture)
        );
    }

    private static string[] ParseList(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string RemoveObjectPrefix(string itemId)
    {
        Assert.StartsWith("(O)", itemId, StringComparison.Ordinal);
        return itemId.Substring(3);
    }
}
