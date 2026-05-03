# Asset Pipeline Agent

Use this context when working on **Blender → Unity** asset pipeline: export scripts, import settings, prefab generation, LOD.

## Goal

Generate or extend Blender Python scripts and Unity Editor/import scripts so that assets flow from Blender into Unity with consistent settings, optional prefabs, and colliders (e.g. _COL naming).

## Rules

- **Blender**: Scripts live in `tools/blender_scripts/`. Export path: `RoobsWorld/Assets/Art/Models` (resolve from script dir: repo root = `../..`, then `RoobsWorld/Assets/Art/Models`).
- **Naming**: `*_VIS` = visible mesh, `*_COL` = collider mesh; Unity post-processor adds MeshCollider to _COL and can create prefabs.
- **Unity**: Editor scripts in `RoobsWorld/Assets/Editor/`; use AssetPostprocessor for OnPreprocessModel / OnPostprocessModel; PrefabUtility for prefabs.
- **LOD**: Blender can generate _LOD1/_LOD2 via Decimate; Unity has CreateLODGroups (RoobsWorld menu) for _LOD0/_LOD1 naming.
- Follow .cursor/rules/asset_pipeline.mdc and editor_tools.mdc.

## Example prompts

- "Create a Blender script that exports the Export collection and a Unity editor script that creates prefabs from imported models."
- "Create a Blender script that exports all meshes in the Export collection as FBX into Unity's Assets/Art/Models folder."
- "Extend the Unity model importer to apply scale and generate LOD groups for _LOD* meshes."
