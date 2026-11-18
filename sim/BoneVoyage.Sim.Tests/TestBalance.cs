using System.Collections.Generic;
using BoneVoyage.Sim;

namespace BoneVoyage.Sim.Tests;

public static class TestBalance
{
    // 2 zones of 10 units; marcher speed 5 => 2s per zone = 20 ticks. ArrowTower dps 10 in zone 1.
    public static BalanceData Make() => new(
        TickSeconds: 0.1, ZoneCount: 2, ZoneLength: 10.0,
        GoldPerWallDamage: 1.0,
        Banner: new(DrPerBanner: 0.10, DrCap: 0.4),
        Skeletons: new Dictionary<SkeletonTypeId, SkeletonBalance>
        {
            [SkeletonTypeId.Marcher] = new(Speed: 5.0, Hp: 10.0, WallDamage: 5.0, Bounty: 1.0),
            [SkeletonTypeId.Shieldbearer] = new(Speed: 5.0, Hp: 50.0, WallDamage: 3.0, Bounty: 4.0),
            [SkeletonTypeId.Sprinter] = new(Speed: 10.0, Hp: 5.0, WallDamage: 4.0, Bounty: 2.0),
            [SkeletonTypeId.Bomber] = new(Speed: 2.5, Hp: 20.0, WallDamage: 100.0, Bounty: 3.0),
            [SkeletonTypeId.Bannerbones] = new(Speed: 5.0, Hp: 25.0, WallDamage: 1.0, Bounty: 6.0),
        },
        Kingdoms: new List<KingdomDef>
        {
            new("Testshire", BaseWallHp: 100.0,
                Towers: new List<TowerBalance> { new("ArrowTower", Zone: 1, Dps: 10.0) },
                Moats: new List<MoatBalance>(),
                Perk: new(PerkId.BountyMult, 2.0)),
            new("Moatholm", BaseWallHp: 200.0,
                Towers: new List<TowerBalance>(),
                Moats: new List<MoatBalance> { new(Zone: 0, SpeedMult: 0.5) },
                Perk: new(PerkId.GoldMult, 3.0)),
        },
        Cycle: new(WallHpGrowth: 2.0, DpsGrowth: 1.5, BaseJackpot: 50.0,
                   JackpotGrowth: 2.0, GoldJackpotFactor: 0.5),
        Upgrades: new Dictionary<UpgradeId, UpgradeBalance>
        {
            [UpgradeId.MarcherRate] = new(1.0, 0.25, 10.0, 1.35, Currency.Bones),
            [UpgradeId.ShieldbearerRate] = new(0.0, 0.1, 150.0, 1.4, Currency.Bones),
            [UpgradeId.Recovery] = new(1.0, 0.15, 25.0, 1.5, Currency.Bones),
            [UpgradeId.SprinterRate] = new(0.0, 0.2, 40.0, 1.4, Currency.Bones),
            [UpgradeId.BomberRate] = new(0.0, 0.1, 60.0, 1.45, Currency.Bones),
            [UpgradeId.BannerRate] = new(0.0, 0.05, 80.0, 1.5, Currency.Bones),
            [UpgradeId.Helmets] = new(1.0, 0.05, 50.0, 1.5, Currency.Gold),
            [UpgradeId.BatteringRams] = new(1.0, 0.05, 75.0, 1.5, Currency.Gold),
            [UpgradeId.MarchingBoots] = new(1.0, 0.5, 60.0, 1.55, Currency.Gold),
            [UpgradeId.WarBanners] = new(1.0, 0.10, 200.0, 1.6, Currency.Gold),
        },
        StartingBones: 0.0);
}
