using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class AuraAndMoatTests
{
    private static Simulation OnMoatKingdom()
    {
        var b = TestBalance.Make();
        // Kingdom 1 (Moatholm): moat zone 0 speedMult 0.5, cannon zone 0.
        // Disable spawning to isolate moat behavior
        var up = new System.Collections.Generic.Dictionary<UpgradeId, UpgradeBalance>(b.Upgrades)
        {
            [UpgradeId.MarcherRate] = b.Upgrades[UpgradeId.MarcherRate] with { BaseValue = 0.0 }
        };
        var sim = new Simulation(b with { Upgrades = up });
        sim.State.KingdomIndex = 1;
        sim.State.WallHp = sim.WallHpFor(1, 1);
        return sim;
    }

    [Fact]
    public void MoatHalvesTransitSpeed()
    {
        var sim = OnMoatKingdom();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        for (int i = 0; i < 39; i++) sim.Tick(); // normal transit 20 ticks; moat x2 => 40
        Assert.Equal(0, sim.State.Counts[t, 1]);
        sim.Tick();
        Assert.Equal(1, sim.State.Counts[t, 1]);
    }

    [Fact]
    public void MoatMultReadsCurrentKingdom()
    {
        var sim = new Simulation(TestBalance.Make());
        Assert.Equal(1.0, sim.MoatMult(0)); // Testshire has no moat
        sim.State.KingdomIndex = 1;
        Assert.Equal(0.5, sim.MoatMult(0));
        Assert.Equal(1.0, sim.MoatMult(1));
    }

    [Fact]
    public void BannersReduceTowerDamageAccrual()
    {
        // DrPerBanner 0.10, 2 banners => 20% reduction; cap 0.4 at >=4 banners.
        var big = TestBalance.Make();
        var sim = new Simulation(big with
            { Kingdoms = new() { big.Kingdoms[0] with { BaseWallHp = 1e12 } } });
        int m = (int)SkeletonTypeId.Marcher, b = (int)SkeletonTypeId.Bannerbones;
        sim.State.Counts[m, 1] = 1000;
        sim.State.Counts[b, 1] = 2;   // 20% DR — banners are killed LAST (priority), marchers die first
        var kills = 0;
        for (int i = 0; i < 25; i++)  // 2.5s * 10 dps * 0.8 = 20 dmg = 2 marchers
            kills += sim.Tick().OfType<SkeletonsDied>()
                .Where(e => e.Cause == DeathCause.Tower && e.Type == SkeletonTypeId.Marcher)
                .Sum(e => e.Count);
        Assert.Equal(2, kills);
    }

    [Fact]
    public void BannerDrIsCapped()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Counts[(int)SkeletonTypeId.Bannerbones, 1] = 100;
        Assert.Equal(0.4, sim.BannerDr(1)); // cap, not 100*0.10
    }
}
