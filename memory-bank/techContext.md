# Tech Context - RoobsWorld

## Stack

- Unity (C#)
- Runtime-generated gameplay and UI systems
- EditMode test coverage for core systems and gameplay logic

## Architecture Notes

- Stage orchestration is centralized in `StageManager`.
- Stage lifecycle uses `IGameStage` (`OnEnter`, `OnExit`, `OnUpdate`).
- Current supported gameplay stages are Cell and Creature only.
- Shared systems (input, save, audio, camera, UI) live in `Assets/Scripts/Core`.

## Performance Focus Areas

- Cell stage entity counts and frequent interactions.
- Creature generation/runtime mesh usage.
- Event-driven updates to avoid unnecessary per-frame work.
- Object reuse via pooled patterns where practical.

## Testing Focus Areas

- Stage transition behavior between Cell and Creature.
- Save/load integrity for stage enum and progression data.
- Tutorial and UI behavior under both supported stages.
- Creature spawn and stat initialization sanity checks.

## Scope Constraints

Do not add or maintain technical pathways for stages beyond Cell and Creature.
