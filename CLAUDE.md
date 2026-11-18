# CLAUDE.md — Bone Voyage

Zero-stress reverse tower defense (idle). Spec: docs/superpowers/specs/2026-07-22-bone-voyage-design.md

## Architecture (load-bearing rule)
- `sim/BoneVoyage.Sim` — ALL gameplay math. Pure C#, no Godot. Deterministic (no RNG/clock). xUnit-tested.
- `game/` — Godot 4.4.1 .NET view layer. Reads sim state, consumes sim events. Never computes gameplay.
- `game/balance.json` — the only home of tuning numbers.
- `tools/spritegen/` — Python/Pillow pixel-art generator; sprites are code, regenerate with `python3 tools/spritegen/generate.py`.

## Commands
- Tests: `dotnet test BoneVoyage.sln`
- Run game: `godot --path game` (Linux .NET build at ~/.local/bin/godot; Windows editor must be 4.4.1 .NET)
- Screenshot verify: `godot --path game -- --shot=/tmp/shot.png` (default: 300 frames, ~5s). Add `--frames=N` for a later capture, e.g. `--frames=5400` for ~90s. Also autosaves — delete the save file (below) between runs that must start fresh.
- Dev flag `--kingdom=N` forces a starting kingdom (0-indexed) for manual/screenshot verification; not in the shipped UI.

## Saves
`user://save.json` — on Linux: `~/.local/share/godot/app_userdata/Bone Voyage/save.json`. Autosaves every 30s and on quit. Versioned via `SaveCodec` (sim); unreadable/future-versioned saves start fresh with a warning.

## Rules
- Git author: the owner's git identity, no Co-Authored-By.
- UI strings via Tr(); cheerful-union-worker tone in all copy.
