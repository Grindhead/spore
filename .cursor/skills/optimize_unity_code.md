# Optimize this Unity code

**Focus on:**

- Reducing allocations (pooling, cached buffers, no new in Update).
- Caching component and transform references.
- Improving frame time and memory; use Profiler to verify.
- Follow `.cursor/rules/performance_memory.mdc`, `performance_rendering.mdc`, `performance_physics.mdc` and `memory-bank/techContext.md`.
