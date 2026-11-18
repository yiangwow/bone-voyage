using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class PurchaseTests
{
    [Fact]
    public void PurchaseFailsWhenBroke()
    {
        var sim = new Simulation(TestBalance.Make());
        Assert.False(sim.TryPurchase(UpgradeId.MarcherRate));
        Assert.Equal(0, sim.State.UpgradeLevels.GetValueOrDefault(UpgradeId.MarcherRate));
    }

    [Fact]
    public void PurchaseSpendsBonesAndRaisesLevelAndCost()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Bones = 100.0;
        Assert.Equal(10.0, sim.GetUpgradeCost(UpgradeId.MarcherRate));
        Assert.True(sim.TryPurchase(UpgradeId.MarcherRate));
        Assert.Equal(90.0, sim.State.Bones);
        Assert.Equal(1.25, sim.GetUpgradeValue(UpgradeId.MarcherRate)); // 1.0 + 0.25
        Assert.Equal(13.5, sim.GetUpgradeCost(UpgradeId.MarcherRate), 3); // 10 * 1.35
    }

    [Fact]
    public void RecoveryMultipliesBounties()
    {
        var b0 = TestBalance.Make();
        var b = b0 with { Kingdoms = new() { b0.Kingdoms[0] with { Towers = new() } } };
        var sim = new Simulation(b);
        sim.State.Bones = 1000.0;
        sim.TryPurchase(UpgradeId.Recovery); // recovery 1.15
        double before = sim.State.Bones;
        int t = (int)SkeletonTypeId.Marcher;
        int last = sim.Balance.ZoneCount - 1;
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999;
        for (int i = 0; i < 40; i++) sim.Tick();
        Assert.Equal(before + 1.15, sim.State.Bones, 2); // bounty 1 * 1.15
    }
}
