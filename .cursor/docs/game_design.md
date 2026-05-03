# Game Design - Project Spore (RoobsWorld)

High-level design summary for AI context. **Single source of truth:** `memory-bank/projectbrief.md`.

## Vision

A spiritual successor to Spore focused on the early evolution arc: single-player progression from cell life to full creature gameplay.

## Supported Stages

| Stage | Genre | Camera |
|-------|-------|--------|
| Cell | Top-down arcade / eat-or-be-eaten | 2D orthographic |
| Creature | Third-person action-adventure | 3D follow-cam |

Transition is one-way: Cell -> Creature.

## Procedural Creature Editor

- Drag-and-drop body parts onto a skeletal armature.
- Real-time mesh generation and UV mapping.
- Parts affect stats (speed, attack, social, stealth, etc.).
- Export/import creature designs (JSON + mesh data).

## Procedural World and Progression

- Deterministic seed-based generation for reproducibility.
- DNA points / evolution currency; creature genome carries forward; save/load with auto-save.

## Targets

- PC (Steam), PlayStation, Xbox, Nintendo Switch.
- 60 FPS on target hardware; full gamepad and KB/M support.

For full requirements and success criteria, see `memory-bank/projectbrief.md`.
