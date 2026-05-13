# Gameplay Revision Pass 5 TODO

Scope: gameplay review follow-up after passes 1-4. The core Cell and Creature systems are now playable, so this pass should improve replay pressure, decision variety, and player understanding rather than add new stages.

## Review Findings

- Cell micro-goals are richer than before, but the chain still ends instead of becoming a repeatable play loop. After the final toxin goal, the HUD can stop giving the player a short-term reason to keep making interesting choices.
- Cell varied-diet and biome systems reward switching behavior, but the game does not yet turn those into strategic route choices such as "risk a toxic route for a larger payout" or "hunt during a bloom before predators arrive."
- Creature combat has attack, guard, telegraph, recovery, target assist, and rival archetypes, but the player still makes mostly binary decisions: attack now, guard now, or move away. It needs one more layer of commitment or ability timing.
- Creature objectives rotate and pay out reliably now, but the objectives do not yet change how the player uses the arena. "Hold ground" should pull the player toward landmarks; "Alpha hunt" should make a specific rival feel different before the fight starts.
- Creature upgrades are real choices, but they are mostly stat deltas. The strongest next improvement is to make at least some choices build-defining, with visible combat or movement consequences.
- The Cell-to-Creature inheritance exists under the hood, but the player needs a clearer transition readout showing which Cell traits became Creature advantages.

## P0 - Next Gameplay Pass

- [x] Turn Cell micro-goals into a repeatable weighted goal deck after the authored chain completes.
- [x] Add goal weights that avoid repeating the same action category twice in a row unless the player is stuck.
- [x] Add at least three "risk route" Cell goal variants: bloom feeding, toxin collection, and larger-prey hunt with higher DNA payouts.
- [x] Add a Creature stamina or momentum meter that makes attack/guard timing a more deliberate decision.
- [x] Add one player-triggered Creature ability derived from Cell DNA or equipped Creature parts.
- [x] Make alpha rivals visually and mechanically distinct during Alpha Hunt objectives.
- [x] Add objective markers or arena pings for Hold Feeding Ground so the player knows where to go without reading text.

## P1 - Readability And Progression

- [x] Add a Cell-to-Creature transition summary panel: inherited traits, starter parts, stat changes, and first Creature-stage advantage.
- [x] Add HUD hints that explain why a varied-diet chain, bloom route, or toxin route paid out.
- [x] Add Creature objective completion stats to the HUD or telemetry: reward earned, completion count, fastest completion, and streak.
- [x] Add upgrade option tags such as "mobility", "survival", "burst damage", and "control" so choices are easier to compare.
- [x] Add one negative tradeoff to each high-impact Creature upgrade so choices feel consequential rather than obvious.
- [x] Improve healing zone readability with pulsing visuals and a short HUD cue when healing starts.

## P2 - Balance And Playtest Evidence

- [x] Add tuning constants for repeatable Cell goal rewards, goal deck weights, stamina recovery, and ability cooldowns.
- [x] Add EditMode tests for weighted goal rotation, no-repeat category rules, stamina spending/recovery, and inherited ability selection.
- [x] Add telemetry counters for Cell goal category repeats, Creature stamina exhaustion, ability usage, and objective completion time.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record the first boredom moment, first confusion moment, and first "I want another run" moment. Status: requires human playtest sessions; not safe to mark complete from static validation.
- [ ] Use playtest notes to decide whether the next pass should tune pacing, combat depth, or upgrade identity first. Status: waiting on the three playtest notes above.

## Suggested Implementation Order

1. Build the repeatable Cell goal deck and tests.
2. Add Creature stamina/momentum with a minimal HUD meter.
3. Add one inherited Creature ability from existing Cell DNA traits.
4. Make Alpha Hunt and Hold Feeding Ground objectives more spatially readable.
5. Add the Cell-to-Creature transition summary once the inherited ability is real.

## Completion Notes - 2026-05-09

- Implemented the repeatable Cell goal deck with deterministic weighted selection, no immediate action-category repeats, and risk-route DNA multipliers.
- Added Creature stamina spending/recovery for attack, guard, and inherited active abilities.
- Added Cell-to-Creature legacy ability selection, transition summary text, Creature HUD ability/stamina feedback, alpha rival promotion, Hold Feeding Ground marker, pulsing healing-zone feedback, upgrade tags/tradeoffs, and telemetry counters.
- Added focused EditMode coverage for goal rotation, risk routes, stamina spend/recovery, inherited ability selection, alpha rival rules, objective completion stats, healing-zone pulse bounds, and tuning constants.
- Validation: `dotnet restore RoobsWorld.slnx -v:minimal` passed; `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Unity EditMode runner attempted with `TestResults/EditMode-20260509-pass5.xml`, but Unity exited before producing results because Package Manager IPC could not connect after 30 seconds and reported readonly database writes in `TestResults/EditMode-20260509-pass5.log`.
