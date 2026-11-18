using System.Collections.Generic;
using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class TowerTests
{
    // Wall too big to fall during the test: keeps CycleTier at 1 so the
    // tower DPS multiplier stays 1.0 while a parked horde leaks into the wall.
    private static Simulation Tier1Sim()
    {
        var b = TestBalance.Make();
        var kingdoms = new List<KingdomDef> { b.Kingdoms[0] with { BaseWallHp = 1e12 }, b.Kingdoms[1] };
        return new Simulation(b with { Kingdoms = kingdoms });
    }

    // TestBalance: ArrowTower zone 1, dps 10. Marcher hp 10, SB hp 50.

    [Fact]
    public void TowerKillsOneMarcherPerSecond()
    {
        var sim = Tier1Sim();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 1] = 1000; // park a horde in the tower zone
        var kills = 0;
        for (int i = 0; i < 10; i++) // 1 second
            kills += sim.Tick().OfType<SkeletonsDied>()
                .Where(e => e.Cause == DeathCause.Tower).Sum(e => e.Count);
        Assert.Equal(1, kills); // 10 dps * 1s / 10 hp
    }

    [Fact]
    public void ShieldbearersSoakBeforeMarchers()
    {
        var sim = Tier1Sim();
        int m = (int)SkeletonTypeId.Marcher, s = (int)SkeletonTypeId.Shieldbearer;
        sim.State.Counts[m, 1] = 1000;
        sim.State.Counts[s, 1] = 1000;
        var deaths = new System.Collections.Generic.List<SkeletonsDied>();
        for (int i = 0; i < 50; i++) // 5s = 50 damage = 1 SB death, 0 marcher deaths
            deaths.AddRange(sim.Tick().OfType<SkeletonsDied>().Where(e => e.Cause == DeathCause.Tower));
        Assert.Equal(0, deaths.Where(d => d.Type == SkeletonTypeId.Marcher).Sum(d => d.Count));
        Assert.Equal(1, deaths.Where(d => d.Type == SkeletonTypeId.Shieldbearer).Sum(d => d.Count));
    }

    [Fact]
    public void EmptyZoneDoesNotBankDamage()
    {
        var sim = new Simulation(TestBalance.Make());
        for (int i = 0; i < 100; i++) sim.Tick(); // tower zone mostly empty early
        // Residue below one marcher HP is fine; the invariant is no unbounded banking.
        Assert.True(sim.State.DamagePool[1] < 10.0);
    }

    [Fact]
    public void TwoTowersInSameZoneShareOnePoolWithoutDoubleReset()
    {
        var b = TestBalance.Make();
        var sim = new Simulation(b with { Kingdoms = new()
        {
            b.Kingdoms[0] with
            {
                BaseWallHp = 1e12,
                Towers = new() { new("ArrowTower", 1, 10.0), new("ArrowTower", 1, 10.0) }
            }
        }});
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 1] = 1000;
        var kills = 0;
        for (int i = 0; i < 10; i++) // 1s * 20 combined dps / 10 hp = 2 kills
            kills += sim.Tick().OfType<SkeletonsDied>()
                .Where(e => e.Cause == DeathCause.Tower).Sum(e => e.Count);
        Assert.Equal(2, kills);
    }
}
