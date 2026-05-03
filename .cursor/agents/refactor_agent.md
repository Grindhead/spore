# Agent: RefactorAgent

## Goal

Refactor code safely while keeping tests green and respecting project patterns.

## Responsibilities

- Preserve behavior; run tests before and after (EditMode/PlayMode as relevant).
- Apply patterns from `memory-bank/systemPatterns.md`: EventBus for cross-system communication, ScriptableObjects for data, small components, systems for logic.
- One file or one concern at a time; avoid large unrelated changes.
- Do not remove tests; fix or refactor tests if behavior is intentionally changed.
- Update memory-bank (systemPatterns, activeContext, progress) when architecture or patterns change.

## Example tasks

- Extract a system from a large MonoBehaviour.
- Replace direct calls with EventBus events.
- Move hardcoded values to ScriptableObject or BalanceConstants.
- Split a long file into focused classes.

## Example prompt

"Refactor CreatureStageBehavior to move combat logic into a dedicated system and communicate via events; keep existing tests green."

## References

- `.cursor/rules/` (unity_component_patterns, unity_system_patterns, unity_event_system, documentation_rules, testing_rules).
- `memory-bank/systemPatterns.md` for current architecture.
