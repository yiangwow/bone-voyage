# M3 "Idle Polish" — Design Decisions

**Date:** 2026-07-22 · **Status:** Decided autonomously per standing "be bold" rule; flagged in session summary.
Parent spec: `2026-07-22-bone-voyage-design.md` (M3 milestone). Scope source: user M3 brief + M2 ledger deferred items.

## Offline progress
- `OfflineCalculator` (sim/): replays the exact deterministic ticks over elapsed wall-clock seconds. No approximation — spec's "exact, not estimated".
- **Cap: 24 h** (`CapSeconds = 86400`). ~864k ticks replay in well under a second for this sim size.
- Report dialog shows only when elapsed ≥ 60 s. Cheerful union copy; notes the cap when hit ("The union caps overtime at 24 hours. Rules are rules.").
- Offline events are aggregated into an `OfflineReport` and never reach the view (no 864k-event replay on screen).
- `Main._acc` catch-up clamped to 1 s. Mid-session OS sleep is detected by a wall-clock gap check (≥ 60 s) in `_Process` and routed through OfflineCalculator + the same dialog — this closes the deferred "double-pay after OS sleep" bug for both the startup and mid-session cases.
- No purchases happen offline (player is a shopkeeper; the march runs itself).

## Balance harness + tuning
- Harness is a committed console project `tools/balance/BoneVoyage.BalanceHarness` (net8.0, references sim, added to the solution). Greedy buyer: every sim-second it repeatedly buys the cheapest affordable upgrade. Outputs progression CSV + kingdom-fall timing summary.
- Tuning targets (greedy-buyer timings): K1 fall stays 4–7 min (early generosity is the hook), K2 15–25 min, K3 45–75 min, 4th fall (Meadowshire II) 2.5–5 h, 5–8 total falls at a 20 h horizon.
- Zero-stress invariants: base Marcher income untouched, bones strictly increasing, no wall regen, K1 pacing unchanged.
- Knobs: `balance.json` only — K2/K3 `baseWallHp`, `cycle.wallHpGrowth`, `cycle.dpsGrowth`, jackpots. Skeleton stats stay (they carry feel/art).

## Tone pass
- `die_wave` rebuilt as a loose two-arm goodbye with body rock; `die_salute` as a stiff heels-together salute with closed proud eyes and a hand glint — distinct silhouettes, verified via preview sheets + screenshots.
- New `die_sign` variant (row 4): skeleton hoists a "see you tomorrow!" picket sign (squiggle writing — 24 px), sign stays planted in the bone pile. Weighted ~20% of tower deaths.
- Respawn flavor: free-standing punch-clock prop by the necropolis gate that squashes ("dings") on spawns, rate-limited to ≤1 per 0.5 s.
- KingdomFell: confetti burst (CpuParticles2D, palette colors) at the wall + centered fading caption "The union takes {kingdom}!".
- Wall bar: stone/gold StyleBoxFlat + "Wall integrity" label. Conquest strip: dark-ink text (`403342`) on the light sky, gold + outline for the current kingdom; per-frame theme-override churn fixed with a dirty check.
- All HUD elements move from hardcoded 720px-edge positions to anchor-based layout.
- Moat slowdown becomes visible: actors query a view-side speed-mult delegate backed by `Simulation.MoatMult` for the zone under their feet (cosmetic; sim math untouched).

## Audio
- Generated-first, like sprites: `tools/audiogen/generate.py` (Python stdlib only) synthesizes chiptune-adjacent WAVs into `game/assets/audio/`: spawn ding, death chime, whack thud, kingdom-fall fanfare, and a 12 s seamless ambient pad (integer-cycle frequencies so the loop has no click).
- `AudioManager` (game/): pooled players, per-SFX rate limits (50 deaths in a tick play once), pitch jitter (cosmetic RNG, allowed in game/), ambient loop via `AudioStreamWav.LoopMode`.
- Volume settings persisted **in the save** per the brief: `SaveData` v2 adds an optional `SaveSettings(musicVolume, sfxVolume)` passthrough record (sim stays clock/RNG-free; it's inert data). v1 saves load with defaults (0.8/0.8). Minimal "Break room" settings popup (two sliders) — the full settings menu remains M4.

## Deferred fixes picked up (from M2 ledger)
- `SpeedMult` perk path test; multi-kingdom-overkill regression test (one hit chains ≥2 falls); `MoatBalance.SpeedMult` validation (must be in (0, 1]).

## Post-tuning note (Task 5 review, 2026-07-22)
Hitting the fall-4 band required `wallHpGrowth` 2000 (income compounds so hard that wall-break time is only weakly sensitive to this knob; `dpsGrowth` is the brittle lever — it stalls the march entirely past ~60). Consequence, independently verified: tier-2 Knightholm takes multiple days and tier-3 walls (growth², 4,000,000× base) are effectively unreachable. Acceptable for the shipped pre-prestige window (falls 1–5 in 20 h greedy); the deferred prestige system ("Rebuild the Necropolis", spec back-pocket) is now the designated valve and should land before any tier-3 content is surfaced to players.
