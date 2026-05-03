# Create a procedural dungeon generator

**Requirements:**

- Deterministic: same seed produces same layout; accept seed parameter.
- Configurable parameters (room count, size, corridor width, etc.) via ScriptableObject or inspector.
- Chunk or room-based; do not generate entire dungeon in one frame if large.
- Prefab or procedural placement for rooms/corridors; follow naming and folder structure.
- Follow `.cursor/rules/level_generation_rules.mdc`, `procedural_generation_rules.mdc`; add EditMode tests for determinism.
