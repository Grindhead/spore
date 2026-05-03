# Optimize this Unity code

**Focus on:**

- Reducing allocations in Update and hot paths (reuse buffers, avoid new, use pooling).
- Caching component and transform references; no GetComponent or Find in Update.
- Moving heavy work to Jobs/Burst or incremental updates where appropriate.
- Adding or using LOD for large meshes; GPU instancing for repeated geometry.
- Frame budget 16.6 ms; profile before and after. Follow `.cursor/rules/performance_*.mdc` and `memory-bank/techContext.md`.
