# Gameplay Revision Pass 13 TODO

Scope: continue improving Cell-stage first-minute pacing and food goal reliability.

## Review Findings

- The first active Cell-stage goal is "eat plant food", but normal food spawning is biome-distributed rather than guaranteed near the player.
- Later non-toxin food goals, especially meat, can also feel stale if the player is far from the matching food type when the goal appears.
- Toxin already has a goal trail, so plant and meat needed a lighter equivalent that preserves exploration without leaving the goal empty.

## Tasks

- [x] Review first-minute feeding flow and non-toxin food goal spawning.
- [x] Add a small goal food cluster for plant/meat goals when no matching food is already nearby.
- [x] Place starter food ahead of the player in a small readable spread rather than directly on top of the player.
- [x] Reseed non-toxin food goal opportunities after partial progress if the nearby matching food is gone.
- [x] Add focused EditMode coverage for starter food geometry and nutrition values.
- [ ] Run three 8-10 minute Cell-to-Creature playtests and record boredom/confusion/another-run moments. Status: still requires human playtest sessions.

## Completion Notes - 2026-05-09

- Plant and meat food goals now seed a small nearby food cluster only when the matching food type is not already close to the player.
- The cluster appears ahead of the player in a small spread, giving the opening goal an immediate direction without erasing exploration.
- Toxin goals still use the existing toxin trail toward the toxic biome, preserving the higher-risk objective shape.
- Validation: `dotnet build RoobsWorld.slnx --no-restore -v:minimal` passed with 0 warnings and 0 errors.
- Validation: `dotnet test RoobsWorld.Tests.EditMode.csproj --no-build -v:minimal` exited 0. This Unity-style project does not print individual NUnit cases through the .NET runner.
