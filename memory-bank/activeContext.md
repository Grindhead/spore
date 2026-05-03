# Active Context - Project Spore (RoobsWorld)

## Current Scope

The project scope is now exactly two stages:

1. Cell
2. Creature

All later Spore-style stages have been removed from scope and should not be reintroduced.

## Current State

- Cell stage code is the most complete gameplay path.
- Creature systems (DNA, stats, factory, editor, animation, AI modules) exist in the codebase.
- Stage runtime now supports Cell and Creature stage types only.
- Documentation has been updated to remove references to later stages.

## Working Priorities

1. Keep Cell stage polished and testable.
2. Keep Creature stage runnable and integrated with stage transitions.
3. Maintain save/load compatibility for Cell and Creature stage values.
4. Keep tutorial, UI, and achievements aligned to Cell + Creature only.

## Guardrails

- Do not add any stage code paths beyond Cell and Creature.
- Do not add build settings entries for removed stages.
- Prefer removing stale references over leaving dead design text.

## Suggested Next Validation

1. Open Unity and verify Cell stage boot path still works.
2. Transition to Creature stage in a test flow and verify camera + spawn behavior.
3. Run EditMode tests and fix any regressions from stage enum/support changes.
