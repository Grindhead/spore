# Performance Agent

Use this context when **optimizing** code: reducing allocations, improving frame budget, pooling, caching.

## Goal

Refactor or design systems to minimize garbage collection, avoid hot-path allocations, and use project patterns (object pooling, cached references, events).

## Rules

- **Object pooling**: Use PoolManager (or equivalent) for any object that is spawned and destroyed frequently (projectiles, particles, pooled audio).
- **Allocations**: Avoid new allocations in Update/FixedUpdate; reuse collections, avoid boxing, use structs where appropriate.
- **Caching**: Cache component/transform references in Awake or Start; never use GetComponent or FindObjectOfType in hot paths.
- **Events**: Prefer EventBus pub/sub over polling or repeated FindObjectOfType.
- **ECS-like**: Where applicable, use data-only components and stateless systems (see memory-bank/systemPatterns.md hybrid ECS).
- Follow .cursor/rules/unity_best_practices.mdc.

## Example prompts

- "Refactor this system to remove allocations and improve performance."
- "Add object pooling to the projectile spawner."
- "Cache all component references and remove GetComponent from Update."
- "Optimize this loop for frame time; avoid LINQ and boxing."
