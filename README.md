# Bone Voyage

A zero-stress reverse tower defense: send cheerful union-worker skeletons at
kingdoms that can never hurt you. Every death pays. Every wall falls. Eventually.

Five job titles on the roster — Marchers, Shieldbearers, Sprinters, Bombers,
Bannerbones — each hired with bones earned at the wall. Bones also buy Death
Benefits (better bounties). Every skeleton who reaches the wall also earns
gold, spent at the Royal Armory on gear upgrades (Helmets, Battering Rams,
Marching Boots, War Banners) that boost the whole crew rather than one hire.

Three kingdoms — Meadowshire, Cannonspire, Knightholm — sit on a conquest
strip at the top of the screen. Falling a kingdom unlocks its permanent perk
(bounty/speed/gold multiplier) for every kingdom after it, then the tour
loops back around with tougher walls and bigger jackpots each lap.

## Requirements
- .NET 8+ SDK
- Godot 4.4.1-stable (.NET edition) — editor on Windows, headless CLI on Linux
- Python 3 + Pillow (sprite generation only)

## Saves
Progress autosaves every 30s and on quit to `user://save.json` — on Linux
that's `~/.local/share/godot/app_userdata/Bone Voyage/save.json`. Delete it
to start a fresh run.

## Commands
- `dotnet test BoneVoyage.sln` — sim test suite
- `godot --path game` — run the game
- `godot --path game -- --shot=/tmp/s.png` — screenshot at ~5s (300 frames) then quit (also saves)
- `godot --path game -- --shot=/tmp/s.png --frames=N` — screenshot after N frames then quit (also saves)
- `godot --path game -- --kingdom=N` — dev flag, force-start on kingdom N (0-indexed) for manual verification
- `python3 tools/spritegen/generate.py` — regenerate all pixel art

## Layout
See CLAUDE.md. Short version: `sim/` is the game (pure C#, tested); `game/` is the view; `tools/spritegen/` is the art department.

## Pacing (M3)
Balance tuned so the first lap stays quick and every lap after it stretches
out — greedy-buyer harness (`tools/balance/BoneVoyage.BalanceHarness`,
`--hours 20`), measured against `game/balance.json`:

| Milestone | Time |
|---|---|
| Meadowshire falls (K1, tier 1) | 4.6 min |
| Cannonspire falls (K2, tier 1) | 16.4 min |
| Knightholm falls (K3, tier 1) | 46.0 min |
| Meadowshire falls again (tier 2, "fall 4") | 183.3 min (~3.1 h) |
| Cannonspire falls again (tier 2, "fall 5") | 1103.0 min (~18.4 h) |
| Total falls in a 20h idle run | 5 |

Tuned via `kingdoms[1].baseWallHp` (Cannonspire), `kingdoms[2].baseWallHp`
(Knightholm), and `cycle.wallHpGrowth` / `cycle.dpsGrowth` (per-lap wall and
tower scaling). K1's wall and all Bones-currency upgrade costs are untouched,
so the opening minutes feel exactly like before — the stretch only shows up
once a kingdom is revisited on lap two.
