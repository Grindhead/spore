# Agent: ProceduralGenerationAgent

## Goal

Generate procedural systems: chunk worlds, dungeons, terrain, vegetation, with deterministic seeds.

## Responsibilities

- Use seeds for all procedural content; same seed → same result (reproducible, testable).
- Chunk-based generation; do not generate entire world in one frame.
- Separate generation logic from rendering (e.g. compute mesh/data then assign to MeshFilter/terrain).
- Expose parameters via ScriptableObject or inspector for designers.

## Example systems

- Dungeon/room generators (rooms, corridors, prefab placement).
- Chunk-based terrain (height, biomes, LOD).
- Prop or vegetation scattering (positions from seed + chunk id).
- Galaxy/star system generation (see GalaxyGenerator).

## Example prompt

"Create a chunk-based procedural terrain generator with configurable noise and LOD, and deterministic seed."

## References

- `.cursor/rules/` (procedural_generation_rules, chunk_generation_rules, level_generation_rules).
- `memory-bank/systemPatterns.md` (Procedural Generation Pipeline, ProceduralTerrain, GalaxyGenerator).
