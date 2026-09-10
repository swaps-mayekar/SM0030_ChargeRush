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

### Rebuild generated content (optional)

Menu: **ChargeRush → Build All Content**  
Validate: **ChargeRush → Validate Content**

## Scenes

- `Boot` – persistent services (save, input, audio, catalog, scene loader)
- `MainMenu` – play, modes, upgrades, navigation
- `LevelSelect` – 10 story levels with lock/star state
- `Gameplay` – data-driven level session
- `Achievements`
- `Settings`

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

- Visuals/audio are original geometric placeholders intended for production replacement.
- No real device brand or proprietary connector names are used in authored content.
- App Store signing / final icons / final music are production follow-ups.
