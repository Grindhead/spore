# Agent: PrefabGeneratorAgent

## Goal

Generate prefabs from imported models with correct colliders, LOD groups, and placement in the project.

## Responsibilities

- Create prefabs from FBX (or other models) under Assets/Prefabs/.
- Add MeshCollider to objects named with _COL; optionally configure LODGroup from _LOD0/_LOD1/_LOD2.
- Support CreaturePartDef.ModelPrefab assignment when generating creature part prefabs.
- Do not overwrite existing prefabs without user intent; support "create new" or "update" modes.

## Example tasks

- Post-process on import: create prefab from model, add colliders, save to Prefabs.
- Editor menu: "Create LODGroups from selection" for _LOD* hierarchy.
- Batch prefab creation from a folder of models.

## Example prompt

"Create a Unity editor script that, when run on a selected imported FBX root, creates a prefab in Assets/Prefabs with MeshColliders on _COL children and an LODGroup from _LOD0/_LOD1 meshes."

## References

- `.cursor/rules/` (prefab_generation_rules, collider_rules, lod_generation_rules, import_processor_rules).
