# System Patterns - RoobsWorld

## High-Level Architecture

Top-level flow is intentionally limited to:

- Main Menu
- Cell Stage
- Creature Stage

The stage system must reject unsupported stage requests and normalize to Cell when needed.

## Stage Runtime Pattern

- `GameStageType` includes `None`, `Cell`, and `Creature`.
- `StageManager` owns stage registration, transitions, and per-frame dispatch.
- `IGameStage` is the lifecycle contract used by stage implementations.
- Supported stage implementations:
  - `RoobsWorld.Stages.Cell.CellStage`
  - `RoobsWorld.Stages.Creature.CreatureStage`

## Core Technical Patterns

- Singleton managers for global systems (`GameManager`, `StageManager`, `InputManager`, etc.).
- Event-driven cross-system communication via `EventBus`.
- Procedural creature pipeline:
  - DNA model
  - part catalog
  - stat calculation
  - runtime factory spawn
- Separation between core systems (`Assets/Scripts/Core`) and gameplay systems (`Assets/Scripts/Stages`, `Assets/Scripts/Creature`).

## Folder Intent

- `Assets/Scripts/Core` - app-level systems and stage orchestration.
- `Assets/Scripts/Stages/Cell` - cell stage gameplay loop.
- `Assets/Scripts/Stages/Creature` - creature stage runtime behavior.
- `Assets/Scripts/Creature` - shared creature generation/editor systems.
- `Assets/Scripts/UI` - reusable and stage-specific UI.

## Scope Rule

Any design or implementation that introduces stage behavior beyond Cell and Creature is out of scope.
