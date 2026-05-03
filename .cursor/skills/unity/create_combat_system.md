# Create a combat or damage resolution system

**Requirements:**

- Damage and resistance in data (ScriptableObject or config); single resolution path (e.g. DamageCalculator, AbilityResolver).
- Publish combat events (damage dealt, death, ability used) via EventBus for UI and achievements.
- Cooldowns and ability costs tracked per entity; testable logic with EditMode tests.
- Follow `.cursor/rules/unity_scriptableobjects.mdc`, `unity_event_system.mdc`; see CreatureCombatController, CivCombatResolver, SpaceCombatResolver.
