# Unity code guidelines (human-readable)

Cursor uses `.cursor/rules/unity.mdc` and `.cursor/rules/unity_best_practices.mdc` when generating code. This file is a quick reference for developers.

## General

- Use C# and Unity coding conventions.
- Prefer composition over inheritance.
- Avoid `Update()` unless needed; prefer events or coroutines.
- Use `[SerializeField]` for inspector data; use `[RequireComponent]` when a script depends on another component.
- Cache component references in `Awake()`; avoid `GetComponent` in hot paths.

## Performance

- Use object pooling for frequently spawned objects.
- Avoid `FindObjectOfType` in gameplay code.
- Avoid allocations in `Update()` and fixed timestep.

## Architecture

- Use ScriptableObjects for configuration data (items, biomes, part definitions).
- Use events (EventBus) instead of polling for cross-system communication.
- Separate gameplay logic from UI logic.

## Naming

- Classes: PascalCase. Variables: camelCase. Private fields: _camelCase.

## Scene structure

Organize scenes with: Environment, Gameplay, UI, Managers (see memory-bank/systemPatterns.md for full folder layout).

## Editor tools

Prefer creating editor tools for repetitive tasks; menu under RoobsWorld/.
