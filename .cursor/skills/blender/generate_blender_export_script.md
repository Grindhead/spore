# Generate a Blender export script for Unity

**Requirements:**

- Apply all transforms before export; scale 1.
- Export format: FBX; output path configurable (e.g. RoobsWorld/Assets/Art/Models).
- Naming: _VIS for visible mesh, _COL for collider; optional _LOD0/_LOD1/_LOD2.
- Triangulate meshes if required by pipeline.
- Follow `.cursor/rules/blender_export_rules.mdc`, `blender_asset_rules.mdc`. Optional: use Blender MCP when available (see .cursor/README-mcp-blender.md).
