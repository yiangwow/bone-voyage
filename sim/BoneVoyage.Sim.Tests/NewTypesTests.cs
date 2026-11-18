using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class NewTypesTests
{
    private static Simulation Sim() => new(TestBalance.Make());

    [Fact]
    public void SprinterSpawnsAfterRatePurchase()
    {
        var sim = Sim();
        sim.State.Bones = 1000;
        Assert.True(sim.TryPurchase(UpgradeId.SprinterRate)); // rate 0.2/s
        var spawned = 0;
        for (int i = 0; i < 50; i++) // 5s * 0.2/s = 1
            spawned += sim.Tick().OfType<SkeletonsSpawned>()
                .Where(e => e.Type == SkeletonTypeId.Sprinter).Sum(e => e.Count);
        Assert.Equal(1, spawned);
    }

    [Fact]
    public void SprinterTransitsFasterThanMarcher()
    {
        // speed 10 vs 5 => sprinter zone transit 10 ticks, marcher 20
        var sim = Sim();
        int s = (int)SkeletonTypeId.Sprinter, m = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[s, 0] = 1;
        sim.State.Counts[m, 0] = 1;
        for (int i = 0; i < 10; i++) sim.Tick();
        Assert.Equal(1, sim.State.Counts[s, 1]); // sprinter advanced
        Assert.Equal(0, sim.State.Counts[m, 1]); // marcher has not (spawner adds to zone 0 only)
    }

    [Fact]
    public void KillPriorityFollowsEnumOrderAfterShieldbearers()
    {
        // Park sprinters and marchers in tower zone with no shieldbearers:
        // pool must kill marchers before sprinters.
        var sim = new Simulation(TestBalance.Make() with
            { Kingdoms = new() { TestBalance.Make().Kingdoms[0] with { BaseWallHp = 1e12 } } });
        int m = (int)SkeletonTypeId.Marcher, s = (int)SkeletonTypeId.Sprinter;
        sim.State.Counts[m, 1] = 1000;
        sim.State.Counts[s, 1] = 1000;
        var deaths = new System.Collections.Generic.List<SkeletonsDied>();
        for (int i = 0; i < 10; i++) // 1s = 10 dmg = exactly 1 marcher (hp 10), 0 sprinters
            deaths.AddRange(sim.Tick().OfType<SkeletonsDied>().Where(e => e.Cause == DeathCause.Tower));
        Assert.Equal(1, deaths.Where(d => d.Type == SkeletonTypeId.Marcher).Sum(d => d.Count));
        Assert.Equal(0, deaths.Where(d => d.Type == SkeletonTypeId.Sprinter).Sum(d => d.Count));
    }
}
