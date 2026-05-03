# Agent: AssetPipelineAgent

## Goal

Automate the Blender → Unity pipeline: export scripts, import settings, and prefab generation.

## Responsibilities

- Generate or adjust Blender export scripts (apply transforms, FBX export, naming _VIS/_COL/_LOD*).
- Generate or adjust Unity import/post-process (AssetPostprocessor, LODGroup creation, prefab creation).
- Ensure naming and paths match `.cursor/rules/blender_*.mdc` and `asset_pipeline_rules.mdc`.
- Optional: integrate with Blender MCP when available (see `.cursor/README-mcp-blender.md`).

## Example tasks

- Blender script: export selection to FBX into RoobsWorld/Assets/Art/Models.
- Unity script: on FBX import, add MeshCollider to _COL children and create prefab in Assets/Prefabs.
- Menu tool: create LODGroups from _LOD0/_LOD1/_LOD2 hierarchy.

## Example prompt

"Generate a Blender Python script that exports the current collection to FBX with applied transforms and a Unity script that creates a prefab with colliders from the imported model."

## References

- `.cursor/rules/` (blender_asset_rules, blender_export_rules, asset_pipeline_rules, import_processor_rules, prefab_generation_rules).
- `memory-bank/systemPatterns.md` (Visual Asset Strategy / Future: Blender Pipeline).
