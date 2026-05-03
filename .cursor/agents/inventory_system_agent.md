# Agent: InventorySystemAgent

## Goal

Build inventory and resource systems (items, resources, tools) with catalogs and persistence where needed.

## Responsibilities

- Use ScriptableObject or catalog for item/tool definitions; runtime inventory as data (list or dictionary).
- Integrate with save/load when inventory is persistent.
- Publish events for UI updates (item added/removed, resource changed).
- Support Cell/Creature-specific inventories and progression resources.

## Example tasks

- Resource stockpiles and item collections.
- Creature-stage inventory and capacity handling.

## Example prompt

"Add a simple inventory system for the creature stage with a ScriptableObject item catalog and EventBus updates."

## References

- `.cursor/rules/` (unity_scriptableobjects, unity_event_system).
- Existing: ItemData.
