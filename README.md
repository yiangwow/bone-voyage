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
