# Create or extend the save/load system

**Requirements:**

- Serialize to JSON under PersistentDataPath (or project-defined path); include version if format may change.
- SaveData should include all persistent state (stage, creature DNA, world seed, DNA points, achievements, etc.) per `memory-bank/systemPatterns.md`.
- Use SaveManager as the single entry point; avoid duplicate save logic.
- Do not serialize non-persistent or engine references; store IDs or keys and resolve on load.
- Follow `.cursor/rules/unity_scriptableobjects.mdc` and existing SaveManager, GameManager.
