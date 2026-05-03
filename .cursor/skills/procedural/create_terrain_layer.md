# Add a terrain layer or biome layer to the terrain generator

**Requirements:**

- Deterministic: seed + chunk/position → same result.
- Configurable (noise scale, blend, thresholds) via ScriptableObject or inspector.
- Integrate with existing ProceduralTerrain or chunk system; separate generation from rendering.
- Follow `.cursor/rules/procedural_generation_rules.mdc`, `chunk_generation_rules.mdc`; see World/ scripts.
