# Create a procedural vegetation generator

**Requirements:**

- Deterministic seed; same area seed produces same vegetation placement.
- Use prefabs or procedural meshes from project (trees, grass, rocks); configurable density and types.
- Chunk or region-based; support LOD or culling for performance (GPU instancing where applicable).
- Follow `.cursor/rules/procedural_generation_rules.mdc`, `chunk_generation_rules.mdc`, `performance_rendering.mdc`.
