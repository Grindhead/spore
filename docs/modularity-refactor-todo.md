# Modularity And Maintainability Refactor TODO

Last updated: 2026-05-13

## Checklist

- [ ] 1. Break up `CellStage` god class into focused modules.
- [ ] 2. Extract stage lifecycle/orchestration from `CellStage` into a small coordinator.
- [ ] 3. Move spawn responsibilities into a dedicated spawn director/service.
- [ ] 4. Move goal selection/progress/reward/opportunity logic into a dedicated goal module.
- [ ] 5. Move backdrop/water/ambient visual generation out of `CellStage`.
- [ ] 6. Centralize predator/current/organism cache handling.
- [ ] 7. Split `PlayerCell` into movement, consumption/combat, evolution, trait passives, and death handlers.
- [ ] 8. Isolate mutation and symbiosis rules from `PlayerCell` into pure logic services.
- [ ] 9. Remove gameplay-time `Find*` usage and replace with explicit dependency wiring/context objects.
- [ ] 10. Reduce direct singleton coupling via lightweight interfaces and injected references.
- [ ] 11. Introduce a `CellStageContext` runtime service container.
- [ ] 12. Split `MainMenuUI` into View, Controller, Art/Theme factory, and animation helpers.
- [ ] 13. Split `MultiplayerLobbyUI` into transport/session logic vs UI presentation.
- [ ] 14. Separate runtime-generated texture/material code from menu flow logic into reusable UI asset factories.
- [ ] 15. Reduce oversized `CellStageHUD` responsibilities.
- [ ] 16. Split monolithic asmdef into domain assemblies.
- [ ] 17. Define explicit dependency direction rules between assemblies.
- [ ] 18. Add architectural guard tests for singleton and `Find*` backsliding.
- [ ] 19. Add refactor safety tests around `CellStage` and `PlayerCell` behavior.
- [ ] 20. Document incremental migration phases and rollout.

## Progress Log

- [2026-05-13] Created tracker.
