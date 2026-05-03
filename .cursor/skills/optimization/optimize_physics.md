# Optimize physics usage

**Focus on:**

- Use physics only where needed; remove unnecessary Rigidbody or colliders.
- Prefer simple colliders (box, sphere, capsule) over MeshCollider when sufficient.
- Reduce continuous collision or high-frequency checks; use layers and query filters.
- Move heavy physics or pathfinding to Jobs where applicable; see `memory-bank/techContext.md` (Burst, Jobs).
- Follow `.cursor/rules/performance_physics.mdc`, `collider_rules.mdc`.
