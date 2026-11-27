using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace CarnivorousPlants.GameAcceptanceTests;

public sealed class ModEntry : Mod
{
    private const string ModItemPrefix = "CarnivorousCrops.ContentPack_";
    private const string ObjectPrefix = "(O)";
    private const string ObjectCustomFieldPrefix = "CarnivorousCrops.ContentPack/";
    private bool _hasRun;

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.SaveLoaded += (_, _) => RunTestsOnce();
    }

    private void RunTestsOnce()
    {
        if (_hasRun)
            return;

        _hasRun = true;
        int passed = 0;
        int failed = 0;

        RunTest("insect and bait items load in the game", CheckInsectAndBaitItems, ref passed, ref failed);
        RunTest("crop schedules and harvest items load in the game", CheckCropSchedules, ref passed, ref failed);
        RunTest("artisan machines have the configured outputs", CheckArtisanMachineOutputs, ref passed, ref failed);
        RunTest("bug nets accept bait and reject insects as bait", CheckBugNetAttachmentRules, ref passed, ref failed);
        RunTest("all legendary insects load with legendary metadata", CheckLegendaryInsects, ref passed, ref failed);

        Monitor.Log(
            $"Acceptance suite complete: {passed} passed, {failed} failed.",
            failed == 0 ? LogLevel.Info : LogLevel.Error
        );
    }

    private void RunTest(string name, Action test, ref int passed, ref int failed)
    {
        try
        {
            test();
            passed++;
            Monitor.Log($"PASS {name}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            failed++;
            Monitor.Log($"FAIL {name}: {ex}", LogLevel.Error);
        }
    }

    private static void CheckInsectAndBaitItems()
    {
        int insectCount = Game1.objectData.Count(entry =>
            entry.Value.CustomFields?.ContainsKey(ObjectCustomFieldPrefix + "InsectType") == true
        );
        Require(insectCount == 268, $"Expected 268 loaded insect items, found {insectCount}.");

        Item monarch = CreateItem(ObjectPrefix + ModItemPrefix + "InsectMonarchButterfly");
        Require(monarch.HasContextTag("carnivorousplants_insect"), "Monarch Butterfly is missing its insect tag.");
        Require(
            monarch.GetContextTags().Contains("carnivorousplants_insect"),
            "The loaded Monarch Butterfly doesn't expose its insect context tag."
        );

        Item proteinBait = CreateItem(ObjectPrefix + ModItemPrefix + "ProteinBait");
        Require(proteinBait.HasContextTag("insect_bait"), "Protein Bait is missing its bait tag.");
    }

    private static void CheckCropSchedules()
    {
        var expectedCrops = new Dictionary<string, (string Harvest, string Seasons, int[] Phases, int RegrowDays)>
        {
            ["FlytrapSeeds"] = ("Flytrap", "spring,summer", new[] { 1, 2, 2, 3, 4 }, -1),
            ["PitcherSeeds"] = ("Pitcher", "summer", new[] { 2, 2, 3, 3, 4 }, -1),
            ["SundewSeeds"] = ("Sundew", "summer,fall", new[] { 1, 1, 2, 3, 3 }, -1),
            ["TrumpetSeeds"] = ("TrumpetTube", "spring,summer", new[] { 2, 2, 3, 3, 4 }, 5),
            ["ButterwortSeeds"] = ("ButterwortLeaf", "spring,fall", new[] { 1, 2, 2, 3, 4 }, 4),
            ["CobraSeeds"] = ("CobraHood", "summer,fall", new[] { 2, 3, 3, 4, 6 }, -1),
            ["AstralSeeds"] = ("AstralBlossom", "spring,summer,fall", new[] { 3, 5, 5, 7, 8 }, 14),
            ["AustralianPitcherSeeds"] = ("AustralianPitcher", "spring,fall", new[] { 2, 3, 3, 4, 4 }, -1),
            ["SunPitcherSeeds"] = ("SunPitcher", "summer", new[] { 2, 3, 4, 4, 5 }, 5),
            ["RainbowPlantSeeds"] = ("RainbowPlant", "spring,summer", new[] { 1, 2, 2, 3, 4 }, -1),
            ["BladderwortSeeds"] = ("Bladderwort", "spring,summer", new[] { 1, 2, 2, 2, 3 }, -1),
            ["DewyPineSeeds"] = ("DewyPine", "summer", new[] { 2, 2, 3, 3, 4 }, 5),
            ["WaterwheelPlantSeeds"] = ("WaterwheelPlant", "summer,fall", new[] { 2, 3, 3, 4, 4 }, 6)
        };

        Require(Game1.cropData.Count(key => key.Key.StartsWith(ModItemPrefix, StringComparison.Ordinal)) == expectedCrops.Count,
            "The loaded crop data has an unexpected number of Carnivorous Crops entries.");

        foreach (var (seedId, expected) in expectedCrops)
        {
            string qualifiedSeedId = ModItemPrefix + seedId;
            if (!Game1.cropData.TryGetValue(qualifiedSeedId, out var crop) || crop is null)
                throw new InvalidOperationException($"Missing crop data for {qualifiedSeedId}.");

            Require(crop.HarvestItemId == ModItemPrefix + expected.Harvest, $"Wrong harvest item for {qualifiedSeedId}.");
            Require(string.Join(",", crop.Seasons) == expected.Seasons, $"Wrong growing seasons for {qualifiedSeedId}.");
            Require(crop.DaysInPhase.SequenceEqual(expected.Phases), $"Wrong growth schedule for {qualifiedSeedId}.");
            Require(crop.RegrowDays == expected.RegrowDays, $"Wrong regrow period for {qualifiedSeedId}.");
            Require(
                Game1.objectData.ContainsKey(crop.HarvestItemId),
                $"Harvest item '{crop.HarvestItemId}' is missing from loaded object data."
            );
            CreateItem(ObjectPrefix + crop.HarvestItemId);
        }
    }

    private static void CheckArtisanMachineOutputs()
    {
        var expectedOutputs = new Dictionary<string, (string Input, int Minutes)>
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
        var actualOutputs = new Dictionary<string, (string Input, int Minutes)>(StringComparer.Ordinal);

        AddMachineOutputs(
            "(BC)" + ModItemPrefix + "BotanicalExtractor",
            actualOutputs
        );
        AddMachineOutputs("(BC)12", actualOutputs);
        AddMachineOutputs("(BC)15", actualOutputs);

        Require(actualOutputs.Count == expectedOutputs.Count,
            $"Expected {expectedOutputs.Count} custom machine outputs, found {actualOutputs.Count}.");

        foreach (var (output, expected) in expectedOutputs)
        {
            string qualifiedOutputId = ObjectPrefix + ModItemPrefix + output;
            string qualifiedInputId = ObjectPrefix + ModItemPrefix + expected.Input;
            Require(actualOutputs.TryGetValue(qualifiedOutputId, out var actual), $"Missing machine output {qualifiedOutputId}.");
            Require(actual.Input == qualifiedInputId, $"Wrong input configured for {qualifiedOutputId}.");
            Require(actual.Minutes == expected.Minutes, $"Wrong processing time configured for {qualifiedOutputId}.");
            CreateItem(qualifiedOutputId);
        }
    }

    private static void AddMachineOutputs(
        string machineItemId,
        IDictionary<string, (string Input, int Minutes)> outputs
    )
    {
        if (CreateItem(machineItemId) is not SObject machine)
            throw new InvalidOperationException($"'{machineItemId}' isn't a machine object.");

        var machineData = machine.GetMachineData()
            ?? throw new InvalidOperationException($"No machine data loaded for '{machineItemId}'.");

        foreach (var rule in machineData.OutputRules)
        {
            foreach (var output in rule.OutputItem)
            {
                if (!output.ItemId.StartsWith(ObjectPrefix + ModItemPrefix, StringComparison.Ordinal))
                    continue;

                var trigger = rule.Triggers.FirstOrDefault(item =>
                    !string.IsNullOrEmpty(item.RequiredItemId)
                ) ?? throw new InvalidOperationException($"Machine output '{output.ItemId}' has no item trigger.");

                Require(
                    outputs.TryAdd(output.ItemId, (trigger.RequiredItemId!, rule.MinutesUntilReady)),
                    $"Duplicate loaded machine output '{output.ItemId}'."
                );
                Require(output.MinStack == 1 && output.MaxStack == 1, $"Wrong output stack size for '{output.ItemId}'.");
                Require(trigger.RequiredCount == 1, $"Wrong required input count for '{output.ItemId}'.");
            }
        }
    }

    private static void CheckBugNetAttachmentRules()
    {
        SObject bait = CreateObject(ObjectPrefix + ModItemPrefix + "ProteinBait");
        SObject insect = CreateObject(ObjectPrefix + ModItemPrefix + "InsectMonarchButterfly");

        CheckNet("(T)" + ModItemPrefix + "BugNet", bait, insect);
        CheckNet("(T)" + ModItemPrefix + "IridiumBugNet", bait, insect);
    }

    private static void CheckNet(string itemId, SObject bait, SObject insect)
    {
        if (CreateItem(itemId) is not Tool net)
            throw new InvalidOperationException($"'{itemId}' couldn't be created as a tool.");

        Require(net.attachments.Length == 1, $"'{itemId}' should have exactly one bait slot.");
        Require(net.canThisBeAttached(bait), $"'{itemId}' rejected valid insect bait.");
        Require(!net.canThisBeAttached(insect), $"'{itemId}' accepted an insect as bait.");
    }

    private static void CheckLegendaryInsects()
    {
        string[] legendaryIds =
        {
            "InsectGlowingCaveButterfly",
            "InsectLavaButterfly",
            "InsectIceButterfly",
            "InsectDesertButterfly",
            "InsectVoidMoth",
            "InsectPrismaticButterfly"
        };

        int loadedLegendaryCount = Game1.objectData.Count(entry =>
            entry.Value.CustomFields is not null
            && entry.Value.CustomFields.TryGetValue(ObjectCustomFieldPrefix + "Rarity", out string? rarity)
            && rarity.Equals("legendary", StringComparison.OrdinalIgnoreCase)
        );
        Require(loadedLegendaryCount == legendaryIds.Length,
            $"Expected {legendaryIds.Length} loaded legendary insects, found {loadedLegendaryCount}.");

        foreach (string insectId in legendaryIds)
        {
            string itemId = ModItemPrefix + insectId;
            if (!Game1.objectData.TryGetValue(itemId, out var data) || data is null)
                throw new InvalidOperationException($"Missing legendary insect '{itemId}'.");

            Require(
                data.CustomFields is not null
                    && data.CustomFields.TryGetValue(ObjectCustomFieldPrefix + "Rarity", out string? rarity)
                    && rarity.Equals("legendary", StringComparison.OrdinalIgnoreCase),
                $"'{itemId}' isn't marked legendary."
            );
            Require(
                data.CustomFields is not null
                    && data.CustomFields.TryGetValue(ObjectCustomFieldPrefix + "IsLegendary", out string? isLegendary)
                    && bool.TryParse(isLegendary, out bool parsed)
                    && parsed,
                $"'{itemId}' is missing the legendary flag."
            );
        }
    }

    private static Item CreateItem(string itemId)
    {
        return ItemRegistry.Create(itemId, allowNull: true)
            ?? throw new InvalidOperationException($"Item '{itemId}' couldn't be created.");
    }

    private static SObject CreateObject(string itemId)
    {
        return CreateItem(itemId) as SObject
            ?? throw new InvalidOperationException($"Item '{itemId}' isn't an object.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
