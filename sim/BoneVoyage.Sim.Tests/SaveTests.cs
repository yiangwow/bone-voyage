using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class SaveTests
{
    [Fact]
    public void RoundTripRestoresEconomyAndProgress()
    {
        var b = TestBalance.Make();
        var sim = new Simulation(b);
        sim.State.Bones = 123.5; sim.State.Gold = 77.25;
        sim.State.KingdomIndex = 1; sim.State.CycleTier = 3;
        sim.State.PerksUnlocked = 2; sim.State.TotalFalls = 5;
        sim.State.WallHp = 42.0;
        sim.State.UpgradeLevels[UpgradeId.Helmets] = 4;
        sim.State.Counts[(int)SkeletonTypeId.Bomber, 1] = 9;

        var json = SaveCodec.ToJson(sim.State, timestampUtcSeconds: 1753180000);
        var restored = new Simulation(b, SaveCodec.Parse(json));

        Assert.Equal(123.5, restored.State.Bones);
        Assert.Equal(77.25, restored.State.Gold);
        Assert.Equal(1, restored.State.KingdomIndex);
        Assert.Equal(3, restored.State.CycleTier);
        Assert.Equal(2, restored.State.PerksUnlocked);
        Assert.Equal(5, restored.State.TotalFalls);
        Assert.Equal(42.0, restored.State.WallHp);
        Assert.Equal(4, restored.State.UpgradeLevels[UpgradeId.Helmets]);
        Assert.Equal(9, restored.State.Counts[(int)SkeletonTypeId.Bomber, 1]);
    }

    [Fact]
    public void TimestampSurvivesRoundTrip()
    {
        var sim = new Simulation(TestBalance.Make());
        var save = SaveCodec.Parse(SaveCodec.ToJson(sim.State, 1753180042));
        Assert.Equal(1753180042, save.TimestampUtcSeconds);
        Assert.Equal(SaveCodec.CurrentVersion, save.Version);
    }

    [Fact]
    public void FutureVersionIsRejected()
    {
        var sim = new Simulation(TestBalance.Make());
        var json = SaveCodec.ToJson(sim.State, 0).Replace("\"version\":1", "\"version\":99");
        Assert.Throws<System.ArgumentException>(() => SaveCodec.Parse(json));
    }

    [Fact]
    public void RestoredSimKeepsTicking()
    {
        var b = TestBalance.Make();
        var sim = new Simulation(b);
        for (int i = 0; i < 500; i++) sim.Tick();
        var restored = new Simulation(b, SaveCodec.Parse(SaveCodec.ToJson(sim.State, 0)));
        for (int i = 0; i < 500; i++) restored.Tick();
        Assert.True(restored.State.Bones > sim.State.Bones); // life goes on
    }

    [Fact]
    public void IncompleteSaveIsRejected()
    {
        var sim = new Simulation(TestBalance.Make());
        var json = SaveCodec.ToJson(sim.State, 0).Replace("\"counts\"", "\"oldCounts\"");
        Assert.Throws<System.ArgumentException>(() => SaveCodec.Parse(json));
    }
}
