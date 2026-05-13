# Platform achievements TODO

Goal: PC achievements are fully integrated with Steam, PlayStation trophies are fully integrated with PlayStation Network services, and Xbox achievements are fully integrated with Xbox services. Local in-game achievements remain the source of truth and each platform provider mirrors successful unlocks to its platform service.

## Shared achievement foundation

- [x] Freeze the canonical in-game achievement IDs in `AchievementCatalog`.
- [x] Assign explicit platform API names/IDs for every achievement:
  - [x] Steam API name.
  - [x] PlayStation trophy ID.
  - [x] Xbox achievement ID.
- [x] Add validation tests that every `AchievementDef` has all required platform mappings.
- [x] Add a platform achievement sync path that reports already-unlocked local achievements after platform sign-in/init.
- [x] Add retry/error logging for platform reporting failures without blocking gameplay.
- [x] Add a debug-only achievement reset/reporting tool for QA builds.
- [x] Document which achievements are visible, hidden, progression-based, or one-shot on each platform.
- [x] Generate dashboard-ready achievement manifest at `docs/platform-achievements-manifest.csv`.
- [x] Add local prerequisite validator at `tools/validate-platform-achievements.ps1`.

## Steam PC

- [x] Import Steamworks.NET into the Unity project via UPM Git package `com.rlabrecque.steamworks.net#2025.162.1`.
- [x] Resolve Steamworks.NET into `Packages/packages-lock.json` with hash `af710043b9097e8ffedf74d67503c3a7eb498260`.
- [x] Steamworks.NET package supplies `STEAMWORKS_NET`; RoobsWorld references the Steamworks.NET assembly directly.
- [ ] Blocked: Replace development App ID `480` with the real Steam App ID in:
  - [ ] `Assets/Scripts/Core/PlatformServicesConfig.cs`
  - [ ] `steam_appid.txt`
- [ ] Blocked: Create all achievement API names in Steamworks using the names from `AchievementCatalog`.
- [ ] Blocked: Verify `SteamAPI.Init`, `SteamAPI.RunCallbacks`, and `SteamAPI.Shutdown` in Editor and Windows builds.
- [ ] Blocked: Verify `SteamAPI.RestartAppIfNecessary` launches through Steam in packaged builds.
- [ ] Blocked: Verify `SteamUserStats.RequestCurrentStats` succeeds before reporting unlocks.
- [ ] Blocked: Verify `SteamUserStats.SetAchievement` and `SteamUserStats.StoreStats` unlock each achievement in Steam.
- [x] Code ready: local unlocked achievements sync to Steam on next launch once Steam initializes.
- [x] Code ready: Steam overlay availability and overlay actions are exposed by `SteamPlatformProvider`.
- [ ] Blocked: Configure Steam Cloud for `Application.persistentDataPath/Saves`.
- [ ] Blocked: Test Steam Cloud conflict/reinstall flow with at least two machines or Steam accounts.

## PlayStation

- [ ] Blocked: Install and configure the PlayStation Unity SDK/plugin for supported targets.
- [ ] Blocked: Add a PlayStation-specific scripting define, for example `ROOBSWORLD_PLAYSTATION`.
- [ ] Blocked: Replace `PlayStationAchievementProvider` bridge with SDK-backed trophy unlock calls.
- [x] Map every `AchievementDef` to the planned PlayStation trophy ID.
- [ ] Blocked: Configure trophy metadata, icons, grades, hidden state, and localization in the PlayStation tooling.
- [x] Code ready: PlayStation achievement provider is bootstrapped on `ROOBSWORLD_PLAYSTATION` builds.
- [ ] Blocked: Initialize PlayStation services at startup on PlayStation builds after the PlayStation SDK is installed.
- [ ] Blocked: Bind the active signed-in PlayStation user before reporting trophies.
- [x] Code ready: platform reporting failures are caught/logged so offline/unavailable services do not block gameplay.
- [ ] Blocked: Verify every trophy unlocks on development hardware.
- [ ] Blocked: Verify hidden trophies remain hidden until unlocked.
- [ ] Blocked: Run platform compliance checks for trophy unlock behavior, user switching, suspend/resume, and network loss.

## Xbox

- [ ] Blocked: Install and configure the Xbox GDK/XSAPI Unity integration for supported targets.
- [ ] Blocked: Add an Xbox-specific scripting define, for example `ROOBSWORLD_XBOX`.
- [ ] Blocked: Replace `XboxAchievementProvider` bridge with Xbox services achievement calls.
- [x] Map every `AchievementDef` to the planned Xbox achievement ID.
- [ ] Blocked: Configure achievement metadata, gamerscore, hidden state, icons, and localization in Partner Center.
- [x] Code ready: Xbox achievement provider is bootstrapped on `ROOBSWORLD_XBOX` builds.
- [ ] Blocked: Initialize Xbox services at startup on Xbox builds after the Xbox SDK is installed.
- [ ] Blocked: Bind the active Xbox user before reporting achievements.
- [x] Code ready: platform reporting failures are caught/logged so offline/unavailable services do not block gameplay.
- [ ] Blocked: Verify one-shot achievements unlock through Xbox services.
- [ ] Not needed yet: add support for progress-based achievements if any Xbox achievement requires percentage progress.
- [ ] Blocked: Verify unlocks on development hardware and through the Xbox services sandbox.
- [ ] Blocked: Run Xbox certification checks for user switching, suspend/resume, network loss, sign-out, and achievement state sync.

## QA matrix

- [ ] Blocked: EditMode test XML is not emitted by `tools/run-editmode-tests.ps1`. Unity exits with code 0 after asset refresh and no C# compile errors are present in `TestResults/EditMode.log`, but `TestResults/EditMode.xml` is not produced.
- [ ] PC standalone without Steam running: game launches, Steam features no-op cleanly.
- [ ] PC standalone through Steam: all Steam features initialize and achievements unlock.
- [ ] PlayStation development kit online: all trophies unlock and persist.
- [ ] PlayStation development kit offline/network-interrupted: game continues and sync plan is verified.
- [ ] Xbox development kit online: all achievements unlock and persist.
- [ ] Xbox development kit offline/network-interrupted: game continues and sync plan is verified.
- [ ] Fresh save unlock flow works on every platform.
- [x] Code ready: existing local save syncs historical achievements after platform initialization.
- [ ] Platform dashboards show the expected unlock state for every achievement.

## Release gates

- [ ] Blocked: Real platform App IDs/service IDs are configured for each target in `PlatformServicesConfig`.
- [x] Release build guard blocks known placeholder platform IDs when matching platform symbols are enabled.
- [x] No development App ID or sandbox-only config can ship in release builds when matching platform symbols are enabled.
- [ ] Blocked: All platform achievement mappings are reviewed against dashboard/trophy-pack configuration.
- [x] All SDK code is compiled only for the matching platform/define.
- [ ] Blocked: Platform-specific legal/certification requirements are reviewed before submission.
- [x] Document external vendor inputs and setup handoff in `docs/platform-achievements-external-setup.md`.
- [x] Document dashboard execution steps in `docs/platform-achievements-portal-runbook.md`.

## Achievement metadata

All currently defined achievements are one-shot unlocks. None require percentage progress yet. `cell_defeated_leviathan` is hidden; all others are visible.

| Local ID | Steam API name | PlayStation trophy ID | Xbox achievement ID | Hidden | Progress |
| --- | --- | --- | --- | --- | --- |
| `cell_first_eat` | `CELL_FIRST_EAT` | `cell_first_eat` | `cell_first_eat` | No | One-shot |
| `cell_first_goal` | `CELL_FIRST_GOAL` | `cell_first_goal` | `cell_first_goal` | No | One-shot |
| `cell_grow_large` | `CELL_GROW_LARGE` | `cell_grow_large` | `cell_grow_large` | No | One-shot |
| `cell_survive_predator` | `CELL_SURVIVE_PREDATOR` | `cell_survive_predator` | `cell_survive_predator` | No | One-shot |
| `cell_all_parts` | `CELL_ALL_PARTS` | `cell_all_parts` | `cell_all_parts` | No | One-shot |
| `cell_evolve` | `CELL_EVOLVE` | `cell_evolve` | `cell_evolve` | No | One-shot |
| `cell_first_speciation` | `CELL_FIRST_SPECIATION` | `cell_first_speciation` | `cell_first_speciation` | No | One-shot |
| `cell_first_extinction` | `CELL_FIRST_EXTINCTION` | `cell_first_extinction` | `cell_first_extinction` | No | One-shot |
| `cell_10_species` | `CELL_10_SPECIES` | `cell_10_species` | `cell_10_species` | No | One-shot |
| `cell_survived_leviathan` | `CELL_SURVIVED_LEVIATHAN` | `cell_survived_leviathan` | `cell_survived_leviathan` | No | One-shot |
| `cell_defeated_leviathan` | `CELL_DEFEATED_LEVIATHAN` | `cell_defeated_leviathan` | `cell_defeated_leviathan` | Yes | One-shot |
| `creature_first_kill` | `CREATURE_FIRST_KILL` | `creature_first_kill` | `creature_first_kill` | No | One-shot |
| `creature_pack_breaker` | `CREATURE_PACK_BREAKER` | `creature_pack_breaker` | `creature_pack_breaker` | No | One-shot |
| `creature_apex_streak` | `CREATURE_APEX_STREAK` | `creature_apex_streak` | `creature_apex_streak` | No | One-shot |
