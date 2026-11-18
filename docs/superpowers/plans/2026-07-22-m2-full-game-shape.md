# Bone Voyage M2 (Full Game Shape) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** All 5 skeleton types, the gold/gear armory, 3 distinct kingdoms with a perk-granting conquest strip, and versioned saves — the complete game shape on top of M1.

**Architecture:** Same two layers as M1: pure deterministic C# sim (`sim/BoneVoyage.Sim`) extended with gold, perks, moats, banner auras, gear multipliers, multi-kingdom progression, and a save codec; Godot view (`game/`) gains new actor types, per-kingdom battlefield rebuild, a conquest strip, an armory panel, and a SaveManager. Balance data restructures to a v2 schema (list of kingdoms) with load-time validation.

**Tech Stack:** Godot 4.4.1-stable (.NET), C# net8.0, xUnit, Python 3 + Pillow.

## Global Constraints

- Godot 4.4.1-stable (Godot.NET.Sdk/4.4.1); sim + tests net8.0. Build must stay 0 errors / 0 warnings.
- Git author the owner's git identity; commit messages contain NO Co-Authored-By trailer and NO "Generated with" footer.
- All gameplay math in `BoneVoyage.Sim`; game/ scripts only call sim methods, read state, consume events. Cosmetic RNG only in game/.
- Sim deterministic: no RNG, no clock reads. Timestamps for saves are passed in by the caller.
- All tuning numbers only in `game/balance.json`. UI effect labels must be DERIVED from `Simulation.Balance` (M1 deferred item — no hard-coded "+0.25/s" strings).
- UI strings via `Tr()` with English keys; cheerful-union-worker tone ("Necropolis HR", "Death Benefits", "Royal Armory").
- FP accumulators use the established epsilon pattern: `(int)Math.Floor(x + 1e-10)` with the one-line IEEE-754 comment.
- Deferred M1 items folded here: balance validation (Task 1), tightened movement tests + multi-tower-zone test (Task 2/5), Hud type-count derived from enum (Task 9), derived shop labels (Task 9), `Main._acc` clamp stays deferred to M3 (needs OfflineCalculator) — do NOT clamp it in M2.

## File Structure

```
sim/BoneVoyage.Sim/
  SkeletonTypeId.cs      # MODIFY: +Sprinter/Bomber/Bannerbones, +Currency, +PerkId, UpgradeId extended
  BalanceData.cs         # MODIFY: v2 schema (KingdomDef list, CycleBalance, BannerBalance, currency on upgrades) + Validate()
  SimEvents.cs           # MODIFY: KingdomFell v2 signature
  SimState.cs            # MODIFY: Gold, KingdomIndex, CycleTier, PerksUnlocked, TotalFalls
  Simulation.cs          # MODIFY: 5-type pipeline, moats, banner DR, gold, gear multipliers, perks, kingdom advance, save-restore ctor
  SaveCodec.cs           # CREATE: SaveData record + ToJson/Parse
sim/BoneVoyage.Sim.Tests/
  TestBalance.cs         # MODIFY: v2 fixture
  BalanceDataTests.cs    # MODIFY: v2 + validation tests
  MovementTests.cs       # MODIFY: tightened (deferred item)
  TowerTests.cs          # MODIFY: adapt to v2 fixture; + multi-tower-same-zone test
  WallTests.cs           # MODIFY: adapt; kingdom advance covered in KingdomTests
  NewTypesTests.cs       # CREATE
  AuraAndMoatTests.cs    # CREATE
  EconomyTests.cs        # CREATE (gold, gear)
  KingdomTests.cs        # CREATE (progression, perks, jackpots)
  SaveTests.cs           # CREATE
tools/spritegen/
  skeletons.py           # MODIFY: sprinter/bomber/bannerbones variants, bomber boom row
  props.py               # MODIFY: knight post, moat tile, flag icon
game/
  balance.json           # MODIFY: v2
  scripts/Hud.cs         # MODIFY: gold label, kingdom name, enum-derived counts, ConquestStrip + ArmoryPanel wiring
  scripts/ShopPanel.cs   # MODIFY: 6 bones rows, derived labels
  scripts/ArmoryPanel.cs # CREATE: 4 gold rows, derived labels
  scripts/ConquestStrip.cs # CREATE
  scripts/BattlefieldView.cs # MODIFY: kingdom rebuild, moats, 5 actor types
  scripts/SaveManager.cs # CREATE
  scripts/Main.cs        # MODIFY: SaveManager wiring, load-on-start
```

---

### Task 1: Balance v2 — schema, validation, fixture, migration

**Files:**
- Modify: `sim/BoneVoyage.Sim/SkeletonTypeId.cs`, `sim/BoneVoyage.Sim/BalanceData.cs`, `game/balance.json`, `sim/BoneVoyage.Sim.Tests/TestBalance.cs`, `sim/BoneVoyage.Sim.Tests/BalanceDataTests.cs`, `sim/BoneVoyage.Sim/SimState.cs`, `sim/BoneVoyage.Sim/Simulation.cs`, `sim/BoneVoyage.Sim/SimEvents.cs`, plus mechanical fixes in `MovementTests.cs`/`TowerTests.cs`/`WallTests.cs`/`PurchaseTests.cs`/`SpawnTests.cs` and `game/scripts/Hud.cs` so the SOLUTION COMPILES AND ALL EXISTING TESTS PASS at the end of this task.

**Interfaces (produced — every later task depends on these exact signatures):**

```csharp
public enum SkeletonTypeId { Marcher = 0, Shieldbearer = 1, Sprinter = 2, Bomber = 3, Bannerbones = 4 }
public enum DeathCause { Tower, WallCrumble }
public enum UpgradeId { MarcherRate, ShieldbearerRate, Recovery, SprinterRate, BomberRate, BannerRate,
                        Helmets, BatteringRams, MarchingBoots, WarBanners }
public enum Currency { Bones, Gold }
public enum PerkId { BountyMult, SpeedMult, GoldMult }

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
public sealed record BalanceData(double TickSeconds, int ZoneCount, double ZoneLength,
    double GoldPerWallDamage, BannerBalance Banner,
    Dictionary<SkeletonTypeId, SkeletonBalance> Skeletons, List<KingdomDef> Kingdoms,
    CycleBalance Cycle, Dictionary<UpgradeId, UpgradeBalance> Upgrades, double StartingBones)
{ public static BalanceData Load(string json); public void Validate(); }
```

`Load` calls `Validate()` before returning. `Validate()` throws `ArgumentException` naming the offending field when: `TickSeconds <= 0`; `ZoneCount <= 0`; any tower/moat `Zone` outside `[0, ZoneCount-1]`; any kingdom `BaseWallHp <= 0`; `Cycle.WallHpGrowth <= 1`; `Banner.DrCap` outside `[0,1)`; a `SkeletonBalance` with `Speed <= 0` or `Hp <= 0`; `Kingdoms` empty. (M1 deferred item.)

`SimState` fields become: `double Bones; double Gold; int KingdomIndex; int CycleTier = 1; int PerksUnlocked; int TotalFalls; double WallHp; int[,] Counts; double[] SpawnAcc; double[,] MoveAcc; double[] DamagePool; Dictionary<UpgradeId,int> UpgradeLevels;` — array sizes come from `typeCount`/`zoneCount` ctor args as before. `KingdomTier` is REMOVED (replaced by `CycleTier`).

`SimEvents.cs`: `KingdomFell` becomes `public sealed record KingdomFell(int KingdomIndex, int CycleTier, double BonesJackpot, double GoldJackpot) : ISimEvent;` — other events unchanged.

`Simulation` (this task only migrates; new mechanics come in Tasks 2–5): `Types` array now contains all five members in enum order. `CurrentKingdom => Balance.Kingdoms[State.KingdomIndex]`. `WallHpFor(int kingdomIndex, int cycleTier) => Balance.Kingdoms[kingdomIndex].BaseWallHp * Math.Pow(Balance.Cycle.WallHpGrowth, cycleTier - 1)` (replaces `WallHpForTier`). Towers/dps/jackpot lookups move to `CurrentKingdom`/`Balance.Cycle`. Kingdom-fall block (minimal migration this task): pay `BonesJackpot = Cycle.BaseJackpot * Math.Pow(Cycle.JackpotGrowth, State.TotalFalls)` and `GoldJackpot = BonesJackpot * Cycle.GoldJackpotFactor` (into `State.Gold`), then `State.TotalFalls++`, `State.PerksUnlocked = Math.Max(State.PerksUnlocked, State.KingdomIndex + 1)`, `State.KingdomIndex = (State.KingdomIndex + 1) % Balance.Kingdoms.Count`, `State.CycleTier = State.TotalFalls / Balance.Kingdoms.Count + 1`, `State.WallHp += WallHpFor(State.KingdomIndex, State.CycleTier)`, emit new `KingdomFell`.

- [ ] **Step 1: Write the new `game/balance.json` (v2)**

```json
{
  "tickSeconds": 0.1,
  "zoneCount": 10,
  "zoneLength": 10.0,
  "goldPerWallDamage": 1.0,
  "banner": { "drPerBanner": 0.05, "drCap": 0.4 },
  "skeletons": {
    "Marcher":      { "speed": 5.0, "hp": 10.0, "wallDamage": 5.0,  "bounty": 1.0 },
    "Shieldbearer": { "speed": 4.0, "hp": 50.0, "wallDamage": 3.0,  "bounty": 4.0 },
    "Sprinter":     { "speed": 9.0, "hp": 6.0,  "wallDamage": 4.0,  "bounty": 2.0 },
    "Bomber":       { "speed": 3.5, "hp": 14.0, "wallDamage": 60.0, "bounty": 3.0 },
    "Bannerbones":  { "speed": 5.0, "hp": 25.0, "wallDamage": 1.0,  "bounty": 6.0 }
  },
  "kingdoms": [
    { "name": "Meadowshire", "baseWallHp": 500.0,
      "towers": [ { "name": "ArrowTower", "zone": 4, "dps": 8.0 },
                  { "name": "Cannon",     "zone": 7, "dps": 20.0 } ],
      "moats": [],
      "perk": { "id": "BountyMult", "mult": 1.15 } },
    { "name": "Cannonspire", "baseWallHp": 1600.0,
      "towers": [ { "name": "Cannon", "zone": 3, "dps": 18.0 },
                  { "name": "Cannon", "zone": 6, "dps": 26.0 },
                  { "name": "ArrowTower", "zone": 8, "dps": 12.0 } ],
      "moats": [ { "zone": 5, "speedMult": 0.5 } ],
      "perk": { "id": "SpeedMult", "mult": 1.10 } },
    { "name": "Knightholm", "baseWallHp": 4500.0,
      "towers": [ { "name": "KnightPost", "zone": 1, "dps": 30.0 },
                  { "name": "KnightPost", "zone": 2, "dps": 30.0 },
                  { "name": "Cannon",     "zone": 6, "dps": 35.0 },
                  { "name": "ArrowTower", "zone": 8, "dps": 15.0 } ],
      "moats": [ { "zone": 0, "speedMult": 0.7 }, { "zone": 1, "speedMult": 0.7 } ],
      "perk": { "id": "GoldMult", "mult": 1.25 } }
  ],
  "cycle": { "wallHpGrowth": 2.5, "dpsGrowth": 1.8, "baseJackpot": 100.0,
             "jackpotGrowth": 2.2, "goldJackpotFactor": 0.5 },
  "upgrades": {
    "MarcherRate":      { "baseValue": 1.0, "perLevel": 0.25, "baseCost": 10.0,    "costGrowth": 1.35, "currency": "Bones" },
    "ShieldbearerRate": { "baseValue": 0.0, "perLevel": 0.1,  "baseCost": 150.0,   "costGrowth": 1.4,  "currency": "Bones" },
    "Recovery":         { "baseValue": 1.0, "perLevel": 0.15, "baseCost": 25.0,    "costGrowth": 1.5,  "currency": "Bones" },
    "SprinterRate":     { "baseValue": 0.0, "perLevel": 0.2,  "baseCost": 400.0,   "costGrowth": 1.4,  "currency": "Bones" },
    "BomberRate":       { "baseValue": 0.0, "perLevel": 0.1,  "baseCost": 1500.0,  "costGrowth": 1.45, "currency": "Bones" },
    "BannerRate":       { "baseValue": 0.0, "perLevel": 0.05, "baseCost": 5000.0,  "costGrowth": 1.5,  "currency": "Bones" },
    "Helmets":          { "baseValue": 1.0, "perLevel": 0.05, "baseCost": 50.0,    "costGrowth": 1.5,  "currency": "Gold" },
    "BatteringRams":    { "baseValue": 1.0, "perLevel": 0.05, "baseCost": 75.0,    "costGrowth": 1.5,  "currency": "Gold" },
    "MarchingBoots":    { "baseValue": 1.0, "perLevel": 0.03, "baseCost": 60.0,    "costGrowth": 1.55, "currency": "Gold" },
    "WarBanners":       { "baseValue": 1.0, "perLevel": 0.10, "baseCost": 200.0,   "costGrowth": 1.6,  "currency": "Gold" }
  },
  "startingBones": 0.0
}
```

- [ ] **Step 2: Rewrite `TestBalance.Make()`** — same 2-zone geometry as M1 so surviving tests keep their mental math; all five types; two kingdoms so progression is testable:

```csharp
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
                Towers: new List<TowerBalance> { new("Cannon", Zone: 0, Dps: 10.0) },
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
```

(Note `MarchingBoots.PerLevel: 0.5` in the FIXTURE ONLY — a big step makes speed-multiplier tests exact.)

- [ ] **Step 3: Write failing balance tests** — replace `BalanceDataTests.cs`:

```csharp
using System;
using System.IO;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class BalanceDataTests
{
    [Fact]
    public void LoadsShippedBalanceJson()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "balance.json"));
        var b = BalanceData.Load(json);
        Assert.Equal(0.1, b.TickSeconds);
        Assert.Equal(10, b.ZoneCount);
        Assert.Equal(5, b.Skeletons.Count);
        Assert.Equal(3, b.Kingdoms.Count);
        Assert.Equal(PerkId.GoldMult, b.Kingdoms[2].Perk.Id);
        Assert.Equal(Currency.Gold, b.Upgrades[UpgradeId.Helmets].Currency);
        Assert.NotEmpty(b.Kingdoms[1].Moats);
    }

    [Fact]
    public void ValidateRejectsOutOfRangeTowerZone()
    {
        var b = TestBalance.Make();
        var bad = b with { Kingdoms = new()
            { b.Kingdoms[0] with { Towers = new() { new("ArrowTower", Zone: 99, Dps: 1.0) } } } };
        var ex = Assert.Throws<ArgumentException>(() => bad.Validate());
        Assert.Contains("Zone", ex.Message);
    }

    [Fact]
    public void ValidateRejectsNonPositiveWallHp()
    {
        var b = TestBalance.Make();
        var bad = b with { Kingdoms = new() { b.Kingdoms[0] with { BaseWallHp = 0.0 } } };
        Assert.Throws<ArgumentException>(() => bad.Validate());
    }

    [Fact]
    public void ValidateRejectsWallGrowthAtOrBelowOne()
    {
        var b = TestBalance.Make();
        var bad = b with { Cycle = b.Cycle with { WallHpGrowth = 1.0 } };
        Assert.Throws<ArgumentException>(() => bad.Validate());
    }
}
```

- [ ] **Step 4: Run to verify failure** — `dotnet test BoneVoyage.sln` → compile errors (expected: v2 types missing).

- [ ] **Step 5: Implement the v2 model** — rewrite `SkeletonTypeId.cs` and `BalanceData.cs` per the Interfaces block above. `Validate()`:

```csharp
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
```

and `Load` ends with `data.Validate(); return data;`.

- [ ] **Step 6: Migrate `SimState`, `SimEvents`, `Simulation`, existing tests, and `Hud.cs`** per the Interfaces block. Mechanical rules for the existing test files: `KingdomTier` → `CycleTier`; `WallHpForTier(1)` → `WallHpFor(0, 1)`; WallTests' "no towers" helper becomes `b.Kingdoms[0] with { Towers = new() }` wrapped back into the list (keep ONLY kingdom 0 in that helper so falls loop within one kingdom: `b with { Kingdoms = new() { b.Kingdoms[0] with { Towers = new() } } }`); `KingdomFell` assertions: `fell.CycleTier == 1`, `fell.BonesJackpot == 50.0`, and add `Assert.Equal(25.0, fell.GoldJackpot)`; the tier-scaling assertion becomes `Assert.True(sim.State.WallHp > 100.0)` unchanged in spirit (single-kingdom helper: next wall = 100 × 2.0 = 200). `Hud.cs`: replace the hard-coded `t < 2` loop bound with `System.Enum.GetValues<SkeletonTypeId>().Length` (M1 deferred item) and `_sim.State.KingdomTier` → `_sim.State.CycleTier`, `WallHpForTier(...)` → `WallHpFor(_sim.State.KingdomIndex, _sim.State.CycleTier)`, kingdom label from `_sim.CurrentKingdom.Name` instead of the hard-coded "Meadowshire" key (wrap in `Tr()`).

- [ ] **Step 7: Run full suite** — `dotnet test BoneVoyage.sln`: ALL tests pass (15 pre-existing, migrated, + 4 new = 19). `dotnet build`: 0 errors 0 warnings.

- [ ] **Step 8: Commit** — `git add -A && git commit -m "feat: balance v2 schema with kingdoms, currencies, perks, validation"`

---

### Task 2: Five-type pipeline + tightened movement tests

**Files:**
- Modify: `sim/BoneVoyage.Sim/Simulation.cs`, `sim/BoneVoyage.Sim.Tests/MovementTests.cs`
- Create: `sim/BoneVoyage.Sim.Tests/NewTypesTests.cs`

**Interfaces:**
- Consumes: Task 1 model. Produces: `Spawn` covers all five types via `RateUpgradeFor(SkeletonTypeId)`; tower kill priority = enum order with Shieldbearer first: `[Shieldbearer, Marcher, Sprinter, Bomber, Bannerbones]` — pool advances to the next type only when the prior type's zone count is 0.

- [ ] **Step 1: Write failing tests** — `NewTypesTests.cs`:

```csharp
using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class NewTypesTests
{
    private static Simulation Sim() => new(TestBalance.Make());

    [Fact]
    public void SprinterSpawnsAfterRatePurchase()
    {
        var sim = Sim();
        sim.State.Bones = 1000;
        Assert.True(sim.TryPurchase(UpgradeId.SprinterRate)); // rate 0.2/s
        var spawned = 0;
        for (int i = 0; i < 50; i++) // 5s * 0.2/s = 1
            spawned += sim.Tick().OfType<SkeletonsSpawned>()
                .Where(e => e.Type == SkeletonTypeId.Sprinter).Sum(e => e.Count);
        Assert.Equal(1, spawned);
    }

    [Fact]
    public void SprinterTransitsFasterThanMarcher()
    {
        // speed 10 vs 5 => sprinter zone transit 10 ticks, marcher 20
        var sim = Sim();
        int s = (int)SkeletonTypeId.Sprinter, m = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[s, 0] = 1;
        sim.State.Counts[m, 0] = 1;
        for (int i = 0; i < 10; i++) sim.Tick();
        Assert.Equal(1, sim.State.Counts[s, 1]); // sprinter advanced
        Assert.Equal(0, sim.State.Counts[m, 1]); // marcher has not (spawner adds to zone 0 only)
    }

    [Fact]
    public void KillPriorityFollowsEnumOrderAfterShieldbearers()
    {
        // Park sprinters and marchers in tower zone with no shieldbearers:
        // pool must kill marchers before sprinters.
        var sim = new Simulation(TestBalance.Make() with
            { Kingdoms = new() { TestBalance.Make().Kingdoms[0] with { BaseWallHp = 1e12 } } });
        int m = (int)SkeletonTypeId.Marcher, s = (int)SkeletonTypeId.Sprinter;
        sim.State.Counts[m, 1] = 1000;
        sim.State.Counts[s, 1] = 1000;
        var deaths = new System.Collections.Generic.List<SkeletonsDied>();
        for (int i = 0; i < 10; i++) // 1s = 10 dmg = exactly 1 marcher (hp 10), 0 sprinters
            deaths.AddRange(sim.Tick().OfType<SkeletonsDied>().Where(e => e.Cause == DeathCause.Tower));
        Assert.Equal(1, deaths.Where(d => d.Type == SkeletonTypeId.Marcher).Sum(d => d.Count));
        Assert.Equal(0, deaths.Where(d => d.Type == SkeletonTypeId.Sprinter).Sum(d => d.Count));
    }
}
```

Replace `MovementTests.cs` with tightened versions (M1 deferred item — exact assertions, no-spawn balance so counts are exact):

```csharp
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class MovementTests
{
    // MarcherRate 0 => no ambient spawns; counts are fully controlled.
    private static Simulation NoSpawnSim()
    {
        var b = TestBalance.Make();
        var up = new System.Collections.Generic.Dictionary<UpgradeId, UpgradeBalance>(b.Upgrades)
        {
            [UpgradeId.MarcherRate] = b.Upgrades[UpgradeId.MarcherRate] with { BaseValue = 0.0 }
        };
        return new Simulation(b with { Upgrades = up });
    }

    [Fact]
    public void SingleMarcherAdvancesExactlyOneZonePerTransitTime()
    {
        var sim = NoSpawnSim();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        for (int i = 0; i < 19; i++) sim.Tick();
        Assert.Equal(1, sim.State.Counts[t, 0]); // not yet: transit = 20 ticks
        Assert.Equal(0, sim.State.Counts[t, 1]);
        sim.Tick(); // tick 20
        Assert.Equal(0, sim.State.Counts[t, 0]); // moved exactly one zone, not two
        Assert.Equal(1, sim.State.Counts[t, 1]);
    }

    [Fact]
    public void NoSkeletonSkipsAZoneInOneTick()
    {
        var sim = NoSpawnSim();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        sim.State.MoveAcc[t, 0] = 0.999; // primed to move NOW
        sim.State.MoveAcc[t, 1] = 0.999; // zone 1 also primed
        sim.Tick();
        // The mover entered zone 1 this tick; it must NOT also exit zone 1 this tick.
        Assert.Equal(1, sim.State.Counts[t, 1]);
    }

    [Fact]
    public void ThroughputIsConservedNoDoubleCountOrLoss()
    {
        var sim = new Simulation(TestBalance.Make() with
            { Kingdoms = new() { TestBalance.Make().Kingdoms[0] with
                { Towers = new(), BaseWallHp = 1e12 } } });
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 100;
        int wallDeaths = 0;
        for (int i = 0; i < 3000; i++)
            foreach (var e in sim.Tick())
                if (e is SkeletonsDied d && d.Cause == DeathCause.WallCrumble && d.Type == SkeletonTypeId.Marcher)
                    wallDeaths += d.Count;
        int alive = sim.State.Counts[t, 0] + sim.State.Counts[t, 1];
        int spawned = 0; // MarcherRate 1.0/s * 300s
        // spawned count: base rate 1.0/s for 300s = 300 spawns
        spawned = 300;
        Assert.Equal(100 + spawned, wallDeaths + alive); // exact conservation
    }
}
```

- [ ] **Step 2: Run to verify failure** — new-type tests fail (Spawn only covers 2 types), tightened movement tests fail or pass depending on migration — run and record.

- [ ] **Step 3: Implement** — in `Simulation`: replace the two `SpawnType` calls with a loop over a static map:

```csharp
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
```

and generalize `TowerFire`'s kill gate to the priority array:

```csharp
    private static readonly SkeletonTypeId[] KillPriority =
    {
        SkeletonTypeId.Shieldbearer, SkeletonTypeId.Marcher, SkeletonTypeId.Sprinter,
        SkeletonTypeId.Bomber, SkeletonTypeId.Bannerbones,
    };
    // in TowerFire, replace the two KillInZone calls:
    foreach (var type in KillPriority)
    {
        KillInZone(type, z, events);
        if (State.Counts[(int)type, z] > 0) break; // survivors soak the residue
    }
```

- [ ] **Step 4: Run tests** — `dotnet test BoneVoyage.sln` → all pass.
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: five skeleton types with priority soak; exact movement tests"`

---

### Task 3: Moats + banner aura

**Files:**
- Modify: `sim/BoneVoyage.Sim/Simulation.cs`
- Create: `sim/BoneVoyage.Sim.Tests/AuraAndMoatTests.cs`

**Interfaces:**
- Consumes: Tasks 1–2. Produces: `Move` uses per-zone effective speed `Skeletons[type].Speed * MoatMult(zone)`; `TowerFire` accrues pool damage scaled by `1 - BannerDr(zone)` where `BannerDr(zone) = Math.Min(Banner.DrCap, Counts[Bannerbones, zone] * Banner.DrPerBanner * GetUpgradeValue(UpgradeId.WarBanners))`. Public helper `double MoatMult(int zone)` (1.0 when no moat) and `double BannerDr(int zone)` on `Simulation` (view/tests use them).

- [ ] **Step 1: Write failing tests** — `AuraAndMoatTests.cs`:

```csharp
using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class AuraAndMoatTests
{
    private static Simulation OnMoatKingdom()
    {
        var b = TestBalance.Make();
        // Kingdom 1 (Moatholm): moat zone 0 speedMult 0.5, cannon zone 0.
        var sim = new Simulation(b);
        sim.State.KingdomIndex = 1;
        sim.State.WallHp = sim.WallHpFor(1, 1);
        return sim;
    }

    [Fact]
    public void MoatHalvesTransitSpeed()
    {
        var sim = OnMoatKingdom();
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        for (int i = 0; i < 39; i++) sim.Tick(); // normal transit 20 ticks; moat x2 => 40
        Assert.Equal(0, sim.State.Counts[t, 1]);
        sim.Tick();
        Assert.Equal(1, sim.State.Counts[t, 1]);
    }

    [Fact]
    public void MoatMultReadsCurrentKingdom()
    {
        var sim = new Simulation(TestBalance.Make());
        Assert.Equal(1.0, sim.MoatMult(0)); // Testshire has no moat
        sim.State.KingdomIndex = 1;
        Assert.Equal(0.5, sim.MoatMult(0));
        Assert.Equal(1.0, sim.MoatMult(1));
    }

    [Fact]
    public void BannersReduceTowerDamageAccrual()
    {
        // DrPerBanner 0.10, 2 banners => 20% reduction; cap 0.4 at >=4 banners.
        var big = TestBalance.Make();
        var sim = new Simulation(big with
            { Kingdoms = new() { big.Kingdoms[0] with { BaseWallHp = 1e12 } } });
        int m = (int)SkeletonTypeId.Marcher, b = (int)SkeletonTypeId.Bannerbones;
        sim.State.Counts[m, 1] = 1000;
        sim.State.Counts[b, 1] = 2;   // 20% DR — banners are killed LAST (priority), marchers die first
        var kills = 0;
        for (int i = 0; i < 25; i++)  // 2.5s * 10 dps * 0.8 = 20 dmg = 2 marchers
            kills += sim.Tick().OfType<SkeletonsDied>()
                .Where(e => e.Cause == DeathCause.Tower && e.Type == SkeletonTypeId.Marcher)
                .Sum(e => e.Count);
        Assert.Equal(2, kills);
    }

    [Fact]
    public void BannerDrIsCapped()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Counts[(int)SkeletonTypeId.Bannerbones, 1] = 100;
        Assert.Equal(0.4, sim.BannerDr(1)); // cap, not 100*0.10
    }
}
```

- [ ] **Step 2: Run to verify failure.**

- [ ] **Step 3: Implement** — in `Simulation`:

```csharp
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
```

In `Move`, transit becomes per-zone: `double transit = Balance.ZoneLength / (Balance.Skeletons[type].Speed * MoatMult(z));` (compute inside the zone loop). In `TowerFire`, pool accrual becomes `State.DamagePool[z] += tower.Dps * dpsMult * (1.0 - BannerDr(z)) * dt;`.

- [ ] **Step 4: Run tests** — all pass.
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: moat slow zones and bannerbones damage-reduction aura"`

---

### Task 4: Gold income + gear multipliers + currency-aware purchases

**Files:**
- Modify: `sim/BoneVoyage.Sim/Simulation.cs`
- Create: `sim/BoneVoyage.Sim.Tests/EconomyTests.cs`

**Interfaces:**
- Consumes: Tasks 1–3. Produces on `Simulation`: `double EffHp(SkeletonTypeId)` (= Hp × Helmets value), `double EffWallDamage(SkeletonTypeId)` (= WallDamage × BatteringRams value), `double EffSpeed(SkeletonTypeId)` (= Speed × MarchingBoots value × perk SpeedMult — perks arrive Task 5; use `GetPerkMult(PerkId.SpeedMult)` which this task stubs to return 1.0 until Task 5 implements it). `TryPurchase` debits `State.Gold` when `Upgrades[id].Currency == Currency.Gold`. Wall damage pays `State.Gold += damage * Balance.GoldPerWallDamage * GetPerkMult(PerkId.GoldMult)`. `KillInZone` uses `EffHp`; `HitWall` uses `EffWallDamage`; `Move` uses `EffSpeed` (× MoatMult).

- [ ] **Step 1: Write failing tests** — `EconomyTests.cs`:

```csharp
using System.Linq;
using BoneVoyage.Sim;
using Xunit;

namespace BoneVoyage.Sim.Tests;

public class EconomyTests
{
    private static Simulation NoTowerSim() =>
        new(TestBalance.Make() with
            { Kingdoms = new() { TestBalance.Make().Kingdoms[0] with
                { Towers = new(), BaseWallHp = 1e12 } } });

    private static void SendOneToWall(Simulation sim, SkeletonTypeId type)
    {
        int t = (int)type, last = sim.Balance.ZoneCount - 1;
        sim.State.Counts[t, last] = 1;
        sim.State.MoveAcc[t, last] = 0.999;
        for (int i = 0; i < 60; i++) sim.Tick();
    }

    [Fact]
    public void WallDamagePaysGold()
    {
        var sim = NoTowerSim();
        SendOneToWall(sim, SkeletonTypeId.Marcher); // wallDamage 5 * goldPerWallDamage 1.0
        Assert.Equal(5.0, sim.State.Gold, 3);
    }

    [Fact]
    public void GoldPurchaseDebitsGoldNotBones()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Bones = 1000; sim.State.Gold = 1000;
        Assert.True(sim.TryPurchase(UpgradeId.Helmets)); // cost 50 gold
        Assert.Equal(950.0, sim.State.Gold);
        Assert.Equal(1000.0, sim.State.Bones);
    }

    [Fact]
    public void GoldPurchaseFailsWhenOnlyBonesRich()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Bones = 1e9;
        Assert.False(sim.TryPurchase(UpgradeId.Helmets));
    }

    [Fact]
    public void HelmetsRaiseEffectiveHp()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Gold = 1000;
        sim.TryPurchase(UpgradeId.Helmets); // 1.05x
        Assert.Equal(10.5, sim.EffHp(SkeletonTypeId.Marcher), 3);
    }

    [Fact]
    public void RamsRaiseWallDamageAndGold()
    {
        var sim = NoTowerSim();
        sim.State.Gold = 1000;
        sim.TryPurchase(UpgradeId.BatteringRams); // 1.05x => marcher 5.25 dmg
        double goldBefore = sim.State.Gold;
        SendOneToWall(sim, SkeletonTypeId.Marcher);
        Assert.Equal(goldBefore + 5.25, sim.State.Gold, 2);
    }

    [Fact]
    public void BootsSpeedUpTransit()
    {
        var sim = new Simulation(TestBalance.Make());
        sim.State.Gold = 1000;
        sim.TryPurchase(UpgradeId.MarchingBoots); // fixture: +0.5 => 1.5x speed, transit 20 -> 13.33 ticks
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 0] = 1;
        for (int i = 0; i < 14; i++) sim.Tick();
        Assert.Equal(1, sim.State.Counts[t, 1]); // arrived by tick 14 (vs 20 unbuffed)
    }
}
```

- [ ] **Step 2: Run to verify failure.**

- [ ] **Step 3: Implement** — in `Simulation`:

```csharp
    public double GetPerkMult(PerkId id) => 1.0; // Task 5 replaces with real implementation

    public double EffHp(SkeletonTypeId type) =>
        Balance.Skeletons[type].Hp * GetUpgradeValue(UpgradeId.Helmets);

    public double EffWallDamage(SkeletonTypeId type) =>
        Balance.Skeletons[type].WallDamage * GetUpgradeValue(UpgradeId.BatteringRams);

    public double EffSpeed(SkeletonTypeId type) =>
        Balance.Skeletons[type].Speed * GetUpgradeValue(UpgradeId.MarchingBoots)
            * GetPerkMult(PerkId.SpeedMult);
```

`TryPurchase` becomes wallet-aware:

```csharp
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
```

`Move`: `double transit = Balance.ZoneLength / (EffSpeed(type) * MoatMult(z));`. `KillInZone`: `double hp = EffHp(type);`. `HitWall`: `double damage = count * EffWallDamage(type); State.Gold += damage * Balance.GoldPerWallDamage * GetPerkMult(PerkId.GoldMult);` (before the fall-cascade block).

- [ ] **Step 4: Run tests** — all pass.
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: gold income, gear multipliers, currency-aware purchases"`

---

### Task 5: Multi-kingdom progression + perks + multi-tower test

**Files:**
- Modify: `sim/BoneVoyage.Sim/Simulation.cs`
- Create: `sim/BoneVoyage.Sim.Tests/KingdomTests.cs`
- Modify: `sim/BoneVoyage.Sim.Tests/TowerTests.cs` (add multi-tower-same-zone test — M1 deferred item)

**Interfaces:**
- Consumes: Tasks 1–4. Produces: real `GetPerkMult`: product of `Kingdoms[i].Perk.Mult` for `i < State.PerksUnlocked` where `Perk.Id == id`, else 1.0. Bounty payout multiplies by `GetPerkMult(PerkId.BountyMult)`. Kingdom-fall block per Task 1's migration contract (already in place) — this task verifies it end-to-end and adds perk math.

- [ ] **Step 1: Write failing tests** — `KingdomTests.cs`:

```csharp
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
```

Add to `TowerTests.cs` (M1 deferred item):

```csharp
    [Fact]
    public void TwoTowersInSameZoneShareOnePoolWithoutDoubleReset()
    {
        var b = TestBalance.Make();
        var sim = new Simulation(b with { Kingdoms = new()
        {
            b.Kingdoms[0] with
            {
                BaseWallHp = 1e12,
                Towers = new() { new("ArrowTower", 1, 10.0), new("ArrowTower", 1, 10.0) }
            }
        }});
        int t = (int)SkeletonTypeId.Marcher;
        sim.State.Counts[t, 1] = 1000;
        var kills = 0;
        for (int i = 0; i < 10; i++) // 1s * 20 combined dps / 10 hp = 2 kills
            kills += sim.Tick().OfType<SkeletonsDied>()
                .Where(e => e.Cause == DeathCause.Tower).Sum(e => e.Count);
        Assert.Equal(2, kills);
    }
```

- [ ] **Step 2: Run to verify failure** — perk tests fail (`GetPerkMult` stub returns 1.0).

- [ ] **Step 3: Implement** — replace the stub:

```csharp
    public double GetPerkMult(PerkId id)
    {
        double mult = 1.0;
        for (int i = 0; i < State.PerksUnlocked && i < Balance.Kingdoms.Count; i++)
            if (Balance.Kingdoms[i].Perk.Id == id)
                mult *= Balance.Kingdoms[i].Perk.Mult;
        return mult;
    }
```

and in `AwardBounty`, multiply by `GetPerkMult(PerkId.BountyMult)`. Fix the two-towers double-reset if present: the empty-zone `DamagePool` reset must be computed once per zone, not once per tower (guard: reset only if the pool wasn't already handled this tick, or restructure to iterate zones of `CurrentKingdom.Towers.GroupBy(t => t.Zone)` — choose the GroupBy restructure; it also makes the shared-pool semantics explicit).

- [ ] **Step 4: Run tests** — all pass.
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: multi-kingdom conquest with cumulative perks; multi-tower pool test"`

---

### Task 6: Save codec

**Files:**
- Create: `sim/BoneVoyage.Sim/SaveCodec.cs`, `sim/BoneVoyage.Sim.Tests/SaveTests.cs`
- Modify: `sim/BoneVoyage.Sim/Simulation.cs` (restore ctor)

**Interfaces:**
- Produces:

```csharp
public sealed record SaveData(int Version, long TimestampUtcSeconds, double Bones, double Gold,
    int KingdomIndex, int CycleTier, int PerksUnlocked, int TotalFalls, double WallHp,
    Dictionary<UpgradeId, int> UpgradeLevels, int[][] Counts);

public static class SaveCodec
{
    public const int CurrentVersion = 1;
    public static string ToJson(SimState state, long timestampUtcSeconds);
    public static SaveData Parse(string json); // ArgumentException on version > CurrentVersion or null
}
// Simulation gains: public Simulation(BalanceData balance, SaveData? save) — null behaves like M1 ctor;
// non-null restores all SaveData fields into State (Counts clamped to current type/zone dims).
```

Accumulators (`SpawnAcc`/`MoveAcc`/`DamagePool`) are deliberately NOT saved — they hold sub-unit residue only; restore starts them at zero. Timestamp is stored for M3's offline calculator; M2 only round-trips it.

- [ ] **Step 1: Write failing tests** — `SaveTests.cs`:

```csharp
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
}
```

- [ ] **Step 2: Run to verify failure.**

- [ ] **Step 3: Implement** — `SaveCodec.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoneVoyage.Sim;

public sealed record SaveData(int Version, long TimestampUtcSeconds, double Bones, double Gold,
    int KingdomIndex, int CycleTier, int PerksUnlocked, int TotalFalls, double WallHp,
    Dictionary<UpgradeId, int> UpgradeLevels, int[][] Counts);

public static class SaveCodec
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ToJson(SimState state, long timestampUtcSeconds)
    {
        int types = state.Counts.GetLength(0), zones = state.Counts.GetLength(1);
        var counts = new int[types][];
        for (int t = 0; t < types; t++)
        {
            counts[t] = new int[zones];
            for (int z = 0; z < zones; z++) counts[t][z] = state.Counts[t, z];
        }
        var data = new SaveData(CurrentVersion, timestampUtcSeconds, state.Bones, state.Gold,
            state.KingdomIndex, state.CycleTier, state.PerksUnlocked, state.TotalFalls,
            state.WallHp, new(state.UpgradeLevels), counts);
        return JsonSerializer.Serialize(data, Options);
    }

    public static SaveData Parse(string json)
    {
        var data = JsonSerializer.Deserialize<SaveData>(json, Options)
            ?? throw new ArgumentException("save deserialized to null");
        if (data.Version > CurrentVersion)
            throw new ArgumentException($"save version {data.Version} is newer than supported {CurrentVersion}");
        return data;
    }
}
```

`Simulation` restore ctor (keep the existing single-arg ctor delegating to it):

```csharp
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
```

- [ ] **Step 4: Run tests** — all pass.
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: versioned save codec with restore constructor"`

---

### Task 7: Spritegen — Sprinter, Bomber, Bannerbones sheets

**Files:**
- Modify: `tools/spritegen/skeletons.py`, `tools/spritegen/generate.py`, `tools/spritegen/palette.py`
- Output (committed): `game/assets/sprites/{sprinter,bomber,bannerbones}{.png,.meta.json}`

**Interfaces:**
- Produces: three more sheets in the EXACT M1 format (24px frames, rows walk4/die_wave6/die_salute6/whack6, same meta JSON shape). `SpriteSheetLoader` consumes them unchanged. `generate.py` emits: `for name, variant in [("marcher", ...), ("shieldbearer", ...), ("sprinter", ...), ("bomber", ...), ("bannerbones", ...)]`.

Refactor `skeletons.py` from the boolean `shield=` parameter to a variant string: `build_sheet(variant)` where `variant in {"marcher","shieldbearer","sprinter","bomber","bannerbones"}`; pose functions take `variant` and add trimmings:
- **sprinter**: 1px-narrower torso, a 2–3px trailing scarf behind the neck (new palette entry `SCARF = (214, 96, 88, 255)`, documented), legs in wider stride poses on walk frames.
- **bomber**: carries a round dark bomb (5px circle, `OUTLINE` fill, `SPARK` fuse pixel) in the front arm on walk/die rows; its **whack row is a bomb-boom**: f0 places bomb at wall side, f1 crouch + fuse spark, f2 EXPLOSION (12–14 px starburst of `SPARK`/`BLUSH`/`BONE` pixels replacing the skeleton), f3–5 bone pile with drifting sparks. The bomb is absent from the pile frames (it exploded).
- **bannerbones**: carries a vertical banner pole (2px `SIGN_WOOD` pole topped with a 5×4 `ACCENT`-colored pennant — add `ACCENT = (255, 183, 77, 255)` to palette.py if not present) on walk/die rows; pole falls across the bone pile in death frames.

The brief's drawing details are starting points — iterate by reading the 8× previews until each type is instantly tellable from the others in the walk row, and the bomber's boom reads as an explosion.

- [ ] **Step 1: Refactor `build_sheet(shield=False)` → `build_sheet(variant)`** keeping marcher/shieldbearer output byte-identical is NOT required (small drift acceptable) but the sheet format contract is frozen. Update `generate.py`'s loop accordingly.
- [ ] **Step 2: Implement the three variants** per the trimmings above.
- [ ] **Step 3: Run `python3 tools/spritegen/generate.py`** — expect five `wrote <name>: 144x96` lines + props. Read all five previews; iterate until distinct and readable.
- [ ] **Step 4: Verify meta contract** — each new `.meta.json` byte-equal in structure to marcher's (same animation names/rows/frames/fps/loop).
- [ ] **Step 5: Commit** — `git add -A && git commit -m "feat: sprinter, bomber, bannerbones sprite sheets"`

---

### Task 8: Spritegen — knight post, moat tile, conquest flag

**Files:**
- Modify: `tools/spritegen/props.py`
- Output (committed): `game/assets/sprites/tower_knight.png` (24×48), `moat.png` (24×24), `flag.png` (16×16)

**Interfaces:**
- Produces: `tower_knight` 24×48 (stone post with a small knight helmet + sword silhouette on top — visually distinct from arrow/cannon towers); `moat` 24×24 water tile (two blues — add `WATER = (96, 148, 200, 255)` and `WATER_DK = (66, 108, 156, 255)` to palette.py, documented — with 2–3 lighter ripple pixels); `flag` 16×16 (pole + `ACCENT` pennant, transparent background) for the conquest strip. Task 10 places `moat.png` stretched across moat zones; Task 11 uses `flag.png`.

- [ ] **Step 1: Implement the three props** in `props.py`'s `emit_all` (same emit pipeline; static sprites, no meta).
- [ ] **Step 2: Run generator; read previews; iterate.** Knight post must read as "melee danger" next to the arrow/cannon towers.
- [ ] **Step 3: Commit** — `git add -A && git commit -m "feat: knight post, moat tile, conquest flag props"`

---

### Task 9: Shop + Armory panels with derived labels; HUD gold

**Files:**
- Modify: `game/scripts/ShopPanel.cs`, `game/scripts/Hud.cs`
- Create: `game/scripts/ArmoryPanel.cs`

**Interfaces:**
- Consumes: sim v2 (`GetUpgradeValue`, `GetUpgradeCost`, `TryPurchase`, `Balance.Upgrades[id]` incl. `PerLevel` + `Currency`).
- Produces: `ShopPanel` (bones, bottom-left, 6 rows: 5 rates + Recovery) and `ArmoryPanel` (gold, bottom-right, 4 rows: Helmets/BatteringRams/MarchingBoots/WarBanners), both with labels DERIVED from balance (M1 deferred item). Shared static helper `ShopRow.EffectLabel(Simulation sim, UpgradeId id)`:
  - rate upgrades (`PerLevel` interpreted as /s): `$"+{u.PerLevel:0.##}/s"`
  - multiplier upgrades (BaseValue 1.0 gear + Recovery): `$"+{u.PerLevel:P0}"` (e.g. "+5%")
  - Decide by id: `Recovery, Helmets, BatteringRams, MarchingBoots, WarBanners` → percent; others → rate.
- `Hud` adds a gold label (`Tr("Gold")`, gold-ish color `new Color("ffd670")`) under the bones label, and instantiates/refreshes both panels (ShopPanel keeps its position `(16, 484)`; ArmoryPanel at `(1000, 484)`, size `(264, 220)`).

- [ ] **Step 1: Implement** — new `ArmoryPanel.cs` and reworked `ShopPanel.cs` share structure; to stay DRY put the row logic in one file:

`game/scripts/ShopPanel.cs` (full replacement — contains the shared row helper both panels use):

```csharp
using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public static class ShopRow
{
    private static readonly HashSet<UpgradeId> Percent = new()
    {
        UpgradeId.Recovery, UpgradeId.Helmets, UpgradeId.BatteringRams,
        UpgradeId.MarchingBoots, UpgradeId.WarBanners,
    };

    public static string EffectLabel(Simulation sim, UpgradeId id)
    {
        var u = sim.Balance.Upgrades[id];
        return Percent.Contains(id) ? $"+{u.PerLevel:P0}" : $"+{u.PerLevel:0.##}/s";
    }

    public static void Refresh(Simulation sim, UpgradeId id, string name, Button btn)
    {
        int lvl = sim.State.UpgradeLevels.GetValueOrDefault(id);
        double cost = sim.GetUpgradeCost(id);
        bool gold = sim.Balance.Upgrades[id].Currency == Currency.Gold;
        double wallet = gold ? sim.State.Gold : sim.State.Bones;
        string unit = gold ? "gold" : "bones";
        btn.Text = $"{name} Lv{lvl} ({EffectLabel(sim, id)})\n{Fmt.Num(cost)} {unit}";
        btn.Disabled = wallet < cost;
    }
}

public partial class ShopPanel : PanelContainer
{
    private readonly Simulation _sim;
    private readonly List<(UpgradeId Id, string Name, Button Btn)> _rows = new();

    public ShopPanel(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        Position = new Vector2(16, 420);
        Size = new Vector2(264, 288);
        var box = new VBoxContainer();
        AddChild(box);
        var title = new Label { Text = Tr("Necropolis HR") };
        title.AddThemeFontSizeOverride("font_size", 18);
        box.AddChild(title);
        AddRow(box, UpgradeId.MarcherRate, Tr("Hire Marchers"));
        AddRow(box, UpgradeId.ShieldbearerRate, Tr("Hire Shieldbearers"));
        AddRow(box, UpgradeId.SprinterRate, Tr("Hire Sprinters"));
        AddRow(box, UpgradeId.BomberRate, Tr("Hire Bombers"));
        AddRow(box, UpgradeId.BannerRate, Tr("Hire Bannerbones"));
        AddRow(box, UpgradeId.Recovery, Tr("Death Benefits"));
    }

    private void AddRow(VBoxContainer box, UpgradeId id, string name)
    {
        var btn = new Button { CustomMinimumSize = new Vector2(248, 40) };
        btn.Pressed += () => _sim.TryPurchase(id);
        box.AddChild(btn);
        _rows.Add((id, name, btn));
    }

    public void Refresh()
    {
        foreach (var (id, name, btn) in _rows)
            ShopRow.Refresh(_sim, id, name, btn);
    }
}
```

`game/scripts/ArmoryPanel.cs`:

```csharp
using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class ArmoryPanel : PanelContainer
{
    private readonly Simulation _sim;
    private readonly List<(UpgradeId Id, string Name, Button Btn)> _rows = new();

    public ArmoryPanel(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        Position = new Vector2(1000, 484);
        Size = new Vector2(264, 220);
        var box = new VBoxContainer();
        AddChild(box);
        var title = new Label { Text = Tr("Royal Armory") };
        title.AddThemeFontSizeOverride("font_size", 18);
        box.AddChild(title);
        AddRow(box, UpgradeId.Helmets, Tr("Helmets"));
        AddRow(box, UpgradeId.BatteringRams, Tr("Battering Rams"));
        AddRow(box, UpgradeId.MarchingBoots, Tr("Marching Boots"));
        AddRow(box, UpgradeId.WarBanners, Tr("War Banners"));
    }

    private void AddRow(VBoxContainer box, UpgradeId id, string name)
    {
        var btn = new Button { CustomMinimumSize = new Vector2(248, 40) };
        btn.Pressed += () => _sim.TryPurchase(id);
        box.AddChild(btn);
        _rows.Add((id, name, btn));
    }

    public void Refresh()
    {
        foreach (var (id, name, btn) in _rows)
            ShopRow.Refresh(_sim, id, name, btn);
    }
}
```

`Hud.cs` additions: `_gold` label at `(16, 44)` size 20 with `AddThemeColorOverride("font_color", new Color("ffd670"))` (shift `_tier` to `(16, 74)` and `_employees` to `(16, 100)`); `_armory = new ArmoryPanel(_sim); AddChild(_armory);` next to the shop; `Refresh()` sets `_gold.Text = $"{Tr("Gold")}: {Fmt.Num(_sim.State.Gold)}";` and calls `_armory.Refresh();`.

- [ ] **Step 2: Build + screenshot** — `dotnet build` 0 errors 0 warnings; `timeout 90 godot --path game -- --shot=<repo>/.superpowers/sdd/task9-shot.png`; READ it: both panels visible without overlapping battlefield props; six shop rows fit inside the panel; gold label present.
- [ ] **Step 3: Commit** — `git add -A && git commit -m "feat: six-row shop, royal armory, gold HUD with derived labels"`

---

### Task 10: Battlefield — kingdom rebuild, moats, five actor types

**Files:**
- Modify: `game/scripts/BattlefieldView.cs`

**Interfaces:**
- Consumes: sim v2 (`CurrentKingdom` with `.Towers`/`.Moats`/`.Name`, `KingdomFell` v2, five-type `_frames`), sprites from Tasks 7–8.
- Produces: `BattlefieldView` loads all five sheets into `_frames`; props (towers, moat tiles, wall) are built by `RebuildKingdom()` — children tagged in a `List<Node2D> _kingdomProps` that gets freed and rebuilt from `CurrentKingdom` on `_Ready` AND on every `KingdomFell` event; tower sprite by name: `"Cannon"→tower_cannon`, `"KnightPost"→tower_knight`, anything else→`tower_arrow`; moat zones render `moat.png` tiled across the zone width (10 tiles of 24px scaled ×~0.42 — simpler: one `Sprite2D` per moat zone scaled to `(zoneWidthPx/24f, 1.2f)` at road level); actor speed uses `sim.EffSpeed(type) * PxPerUnit` at spawn time (cosmetic — sim remains authoritative).

- [ ] **Step 1: Implement** — key changes (full method bodies):

```csharp
    private readonly List<Node2D> _kingdomProps = new();

    public override void _Ready()
    {
        // sky/grass/road ColorRects and necropolis Place() unchanged from M1 ...
        foreach (SkeletonTypeId type in System.Enum.GetValues<SkeletonTypeId>())
            _frames[type] = SpriteSheetLoader.Load(
                $"res://assets/sprites/{type.ToString().ToLower()}");
        RebuildKingdom();
    }

    private static string TowerSprite(string towerName) => towerName switch
    {
        "Cannon" => "tower_cannon",
        "KnightPost" => "tower_knight",
        _ => "tower_arrow",
    };

    private void RebuildKingdom()
    {
        foreach (var p in _kingdomProps) if (IsInstanceValid(p)) p.QueueFree();
        _kingdomProps.Clear();
        float zonePx = (float)_sim.Balance.ZoneLength * PxPerUnit;
        foreach (var moat in _sim.CurrentKingdom.Moats)
        {
            var tile = new Sprite2D
            {
                Texture = GD.Load<Texture2D>("res://assets/sprites/moat.png"),
                Position = new Vector2(RoadStartX + moat.Zone * zonePx, GroundY + 18),
                Scale = new Vector2(zonePx / 24f, 1.2f),
                Centered = false, ZIndex = -15,
            };
            _kingdomProps.Add(tile); AddChild(tile);
        }
        foreach (var tower in _sim.CurrentKingdom.Towers)
        {
            float x = RoadStartX + (tower.Zone + 0.5f) * zonePx;
            var s = new Sprite2D
            {
                Texture = GD.Load<Texture2D>($"res://assets/sprites/{TowerSprite(tower.Name)}.png"),
                Position = new Vector2(x - 24, GroundY - 76),
                Scale = new Vector2(2, 2), Centered = false, ZIndex = -10,
            };
            _kingdomProps.Add(s); AddChild(s);
        }
        var wall = new Sprite2D
        {
            Texture = GD.Load<Texture2D>("res://assets/sprites/wall.png"),
            Position = new Vector2(WallX, GroundY - 300),
            Scale = new Vector2(2, 2), Centered = false, ZIndex = -10,
        };
        _kingdomProps.Add(wall); AddChild(wall);
    }
```

(The M1 `_Ready` wall/tower `Place()` calls are replaced by `RebuildKingdom()`; the necropolis stays a permanent child. Remove the old `Place` usage for towers/wall; keep `Place` for the necropolis or inline it.) In `OnSimEvents`, add: `case KingdomFell: RebuildKingdom(); break;`. In `SpawnActors`, speed becomes `float speedPx = (float)_sim.EffSpeed(e.Type) * PxPerUnit;`.

- [ ] **Step 2: Build + screenshots** — default shot (Meadowshire: 2 towers, no moat) and a long `--frames=12000` (~3.3 min; first wall may fall if bones bought — at minimum verify no errors and marching of multiple types after temporarily giving startingBones? NO — do not edit balance. Instead verify Cannonspire visuals directly: add a TEMPORARY test save file? No — simplest correct check: run the game with a temporary save written by a scratch C# script? Overkill. Accept: verify Meadowshire visuals in the shot; kingdom-switch rendering is exercised by the `KingdomFell → RebuildKingdom` path which Task 13's long playtest covers). READ the screenshot: marchers walking; towers/wall present; no duplicated props.
- [ ] **Step 3: Commit** — `git add -A && git commit -m "feat: per-kingdom battlefield rebuild with moats and five actor types"`

---

### Task 11: Conquest strip

**Files:**
- Create: `game/scripts/ConquestStrip.cs`
- Modify: `game/scripts/Hud.cs`

**Interfaces:**
- Consumes: `Balance.Kingdoms` (names/perks), `State.KingdomIndex`, `State.PerksUnlocked`, `State.CycleTier`, `flag.png`.
- Produces: `ConquestStrip(Simulation)` — a `Control` row centered at top (`Position (400, 8)`, `Size (480, 48)`), one cell per kingdom: kingdom name label; a `TextureRect` flag (visible when `index < PerksUnlocked`); current-siege cell highlighted (name in `ffd670`); below each name, its perk in short form via `PerkLabel` (e.g. "+15% bounty"). `void Refresh()` updates visibility/colors. Hud instantiates and refreshes it; Hud's own `_tier` label is REMOVED (kingdom name + tier now live in the strip: current cell text `$"{Tr(name)} {Roman(CycleTier)}"` — move the `ToRoman` helper into `ConquestStrip` and delete it from Hud).

- [ ] **Step 1: Implement** — `ConquestStrip.cs`:

```csharp
using System.Collections.Generic;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public partial class ConquestStrip : Control
{
    private readonly Simulation _sim;
    private readonly List<(Label Name, Label Perk, TextureRect Flag)> _cells = new();

    public ConquestStrip(Simulation sim) => _sim = sim;

    public override void _Ready()
    {
        Position = new Vector2(400, 8);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        AddChild(row);
        foreach (var k in _sim.Balance.Kingdoms)
        {
            var cell = new VBoxContainer();
            var top = new HBoxContainer();
            var flag = new TextureRect
            {
                Texture = GD.Load<Texture2D>("res://assets/sprites/flag.png"),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(20, 20), Visible = false,
            };
            var name = new Label();
            name.AddThemeFontSizeOverride("font_size", 16);
            top.AddChild(flag); top.AddChild(name);
            var perk = new Label { Text = PerkLabel(k.Perk) };
            perk.AddThemeFontSizeOverride("font_size", 11);
            perk.AddThemeColorOverride("font_color", new Color("c9d1bf"));
            cell.AddChild(top); cell.AddChild(perk);
            row.AddChild(cell);
            _cells.Add((name, perk, flag));
        }
    }

    private string PerkLabel(PerkBalance perk)
    {
        string pct = $"+{perk.Mult - 1.0:P0}";
        return perk.Id switch
        {
            PerkId.BountyMult => $"{pct} {Tr("bounty")}",
            PerkId.SpeedMult => $"{pct} {Tr("march speed")}",
            PerkId.GoldMult => $"{pct} {Tr("gold")}",
            _ => pct,
        };
    }

    public void Refresh()
    {
        for (int i = 0; i < _cells.Count; i++)
        {
            var k = _sim.Balance.Kingdoms[i];
            bool current = i == _sim.State.KingdomIndex;
            _cells[i].Name.Text = current
                ? $"{Tr(k.Name)} {Roman(_sim.State.CycleTier)}" : Tr(k.Name);
            _cells[i].Name.AddThemeColorOverride("font_color",
                current ? new Color("ffd670") : new Color("ffffff"));
            _cells[i].Flag.Visible = i < _sim.State.PerksUnlocked;
        }
    }

    private static string Roman(int n)
    {
        if (n >= 40) return n.ToString();
        string[] tens = { "", "X", "XX", "XXX" };
        string[] ones = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
        return tens[n / 10] + ones[n % 10];
    }
}
```

Hud: remove `_tier` label + `ToRoman`; add `_strip = new ConquestStrip(_sim); AddChild(_strip);` and `_strip.Refresh();` in `Refresh()`.

- [ ] **Step 2: Build + screenshot; READ it** — three kingdom cells top-center, Meadowshire highlighted gold with tier "I", perk captions under each, no flags yet (none conquered), no overlap with the wall-HP bar.
- [ ] **Step 3: Commit** — `git add -A && git commit -m "feat: conquest strip with perk captions and flags"`

---

### Task 12: SaveManager

**Files:**
- Create: `game/scripts/SaveManager.cs`
- Modify: `game/scripts/Main.cs`

**Interfaces:**
- Consumes: `SaveCodec.ToJson(state, timestamp)` / `Parse`, `Simulation(balance, SaveData?)`.
- Produces: `static class SaveManager` with `const string SavePath = "user://save.json"`; `SaveData? TryLoad()` (null when absent or corrupt — corrupt logs `GD.PushWarning` and returns null rather than crashing); `void Save(SimState state)` (timestamp from `DateTimeOffset.UtcNow.ToUnixTimeSeconds()` — CLOCK LIVES HERE in game/, never in sim). `Main` loads at startup (`_sim = new Simulation(balance, SaveManager.TryLoad());`), autosaves every 30 s (accumulate in `_Process`), and saves on `NotificationWMCloseRequest` (then quits).

- [ ] **Step 1: Implement** — `SaveManager.cs`:

```csharp
using System;
using BoneVoyage.Sim;
using Godot;

namespace BoneVoyage.Game;

public static class SaveManager
{
    public const string SavePath = "user://save.json";

    public static SaveData? TryLoad()
    {
        if (!FileAccess.FileExists(SavePath)) return null;
        try
        {
            return SaveCodec.Parse(FileAccess.GetFileAsString(SavePath));
        }
        catch (Exception e)
        {
            GD.PushWarning($"Save unreadable, starting fresh: {e.Message}");
            return null;
        }
    }

    public static void Save(SimState state)
    {
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        f.StoreString(SaveCodec.ToJson(state,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    }
}
```

`Main.cs`: field `private double _saveAcc;`; in `_Ready`: `_sim = new Simulation(BalanceData.Load(json), SaveManager.TryLoad());`; in `_Process` after the tick loop:

```csharp
        _saveAcc += delta;
        if (_saveAcc >= 30.0) { _saveAcc = 0; SaveManager.Save(_sim.State); }
```

and:

```csharp
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            SaveManager.Save(_sim.State);
            GetTree().Quit();
        }
    }
```

- [ ] **Step 2: Verify round-trip live** — run `timeout 45 godot --path game -- --shot=<scratch>/save-run1.png` (the --shot quit path also triggers close; if `--shot` quits via `GetTree().Quit()` directly, ALSO call `SaveManager.Save` there before quitting — check Main's shot branch and add it). Then run again with `--frames=60` and confirm console/HUD shows the restored bones (nonzero at startup): capture `save-run2.png`, READ it, bones label must be > 0 at ~1s in.
- [ ] **Step 3: Commit** — `git add -A && git commit -m "feat: save manager with autosave and save-on-quit"`

---

### Task 13: Playtest, balance pass, docs

**Files:**
- Modify: `README.md`, `CLAUDE.md` (commands if changed), possibly `game/balance.json` (tuning only)

- [ ] **Step 1: Full suite + build** — all tests green, 0 warnings.
- [ ] **Step 2: Balance sanity (temp harness, then delete)** — construct `Simulation` from shipped balance.json; script a "greedy buyer" (each tick: try purchasing the cheapest affordable upgrade) for 4 sim-hours (144000 ticks); print kingdom falls with timestamps + bones/gold curves at 30-min samples. Success shape: Meadowshire falls ≤ 20 min; Cannonspire falls ≤ 90 min; Knightholm falls ≤ 4 h; gold income becomes nonzero as soon as the first survivors reach the wall; every gear upgrade gets bought at least once by the greedy buyer (proves gold sink is reachable). If any target misses, make the smallest balance.json change (document old→new in the commit message `balance: ...`), rerun harness + tests.
- [ ] **Step 3: Visual playtest** — shots at default, `--frames=6000` (~100 s), and `--frames=30000` (~8 min with save from a prior long run so upgrades exist). READ each: five distinguishable unit types once hired (at minimum marchers visible), armory/shop/strip/HUD all legible, moat visible if the run reaches Cannonspire (long-run save makes this plausible; if not reached, note it — Task 10's rebuild path is code-reviewed and the sim tests cover kingdom advance).
- [ ] **Step 4: Update `README.md`** — add: gold/armory, conquest strip, saves (`user://save.json` — document the Linux path `~/.local/share/godot/app_userdata/Bone Voyage/save.json`), and the M2 command set (unchanged commands verified).
- [ ] **Step 5: Update repo `CLAUDE.md`** — add save path note + any new commands; keep under 60 lines.
- [ ] **Step 6: Commit** — `git add -A && git commit -m "docs: M2 playtest notes, README and CLAUDE.md updates"`
- [ ] **Step 7: Report** — summary with screenshots, balance timings, and the M3 backlog (offline progress + `_acc` clamp, tone pass incl. die_wave/die_salute differentiation + wall-bar styling + `KingdomFell` celebration animation, audio).

---

## Self-Review Notes

- **Spec coverage:** M2 = 5 types (T2/T7), gold/gear armory (T4/T9), 3 kingdoms (T1/T5/T10), conquest map with perks (T5/T11), versioned saves (T6/T12). Deferred M1 items all placed: validation T1, tightened movement + multi-tower tests T2/T5, enum-derived Hud counts T1, derived labels T9. `_acc` clamp explicitly out (M3, Global Constraints).
- **Type consistency check:** `KingdomFell(KingdomIndex, CycleTier, BonesJackpot, GoldJackpot)` consistent across T1 (definition), T5 (tests), T10 (view). `WallHpFor(kingdomIndex, cycleTier)` replaces M1's `WallHpForTier` everywhere. `EffSpeed/EffHp/EffWallDamage` defined T4, consumed T10 (`EffSpeed` only). `SaveData`/`SaveCodec` T6 ↔ T12. `ShopRow.Refresh/EffectLabel` T9 shared by both panels.
- **Known deliberate simplifications:** perk unlock is linear (`PerksUnlocked` count) because kingdom order is linear; accumulators not saved (sub-unit residue); Cannonspire/Knightholm visuals not directly screenshot-verified until a run reaches them (sim-tested; rebuild path code-reviewed); bomber's whack row doubles as its boom (no separate event needed).
