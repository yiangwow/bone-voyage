using System;
using System.Collections.Generic;
using System.Linq;

namespace BoneVoyage.Sim;

public sealed class Simulation
{
    public BalanceData Balance { get; }
    public SimState State { get; }

    internal static readonly SkeletonTypeId[] Types =
        { SkeletonTypeId.Marcher, SkeletonTypeId.Shieldbearer, SkeletonTypeId.Sprinter,
          SkeletonTypeId.Bomber, SkeletonTypeId.Bannerbones };

    public KingdomDef CurrentKingdom => Balance.Kingdoms[State.KingdomIndex];

    public Simulation(BalanceData balance) : this(balance, null) { }

    public Simulation(BalanceData balance, SaveData? save)
    {
        Balance = balance;
        State = new SimState(Types.Length, balance.ZoneCount);
        if (save == null)
        {
            State.Bones = balance.StartingBones;
            State.WallHp = WallHpFor(0, 1);
            return;
        }
        State.Bones = save.Bones; State.Gold = save.Gold;
        State.KingdomIndex = Math.Clamp(save.KingdomIndex, 0, balance.Kingdoms.Count - 1);
        State.CycleTier = Math.Max(1, save.CycleTier);
        State.PerksUnlocked = Math.Clamp(save.PerksUnlocked, 0, balance.Kingdoms.Count);
        State.TotalFalls = Math.Max(0, save.TotalFalls);
        State.WallHp = save.WallHp;
        State.UpgradeLevels = new(save.UpgradeLevels);
        for (int t = 0; t < Types.Length && t < save.Counts.Length; t++)
            for (int z = 0; z < balance.ZoneCount && z < save.Counts[t].Length; z++)
                State.Counts[t, z] = save.Counts[t][z];
    }

    public double GetUpgradeValue(UpgradeId id)
    {
        var u = Balance.Upgrades[id];
        return u.BaseValue + u.PerLevel * State.UpgradeLevels.GetValueOrDefault(id);
    }

    public double GetUpgradeCost(UpgradeId id)
    {
        var u = Balance.Upgrades[id];
        return u.BaseCost * Math.Pow(u.CostGrowth, State.UpgradeLevels.GetValueOrDefault(id));
    }

    public double MoatMult(int zone)
    {
        foreach (var m in CurrentKingdom.Moats)
            if (m.Zone == zone) return m.SpeedMult;
        return 1.0;
    }

    public double BannerDr(int zone) =>
        Math.Min(Balance.Banner.DrCap,
            State.Counts[(int)SkeletonTypeId.Bannerbones, zone]
                * Balance.Banner.DrPerBanner
                * GetUpgradeValue(UpgradeId.WarBanners));

    public double GetPerkMult(PerkId id)
    {
        double mult = 1.0;
        for (int i = 0; i < State.PerksUnlocked && i < Balance.Kingdoms.Count; i++)
            if (Balance.Kingdoms[i].Perk.Id == id)
                mult *= Balance.Kingdoms[i].Perk.Mult;
        return mult;
    }

    public double EffHp(SkeletonTypeId type) =>
        Balance.Skeletons[type].Hp * GetUpgradeValue(UpgradeId.Helmets);

    public double EffWallDamage(SkeletonTypeId type) =>
        Balance.Skeletons[type].WallDamage * GetUpgradeValue(UpgradeId.BatteringRams);

    public double EffSpeed(SkeletonTypeId type) =>
        Balance.Skeletons[type].Speed * GetUpgradeValue(UpgradeId.MarchingBoots)
            * GetPerkMult(PerkId.SpeedMult);

    public bool TryPurchase(UpgradeId id)
    {
        var cost = GetUpgradeCost(id);
        bool gold = Balance.Upgrades[id].Currency == Currency.Gold;
        double wallet = gold ? State.Gold : State.Bones;
        if (wallet < cost) return false;
        if (gold) State.Gold -= cost; else State.Bones -= cost;
        State.UpgradeLevels[id] = State.UpgradeLevels.GetValueOrDefault(id) + 1;
        return true;
    }

    public double WallHpFor(int kingdomIndex, int cycleTier) =>
        Balance.Kingdoms[kingdomIndex].BaseWallHp * Math.Pow(Balance.Cycle.WallHpGrowth, cycleTier - 1);

    public List<ISimEvent> Tick()
    {
        var events = new List<ISimEvent>();
        double dt = Balance.TickSeconds;
        Spawn(events, dt);
        Move(events, dt);
        TowerFire(events, dt);
        return events;
    }

    private static readonly (SkeletonTypeId Type, UpgradeId Rate)[] SpawnRates =
    {
        (SkeletonTypeId.Marcher, UpgradeId.MarcherRate),
        (SkeletonTypeId.Shieldbearer, UpgradeId.ShieldbearerRate),
        (SkeletonTypeId.Sprinter, UpgradeId.SprinterRate),
        (SkeletonTypeId.Bomber, UpgradeId.BomberRate),
        (SkeletonTypeId.Bannerbones, UpgradeId.BannerRate),
    };

    private void Spawn(List<ISimEvent> events, double dt)
    {
        foreach (var (type, rate) in SpawnRates) SpawnType(type, rate, events, dt);
    }

    private void SpawnType(SkeletonTypeId type, UpgradeId rateUpgrade,
        List<ISimEvent> events, double dt)
    {
        int t = (int)type;
        State.SpawnAcc[t] += GetUpgradeValue(rateUpgrade) * dt;
        // Epsilon-tolerant floor: IEEE-754 accumulation of 0.1 undershoots (10×0.1 < 1.0)
        int n = (int)Math.Floor(State.SpawnAcc[t] + 1e-10);
        if (n <= 0) return;
        State.SpawnAcc[t] -= n;
        State.Counts[t, 0] += n;
        events.Add(new SkeletonsSpawned(type, n));
    }

    private void Move(List<ISimEvent> events, double dt)
    {
        foreach (var type in Types)
        {
            int t = (int)type;
            for (int z = Balance.ZoneCount - 1; z >= 0; z--)
            {
                double transit = Balance.ZoneLength / (EffSpeed(type) * MoatMult(z));
                State.MoveAcc[t, z] += State.Counts[t, z] * dt / transit;
                // Epsilon-tolerant floor: IEEE-754 accumulation of 0.1 undershoots (10×0.1 < 1.0)
                int k = Math.Min((int)Math.Floor(State.MoveAcc[t, z] + 1e-10), State.Counts[t, z]);
                if (k <= 0) continue;
                State.MoveAcc[t, z] -= k;
                State.Counts[t, z] -= k;
                if (z == Balance.ZoneCount - 1) HitWall(type, k, events);
                else State.Counts[t, z + 1] += k;
            }
        }
    }

    private void HitWall(SkeletonTypeId type, int count, List<ISimEvent> events)
    {
        double damage = count * EffWallDamage(type);
        State.WallHp -= damage;
        State.Gold += damage * Balance.GoldPerWallDamage * GetPerkMult(PerkId.GoldMult);
        events.Add(new WallHit(type, count, damage));
        AwardBounty(type, count);
        events.Add(new SkeletonsDied(type, count, Balance.ZoneCount - 1, DeathCause.WallCrumble));
        while (State.WallHp <= 0)
        {
            int fallenIndex = State.KingdomIndex;
            int fallenTier = State.CycleTier;
            double bonesJackpot = Balance.Cycle.BaseJackpot * Math.Pow(Balance.Cycle.JackpotGrowth, State.TotalFalls);
            double goldJackpot = bonesJackpot * Balance.Cycle.GoldJackpotFactor;
            State.Bones += bonesJackpot;
            State.Gold += goldJackpot;
            events.Add(new KingdomFell(fallenIndex, fallenTier, bonesJackpot, goldJackpot));
            State.TotalFalls++;
            State.PerksUnlocked = Math.Max(State.PerksUnlocked, State.KingdomIndex + 1);
            State.KingdomIndex = (State.KingdomIndex + 1) % Balance.Kingdoms.Count;
            State.CycleTier = State.TotalFalls / Balance.Kingdoms.Count + 1;
            State.WallHp += WallHpFor(State.KingdomIndex, State.CycleTier);
        }
    }

    private void AwardBounty(SkeletonTypeId type, int count) =>
        State.Bones += count * Balance.Skeletons[type].Bounty * GetUpgradeValue(UpgradeId.Recovery)
            * GetPerkMult(PerkId.BountyMult);

    private static readonly SkeletonTypeId[] KillPriority =
    {
        SkeletonTypeId.Shieldbearer, SkeletonTypeId.Marcher, SkeletonTypeId.Sprinter,
        SkeletonTypeId.Bomber, SkeletonTypeId.Bannerbones,
    };

    private void TowerFire(List<ISimEvent> events, double dt)
    {
        double dpsMult = Math.Pow(Balance.Cycle.DpsGrowth, State.CycleTier - 1);
        foreach (var group in CurrentKingdom.Towers.GroupBy(t => t.Zone))
        {
            int z = group.Key;
            int total = 0;
            foreach (var type in Types) total += State.Counts[(int)type, z];
            if (total == 0) { State.DamagePool[z] = 0; continue; }

            double dps = group.Sum(t => t.Dps);
            State.DamagePool[z] += dps * dpsMult * (1.0 - BannerDr(z)) * dt;
            foreach (var type in KillPriority)
            {
                KillInZone(type, z, events);
                if (State.Counts[(int)type, z] > 0) break; // survivors soak the residue
            }
        }
    }

    private void KillInZone(SkeletonTypeId type, int z, List<ISimEvent> events)
    {
        int t = (int)type;
        if (State.Counts[t, z] == 0) return;
        double hp = EffHp(type);
        // Epsilon-tolerant floor: IEEE-754 accumulation of 0.1 undershoots (10×0.1 < 1.0)
        int kills = Math.Min(State.Counts[t, z], (int)Math.Floor(State.DamagePool[z] / hp + 1e-10));
        if (kills <= 0) return;
        State.DamagePool[z] -= kills * hp;
        State.Counts[t, z] -= kills;
        AwardBounty(type, kills);
        events.Add(new SkeletonsDied(type, kills, z, DeathCause.Tower));
    }
}
