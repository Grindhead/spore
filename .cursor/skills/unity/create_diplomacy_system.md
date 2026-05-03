# Create or extend a diplomacy/social system

**Requirements:**

- States (e.g. Hostile, Neutral, Friendly) and transitions driven by actions (gift, help, attack); data in ScriptableObject or config.
- Publish diplomacy/social events for UI and achievements; use EventBus.
- Keep implementation scoped to Cell/Creature gameplay systems.
- Follow `.cursor/rules/unity_event_system.mdc`, `unity_scriptableobjects.mdc`.
