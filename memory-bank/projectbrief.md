# Project Brief - Spore-like Game (RoobsWorld)

## Vision

An evolution sandbox focused on two playable stages:

- Cell stage: top-down survival and evolution loop.
- Creature stage: third-person creature gameplay.

Everything beyond Creature stage is out of scope.

## Target Platforms

- PC (Steam)
- PlayStation
- Xbox
- Nintendo Switch

## Core Requirements

### 1. Playable Stages

| Stage | Genre | Camera |
|---|---|---|
| Cell | Top-down arcade / eat-or-be-eaten | 2D orthographic |
| Creature | Third-person action | 3D follow |

### 2. Creature Systems

- Procedural creature part system and stats.
- Creature DNA serialization and runtime spawning.
- Creature editor workflow for creating variants.

### 3. Cell Systems

- Food web gameplay (eat, avoid threats, evolve).
- Mutation and progression loop.
- Tutorialized onboarding and HUD feedback.

### 4. Progression and Persistence

- Save/load with stage and progression state.
- DNA points and unlock tracking.
- Achievements with platform-provider integration points.

### 5. Multiplayer

- Shared ecosystem support for core stage gameplay.
- Session-based host/client model where supported.

## Non-Goals

- Any additional gameplay stages beyond Cell and Creature
- Mobile release
- Microtransactions

## Success Criteria

- Cell stage is stable and enjoyable.
- Creature stage is playable and transition-ready.
- Save/load, tutorial, and achievements work reliably.
- Performance is stable on target platforms.
