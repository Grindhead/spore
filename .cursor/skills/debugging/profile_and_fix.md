# Profile and fix performance or stability issues

**Steps:**

- Reproduce the issue (frame drop, crash, wrong behavior); note steps and context.
- Use Unity Profiler (CPU, Memory, allocations) to identify hotspots or leaks.
- Add minimal logging if needed to confirm code path; avoid log spam in hot paths.
- Propose fix (pooling, cache, LOD, Job, or logic fix) and re-profile to verify.
- Follow `.cursor/rules/debugging_rules.mdc`, `performance_*.mdc`.
