# Gameplay Revision Pass 12 TODO

Scope: continue improving Cell-stage goal reliability after the larger-prey hunt pass.

## Review Findings

- The authored goal chain asks the player to collect cell parts immediately after the larger-prey hunt.
- Initial part pickups exist, but the goal can feel stale if the player has already collected nearby pickups or drifted far from remaining ones.
- Part pickups already unlock meaningful movement/combat tools, so a goal-specific pickup can also improve build momentum.

## Tasks

- [x] Review Collect Parts goal flow and part pickup spawning.
- [x] Seed a nearby part pickup opportunity when the Collect Parts goal starts and no pickup is already nearby.
- [x] Prefer still-locked utility parts for goal pickups before falling back to a repeatable pickup type.
- [x] Reseed a pickup after partial Collect Parts progress so repeatable goals stay actionable.
- [x] Add focused EditMode coverage for goal pickup placement and goal part selection.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Collect Parts now presents a nearby pickup opportunity when the goal starts and no pickup is already within the goal radius.
- Partial Collect Parts progress now reseeds the next pickup opportunity, keeping repeatable collect goals actionable without flooding the arena.
- Goal pickup selection prefers locked utility parts in this order: Cilia, Spike, Jet, then Flagella. If every part is already unlocked, it falls back to Cilia so the goal can still progress.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
