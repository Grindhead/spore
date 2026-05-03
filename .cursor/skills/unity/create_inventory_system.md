# Create an inventory or resource system

**Requirements:**

- Item/resource definitions in ScriptableObject or catalog; runtime state in data (list/dictionary).
- Publish events when inventory changes (item added/removed, resource updated) for UI and save.
- Integrate with SaveManager if inventory is persistent (serialize/deserialize).
- Follow `.cursor/rules/unity_scriptableobjects.mdc` and `unity_event_system.mdc`; see existing ResourceStockpile, ToolSystem, SpaceToolInventory.
