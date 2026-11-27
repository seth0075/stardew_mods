using System.Collections.Generic;
using System.Linq;
using CarnivorousPlants.Core;
using Xunit;

namespace CarnivorousPlants.Tests;

public sealed class InsectCatchRulesTests
{
    private static readonly InsectData Butterfly = CreateInsect("Monarch", "butterfly", InsectRarity.Common, 600, 1800);
    private static readonly InsectData RareButterfly = CreateInsect("BlueMorpho", "butterfly", InsectRarity.Rare, 600, 1800);
    private static readonly InsectData LegendaryButterfly = CreateInsect("Prismatic", "butterfly", InsectRarity.Legendary, 600, 2600);
    private static readonly BaitCatchRules ButterflyBait = new(false, new HashSet<string> { "butterfly" }, false, false);
    private static readonly BaitCatchRules RareButterflyBait = new(false, new HashSet<string> { "butterfly" }, true, false);
    private static readonly BaitCatchRules CelestialBait = new(true, new HashSet<string>(), true, true);

    [Fact]
    public void CatchAvailabilityRequiresSeasonTypeAndInclusiveTimeWindow()
    {
        Assert.True(InsectCatchRules.IsInSeason(Butterfly, "spring"));
        Assert.False(InsectCatchRules.IsInSeason(Butterfly, "fall"));
        Assert.True(InsectCatchRules.IsEligibleForBait(Butterfly, ButterflyBait, 600));
        Assert.True(InsectCatchRules.IsEligibleForBait(Butterfly, ButterflyBait, 1800));
        Assert.False(InsectCatchRules.IsEligibleForBait(Butterfly, ButterflyBait, 559));
        Assert.False(InsectCatchRules.IsEligibleForBait(Butterfly, ButterflyBait, 1801));
    }

    [Fact]
    public void BaitOnlyCatchesItsConfiguredInsectTypes()
    {
        var beetle = CreateInsect("Beetle", "beetle", InsectRarity.Common, 600, 1800);

        Assert.True(InsectCatchRules.IsEligibleForBait(Butterfly, ButterflyBait, 1200));
        Assert.False(InsectCatchRules.IsEligibleForBait(beetle, ButterflyBait, 1200));
        Assert.True(InsectCatchRules.IsEligibleForBait(beetle, CelestialBait, 1200));
    }

    [Fact]
    public void CommonBaitCannotSelectRareOrLegendaryInsects()
    {
        List<InsectData> pool = InsectCatchRules.SelectRarityPool(
            new[] { Butterfly, RareButterfly, LegendaryButterfly },
            ButterflyBait,
            () => 0
        );

        Assert.Equal(new[] { Butterfly }, pool);
        Assert.Empty(InsectCatchRules.SelectRarityPool(
            new[] { RareButterfly },
            ButterflyBait,
            () => 0
        ));
        Assert.Empty(InsectCatchRules.SelectRarityPool(
            new[] { LegendaryButterfly },
            ButterflyBait,
            () => 0
        ));
    }

    [Fact]
    public void RareBaitCanSelectRareButNotLegendaryInsects()
    {
        List<InsectData> commonPool = InsectCatchRules.SelectRarityPool(
            new[] { Butterfly, RareButterfly, LegendaryButterfly },
            RareButterflyBait,
            () => 0.25
        );
        List<InsectData> rarePool = InsectCatchRules.SelectRarityPool(
            new[] { Butterfly, RareButterfly, LegendaryButterfly },
            RareButterflyBait,
            () => 0.75
        );

        Assert.Equal(new[] { Butterfly }, commonPool);
        Assert.Equal(new[] { RareButterfly }, rarePool);
        Assert.Empty(InsectCatchRules.SelectRarityPool(
            new[] { LegendaryButterfly },
            RareButterflyBait,
            () => 0
        ));
    }

    [Fact]
    public void LegendaryInsectsRequireLegendaryBaitAndHaveTheirOwnSelectionChance()
    {
        List<InsectData> legendaryPool = InsectCatchRules.SelectRarityPool(
            new[] { Butterfly, RareButterfly, LegendaryButterfly },
            CelestialBait,
            () => 0.04
        );
        List<InsectData> nonLegendaryPool = InsectCatchRules.SelectRarityPool(
            new[] { Butterfly, RareButterfly, LegendaryButterfly },
            CelestialBait,
            () => 0.06
        );

        Assert.Equal(new[] { LegendaryButterfly }, legendaryPool);
        Assert.DoesNotContain(nonLegendaryPool, insect => insect.Rarity == InsectRarity.Legendary);
    }

    [Theory]
    [InlineData("Forest", "Forest", false, 0, false, true)]
    [InlineData("Witch's Swamp", "WitchSwamp", false, 0, false, true)]
    [InlineData("Mines", "Mine", true, 120, false, true)]
    [InlineData("Mines", "Mine", true, 121, false, false)]
    [InlineData("Mines Floor 20", "Mine", true, 20, false, true)]
    [InlineData("Mines Floor 20", "Mine", true, 21, false, false)]
    [InlineData("Skull Cavern Floor 100", "Mine", true, 220, false, true)]
    [InlineData("Ginger Island", "IslandWest", false, 0, false, true)]
    [InlineData("Ginger Island Caldera", "Caldera", false, 0, true, true)]
    [InlineData("Ginger Island", "Forest", false, 0, false, false)]
    public void LocationAvailabilityHonorsNamedAndSpecialLocations(
        string catchLocation,
        string locationName,
        bool isMine,
        int mineLevel,
        bool isCaldera,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            InsectLocationRules.MatchesLocation(catchLocation, locationName, isMine, mineLevel, isCaldera)
        );
    }

    private static InsectData CreateInsect(
        string itemId,
        string type,
        InsectRarity rarity,
        int startTime,
        int endTime
    )
    {
        return new InsectData(
            itemId,
            type,
            rarity,
            new[] { "Forest" },
            new[] { "Spring" },
            startTime,
            endTime,
            1
        );
    }
}
