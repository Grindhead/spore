# Create an ability or cooldown system

**Requirements:**

- Ability data (range, damage, cooldown, cost) in ScriptableObject or structured config.
- Single resolution path (e.g. AbilityResolver) that is testable; add EditMode tests.
- Cooldown tracking per ability/per entity; expose events for UI (ability used, cooldown ready).
- Publish combat/ability events via EventBus for achievements and feedback.
- Follow `.cursor/rules/unity_scriptableobjects.mdc`, `unity_event_system.mdc`; see AbilityResolver, CooldownTracker.
