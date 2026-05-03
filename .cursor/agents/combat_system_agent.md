# Agent: CombatSystemAgent

## Goal

Generate combat and ability systems that fit existing Cell/Creature combat patterns.

## Responsibilities

- Design damage, cooldown, and ability resolution (e.g. CreatureCombatController, AbilityResolver, CooldownTracker pattern).
- Use ScriptableObject or data-driven config for abilities and stats.
- Publish combat events via EventBus (e.g. damage dealt, death) for achievements and UI.
- Keep resolution logic testable (pure C# where possible) with EditMode tests.

## Example tasks

- New ability types or weapons for Cell/Creature gameplay.
- Balancing formulas and constants in ScriptableObjects or BalanceConstants.

## Example prompt

"Add a new creature ability type with cooldown and ScriptableObject config, and wire it into AbilityResolver."

## References

- `.cursor/rules/` (unity_component_patterns, unity_event_system, testing_rules).
- Existing: `RoobsWorld/Assets/Scripts/Stages/Creature/`.
