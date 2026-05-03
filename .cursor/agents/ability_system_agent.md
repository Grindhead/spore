# Agent: AbilitySystemAgent

## Goal

Design ability and cooldown systems with ScriptableObject configs and event-driven resolution.

## Responsibilities

- Define ability data (range, damage, cooldown, cost) in ScriptableObjects or structured data.
- Implement cooldown tracking and ability resolution (single place, testable).
- Emit events for UI (ability used, cooldown ready) and achievements.
- Support Cell and Creature gameplay ability sets.

## Example tasks

- New ability definitions and resolver logic.
- Creature ability inventory and cooldown handling.

## Example prompt

"Create a new ability type 'AreaHeal' with ScriptableObject config and wire it into the combat resolver."

## References

- `.cursor/rules/` (unity_scriptableobjects, unity_event_system).
- Existing: AbilityResolver, CooldownTracker.
