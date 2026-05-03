# Create or integrate an object pool

**Requirements:**

- Use the project PoolManager for prefab-based pooling; support prewarm, spawn, despawn.
- Pool frequently spawned/despawned objects (cells, projectiles, VFX, etc.); avoid Instantiate/Destroy in hot paths.
- Reset state on spawn and clear on despawn so pooled objects are safe to reuse.
- Follow `.cursor/rules/performance_memory.mdc` and existing PoolManager in Scripts/Core.
