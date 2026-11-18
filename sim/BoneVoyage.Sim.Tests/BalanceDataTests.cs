using System;
using System.IO;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class BalanceDataTests
{
    [Fact]
    public void LoadsShippedBalanceJson()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json"));
        var b = BalanceData.Load(json);
        Assert.Equal(0.1, b.TickSeconds);
        Assert.Equal(10, b.ZoneCount);
        Assert.Equal(5, b.Skeletons.Count);
        Assert.Equal(3, b.Kingdoms.Count);
        Assert.Equal(PerkId.GoldMult, b.Kingdoms[2].Perk.Id);
        Assert.Equal(Currency.Gold, b.Upgrades[UpgradeId.Helmets].Currency);
        Assert.NotEmpty(b.Kingdoms[1].Moats);
    }

    [Fact]
    public void ValidateRejectsOutOfRangeTowerZone()
    {
        var b = TestBalance.Make();
        var bad = b with { Kingdoms = new()
            { b.Kingdoms[0] with { Towers = new() { new("ArrowTower", Zone: 99, Dps: 1.0) } } } };
        var ex = Assert.Throws<ArgumentException>(() => bad.Validate());
        Assert.Contains("Zone", ex.Message);
    }

    [Fact]
    public void ValidateRejectsNonPositiveWallHp()
    {
        var b = TestBalance.Make();
        var bad = b with { Kingdoms = new() { b.Kingdoms[0] with { BaseWallHp = 0.0 } } };
        Assert.Throws<ArgumentException>(() => bad.Validate());
    }

    [Fact]
    public void ValidateRejectsWallGrowthAtOrBelowOne()
    {
        var b = TestBalance.Make();
        var bad = b with { Cycle = b.Cycle with { WallHpGrowth = 1.0 } };
        Assert.Throws<ArgumentException>(() => bad.Validate());
    }
}
