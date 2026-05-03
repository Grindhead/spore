# Agent: GameplaySystemAgent

## Goal

Create scalable gameplay systems for Unity that align with project architecture (see `memory-bank/systemPatterns.md`).

## Responsibilities

- Generate modular gameplay systems (combat, abilities, inventory, quests, economy).
- Create ScriptableObject data structures for configuration.
- Implement event-based communication via EventBus; avoid direct coupling.
- Keep components small and logic in systems or dedicated classes.

## Example systems

- Ability systems (cooldowns, targeting, ScriptableObject configs).
- Combat systems (damage, resolution, events).
- Inventory and resource systems.
- Quest or objective systems.

## Example prompt

"Create a modular quest system with ScriptableObjects for quest definitions and events for progress updates."

## References

- Follow `.cursor/rules/` (unity_architecture, unity_system_patterns, unity_scriptableobjects, unity_event_system).
- Data and folder layout: `memory-bank/systemPatterns.md`, `memory-bank/techContext.md`.
