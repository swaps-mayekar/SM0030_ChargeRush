# ChargeRush

Original casual landscape mobile prototype: run a device-charging service at public events.

## Requirements

- Unity **6000.3.0f1**
- macOS / Editor play supported (mouse)
- Target platform: iOS landscape (iPhone + tablet family)

## How to run

1. Open this folder in Unity Hub with editor `6000.3.0f1`.
2. Open **Assets/Scenes/Boot.unity** (Boot is first in Build Settings).
3. Press Play.
4. Main Menu → **Play** starts the next unlocked story level (Level 1 tutorial first).

### Art / content tools

- **ChargeRush → Apply Final Art** — wires production sprites into data, prefabs, and scenes
- **ChargeRush → Build All Content** — regenerates data/scenes (preserves final art files)
- **ChargeRush → Validate Content** — validates IDs, thresholds, and references

## Scenes

- `Boot` – persistent services (save, input, audio, catalog, scene loader)
- `MainMenu` – logo, play, modes, upgrades, navigation
- `LevelSelect` – 10 story levels with lock/star state
- `Gameplay` – data-driven level session with event backgrounds
- `Achievements`
- `Settings`

## Art direction

Production art lives under `Assets/Art/`:

- Characters (6 guest types)
- Devices (7 categories + charged glow)
- Environment (counter, ports, event backgrounds)
- UI (logo, panels, buttons, stars, credits, mistake icon)
- Connectors (fictional PowerLink / MiniPower / ProPower / UniversalPower badges)

## Playable features

- Drag-and-drop charging loop (take → match connector → charge → return to owner → pay)
- Customer queue, patience, mistakes (3), stars, tutorial Level 1
- 7 fictional devices / 5 connector families / 6 customer types
- 10 story levels, challenge mode, endless mode
- Upgrades, achievements, career ranks
- Versioned JSON save system

## Tests

EditMode and PlayMode assemblies under `Assets/Tests`.

## Notes

- Audio still uses cue hooks / placeholders until final SFX/music are authored
- App Store signing remains a production follow-up
- No real device brand or proprietary connector names are used in authored content
