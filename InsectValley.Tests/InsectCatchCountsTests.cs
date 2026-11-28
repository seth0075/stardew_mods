using System.Collections.Generic;
using CarnivorousPlants.Core;
using Xunit;

namespace InsectValley.Tests;

public sealed class InsectCatchCountsTests
{
    [Fact]
    public void CatchCountsPersistAndIncrementIndependentlyPerInsect()
    {
        var modData = new Dictionary<string, string>();

        Assert.Equal(0, InsectCatchCounts.GetCaughtCount(modData, "InsectValley.Core_InsectMonarchButterfly"));

        InsectCatchCounts.RecordCatch(modData, "InsectValley.Core_InsectMonarchButterfly");
        InsectCatchCounts.RecordCatch(modData, "InsectValley.Core_InsectMonarchButterfly");
        InsectCatchCounts.RecordCatch(modData, "InsectValley.Core_InsectOwlButterfly");

        Assert.Equal(2, InsectCatchCounts.GetCaughtCount(modData, "InsectValley.Core_InsectMonarchButterfly"));
        Assert.Equal(1, InsectCatchCounts.GetCaughtCount(modData, "InsectValley.Core_InsectOwlButterfly"));
    }
}
