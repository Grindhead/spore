# Game Improvement Suggestions — Project Spore (Cell Stage)

Prioritized suggestions to improve depth, feel, and polish of the cell-stage experience. Aligned with **projectbrief.md**, **evolutionPrinciples.md**, and **cellStageOrganismDesign.md**. Update this doc as suggestions are implemented or deprioritized.

---

## 1. Feel & Juice (High impact, low–medium effort)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Screen shake** | Light camera shake on eat (food consumed), damage taken, and evolution complete | **Done:** CameraManager.RequestShake; triggered on PlayerCell.Consume (0.12, 0.1 s), goal complete (0.2, 0.15 s), damage taken (0.18, 0.12 s), and evolution reached (0.25, 0.2 s). |
| **Eat feedback** | Short scale pulse or particle burst on successful consume; distinct for Plant vs Meat vs Toxin | **Done:** PlayerCell.PlayEatPulse(FoodType): EatPulseRoutine coroutine lerps scale to peak (Plant 1.15×, Meat 1.20×, Toxin 1.30×) over 0.07 s then contracts over 0.12 s; _eatPulseScaleMult applied in UpdateVisualScale. |
| **Goal complete celebration** | Clear visual/audio on micro goal complete (e.g. DNA pop, brief highlight on HUD goal text) | **Done:** CellStageHUD shows "Goal complete! +3 DNA" for 2.5 s on CellGoalCompletedEvent (cell.goal.complete_message) |
| **Haptic feedback** | Gamepad rumble on eat, damage, low energy, predator proximity (when supported) | **Done:** Eat/damage haptics wired earlier; **Low-mass danger**: PlayerCell.UpdateLowMassHaptic — pulses every 2.5 s below mass 1.0, intensity scales with proximity to death (0.2–0.7); **Predator proximity**: CellTutorialController.ScanPredators fires haptic (0.35, 0.15, 0.12 s) every 3 s when predator within 10 units. |

---

## 2. Evolution & Progression (High impact, medium effort)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Evolution pressure from behaviour** | Track player stats (e.g. times attacked, times fled, algae eaten, meat eaten) and bias the 3 mutation options toward relevant traits (defence, speed, digestion, attack) | **Done:** `BehaviourBiasTracker` records eat/flee/damage events; `MutationSystem.MutateBiased(genome, tracker)` weights options toward relevant traits; wired into `PlayerCell` and `MutationChoiceUI`. |
| **Evolution milestones** | In-game popups or toasts: "First speciation", "First extinction", "10 species in the tree", "Survived 5 minutes in toxic zone" | **Done:** CellTutorialController extended: OnSpeciesExtinct fires `tutorial.cell.tip.first_extinction` once; OnNewSpecies checks `GetAllSpecies().Count >= 10` and fires `tutorial.cell.tip.ten_species`; 2 new localization keys; MilestoneToastTests (4 tests). |
| **Rare mutation events** | Environmental trigger (e.g. "Radiation surge") → offer 1 extra or forced random mutation with visible world cue (e.g. glow, particle field) | **Done:** RadiationSurge CellEnvironmentEventType; PlayerCell.OfferRadiationMutation(); HUD yellow-gold banner; 7 EnvironmentEventTests. |
| **Stronger niche filling** | When a trophic role drops (e.g. no herbivores), spawn new herbivore-biased organisms or increase mutation bias toward that role in next spawn batch | **Done:** `CellStage.ClassifyTrophicRole(Genome)` classifies organisms as Herbivore/Carnivore/Omnivore (±2-point Attack vs Digestion threshold); `UpdateNicheFill` now counts herbivores and carnivores separately; forces ≥2 herbivores and ≥1 carnivore before checking total count; targets the most-depleted role rather than blindly alternating; `TrophicRoleTests` (7 EditMode tests). |
| **Filter feeder NPC archetype** | Paramecium-inspired passive organism: drifts on cilia, absorbs food within a fixed radius without chasing; high-Digestion genome → Herbivore; provides safe slow-feeding ecological niche | **Done:** `FilterFeederCell` MonoBehaviour: `AbsorptionRadius = 3f`, sinusoidal cilia drift at `WanderSpeedFraction = 0.35×`, passive absorption tick every 0.3 s via `FoodManager.GetNearestFood`; `BuildGenome()` returns Digestion 7, Attack 0, Speed 2, Flagella 5; every 6th organism (i%6==0) in `CellStage.SpawnOrganisms()` is spawned as a filter feeder; `FilterFeederCellTests` (14 EditMode tests). |
| **Scavenger NPC archetype** | Completes the food web (Algae→Herbivore→Predator→**Scavenger**): actively pursues Meat (dead-cell) food; fast but non-aggressive | **Done:** `ScavengerCell` MonoBehaviour: `MeatSeekRadius = 20f`, `ConsumeRadius = 1.8f`, active pursuit at full genome speed, slow wander fallback; `BuildGenome()` returns Speed 4, Digestion 6, Attack 1 (Herbivore trophic role for niche-fill); `FoodTracker.GetNearestOfType` + `FoodManager.GetNearestFoodOfType` added; every 6th organism (i%6==3) spawned as scavenger; `ScavengerCellTests` (18 EditMode tests). |
| **Bacteria swarm NPC archetype** | Tiny, fast organisms that flock via simplified boids (cohesion + separation + wander); high metabolism forces constant sweeping of food areas | **Done:** `BacteriaSwarmCell` MonoBehaviour with boids: `SwarmRadius = 8f`, `SeparationRadius = 1.5f`, cohesion/separation/wander weighted blend computed every `SwarmTickInterval = 0.28s` via `Physics.OverlapSphere`; staggered tick phase; `BuildGenome()` → Size 0.35, Speed 6, Metabolism 1.2, Flagella 6, Omnivore trophic role; spawned at i%6==1 with 3× energy; `BacteriaSwarmCellTests` (19 EditMode tests). |

---

## 3. Micro Goals & Hazards (Medium impact, low–medium effort)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **More goal types** | "Survive N seconds in toxic zone", "Eat during a nutrient bloom", "Reproduce once", "Eat 1 prey larger than you" | **Done:** All four implemented. `GoalType` enum has `ReproduceOnce`, `SurviveInToxicZone`, `EatDuringNutrientBloom`, `EatLargerPrey`; `CellAteLargerPreyEvent` fires from `PlayerCell.ConsumeCell`; all goal types have factory/apply methods, HUD display cases, localization keys, and tests. |
| **Goal-linked tutorial** | When first goal is shown, trigger a short tutorial step: "Eat algae to complete this goal and earn DNA" | **Done:** CellStage.ShowGoalLinkedTutorialTip() called from OnEnter when first goal is set; checks TutorialOverlay.Progress.IsStageCompleted(Cell) — only shown on fresh runs; TutorialOverlay.Progress property exposed; tutorial.cell.goal_intro localization key added. |
| **Predator pack behaviour** | PredatorCells occasionally coordinate (e.g. move toward same prey, simple flanking) | **Done:** PredatorPackCoordinator static service: Register/Unregister on Awake/OnDestroy; GetFlankTarget returns direct playerPos for leader (index 0), flanks ±5 units perpendicular for subsequent chasers; PredatorCell.IsChasing exposed; Chase ExecuteState uses GetFlankTarget; 7 PredatorPackCoordinatorTests. |
| **Territorial predator behaviour** | Predators guard food-rich zones (AlgaeForest band); return home if a chase takes them too far | **Done:** `PredatorCell.SetTerritory(origin, radius)` adds a `Return` state to the FSM; territorial wander uses `PickTerritoryWaypoint()` to patrol within the zone; chase → Return when `dist from origin > radius × 2`; `CellStage.SpawnPredators()` spawns even-indexed predators in AlgaeForest band (0.62–0.88 × arenaRadius) with territory radius 12f; `TerritorialPredatorTests` (9 tests). |

---

## 4. World & Environment (Medium impact, medium effort)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Current indicator on HUD** | When player is inside ArenaCurrent, show small arrow or icon indicating current direction/strength | **Done:** CellStage.TryGetCurrentAtPlayer; ArenaCurrent.FlowDirection/Force; HUD shows "Current →" (cell.current.indicator) when in current |
| **Chemotaxis / biome-affinity AI** | NPC organisms drift toward the biome that suits their genome: Photosynthetic / herbivore → AlgaeForest; carnivore → OpenWater; low-metabolism → DeepZone | **Done:** `CellOrganismAI.GetPreferredRadiusFraction(genome)` returns preferred orbit fraction; `GetBiomeAffinityDrift(pos, center, radius, genome)` computes radial drift vector; blended 35% into `GetNextWanderDirection()` — organisms self-organise into their ecological zones; `ChemotaxisTests` (14 EditMode tests). |
| **Bioluminescence from genome/energy** | Cell glow intensity or colour tint from genome (e.g. Colour gene) or current energy (low energy = dimmer) | **Done:** `Organism.UpdateBioluminescence()` scales emission 0–0.55× with energy/100; `PlayerCell.UpdateVisualScale` scales emission 0.1–0.7× with NormalizedMass using genome colour. |
| **Living currents** | Expand ArenaCurrent into directional flows that push organisms; optional whirlpool zones that pull toward centre | **Done:** `ArenaCurrent` applies directional flow force; `WhirlpoolZone` applies inward pull + tangential spin; both spawned in `CellStage.SpawnHazards`; HUD shows current indicator. |

---

## 5. Balance & Tuning (Ongoing)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Playtest-driven constants** | After playtests, tune BalanceConstants: CellGoalPredatorThreatRadius/SafeRadius, CellDNAPointsPerGoalComplete, evolution mass threshold, reproduction cost | **Done:** `docs/BALANCE_AND_PERFORMANCE.md` updated — all current values corrected (reproduction threshold 6, cost 3; speciation 8; threat 18/safe 28); genetic trait constants table added; difficulty multiplier table added; benchmark baseline from `CellSpatialGridPerformanceTests`; recommended tuning order; full reference links. |
| **Difficulty or run modifiers** | Optional "Hard run": fewer algae, more predators, faster metabolism; "Easy": more food, slower predators | **Done:** `DifficultyModifiers` struct with all 6 multipliers; `NewGameOptionsUI` lets player pick Easy/Normal/Hard; `CellStage` applies FoodCount/PredatorCount/NPCCount; **now fully wired:** `MetabolismMultiplier` applied in `CellOrganismAI.GetMetabolismMultiplier` lambda; `EvolutionThresholdMult` applied in `PlayerCell.Awake()` before EvolutionTracker creation; `DNARewardMultiplier` applied via `Modifiers.ApplyDNAReward()` at all 4 goal-completion reward sites in CellStage; 4 new `GameDifficultyTests`. |

---

## 6. Accessibility & Options (Medium impact, low effort)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Color-blind friendly modes** | Alternate palettes or symbols for cell types (plant/meat/toxin), biome zones, and phylogenetic tree (extinct vs active) | **Done:** ColorBlindMode enum (None/Deuteranopia/Protanopia/Tritanopia); GameSettings.ColorBlindModeIndex; AccessibilityPalette static class with GetFoodColor(type, mode) and GetBacteriaColor(mode); FoodManager.SpawnInArena + AddFoodAt use palette; SettingsUI graphics tab cycle row; 5 localization keys; 10 AccessibilityPaletteTests. |
| **High-contrast outlines** | Optional outline or border on player cell and key hazards (predators, toxin clouds) | **Done:** `GameSettings.HighContrastMode` bool; `HighContrastOutlineManager` MonoBehaviour scans every 0.5 s — PredatorCells glow bright red, ToxinClouds bright yellow; PlayerCell.UpdateVisualScale uses bright cyan emission override; SettingsUI graphics tab toggle; `settings.high_contrast` localization key; 2 new SettingsTests + HighContrastOutlineManagerTests (7 tests). |

---

## 7. Performance & Scale (Before 10k+ organisms)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Profile spatial grid** | Measure CellSpatialGrid with 500+ organisms and 200+ food items; confirm GetNearestInRadius and AI tick stay within frame budget | **Done:** `CellSpatialGridPerformanceTests` (6 tests) — bulk register 700 entries < 50 ms, AI tick 500 queries < 50 ms, bulk update 500 positions < 30 ms; category filtering and count correctness verified; cell size asserted against BalanceConstants. |
| **LOD for distant cells** | Beyond N units from camera: simplified mesh or billboard for organisms; full detail only when nearby | **Done:** `OrganismLOD` MonoBehaviour disables spike/flagella child GameObjects when >30 units from camera (checked every 0.25 s, staggered per organism id). Auto-attached in `Organism.Create()`; `RefreshChildren()` called on `Reinitialize`. |
| **Pool organisms** | Ensure Organism GameObjects are pooled (spawn/despawn) instead of instantiate/destroy | **Done:** `Organism.SetPoolCallback(Action<Organism>)` / `Die()` fires callback for pool return; `Organism.Reinitialize(genome, position, energy)` resets state for reuse; `OrganismPoolTests` (EditMode) verifies callback, reinitialize, and pool round-trip. |

---

## 8. Multiplayer Readiness (When starting Phase 9)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Viral Particles + Biome HUD** | VirusOutbreak now spawns 18 magenta ViralParticle organisms (seek cells, drain mass on contact; Armoured trait halves player drain). Biome zone label in HUD bottom-left, colour-coded per zone. `CellPlayerInfectedEvent` flash. | **Done:** `ViralParticle.cs`; `CellEnvironmentEventManager.SpawnViralParticles`; `GameEvents.CellPlayerInfectedEvent`; `BalanceConstants` viral block; `Localization` biome/virus keys; HUD infection flash + biome indicator; 12 `ViralParticleTests` + `LocalizationTests` extended. |
| **Giant Leviathan** | Rare enormous predator (8× scale) emerges after 90s + 3 species; Patrol/Hunt/Charge/Rest FSM; telegraphed dash; drops food burst on death; two achievements (survived/defeated) | **Done:** `LeviathanCell.cs`; CellStage spawn logic; HUD red warning banner; BalanceConstants leviathan block; AchievementSystem + Service; LocalizationTests; `LeviathanCellTests` (13 EditMode tests). |
| **Sync contract** | Define minimal serializable state per tick: player position, velocity, genome checksum, energy; organisms: id, position, genome checksum, speciesId | **Done:** `CellNetworkState.cs` (PlayerNetState, OrganismNetState, FoodNetState, SessionNetState, RemoteInputPacket, GenomeChecksum FNV-1a); `NetworkSerializer.cs` binary round-trips; 38 `MultiplayerTests`. |
| **Session model** | Document: host/join, invite-by-code, drop-in/drop-out, host migration; platform (Steam vs console) constraints | **Done:** `MultiplayerSessionManager.cs` — max 4 players, 6-char invite codes, host migration; `MultiplayerLobbyUI.cs` procedural overlay; `GameManager.SetSession()`; `NullNetworkTransport` stub until NGO package available. |
| **Authority** | Decide authority for food spawn, organism birth/death, and environmental events (host vs server) | **Done:** `HostAuthorityController.cs` — host/offline authoritative, client defers; wired into `CellStage` (SpawnFood/NPCs/Organisms/NicheFill/Leviathan guards) and `CellEnvironmentEventManager` (TriggerRandomEvent guard); `GameManager.Session` + `SetSession()` integration point. |
| **Game tick loop** | Host broadcasts SessionNetState at 20 Hz; clients receive and reconcile organism/player positions; remote player ghosts | **Done:** `NetworkTickManager.cs` — 20 Hz host broadcast loop; client receive+apply; packet type-tag protocol (0x01 session, 0x02 input); `RemotePlayerCell.cs` — ghost with per-player colour palette + lerp; wired into `CellStage.OnEnter/OnExit`; `GameManager.SessionTransport` + `SetSession(session, transport)` overload; `NetworkTickManagerTests` (20 EditMode tests). |
| **Relay server** | Self-hostable WebSocket relay: session management, packet relay, host migration | **Done:** `server/relay-server.js` (Node.js, ws package); `server/security.js` (RateLimiter, validators, constant-time auth); GAME_TOKEN auth, connection/message/join rate limits, idle timeout, session TTL, input validation, graceful shutdown; 34 server unit tests (`npm test`). `WebSocketNetworkTransport.cs` — INetworkTransport over ClientWebSocket with auth token support. |

---

## 9. Audio & Localization (Polish)

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Eat/evolution SFX** | Distinct sounds for Plant, Meat, Toxin consume; evolution complete and mutation choice appear | **Done:** SFXType.FoodEatenPlant/Meat/Toxin + EvolutionComplete + MutationChoiceShown; PlayerCell.Consume maps food type → SFX; MutationChoiceUI.Show plays MutationChoiceShown; OnEvolutionReached plays EvolutionComplete; catalog IDs in MusicTrackCatalog + AUDIO_CATALOG.md; 3 new AdaptiveMusicTests. |
| **Localize new strings** | Any new goal types, milestones, or tutorial steps get keys in Localization and EN table | **Ongoing:** All new keys added each session are covered by `LocalizationTests`. Current EN table complete for all implemented features. |

---

## 10. Documentation & Ops

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Playtest log template** | Short template: build version, constants used, 3 things that felt good, 3 that felt off, balance notes | **Done:** `docs/PLAYTEST_LOG_TEMPLATE.md` — session info, key BalanceConstants table, good/off/balance/bugs/tuning sections, 1–5 rating axes; linked from PLAYTEST_CHECKLIST.md. |
| **Changelog** | Keep a simple CHANGELOG.md (or progress.md "Recent Completed") for each release so testers know what changed | **Deferred:** progress.md "What Works" section already serves this purpose; no separate CHANGELOG.md needed at this stage. |

---

## 11. Roguelike Meta-Progression

| Suggestion | Description | Notes |
|------------|-------------|--------|
| **Run Records / Personal Bests** | Track best time survived, peak mass, DNA, species count, traits across all runs; show "★ New Best!" in Run Summary | **Done:** `RunRecord.cs` (pure C# serializable class, JSON round-trip, `Update` returns `NewBestFlags`); `SaveData.RunRecordJson`; `GameManager.UpdateRunRecord/GetRunRecord/RecordLeviathanSurvived/Defeated`; `RunSummaryUI` shows new-best badge and run count; 15 `RunRecordTests` (EditMode). |

---

## Priority summary

- **Quick wins:** Screen shake, eat feedback, goal celebration, current indicator on HUD, haptics, color-blind option.
- **High value:** Evolution pressure from behaviour, evolution milestones, more goal types, niche filling.
- **Before scale:** Profile spatial grid, pool organisms, LOD for distant cells.
- **When starting multiplayer:** Sync contract, session model, authority.

Update **progress.md** and **activeContext.md** when implementing items from this list; mark items "Done" or "Deferred" in this file with a short note.
