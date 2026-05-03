# Create a procedural prop scatter system

**Requirements:**

- Deterministic: seed + region/chunk id → same placement.
- Configurable: density, prefab list, scale/rotation variance via ScriptableObject or inspector.
- Use Poisson disk or similar to avoid overlap; optional LOD (e.g. billboard at distance).
- Integrate with chunk system if world is chunked; do not scatter entire world at once.
- Follow `.cursor/rules/procedural_generation_rules.mdc`; consider performance_rendering (instancing).
