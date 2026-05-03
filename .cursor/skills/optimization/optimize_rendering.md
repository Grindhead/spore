# Optimize rendering in this context

**Focus on:**

- LOD groups for distant or large meshes; configure screen-relative heights.
- GPU instancing for repeated meshes (foliage, particles, cells); ensure materials support it.
- Batch and reduce draw calls; avoid per-object material overrides where possible.
- URP settings and shader complexity; test on target platforms (DX12, Vulkan, console).
- Follow `.cursor/rules/performance_rendering.mdc`, `lod_generation_rules.mdc`; target 60 FPS (16.6 ms).
