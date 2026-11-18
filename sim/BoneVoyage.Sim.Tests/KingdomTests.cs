using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class KingdomTests
{
    private static Simulation NoTowerSim()
    {
        var b = TestBalance.Make();
        return new Simulation(b with { Kingdoms = new()
        {
            b.Kingdoms[0] with { Towers = new() },
            b.Kingdoms[1] with { Towers = new() },
        }});
    }

    private static KingdomFell? FellOneWallHit(Simulation sim)
    {
        int t = (int)SkeletonTypeId.Bomber; // 100 dmg one hit
        int last = sim.Balance.ZoneCount - 1;
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999;
        for (int i = 0; i < 100; i++)
        {
            var fell = sim.Tick().OfType<KingdomFell>().FirstOrDefault();
            if (fell != null) return fell;
        }
        return null;
    }

    [Fact]
    public void ConquestAdvancesToNextKingdomAndUnlocksPerk()
    {
        var sim = NoTowerSim(); // Testshire wall 100; bomber hits 100
        var fell = FellOneWallHit(sim);
        Assert.NotNull(fell);
        Assert.Equal(0, fell!.KingdomIndex);
        Assert.Equal(1, fell.CycleTier);
        Assert.Equal(50.0, fell.BonesJackpot);
        Assert.Equal(25.0, fell.GoldJackpot);
        Assert.Equal(1, sim.State.KingdomIndex);      // now besieging Moatholm
        Assert.Equal(1, sim.State.PerksUnlocked);      // Testshire perk (BountyMult 2.0)
        Assert.Equal(1, sim.State.CycleTier);          // cycle completes only after last kingdom
        Assert.Equal(2.0, sim.GetPerkMult(PerkId.BountyMult));
        Assert.Equal(1.0, sim.GetPerkMult(PerkId.GoldMult)); // not yet unlocked
    }

    [Fact]
    public void FullCycleIncrementsCycleTierAndScalesWalls()
    {
        var sim = NoTowerSim();
        Assert.NotNull(FellOneWallHit(sim));           // k0 falls
        var fell2 = FellOneWallHit(sim);               // k1 falls (wall 200; bomber overkill cascades? no: 100 < 200 — need 2 hits)
        if (fell2 == null) fell2 = FellOneWallHit(sim); // second bomber
        Assert.NotNull(fell2);
        Assert.Equal(1, fell2!.KingdomIndex);
        Assert.Equal(0, sim.State.KingdomIndex);       // wrapped
        Assert.Equal(2, sim.State.CycleTier);          // 2 falls / 2 kingdoms + 1
        Assert.Equal(2, sim.State.PerksUnlocked);
        Assert.Equal(3.0, sim.GetPerkMult(PerkId.GoldMult));
        // Cycle-2 Testshire wall: 100 * 2.0^(2-1) = 200 (minus any overkill carry)
        Assert.True(sim.State.WallHp > 100.0);
    }

    [Fact]
    public void BountyPerkMultipliesDeathIncome()
    {
        var sim = NoTowerSim();
        Assert.NotNull(FellOneWallHit(sim)); // unlock BountyMult 2.0
        double before = sim.State.Bones;
        int t = (int)SkeletonTypeId.Marcher, last = sim.Balance.ZoneCount - 1;
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999;
        for (int i = 0; i < 60; i++) sim.Tick();
        // bounty 1 * recovery 1.0 * perk 2.0 = 2 (allow ambient marcher-spawn deaths: assert >= before+2)
        Assert.True(sim.State.Bones >= before + 2.0);
    }

    [Fact]
    public void JackpotGrowsPerTotalFalls()
    {
        var sim = NoTowerSim();
        var f1 = FellOneWallHit(sim)!;
        var f2 = FellOneWallHit(sim) ?? FellOneWallHit(sim)!;
        Assert.Equal(50.0, f1.BonesJackpot);            // growth^0
        Assert.Equal(100.0, f2.BonesJackpot);           // growth^1
    }
}
