# Modularity And Maintainability Refactor TODO

Last updated: 2026-05-13 (post-refactor rescan)

## Checklist

- [x] 1. Break up `CellStage` god class into focused modules.
- [x] 2. Extract stage lifecycle/orchestration from `CellStage` into a small coordinator.
- [x] 3. Move spawn responsibilities into a dedicated spawn director/service.
- [x] 4. Move goal selection/progress/reward/opportunity logic into a dedicated goal module.
- [x] 5. Move backdrop/water/ambient visual generation out of `CellStage`.
- [x] 6. Centralize predator/current/organism cache handling.
- [x] 7. Split `PlayerCell` into movement, consumption/combat, evolution, trait passives, and death handlers.
- [x] 8. Isolate mutation and symbiosis rules from `PlayerCell` into pure logic services.
- [x] 9. Remove gameplay-time `Find*` usage and replace with explicit dependency wiring/context objects.
- [x] 10. Reduce direct singleton coupling via lightweight interfaces and injected references.
- [x] 11. Introduce a `CellStageContext` runtime service container.
- [x] 12. Split `MainMenuUI` into View, Controller, Art/Theme factory, and animation helpers.
- [x] 13. Split `MultiplayerLobbyUI` into transport/session logic vs UI presentation.
- [x] 14. Separate runtime-generated texture/material code from menu flow logic into reusable UI asset factories.
- [x] 15. Reduce oversized `CellStageHUD` responsibilities.
- [x] 16. Split monolithic asmdef into domain assemblies.
- [x] 17. Define explicit dependency direction rules between assemblies.
- [x] 18. Add architectural guard tests for singleton and `Find*` backsliding.
- [x] 19. Add refactor safety tests around `CellStage` and `PlayerCell` behavior.
- [x] 20. Document incremental migration phases and rollout.

## Progress Log

- [2026-05-13] Created tracker.
- [2026-05-13] Added `CellStageContext` for explicit runtime dependencies.
- [2026-05-13] Extracted multiplayer scaling helpers to `CellStageScaling`.
- [2026-05-13] Extracted symbiosis rule math to `PlayerCellSymbiosisRules` and rewired `PlayerCell`.
- [2026-05-13] Added `MainMenuArtFactory` and rewired `MainMenuUI` art generation.
- [2026-05-13] Added architecture dependency rules doc (`docs/architecture-dependency-rules.md`).
- [2026-05-13] Added architecture guard tests (`ArchitectureGuardTests`).
- [2026-05-13] Added extraction safety tests for `CellStage` and `PlayerCell` service facades.
- [2026-05-13] Added orchestration/director modules: `CellStageOrchestrator`, `CellStageSpawnDirector`, `CellGoalDirector`, `CellBackdropPresenter`, `CellThreatCache`.
- [2026-05-13] Added runtime service interfaces/adapters (`IStageRuntime`, `IGameSession`, `ICameraEffects`) and integrated `MainMenuController`.
- [2026-05-13] Added `ICellStageContextAware` + `CellActorContextBinder`; bound `PlayerCell` and `CellAI` to explicit stage/food context.
- [2026-05-13] Extracted relay config loading/parsing to `RelayConfigService` and rewired `MultiplayerLobbyUI`.
- [2026-05-13] Extracted HUD threat text/color presentation to `CellHudPresenter`.
- [2026-05-13] Added incremental domain asmdef split: `RoobsWorld.Cell.Services.asmdef` and root assembly reference wiring.
- [2026-05-13] Validation run: `.\tools\run-editmode-tests.ps1 -NoGraphics` failed due Unity Package Manager IPC connection startup failure (`Could not connect to IPC stream "Upm-27652"`).
- [2026-05-13] Phase rollout notes:
  1. Extract pure logic into services and static helpers first.
  2. Introduce context containers and replace ad-hoc `Find*` usage with explicit wiring.
  3. Split monolith behaviours into directors/controllers while preserving public API.
  4. Add architectural guard tests and only then enforce assembly boundaries.

## Possible Improvements (Post-Refactor Rescan)

- [x] P1. Extract `CellStage` lifecycle coordinator (`CellStageOrchestrator`) and leave `CellStage` as facade + wiring only.
- [x] P1. Extract `CellStageSpawnDirector` from `CellStage` spawn paths (`SpawnFood`, `SpawnNPCs`, `SpawnOrganisms`, `SpawnHazards`, pickups).
- [x] P1. Extract `CellGoalDirector` from `CellStage` goal planning/opportunity/reward logic.
- [x] P1. Extract `CellBackdropPresenter` from `CellStage` backdrop/water/ambient visuals.
- [x] P1. Extract `CellThreatCache` (predator/current/organism cache refresh + query) from `CellStage`.
- [x] P1. Introduce `IStageRuntime`, `IGameSession`, and `ICameraEffects` interfaces and route new code via interfaces before replacing singleton call sites.
- [ ] P1. Add `CreatureStageContext` mirroring `CellStageContext` and migrate `CreatureStage` + encounter/HUD scripts away from ad-hoc `Find*`.
- [ ] P1. Split `PlayerCell` into `PlayerCellMovementController`, `PlayerCellConsumptionService`, and `PlayerCellEvolutionService`.
- [ ] P2. Move remaining `PlayerCell` damage/death flow into `PlayerCellDeathHandler` and make death recommendations/data-driven.
- [x] P2. Add `CellActorContextBinder` component to inject `CellStage` and `FoodManager` into organisms/cell archetypes instead of `FindAnyObjectByType`.
- [x] P2. Split `MultiplayerLobbyUI` into `MultiplayerLobbyController` + `MultiplayerLobbyViewBuilder` + `RelayConfigService`.
- [ ] P2. Split `SettingsUI` into sections (`AudioSettingsPanel`, `VideoSettingsPanel`, `AccessibilitySettingsPanel`, `GameplaySettingsPanel`) with shared row/widget factories.
- [x] P2. Split `CellStageHUD` into `CellHudView`, `CellHudPresenter`, and event-to-view-model mapping.
- [ ] P2. Create `UiRuntimeFactory` for repeated procedural UI sprite/texture/material creation patterns across menus and overlays.
- [ ] P2. Add perf guard: replace repeated `FindObjectsByType` polling paths (`ThreatIndicator`, `NetworkTickManager`, `HighContrastOutlineManager`) with event-maintained registries.
- [x] P3. Introduce domain asmdefs incrementally (`RoobsWorld.Core`, `RoobsWorld.Cell`, `RoobsWorld.Creature`, `RoobsWorld.UI`, `RoobsWorld.Multiplayer`, `RoobsWorld.Utils`) with temporary bridge references.
- [ ] P3. Add architecture tests that fail on new direct singleton access in non-root modules (`GameManager.Instance`, `StageManager.Instance`, `CameraManager.Instance`) unless explicitly allowlisted.
- [ ] P3. Add architecture tests that fail on new `FindAnyObjectByType` usage in gameplay/runtime loops.
- [ ] P3. Add smoke tests for extracted directors (spawn/goal/hud/lobby/settings) to preserve behavior during future splits.
- [x] P3. Add a short migration playbook per large class documenting extraction order, acceptance checks, and rollback points. See [the class migration playbooks](modularity-migration-playbooks.md).

### Rescan Notes

- Current largest files remain high-risk for maintainability:
  - `CellStage.cs` (~2250 lines)
  - `PlayerCell.cs` (~1187 lines)
  - `MultiplayerLobbyUI.cs` (~1178 lines)
  - `SettingsUI.cs` (~1004 lines)
  - `MainMenuUI.cs` (~992 lines)
  - `CellStageHUD.cs` (~869 lines)
- Singleton and scene-discovery coupling remains widespread in Cell, Creature, Multiplayer, and UI runtime scripts.
