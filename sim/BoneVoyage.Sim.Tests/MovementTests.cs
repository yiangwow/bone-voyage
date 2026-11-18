using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class MovementTests
{
    // MarcherRate 0 => no ambient spawns; counts are fully controlled.
    private static Simulation NoSpawnSim()
    {
        var b = TestBalance.Make();
        var up = new System.Collections.Generic.Dictionary<UpgradeId, UpgradeBalance>(b.Upgrades)
        {
            [UpgradeId.MarcherRate] = b.Upgrades[UpgradeId.MarcherRate] with { BaseValue = 0.0 }
        };
        return new Simulation(b with { Upgrades = up });
    }

    [Fact]
    public void SingleMarcherAdvancesExactlyOneZonePerTransitTime()
    {
        var sim = NoSpawnSim();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        for (int i = 0; i < 19; i++) sim.Tick();
        Assert.Equal(1, sim.State.Counts[t, 0]); // not yet: transit = 20 ticks
        Assert.Equal(0, sim.State.Counts[t, 1]);
        sim.Tick(); // tick 20
        Assert.Equal(0, sim.State.Counts[t, 0]); // moved exactly one zone, not two
        Assert.Equal(1, sim.State.Counts[t, 1]);
    }

    [Fact]
    public void NoSkeletonSkipsAZoneInOneTick()
    {
        var sim = NoSpawnSim();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        sim.State.MoveAcc[t, 0] = 0.999; // primed to move NOW
        sim.State.MoveAcc[t, 1] = 0.999; // zone 1 also primed
        sim.Tick();
        // The mover entered zone 1 this tick; it must NOT also exit zone 1 this tick.
        Assert.Equal(1, sim.State.Counts[t, 1]);
    }

    [Fact]
    public void ThroughputIsConservedNoDoubleCountOrLoss()
    {
        var sim = new Simulation(TestBalance.Make() with
            { Kingdoms = new() { TestBalance.Make().Kingdoms[0] with
                { Towers = new(), BaseWallHp = 1e12 } } });
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 100;
        int wallDeaths = 0;
        for (int i = 0; i < 3000; i++)
            foreach (var e in sim.Tick())
                if (e is SkeletonsDied d && d.Cause == DeathCause.WallCrumble && d.Type == SkeletonTypeId.Marcher)
                    wallDeaths += d.Count;
        int alive = sim.State.Counts[t, 0] + sim.State.Counts[t, 1];
        int spawned = 0; // MarcherRate 1.0/s * 300s
        // spawned count: base rate 1.0/s for 300s = 300 spawns
        spawned = 300;
        Assert.Equal(100 + spawned, wallDeaths + alive); // exact conservation
    }
}
