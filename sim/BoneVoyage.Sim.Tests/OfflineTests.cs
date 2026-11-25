using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class OfflineTests
{
    [Fact]
    public void ZeroElapsedChangesNothing()
    {
        var sim = new Simulation(TestBalance.Make());
        var report = OfflineCalculator.Run(sim, 0.0);
        Assert.Equal(0, report.Recycled);
        Assert.Equal(0.0, report.SimulatedSeconds);
        Assert.False(report.Capped);
        Assert.Equal(0.0, sim.State.Bones, 10);
    }

    [Fact]
    public void NegativeElapsedIsTreatedAsZero()
    {
        var sim = new Simulation(TestBalance.Make());
        var report = OfflineCalculator.Run(sim, -3600.0);
        Assert.Equal(0.0, report.ElapsedSeconds);
        Assert.Equal(0.0, report.SimulatedSeconds);
    }

    [Fact]
    public void ReplayMatchesLiveTickingExactly()
    {
        var live = new Simulation(TestBalance.Make());
        var offline = new Simulation(TestBalance.Make());
        for (int i = 0; i < 1200; i++) live.Tick();          // 120 s ticked live
        OfflineCalculator.Run(offline, 120.0);               // 120 s replayed
        Assert.Equal(live.State.Bones, offline.State.Bones, 10);
        Assert.Equal(live.State.Gold, offline.State.Gold, 10);
        Assert.Equal(live.State.TotalFalls, offline.State.TotalFalls);
        Assert.Equal(live.State.WallHp, offline.State.WallHp, 10);
    }

    [Fact]
    public void ElapsedBeyondCapIsClamped()
    {
        var sim = new Simulation(TestBalance.Make());
        var report = OfflineCalculator.Run(sim, OfflineCalculator.CapSeconds + 7200.0);
        Assert.True(report.Capped);
        Assert.Equal(OfflineCalculator.CapSeconds, report.SimulatedSeconds, 3);
    }

    [Fact]
    public void ReportCountsRecycledAndEarnings()
    {
        var sim = new Simulation(TestBalance.Make());
        var report = OfflineCalculator.Run(sim, 300.0);
        Assert.True(report.Recycled > 0);
        Assert.True(report.BonesEarned > 0);
        Assert.True(report.GoldEarned > 0);
        Assert.False(report.Capped);
    }
}
