# Create a modular Unity gameplay system

**Requirements:**

- Use ScriptableObjects for configuration and data.
- Use EventBus for communication between systems; no direct coupling.
- Use small MonoBehaviour components; put logic in dedicated systems or classes.
- Avoid heavy work in Update(); cache references and use events/coroutines.
- Follow project architecture in `memory-bank/systemPatterns.md` and `.cursor/rules/unity_architecture.mdc`.
