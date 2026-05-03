# Reduce allocations in this code

**Focus on:**

- Remove `new` in Update/FixedUpdate or per-frame methods; use pooled or cached collections.
- Replace string concatenation in hot paths with StringBuilder or preformatted strings.
- Use PoolManager for frequently instantiated/destroyed objects (spawn/despawn).
- Avoid boxing (e.g. enum in dictionary); use structs or typed collections.
- Follow `.cursor/rules/performance_memory.mdc`; verify with Profiler (Deep Profile, Memory).
