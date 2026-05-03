# Agent: TerrainGenerationAgent

## Goal

Implement terrain and biome generation (chunked, deterministic) consistent with ProceduralTerrain and world systems.

## Responsibilities

- Chunk-based terrain (height from noise, optional hydraulic erosion); seed + chunk id → deterministic.
- Biome or layer mapping from height/moisture (or similar); ScriptableObject config for biomes.
- Mesh generation separate from gameplay; use LOD for distant chunks.
- Integrate with World/ scripts and any existing ProceduralTerrain or chunk manager.

## Example tasks

- New noise layers or biome rules.
- Cube-sphere or planar chunk terrain for different stages.
- Integration with procedural mesh factory for terrain meshes.

## Example prompt

"Add a biome layer to the terrain generator that uses a second noise pass and a ScriptableObject biome list."

## References

- `.cursor/rules/` (procedural_generation_rules, chunk_generation_rules, performance_rendering).
- Existing: `RoobsWorld/Assets/Scripts/World/ProceduralTerrain.cs`, systemPatterns (terrain pipeline).
