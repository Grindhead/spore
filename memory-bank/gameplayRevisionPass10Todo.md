# Gameplay Revision Pass 10 TODO

Scope: continue improving early Cell-stage goal reliability after the Creature objective passes.

## Review Findings

- The early "Escape predator" goal depended on the player naturally finding the single predator, which could stall the goal chain during quiet runs.
- Predator escape progress already had good threat/safe distance logic, but the stage did not guarantee a readable opportunity when the goal appeared.
- The escape goal tutorial warned the player, yet the world could fail to present a nearby predator to react to.

## Tasks

- [x] Review Cell-stage goal progression and predator escape detection.
- [x] Seed a fair predator opportunity when the escape goal starts and no predator is already nearby.
- [x] Place the goal predator behind/offset from the player within threat range, not on top of the player.
- [x] Avoid spawning an extra goal predator when a predator is already within the escape-safe radius.
- [x] Add focused EditMode coverage for goal predator spawn geometry.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Escape Predator now presents a reliable world opportunity: when the goal starts and no predator is already close, the stage seeds a smaller predator behind/offset from the player inside threat range.
- The spawn math keeps the predator off the player, behind their facing direction, and clamped inside the arena.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
