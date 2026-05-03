# Create a procedural level or content generation tool

**Requirements:**

- Deterministic seed support; same seed → same output.
- Editor tool interface (window or menu) with configurable parameters.
- Configurable via ScriptableObject or inspector (e.g. density, size, prefab lists).
- Do not generate entire world in one frame; use chunks or incremental generation.
- Follow `.cursor/rules/procedural_generation_rules.mdc`, `level_generation_rules.mdc`; add EditMode tests for determinism.
