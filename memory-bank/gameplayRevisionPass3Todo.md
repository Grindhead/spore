# Gameplay Revision Pass 3 TODO

Scope: revise the latest gameplay improvements so their rewards are readable in-play, and keep the Creature stage loop moving after the first objective.

## Tasks

- [x] Surface Cell varied-diet bonuses with a short HUD message so players notice why switching food matters.
- [x] Show Creature objective DNA reward in the objective HUD text before completion.
- [x] Turn Creature objectives into a repeatable rotating encounter loop instead of stopping after one objective.
- [x] Keep first-time Creature objective achievement unlock separate from repeatable objective DNA rewards.
- [x] Add focused EditMode coverage for the new reward display and objective rotation rules.
- [x] Run available automated validation and record any Unity editor blockers.

## Validation

- `dotnet restore RoobsWorld.slnx -v:minimal` passed from `RoobsWorld/`.
- `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed from `RoobsWorld/`.
- `.\\tools\\run-editmode-tests.ps1 -NoGraphics` returned success, but Unity exited with `No valid Unity Editor license found` and did not refresh `TestResults/EditMode.xml`; the XML still has a `2026-05-08 15:40:57Z` start time.
