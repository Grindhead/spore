# Project Architecture - One-Page Summary

Summary for AI context. **Single source of truth:** `memory-bank/systemPatterns.md`.

## Layers

- **Presentation:** UI, cameras, shaders, VFX, animation.
- **Gameplay:** Stage FSM, creature AI, combat, progression.
- **Systems:** Save/Load, Input, Audio, Event Bus, Object Pooling, Asset Streaming.
- **Data:** ScriptableObjects, JSON configs, Creature DNA, world seeds.

## Core Patterns

1. **Stage FSM** - Stages are Cell and Creature, with one-way transition from Cell to Creature; each stage owns scene(s), camera, input, and UI.
2. **Event Bus** - Typed pub/sub (`EventBus.Subscribe/Publish/Unsubscribe`); systems communicate via events, not direct references.
3. **Hybrid ECS-style** - Components hold data or simple behavior; systems are stateless processors; logic testable and decoupled from Unity lifecycle.
4. **ScriptableObject data** - Game data in ScriptableObjects or JSON; avoid hardcoded gameplay values.
5. **Object pooling** - PoolManager for frequently spawned/despawned objects (cells, VFX, etc.).
6. **Procedural pipeline** - Seed -> noise -> height/biome -> mesh -> LOD -> collider; chunk-based, deterministic.

## Folder Structure (Unity: RoobsWorld/Assets/)

- **Scripts:** Core, Creature, Stages (Cell, Creature), World, UI, Utils; Creature/Editor; Editor/ for tools.
- **Art:** Models, Textures, Materials, Shaders, VFX.
- **Data:** CreatureParts, Biomes, Configs.
- **Scenes:** Stage scenes and CreatureEditor.

For component relationships, Creature DNA, save system, and full folder layout, see `memory-bank/systemPatterns.md`.
