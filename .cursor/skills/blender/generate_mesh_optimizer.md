# Generate a Blender mesh optimization script

**Requirements:**

- Reduce polygon count (decimate, remove doubles, limit dissolve) for performance.
- Preserve UVs and normals where needed for materials and lighting.
- Optional: generate _LOD1/_LOD2 variants; name per `blender_asset_rules.mdc`.
- Apply transforms before export; output for Unity import.
