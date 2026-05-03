# Extend save data with new persistent fields

**Requirements:**

- Add new fields to SaveData (or equivalent) and serialize/deserialize in SaveManager.
- Preserve backward compatibility: missing fields in old saves get defaults; optional version field for format changes.
- Update GameManager or load flow to apply loaded data to runtime state; publish events if UI or systems depend on loaded state.
- Do not store non-serializable or engine references; use IDs or keys and resolve on load.
- Follow `.cursor/rules/unity_scriptableobjects.mdc` and existing SaveManager; update memory-bank if save format is documented there.
