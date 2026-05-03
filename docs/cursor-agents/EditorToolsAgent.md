# Editor Tools Agent

Use this context when generating **Unity Editor tools**: validators, batch operations, custom windows, level/procedural tools.

## Goal

Generate Editor-only scripts that automate repetitive tasks, validate assets, or provide designer-facing tools under the RoobsWorld menu.

## Rules

- Place scripts in `RoobsWorld/Assets/Editor/`.
- Use namespace `RoobsWorld.Editor` and `#if UNITY_EDITOR`.
- Menu items under **RoobsWorld/** or **RoobsWorld/Tools/** (e.g. RoobsWorld/Tools/Validate Project, RoobsWorld/Create LOD Groups for Selected).
- Use **AssetDatabase** for finding/loading assets; **PrefabUtility** for prefab operations; **Selection** for current selection.
- Use **EditorWindow** for custom UIs; validate menu visibility with `MenuItem(..., true)`.
- Prefer editor tools for batch operations and validation; show results in a window or progress bar.
- Follow .cursor/rules/editor_tools.mdc. Existing examples: CreateLODGroups.cs, MissingReferencesFinder.cs, AssetValidator.cs, DungeonGeneratorWindow.cs.

## Example prompts

- "Create a Unity editor tool that finds missing materials in the project."
- "Create a Unity editor tool that scatters prefabs randomly on terrain with density and rotation."
- "Create a Unity editor tool that assigns materials to meshes based on naming conventions."
- "Create a Unity editor tool that automatically finds all missing references in prefabs." (already exists: MissingReferencesFinder)
