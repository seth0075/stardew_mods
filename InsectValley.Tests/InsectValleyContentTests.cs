using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CarnivorousPlants.Core;
using Xunit;

namespace InsectValley.Tests;

public sealed class InsectValleyContentTests
{
    private const string InsectItemPrefix = "InsectValley.Core_";
    private const string InsectCustomFieldPrefix = "InsectValley.Core/";

    [Fact]
    public void InsectValleyIsOneSmapiModAndDependsOnCarnivorousCrops()
    {
        using JsonDocument cropsManifest = ReadDocument("Crops/manifest.json");
        using JsonDocument insectManifest = ReadDocument("Insect Valley/manifest.json");

        Assert.Equal("CarnivorousCrops.ContentPack", cropsManifest.RootElement.GetProperty("UniqueID").GetString());
        Assert.Equal("InsectValley.Core", insectManifest.RootElement.GetProperty("UniqueID").GetString());
        Assert.Equal("InsectValley.Core.dll", insectManifest.RootElement.GetProperty("EntryDll").GetString());
        Assert.False(insectManifest.RootElement.TryGetProperty("ContentPackFor", out _));
        Assert.Contains(
            insectManifest.RootElement.GetProperty("Dependencies").EnumerateArray(),
            dependency => dependency.GetProperty("UniqueID").GetString() == "CarnivorousCrops.ContentPack"
                && dependency.GetProperty("IsRequired").GetBoolean()
        );
        Assert.Equal(268, ReadEntries("Insect Valley/data/insects.json", "Data/Objects").Count);
        Assert.Equal(7, ReadEntries("Insect Valley/data/bait.json", "Data/Objects").Count);
        Assert.Equal(2, ReadEntries("Insect Valley/data/tools.json", "Data/Tools").Count);
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
        Dictionary<string, JsonElement> baits = ReadEntries("Insect Valley/data/bait.json", "Data/Objects");

        Assert.Equal(expected.Count, baits.Count);
        var baitSpriteIndices = new List<int>();
        foreach (var (baitId, definition) in expected)
        {
            JsonElement bait = baits[InsectItemPrefix + baitId];
            JsonElement fields = bait.GetProperty("CustomFields");
            Assert.Equal("Mods/InsectValley.Core/bait", bait.GetProperty("Texture").GetString());
            baitSpriteIndices.Add(bait.GetProperty("SpriteIndex").GetInt32());
            string typesText = GetInsectField(fields, "CatchesInsectTypes");
            bool canCatchRare = bool.Parse(GetInsectField(fields, "CanCatchRare"));
            bool canCatchLegendary = bool.Parse(GetInsectField(fields, "CanCatchLegendary"));
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
        Assert.Equal(Enumerable.Range(0, expected.Count), baitSpriteIndices.OrderBy(index => index));

        using JsonDocument document = ReadDocument("Insect Valley/data/crafting_recipes.json");
        JsonElement recipes = document.RootElement;
        Assert.Contains(
            "CarnivorousCrops.ContentPack_FlytrapExtract",
            recipes.GetProperty(InsectItemPrefix + "ProteinBait").GetString()
        );
        Assert.Contains(
            InsectItemPrefix + "CelestialBait",
            recipes.GetProperty(InsectItemPrefix + "CelestialBait").GetString()
        );
    }

    [Fact]
    public void EveryBaitHasThreeCommonCatchesAvailableAtAllOutdoorTimesAndSeasons()
    {
        Dictionary<string, JsonElement> insects = ReadEntries("Insect Valley/data/insects.json", "Data/Objects");
        Dictionary<string, JsonElement> baits = ReadEntries("Insect Valley/data/bait.json", "Data/Objects");
        string[] allSeasons = { "Spring", "Summer", "Fall", "Winter" };

        foreach (var (baitId, bait) in baits)
        {
            string[] baitTypes = ParseList(GetInsectField(bait.GetProperty("CustomFields"), "CatchesInsectTypes"));
            bool catchesAllTypes = baitTypes.Contains("*", StringComparer.OrdinalIgnoreCase);
            int commonCatchCount = 0;

            foreach (JsonElement insect in insects.Values)
            {
                JsonElement fields = insect.GetProperty("CustomFields");
                if (GetInsectField(fields, "Rarity") != "common"
                    || !ParseList(GetInsectField(fields, "CatchLocations")).Contains("Outdoors", StringComparer.OrdinalIgnoreCase)
                    || !allSeasons.All(season => ParseList(GetInsectField(fields, "CatchSeasons"))
                        .Contains(season, StringComparer.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string insectType = GetInsectField(fields, "InsectType");
                if (!catchesAllTypes && !baitTypes.Contains(insectType, StringComparer.OrdinalIgnoreCase))
                    continue;

                string[] times = GetInsectField(fields, "CatchTimes").Split('-');
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
        Dictionary<string, JsonElement> insects = ReadEntries("Insect Valley/data/insects.json", "Data/Objects");
        Assert.Equal(268, insects.Count);

        foreach (var (itemId, item) in insects)
        {
            JsonElement fields = item.GetProperty("CustomFields");
            Assert.Equal("Mods/InsectValley.Core/insects", item.GetProperty("Texture").GetString());
            string description = item.GetProperty("Description").GetString()!;
            Assert.False(description.StartsWith("Scientific:", StringComparison.Ordinal), itemId);
            Assert.DoesNotContain("Scientific name:", description);
            string type = GetInsectField(fields, "InsectType");
            string rarity = GetInsectField(fields, "Rarity");
            string[] locations = ParseList(GetInsectField(fields, "CatchLocations"));
            string[] seasons = ParseList(GetInsectField(fields, "CatchSeasons"));
            string[] times = GetInsectField(fields, "CatchTimes").Split('-');
            double weight = double.Parse(GetInsectField(fields, "CatchWeight"), System.Globalization.CultureInfo.InvariantCulture);

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
            InsectItemPrefix + "InsectGlowingCaveButterfly",
            InsectItemPrefix + "InsectLavaButterfly",
            InsectItemPrefix + "InsectIceButterfly",
            InsectItemPrefix + "InsectDesertButterfly",
            InsectItemPrefix + "InsectVoidMoth",
            InsectItemPrefix + "InsectPrismaticButterfly"
        };
        Assert.Equal(legendaryIds.OrderBy(value => value), insects
        .Where(entry => GetInsectField(entry.Value.GetProperty("CustomFields"), "Rarity") == "legendary")
            .Select(entry => entry.Key)
            .OrderBy(value => value));
        Assert.All(legendaryIds, itemId =>
            Assert.Equal("true", GetInsectField(insects[itemId].GetProperty("CustomFields"), "IsLegendary"))
        );
    }

    [Fact]
    public void RealInsectAvailabilityUsesItsSeasonLocationAndTimeMetadata()
    {
        JsonElement monarch = ReadEntries("Insect Valley/data/insects.json", "Data/Objects")[InsectItemPrefix + "InsectMonarchButterfly"];
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
        Dictionary<string, JsonElement> insects = ReadEntries("Insect Valley/data/insects.json", "Data/Objects");
        var celestialBait = new BaitCatchRules(true, new HashSet<string>(), true, true);

        foreach (var (insectName, location) in expectedLocations)
        {
            InsectData insect = CreateInsectData(insects[InsectItemPrefix + insectName]);
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

    private static string GetInsectField(JsonElement fields, string name)
    {
        return fields.GetProperty(InsectCustomFieldPrefix + name).GetString()!;
    }

    private static InsectData CreateInsectData(JsonElement item)
    {
        JsonElement fields = item.GetProperty("CustomFields");
        string[] timeRange = GetInsectField(fields, "CatchTimes").Split('-');
        return new InsectData(
            item.GetProperty("Name").GetString()!,
            GetInsectField(fields, "InsectType"),
            Enum.Parse<InsectRarity>(GetInsectField(fields, "Rarity"), ignoreCase: true),
            ParseList(GetInsectField(fields, "CatchLocations")),
            ParseList(GetInsectField(fields, "CatchSeasons")),
            int.Parse(timeRange[0], System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(timeRange[1], System.Globalization.CultureInfo.InvariantCulture),
            double.Parse(GetInsectField(fields, "CatchWeight"), System.Globalization.CultureInfo.InvariantCulture)
        );
    }

    private static string[] ParseList(string value)
    {
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

}
