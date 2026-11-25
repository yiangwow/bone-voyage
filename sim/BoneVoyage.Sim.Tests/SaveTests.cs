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

        var json = SaveCodec.ToJson(sim.State, timestampUtcSeconds: 1753180000, SaveSettings.Default);
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
        var save = SaveCodec.Parse(SaveCodec.ToJson(sim.State, 1753180042, SaveSettings.Default));
        Assert.Equal(1753180042, save.TimestampUtcSeconds);
        Assert.Equal(SaveCodec.CurrentVersion, save.Version);
    }

    [Fact]
    public void FutureVersionIsRejected()
    {
        var sim = new Simulation(TestBalance.Make());
        var json = SaveCodec.ToJson(sim.State, 0, SaveSettings.Default).Replace("\"version\":2", "\"version\":99");
        Assert.Throws<System.ArgumentException>(() => SaveCodec.Parse(json));
    }

    [Fact]
    public void RestoredSimKeepsTicking()
    {
        var b = TestBalance.Make();
        var sim = new Simulation(b);
        for (int i = 0; i < 500; i++) sim.Tick();
        var restored = new Simulation(b, SaveCodec.Parse(SaveCodec.ToJson(sim.State, 0, SaveSettings.Default)));
        for (int i = 0; i < 500; i++) restored.Tick();
        Assert.True(restored.State.Bones > sim.State.Bones); // life goes on
    }

    [Fact]
    public void IncompleteSaveIsRejected()
    {
        var sim = new Simulation(TestBalance.Make());
        var json = SaveCodec.ToJson(sim.State, 0, SaveSettings.Default).Replace("\"counts\"", "\"oldCounts\"");
        Assert.Throws<System.ArgumentException>(() => SaveCodec.Parse(json));
    }

    [Fact]
    public void SettingsRoundtripThroughSaveJson()
    {
        var state = new SimState(5, 2);
        string json = SaveCodec.ToJson(state, 1234, new SaveSettings(0.25, 0.9));
        var data = SaveCodec.Parse(json);
        Assert.Equal(2, data.Version);
        Assert.NotNull(data.Settings);
        Assert.Equal(0.25, data.Settings!.MusicVolume, 10);
        Assert.Equal(0.9, data.Settings.SfxVolume, 10);
    }

    [Fact]
    public void V1SaveWithoutSettingsParsesWithNullSettings()
    {
        string v1 = "{\"version\":1,\"timestampUtcSeconds\":5,\"bones\":1,\"gold\":2," +
            "\"kingdomIndex\":0,\"cycleTier\":1,\"perksUnlocked\":0,\"totalFalls\":0," +
            "\"wallHp\":100,\"upgradeLevels\":{},\"counts\":[[0,0]]}";
        var data = SaveCodec.Parse(v1);
        Assert.Null(data.Settings);
        Assert.Equal(1.0, data.Bones, 10);
    }
}
