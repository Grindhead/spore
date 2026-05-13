# Gameplay Revision Pass 11 TODO

Scope: continue improving Cell-stage goal reliability after the escape predator and nutrient bloom passes.

## Review Findings

- The authored goal chain asks the player to "eat larger prey" after the nutrient bloom.
- The normal predation rule rejects organisms larger than the player, so the goal could be requested without a fair way to complete it.
- The stage did not seed a readable hunt target when the goal appeared.

## Tasks

- [x] Review the Eat Larger Prey goal path and player predation rules.
- [x] Seed a goal-specific larger prey opportunity when the hunt goal starts.
- [x] Make the spawned prey visibly larger but only by a small, readable margin.
- [x] Mark spawned larger prey as a special vulnerable goal target so normal size-balance rules remain intact.
- [x] Add focused EditMode coverage for larger-prey spawn geometry, target sizing, and player consume eligibility.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Eat Larger Prey now presents a fair world opportunity: when the goal starts, the stage seeds a fresh marked larger prey near the player if a matching goal prey is not already nearby.
- Repeatable larger-prey goals reseed after partial progress, so the next target is sized against the player's newly grown scale.
- The spawned prey is tuned to be visibly larger than the player but still within the special vulnerable-prey consume window.
- Normal predation sizing remains intact; only marked goal prey can use the larger-prey consume exception.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
