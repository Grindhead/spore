# Migrate the large runtime classes

Use these plans for the remaining work in [the refactor tracker](modularity-refactor-todo.md).
The dependency direction is defined in [the architecture rules](architecture-dependency-rules.md).
Keep gameplay limited to Cell and Creature, as required by [the system patterns](../memory-bank/systemPatterns.md).

## Establish a baseline

1. Connect the Unity MCP server to this checkout and inspect the editor state.
2. Record the branch, commit, Unity version, active scene, and scene overrides.
3. Run all EditMode tests and retain the fresh NUnit XML and editor log.
4. Run the relevant flow below in PlayMode and record its visible result.
5. Commit each extraction only after compilation, tests, and the same flow pass.

Use the project's Unity version from `RoobsWorld/ProjectSettings/ProjectVersion.txt`.
An editor process that exits successfully without fresh test XML does not pass the test gate.
Keep scene, prefab, and script changes for one extraction in the same commit.

For every class, capture the baseline commit before the first extraction.
If an acceptance check fails, stop the next extraction and fix or revert the affected commit.
After a revert, reimport through Unity MCP, rerun the tests, and repeat the baseline flow.
Restore the related serialized references with the scripts so scenes cannot retain missing component bindings.

## CellStage

The tracker names the existing orchestration, spawn, goal, backdrop, and threat modules.
Preserve those ownership boundaries while reducing the facade's remaining work.

Extraction order:

1. Confirm `CellStageContext` supplies the stage, food, and runtime service dependencies.
2. Move lifecycle sequencing into `CellStageOrchestrator`.
3. Move entity creation and pool ownership into `CellStageSpawnDirector`.
4. Move goal selection, progress, rewards, and opportunities into `CellGoalDirector`.
5. Move visual setup into `CellBackdropPresenter` and threat queries into `CellThreatCache`.
6. Remove migrated logic from `CellStage` after its callers use the owning module.

Acceptance checks:

- Start a new Cell run, complete a goal, die, and start a second run.
- Confirm each enter creates one set of stage objects and each exit releases the stage's subscriptions and objects.
- Confirm pooled entities regain their context after reuse and goals award their reward once.
- Run director smoke tests, context tests, and all EditMode tests.

Rollback point: keep a separate commit for each module. Revert the first failing module and its caller wiring together.

## CreatureStage

Extraction order:

1. Add `CreatureStageContext` using the explicit dependency pattern in `CellStageContext`.
2. Bind the player, encounters, objective state, and stage-facing HUD dependencies when the stage enters.
3. Migrate encounter code to the context before migrating HUD code.
4. Move repeated discovery out of runtime loops, then remove the superseded lookups.
5. Clear context references and event subscriptions when the stage exits.

Acceptance checks:

- Evolve from Cell to Creature and confirm the player, encounters, and objective HUD use the same stage instance.
- Exercise combat and social objectives, including death and stage reentry.
- Confirm unloaded encounters cannot update the next run's HUD.
- Run context, objective, and stage transition tests, followed by all EditMode tests.

Rollback point: revert each consumer migration with its binding changes. Revert the context after all of its consumers are removed.

## PlayerCell

Extraction order:

1. Keep context binding in `CellActorContextBinder` and preserve the existing symbiosis rules.
2. Move movement and input translation into `PlayerCellMovementController`.
3. Move food, cell consumption, combat rewards, and consumption events into `PlayerCellConsumptionService`.
4. Move evolution thresholds, part choices, reproduction, and genome updates into `PlayerCellEvolutionService`.
5. Move damage, death, and death recommendations into `PlayerCellDeathHandler`.
6. Store recommendation data with the project's configuration data and remove migrated branches from `PlayerCell`.

Acceptance checks:

- Compare keyboard and controller movement, mass-dependent speed, and hazard response with the baseline.
- Consume food and a smaller cell, then confirm mass, DNA, and events change once.
- Complete evolution and reproduction choices, then save and load the updated genome.
- Die once, verify the recommendation, and restart without a duplicate death event.
- Run service and extraction safety tests, then all EditMode tests.

Rollback point: use one commit per responsibility. Restore public callers and serialized component references with the extracted responsibility.

## SettingsUI

Extraction order:

1. Move shared row and widget construction into a factory with no settings persistence ownership.
2. Extract `AudioSettingsPanel`, then `VideoSettingsPanel`.
3. Extract `AccessibilitySettingsPanel`, then `GameplaySettingsPanel`.
4. Keep panel composition and navigation in `SettingsUI`.
5. Remove repeated widget construction after each panel uses the shared factory.

Acceptance checks:

- Open settings from both the main menu and pause menu.
- Change a setting in every panel, close settings, reopen it, and verify the saved value.
- Verify keyboard and controller focus, localization, scaling, high contrast, and cancel navigation.
- Confirm opening and closing settings does not duplicate callbacks or runtime UI resources.
- Run settings smoke tests and all EditMode tests.

Rollback point: revert one panel with its factory call sites. Preserve unrelated saved preference keys.

## MultiplayerLobbyUI

Extraction order:

1. Keep relay parsing and configuration in `RelayConfigService`.
2. Move connection, session, lobby discovery, and error state into `MultiplayerLobbyController`.
3. Move view construction into `MultiplayerLobbyViewBuilder`.
4. Bind controller state to the view through explicit callbacks or events.
5. Remove superseded transport and view code from `MultiplayerLobbyUI`.

Acceptance checks:

- Host a session, join it from another client, leave, and join again.
- Exercise an invalid code, a failed connection, cancellation, and a successful retry.
- Confirm one connection attempt produces one result and closing the lobby releases its subscriptions.
- Run lobby and relay configuration tests, the Node relay suite, and all EditMode tests.

Rollback point: keep transport extraction separate from view extraction. Revert each extraction with its event binding and cleanup changes.

## MainMenuUI

Extraction order:

1. Keep generated art in `MainMenuArtFactory`.
2. Keep navigation and game-session actions in `MainMenuController` through runtime interfaces.
3. Separate view construction from animation updates.
4. Move reusable visual creation into `UiRuntimeFactory` only after another consumer uses the same operation.
5. Remove duplicate menu art and navigation code after caller migration.

Acceptance checks:

- Open New Game, Load Game, Settings, and Multiplayer, then return to the menu.
- Confirm each activation performs one navigation action and input focus returns correctly.
- Repeat menu entry after a Cell run and after a Creature run.
- Verify generated textures, sprites, and materials have one owner and release on teardown.
- Run menu smoke tests and all EditMode tests.

Rollback point: revert view, animation, or shared visual changes with their resource ownership changes. Restore serialized menu references in that commit.

## CellStageHUD

Extraction order:

1. Keep view construction in `CellHudView` and threat presentation in `CellHudPresenter`.
2. Move event-to-view-model mapping into the presenter.
3. Pass goal and threat state through the stage context instead of discovering scene objects.
4. Move reusable visual creation into `UiRuntimeFactory` after menu and HUD callers agree on ownership.
5. Remove duplicate presentation and runtime polling from `CellStageHUD`.

Acceptance checks:

- Verify mass, DNA, goals, threats, time scale, and pause state update during a Cell run.
- Trigger rewards, mutation choices, and death, then verify each overlay opens once.
- Verify localization, text scaling, high contrast, and multiplayer labels.
- Exit and restart the stage, then confirm old subscriptions cannot update the new HUD.
- Run presenter, HUD, and director smoke tests, followed by all EditMode tests.

Rollback point: revert event mapping with its subscriptions and view contract. Restore resource ownership whenever a visual factory migration is reverted.

## Enforce the completed boundaries

After a consumer migration passes its flow, extend `ArchitectureGuardTests` to reject new direct singleton access outside composition roots.
Keep exceptions in an explicit allowlist with the owning composition root.
Reject new runtime-loop `FindAnyObjectByType` calls and add the extracted director's smoke test before enforcing its boundary.
Run all EditMode tests after each guard change so the guard protects a verified migration.
