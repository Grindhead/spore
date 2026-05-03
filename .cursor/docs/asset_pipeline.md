# Asset Pipeline — Blender to Unity

Summary for AI context. **Detailed pipeline:** `memory-bank/systemPatterns.md` (Visual Asset Strategy / Future: Blender Pipeline).

## Flow

```
Blender → Export (FBX) → Unity importer → Prefab generation → Validation
```

## Naming Rules

- **Visible mesh:** suffix `_VIS`.
- **Collider mesh:** suffix `_COL` (Unity post-process can add MeshCollider).
- **LOD:** `_LOD0` (highest detail), `_LOD1`, `_LOD2`.

## Blender Export

- Apply all transforms; mesh scale 1.
- Format: FBX.
- Triangulate meshes before export if required.
- Output path: e.g. `RoobsWorld/Assets/Art/Models/`.

## Unity Import & Prefab

- AssetPostprocessor: set globalScale=1, import materials; optional: add MeshCollider to _COL children.
- Prefab creation: from imported FBX to `Assets/Prefabs/`; LODGroup from _LOD0/_LOD1/_LOD2.
- CreaturePartDef.ModelPrefab: assign generated prefabs when using Blender-sourced creature parts.

## Cursor Rules

- `.cursor/rules/blender_asset_rules.mdc`, `blender_export_rules.mdc`, `asset_pipeline_rules.mdc`, `import_processor_rules.mdc`, `prefab_generation_rules.mdc`.
- Blender MCP: `.cursor/README-mcp-blender.md` when using Cursor with Blender.
