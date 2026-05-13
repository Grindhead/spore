# Gameplay Improvement TODO - RoobsWorld

Scope: improve the two supported gameplay stages only: Cell and Creature. Do not add later Spore-style stages.

Status: implementation checklist complete as of 2026-05-09. The remaining work is manual playtest evidence, listed separately at the bottom so the active code TODO does not pretend automated work can replace a human session.

## Gameplay Targets

- [x] Cell stage should support a tense 8-10 minute run with readable threats, meaningful mutation choices, and a visible ecosystem story.
- [x] Creature stage should support a satisfying 8-10 minute arena loop with readable combat, varied rivals, and real upgrade decisions.
- [x] The Cell-to-Creature transition should make the player's evolved DNA feel visible and consequential.

## P0 - Highest Impact

- [x] Prepare playtest logging and telemetry.
  - Done: `docs/PLAYTEST_LOG_TEMPLATE.md` exists, and `GameplayTelemetryTracker` now records run duration, deaths, goals, mutations, food by type, rival kills, and average FPS with an editor/development debug panel.
- [x] Tune Cell movement feel around acceleration, boost duration, turn speed, and size-speed penalty.
  - Done: size-speed penalty is now gentler and test-covered so large cells feel weighty without becoming unusably sluggish.
- [x] Make Cell threat reads clearer before damage happens.
  - Done: Cell threat cues now distinguish predator, leviathan, virus, and toxin hazards in the threat indicator/HUD path.
- [x] Improve Cell micro-goal pacing so goals rotate between feeding, escaping, exploring biomes, reproducing, and hunting.
  - Done: goal progression rotates action categories and has an EditMode regression test preventing consecutive category repeats.
- [x] Make mutation choices sharper.
  - Done: Cell mutation prompts offer offense, mobility/defense, and survival/ecology options with immediate trait/stat boosts.
- [x] Turn Creature upgrades into an actual player choice.
  - Done: `CreatureStage.OfferUpgradeChoiceIfReady` opens `CreatureUpgradeChoiceUI` with multiple options instead of immediately applying `GetCoreOption()`.
- [x] Add stronger Creature combat feedback.
  - Done: Creature combat now publishes hit, miss, kill, guard, block, telegraph, player damage, and low-health feedback events, with transient HUD feedback.

## P1 - Cell Stage Depth

- [x] Add build-defining cell parts rather than mostly stat bumps.
  - Done: cilia improves feeding reach/efficiency, spike improves contact damage, and jet strengthens boost duration/speed.
- [x] Make part pickups strategic.
  - Done: pickups now use part-specific strategic spawn bands and the HUD previews the nearest pickup role/distance.
- [x] Add ecosystem event chains.
  - Done: nutrient blooms attract herbivores and predators, virus outbreaks leave scavenger food, and oxygen surges reinforce herbivore/photosynthetic pressure.
- [x] Strengthen biome identity.
  - Done: each biome now has a profile with visit/leave reasons, risk level, and preferred food, surfaced through the biome HUD label.
- [x] Make the food web easier to understand at a glance.
  - Done: food, biome, part, and hazard readability is now backed by distinct palette/profile/cue helpers and HUD labels.
- [x] Add a player-facing Documentary Mode control.
  - Done: Cell HUD shows the current speed and cycling control; cycling is available outside development builds.
- [x] Improve death/retry flow.
  - Done: death summary now records why the player died, suggests a next-run adaptation, and keeps the fast retry path.
- [x] Add run-specific evolution story summary.
  - Done: run summary names the dominant behavior and signature mutations/parts from the final cell genome.

## P1 - Creature Stage Depth

- [x] Add rival archetypes with different silhouettes and tactics.
  - Done: Creature rivals rotate through charger, stalker, tank, and skirmisher archetypes with different colors, silhouettes, and AI tuning.
- [x] Add at least one defensive action.
  - Done: Creature player can use a timed guard via Jump/Interact, granting brief invulnerability with a cooldown.
- [x] Improve enemy attack telegraphs and recovery windows.
  - Done: rival attacks now wind up before resolving, can miss if the player escapes reach, and enter a punishable recovery window.
- [x] Add objective variety beyond "defeat 5 rivals."
  - Done: Creature stage now selects from rival cull, alpha hunt, and hold-feeding-ground objective templates.
- [x] Add arena landmarks and tactical terrain.
  - Done: Creature arena builds cover, high ground, a narrow passage, and a healing zone.
- [x] Connect Cell DNA to Creature abilities more visibly.
  - Done: Cell genome traits and part loadout now bias Creature body scale, colors, and starter parts during the Cell-to-Creature transition.
- [x] Add target assist and camera polish.
  - Done: Creature melee uses forward-cone target assist so close-range attacks prefer readable targets in front of the player.

## P2 - Progression, Balance, and Multiplayer Readiness

- [x] Rebalance DNA rewards across Cell food, goals, reproduction, rival defeats, and objective completion.
  - Done: reward pacing is guarded by balance tests and telemetry so playtests can tune from evidence without changing mechanics blind.
- [x] Add a lightweight gameplay telemetry/debug panel for playtests.
  - Done: runtime telemetry tracks run duration, deaths, goals completed, mutations chosen, food eaten by type, rival kills, and average FPS.
- [x] Add difficulty presets that alter behavior and pacing, not only counts.
  - Done: difficulty presets now adjust food/predator/metabolism/evolution pacing plus enemy telegraph and recovery windows.
- [x] Keep multiplayer secondary until the solo loops are fun.
  - Done: this pass stayed scoped to solo Cell and Creature readability, pacing, combat, progression, and telemetry.
- [x] Add regression tests for new gameplay rules.
  - Done: added/updated EditMode coverage for movement penalty, strategic pickups, biome identity, threat cue labels, creature telegraphs, target assist, tactical terrain, objective selection, difficulty pacing, cell-to-creature inheritance, and earlier mutation/goal/part/guard/archetype rules.

## First Three Recommended Tickets

1. [x] Creature upgrade choice: replace automatic core upgrade with a real multi-option choice modal.
2. [x] Cell mutation clarity: make each offered mutation visibly change movement, survival, feeding, or combat behavior.
3. [x] Playtest readiness: add telemetry, HUD feedback, and the playtest template so three 8-10 minute sessions can be run from evidence.

## Manual QA Still Needed

Run three real 8-10 minute Cell-to-Creature playtests when Unity can launch with a valid Package Manager/license state. For each run, fill `docs/PLAYTEST_LOG_TEMPLATE.md` and capture: first frustrating moment, first fun moment, first death/down, first "what do I do next?" moment, and at least five tuning decisions.
