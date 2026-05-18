# Architecture Dependency Rules

Last updated: 2026-05-13

## Intended Assembly Boundaries

1. `RoobsWorld.Core` contains runtime services and contracts shared by all gameplay domains.
2. `RoobsWorld.CellStage` depends on `RoobsWorld.Core` only.
3. `RoobsWorld.CreatureStage` depends on `RoobsWorld.Core` and shared creature data modules.
4. `RoobsWorld.Multiplayer` depends on `RoobsWorld.Core` and transport contracts; gameplay stages depend on multiplayer via interfaces, not concrete scene lookups.
5. `RoobsWorld.UI` depends on `RoobsWorld.Core` and stage-facing view-model contracts only.
6. `RoobsWorld.Utils` must remain leaf-like (pure helpers) and avoid singleton/service coupling.

## Direction Constraints

1. Core never references stage or UI assemblies.
2. Stage implementations never directly own platform integration code.
3. UI does not call `FindAnyObjectByType` for gameplay systems; dependencies are provided by context/binders.
4. Multiplayer transport and protocol code remains independent of stage-specific behaviour.

## Runtime Wiring Rules

1. Use explicit context objects (`CellStageContext`, future stage contexts) instead of ad-hoc scene discovery.
2. Keep singletons as composition roots and forward through interfaces/contexts.
3. New pure gameplay math belongs in service classes that can be unit tested without scene bootstrapping.
