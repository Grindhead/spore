# Agent: BlenderPipelineAgent

## Goal

Handle Blender-side pipeline: transforms, LOD generation, FBX export, and naming for Unity.

## Responsibilities

- Scripts that apply transforms, set scale to 1, and export FBX.
- Generate LOD meshes (e.g. decimate) and name with _LOD0, _LOD1, _LOD2.
- Enforce naming: _VIS (visible), _COL (collider).
- Output to a path that Unity can import (e.g. RoobsWorld/Assets/Art/Models).

## Example tasks

- Apply-all-transforms and export selected to FBX.
- Duplicate and decimate for LODs; name and export.
- Batch export multiple blends with consistent settings.

## Example prompt

"Create a Blender script that applies transforms, generates two LOD levels by decimation, and exports each as FBX with _LOD0/_LOD1 naming."

## References

- `.cursor/rules/blender_asset_rules.mdc`, `blender_export_rules.mdc`, `lod_generation_rules.mdc`.
- Blender MCP: `.cursor/README-mcp-blender.md` (when using Cursor with Blender MCP).
