using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class WallTests
{
    private static Simulation NoTowerSim()
    {
        var b = TestBalance.Make();
        var sim = new Simulation(b with { Kingdoms = new() { b.Kingdoms[0] with { Towers = new() } } });
        return sim;
    }

    [Fact]
    public void WallHitDealsDamagePaysBountyAndKills()
    {
        var sim = NoTowerSim();
        int t = (int)SkeletonTypeId.Marcher;
        int last = sim.Balance.ZoneCount - 1;
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999; // arrives at wall within a tick or two
        double wallBefore = sim.State.WallHp;
        WallHit? hit = null;
        for (int i = 0; i < 40 && hit == null; i++)
            hit = sim.Tick().OfType<WallHit>().FirstOrDefault();
        Assert.NotNull(hit);
        Assert.Equal(5.0, hit!.Damage);                    // marcher wallDamage 5
        Assert.Equal(wallBefore - 5.0, sim.State.WallHp);
        Assert.True(sim.State.Bones >= 1.0);               // bounty 1 * recovery 1.0
    }

    [Fact]
    public void KingdomFallsPaysJackpotAndScalesTier()
    {
        var sim = NoTowerSim();
        int t = (int)SkeletonTypeId.Marcher;
        int last = sim.Balance.ZoneCount - 1;
        sim.State.WallHp = 4.0; // one marcher hit (5 dmg) fells it
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999;
        KingdomFell? fell = null;
        for (int i = 0; i < 40 && fell == null; i++)
            fell = sim.Tick().OfType<KingdomFell>().FirstOrDefault();
        Assert.NotNull(fell);
        Assert.Equal(1, fell!.CycleTier);
        Assert.Equal(50.0, fell.BonesJackpot);                  // baseJackpot tier 1
        Assert.Equal(25.0, fell.GoldJackpot);                   // bonesJackpot * goldJackpotFactor
        Assert.Equal(2, sim.State.CycleTier);
        Assert.True(sim.State.WallHp > 100.0);                  // 100 * 2.0 growth, minus overkill leftover
    }
}
