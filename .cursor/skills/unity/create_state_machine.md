# Create a state machine (e.g. stage FSM or entity FSM)

**Requirements:**

- Define states and transitions clearly; one-way or explicit transition table.
- Each state can own its own scene, camera, input mapping, or UI as needed (see StageManager).
- Use EventBus for transition triggers; avoid tight coupling between states.
- Keep state logic testable; use interfaces (e.g. IGameStage) for test doubles.
- Follow `memory-bank/systemPatterns.md` (Stage State Machine) and `.cursor/rules/unity_event_system.mdc`.
