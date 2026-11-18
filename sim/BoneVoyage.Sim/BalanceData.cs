using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoneVoyage.Sim;

public sealed record SkeletonBalance(double Speed, double Hp, double WallDamage, double Bounty);
public sealed record TowerBalance(string Name, int Zone, double Dps);
public sealed record MoatBalance(int Zone, double SpeedMult);
public sealed record PerkBalance(PerkId Id, double Mult);
public sealed record KingdomDef(string Name, double BaseWallHp, List<TowerBalance> Towers,
                                List<MoatBalance> Moats, PerkBalance Perk);
public sealed record CycleBalance(double WallHpGrowth, double DpsGrowth, double BaseJackpot,
                                  double JackpotGrowth, double GoldJackpotFactor);
public sealed record BannerBalance(double DrPerBanner, double DrCap);
public sealed record UpgradeBalance(double BaseValue, double PerLevel, double BaseCost,
                                    double CostGrowth, Currency Currency);

public sealed record BalanceData(
    double TickSeconds,
    int ZoneCount,
    double ZoneLength,
    double GoldPerWallDamage,
    BannerBalance Banner,
    Dictionary<SkeletonTypeId, SkeletonBalance> Skeletons,
    List<KingdomDef> Kingdoms,
    CycleBalance Cycle,
    Dictionary<UpgradeId, UpgradeBalance> Upgrades,
    double StartingBones)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static BalanceData Load(string json)
    {
        var data = JsonSerializer.Deserialize<BalanceData>(json, Options)
            ?? throw new JsonException("balance.json deserialized to null");
        data.Validate();
        return data;
    }

    public void Validate()
    {
        if (TickSeconds <= 0) throw new ArgumentException("TickSeconds must be > 0");
        if (ZoneCount <= 0) throw new ArgumentException("ZoneCount must be > 0");
        if (Kingdoms.Count == 0) throw new ArgumentException("Kingdoms must not be empty");
        if (Cycle.WallHpGrowth <= 1.0) throw new ArgumentException("Cycle.WallHpGrowth must be > 1");
        if (Banner.DrCap is < 0 or >= 1.0) throw new ArgumentException("Banner.DrCap must be in [0,1)");
        foreach (var (type, sk) in Skeletons)
        {
            if (sk.Speed <= 0) throw new ArgumentException($"{type} Speed must be > 0");
            if (sk.Hp <= 0) throw new ArgumentException($"{type} Hp must be > 0");
        }
        foreach (var k in Kingdoms)
        {
            if (k.BaseWallHp <= 0) throw new ArgumentException($"{k.Name} BaseWallHp must be > 0");
            foreach (var t in k.Towers)
                if (t.Zone < 0 || t.Zone >= ZoneCount)
                    throw new ArgumentException($"{k.Name}/{t.Name} Zone {t.Zone} out of range");
            foreach (var m in k.Moats)
                if (m.Zone < 0 || m.Zone >= ZoneCount)
                    throw new ArgumentException($"{k.Name} moat Zone {m.Zone} out of range");
        }
    }
}
