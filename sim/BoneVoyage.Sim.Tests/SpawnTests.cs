using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class SpawnTests
{
    [Fact]
    public void MarcherSpawnsOncePerSecondAtBaseRate()
    {
        var sim = new Simulation(TestBalance.Make()); // rate 1.0/s, tick 0.1s
        var spawned = 0;
        for (int i = 0; i < 10; i++)
            spawned += sim.Tick().OfType<SkeletonsSpawned>().Sum(e => e.Count);
        Assert.Equal(1, spawned);
        Assert.Equal(1, sim.State.Counts[(int)SkeletonTypeId.Marcher, 0]);
    }

    [Fact]
    public void ShieldbearerDoesNotSpawnAtLevelZero()
    {
        var sim = new Simulation(TestBalance.Make()); // SB base rate 0.0
        for (int i = 0; i < 100; i++) sim.Tick();
        Assert.Equal(0, sim.State.Counts[(int)SkeletonTypeId.Shieldbearer, 0]);
    }

    [Fact]
    public void InitialStateComesFromBalance()
    {
        var sim = new Simulation(TestBalance.Make());
        Assert.Equal(0.0, sim.State.Bones);
        Assert.Equal(1, sim.State.CycleTier);
        Assert.Equal(100.0, sim.State.WallHp); // BaseWallHp tier 1
    }
}
