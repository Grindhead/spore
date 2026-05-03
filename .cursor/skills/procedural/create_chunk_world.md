# Create a chunk-based procedural world

**Requirements:**

- World divided into chunks; each chunk generated from world seed + chunk id (deterministic).
- Generate on demand or over multiple frames; do not generate full world in one frame.
- Separate generation logic from rendering (e.g. compute mesh/data then assign).
- Support LOD for distant chunks; follow `.cursor/rules/chunk_generation_rules.mdc`, `performance_rendering.mdc`.
- See ProceduralTerrain, GalaxyGenerator in codebase for patterns.
