using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class EconomyTests
{
    private static Simulation NoTowerSim()
    {
        var b = TestBalance.Make();
        var up = new System.Collections.Generic.Dictionary<UpgradeId, UpgradeBalance>(b.Upgrades);
        // Disable spawning
        foreach (var key in b.Upgrades.Keys)
            if (key is UpgradeId.MarcherRate or UpgradeId.ShieldbearerRate or UpgradeId.SprinterRate
                or UpgradeId.BomberRate or UpgradeId.BannerRate)
                up[key] = b.Upgrades[key] with { BaseValue = 0.0 };
        return new(b with
            { Upgrades = up,
              Kingdoms = new() { b.Kingdoms[0] with
                { Towers = new(), BaseWallHp = 1e12 } } });
    }

    private static void SendOneToWall(Simulation sim, SkeletonTypeId type)
    {
        int t = (int)type, last = sim.Balance.ZoneCount - 1;
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999;
        for (int i = 0; i < 60; i++) sim.Tick();
    }

    [Fact]
    public void WallDamagePaysGold()
    {
        var sim = NoTowerSim();
        SendOneToWall(sim, SkeletonTypeId.Marcher); // wallDamage 5 * goldPerWallDamage 1.0
        Assert.Equal(5.0, sim.State.Gold, 3);
    }

    [Fact]
    public void GoldPurchaseDebitsGoldNotBones()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Bones = 1000; sim.State.Gold = 1000;
        Assert.True(sim.TryPurchase(UpgradeId.Helmets)); // cost 50 gold
        Assert.Equal(950.0, sim.State.Gold);
        Assert.Equal(1000.0, sim.State.Bones);
    }

    [Fact]
    public void GoldPurchaseFailsWhenOnlyBonesRich()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Bones = 1e9;
        Assert.False(sim.TryPurchase(UpgradeId.Helmets));
    }

    [Fact]
    public void HelmetsRaiseEffectiveHp()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Gold = 1000;
        sim.TryPurchase(UpgradeId.Helmets); // 1.05x
        Assert.Equal(10.5, sim.EffHp(SkeletonTypeId.Marcher), 3);
    }

    [Fact]
    public void RamsRaiseWallDamageAndGold()
    {
        var sim = NoTowerSim();
        sim.State.Gold = 1000;
        sim.TryPurchase(UpgradeId.BatteringRams); // 1.05x => marcher 5.25 dmg
        double goldBefore = sim.State.Gold;
        SendOneToWall(sim, SkeletonTypeId.Marcher);
        Assert.Equal(goldBefore + 5.25, sim.State.Gold, 2);
    }

    [Fact]
    public void BootsSpeedUpTransit()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Gold = 1000;
        sim.TryPurchase(UpgradeId.MarchingBoots); // fixture: +0.5 => 1.5x speed, transit 20 -> 13.33 ticks
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        for (int i = 0; i < 14; i++) sim.Tick();
        Assert.Equal(1, sim.State.Counts[t, 1]); // arrived by tick 14 (vs 20 unbuffed)
    }
}
