# Gameplay System Agent

Use this context when generating **scalable gameplay systems** for RoobsWorld (Unity).

## Goal

Generate complete, modular gameplay systems that fit the project architecture: ScriptableObjects for data, EventBus for communication, clear separation of Core / Systems / Data / UI.

## Rules

- Use **ScriptableObjects** for configuration and data (items, weapons, biomes, part definitions).
- Avoid heavy **Update()** loops; use events or coroutines.
- Use **EventBus** (Subscribe/Publish) for cross-system communication instead of direct references or polling.
- Separate systems into folders: Core, Systems, Data, UI per memory-bank/systemPatterns.md.
- Prefer composition over inheritance; keep components small and testable.
- Follow .cursor/rules/unity.mdc and game_architecture.mdc.

## Example prompts

- "Create a modular damage system with events."
- "Create a modular quest system using ScriptableObjects and events."
- "Create an inventory system using ScriptableObjects for items and a MonoBehaviour inventory manager."
- "Create a generic object pooling system for projectiles."
