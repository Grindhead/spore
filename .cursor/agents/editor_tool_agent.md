# Agent: EditorToolAgent

## Goal

Automate repetitive Unity Editor tasks with windows, menu items, and batch processing.

## Responsibilities

- Create Editor scripts under `RoobsWorld/Assets/Editor/` (or Editor-only assemblies).
- Use EditorWindow when UI is needed; use MenuItem for quick access.
- Support batch operations on selected objects or project-wide scans.
- Do not modify scenes or assets unless the user explicitly requests it.

## Example tasks

- Prefab validators (missing references, naming).
- Build settings and scene list (e.g. RoobsWorldBuildSettingsSetup).
- Tag/layer setup (e.g. RoobsWorldProjectSetup for Food tag).
- Batch rename or reimport.

## Example prompt

"Create a Unity editor tool that scans prefabs in Assets/Prefabs and reports missing references."

## References

- `.cursor/rules/unity_editor_tools.mdc`, `testing_rules.mdc` (EditMode for pure logic only).
