# Bone Voyage — Design Spec

**Date:** 2026-07-22
**Status:** Approved pending user review
**Working title:** Bone Voyage (subject to change)

## Concept

A zero-stress incremental idle game disguised as a *reverse* tower defense, inspired by the structure of *Zero Stress King: Idle Defense* but role-flipped: you are an immortal necromancer marching endless cheerful skeletons at kingdoms that cannot ever harm you. There is no failure state. Every outcome pays you.

**Tone:** Cheerful union workers. Skeletons treat dying as their job — they wave, salute, and hold tiny "see you tomorrow!" signs as they crumble, then rejoin the respawn queue at the necropolis punch-clock. Dark-comedy warmth; UI copy plays it straight ("47 employees recycled this shift"). Death animations are the content.

## Core Loop

1. Skeletons auto-spawn at the Necropolis and march across the field toward the current kingdom.
2. Kingdom defenses (arrow towers, cannons, knight sorties, moats) kill skeletons along the road.
3. Every dead skeleton is **recycled**: it pays a bone bounty (type-dependent) multiplied by your recovery upgrades. Spawning itself is free, rate-limited by spawn-rate upgrades.
4. Survivors reach the wall and deal damage; **every point of wall damage pays gold**.
5. Wall falls → kingdom conquered → large payout + permanent perk + next (tougher) kingdom.

**Zero-stress guarantee:** kingdoms never counter-attack the necropolis. The worst a kingdom can do is fall slowly. Both currencies always flow regardless of how the march goes.

## Economy — Two Currencies, Two Shops

| Currency | Source | Buys (theme) | Examples |
|---|---|---|---|
| **Bones** | Your own dead (recycling) | **Quantity** | Spawn rate, new skeleton types, necropolis capacity, recovery % (graverobbers, return-to-sender enchantments, bone compost) |
| **Gold** | Damage dealt to kingdoms | **Quality** | Gear: helmets, shields, battering rams, banners — stat upgrades on skeletons |

Strategic texture (gentle, optional): a cheap meat-grinder legion dies fast and farms bones; a durable armored legion reaches walls and farms gold. Both are always profitable; the player tunes the mix.

## Skeleton Roster (v1: 5 types)

- **Marcher** — basic unit.
- **Shieldbearer** — tanky; soaks tower shots so others get through.
- **Sprinter** — fast; slips past defenses.
- **Bomber** — explodes on the wall for huge damage; pays bones *and* gold. The union's most enthusiastic member.
- **Bannerbones** — support; buffs nearby marchers.

## World Structure — Conquest Map

A world map of discrete kingdoms, besieged one at a time. Each conquered node grants a **permanent perk** — the map IS the skill tree, and painting it is the long-term goal. Kingdoms have personality and distinct defense mixes (arrow-heavy, cannon-heavy, sortie-heavy, moats that slow the march), favoring different rosters — variety without difficulty.

**Deferred (post-v1 back pocket):** prestige resets ("Rebuild the Necropolis" converting glory into permanent bloodline bonuses).

## Player Role

**Pure shopkeeper.** The player only buys: skeleton types, spawn rate, recovery upgrades, gear. The march runs itself. Relaxation comes from watching, not doing. Generous **offline progress** — the march continues while the game is closed; the analytic sim makes it exact, not estimated. Zero stress includes zero FOMO.

## Architecture

**Engine:** Godot 4.x with **C#** (.NET).

### Layer 1: `Sim/` — pure C#, zero Godot dependencies

- `LegionState`, `KingdomState`, `Economy`, `TickEngine` (fixed-tick, deterministic), `OfflineCalculator` (runs the same ticks over elapsed wall-clock time), `Balance` (all tuning numbers in data files).
- Emits typed events consumed by the view: `SkeletonsDied(type, count, zone)`, `WallDamaged`, `KingdomFell`.
- Fully unit-tested (xUnit). Views never compute gameplay.

### Layer 2: `Game/` — Godot scenes

- Battlefield view, Necropolis shop (bones), Armory (gold), Conquest map, HUD.
- Views only read sim state and consume sim events.

### Individual deaths, aggregated math (core presentation rule)

The sim computes aggregate rates; the battlefield renders **individual skeleton actors**. When the sim reports "3 marchers died at the cannon zone this tick," three specific on-screen skeletons get hit, wave goodbye, and crumble — each an individual moment. Early game is literally 1:1. Late game (e.g. 50,000 marchers) the screen shows a capped cast of representatives (~100–200 actors) whose deaths are sampled from true rates while counters show real numbers. Individuality of deaths is never lost; math never depends on rendering.

### Saves

Versioned JSON snapshot of sim state + timestamp; save on interval and on quit. Steam Cloud via autocloud. Deterministic sim ⇒ save + elapsed time fully reconstructs offline progress.

### Steam

GodotSteam (GDExtension) for achievements, cloud, overlay — isolated behind a small interface so the game runs without Steam during dev.

### Localization

All strings through Godot translation keys from day one; English source CSV.

### Testing & balancing

- xUnit on the sim core: economy math, offline calc, kingdom-fall conditions.
- Headless balance harness: simulate ~20 hours of play in seconds, output progression curves as CSV; pacing tuned from graphs.

## Art

**Pixel art, cute and colorful. Generated-first, packs-second:**

- Custom units (walk cycles, **death variants**: wave, salute, sign-holding, confetti-crumble) are authored as code — a sprite-generator pipeline (Python/PIL) in the repo outputs palette-locked PNG sprite sheets (16×16–32×32). Art is versioned, editable, regenerable; iterated via screenshot verification.
- CC0 asset packs (Kenney, itch.io) fill environment, tiles, and UI where generic is fine.
- A commissioned polish pass before Steam release is an option, not a dependency.

Death animations get the largest art budget — they carry the tone.

## Milestones

- **M1 — First playable:** one kingdom, Marcher + Shieldbearer, bones economy, battlefield with individual deaths driven by the aggregate sim, basic shop. Proves the core feel.
- **M2 — Full game shape:** all 5 skeleton types, gold/gear armory, 3 kingdoms, conquest map with perks, versioned saves.
- **M3 — Idle polish:** offline progress, balance harness + tuning pass, tone pass (signs, UI copy, respawn punch-clock), audio.
- **M4 — Steam-ready:** GodotSteam achievements + cloud, localization keys throughout, settings menu, trailer-able build.

Each milestone gets its own implementation plan and PR flow (feature branches, yiangwow authorship).

## Practical Environment Notes

- Godot editor runs on the Windows side (WSLg GPU/audio unreliable for game dev); repo accessible from both sides; CLI/agent work from WSL.
- Repo: `~/bone-voyage`, to be pushed as `yiangwow/bone-voyage`. Repo must carry its own CLAUDE.md (per-project rule).
